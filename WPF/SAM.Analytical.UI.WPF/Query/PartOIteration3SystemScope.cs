// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// SAM #114's production answer: which authored ventilation systems the Iteration 3
        /// materialisation is given, decided by <b>identity</b>.
        ///
        /// <para><b>The rule is SAM's (PR-1)</b></para>
        /// <para>
        /// Keep exactly the systems the Part O preparation built; remove an authored system that carries
        /// no effective mechanical duty anywhere in the thermal model; <b>refuse</b> - rather than choose -
        /// where an authored system does carry one. That rule, and why the whole thermal domain is
        /// inspected, is <c>SAM.Analytical.Query.PartOSystemsMaterialisationScope</c>, shared with Mixed
        /// Design. This method only asks it, and words its structured answer the way Iteration 3 always has
        /// (its records persist these notes), so Iteration 3's behaviour did not change when the rule moved.
        /// Nothing here decides what is kept, removed or refused.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">
        /// The <b>prepared</b> design. Read only; the working copy is built from it and it is never
        /// modified.
        /// </param>
        /// <param name="guids_VentilationSystem_Prepared">
        /// The identities the Part O preparation reported - <c>PartORun.Guids_VentilationSystem_Prepared</c>.
        /// </param>
        /// <param name="guids_Space_Dwelling">
        /// The Part O dwelling design scope, from <c>Query.PartODwellingSpaceGuids</c>. Used only to word
        /// the refusal.
        /// </param>
        public static PartOIteration3SystemScope PartOIteration3SystemScope(AdjacencyCluster adjacencyCluster, IEnumerable<Guid> guids_VentilationSystem_Prepared, IEnumerable<Guid> guids_Space_Dwelling)
        {
            PartOSystemsMaterialisationScope partOSystemsMaterialisationScope = Analytical.Query.PartOSystemsMaterialisationScope(adjacencyCluster, guids_VentilationSystem_Prepared, guids_Space_Dwelling);

            if (!partOSystemsMaterialisationScope.IsScoped)
            {
                return new PartOIteration3SystemScope(null, null, null, null, partOSystemsMaterialisationScope.Refusals.ConvertAll(PartOIteration3SystemScopeRefusal));
            }

            List<string> notes = partOSystemsMaterialisationScope.Exclusions.ConvertAll(PartOIteration3SystemScopeNote);

            notes.Add(string.Format(
                "{0} ventilation system(s) built by this Part O iteration are the design under assessment; {1} authored system(s) were left out of the materialisation input and none of them states mechanical duty.",
                partOSystemsMaterialisationScope.Guids_Retained.Count,
                partOSystemsMaterialisationScope.Guids_Removed.Count));

            return new PartOIteration3SystemScope(partOSystemsMaterialisationScope.AdjacencyCluster, partOSystemsMaterialisationScope.Guids_Retained, partOSystemsMaterialisationScope.Guids_Removed, notes, null);
        }

        /// <summary>Iteration 3's wording of one system SAM's scope left out.</summary>
        private static string PartOIteration3SystemScopeNote(PartOSystemsScopeExclusion partOSystemsScopeExclusion)
        {
            return string.Format(
                "Ventilation system '{0}' ({1}) was left out of the materialisation input: this iteration did not build it, and it carries {2} - so it states no mechanical duty that Candidate B's explicit Systems route has to recreate. "
                + "It remains on the design and in the thermal model, where its authored behaviour is simulated exactly as Reference A simulates it.",
                partOSystemsScopeExclusion.Name_VentilationSystem,
                partOSystemsScopeExclusion.Guid_VentilationSystem,
                partOSystemsScopeExclusion.Count_VentilationTerminal == 0 ? "no design ventilation terminal" : string.Format("{0} design ventilation terminal(s), none of which states an effective design airflow", partOSystemsScopeExclusion.Count_VentilationTerminal));
        }

        /// <summary>Iteration 3's wording of one reason SAM's scope refused.</summary>
        private static string PartOIteration3SystemScopeRefusal(PartOSystemsScopeRefusal partOSystemsScopeRefusal)
        {
            double designFlowRate_Lps = partOSystemsScopeRefusal.DesignFlowRate_Lps ?? double.NaN;

            switch (partOSystemsScopeRefusal.Reason)
            {
                case PartOSystemsScopeRefusalReason.NoModel:
                    return "No prepared model was supplied, so there is no ventilation design to scope.";

                case PartOSystemsScopeRefusalReason.NoIdentities:
                    return "This Part O run captured no ventilation system identities from its preparation, so which of the model's authored ventilation systems is the design under assessment is not known. "
                        + "Prepare and run Iteration 1a again in this session; a reopened run records what was run rather than how it was prepared.";

                case PartOSystemsScopeRefusalReason.IdentityNotOnModel:
                    return string.Format(
                        "The ventilation system {0} this Part O run was prepared with is not on the prepared model, so the design under assessment cannot be identified on it.",
                        partOSystemsScopeRefusal.Guid_VentilationSystem);

                case PartOSystemsScopeRefusalReason.DutyServesNoSpace:
                    return string.Format(
                        "Ventilation system '{0}' ({1}) carries the design terminal '{2}' ({3}) at {4:0.###} l/s, and that terminal is not related to any space - so it cannot be shown that removing its mechanical ventilation does not change the thermal case. "
                        + "Approved Document O Iteration 3 will not compare a model it cannot account for.",
                        partOSystemsScopeRefusal.Name_VentilationSystem,
                        partOSystemsScopeRefusal.Guid_VentilationSystem,
                        partOSystemsScopeRefusal.Name_VentilationTerminal,
                        partOSystemsScopeRefusal.Guid_VentilationTerminal,
                        designFlowRate_Lps);

                case PartOSystemsScopeRefusalReason.DutyInsideDwellingScope:
                    return string.Format(
                        "Ventilation system '{0}' ({1}) carries a design {2} terminal of {3:0.###} l/s in '{4}' ({5}), which is inside the assessed Approved Document O dwelling scope. "
                        + "That is a second mechanical ventilation design for a room this iteration already designed, and choosing between two designs is not this orchestration's decision. "
                        + "Resolve the model so one mechanical design serves the room, then run Iteration 3 again.",
                        partOSystemsScopeRefusal.Name_VentilationSystem,
                        partOSystemsScopeRefusal.Guid_VentilationSystem,
                        partOSystemsScopeRefusal.FlowClassification,
                        designFlowRate_Lps,
                        partOSystemsScopeRefusal.Name_Space,
                        partOSystemsScopeRefusal.Guid_Space);

                case PartOSystemsScopeRefusalReason.DutyOutsideDwellingScope:
                    return string.Format(
                        "Ventilation system '{0}' ({1}) carries a design {2} terminal of {3:0.###} l/s in '{4}' ({5}). That room is outside the assessed dwellings but is part of the same thermal model. "
                        + "Candidate B's no-IZAM source removes mechanical ventilation from the WHOLE model and reinstates only the systems materialised here, so this room's ventilation would exist in Reference A and be absent from Candidate B - and it is thermally coupled to the assessed rooms. "
                        + "The two cases would therefore differ by more than the route being compared, so the comparison is refused rather than reported.",
                        partOSystemsScopeRefusal.Name_VentilationSystem,
                        partOSystemsScopeRefusal.Guid_VentilationSystem,
                        partOSystemsScopeRefusal.FlowClassification,
                        designFlowRate_Lps,
                        partOSystemsScopeRefusal.Name_Space,
                        partOSystemsScopeRefusal.Guid_Space);

                case PartOSystemsScopeRefusalReason.NotRemovable:
                    return string.Format(
                        "Ventilation system {0} could not be removed from the materialisation input, so the systems handed to the materialisation are not the ones this scope decided on.",
                        partOSystemsScopeRefusal.Guid_VentilationSystem);

                default:
                    //A reason added to SAM after this wording: SAM's own sentence, never silence.
                    return partOSystemsScopeRefusal.Message;
            }
        }
    }
}
