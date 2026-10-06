// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Why an automatic Approved Document O Iteration 2B optimisation stopped.
    /// <para>
    /// <b>Stated, never inferred from prose.</b> An optimisation that ended at the selected unit's capacity
    /// with rooms still failing is a perfectly good engineering answer, and one that ended because the
    /// simulation would not run is not - a reader must not have to tell those apart by reading a message.
    /// Every value below is a different thing for an engineer to do next.
    /// </para>
    /// </summary>
    public enum PartOOptimisationStopReason
    {
        /// <summary>The optimisation has not finished. Never a final state.</summary>
        [Description("Running")] Running,

        /// <summary>Every eligible occupied space passes its production TM59 criteria.</summary>
        [Description("Passed")] Passed,

        /// <summary>
        /// The selected ventilation unit cannot carry another full step in any remaining dwelling. The last
        /// valid design is kept and is the run's answer.
        /// <para>
        /// <b>A useful result, not a failure.</b> It says what the selected product can and cannot deliver,
        /// which is exactly what an engineer choosing equipment needs to know.
        /// </para>
        /// </summary>
        [Description("Capacity Reached")] CapacityReached,

        /// <summary>
        /// Nothing is left to target: either every failing space is outside the optimisation's scope, or no
        /// failing space has an Approved Document O design terminal to move.
        /// </summary>
        [Description("No Eligible Targets")] NoEligibleTargets,

        /// <summary>
        /// The design airflow round itself refused - an Approved Document F floor, a dwelling that could not
        /// be balanced, or terminals that could not be attributed. Distinct from
        /// <see cref="CapacityReached"/>, which is the equipment saying no rather than the design.
        /// </summary>
        [Description("Rebalance Refused")] RebalanceRefused,

        /// <summary>Re-preparing the Part O iteration over the optimised design failed.</summary>
        [Description("Preparation Failed")] PreparationFailed,

        /// <summary>The TAS workflow did not produce a full-year result this run could be assessed from.</summary>
        [Description("Simulation Failed")] SimulationFailed,

        /// <summary>The production TM59 assessment could not be produced or resolved for the simulated model.</summary>
        [Description("Assessment Failed")] AssessmentFailed,

        /// <summary>The configured maximum number of optimisation iterations was reached.</summary>
        [Description("Iteration Limit Reached")] IterationLimitReached,

        /// <summary>The user cancelled. Nothing partial is left behind as a successful run.</summary>
        [Description("Cancelled")] Cancelled,
    }
}
