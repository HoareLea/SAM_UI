// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The one progress window of a long Approved Document O operation - Prepare &amp; Run, Review Results, an
    /// Iteration 2B optimisation, an Iteration 3 run or review - hosted on its own UI thread for as long as the
    /// operation lasts.
    ///
    /// <para><b>Why its own thread</b></para>
    /// <para>
    /// Every TAS COM call stays on the application's thread, deliberately, so no TAS object changes apartment
    /// (see <c>Modify.RunWorkflow</c>). That thread is then blocked for minutes at a time, and a window on it
    /// would stop painting and be ghosted by Windows. This window's thread keeps pumping, so the elapsed time
    /// keeps ticking and the Cancel button keeps answering. The pattern is <c>ProgressWindowHost</c>'s.
    /// </para>
    ///
    /// <para><b>One window, not one per step</b></para>
    /// <para>
    /// While a host is <see cref="Current"/>, the nested steps that used to open their own dialogs - the
    /// "Preparing Model" window of <c>Modify.RunPartOSimulation</c> and the "Tas Workflow" window of
    /// <c>Modify.RunWorkflow</c> - report into this one as <see cref="PartOProgressState.Detail"/> and take
    /// their cancellation from <see cref="Token"/>. Nothing they run changes.
    /// </para>
    ///
    /// <para><b>Cancellation is between stages</b></para>
    /// <para>
    /// Cancel latches <see cref="Token"/>. The work observes it where it already could - between workflow
    /// steps and between Iteration 3 stages. A TAS call in flight always finishes first, and the window says
    /// so. Where an operation also has stretches that never observe it, Cancel is offered only inside
    /// <see cref="AllowCancel"/> scopes, so it is never accepted and then ignored.
    /// </para>
    ///
    /// <para><b>Never fatal</b></para>
    /// <para>
    /// A window that cannot be shown - a test host, a session without a desktop - leaves a host that still
    /// holds its state and its token and simply shows nothing. Progress must never be able to stop the work.
    /// </para>
    /// </summary>
    public sealed class PartOProgressHost : IDisposable
    {
        [ThreadStatic]
        private static PartOProgressHost current;

        private readonly CancellationTokenSource cancellationTokenSource = new();

        private readonly PartOProgressHost previous;

        private readonly ManualResetEventSlim manualResetEventSlim = new(false);

        private Thread thread;

        private Dispatcher dispatcher;

        private PartOProgressWindow partOProgressWindow;

        private bool disposed;

        /// <param name="heading">What is running, in the engineer's terms.</param>
        /// <param name="subheading">One line of context - the reference case, the method. Optional.</param>
        /// <param name="stageNames">The stages, in order.</param>
        /// <param name="cancellable">Shows Cancel.</param>
        /// <param name="show">False builds the state and token only - for tests, and for callers with no desktop.</param>
        /// <param name="cancelOnlyWhileObserved">
        /// Offers Cancel only inside <see cref="AllowCancel"/> scopes - the stretches where the work is certain
        /// to observe the token. For an operation with long stretches that never look at it (the Iteration 2B
        /// assessments between rounds), where a click could otherwise be accepted and then never acted on.
        /// </param>
        public PartOProgressHost(string heading, string subheading, IEnumerable<string> stageNames, bool cancellable = true, bool show = true, bool cancelOnlyWhileObserved = false)
        {
            State = new PartOProgressState(stageNames)
            {
                CancelAvailable = !cancelOnlyWhileObserved,
            };

            Heading = heading;

            previous = current;
            current = this;

            if (show)
            {
                Open(heading, subheading, cancellable);
            }
        }

        /// <summary>
        /// The host of the operation running on this thread, or null. What the nested steps consult instead
        /// of opening their own dialogs.
        /// </summary>
        public static PartOProgressHost Current => current;

        public string Heading { get; }

        public PartOProgressState State { get; }

        /// <summary>Latched by Cancel.</summary>
        public CancellationToken Token => cancellationTokenSource.Token;

        public bool IsCancellationRequested => cancellationTokenSource.IsCancellationRequested;

        /// <summary>Whether a window actually came up.</summary>
        public bool IsShown => partOProgressWindow is not null;

        /// <summary>Starts a stage by index; see <see cref="PartOProgressState.Start"/>.</summary>
        public void Start(int index)
        {
            State.Start(index);
        }

        /// <summary>Names the step inside the running stage.</summary>
        public void Detail(string text)
        {
            State.Detail = text;
        }

        /// <summary>Requests cancellation as the button does. For callers and tests.</summary>
        public void Cancel()
        {
            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Offers Cancel for the life of the scope, then puts back what was offered before. Opened by the code
        /// that observes the token - the preparation steps of <c>RunPartOSimulation</c>, the workflow of
        /// <c>RunWorkflow</c> - around exactly the stretch it observes it in, and disposed BEFORE that code's
        /// final check: <see cref="SetCancelAvailable"/> waits for the window's thread, so a click either
        /// finished before the scope closed, and the final check sees it, or finds Cancel withdrawn. Where
        /// Cancel is always offered this changes nothing.
        /// </summary>
        public IDisposable AllowCancel()
        {
            bool previous_CancelAvailable = State.CancelAvailable;

            SetCancelAvailable(true);

            return new CancelScope(this, previous_CancelAvailable);
        }

        /// <summary>
        /// Offers or withdraws Cancel, and waits until the window shows it - so once this returns no click
        /// can still be in flight against the previous offer.
        /// </summary>
        public void SetCancelAvailable(bool cancelAvailable)
        {
            State.CancelAvailable = cancelAvailable;

            Post(window => window.Render());
        }

        /// <summary>
        /// Hides the window while a modal decision is on screen - a topmost window on another thread would
        /// otherwise sit over the dialog the engineer has to answer.
        /// </summary>
        public void Hide()
        {
            Post(window => window.Hide());
        }

        /// <summary>Shows the window again after <see cref="Hide"/>.</summary>
        public void Show()
        {
            Post(window => window.Show());
        }

        private void Post(Action<PartOProgressWindow> action)
        {
            Dispatcher dispatcher_Temp = dispatcher;
            PartOProgressWindow partOProgressWindow_Temp = partOProgressWindow;

            if (dispatcher_Temp is null || partOProgressWindow_Temp is null)
            {
                return;
            }

            try
            {
                //Synchronous, so a Hide has taken effect before the caller opens its dialog. Bounded, so a
                //wedged progress thread can never hold up the work.
                dispatcher_Temp.Invoke(() => action(partOProgressWindow_Temp), DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromSeconds(1));
            }
            catch (Exception)
            {
                //The window is closing or its thread is gone; progress is advisory.
            }
        }

        private void Open(string heading, string subheading, bool cancellable)
        {
            try
            {
                thread = new Thread(() =>
                {
                    try
                    {
                        dispatcher = Dispatcher.CurrentDispatcher;

                        PartOProgressWindow partOProgressWindow_Temp = new()
                        {
                            //Not owned: an owner must live on the same thread. Topmost keeps it in front of
                            //the frozen application instead.
                            Topmost = true,
                            Title = "Part O",
                            Heading = heading,
                            Subheading = subheading,
                            Cancellable = cancellable,
                            State = State,
                        };

                        partOProgressWindow_Temp.CancelRequested += (s, e) => Cancel();

                        //Closing the window is not a cancel - the same rule ProgressWindowHost follows: the X
                        //dismisses the window and the run carries on to finish normally.
                        partOProgressWindow_Temp.Closed += (s, e) =>
                        {
                            partOProgressWindow = null;
                            Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                        };

                        partOProgressWindow_Temp.Loaded += (s, e) => manualResetEventSlim.Set();

                        partOProgressWindow = partOProgressWindow_Temp;

                        if (!disposed)
                        {
                            partOProgressWindow_Temp.Show();
                            Dispatcher.Run();
                        }
                    }
                    catch (Exception)
                    {
                        //A progress window must never take the application down.
                        partOProgressWindow = null;
                    }
                    finally
                    {
                        try
                        {
                            manualResetEventSlim.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }
                })
                {
                    IsBackground = true,
                    Name = "sam-parto-progress",
                };

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                //Bounded: a window that will not come up must never hold up the job it reports on.
                manualResetEventSlim.Wait(5000);
            }
            catch (Exception)
            {
                partOProgressWindow = null;
            }
        }

        /// <summary>
        /// Closes the window and ends its thread - after draining input, so a Cancel click already made is
        /// never torn down unprocessed (the rule <c>ProgressWindowHost.Shutdown</c> documents).
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (ReferenceEquals(current, this))
            {
                current = previous;
            }

            Dispatcher dispatcher_Temp = dispatcher;

            if (dispatcher_Temp is not null)
            {
                try
                {
                    dispatcher_Temp.Invoke(() => { }, DispatcherPriority.Input, CancellationToken.None, TimeSpan.FromSeconds(1));
                }
                catch (Exception)
                {
                }

                try
                {
                    PartOProgressWindow partOProgressWindow_Temp = partOProgressWindow;

                    if (partOProgressWindow_Temp is not null)
                    {
                        dispatcher_Temp.BeginInvoke(DispatcherPriority.Background, new Action(partOProgressWindow_Temp.Close));
                    }

                    dispatcher_Temp.BeginInvokeShutdown(DispatcherPriority.Background);
                }
                catch (Exception)
                {
                }
            }

            try
            {
                thread?.Join(5000);
            }
            catch (Exception)
            {
            }

            manualResetEventSlim.Dispose();
        }

        private sealed class CancelScope : IDisposable
        {
            private readonly PartOProgressHost partOProgressHost;

            private readonly bool cancelAvailable;

            private bool disposed;

            internal CancelScope(PartOProgressHost partOProgressHost, bool cancelAvailable)
            {
                this.partOProgressHost = partOProgressHost;
                this.cancelAvailable = cancelAvailable;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;

                partOProgressHost.SetCancelAvailable(cancelAvailable);
            }
        }
    }
}
