// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What an automatic Approved Document O Iteration 2B optimisation is allowed to do: how big a design
    /// airflow step it takes, and how many rounds it may take before it stops on its own.
    /// <para>
    /// <b>A fixed step, deliberately.</b> Iteration 2B v1 raises each eligible failing room's design airflow
    /// by the same amount every round and reruns the whole thermal case. It does not search for the least
    /// airflow that would pass, and a result reached at a 5 l/s step must therefore never be described as a
    /// minimum - it is the first tested passing design at that step. Anything else would be claiming a
    /// search nobody ran.
    /// </para>
    /// <para>
    /// <b>The iteration limit is a guard, not a target.</b> A full-year TAS run per round is minutes of
    /// work, and a design that responds non-monotonically could otherwise iterate until somebody noticed.
    /// The optimisation is expected to end at
    /// <see cref="PartOOptimisationStopReason.Passed"/> or
    /// <see cref="PartOOptimisationStopReason.CapacityReached"/> long before it.
    /// </para>
    /// </summary>
    public class PartOOptimisationSettings
    {
        /// <summary>The step every Iteration 2B v1 optimisation defaults to [l/s].</summary>
        public const double DefaultAirFlowStep_Lps = 5;

        /// <summary>
        /// The default safety limit on optimisation rounds. Chosen so a dwelling starting at its Approved
        /// Document F minimum can explore a realistic domestic unit's whole capacity in 5 l/s steps and
        /// still stop on the equipment rather than on the guard.
        /// </summary>
        public const int DefaultMaximumIterations = 10;

        /// <summary>
        /// How much each eligible failing room's design airflow is raised per round [l/s]. Must be positive:
        /// a step of zero would rerun the same design forever, and a negative one is not an optimisation.
        /// </summary>
        public double AirFlowStep_Lps { get; set; } = DefaultAirFlowStep_Lps;

        /// <summary>
        /// The most optimisation rounds to run, not counting the baseline. Must be at least one.
        /// </summary>
        public int MaximumIterations { get; set; } = DefaultMaximumIterations;

        /// <summary>Flow rate comparison tolerance [l/s], passed to every design airflow round.</summary>
        public double Tolerance_Lps { get; set; } = 0.001;

        /// <summary>
        /// Whether each iteration may be <b>warm started</b> from the canonical TBD the run's own baseline
        /// conversion produced, instead of converting the same geometry again.
        ///
        /// <para><b>A workflow optimisation, and only that</b></para>
        /// <para>
        /// Between Iteration 2B rounds only the ventilation design and network change; the geometry, zones,
        /// surfaces, apertures, constructions and the shading calculation are identical every round. A
        /// warm-started round still performs a <b>real full-year TAS simulation</b> of the current design
        /// and is still assessed with production TM59 - it simply does not recompute the conversion of
        /// inputs that did not change. Measured on the licensed acceptance model, that conversion is 41.6 s
        /// of a 64.2 s round while the simulation itself is 3.6 s.
        /// </para>
        /// <para>
        /// <b>The full conversion remains the authority.</b> Warm starting is allowed only while the
        /// canonical baseline is provably still the conversion of this model and this TAS case, checked
        /// every round; anything unproven falls back to the full path and says why. Turning this off runs
        /// exactly the workflow every round ran before it existed, which is what makes it usable as a
        /// reference.
        /// </para>
        /// </summary>
        public bool WarmStart { get; set; } = true;

        /// <summary>
        /// Whether, when the optimisation stops with eligible rooms still failing, one further
        /// <b>diagnostic</b> run is made: the last valid design grown proportionally - every terminal
        /// keeping its share of it - until the <i>already-selected</i> unit's own design-capacity ceiling
        /// binds.
        ///
        /// <para><b>What it is for</b></para>
        /// <para>
        /// A run that stops on capacity, or on the iteration guard, leaves an engineer with "this design
        /// still fails" and no statement of how close the equipment already bought can get. The envelope is
        /// that statement - "what design could the unit I have already bought support?" - and it is not
        /// another round: it is a design the all-or-nothing policy deliberately refuses, evaluated once, on
        /// its own, and reported as its own stage.
        /// </para>
        ///
        /// <para><b>Why it is optional, and why it defaults to on</b></para>
        /// <para>
        /// It costs one more full-year TAS run, which is minutes, and there are runs where nobody wants to
        /// spend them. It defaults to on because the case it answers - a run stopping short of a pass - is
        /// exactly the case in which the run on its own does not tell an engineer what to do next, and
        /// because it never touches the design the optimisation accepted.
        /// </para>
        /// <para>
        /// It costs <b>nothing</b> on a run that passes: there is then no failing target to envelope and
        /// none is evaluated.
        /// </para>
        /// </summary>
        public bool CapacityEnvelope { get; set; } = true;

        /// <summary>
        /// Whether these settings describe an optimisation that can actually run, and why not where they do
        /// not.
        /// </summary>
        public bool IsValid(out string refusal)
        {
            refusal = null;

            if (double.IsNaN(AirFlowStep_Lps) || double.IsInfinity(AirFlowStep_Lps) || AirFlowStep_Lps <= 0)
            {
                refusal = string.Format("An airflow step of {0} l/s is not an optimisation step - it has to be a finite airflow greater than zero. A step of zero would re-simulate the same design every round.", AirFlowStep_Lps);

                return false;
            }

            if (MaximumIterations < 1)
            {
                refusal = string.Format("A maximum of {0} optimisation iteration(s) leaves nothing to run. Allow at least one round.", MaximumIterations);

                return false;
            }

            return true;
        }
    }
}
