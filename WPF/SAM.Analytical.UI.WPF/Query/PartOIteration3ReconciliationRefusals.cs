// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.TPD;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The engineering reconciliation of an Iteration 3 pairing: everything that has to be true before
        /// two sets of temperatures may be called a comparison of two <b>routes</b> over one design.
        ///
        /// <para><b>Guid only, one pass each, fail closed</b></para>
        /// <para>
        /// Every set below is a <c>HashSet&lt;Guid&gt;</c> or a <c>Dictionary&lt;Guid, ...&gt;</c> built
        /// once; no list is scanned inside a loop over another list and nothing is matched by name. Any
        /// single failure refuses the whole pairing - there is no "compare the rooms that agree", because
        /// the rooms that agree are not the ones a wrong answer would be hiding in.
        /// </para>
        ///
        /// <para><b>What is checked, and what each failure would otherwise look like</b></para>
        /// <list type="number">
        /// <item><b>The no-IZAM sweep happened.</b> Without it Candidate B carries the building's own
        /// mechanical ventilation AND the explicit TAS Systems network - the ventilation modelled twice,
        /// which produces entirely plausible temperatures.</item>
        /// <item><b>Every bound room is in the dwelling scope.</b> A route binding outside it is a room
        /// this run never intended to assess.</item>
        /// <item><b>Every retained system's design duty is bound.</b> This is the other half of the
        /// whole-thermal-domain comparability gate: <c>Query.PartOIteration3SystemScope</c> refuses an
        /// unrelated system that carries duty, and this refuses a duty of the design under assessment
        /// that Candidate B did not recreate. Together they prove that every effective mechanical
        /// ventilation the no-IZAM sweep removed is reinstated explicitly.</item>
        /// <item><b>No missing and no extra binding against the two assessments.</b> A room that produced
        /// no series on one side cannot be compared, and a series for a room nobody bound is a room
        /// resolved wrongly.</item>
        /// <item><b>The same rooms are assessed by both.</b> A TM59 verdict over a different set of rooms
        /// is a different verdict.</item>
        /// <item><b>The same criteria per room.</b> A room assessed against the mechanical criterion in
        /// one case and the natural one in the other is not the same assessment, and comparing their
        /// Actuals would be meaningless.</item>
        /// <item><b>The design airflows that reached TAS are the design's own.</b> Cross-checked against
        /// the prepared model's own terminals, per room and per direction. This is where
        /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>
        /// is actually enforced: a requirement or a selected unit's capacity substituted anywhere upstream
        /// does not match the terminals and refuses here.</item>
        /// <item><b>The transfer topology is the design's own.</b> Every authored space-to-space design
        /// air movement between two bound rooms appears exactly once as a transfer leg carrying its
        /// flow, and no transfer leg exists that the design does not state.</item>
        /// </list>
        /// </summary>
        /// <param name="adjacencyCluster_Prepared">The prepared design - the airflow and transfer authority.</param>
        /// <param name="partOIteration3SystemScope">The scope taken, including which systems were retained.</param>
        /// <param name="systemVentilationRoute">Candidate B's completed route.</param>
        /// <param name="noIzamThermalSource">Candidate B's thermal source, for the sweep evidence.</param>
        /// <param name="partOIteration3Assessment_A">Reference A's assessment and its captured series.</param>
        /// <param name="partOIteration3Assessment_B">Candidate B's, likewise.</param>
        /// <param name="dictionary_Room">The dwelling scope, indexed by space guid.</param>
        /// <param name="rooms_Comparable">The rooms the comparison may use. Empty on any refusal.</param>
        /// <param name="criteria">Their TM59 criteria, both sides, carried verbatim. Empty on any refusal.</param>
        /// <param name="notes">What reconciled.</param>
        internal static List<string> PartOIteration3ReconciliationRefusals(
            AdjacencyCluster adjacencyCluster_Prepared,
            PartOIteration3SystemScope partOIteration3SystemScope,
            SystemVentilationRoute systemVentilationRoute,
            NoIzamThermalSource noIzamThermalSource,
            PartOIteration3Assessment partOIteration3Assessment_A,
            PartOIteration3Assessment partOIteration3Assessment_B,
            Dictionary<Guid, PartOIteration3Room> dictionary_Room,
            out List<PartOIteration3Room> rooms_Comparable,
            out List<PartOIteration3CriterionComparison> criteria,
            out List<string> notes)
        {
            List<string> result = [];

            rooms_Comparable = [];
            criteria = [];
            notes = [];

            if (adjacencyCluster_Prepared is null || partOIteration3SystemScope is null || systemVentilationRoute is null || noIzamThermalSource is null || partOIteration3Assessment_A is null || partOIteration3Assessment_B is null)
            {
                result.Add("The Iteration 3 reconciliation was asked to compare something that is not there, so nothing was reconciled.");

                return result;
            }

            //-------------------------------------------------------------------------------------------
            //1. The sweep
            //-------------------------------------------------------------------------------------------
            if (!noIzamThermalSource.RemovedIZAMs)
            {
                result.Add("Candidate B's thermal source does not record that inherited IZAMs were removed, so the building's own mechanical ventilation may be modelled alongside the explicit TAS Systems network.");
            }

            if (!noIzamThermalSource.RemovedMechanicalVentilationGains)
            {
                result.Add("Candidate B's thermal source does not record that the mechanical ventilation gain was neutralised, so the building's own mechanical ventilation may be modelled alongside the explicit TAS Systems network.");
            }

            //-------------------------------------------------------------------------------------------
            //2. The three sets
            //-------------------------------------------------------------------------------------------
            Dictionary<Guid, SystemVentilationBinding> dictionary_Binding = [];

            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationRoute.Bindings)
            {
                if (!dictionary_Binding.ContainsKey(systemVentilationBinding.Guid_Space))
                {
                    dictionary_Binding[systemVentilationBinding.Guid_Space] = systemVentilationBinding;

                    continue;
                }

                result.Add(string.Format("Room '{0}' ({1}) is bound to the explicit ventilation route twice.", Name(dictionary_Room, systemVentilationBinding.Guid_Space), systemVentilationBinding.Guid_Space));
            }

            foreach (Guid guid_Space in dictionary_Binding.Keys)
            {
                if (!dictionary_Room.ContainsKey(guid_Space))
                {
                    result.Add(string.Format(
                        "Room {0} is bound to the explicit ventilation route but is not in the Approved Document O dwelling scope this run was prepared over, so Candidate B ventilates a room Reference A never assessed.",
                        guid_Space));
                }
            }

            //-------------------------------------------------------------------------------------------
            //3. Every retained system's duty is reinstated - the second half of the comparability gate
            //-------------------------------------------------------------------------------------------
            HashSet<Guid> guids_Retained = [.. partOIteration3SystemScope.Guids_Retained];

            foreach (VentilationSystem ventilationSystem in adjacencyCluster_Prepared.GetObjects<VentilationSystem>() ?? [])
            {
                if (ventilationSystem is null || !guids_Retained.Contains(ventilationSystem.Guid))
                {
                    continue;
                }

                foreach (VentilationTerminal ventilationTerminal in adjacencyCluster_Prepared.VentilationTerminals(ventilationSystem) ?? [])
                {
                    double? designFlowRate_Lps = ventilationTerminal?.DesignFlowRate_Lps;

                    if (!designFlowRate_Lps.HasValue || double.IsNaN(designFlowRate_Lps.Value) || double.IsInfinity(designFlowRate_Lps.Value) || designFlowRate_Lps.Value == 0)
                    {
                        continue;
                    }

                    foreach (Space space in adjacencyCluster_Prepared.GetRelatedObjects<Space>(ventilationTerminal) ?? [])
                    {
                        if (space is not null && !dictionary_Binding.ContainsKey(space.Guid))
                        {
                            result.Add(string.Format(
                                "The design under assessment states a {0} duty of {1:0.###} l/s in '{2}' ({3}) through system '{4}', and Candidate B's explicit route does not serve that room. "
                                + "Candidate B's no-IZAM source removed that ventilation and nothing reinstated it, so the two cases differ by more than the route being compared.",
                                ventilationTerminal.FlowClassification,
                                designFlowRate_Lps.Value,
                                space.Name,
                                space.Guid,
                                ventilationSystem.Name));
                        }
                    }
                }
            }

            //-------------------------------------------------------------------------------------------
            //4-6. Both assessments, room by room
            //-------------------------------------------------------------------------------------------
            Dictionary<Guid, Dictionary<string, PartOTM59SpaceResult>> dictionary_A = Criteria(partOIteration3Assessment_A, result, "Reference A");
            Dictionary<Guid, Dictionary<string, PartOTM59SpaceResult>> dictionary_B = Criteria(partOIteration3Assessment_B, result, "Candidate B");

            HashSet<Guid> guids_InformationOnly_A = [.. partOIteration3Assessment_A.SpaceGuids_InformationOnly];
            HashSet<Guid> guids_InformationOnly_B = [.. partOIteration3Assessment_B.SpaceGuids_InformationOnly];

            List<Guid> guids_Bound = [.. dictionary_Binding.Keys];
            guids_Bound.Sort();

            int count_InformationOnly = 0;

            foreach (Guid guid_Space in guids_Bound)
            {
                string name = Name(dictionary_Room, guid_Space);

                bool assessed_A = dictionary_A.TryGetValue(guid_Space, out Dictionary<string, PartOTM59SpaceResult> criteria_A);
                bool assessed_B = dictionary_B.TryGetValue(guid_Space, out Dictionary<string, PartOTM59SpaceResult> criteria_B);

                //A served room that carries no occupied-space criterion in EITHER case - a bathroom or ensuite, whose
                //>28 C row is supplementary information only. That is the same classification on both sides, not a
                //missing result, provided BOTH reports show it as information only; its temperatures are still
                //compared, with no criterion to compare.
                if (!assessed_A && !assessed_B && guids_InformationOnly_A.Contains(guid_Space) && guids_InformationOnly_B.Contains(guid_Space))
                {
                    bool series_InformationOnly_A = partOIteration3Assessment_A.ResultantTemperature(guid_Space) is not null;
                    bool series_InformationOnly_B = partOIteration3Assessment_B.ResultantTemperature(guid_Space) is not null;

                    if (!series_InformationOnly_A || !series_InformationOnly_B)
                    {
                        result.Add(string.Format(
                            "Room '{0}' ({1}) produced no captured resultant temperature series in {2}, so its two cases cannot be compared.",
                            name,
                            guid_Space,
                            series_InformationOnly_A ? "Candidate B" : series_InformationOnly_B ? "Reference A" : "either case"));

                        continue;
                    }

                    if (dictionary_Room.TryGetValue(guid_Space, out PartOIteration3Room partOIteration3Room_InformationOnly))
                    {
                        rooms_Comparable.Add(partOIteration3Room_InformationOnly);
                        count_InformationOnly++;
                    }

                    continue;
                }

                if (!assessed_A || !assessed_B)
                {
                    result.Add(assessed_A || assessed_B
                        ? string.Format(
                            "Room '{0}' ({1}) is served by the explicit ventilation route but produced a TM59 result in {2} only, so the two cases were not assessed over the same rooms. In {3} it is {4}.",
                            name,
                            guid_Space,
                            assessed_A ? "Reference A" : "Candidate B",
                            assessed_A ? "Candidate B" : "Reference A",
                            Classification(false, assessed_A ? guids_InformationOnly_B : guids_InformationOnly_A, guid_Space))
                        : string.Format(
                            "Room '{0}' ({1}) is served by the explicit ventilation route but produced no TM59 occupied-space result in either case, and the two cases do not both report it as supplementary information only (Reference A: {2}; Candidate B: {3}), so the two cases were not assessed over the same rooms.",
                            name,
                            guid_Space,
                            Classification(false, guids_InformationOnly_A, guid_Space),
                            Classification(false, guids_InformationOnly_B, guid_Space)));

                    continue;
                }

                bool series_A = partOIteration3Assessment_A.ResultantTemperature(guid_Space) is not null;
                bool series_B = partOIteration3Assessment_B.ResultantTemperature(guid_Space) is not null;

                if (!series_A || !series_B)
                {
                    result.Add(string.Format(
                        "Room '{0}' ({1}) produced no captured resultant temperature series in {2}, so its two cases cannot be compared.",
                        name,
                        guid_Space,
                        series_A ? "Candidate B" : series_B ? "Reference A" : "either case"));

                    continue;
                }

                bool matched = true;

                foreach (KeyValuePair<string, PartOTM59SpaceResult> keyValuePair in criteria_A)
                {
                    if (!criteria_B.ContainsKey(keyValuePair.Key))
                    {
                        result.Add(string.Format("Room '{0}' ({1}) was assessed against TM59 criterion '{2}' in Reference A and not in Candidate B.", name, guid_Space, keyValuePair.Key));

                        matched = false;
                    }
                }

                foreach (KeyValuePair<string, PartOTM59SpaceResult> keyValuePair in criteria_B)
                {
                    if (!criteria_A.ContainsKey(keyValuePair.Key))
                    {
                        result.Add(string.Format("Room '{0}' ({1}) was assessed against TM59 criterion '{2}' in Candidate B and not in Reference A.", name, guid_Space, keyValuePair.Key));

                        matched = false;
                    }
                }

                if (!matched)
                {
                    continue;
                }

                if (!dictionary_Room.TryGetValue(guid_Space, out PartOIteration3Room partOIteration3Room))
                {
                    //Already refused above; skip rather than report the same room twice.
                    continue;
                }

                rooms_Comparable.Add(partOIteration3Room);

                foreach (KeyValuePair<string, PartOTM59SpaceResult> keyValuePair in criteria_A)
                {
                    PartOTM59SpaceResult partOTM59SpaceResult_A = keyValuePair.Value;
                    PartOTM59SpaceResult partOTM59SpaceResult_B = criteria_B[keyValuePair.Key];

                    criteria.Add(new PartOIteration3CriterionComparison(
                        guid_Space,
                        partOIteration3Room.Name_Space,
                        partOIteration3Room.Guid_Dwelling,
                        partOIteration3Room.Name_Dwelling,
                        keyValuePair.Key,
                        partOTM59SpaceResult_A.Mechanical,
                        partOTM59SpaceResult_A.Actual,
                        partOTM59SpaceResult_A.Limit,
                        partOTM59SpaceResult_A.ComplianceStatus,
                        partOTM59SpaceResult_B.Actual,
                        partOTM59SpaceResult_B.Limit,
                        partOTM59SpaceResult_B.ComplianceStatus));
                }
            }

            //-------------------------------------------------------------------------------------------
            //7. The design airflows that reached TAS
            //-------------------------------------------------------------------------------------------
            foreach (Guid guid_Space in guids_Bound)
            {
                Space space = adjacencyCluster_Prepared.GetObject<Space>(guid_Space);

                if (space is null)
                {
                    result.Add(string.Format("Room {0} is bound to the explicit ventilation route but is not on the prepared design, so its design airflow could not be cross-checked.", guid_Space));

                    continue;
                }

                List<VentilationTerminal> ventilationTerminals = adjacencyCluster_Prepared.VentilationTerminals(space) ?? [];

                Compare(result, dictionary_Room, guid_Space, FlowClassification.Supply, Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Supply), dictionary_Binding[guid_Space].DesignFlowRate_Supply_Lps);
                Compare(result, dictionary_Room, guid_Space, FlowClassification.Extract, Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Extract), dictionary_Binding[guid_Space].DesignFlowRate_Extract_Lps);
            }

            //-------------------------------------------------------------------------------------------
            //8. The transfer topology
            //-------------------------------------------------------------------------------------------
            Dictionary<(Guid, Guid), Analytical.Query.DesignTransferAirMovement> dictionary_Transfer = adjacencyCluster_Prepared.DesignTransferSpaceAirMovements();

            Dictionary<Guid, Guid> dictionary_Space_By_SystemSpace = [];
            foreach (KeyValuePair<Guid, SystemVentilationBinding> keyValuePair in dictionary_Binding)
            {
                dictionary_Space_By_SystemSpace[keyValuePair.Value.Guid_SystemSpace] = keyValuePair.Key;
            }

            HashSet<(Guid, Guid)> pairs_Route = [];

            foreach (SystemVentilationConnectionBinding systemVentilationConnectionBinding in systemVentilationRoute.ConnectionBindings)
            {
                if (systemVentilationConnectionBinding.ConnectionType != SystemVentilationConnectionType.Transfer)
                {
                    continue;
                }

                if (!dictionary_Space_By_SystemSpace.TryGetValue(systemVentilationConnectionBinding.Guid_SystemSpace_From, out Guid guid_From)
                    || !dictionary_Space_By_SystemSpace.TryGetValue(systemVentilationConnectionBinding.Guid_SystemSpace_To, out Guid guid_To))
                {
                    result.Add(string.Format(
                        "A transfer leg of the explicit ventilation route connects system spaces {0} and {1}, and at least one of them is not a room this route bound - so the transfer topology cannot be reconciled against the design.",
                        systemVentilationConnectionBinding.Guid_SystemSpace_From,
                        systemVentilationConnectionBinding.Guid_SystemSpace_To));

                    continue;
                }

                double? flowRate_Design_Lps = adjacencyCluster_Prepared.DesignTransferFlowRate_Lps(guid_From, guid_To, out Guid guid_From_Design, out Guid guid_To_Design, dictionary_Transfer);

                if (!flowRate_Design_Lps.HasValue)
                {
                    result.Add(string.Format(
                        "The explicit ventilation route transfers {0:0.###} l/s from '{1}' ({2}) to '{3}' ({4}), and the design states no transfer air between those rooms.",
                        systemVentilationConnectionBinding.DesignFlowRate_Lps,
                        Name(dictionary_Room, guid_From),
                        guid_From,
                        Name(dictionary_Room, guid_To),
                        guid_To));

                    continue;
                }

                if (!pairs_Route.Add(Key(guid_From, guid_To)))
                {
                    result.Add(string.Format(
                        "The explicit ventilation route states two transfer legs between '{0}' ({1}) and '{2}' ({3}), and the design states one.",
                        Name(dictionary_Room, guid_From),
                        guid_From,
                        Name(dictionary_Room, guid_To),
                        guid_To));

                    continue;
                }

                if (guid_From_Design != guid_From || guid_To_Design != guid_To)
                {
                    result.Add(string.Format(
                        "The explicit ventilation route transfers air from '{0}' ({1}) to '{2}' ({3}), and the design states the opposite direction.",
                        Name(dictionary_Room, guid_From),
                        guid_From,
                        Name(dictionary_Room, guid_To),
                        guid_To));

                    continue;
                }

                if (Math.Abs(flowRate_Design_Lps.Value - systemVentilationConnectionBinding.DesignFlowRate_Lps) > Modify.Tolerance_DesignAirFlow_Lps)
                {
                    result.Add(string.Format(
                        "The transfer from '{0}' ({1}) to '{2}' ({3}) reached TAS as {4:0.######} l/s and the design states {5:0.######} l/s.",
                        Name(dictionary_Room, guid_From),
                        guid_From,
                        Name(dictionary_Room, guid_To),
                        guid_To,
                        systemVentilationConnectionBinding.DesignFlowRate_Lps,
                        flowRate_Design_Lps.Value));
                }
            }

            foreach (KeyValuePair<(Guid, Guid), Analytical.Query.DesignTransferAirMovement> keyValuePair in dictionary_Transfer)
            {
                Guid guid_From = keyValuePair.Value.FromGuid;
                Guid guid_To = keyValuePair.Value.ToGuid;

                //Only transfers BETWEEN two bound rooms. A design transfer touching a room the route does
                //not serve is not this route's to carry, and the duty checks above are what establish that
                //no served room was left unbound.
                if (!dictionary_Binding.ContainsKey(guid_From) || !dictionary_Binding.ContainsKey(guid_To))
                {
                    continue;
                }

                if (!pairs_Route.Contains(Key(guid_From, guid_To)))
                {
                    result.Add(string.Format(
                        "The design states transfer air from '{0}' ({1}) to '{2}' ({3}), and the explicit ventilation route carries no leg between those rooms - so Candidate B moves air differently from the design Reference A was simulated with.",
                        Name(dictionary_Room, guid_From),
                        guid_From,
                        Name(dictionary_Room, guid_To),
                        guid_To));
                }
            }

            if (result.Count != 0)
            {
                rooms_Comparable.Clear();
                criteria.Clear();

                return result;
            }

            notes.Add(string.Format(
                "{0} room(s) reconcile by identity: the same rooms, the same TM59 criteria, design airflows equal to the prepared design's own terminals and {1} transfer leg(s) matching the design's authored air movements.",
                rooms_Comparable.Count,
                pairs_Route.Count));

            if (count_InformationOnly != 0)
            {
                notes.Add(string.Format(
                    "{0} of them carry no occupied-space criterion in either case - both reports show them only as supplementary >28 C information - so their temperatures are compared and no TM59 criterion is.",
                    count_InformationOnly));
            }

            return result;
        }

        /// <summary>How one case classified a room that has no occupied-space criterion there.</summary>
        private static string Classification(bool assessed, HashSet<Guid> guids_InformationOnly, Guid guid_Space)
        {
            return assessed
                ? "assessed against a TM59 occupied-space criterion"
                : guids_InformationOnly.Contains(guid_Space)
                    ? "reported as supplementary >28 C information only"
                    : "not assessed at all";
        }

        /// <summary>One case's criteria, indexed by room then by check name. A room assessed twice on one criterion refuses.</summary>
        private static Dictionary<Guid, Dictionary<string, PartOTM59SpaceResult>> Criteria(PartOIteration3Assessment partOIteration3Assessment, List<string> refusals, string description)
        {
            Dictionary<Guid, Dictionary<string, PartOTM59SpaceResult>> result = [];

            foreach (PartOTM59SpaceResult partOTM59SpaceResult in partOIteration3Assessment.SpaceResults)
            {
                if (!result.TryGetValue(partOTM59SpaceResult.SpaceGuid_Design, out Dictionary<string, PartOTM59SpaceResult> dictionary))
                {
                    dictionary = new Dictionary<string, PartOTM59SpaceResult>(StringComparer.Ordinal);

                    result[partOTM59SpaceResult.SpaceGuid_Design] = dictionary;
                }

                if (dictionary.ContainsKey(partOTM59SpaceResult.Check ?? string.Empty))
                {
                    refusals.Add(string.Format(
                        "{0} reported TM59 criterion '{1}' twice for room '{2}' ({3}), so which of the two is that room's verdict is not knowable.",
                        description,
                        partOTM59SpaceResult.Check,
                        partOTM59SpaceResult.SpaceName,
                        partOTM59SpaceResult.SpaceGuid_Design));

                    continue;
                }

                dictionary[partOTM59SpaceResult.Check ?? string.Empty] = partOTM59SpaceResult;
            }

            return result;
        }

        /// <summary>
        /// One room and direction: what the prepared design's own terminals state against what reached
        /// TAS.
        /// <para>
        /// A room with no terminal of that direction has no duty, and the route must carry none either -
        /// null and null agree. A duty on one side and nothing on the other is a refusal, because a room
        /// that is ventilated in the design and not in the simulation, or the reverse, is not the same
        /// room.
        /// </para>
        /// </summary>
        private static void Compare(List<string> refusals, Dictionary<Guid, PartOIteration3Room> dictionary_Room, Guid guid_Space, FlowClassification flowClassification, double? design_Lps, double? route_Lps)
        {
            if (!design_Lps.HasValue && !route_Lps.HasValue)
            {
                return;
            }

            if (!design_Lps.HasValue || !route_Lps.HasValue)
            {
                refusals.Add(string.Format(
                    "Room '{0}' ({1}): the prepared design states {2} design {3} airflow and the explicit ventilation route carries {4}.",
                    Name(dictionary_Room, guid_Space),
                    guid_Space,
                    design_Lps.HasValue ? string.Format("{0:0.######} l/s of", design_Lps.Value) : "no",
                    flowClassification,
                    route_Lps.HasValue ? string.Format("{0:0.######} l/s", route_Lps.Value) : "none"));

                return;
            }

            if (Math.Abs(design_Lps.Value - route_Lps.Value) > Modify.Tolerance_DesignAirFlow_Lps)
            {
                refusals.Add(string.Format(
                    "Room '{0}' ({1}): the design {2} airflow that reached TAS is {3:0.######} l/s and the prepared design's own terminals state {4:0.######} l/s. "
                    + "Design airflow is the only airflow authority on this route - an Approved Document F requirement and a selected unit's capacity are different numbers and neither may be substituted for it.",
                    Name(dictionary_Room, guid_Space),
                    guid_Space,
                    flowClassification,
                    route_Lps.Value,
                    design_Lps.Value));
            }
        }

        /// <summary>A space pair keyed with the smaller guid first, exactly as the design transfer index keys it.</summary>
        private static (Guid, Guid) Key(Guid guid_1, Guid guid_2)
        {
            return guid_1.CompareTo(guid_2) <= 0 ? (guid_1, guid_2) : (guid_2, guid_1);
        }
    }
}
