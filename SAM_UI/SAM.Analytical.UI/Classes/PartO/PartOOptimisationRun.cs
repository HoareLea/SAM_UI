// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One automatic Approved Document O Iteration 2B optimisation, start to finish: the baseline it began
    /// from, every round it ran, why it stopped, and the last design that was actually valid.
    ///
    /// <para><b>Why the history is the deliverable, not the model</b></para>
    /// <para>
    /// An optimisation that handed back only its final model would answer "here is a design" and nothing
    /// else. The questions an engineer has afterwards are all about the path: which rooms were chosen and
    /// which only moved to keep a dwelling balanced, what each round asked for and got, what the units were
    /// carrying at each stage, which results file each iteration produced, and - above all - whether the
    /// process ran out of equipment or ran out of failures. None of that is recoverable from the final
    /// model, so it is recorded as it happens.
    /// </para>
    ///
    /// <para><b>What a pass at a fixed step is, and is not</b></para>
    /// <para>
    /// The optimisation raises the same step every round. A design that passes is therefore <b>the first
    /// tested passing design at the configured step</b>. It is <i>not</i> a minimum required airflow: no
    /// search was run between the last failing design and this one, so nothing here establishes that a
    /// smaller airflow would not also have passed. <see cref="Description"/> says so in those words, every
    /// time.
    /// </para>
    ///
    /// <para><b>The diagnostic capacity envelope is kept apart from all of it</b></para>
    /// <para>
    /// Where the ordinary optimisation stops with rooms still failing, an optional final
    /// <see cref="PartOOptimisationStepKind.CapacityEnvelope"/> step answers a different question - what
    /// design the <i>already-selected</i> unit could support if the last valid design were grown
    /// proportionally to its ceiling. It runs the same full year and the same production TM59, so nothing
    /// about its lifecycle distinguishes it from a round; what distinguishes it is that it is a design the
    /// all-or-nothing policy deliberately refuses. It therefore lives in its own properties -
    /// <see cref="AnalyticalModel_CapacityEnvelope"/>, <see cref="Path_TSD_CapacityEnvelope"/>,
    /// <see cref="CapacityEnvelope"/> - is excluded from <see cref="Rounds"/> and
    /// <see cref="Step_LastValid"/>, and is never what <see cref="AnalyticalModel_LastValid"/> holds.
    /// </para>
    ///
    /// <para><b>The last valid design survives every stop</b></para>
    /// <para>
    /// <see cref="AnalyticalModel_LastValid"/> is the model of the last iteration that was prepared,
    /// simulated and assessed successfully - which on a
    /// <see cref="PartOOptimisationStopReason.CapacityReached"/> stop is the design one full step below the
    /// unit's ceiling, and never a partial or unsimulated round. A cancelled or failed iteration contributes
    /// a recorded step and no model, so it can never become a false successful result.
    /// </para>
    /// </summary>
    public class PartOOptimisationRun
    {
        public PartOOptimisationRun(PartOOptimisationSettings partOOptimisationSettings)
        {
            Settings = partOOptimisationSettings ?? new PartOOptimisationSettings();
        }

        /// <summary>The step and the iteration guard this run was given.</summary>
        public PartOOptimisationSettings Settings { get; }

        /// <summary>
        /// Every iteration, in order, starting with the baseline at index 0. A refused, cancelled or failed
        /// iteration is here too - what was attempted is part of the record.
        /// </summary>
        public List<PartOOptimisationStep> Steps { get; } = [];

        /// <summary>Why the optimisation stopped. <see cref="PartOOptimisationStopReason.Running"/> until it has.</summary>
        public PartOOptimisationStopReason StopReason { get; set; } = PartOOptimisationStopReason.Running;

        /// <summary>The stop reason in the run's own words - what happened, and what an engineer does next.</summary>
        public string StopDescription { get; set; }

        /// <summary>
        /// The model of the last iteration that completed - prepared, simulated with the full year and
        /// assessed. Null only where even the baseline could not be established.
        /// </summary>
        public AnalyticalModel AnalyticalModel_LastValid { get; set; }

        /// <summary>The results file of that same iteration.</summary>
        public string Path_TSD_LastValid { get; set; }

        /// <summary>The scenarios of that same iteration's preparation, so its assessment can be re-run.</summary>
        public List<OverheatingScenario> OverheatingScenarios_LastValid { get; } = [];

        /// <summary>
        /// The converted TBD this run's later iterations were started from, or null where every iteration
        /// converted the model in full.
        /// <para>
        /// <b>Owned by this run and by nothing else.</b> It is not a cache and does not outlive the
        /// optimisation that made it - see <see cref="PartOCanonicalTBD"/>.
        /// </para>
        /// </summary>
        public PartOCanonicalTBD CanonicalTBD { get; set; }

        /// <summary>
        /// Why no canonical TBD was adopted, where none was - so a run that converted every iteration in
        /// full says why rather than simply being slow.
        /// </summary>
        public string CanonicalTBDRefusal { get; set; }

        /// <summary>
        /// How many iterations were warm started from <see cref="CanonicalTBD"/> rather than converting the
        /// geometry again. Counted off the steps, so it cannot disagree with them.
        /// </summary>
        public int WarmStarted => Steps.FindAll(x => x.WarmStarted).Count;

        /// <summary>The baseline, or null where the run never established one.</summary>
        public PartOOptimisationStep Step_Baseline => Steps.Count == 0 ? null : Steps[0];

        /// <summary>
        /// The last iteration that completed - the one <see cref="AnalyticalModel_LastValid"/> came from.
        /// <para>
        /// <b>The capacity envelope is excluded, and this is the single most important exclusion here.</b>
        /// An envelope is prepared, simulated over the full year and assessed, and it completes - so a
        /// "last step that completed" that did not say <i>which kind</i> of step would answer with the
        /// diagnostic, and the run would then hand back, and the command would adopt, a design the
        /// optimiser's own all-or-nothing policy refuses.
        /// </para>
        /// </summary>
        public PartOOptimisationStep Step_LastValid => Steps.FindLast(x => x.IsCompleted && !x.IsCapacityEnvelope);

        /// <summary>
        /// How many <b>optimisation rounds</b> ran - not the baseline, and <b>not the capacity envelope</b>.
        /// Counting the envelope would report it as another successful step at the configured airflow, which
        /// is precisely what it is not.
        /// </summary>
        public int Rounds => Steps.FindAll(x => x.IsOptimisationRound).Count;

        /// <summary>
        /// The diagnostic selected-equipment capacity envelope step, or null where none was calculated -
        /// because the optimisation passed, nothing eligible was left, the equipment had nothing more to
        /// give, or it was not asked for.
        /// </summary>
        public PartOOptimisationStep Step_CapacityEnvelope => Steps.Find(x => x.IsCapacityEnvelope);

        /// <summary>
        /// What the capacity envelope came to, as <c>SAM.Analytical</c> calculated it - the per-equipment
        /// proportional growth factors, ceilings and stated reasons. Null where none was calculated.
        /// </summary>
        public DesignAirFlowCapacityEnvelope CapacityEnvelope { get; set; }

        /// <summary>
        /// Why no capacity envelope was calculated, or - where one was - what it came to. <b>Always
        /// stated</b>, including when the answer is "the run passed, so there was nothing to diagnose":
        /// an optional diagnostic that silently produces nothing leaves a reader unable to tell it was
        /// considered at all.
        /// </summary>
        public string CapacityEnvelopeDescription { get; set; }

        /// <summary>
        /// What every round, and the capacity envelope, of this optimisation is derived from (PR-5): Iteration 2B, sourced from the
        /// Iteration 2 result the optimisation started from (run 0), and the design that result was derived from. Null where
        /// that result is not a proven one. Stamped on each round's model before it is simulated.
        /// </summary>
        public PartOBaselineReference PartOBaselineReference { get; set; }

        /// <summary>
        /// The <b>diagnostic</b> model the capacity envelope produced, simulated over the same full year
        /// and assessed with production TM59 - kept here and <b>nowhere near</b>
        /// <see cref="AnalyticalModel_LastValid"/>.
        /// <para>
        /// <b>This is not the run's answer and is never adopted.</b> It is what the already-selected
        /// equipment could deliver, which is a different statement from what the optimisation accepted.
        /// Feeding it into a later optimisation would make that run's baseline a design this run's own
        /// policy refused.
        /// </para>
        /// </summary>
        public AnalyticalModel AnalyticalModel_CapacityEnvelope { get; set; }

        /// <summary>The envelope's own results file - its own <c>-OptMax</c> identity, overwriting no round's evidence.</summary>
        public string Path_TSD_CapacityEnvelope { get; set; }

        /// <summary>The scenarios of the envelope's own preparation, so its assessment can be re-run.</summary>
        public List<OverheatingScenario> OverheatingScenarios_CapacityEnvelope { get; } = [];

        /// <summary>
        /// Whether a diagnostic capacity envelope design was produced, simulated and assessed - and
        /// therefore whether <see cref="AnalyticalModel_CapacityEnvelope"/> is there to look at.
        /// </summary>
        public bool HasCapacityEnvelope => AnalyticalModel_CapacityEnvelope is not null && (Step_CapacityEnvelope?.IsCompleted ?? false);

        /// <summary>Whether the optimisation ended with every eligible occupied space passing.</summary>
        public bool IsPassed => StopReason == PartOOptimisationStopReason.Passed;

        /// <summary>
        /// One paragraph stating what happened, in language that cannot be mistaken for a claim the run did
        /// not make.
        /// </summary>
        public string Description
        {
            get
            {
                PartOOptimisationStep partOOptimisationStep = Step_LastValid;

                string outcome = StopReason switch
                {
                    PartOOptimisationStopReason.Passed => string.Format(
                        "Every eligible occupied space passes its production TM59 criteria. This is the FIRST TESTED PASSING DESIGN at the configured {0:0.###} l/s step - it is not a minimum required airflow, because no search was run between the last failing design and this one.",
                        Settings.AirFlowStep_Lps),

                    PartOOptimisationStopReason.CapacityReached => string.Format(
                        "The selected ventilation unit cannot carry another full {0:0.###} l/s step, so the optimisation stopped at the last valid design. TM59 results that still fail are reported as they stand - a design limited by the equipment that was selected is a real answer, not a failure of the process. Selecting a larger product is a deliberate engineering decision and is never made automatically.",
                        Settings.AirFlowStep_Lps),

                    PartOOptimisationStopReason.NoEligibleTargets =>
                        "No failing space is left that this optimisation can move: either every remaining failure is outside the Part O dwelling scope, or it has no Approved Document O design terminal to raise. No terminal was invented to create one.",

                    PartOOptimisationStopReason.RebalanceRefused =>
                        "A design airflow round was refused, so no further design was produced. The last valid design is kept.",

                    PartOOptimisationStopReason.PreparationFailed =>
                        "The Part O iteration could not be re-prepared over the optimised design, so it was never simulated. The last valid design is kept.",

                    PartOOptimisationStopReason.SimulationFailed =>
                        "The TAS workflow did not produce the full-year results a TM59 assessment reads, so the round could not be assessed. The last valid design is kept.",

                    PartOOptimisationStopReason.AssessmentFailed =>
                        "The production TM59 assessment could not be produced for the simulated model, so the round's outcome is unknown. The last valid design is kept.",

                    PartOOptimisationStopReason.IterationLimitReached => string.Format(
                        "The safety limit of {0} optimisation iteration(s) was reached with eligible spaces still failing. Raise the limit, or reconsider the design or the selected equipment.",
                        Settings.MaximumIterations),

                    PartOOptimisationStopReason.Cancelled =>
                        "Cancelled. The last iteration that completed in full is kept; the cancelled one is recorded and is not a result.",

                    _ => "The optimisation has not finished.",
                };

                return string.Format(
                    "{0} optimisation round(s) after the baseline, at a {1:0.###} l/s step. Last valid design: run {2}. {3}{4}{5}",
                    Rounds,
                    Settings.AirFlowStep_Lps,
                    partOOptimisationStep is null ? "none" : partOOptimisationStep.Iteration.ToString(),
                    outcome,
                    string.IsNullOrWhiteSpace(StopDescription) ? string.Empty : " " + StopDescription,
                    string.IsNullOrWhiteSpace(CapacityEnvelopeDescription) ? string.Empty : " CAPACITY ENVELOPE: " + CapacityEnvelopeDescription);
            }
        }
    }
}
