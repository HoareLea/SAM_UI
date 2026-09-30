// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

// TEMPORARY PR-1 PIN. A VERBATIM copy of the Iteration 3 system-scope rule as it stood in SAM_UI at a3ae5df,
// before PR-1 moved it into SAM (Analytical.Query.PartOSystemsMaterialisationScope). It exists only so
// PartOIteration3SystemScopeEquivalenceTests can prove the move is output-identical, and is deleted once that
// proof is recorded. It is not a second implementation to maintain.

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF.Tests
{
    internal class PartOIteration3SystemScopeOracleResult
    {
        private readonly List<Guid> guids_Retained = [];

        private readonly List<Guid> guids_Removed = [];

        private readonly List<string> notes = [];

        private readonly List<string> refusals = [];

        internal PartOIteration3SystemScopeOracleResult(
            AdjacencyCluster adjacencyCluster,
            IEnumerable<Guid> guids_Retained,
            IEnumerable<Guid> guids_Removed,
            IEnumerable<string> notes,
            IEnumerable<string> refusals)
        {
            foreach (Guid guid in guids_Retained ?? [])
            {
                this.guids_Retained.Add(guid);
            }

            foreach (Guid guid in guids_Removed ?? [])
            {
                this.guids_Removed.Add(guid);
            }

            foreach (string note in notes ?? [])
            {
                if (!string.IsNullOrWhiteSpace(note))
                {
                    this.notes.Add(note);
                }
            }

            foreach (string refusal in refusals ?? [])
            {
                if (!string.IsNullOrWhiteSpace(refusal))
                {
                    this.refusals.Add(refusal);
                }
            }

            //Fail closed, structurally - see the class summary.
            AdjacencyCluster = this.refusals.Count == 0 ? adjacencyCluster : null;

            if (this.refusals.Count != 0)
            {
                this.guids_Retained.Clear();
                this.guids_Removed.Clear();
                this.notes.Clear();
            }
        }

        /// <summary>
        /// The working copy handed to <c>SAM.Analytical.Systems.Create.MechanicalVentilation</c>, or
        /// <b>null</b> whenever <see cref="Refusals"/> is non-empty.
        /// </summary>
        public AdjacencyCluster AdjacencyCluster { get; }

        /// <summary>The systems kept - exactly the ones the Part O preparation built, by identity.</summary>
        public List<Guid> Guids_Retained => [.. guids_Retained];

        /// <summary>The authored systems removed from the working copy, by identity.</summary>
        public List<Guid> Guids_Removed => [.. guids_Removed];

        /// <summary>Why each removed system carried no mechanical duty Candidate B has to recreate.</summary>
        public List<string> Notes => [.. notes];

        /// <summary>Why no scope could be taken. Ordered, and never empty on a refusal.</summary>
        public List<string> Refusals => [.. refusals];

        /// <summary>Whether a working copy was produced at all.</summary>
        public bool IsScoped => AdjacencyCluster is not null && refusals.Count == 0;

        public override string ToString()
        {
            return IsScoped
                ? string.Format("System scope: {0} retained, {1} removed.", guids_Retained.Count, guids_Removed.Count)
                : string.Format("System scope REFUSED ({0} reason(s)).", refusals.Count);
        }
    }

    internal static class PartOIteration3SystemScopeOracle
    {
        internal static PartOIteration3SystemScopeOracleResult Scope(AdjacencyCluster adjacencyCluster, IEnumerable<Guid> guids_VentilationSystem_Prepared, IEnumerable<Guid> guids_Space_Dwelling)
        {
            List<string> refusals = [];
            List<string> notes = [];

            if (adjacencyCluster is null)
            {
                refusals.Add("No prepared model was supplied, so there is no ventilation design to scope.");

                return new PartOIteration3SystemScopeOracleResult(null, null, null, null, refusals);
            }

            HashSet<Guid> guids_Retained = [];
            foreach (Guid guid in guids_VentilationSystem_Prepared ?? [])
            {
                if (guid != Guid.Empty)
                {
                    guids_Retained.Add(guid);
                }
            }

            if (guids_Retained.Count == 0)
            {
                refusals.Add(
                    "This Part O run captured no ventilation system identities from its preparation, so which of the model's authored ventilation systems is the design under assessment is not known. "
                    + "Prepare and run Iteration 1a again in this session; a reopened run records what was run rather than how it was prepared.");

                return new PartOIteration3SystemScopeOracleResult(null, null, null, null, refusals);
            }

            HashSet<Guid> guids_Dwelling = [];
            foreach (Guid guid in guids_Space_Dwelling ?? [])
            {
                guids_Dwelling.Add(guid);
            }

            //One pass each. Nothing below re-enumerates the model.
            Dictionary<Guid, VentilationSystem> dictionary_System = [];
            foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetObjects<VentilationSystem>() ?? [])
            {
                if (ventilationSystem is not null && ventilationSystem.Guid != Guid.Empty)
                {
                    dictionary_System[ventilationSystem.Guid] = ventilationSystem;
                }
            }

            foreach (Guid guid in guids_Retained)
            {
                if (!dictionary_System.ContainsKey(guid))
                {
                    refusals.Add(string.Format(
                        "The ventilation system {0} this Part O run was prepared with is not on the prepared model, so the design under assessment cannot be identified on it.",
                        guid));
                }
            }

            if (refusals.Count != 0)
            {
                return new PartOIteration3SystemScopeOracleResult(null, null, null, null, refusals);
            }

            List<Guid> guids_Removed = [];

            foreach (KeyValuePair<Guid, VentilationSystem> keyValuePair in dictionary_System)
            {
                Guid guid_System = keyValuePair.Key;

                if (guids_Retained.Contains(guid_System))
                {
                    continue;
                }

                VentilationSystem ventilationSystem = keyValuePair.Value;

                int count_Terminal = 0;
                int count_Duty = 0;

                foreach (VentilationTerminal ventilationTerminal in adjacencyCluster.VentilationTerminals(ventilationSystem) ?? [])
                {
                    if (ventilationTerminal is null)
                    {
                        continue;
                    }

                    count_Terminal++;

                    if (!IsEffectiveMechanicalDuty(ventilationTerminal))
                    {
                        continue;
                    }

                    count_Duty++;

                    List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(ventilationTerminal) ?? [];

                    if (spaces.Count == 0)
                    {
                        //Fail closed. A duty that serves no identified room cannot be shown to be outside
                        //the thermal case, and "probably harmless" is not a standard this comparison can
                        //be built on.
                        refusals.Add(string.Format(
                            "Ventilation system '{0}' ({1}) carries the design terminal '{2}' ({3}) at {4:0.###} l/s, and that terminal is not related to any space - so it cannot be shown that removing its mechanical ventilation does not change the thermal case. "
                            + "Approved Document O Iteration 3 will not compare a model it cannot account for.",
                            ventilationSystem.Name,
                            guid_System,
                            ventilationTerminal.Name,
                            ventilationTerminal.Guid,
                            ventilationTerminal.DesignFlowRate_Lps ?? double.NaN));

                        continue;
                    }

                    foreach (Space space in spaces)
                    {
                        if (space is null)
                        {
                            continue;
                        }

                        refusals.Add(guids_Dwelling.Contains(space.Guid)
                            ? string.Format(
                                "Ventilation system '{0}' ({1}) carries a design {2} terminal of {3:0.###} l/s in '{4}' ({5}), which is inside the assessed Approved Document O dwelling scope. "
                                + "That is a second mechanical ventilation design for a room this iteration already designed, and choosing between two designs is not this orchestration's decision. "
                                + "Resolve the model so one mechanical design serves the room, then run Iteration 3 again.",
                                ventilationSystem.Name,
                                guid_System,
                                ventilationTerminal.FlowClassification,
                                ventilationTerminal.DesignFlowRate_Lps ?? double.NaN,
                                space.Name,
                                space.Guid)
                            : string.Format(
                                "Ventilation system '{0}' ({1}) carries a design {2} terminal of {3:0.###} l/s in '{4}' ({5}). That room is outside the assessed dwellings but is part of the same thermal model. "
                                + "Candidate B's no-IZAM source removes mechanical ventilation from the WHOLE model and reinstates only the systems materialised here, so this room's ventilation would exist in Reference A and be absent from Candidate B - and it is thermally coupled to the assessed rooms. "
                                + "The two cases would therefore differ by more than the route being compared, so the comparison is refused rather than reported.",
                                ventilationSystem.Name,
                                guid_System,
                                ventilationTerminal.FlowClassification,
                                ventilationTerminal.DesignFlowRate_Lps ?? double.NaN,
                                space.Name,
                                space.Guid));
                    }
                }

                if (count_Duty != 0)
                {
                    continue;
                }

                guids_Removed.Add(guid_System);

                notes.Add(string.Format(
                    "Ventilation system '{0}' ({1}) was left out of the materialisation input: this iteration did not build it, and it carries {2} - so it states no mechanical duty that Candidate B's explicit Systems route has to recreate. "
                    + "It remains on the design and in the thermal model, where its authored behaviour is simulated exactly as Reference A simulates it.",
                    ventilationSystem.Name,
                    guid_System,
                    count_Terminal == 0 ? "no design ventilation terminal" : string.Format("{0} design ventilation terminal(s), none of which states an effective design airflow", count_Terminal)));
            }

            if (refusals.Count != 0)
            {
                return new PartOIteration3SystemScopeOracleResult(null, null, null, null, refusals);
            }

            //Ordered before the removals so the working copy, the record and the evidence are the same on
            //every machine - a dictionary walk is not.
            guids_Removed.Sort();

            List<Guid> guids_Retained_Ordered = [.. guids_Retained];
            guids_Retained_Ordered.Sort();

            //A COPY. The removals below must never reach the design - see the class summary. The shallow
            //copy is the right one: it rebuilds the cluster's own dictionaries, which is all that is
            //written here, and shares the objects, none of which is touched.
            AdjacencyCluster adjacencyCluster_Working = new(adjacencyCluster);

            foreach (Guid guid in guids_Removed)
            {
                VentilationSystem ventilationSystem = adjacencyCluster_Working.GetObject<VentilationSystem>(guid);

                if (ventilationSystem is null || !adjacencyCluster_Working.RemoveObject(ventilationSystem))
                {
                    refusals.Add(string.Format(
                        "Ventilation system {0} could not be removed from the materialisation input, so the systems handed to the materialisation are not the ones this scope decided on.",
                        guid));
                }
            }

            if (refusals.Count != 0)
            {
                return new PartOIteration3SystemScopeOracleResult(null, null, null, null, refusals);
            }

            notes.Add(string.Format(
                "{0} ventilation system(s) built by this Part O iteration are the design under assessment; {1} authored system(s) were left out of the materialisation input and none of them states mechanical duty.",
                guids_Retained_Ordered.Count,
                guids_Removed.Count));

            return new PartOIteration3SystemScopeOracleResult(adjacencyCluster_Working, guids_Retained_Ordered, guids_Removed, notes, null);
        }

        /// <summary>
        /// Whether one design terminal states mechanical duty that actually moves air.
        /// <para>
        /// A supply or extract terminal with a stated, finite, non-zero design airflow does. A terminal
        /// with no stated airflow, with <see cref="double.NaN"/>, or designed at nothing does not - there
        /// is no ventilation for the no-IZAM sweep to remove and none for Candidate B to reinstate, so
        /// refusing over it would refuse models nothing is wrong with.
        /// </para>
        /// <para>
        /// The classification is required as well as the value: <c>FlowClassification.Undefined</c> on a
        /// terminal carrying a number states neither supply nor extract, and a duty whose direction is
        /// unknown is not one this can reason about - so it counts, and refuses.
        /// </para>
        /// </summary>
        private static bool IsEffectiveMechanicalDuty(VentilationTerminal ventilationTerminal)
        {
            double? designFlowRate_Lps = ventilationTerminal?.DesignFlowRate_Lps;

            return designFlowRate_Lps.HasValue
                && !double.IsNaN(designFlowRate_Lps.Value)
                && !double.IsInfinity(designFlowRate_Lps.Value)
                && designFlowRate_Lps.Value != 0;
        }
    }
}
