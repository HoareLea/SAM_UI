// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Runs work items one at a time on a single dedicated STA thread (the Tas TCD COM calculation needs one),
    /// keeping at most ONE pending item: a newer submission supersedes the pending one, whose task is cancelled.
    /// <para>
    /// <b>Debounce.</b> A pending item starts only after <c>debounce</c> has passed without a newer submission,
    /// so typing "0.35" evaluates once, not three times.
    /// </para>
    /// <para>
    /// <b>Stale results.</b> A running item cannot be interrupted (the COM bisection has no cancellation), but
    /// cancelling its token completes its task as cancelled at once and its eventual result is dropped.
    /// </para>
    /// Measured on the real calculator (U-value PR2a spike): a reachable evaluation is about 0.25 s.
    /// </summary>
    public sealed class StaSingleFlightWorker<TRequest, TResult> : IDisposable
    {
        private sealed class Item
        {
            public Item(TRequest request, CancellationToken cancellationToken)
            {
                Request = request;
                CancellationToken = cancellationToken;
                Completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                Submitted = Stopwatch.StartNew();
            }

            public TRequest Request { get; }

            public CancellationToken CancellationToken { get; }

            public TaskCompletionSource<TResult> Completion { get; }

            public Stopwatch Submitted { get; }

            public CancellationTokenRegistration Registration { get; set; }
        }

        private readonly object gate = new object();
        private readonly AutoResetEvent signal = new AutoResetEvent(false);
        private readonly Func<TRequest, TResult> work;
        private readonly TimeSpan debounce;
        private readonly Thread thread;
        private Item pending;
        private bool disposed;
        private int started;

        public StaSingleFlightWorker(Func<TRequest, TResult> work, TimeSpan debounce, string name = null)
        {
            this.work = work ?? throw new ArgumentNullException(nameof(work));
            this.debounce = debounce < TimeSpan.Zero ? TimeSpan.Zero : debounce;

            thread = new Thread(Loop)
            {
                IsBackground = true,
                Name = string.IsNullOrWhiteSpace(name) ? "SAM STA single-flight worker" : name,
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>How many work items have started running (superseded and cancelled-while-pending ones never start).</summary>
        public int Started => Volatile.Read(ref started);

        /// <summary>
        /// Queues <paramref name="request"/>, superseding (cancelling) any item that has not started yet.
        /// The task completes with the result, faults with the work's exception, or is cancelled when superseded
        /// or when <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        public Task<TResult> Submit(TRequest request, CancellationToken cancellationToken = default)
        {
            Item item = new Item(request, cancellationToken);
            if (cancellationToken.CanBeCanceled)
            {
                // Registered before the item is published, so the worker never sees it without its registration.
                item.Registration = cancellationToken.Register(() => item.Completion.TrySetCanceled(cancellationToken));
            }

            Item superseded;
            lock (gate)
            {
                if (disposed)
                {
                    item.Registration.Dispose();
                    throw new ObjectDisposedException(GetType().Name);
                }

                superseded = pending;
                pending = item;
            }

            if (superseded != null)
            {
                superseded.Completion.TrySetCanceled();
                superseded.Registration.Dispose();
            }

            signal.Set();
            return item.Completion.Task;
        }

        private void Loop()
        {
            while (true)
            {
                Item item;
                lock (gate)
                {
                    if (disposed)
                    {
                        return;
                    }

                    item = pending;
                }

                if (item == null)
                {
                    signal.WaitOne();
                    continue;
                }

                // Debounce: wait until the newest item has been quiet for the debounce period. A newer
                // submission wakes us early and the loop re-reads which item is pending.
                TimeSpan remaining = debounce - item.Submitted.Elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    signal.WaitOne(remaining);
                    continue;
                }

                lock (gate)
                {
                    if (!ReferenceEquals(pending, item))
                    {
                        continue;
                    }

                    pending = null;
                }

                if (item.Completion.Task.IsCompleted)
                {
                    // Cancelled while pending.
                    item.Registration.Dispose();
                    continue;
                }

                Interlocked.Increment(ref started);
                try
                {
                    TResult result = work(item.Request);
                    item.Completion.TrySetResult(result);
                }
                catch (Exception exception)
                {
                    item.Completion.TrySetException(exception);
                }
                finally
                {
                    item.Registration.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Item item;
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                item = pending;
                pending = null;
            }

            item?.Completion.TrySetCanceled();
            signal.Set();
        }
    }
}
