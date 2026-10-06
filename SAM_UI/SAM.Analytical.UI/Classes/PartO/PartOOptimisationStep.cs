// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One iteration of an automatic Iteration 2B optimisation, recorded in enough detail to audit and
    /// reproduce it: what was deliberately targeted, what that derived, what the dwellings and their units
    /// then had to carry, which results file it produced, and what the production TM59 assessment made of it.
    /// <para>
    /// <b>Iteration 0 is the baseline.</b> It has no targets and no round - it is the Iteration 2 design as
    /// it stood, with its own results and its own assessment, and it is what every later iteration is read
    /// against. An optimisation that returned only its final model would leave nobody able to say what was
    /// tried or why it stopped.
    /// </para>
    /// <para>
    /// <b>The four authorities stay apart on every row.</b>
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>. The
    /// requirement is carried on each adjustment and never moves; the capacity is the unit's rating and is
    /// never a target; nothing here is a runtime airflow.
    /// </para>
    /// </summary>
    public class PartOOptimisationStep
    {
        /// <param name="iteration">0 for the baseline, then 1 upwards - one per optimisation round.</param>
        public PartOOptimisationStep(int iteration)
            : this(iteration, iteration == 0 ? PartOOptimisationStepKind.Baseline : PartOOptimisationStepKind.OptimisationRound)
        {
        }

        /// <param name="iteration">0 for the baseline, then 1 upwards - one per optimisation round.</param>
        /// <param name="partOOptimisationStepKind">
        /// <b>What this step is</b>, stated rather than inferred from the number. A capacity envelope is
        /// prepared, simulated and assessed exactly as a round is and it completes, so nothing about its
        /// lifecycle tells it apart from one - see <see cref="PartOOptimisationStepKind"/>.
        /// </param>
        public PartOOptimisationStep(int iteration, PartOOptimisationStepKind partOOptimisationStepKind)
        {
            Iteration = iteration;
            Kind = partOOptimisationStepKind;
        }

        /// <summary>Which iteration this is. <b>0 is the baseline</b>, not the first round.</summary>
        public int Iteration { get; }

        /// <summary>
        /// What this step is - baseline, ordinary optimisation round, or diagnostic capacity envelope.
        /// <para>
        /// <b>Read this rather than the iteration number.</b> The envelope carries an iteration number so
        /// its files sort with the rest of the run, and that number is not what makes it one.
        /// </para>
        /// </summary>
        public PartOOptimisationStepKind Kind { get; }

        /// <summary>Whether this step is the baseline the optimisation started from.</summary>
        public bool IsBaseline => Kind == PartOOptimisationStepKind.Baseline;

        /// <summary>
        /// Whether this step is an ordinary optimisation round - the only kind that counts towards the
        /// round total and the only kind that may become the run's last valid design.
        /// </summary>
        public bool IsOptimisationRound => Kind == PartOOptimisationStepKind.OptimisationRound;

        /// <summary>
        /// Whether this step is the diagnostic selected-equipment capacity envelope. <b>Not a round</b>,
        /// however completely it ran.
        /// </summary>
        public bool IsCapacityEnvelope => Kind == PartOOptimisationStepKind.CapacityEnvelope;

        /// <summary>
        /// The project name this iteration's TAS case ran as - <c>&lt;project&gt;-Opt00</c> upwards. The
        /// identity that ties the model, the TBD and the TSD of one iteration together.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// The results file this iteration produced. <b>Unique per iteration</b>, so no round overwrites the
        /// evidence for another and any of them can be re-opened afterwards.
        /// </summary>
        public string Path_TSD { get; set; }

        /// <summary>The weather this iteration was simulated against - the same one every iteration uses.</summary>
        public string WeatherData { get; set; }

        /// <summary>
        /// Whether this iteration was <b>warm started</b> from the run's canonical TBD rather than
        /// converting the geometry again.
        /// <para>
        /// <b>Recorded per iteration, not per run.</b> A warm start is allowed only while the canonical
        /// baseline is provably still the conversion of this model and this TAS case, and that is checked
        /// every round - so one round of a run can fall back to the full conversion while the others do not,
        /// and a reader has to be able to see which did. The reason for any fallback is on
        /// <see cref="Notes"/>.
        /// </para>
        /// <para>
        /// It changes <b>nothing</b> about what the iteration means: either way it is a real full-year
        /// simulation of this design, assessed with production TM59, and either way the result is this
        /// iteration's own.
        /// </para>
        /// </summary>
        public bool WarmStarted { get; set; }

        /// <summary>
        /// The rooms this round <b>deliberately</b> targeted, with the airflow each was designed at before
        /// the round, what was requested of it, and what it achieved. Empty on the baseline.
        /// <para>
        /// An automatic round achieves exactly what it requested or it is not adopted, so these agree - which
        /// is the point of recording both rather than one.
        /// </para>
        /// </summary>
        public List<DesignAirFlowAdjustment> TargetedAdjustments { get; } = [];

        /// <summary>
        /// The rooms that moved to keep their dwellings balanced. <b>Not optimisation targets</b>, and kept
        /// apart from them for exactly that reason: a report that merged the two would claim every room that
        /// moved was chosen.
        /// </summary>
        public List<DesignAirFlowAdjustment> DerivedAdjustments { get; } = [];

        /// <summary>
        /// Targets this round could not take, with the reason - the explicit "not automatically optimisable"
        /// record for a room with no Approved Document O design terminal to move.
        /// </summary>
        public List<DesignAirFlowTargetRefusal> TargetRefusals { get; } = [];

        /// <summary>
        /// What each dwelling and its air handling unit carried after this iteration - the duty, the
        /// selected product, its maximum, the remaining headroom and the equipment outcome.
        /// </summary>
        public List<PartOOptimisationUnitState> UnitStates { get; } = [];

        /// <summary>
        /// <b>The complete design ventilation vector after this iteration</b> - every space and direction
        /// this run's equipment serves, whether or not anything moved it.
        /// <para>
        /// The adjustments above say what CHANGED; this says what EXISTS. Both are needed, and recording
        /// only the first is what made the airflow history read as though an untouched direction had been
        /// removed: a room-direction no round moved contributed no adjustment, so it appeared in no step and
        /// the report could not print it. See <see cref="PartODesignAirFlowState"/>.
        /// </para>
        /// </summary>
        public List<PartODesignAirFlowState> DesignAirFlowStates { get; } = [];

        /// <summary>
        /// Every adjustment this iteration made, <b>targeted first</b> and derived after - the order the
        /// round itself settled on. Each one still carries <see cref="DesignAirFlowAdjustment.IsDerived"/>,
        /// so nothing about which were engineering decisions is lost by reading them together.
        /// </summary>
        public List<DesignAirFlowAdjustment> Adjustments()
        {
            List<DesignAirFlowAdjustment> result = [.. TargetedAdjustments];

            result.AddRange(DerivedAdjustments);

            return result;
        }

        /// <summary>
        /// The production TM59 result for every resolved design space of this iteration, criterion by
        /// criterion, exactly as the assessment produced them.
        /// </summary>
        public List<PartOTM59SpaceResult> TM59Results { get; } = [];

        /// <summary>
        /// The production TM59 verdict for the occupied spaces of this iteration, combined by the assessment
        /// itself. <b>Never recomputed here</b> from the rows above.
        /// </summary>
        public TM59ComplianceStatus OccupiedSpaceComplianceStatus { get; set; } = TM59ComplianceStatus.Undefined;

        /// <summary>Notes worth keeping from the round, the preparation and the assessment.</summary>
        public List<string> Notes { get; } = [];

        /// <summary>Advisories that did not stop the iteration.</summary>
        public List<string> Warnings { get; } = [];

        /// <summary>Why this iteration did not complete, where it did not.</summary>
        public List<string> Refusals { get; } = [];

        /// <summary>
        /// Whether this iteration produced a design that was simulated and assessed - and is therefore a
        /// step the optimisation could carry forward. False on an iteration that was refused, cancelled or
        /// could not be simulated.
        /// </summary>
        public bool IsCompleted { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Run {0} ({1}): {2} targeted, {3} derived, TM59 {4}",
                IsCapacityEnvelope ? "MAX" : Iteration.ToString(),
                Core.Query.Description(Kind),
                TargetedAdjustments.Count,
                DerivedAdjustments.Count,
                Core.Query.Description(OccupiedSpaceComplianceStatus));
        }
    }
}
