// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.TPD;
using SAM.Analytical.UI;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Everything that has to be true before two sets of temperatures may be called a comparison of
    /// two routes over one design.</b>
    ///
    /// <para><b>The invariant these tests exist for</b></para>
    /// <para>
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>.
    /// Every one of those is a number somebody could plausibly have substituted upstream, and every one
    /// of them would produce a complete, finite, entirely believable set of results. The only thing that
    /// catches it is cross-checking what reached TAS against the prepared design's own terminals - which
    /// is what these cases do, with a requirement and a capacity standing in for the design airflow.
    /// </para>
    /// </summary>
    public class PartOIteration3ReconciliationTests : IDisposable
    {
        private readonly string directory = PartOIteration3Fixture.Directory_Temp();

        private AdjacencyCluster adjacencyCluster;

        private List<Guid> guids_VentilationSystem;

        private List<Zone> zones;

        private List<Guid> guids_Space_Dwelling;

        private Dictionary<Guid, PartOIteration3Room> dictionary_Room;

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private PartOIteration3SystemScope Scope()
        {
            adjacencyCluster = PartOIteration3Fixture.Design(out guids_VentilationSystem, out zones);

            guids_Space_Dwelling = [];
            foreach (Zone zone in zones)
            {
                foreach (Space space in PartOIteration3Fixture.Spaces(adjacencyCluster, zone))
                {
                    guids_Space_Dwelling.Add(space.Guid);
                }
            }

            dictionary_Room = Query.PartOIteration3Rooms(adjacencyCluster, zones);

            return Query.PartOIteration3SystemScope(adjacencyCluster, guids_VentilationSystem, guids_Space_Dwelling);
        }

        /// <summary>A complete route over the dwelling, with whatever duties and transfers the caller wants.</summary>
        private SystemVentilationRoute Route(
            Func<Space, FlowClassification, double?> func_Duty = null,
            Func<Guid, Guid, double, double> func_Transfer = null,
            IEnumerable<Guid> guids_Space_Override = null)
        {
            NoIzamThermalSource noIzamThermalSource = PartOIteration3Fixture.ThermalSource(directory, guids_Space_Dwelling);

            List<SystemVentilationBinding> bindings = [];
            List<SystemVentilationConnectionBinding> connectionBindings = [];

            Guid guid_AirSystem = Guid.NewGuid();

            Dictionary<Guid, Guid> dictionary_SystemSpace = [];

            List<Guid> guids = guids_Space_Override is null ? guids_Space_Dwelling : [.. guids_Space_Override];

            foreach (Guid guid in guids)
            {
                Space space = adjacencyCluster.GetObject<Space>(guid);

                List<VentilationTerminal> ventilationTerminals = space is null ? [] : adjacencyCluster.VentilationTerminals(space) ?? [];

                double? supply = func_Duty is null ? Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Supply) : func_Duty(space, FlowClassification.Supply);
                double? extract = func_Duty is null ? Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Extract) : func_Duty(space, FlowClassification.Extract);

                bindings.Add(PartOIteration3Fixture.Binding(guid, guid_AirSystem, supply, extract, out Guid guid_SystemSpace));

                dictionary_SystemSpace[guid] = guid_SystemSpace;
            }

            foreach (KeyValuePair<(Guid, Guid), Analytical.Query.DesignTransferAirMovement> keyValuePair in adjacencyCluster.DesignTransferSpaceAirMovements())
            {
                Guid guid_From = keyValuePair.Value.FromGuid;
                Guid guid_To = keyValuePair.Value.ToGuid;

                if (!dictionary_SystemSpace.TryGetValue(guid_From, out Guid guid_SystemSpace_From) || !dictionary_SystemSpace.TryGetValue(guid_To, out Guid guid_SystemSpace_To))
                {
                    continue;
                }

                double flowRate = adjacencyCluster.DesignTransferFlowRate_Lps(guid_From, guid_To, out Guid _, out Guid _).Value;

                connectionBindings.Add(new SystemVentilationConnectionBinding(
                    SystemVentilationConnectionType.Transfer,
                    keyValuePair.Value.SpaceAirMovement.Guid,
                    Guid.NewGuid(),
                    guid_AirSystem,
                    guid_SystemSpace_From,
                    guid_SystemSpace_To,
                    func_Transfer is null ? flowRate : func_Transfer(guid_From, guid_To, flowRate),
                    "damper"));
            }

            string path_TPD = Path.Combine(directory, "Flat-It3B.tpd");

            return new SystemVentilationRoute(
                noIzamThermalSource,
                path_TPD,
                PartOIteration3Fixture.Evidence(path_TPD, path_TPD),
                bindings,
                connectionBindings,
                PartOIteration3Fixture.ZoneTemperatures(bindings, 0, 23),
                null,
                null);
        }

        private static PartOIteration3Assessment Assessment(IEnumerable<Guid> guids, string check = "TM59 Criterion A", TM59ComplianceStatus status = TM59ComplianceStatus.Pass)
        {
            List<PartOTM59SpaceResult> spaceResults = [];
            Dictionary<Guid, double[]> resultantTemperatures = [];

            foreach (Guid guid in guids)
            {
                spaceResults.Add(new PartOTM59SpaceResult(guid, "room", check, 10, 32, status, true));

                resultantTemperatures[guid] = new double[24];
            }

            return new PartOIteration3Assessment(true, null, status, spaceResults, null, null, null, resultantTemperatures, "report", null, spaceResults.Count);
        }

        /// <summary>
        /// An assessment whose <paramref name="guids_InformationOnly"/> rooms carry no occupied-space criterion and are
        /// reported only as supplementary &gt;28 C information - a bathroom or ensuite - with a captured series unless
        /// <paramref name="series_InformationOnly"/> is false.
        /// </summary>
        private static PartOIteration3Assessment Assessment(IEnumerable<Guid> guids, IEnumerable<Guid> guids_InformationOnly, bool series_InformationOnly = true)
        {
            HashSet<Guid> informationOnly = [.. guids_InformationOnly];

            List<PartOTM59SpaceResult> spaceResults = [];
            Dictionary<Guid, double[]> resultantTemperatures = [];

            foreach (Guid guid in guids)
            {
                if (informationOnly.Contains(guid))
                {
                    if (series_InformationOnly)
                    {
                        resultantTemperatures[guid] = new double[24];
                    }

                    continue;
                }

                spaceResults.Add(new PartOTM59SpaceResult(guid, "room", "TM59 Criterion A", 10, 32, TM59ComplianceStatus.Pass, true));

                resultantTemperatures[guid] = new double[24];
            }

            return new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Pass, spaceResults, null, null, null, resultantTemperatures, "report", null, spaceResults.Count, null, informationOnly);
        }

        private List<string> Reconcile(PartOIteration3SystemScope partOIteration3SystemScope, SystemVentilationRoute systemVentilationRoute, PartOIteration3Assessment assessment_A, PartOIteration3Assessment assessment_B, out List<PartOIteration3Room> rooms)
        {
            return Query.PartOIteration3ReconciliationRefusals(
                adjacencyCluster,
                partOIteration3SystemScope,
                systemVentilationRoute,
                systemVentilationRoute.NoIzamThermalSource,
                assessment_A,
                assessment_B,
                dictionary_Room,
                out rooms,
                out List<PartOIteration3CriterionComparison> _,
                out List<string> _);
        }

        [Fact]
        public void The_canonical_case_reconciles()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids_Space_Dwelling), out List<PartOIteration3Room> rooms);

            Assert.Empty(refusals);
            Assert.Equal(guids_Space_Dwelling.Count, rooms.Count);
        }

        //-------------------------------------------------------------------------------------------------
        //The airflow invariant
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// An Approved Document F <b>requirement</b> substituted for the design airflow. It is a real
        /// number of the right order for the room, so nothing but this cross-check would notice.
        /// </summary>
        [Fact]
        public void An_Approved_Document_F_requirement_substituted_for_the_design_airflow_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            //Part F Table 1.2 continuous supply for a one-bedroom dwelling, used where the design's own
            //terminal states 13 l/s.
            SystemVentilationRoute systemVentilationRoute = Route((space, flowClassification) =>
            {
                List<VentilationTerminal> ventilationTerminals = adjacencyCluster.VentilationTerminals(space) ?? [];

                double? result = Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, flowClassification);

                return result.HasValue && flowClassification == FlowClassification.Supply ? 19.0 : result;
            });

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids_Space_Dwelling), out List<PartOIteration3Room> rooms);

            Assert.NotEmpty(refusals);
            Assert.Empty(rooms);
            Assert.Contains(refusals, x => x.Contains("19") && x.Contains("13") && x.Contains("Design airflow is the only airflow authority"));
        }

        /// <summary>A selected unit's rated capacity substituted for the design airflow. Same rule.</summary>
        [Fact]
        public void A_selected_equipment_capacity_substituted_for_the_design_airflow_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route((space, flowClassification) =>
            {
                List<VentilationTerminal> ventilationTerminals = adjacencyCluster.VentilationTerminals(space) ?? [];

                double? result = Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, flowClassification);

                //An MRXBOX's rated maximum, which is a property of the product and not of the room.
                return result.HasValue ? 51.0 : result;
            });

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids_Space_Dwelling), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains("51") && x.Contains("Design airflow is the only airflow authority"));
        }

        [Fact]
        public void A_room_ventilated_in_the_design_and_not_on_the_route_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route((space, flowClassification) => null);

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids_Space_Dwelling), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains("the explicit ventilation route carries none"));
        }

        //-------------------------------------------------------------------------------------------------
        //Bindings
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A room the design under assessment ventilates that Candidate B does not serve: its
        /// ventilation was removed by the no-IZAM sweep and nothing reinstated it - the other half of the
        /// whole-thermal-domain comparability gate.
        /// </summary>
        [Fact]
        public void A_served_room_that_the_route_did_not_bind_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            List<Guid> guids = [.. guids_Space_Dwelling];

            //Drop the first room that actually carries a duty.
            Guid guid_Dropped = Guid.Empty;
            foreach (Guid guid in guids)
            {
                Space space = adjacencyCluster.GetObject<Space>(guid);

                if ((adjacencyCluster.VentilationTerminals(space) ?? []).Count != 0)
                {
                    guid_Dropped = guid;

                    break;
                }
            }

            guids.Remove(guid_Dropped);

            SystemVentilationRoute systemVentilationRoute = Route(null, null, guids);

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains("Candidate B's explicit route does not serve that room") && x.Contains(guid_Dropped.ToString()));
        }

        /// <summary>A binding for a room outside the assessed dwelling scope - Candidate B ventilating something Reference A never assessed.</summary>
        [Fact]
        public void A_binding_outside_the_dwelling_scope_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            Space space_Corridor = PartOIteration3Fixture.Space(adjacencyCluster, "Corridor");

            List<Guid> guids = [.. guids_Space_Dwelling, space_Corridor.Guid];

            SystemVentilationRoute systemVentilationRoute = Route(null, null, guids);

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids), Assessment(guids), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains(space_Corridor.Guid.ToString()) && x.Contains("not in the Approved Document O dwelling scope"));
        }

        [Fact]
        public void A_room_assessed_in_one_case_only_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            List<Guid> guids_B = [.. guids_Space_Dwelling];
            guids_B.RemoveAt(0);

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids_B), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains("produced a TM59 result in Reference A only"));
        }

        //-------------------------------------------------------------------------------------------------
        //Supplementary (information-only) rooms - the 2026-09-29 real project: a bathroom and two ensuites
        //served by the route, whose >28 C rows both reports show as information only. The reconciliation
        //refused them as "produced a TM59 result in Candidate B only", although NEITHER case had given them an
        //occupied-space criterion - the classification was the same on both sides.
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_served_room_that_both_cases_report_as_information_only_reconciles()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            Guid guid_WetRoom = guids_Space_Dwelling[0];

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling, [guid_WetRoom]), Assessment(guids_Space_Dwelling, [guid_WetRoom]), out List<PartOIteration3Room> rooms);

            Assert.Empty(refusals);
            Assert.Equal(guids_Space_Dwelling.Count, rooms.Count);
            Assert.Contains(rooms, x => x.Guid_Space == guid_WetRoom);
        }

        [Fact]
        public void A_served_room_with_no_result_in_either_case_is_not_blamed_on_Candidate_B()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            List<Guid> guids_Assessed = [.. guids_Space_Dwelling];
            guids_Assessed.RemoveAt(0);

            //Neither case assessed it and neither shows it as information only: still refused, but described as it is.
            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Assessed), Assessment(guids_Assessed), out List<PartOIteration3Room> _);

            string refusal = Assert.Single(refusals, x => x.Contains(guids_Space_Dwelling[0].ToString()));
            Assert.Contains("no TM59 occupied-space result in either case", refusal);
            Assert.Contains("Reference A: not assessed at all; Candidate B: not assessed at all", refusal);
            Assert.DoesNotContain("in Candidate B only", refusal);
        }

        [Fact]
        public void A_room_information_only_in_one_case_and_absent_in_the_other_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            Guid guid_WetRoom = guids_Space_Dwelling[0];

            List<Guid> guids_B = [.. guids_Space_Dwelling];
            guids_B.RemoveAt(0);

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling, [guid_WetRoom]), Assessment(guids_B), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains(guid_WetRoom.ToString()) && x.Contains("Reference A: reported as supplementary >28 C information only; Candidate B: not assessed at all"));
        }

        [Fact]
        public void A_room_information_only_in_one_case_and_assessed_in_the_other_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            Guid guid_WetRoom = guids_Space_Dwelling[0];

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling, [guid_WetRoom]), Assessment(guids_Space_Dwelling), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains(guid_WetRoom.ToString()) && x.Contains("produced a TM59 result in Candidate B only") && x.Contains("In Reference A it is reported as supplementary >28 C information only"));
        }

        [Fact]
        public void An_information_only_room_without_a_series_on_one_side_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            Guid guid_WetRoom = guids_Space_Dwelling[0];

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling, [guid_WetRoom]), Assessment(guids_Space_Dwelling, [guid_WetRoom], false), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains(guid_WetRoom.ToString()) && x.Contains("no captured resultant temperature series in Candidate B"));
        }

        [Fact]
        public void A_room_assessed_against_different_criteria_in_the_two_cases_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            List<string> refusals = Reconcile(
                partOIteration3SystemScope,
                systemVentilationRoute,
                Assessment(guids_Space_Dwelling, "TM59 Criterion A"),
                Assessment(guids_Space_Dwelling, "TM59 Criterion B"),
                out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains("'TM59 Criterion A' in Reference A and not in Candidate B"));
            Assert.Contains(refusals, x => x.Contains("'TM59 Criterion B' in Candidate B and not in Reference A"));
        }

        //-------------------------------------------------------------------------------------------------
        //Transfer topology
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_transfer_leg_carrying_a_different_flow_from_the_design_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route(null, (guid_From, guid_To, flowRate) => flowRate + 1.0);

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids_Space_Dwelling), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains("reached TAS as") && x.Contains("the design states"));
        }

        /// <summary>
        /// A design transfer the route carries no leg for: Candidate B moves air differently from the
        /// design Reference A was simulated with.
        /// </summary>
        [Fact]
        public void A_design_transfer_the_route_does_not_carry_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            NoIzamThermalSource noIzamThermalSource = PartOIteration3Fixture.ThermalSource(directory, guids_Space_Dwelling);

            List<SystemVentilationBinding> bindings = [];

            Guid guid_AirSystem = Guid.NewGuid();

            foreach (Guid guid in guids_Space_Dwelling)
            {
                Space space = adjacencyCluster.GetObject<Space>(guid);

                List<VentilationTerminal> ventilationTerminals = adjacencyCluster.VentilationTerminals(space) ?? [];

                bindings.Add(PartOIteration3Fixture.Binding(
                    guid,
                    guid_AirSystem,
                    Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Supply),
                    Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Extract),
                    out Guid _));
            }

            string path_TPD = Path.Combine(directory, "Flat-It3B.tpd");

            //No connection bindings at all: every authored transfer is missing.
            SystemVentilationRoute systemVentilationRoute = new(
                noIzamThermalSource,
                path_TPD,
                PartOIteration3Fixture.Evidence(path_TPD, path_TPD),
                bindings,
                [],
                PartOIteration3Fixture.ZoneTemperatures(bindings, 0, 23),
                null,
                null);

            List<string> refusals = Reconcile(partOIteration3SystemScope, systemVentilationRoute, Assessment(guids_Space_Dwelling), Assessment(guids_Space_Dwelling), out List<PartOIteration3Room> _);

            Assert.Contains(refusals, x => x.Contains("the explicit ventilation route carries no leg between those rooms"));
        }

        //-------------------------------------------------------------------------------------------------
        //The sweep
        //-------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(false, true, "inherited IZAMs were removed")]
        [InlineData(true, false, "mechanical ventilation gain was neutralised")]
        public void A_thermal_source_that_does_not_evidence_both_cleanups_refuses(bool removedIZAMs, bool removedGains, string expected)
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            NoIzamThermalSource noIzamThermalSource = PartOIteration3Fixture.ThermalSource(directory, guids_Space_Dwelling, removedIZAMs, removedGains);

            List<string> refusals = Query.PartOIteration3ReconciliationRefusals(
                adjacencyCluster,
                partOIteration3SystemScope,
                systemVentilationRoute,
                noIzamThermalSource,
                Assessment(guids_Space_Dwelling),
                Assessment(guids_Space_Dwelling),
                dictionary_Room,
                out List<PartOIteration3Room> _,
                out List<PartOIteration3CriterionComparison> _,
                out List<string> _);

            Assert.Contains(refusals, x => x.Contains(expected));
            Assert.Contains(refusals, x => x.Contains("modelled alongside the explicit TAS Systems network"));
        }

        //-------------------------------------------------------------------------------------------------
        //Identity
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// Both dwellings hold a room called "Bedroom 2". A join that fell back to a name would merge
        /// them and report half the rooms - so this asserts the count as well as the criteria.
        /// </summary>
        [Fact]
        public void Rooms_with_the_same_name_in_two_dwellings_are_kept_apart()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Scope();

            SystemVentilationRoute systemVentilationRoute = Route();

            Query.PartOIteration3ReconciliationRefusals(
                adjacencyCluster,
                partOIteration3SystemScope,
                systemVentilationRoute,
                systemVentilationRoute.NoIzamThermalSource,
                Assessment(guids_Space_Dwelling),
                Assessment(guids_Space_Dwelling),
                dictionary_Room,
                out List<PartOIteration3Room> rooms,
                out List<PartOIteration3CriterionComparison> criteria,
                out List<string> _);

            Assert.Equal(6, rooms.Count);
            Assert.Equal(6, criteria.Count);

            //Two distinct rooms, same name, different dwellings.
            List<PartOIteration3Room> rooms_Bedroom = rooms.FindAll(x => x.Name_Space == "Bedroom 2");

            Assert.Equal(2, rooms_Bedroom.Count);
            Assert.NotEqual(rooms_Bedroom[0].Guid_Space, rooms_Bedroom[1].Guid_Space);
            Assert.NotEqual(rooms_Bedroom[0].Guid_Dwelling, rooms_Bedroom[1].Guid_Dwelling);
        }
    }
}
