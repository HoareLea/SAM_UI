// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Where one stage of a long Part O operation is.</summary>
    public enum PartOProgressStageStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped,
    }

    /// <summary>
    /// The stage list of one long Part O operation - what finished, what is running, and for how long.
    ///
    /// <para><b>Honest by construction</b></para>
    /// <para>
    /// There is no day counter here, because TAS reports none: its interop exposes no simulation events and
    /// nothing in SAM polls for a day. What is known is which stage has started, which have finished, how long
    /// each took and, where the workflow announces them, the name of the step inside a stage. That is what
    /// this holds.
    /// </para>
    /// <para>
    /// A percentage exists only where a caller reports a real count through <see cref="Report"/> - items
    /// actually done out of items actually there. It is never derived from time, from stage positions or
    /// from how many messages arrived, and it is cleared whenever a stage starts or ends. No Part O operation
    /// reports one today, so every Part O window is indeterminate and says why (<see cref="Note"/>).
    /// </para>
    /// <para>
    /// <b>Pure.</b> No window, no thread, and the clock is injected - so the transitions a person sees are
    /// testable without a desktop. <see cref="PartOProgressHost"/> renders it.
    /// </para>
    /// </summary>
    public class PartOProgressState
    {
        private readonly Func<DateTime> clock;

        private readonly object @lock = new();

        private readonly List<string> names = [];

        private readonly List<PartOProgressStageStatus> statuses = [];

        private readonly List<DateTime?> starts = [];

        private readonly List<TimeSpan?> durations = [];

        private readonly List<string> activities = [];

        private readonly DateTime start;

        private DateTime? end;

        private string detail;

        private double? fraction;

        private bool cancelAvailable = true;

        public PartOProgressState(IEnumerable<string> stageNames, Func<DateTime> clock = null)
        {
            this.clock = clock ?? (() => DateTime.UtcNow);

            foreach (string name in stageNames ?? [])
            {
                names.Add(name);
                statuses.Add(PartOProgressStageStatus.Pending);
                starts.Add(null);
                durations.Add(null);
                activities.Add(null);
            }

            start = this.clock();
        }

        public int Count => names.Count;

        /// <summary>
        /// Starts a stage. Every earlier stage still running is completed, and every earlier stage that never
        /// started is marked skipped - so the list never shows two stages running, or a later stage done while
        /// an earlier one still reads as pending.
        /// </summary>
        public void Start(int index)
        {
            lock (@lock)
            {
                if (index < 0 || index >= names.Count || statuses[index] == PartOProgressStageStatus.Running)
                {
                    return;
                }

                DateTime now = clock();

                for (int i = 0; i < names.Count; i++)
                {
                    if (i == index)
                    {
                        continue;
                    }

                    if (statuses[i] == PartOProgressStageStatus.Running)
                    {
                        Finish(i, PartOProgressStageStatus.Completed, now);
                    }
                    else if (i < index && statuses[i] == PartOProgressStageStatus.Pending)
                    {
                        statuses[i] = PartOProgressStageStatus.Skipped;
                    }
                }

                statuses[index] = PartOProgressStageStatus.Running;
                starts[index] = now;
                durations[index] = null;
                activities[index] = null;
                detail = null;
                fraction = null;
            }
        }

        /// <summary>
        /// Which pass of the running stage this is, where the stage repeats - "round 2" of an optimisation.
        /// Shown beside the stage's name, and kept on it once it ends, so the list says how far it got. A
        /// count only where the work itself counts: no total is implied.
        /// </summary>
        public void Activity(string text)
        {
            lock (@lock)
            {
                int index = statuses.IndexOf(PartOProgressStageStatus.Running);

                if (index >= 0)
                {
                    activities[index] = string.IsNullOrWhiteSpace(text) ? null : text;
                }
            }
        }

        /// <summary>The step inside the running stage, where the workflow names one. Null clears it.</summary>
        public string Detail
        {
            get
            {
                lock (@lock)
                {
                    return detail;
                }
            }

            set
            {
                lock (@lock)
                {
                    detail = value;
                }
            }
        }

        /// <summary>
        /// Real progress inside the running stage: <paramref name="completed"/> of <paramref name="total"/>
        /// items that the work itself counted. Ignored where there is no running stage or no total, so a
        /// caller cannot make a bar move by reporting nothing.
        /// </summary>
        public void Report(long completed, long total)
        {
            lock (@lock)
            {
                if (total <= 0 || completed < 0 || !statuses.Contains(PartOProgressStageStatus.Running))
                {
                    fraction = null;

                    return;
                }

                fraction = Math.Min(completed, total) / (double)total;
            }
        }

        /// <summary>
        /// Whether a Cancel pressed now is certain to be observed. True for the life of most operations; an
        /// operation with stretches that never look at its token turns it off outside the stretches that do
        /// (see <see cref="PartOProgressHost.AllowCancel"/>), so Cancel is never accepted and then ignored.
        /// </summary>
        public bool CancelAvailable
        {
            get
            {
                lock (@lock)
                {
                    return cancelAvailable;
                }
            }

            set
            {
                lock (@lock)
                {
                    cancelAvailable = value;
                }
            }
        }

        /// <summary>The reported fraction, 0 to 1, or null where nothing real is known - the usual case.</summary>
        public double? Fraction
        {
            get
            {
                lock (@lock)
                {
                    return fraction;
                }
            }
        }

        public bool IsDeterminate => Fraction.HasValue;

        /// <summary>"63%" - rounded down, so 100% is only ever all of it - or null where the bar is indeterminate.</summary>
        public string Percent
        {
            get
            {
                double? fraction_Temp = Fraction;

                return fraction_Temp.HasValue
                    ? string.Format(CultureInfo.InvariantCulture, "{0}%", (int)Math.Floor(fraction_Temp.Value * 100 + 1e-9))
                    : null;
            }
        }

        /// <summary>Completes the running stage, and ends the operation's clock where it is the last word.</summary>
        public void Complete(bool final = true)
        {
            lock (@lock)
            {
                DateTime now = clock();

                for (int i = 0; i < names.Count; i++)
                {
                    if (statuses[i] == PartOProgressStageStatus.Running)
                    {
                        Finish(i, PartOProgressStageStatus.Completed, now);
                    }
                }

                detail = null;
                fraction = null;

                if (final)
                {
                    end = now;
                }
            }
        }

        /// <summary>
        /// Marks every stage that never started as not needed. For an operation that ended at a condition of
        /// its own before reaching them - so the last render never lists as upcoming a stage that will not run.
        /// </summary>
        public void SkipUnstarted()
        {
            lock (@lock)
            {
                for (int i = 0; i < names.Count; i++)
                {
                    if (statuses[i] == PartOProgressStageStatus.Pending)
                    {
                        statuses[i] = PartOProgressStageStatus.Skipped;
                    }
                }
            }
        }

        /// <summary>Fails the running stage (or, where none is running, the first pending one) and ends the clock.</summary>
        public void Fail(string text = null)
        {
            lock (@lock)
            {
                DateTime now = clock();

                int index = statuses.IndexOf(PartOProgressStageStatus.Running);

                if (index < 0)
                {
                    index = statuses.IndexOf(PartOProgressStageStatus.Pending);
                }

                if (index >= 0)
                {
                    Finish(index, PartOProgressStageStatus.Failed, now);
                }

                detail = text;
                fraction = null;
                end = now;
            }
        }

        /// <summary>Whether the operation has ended - completed or failed. The clock has stopped.</summary>
        public bool IsFinished
        {
            get
            {
                lock (@lock)
                {
                    return end.HasValue;
                }
            }
        }

        /// <summary>How long the whole operation has run, or ran.</summary>
        public TimeSpan Elapsed
        {
            get
            {
                lock (@lock)
                {
                    return (end ?? clock()) - start;
                }
            }
        }

        /// <summary>"Elapsed 7m 48s" - how long it has run. Never a time remaining: nothing here knows one.</summary>
        public string ElapsedText => string.Format("Elapsed {0}", Format(Elapsed));

        public PartOProgressStageStatus Status(int index)
        {
            lock (@lock)
            {
                return index >= 0 && index < statuses.Count ? statuses[index] : PartOProgressStageStatus.Pending;
            }
        }

        public string Name(int index)
        {
            return index >= 0 && index < names.Count ? names[index] : null;
        }

        /// <summary>The index of the stage with this name, or -1 where this operation has no such stage.</summary>
        public int IndexOf(string name)
        {
            return names.IndexOf(name);
        }

        /// <summary>The stage's name with its activity, where it has one: "Optimisation rounds · round 2".</summary>
        public string Label(int index)
        {
            if (index < 0 || index >= names.Count)
            {
                return null;
            }

            string activity;

            lock (@lock)
            {
                activity = activities[index];
            }

            return activity is null ? names[index] : string.Format("{0} · {1}", names[index], activity);
        }

        /// <summary>How long a stage took, or has taken so far while it runs. Null where it never started.</summary>
        public TimeSpan? Duration(int index)
        {
            lock (@lock)
            {
                if (index < 0 || index >= names.Count)
                {
                    return null;
                }

                if (statuses[index] == PartOProgressStageStatus.Running && starts[index].HasValue)
                {
                    return clock() - starts[index].Value;
                }

                return durations[index];
            }
        }

        /// <summary>One line per stage, as it is shown: "✓ TAS simulation · 7m 48s".</summary>
        public List<string> Lines()
        {
            List<string> result = [];

            for (int i = 0; i < names.Count; i++)
            {
                TimeSpan? duration = Duration(i);

                result.Add(string.Format(
                    "{0} {1}{2}",
                    Glyph(Status(i)),
                    Label(i),
                    duration.HasValue && Status(i) != PartOProgressStageStatus.Pending ? " · " + Format(duration.Value) : string.Empty));
            }

            return result;
        }

        /// <summary>
        /// The row as a screen reader says it - the status in words, so nothing rests on the glyph or its
        /// colour: "Completed: TAS simulation (full year), 7m 48s". A window whose duration column is read on
        /// its own passes <paramref name="withDuration"/> false, so the time is not heard twice.
        /// </summary>
        public string AccessibleLine(int index, bool withDuration = true)
        {
            PartOProgressStageStatus partOProgressStageStatus = Status(index);
            TimeSpan? duration = Duration(index);

            return string.Format(
                "{0}: {1}{2}",
                StatusText(partOProgressStageStatus),
                Label(index),
                withDuration && duration.HasValue && (partOProgressStageStatus == PartOProgressStageStatus.Running || partOProgressStageStatus == PartOProgressStageStatus.Completed || partOProgressStageStatus == PartOProgressStageStatus.Failed) ? ", " + Format(duration.Value) : string.Empty);
        }

        public static string Glyph(PartOProgressStageStatus partOProgressStageStatus)
        {
            return partOProgressStageStatus switch
            {
                PartOProgressStageStatus.Running => "●",
                PartOProgressStageStatus.Completed => "✓",
                PartOProgressStageStatus.Failed => "✕",
                PartOProgressStageStatus.Skipped => "–",
                _ => "○",
            };
        }

        /// <summary>The stage status in words - what the glyph means.</summary>
        public static string StatusText(PartOProgressStageStatus partOProgressStageStatus)
        {
            return partOProgressStageStatus switch
            {
                PartOProgressStageStatus.Running => "Running now",
                PartOProgressStageStatus.Completed => "Completed",
                PartOProgressStageStatus.Failed => "Did not complete",
                PartOProgressStageStatus.Skipped => "Not needed",
                _ => "Upcoming",
            };
        }

        /// <summary>
        /// The line under a Part O progress window: whether the percentage is real, and what Cancel does. The
        /// same words on every Part O operation, so the answer to "can I stop this, and is that number real?"
        /// never depends on which window asked.
        /// </summary>
        /// <param name="determinate">A real count is being reported (<see cref="Report"/>).</param>
        /// <param name="cancellable">The operation observes Cancel - every Part O operation that runs TAS.</param>
        /// <param name="cancelRequested">Cancel has been pressed and the operation has not yet stopped.</param>
        public static string Note(bool determinate, bool cancellable, bool cancelRequested)
        {
            return Note(determinate, cancellable, cancelRequested, true);
        }

        /// <param name="cancelAvailable">
        /// Cancel is offered at this moment (<see cref="CancelAvailable"/>). Where it is not, the note says when
        /// it is, rather than implying a click would be acted on.
        /// </param>
        public static string Note(bool determinate, bool cancellable, bool cancelRequested, bool cancelAvailable)
        {
            if (cancelRequested)
            {
                return "Cancel requested. It stops at the next safe point between steps; a TAS step already running - a conversion, the shading or the simulation - finishes first, which can take minutes.";
            }

            string progress = determinate
                ? null
                : cancellable
                    ? "No percentage is shown: TAS does not report progress inside a simulation."
                    : "No percentage is shown: this step does not report one.";

            string cancel = !cancellable
                ? "It cannot be cancelled."
                : cancelAvailable
                    ? "Cancel takes effect at the next safe point between steps; a TAS step already running finishes first."
                    : "Cancel is offered only while a TAS simulation is being prepared or run; this step does not stop for it.";

            return progress is null ? cancel : string.Format("{0} {1}", progress, cancel);
        }

        /// <summary>"7m 48s", "42s", "1h 03m" - how a duration is written everywhere in the Part O workflow.</summary>
        public static string Format(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                timeSpan = TimeSpan.Zero;
            }

            if (timeSpan.TotalHours >= 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}h {1:00}m", (int)timeSpan.TotalHours, timeSpan.Minutes);
            }

            if (timeSpan.TotalMinutes >= 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}m {1:00}s", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}s", (int)Math.Round(timeSpan.TotalSeconds));
        }

        private void Finish(int index, PartOProgressStageStatus partOProgressStageStatus, DateTime now)
        {
            statuses[index] = partOProgressStageStatus;
            durations[index] = starts[index].HasValue ? now - starts[index].Value : null;
        }
    }
}
