// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Every wording of the Hub's outcome line, in one place.
    ///
    /// <para><b>Presentation only, from authorities that already exist</b></para>
    /// <list type="bullet">
    /// <item>TM59: the <see cref="PartOTM59ResultSummary"/> the result window was given - its verdict, glyph and
    /// counts. Nothing is read out of report text and no TM59 rule is restated.</item>
    /// <item>Iteration 2B: the run's own <see cref="PartOOptimisationStopReason"/>, and the last valid step's
    /// production <c>TM59ComplianceStatus</c>.</item>
    /// <item>The run: <see cref="PartORun.State"/>, <see cref="PartORun.IsRestored"/>,
    /// <see cref="PartORun.InvalidationReason"/> and the capabilities' <c>IsAssessable</c> answer.</item>
    /// </list>
    ///
    /// <para><b>Session-only, deliberately.</b> A cancelled review or a cancelled TAS run is not something the
    /// model records, so it is said only by the line the Hub carries to its next showing. Nothing here persists,
    /// and nothing here can manufacture such an event from the run: <see cref="StandingOutcome"/> only ever says
    /// what the run is now.</para>
    /// </summary>
    public static partial class Modify
    {
        /// <summary>
        /// The line the Hub shows: the session record of the last action while the run still holds what it
        /// claims, otherwise what the run itself says now. Null where there is nothing to say - a fresh Hub over
        /// a run that has never been prepared.
        /// </summary>
        internal static PartOWorkflowOutcome? HubOutcome(PartOWorkflowOutcome? partOWorkflowOutcome_Last, PartORun? partORun, PartOWorkflowCapabilities? partOWorkflowCapabilities)
        {
            if (partOWorkflowOutcome_Last is not null && (partORun is null || Holds(partOWorkflowOutcome_Last, partORun, partOWorkflowCapabilities)))
            {
                return partOWorkflowOutcome_Last;
            }

            PartOWorkflowOutcome? result = StandingOutcome(partORun, partOWorkflowCapabilities);

            //Superseded, not hidden: what happened earlier is still one hover away.
            if (partOWorkflowOutcome_Last is not null && result is not null)
            {
                result = new PartOWorkflowOutcome(
                    result.Kind,
                    result.Glyph,
                    result.Headline,
                    result.Detail,
                    string.Join("\n\n", new[] { result.ToolTip, "Earlier in this session: " + partOWorkflowOutcome_Last.Text }.Where(x => !string.IsNullOrWhiteSpace(x))));
            }

            return result;
        }

        /// <summary>Whether the run still says what the line claims about it.</summary>
        private static bool Holds(PartOWorkflowOutcome partOWorkflowOutcome, PartORun partORun, PartOWorkflowCapabilities? partOWorkflowCapabilities)
        {
            if (partOWorkflowOutcome.RunState is not PartORunState partORunState)
            {
                return true;
            }

            if (partORun.State != partORunState)
            {
                return false;
            }

            return partORunState != PartORunState.WorkflowCompleted || (partOWorkflowCapabilities?.ResultsAvailable ?? false);
        }

        /// <summary>
        /// What the run says now, with no session record: prepared and waiting for TAS, results reopened or
        /// completed and ready to review, or a previous run no longer valid. Never a verdict - a TM59 verdict
        /// exists only once the assessment has run, and this does not run it.
        /// </summary>
        internal static PartOWorkflowOutcome? StandingOutcome(PartORun? partORun, PartOWorkflowCapabilities? partOWorkflowCapabilities)
        {
            if (partORun is null)
            {
                return null;
            }

            if (partORun.State == PartORunState.WorkflowCompleted && (partOWorkflowCapabilities?.ResultsAvailable ?? false))
            {
                //A reopened run is named only where its saved record says which run it was (a v2 resume sidecar
                //records whether a catalogue was offered); an older record, or none, leaves it unnamed.
                string name = partORun.IsRestored ? string.Format("{0} reopened", SavedResults(partORun)) : string.Format("{0} completed", ScenarioName(partORun));

                return new PartOWorkflowOutcome(
                    PartOWorkflowOutcomeKind.Information,
                    "○",
                    string.Format("{0} — ready to review", name),
                    "Review Results shows the TM59 verdict · no new simulation is needed",
                    string.IsNullOrWhiteSpace(partORun.Path_TSD) ? null : string.Format("Results: {0}", partORun.Path_TSD))
                {
                    RunState = PartORunState.WorkflowCompleted,
                };
            }

            if (partORun.State == PartORunState.Prepared)
            {
                return new PartOWorkflowOutcome(
                    PartOWorkflowOutcomeKind.Information,
                    "○",
                    string.Format("{0} prepared — waiting for the full-year TAS run", ScenarioName(partORun)),
                    "No simulation has been run for it yet")
                {
                    RunState = PartORunState.Prepared,
                };
            }

            string? reason = partORun.InvalidationReason ?? (partORun.State == PartORunState.WorkflowCompleted ? partOWorkflowCapabilities?.ResultsRefusal : null);
            if (string.IsNullOrWhiteSpace(reason))
            {
                return null;
            }

            return new PartOWorkflowOutcome(
                PartOWorkflowOutcomeKind.Warning,
                "!",
                "Previous Part O run is no longer valid — no results to review",
                FirstSentence(reason!),
                reason);
        }

        /// <summary>
        /// After Prepare &amp; Run assessed its results: "✕ Iteration 1a completed — TM59 FAIL", worded from the
        /// summary the result window was given.
        /// </summary>
        internal static PartOWorkflowOutcome CompletedOutcome(string? name, TimeSpan elapsed_Simulation, PartOTM59ResultSummary? partOTM59ResultSummary, int count_Notes)
        {
            name = Name(name);

            string notes = count_Notes != 0 ? string.Format(" · {0} shown", UI.Query.PartOCount(count_Notes, "note was", "notes were")) : string.Empty;

            if (partOTM59ResultSummary is null)
            {
                return new PartOWorkflowOutcome(
                    PartOWorkflowOutcomeKind.Information,
                    "○",
                    string.Format("{0} completed — results ready to review", name),
                    string.Format("TAS simulation {0}{1}", PartOProgressState.Format(elapsed_Simulation), notes))
                {
                    RunState = PartORunState.WorkflowCompleted,
                };
            }

            return new PartOWorkflowOutcome(
                Kind(partOTM59ResultSummary),
                partOTM59ResultSummary.Glyph,
                string.Format("{0} completed — TM59 {1}", name, partOTM59ResultSummary.VerdictText),
                string.Format("TAS simulation {0}{1}{2}", PartOProgressState.Format(elapsed_Simulation), Counts(partOTM59ResultSummary), notes),
                partOTM59ResultSummary.Reason)
            {
                RunState = PartORunState.WorkflowCompleted,
            };
        }

        /// <summary>
        /// The Hub's line after Review Results, worded from the <see cref="PartOTM59ResultSummary"/> that
        /// <see cref="ReviewPartOTM59"/> built from the assessment and handed to the result window. The Hub never
        /// reads the window: both are readers of that one object, so they cannot disagree about the verdict.
        /// Null where there was no run to review.
        /// </summary>
        internal static PartOWorkflowOutcome? ReviewOutcome(PartOTM59ResultSummary? partOTM59ResultSummary, PartORun? partORun = null)
        {
            if (partOTM59ResultSummary is null)
            {
                return null;
            }

            //A reopened run is named only from its saved record, as StandingOutcome names it.
            string subject = partORun is null
                ? "Results reviewed"
                : partORun.IsRestored
                    ? string.Format("{0} reviewed", SavedResults(partORun))
                    : PartOWorkflowScenario.Find(partORun.PreparationContext) is null ? "Results reviewed" : string.Format("{0} results reviewed", ScenarioName(partORun));

            return new PartOWorkflowOutcome(
                Kind(partOTM59ResultSummary),
                partOTM59ResultSummary.Glyph,
                string.Format("{0} — TM59 {1}", subject, partOTM59ResultSummary.VerdictText),
                string.IsNullOrWhiteSpace(partOTM59ResultSummary.Counts) ? "no simulation was run" : partOTM59ResultSummary.Counts + " · no simulation was run",
                partOTM59ResultSummary.Reason)
            {
                RunState = PartORunState.WorkflowCompleted,
            };
        }

        /// <summary>
        /// The Hub's line after the engineer cancelled the Review iteration window: a deliberate decline before
        /// any TAS work, which changed nothing. Not persisted - it is the Hub's last-outcome line, carried only
        /// to the next showing of the Hub like every other outcome. It claims nothing about the run, so it
        /// holds whatever state the run was already in.
        /// </summary>
        internal static PartOWorkflowOutcome DeclinedOutcome(string? iteration)
        {
            return new PartOWorkflowOutcome(
                PartOWorkflowOutcomeKind.Information,
                "○",
                string.Format("{0} review cancelled — no simulation was run", Name(iteration)),
                "Cancelled before TAS · the model is unchanged");
        }

        /// <summary>
        /// The TAS run was cancelled. The simulation adopts nothing on cancel, so the preparation - adopted at
        /// the review, or reused - is still the run's, and the line holds only while it is.
        /// </summary>
        internal static PartOWorkflowOutcome SimulationCancelledOutcome(string? name)
        {
            return new PartOWorkflowOutcome(
                PartOWorkflowOutcomeKind.Information,
                "○",
                string.Format("{0} TAS run cancelled — no results were produced", Name(name)),
                "The prepared iteration is kept, so it can be run again")
            {
                RunState = PartORunState.Prepared,
            };
        }

        /// <summary>A simulation that ran but did not complete the run - refused by the check, not a full year, not adopted.</summary>
        internal static PartOWorkflowOutcome NotCompletedOutcome(string? name, string? reason)
        {
            return new PartOWorkflowOutcome(
                PartOWorkflowOutcomeKind.Warning,
                "!",
                string.Format("{0} not completed — no TM59 results", Name(name)),
                string.IsNullOrWhiteSpace(reason) ? null : FirstSentence(reason!),
                reason);
        }

        /// <summary>
        /// After Iteration 2B, from the run's own stop reason - never from its prose. The verdict is said only
        /// where it is the run's: a Passed stop, or a stop whose last valid design the production assessment
        /// failed. The full stop description is the tooltip. Null where the optimisation did not start; the
        /// refusal was already shown.
        /// </summary>
        internal static PartOWorkflowOutcome? OptimisationOutcome(PartOOptimisationRun? partOOptimisationRun)
        {
            if (partOOptimisationRun is null)
            {
                return null;
            }

            string name = PartOWorkflowScenario.ShortName(PartOWorkflowScenario.Text_Iteration2B);

            PartOOptimisationStep? partOOptimisationStep_LastValid = partOOptimisationRun.Step_LastValid;

            string facts = string.Format(
                "{0} · {1}",
                UI.Query.PartOCount(partOOptimisationRun.Rounds, "round", "rounds"),
                partOOptimisationStep_LastValid is null ? "no valid design was produced" : string.Format("last valid design: run {0}", partOOptimisationStep_LastValid.Iteration));

            //"round limit", the word the progress window, the Start window and the result window use for the
            //same setting (live acceptance, 26 Sep: the Hub said "iteration limit" beside a "round limit" result).
            string stop = partOOptimisationRun.StopReason == PartOOptimisationStopReason.IterationLimitReached
                ? "round limit reached"
                : Core.Query.Description(partOOptimisationRun.StopReason).ToLowerInvariant();

            string description = partOOptimisationRun.Description;

            switch (partOOptimisationRun.StopReason)
            {
                case PartOOptimisationStopReason.Passed:
                    return new PartOWorkflowOutcome(PartOWorkflowOutcomeKind.Success, "✓", string.Format("{0} optimisation completed — TM59 PASS", name), facts, description)
                    {
                        RunState = PartORunState.WorkflowCompleted,
                    };

                case PartOOptimisationStopReason.Cancelled:
                    return new PartOWorkflowOutcome(PartOWorkflowOutcomeKind.Information, "○", string.Format("{0} optimisation cancelled", name), facts, description);

                case PartOOptimisationStopReason.CapacityReached:
                case PartOOptimisationStopReason.IterationLimitReached:
                case PartOOptimisationStopReason.NoEligibleTargets:
                    if (partOOptimisationStep_LastValid?.OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Fail)
                    {
                        return new PartOWorkflowOutcome(PartOWorkflowOutcomeKind.Fail, "✕", string.Format("{0} optimisation completed — TM59 FAIL", name), string.Format("Stopped: {0} · {1}", stop, facts), description)
                        {
                            RunState = PartORunState.WorkflowCompleted,
                        };
                    }

                    break;
            }

            return new PartOWorkflowOutcome(PartOWorkflowOutcomeKind.Warning, "!", string.Format("{0} optimisation stopped — {1}", name, stop), facts, description);
        }

        private static PartOWorkflowOutcomeKind Kind(PartOTM59ResultSummary partOTM59ResultSummary)
        {
            return partOTM59ResultSummary.Verdict switch
            {
                PartOTM59Verdict.Pass => PartOWorkflowOutcomeKind.Success,
                PartOTM59Verdict.Fail => PartOWorkflowOutcomeKind.Fail,
                _ => PartOWorkflowOutcomeKind.Warning,
            };
        }

        /// <summary>" · 8 spaces assessed · 2 pass · 6 fail · 1 not assessed" - the window's own counts line - or empty.</summary>
        private static string Counts(PartOTM59ResultSummary partOTM59ResultSummary)
        {
            return string.IsNullOrWhiteSpace(partOTM59ResultSummary.Counts) ? string.Empty : " · " + partOTM59ResultSummary.Counts;
        }

        /// <summary>The prepared run's scenario name - "Iteration 2" - from its own record, the way the Hub names it.</summary>
        private static string ScenarioName(PartORun partORun)
        {
            return PartOWorkflowScenario.Find(partORun.PreparationContext)?.Name ?? "Part O iteration";
        }

        /// <summary>
        /// "Saved Iteration 2 results" where the reopened run's saved record names it, otherwise "Saved results".
        /// Never inferred: a v1 sidecar, or a run with no sidecar, has no record to name it from.
        /// </summary>
        private static string SavedResults(PartORun partORun)
        {
            string? name = PartOWorkflowScenario.Find(partORun.PreparationContext)?.Name;

            return string.IsNullOrWhiteSpace(name) ? "Saved results" : string.Format("Saved {0} results", name);
        }

        private static string Name(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? "Part O iteration" : PartOWorkflowScenario.ShortName(text!);
        }

        private static string FirstSentence(string text)
        {
            int index = text.IndexOf(". ", StringComparison.Ordinal);

            return index < 0 ? text.Trim() : text.Substring(0, index + 1);
        }
    }
}
