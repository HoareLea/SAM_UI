// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the Iteration 2B result window says first: the TM59 outcome of the design the optimisation kept,
    /// why it stopped, what changed, and what to do next. The engineering history sits below it.
    ///
    /// <para><b>Presentation only, and read off the run's own record</b></para>
    /// <para>
    /// The stop is <see cref="PartOOptimisationRun.StopReason"/>, never the run's prose. The verdict is the one
    /// the run itself reached: <b>PASS only on a <see cref="PartOOptimisationStopReason.Passed"/> stop</b>, the
    /// optimiser's own authority, which already refused a pass over part of the dwelling scope
    /// (<c>Modify.PartialAssessment</c>). A kept design whose production assessment failed is FAIL. Anything
    /// else is NOT ASSESSED, and a run with no kept design at all is UNAVAILABLE. This is the same rule the
    /// Hub's outcome line follows (<c>Modify.OptimisationOutcome</c>), so the two cannot disagree about a pass.
    /// </para>
    /// <para>
    /// <b>The counts only count.</b> "Spaces with a failing TM59 check" counts the distinct design spaces among
    /// a step's <see cref="PartOOptimisationStep.TM59Results"/> rows whose status the production assessment set
    /// to Fail. No criterion is applied and no space verdict is restated, which is why the wording names the
    /// checks rather than claiming each space's overall result. The airflow counts count the recorded
    /// adjustments of the rounds that completed; no airflow is recalculated.
    /// </para>
    /// <para>
    /// <b>Session only.</b> A <see cref="PartOOptimisationRun"/> is not persisted, so this summary exists only
    /// in the session that ran the optimisation. Nothing here is reconstructed from files.
    /// </para>
    /// </summary>
    public class PartOOptimisationSummary
    {
        /// <summary>One labelled line of the summary; <see cref="Detail"/> is its tooltip.</summary>
        public class Fact
        {
            public Fact(string label, string value, string? detail = null)
            {
                Label = label;
                Value = value;
                Detail = string.IsNullOrWhiteSpace(detail) ? null : detail;
            }

            public string Label { get; }

            public string Value { get; }

            public string? Detail { get; }

            public override string ToString()
            {
                return string.Format("{0}: {1}", Label, Value);
            }
        }

        private PartOOptimisationSummary(PartOTM59Verdict verdict, string stopHeadline, string stopMeaning, string nextStep, List<Fact> facts, string? cancelNote)
        {
            Verdict = verdict;
            StopHeadline = stopHeadline;
            StopMeaning = stopMeaning;
            NextStep = nextStep;
            Facts = facts;
            CancelNote = cancelNote;
        }

        /// <summary>The TM59 outcome of the design the optimisation kept. See the class for the rule.</summary>
        public PartOTM59Verdict Verdict { get; }

        /// <summary>Why the optimisation stopped, in one line - one wording per <see cref="PartOOptimisationStopReason"/>.</summary>
        public string StopHeadline { get; }

        /// <summary>What that stop means for the design, in a sentence.</summary>
        public string StopMeaning { get; }

        /// <summary>What an engineer does next after this stop.</summary>
        public string NextStep { get; }

        /// <summary>Rounds, starting and kept design, what changed, the envelope, the kept results file.</summary>
        public List<Fact> Facts { get; }

        /// <summary>
        /// Said only where Cancel was requested and the run nevertheless stopped for another reason - the round
        /// reached a refusal or failure before a point where it could stop. Null otherwise.
        /// </summary>
        public string? CancelNote { get; }

        /// <summary>"TM59 PASS" / "TM59 FAIL" / "TM59 NOT ASSESSED" / "TM59 UNAVAILABLE" - the word, never colour alone.</summary>
        public string VerdictText => Verdict switch
        {
            PartOTM59Verdict.Pass => "TM59 PASS",
            PartOTM59Verdict.Fail => "TM59 FAIL",
            PartOTM59Verdict.NotAssessed => "TM59 NOT ASSESSED",
            _ => "TM59 UNAVAILABLE",
        };

        public string Glyph => Verdict switch
        {
            PartOTM59Verdict.Pass => "✓",
            PartOTM59Verdict.Fail => "✕",
            _ => "!",
        };

        /// <summary>Everything above the history, as plain text - what Copy All puts first.</summary>
        public string Text
        {
            get
            {
                List<string> lines = [VerdictText, StopHeadline, StopMeaning];

                if (!string.IsNullOrWhiteSpace(CancelNote))
                {
                    lines.Add(CancelNote!);
                }

                lines.AddRange(Facts.Select(x => x.ToString()));
                lines.Add(string.Format("Next: {0}", NextStep));

                return string.Join(Environment.NewLine, lines);
            }
        }

        public override string ToString()
        {
            return Text;
        }

        /// <param name="partOOptimisationRun">The run the optimiser returned.</param>
        /// <param name="cancelRequested">
        /// Whether Cancel was requested in the progress window - a session fact the window owns, used only to
        /// explain a stop that is not <see cref="PartOOptimisationStopReason.Cancelled"/>.
        /// </param>
        /// <param name="canContinue">
        /// Whether the session's Part O run still holds the kept design as a completed run after the stop - so
        /// Iteration 2B can be started again from it. The optimiser drops the run on a cancelled or failed
        /// round, and then it cannot; the next step must not direct the engineer to an action that is not
        /// available. Read off the run by the caller, never inferred from the stop reason. False by default:
        /// nothing is promised that was not checked.
        /// </param>
        public static PartOOptimisationSummary Create(PartOOptimisationRun partOOptimisationRun, bool cancelRequested = false, bool canContinue = false)
        {
            if (partOOptimisationRun is null)
            {
                return new PartOOptimisationSummary(PartOTM59Verdict.Unavailable, "No optimisation was run.", string.Empty, "Return to Part O — Prepare & Run.", [], null);
            }

            PartOOptimisationStep? step_Baseline = partOOptimisationRun.Step_Baseline;
            PartOOptimisationStep? step_LastValid = partOOptimisationRun.Step_LastValid;

            PartOTM59Verdict verdict = VerdictOf(partOOptimisationRun);

            (string headline, string meaning, string next) = StopText(partOOptimisationRun, canContinue);

            List<Fact> facts = [];

            facts.Add(new Fact("Rounds", Rounds(partOOptimisationRun)));

            if (step_Baseline is not null)
            {
                facts.Add(new Fact("Starting design", Design(step_Baseline, step_Baseline.IsCompleted), string.Format("Run 0 is the Iteration 2B starting point: the design as it was, with the results it already had. It is not simulated again. Results: {0}", step_Baseline.Path_TSD ?? "-")));
            }

            if (step_LastValid is not null && !ReferenceEquals(step_LastValid, step_Baseline))
            {
                facts.Add(new Fact("Kept design", Design(step_LastValid, true), string.Format("The last round that was prepared, simulated over the full year and assessed. It is the Iteration 2B result, kept under PartO/Iteration2B; your design model is not changed. Results: {0}", partOOptimisationRun.Path_TSD_LastValid ?? "-")));
            }
            else if (step_LastValid is not null)
            {
                facts.Add(new Fact("Kept design", "The starting design (run 0) — no round changed it", "No optimisation round completed, so the kept design is the one the optimisation started from. Your design model is not changed."));
            }
            else
            {
                facts.Add(new Fact("Kept design", "None — the starting design could not be assessed"));
            }

            facts.Add(new Fact("Design airflow changed", AirFlowChange(partOOptimisationRun), "Counted from the recorded adjustments of the rounds that completed: TARGETED spaces failed TM59 and were raised by the step; BALANCING spaces moved only to keep their dwelling's supply and extract in balance. The Approved Document F requirement is never lowered and the selected ventilation unit is never changed. Every figure is under Design airflow changes."));

            Fact? fact_Envelope = Envelope(partOOptimisationRun);
            if (fact_Envelope is not null)
            {
                facts.Add(fact_Envelope);
            }

            string? cancelNote = cancelRequested && partOOptimisationRun.StopReason != PartOOptimisationStopReason.Cancelled && partOOptimisationRun.StopReason != PartOOptimisationStopReason.Running
                ? "Cancel was requested, but the run stopped for the reason above before it reached a point where it could stop for Cancel."
                : null;

            return new PartOOptimisationSummary(verdict, headline, meaning, next, facts, cancelNote);
        }

        /// <summary>See the class: PASS only on a Passed stop; FAIL where the kept design failed; otherwise no verdict.</summary>
        internal static PartOTM59Verdict VerdictOf(PartOOptimisationRun partOOptimisationRun)
        {
            if (partOOptimisationRun.StopReason == PartOOptimisationStopReason.Passed)
            {
                return PartOTM59Verdict.Pass;
            }

            PartOOptimisationStep? step_LastValid = partOOptimisationRun.Step_LastValid;
            if (step_LastValid is null)
            {
                return PartOTM59Verdict.Unavailable;
            }

            return step_LastValid.OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Fail ? PartOTM59Verdict.Fail : PartOTM59Verdict.NotAssessed;
        }

        /// <summary>
        /// One wording per stop reason: the headline, what it means, and what to do next. Materially different
        /// outcomes are never folded into one "completed".
        /// </summary>
        internal static (string Headline, string Meaning, string Next) StopText(PartOOptimisationRun partOOptimisationRun, bool canContinue = false)
        {
            PartOOptimisationSettings partOOptimisationSettings = partOOptimisationRun.Settings;

            string kept = partOOptimisationRun.Step_LastValid is null
                ? "No design was kept."
                : partOOptimisationRun.Step_LastValid.IsBaseline
                    ? "The starting design is kept."
                    : string.Format("The last complete design (run {0}) is kept.", partOOptimisationRun.Step_LastValid.Iteration);

            //Whether 2B can be started again from the kept design, as the caller read it off the run.
            string again = canContinue
                ? "Optimise (2B) again continues from the kept design."
                : "Iteration 2B cannot continue from this stop, because the stop closed the session run: to optimise further, first produce a new completed Iteration 2 run with Prepare & Run (a full-year simulation), then start Iteration 2B.";

            string fix = string.Format("Open Engineering detail › Notes, warnings and refusals for the cause and resolve it. {0}", again);

            switch (partOOptimisationRun.StopReason)
            {
                case PartOOptimisationStopReason.Passed:
                    return partOOptimisationRun.Rounds == 0
                        ? ("No optimisation needed — the starting design already passes TM59",
                           "Every eligible occupied space passes its production TM59 criteria, so no round was run and nothing was changed.",
                           "Review the TM59 results from Part O — Prepare & Run.")
                        : ("Stopped: TM59 target reached",
                           string.Format("Every eligible occupied space passes its production TM59 criteria. This is the first passing design found at the {0:0.###} l/s step, not a minimum required airflow.", partOOptimisationSettings.AirFlowStep_Lps),
                           "Review the TM59 results of the kept design from Part O — Prepare & Run, and save the model to keep it.");

                case PartOOptimisationStopReason.CapacityReached:
                    return ("Stopped: selected ventilation unit capacity reached",
                            string.Format("No dwelling with a failing space can take another full {0:0.###} l/s step within its selected unit. {1} Spaces that still fail are reported as they stand — a real limit of the selected equipment, not a failure of the process.", partOOptimisationSettings.AirFlowStep_Lps, kept),
                            "Decide how to address the spaces that still fail: a larger ventilation unit is a deliberate engineering choice and is never made automatically; other design changes may also apply. Review the TM59 results of the kept design first.");

                case PartOOptimisationStopReason.IterationLimitReached:
                    return (string.Format("Stopped: round limit reached ({0})", UI.Query.PartOCount(partOOptimisationSettings.MaximumIterations, "round", "rounds")),
                            string.Format("Eligible spaces were still failing after the last allowed round. {0}", kept),
                            canContinue
                                ? "Optimise (2B) again continues from the kept design, and you can allow more rounds. Or reconsider the design or the selected unit."
                                : string.Format("{0} Or reconsider the design or the selected unit.", again));

                case PartOOptimisationStopReason.NoEligibleTargets:
                    return ("Stopped: no failing space that Iteration 2B can change",
                            string.Format("Every space that still fails is outside the Part O dwelling scope or has no Approved Document O design terminal to raise. {0}", kept),
                            "The remaining failures need a design change outside Iteration 2B. The reason for each space is under Engineering detail › Notes, warnings and refusals.");

                case PartOOptimisationStopReason.RebalanceRefused:
                    return ("Stopped: a design airflow round was refused",
                            string.Format("The round could not be adopted as asked — for example an Approved Document F requirement, a dwelling that could not be balanced, or terminals that could not be attributed. {0}", kept),
                            fix);

                case PartOOptimisationStopReason.PreparationFailed:
                    return ("Stopped: the optimised design could not be prepared for simulation",
                            string.Format("The Part O iteration could not be re-prepared over the round's design, so it was never simulated. {0}", kept),
                            fix);

                case PartOOptimisationStopReason.SimulationFailed:
                    return ("Stopped: a round's TAS simulation did not complete",
                            string.Format("The TAS workflow did not produce the full-year results a TM59 assessment reads. {0}", kept),
                            fix);

                case PartOOptimisationStopReason.AssessmentFailed:
                    return ("Stopped: TM59 could not be assessed",
                            string.Format("The production TM59 assessment produced no pass or fail that may be reported for the design. {0}", kept),
                            fix);

                case PartOOptimisationStopReason.Cancelled:
                    return ("Cancelled",
                            string.Format("The round in progress was stopped and is not a result. {0}", kept),
                            again);

                default:
                    return ("The optimisation has not finished", string.Empty, "Return to Part O — Prepare & Run.");
            }
        }

        /// <summary>"3 completed · limit 10" - or "3 run, 2 completed in full" where a round did not complete.</summary>
        private static string Rounds(PartOOptimisationRun partOOptimisationRun)
        {
            int run = partOOptimisationRun.Rounds;
            int completed = partOOptimisationRun.Steps.Count(x => x.IsOptimisationRound && x.IsCompleted);

            string rounds = run == completed
                ? string.Format("{0} completed", run)
                : string.Format("{0} run, {1} completed in full", run, completed);

            return string.Format("{0} · limit {1} · {2:0.###} l/s step", rounds, partOOptimisationRun.Settings.MaximumIterations, partOOptimisationRun.Settings.AirFlowStep_Lps);
        }

        /// <summary>"Run 2 · TM59 FAIL · 3 spaces with a failing TM59 check" - from the step's own record.</summary>
        private static string Design(PartOOptimisationStep partOOptimisationStep, bool assessed)
        {
            string run = string.Format("Run {0}", partOOptimisationStep.Iteration);

            if (!assessed || partOOptimisationStep.TM59Results.Count == 0 && partOOptimisationStep.OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Undefined)
            {
                return string.Format("{0} · not assessed", run);
            }

            int failing = partOOptimisationStep.TM59Results.Where(x => x.IsFail).Select(x => x.SpaceGuid_Design).Distinct().Count();

            return string.Format(
                "{0} · production TM59 status {1} · {2} with a failing TM59 check",
                run,
                Core.Query.Description(partOOptimisationStep.OccupiedSpaceComplianceStatus),
                UI.Query.PartOCount(failing, "space", "spaces"));
        }

        /// <summary>"5 spaces · 3 targeted, 2 balancing" over the completed rounds, or "None".</summary>
        private static string AirFlowChange(PartOOptimisationRun partOOptimisationRun)
        {
            HashSet<Guid> targeted = [];
            HashSet<Guid> derived = [];

            foreach (PartOOptimisationStep partOOptimisationStep in partOOptimisationRun.Steps)
            {
                if (!partOOptimisationStep.IsOptimisationRound || !partOOptimisationStep.IsCompleted)
                {
                    continue;
                }

                partOOptimisationStep.TargetedAdjustments.ForEach(x => targeted.Add(x.SpaceGuid));
                partOOptimisationStep.DerivedAdjustments.ForEach(x => derived.Add(x.SpaceGuid));
            }

            derived.ExceptWith(targeted);

            if (targeted.Count == 0 && derived.Count == 0)
            {
                return "None";
            }

            return string.Format(
                "{0} · {1} targeted, {2} balancing",
                UI.Query.PartOCount(targeted.Count + derived.Count, "space", "spaces"),
                targeted.Count,
                derived.Count);
        }

        /// <summary>
        /// The capacity envelope's line: whether it ran, and that it is a diagnostic that is never adopted. Null
        /// where it was not asked for - there is then nothing to say.
        /// </summary>
        private static Fact? Envelope(PartOOptimisationRun partOOptimisationRun)
        {
            if (!partOOptimisationRun.Settings.CapacityEnvelope)
            {
                return null;
            }

            string? description = partOOptimisationRun.CapacityEnvelopeDescription;

            if (partOOptimisationRun.HasCapacityEnvelope)
            {
                PartOOptimisationStep step = partOOptimisationRun.Step_CapacityEnvelope;

                return new Fact(
                    "Capacity envelope",
                    string.Format("Calculated (diagnostic, not adopted) · production TM59 status {0}", Core.Query.Description(step.OccupiedSpaceComplianceStatus)),
                    string.Format("What the already-selected units could support if the kept design were grown towards their ceilings. It is not the run's answer and is not kept. {0}", description));
            }

            return partOOptimisationRun.Step_CapacityEnvelope is null
                ? new Fact("Capacity envelope", "Not calculated", description)
                : new Fact("Capacity envelope", "Attempted, did not complete", description);
        }
    }
}
