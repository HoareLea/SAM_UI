// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR5B (SAM#111): <c>Query.PartOIteration3CoolingResolution</c>, the cooling evidence row, and the binding and
    /// outcome queries - in isolation, with a catalogue read from a real temporary file through the production
    /// reader. Every figure is a fixture value; no real product appears.
    /// </summary>
    public class PartOIteration3CoolingTests : IDisposable
    {
        private readonly string directory = PartOIteration3Fixture.Directory_Temp();

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

        private const string source = "Test Fixture, Published Cooling Performance, v.1 - not a real product";

        private static AirHandlingUnit Unit(AdjacencyCluster adjacencyCluster, string name, double duty_Lps = 60.0)
        {
            VentilationSystem ventilationSystem = PartOIteration3Fixture.VentilationSystem(adjacencyCluster, name, "MVHR", out AirHandlingUnit airHandlingUnit);

            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem, PartOIteration3Fixture.Space(adjacencyCluster, name + " Bedroom"), FlowClassification.Supply, duty_Lps);
            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem, PartOIteration3Fixture.Space(adjacencyCluster, name + " Bathroom"), FlowClassification.Extract, duty_Lps);

            return airHandlingUnit;
        }

        private static void Select(AdjacencyCluster adjacencyCluster, AirHandlingUnit airHandlingUnit, string manufacturer, string model)
        {
            airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, new VentilationUnitReference(manufacturer, model, null));
            adjacencyCluster.AddObject(airHandlingUnit);
        }

        /// <summary>A catalogue with one product: fixture cooling table (2 x 2 x 3) and a two-point law, capacity as stated.</summary>
        private string Catalogue(string manufacturer = "Fixture Maker", string model = "FIXTURE-C", double capacity_Lps = 150, bool cooling = true, bool law = true, string lawTemperatures = "[21,25]")
        {
            List<string> fields =
            [
                "\"_type\": \"SAM.Analytical.VentilationUnitTemplate,SAM.Analytical\"",
                "\"VentilationUnitReference\": { \"_type\": \"SAM.Analytical.VentilationUnitReference,SAM.Analytical\", \"Manufacturer\": \"" + manufacturer + "\", \"Model\": \"" + model + "\" }",
                "\"MaximumSupplyFlowRate_Lps\": " + capacity_Lps.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "\"MaximumExtractFlowRate_Lps\": " + capacity_Lps.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "\"Source\": \"" + source + "\"",
                "\"Rank\": 0",
            ];

            if (cooling)
            {
                fields.Add(
                    "\"PerformanceTable\": { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceTable,SAM.Analytical\", \"Axes\": [" +
                    "{ \"_type\": \"SAM.Analytical.VentilationUnitPerformanceAxis,SAM.Analytical\", \"Name\": \"ExternalDryBulbTemperature\", \"Unit\": \"degC\", \"Values\": [20,30] }," +
                    "{ \"_type\": \"SAM.Analytical.VentilationUnitPerformanceAxis,SAM.Analytical\", \"Name\": \"EnteringDryBulbTemperature\", \"Unit\": \"degC\", \"Values\": [22,26] }," +
                    "{ \"_type\": \"SAM.Analytical.VentilationUnitPerformanceAxis,SAM.Analytical\", \"Name\": \"AirFlowRate\", \"Unit\": \"l/s\", \"Values\": [40,80,120] } ]," +
                    "\"Outputs\": [ { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceOutput,SAM.Analytical\", \"Name\": \"SupplyAirTemperature\", \"Unit\": \"degC\", \"Values\": [14,15,16,16,17,18,15,16,17,17,18,19] } ] }");
            }

            if (law)
            {
                fields.Add(
                    "\"FlowFractionByControlTemperature\": { \"_type\": \"SAM.Analytical.FlowFractionControlCurve,SAM.Analytical\", \"PerformanceTable\": { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceTable,SAM.Analytical\", \"Axes\": [" +
                    "{ \"_type\": \"SAM.Analytical.VentilationUnitPerformanceAxis,SAM.Analytical\", \"Name\": \"ControlTemperature\", \"Unit\": \"degC\", \"Values\": " + lawTemperatures + " } ]," +
                    "\"Outputs\": [ { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceOutput,SAM.Analytical\", \"Name\": \"FlowFraction\", \"Unit\": \"-\", \"Values\": " + (lawTemperatures == "[21]" ? "[1.0]" : "[0.4,1.0]") + " } ] }, \"PerformanceDomainPolicy\": \"ClampToDomain\" }");
            }

            File.WriteAllText(
                Path.Combine(directory, SAM.Analytical.Systems.Query.VentilationUnitCatalogueFileName),
                "{ \"Schema\": \"VentilationUnitCatalogue:v2\", \"Templates\": [ { " + string.Join(",", fields) + " } ] }");

            return directory;
        }

        private static List<string> Resolve(AdjacencyCluster adjacencyCluster, string directory, out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, out List<PartOIteration3CoolingEvidence> cooling)
        {
            return Query.PartOIteration3CoolingResolution(adjacencyCluster, VentilationUnitCatalogue.Read(directory), out coolingSettings, out cooling, out List<string> _);
        }

        // =====================================================================================================
        // Resolution
        // =====================================================================================================

        [Fact]
        public void A_selected_product_resolves_to_its_table_its_law_the_validated_ceiling_and_the_gate()
        {
            AdjacencyCluster adjacencyCluster = new();
            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "Flat 1");
            Select(adjacencyCluster, airHandlingUnit, "Fixture Maker", "FIXTURE-C");

            List<string> refusals = Resolve(adjacencyCluster, Catalogue(), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, out List<PartOIteration3CoolingEvidence> cooling);

            Assert.Empty(refusals);

            MechanicalVentilationCoolingSettings settings = Assert.Single(coolingSettings).Value;
            Assert.Null(settings.Refusal());

            //The ceiling is the table's own airflow axis - validated performance, not the 150 l/s capacity.
            Assert.Equal(120.0, settings.MaximumOperatingAirFlow_Lps);
            Assert.Equal(48.0, settings.MinimumOperatingAirFlow_Lps, 9);
            Assert.Equal(21.0, settings.CoolingEnableTemperature_C);
            Assert.Equal(12, settings.SupplyAirTemperatureTable.PointCount);

            PartOIteration3CoolingEvidence row = Assert.Single(cooling);
            Assert.Equal(airHandlingUnit.Guid, row.Guid_AirHandlingUnit);
            Assert.Equal(150.0, row.MaximumSupplyFlowRate_Lps);
            Assert.Equal(60.0, row.DesignSupplyFlowRate_Lps);
            Assert.Equal(120.0, row.MaximumOperatingAirFlow_Lps);
            Assert.NotEqual(row.MaximumSupplyFlowRate_Lps, row.MaximumOperatingAirFlow_Lps);
            Assert.NotEqual(row.DesignSupplyFlowRate_Lps, row.MaximumOperatingAirFlow_Lps);
            Assert.Contains("12 cells", row.Table);
            Assert.Equal(64, row.Sha256_Table.Length);
            Assert.False(string.IsNullOrWhiteSpace(row.DeclaredRules));

            //Not yet bound, not yet evidenced: not a complete row.
            Assert.Equal(Guid.Empty, row.Guid_AirSystem);
            Assert.False(row.IsComplete);
        }

        [Fact]
        public void The_resolution_reads_no_name_the_same_data_under_another_name_resolves_identically()
        {
            AdjacencyCluster adjacencyCluster_1 = new();
            AirHandlingUnit airHandlingUnit_1 = Unit(adjacencyCluster_1, "Flat 1");
            Select(adjacencyCluster_1, airHandlingUnit_1, "Maker A", "MODEL-A");
            Resolve(adjacencyCluster_1, Catalogue("Maker A", "MODEL-A"), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings_1, out _);

            AdjacencyCluster adjacencyCluster_2 = new();
            AirHandlingUnit airHandlingUnit_2 = Unit(adjacencyCluster_2, "Flat 1");
            Select(adjacencyCluster_2, airHandlingUnit_2, "Maker B", "MODEL-B");
            Resolve(adjacencyCluster_2, Catalogue("Maker B", "MODEL-B"), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings_2, out _);

            Assert.Equal(
                Assert.Single(coolingSettings_1).Value.ToJsonObject().ToJsonString(),
                Assert.Single(coolingSettings_2).Value.ToJsonObject().ToJsonString());
        }

        [Fact]
        public void A_unit_with_no_selection_refuses_the_whole_call_all_or_nothing()
        {
            AdjacencyCluster adjacencyCluster = new();
            AirHandlingUnit airHandlingUnit_1 = Unit(adjacencyCluster, "Flat 1");
            Unit(adjacencyCluster, "Flat 2");
            Select(adjacencyCluster, airHandlingUnit_1, "Fixture Maker", "FIXTURE-C");

            List<string> refusals = Resolve(adjacencyCluster, Catalogue(), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, out List<PartOIteration3CoolingEvidence> cooling);

            Assert.Contains(refusals, x => x.Contains("no selected ventilation unit product"));
            Assert.Empty(coolingSettings);
            Assert.Empty(cooling);
        }

        [Fact]
        public void A_product_that_publishes_no_cooling_table_or_no_law_refuses()
        {
            foreach ((bool cooling_Table, bool law) in new[] { (false, true), (true, false) })
            {
                AdjacencyCluster adjacencyCluster = new();
                AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "Flat 1");
                Select(adjacencyCluster, airHandlingUnit, "Fixture Maker", "FIXTURE-C");

                List<string> refusals = Resolve(adjacencyCluster, Catalogue(cooling: cooling_Table, law: law), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, out _);

                Assert.Contains(refusals, x => x.Contains("publishes no cooling module"));
                Assert.Empty(coolingSettings);
            }
        }

        [Fact]
        public void A_validated_ceiling_above_the_selected_capacity_refuses()
        {
            AdjacencyCluster adjacencyCluster = new();
            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "Flat 1");
            Select(adjacencyCluster, airHandlingUnit, "Fixture Maker", "FIXTURE-C");

            //Capacity 100 l/s still carries the 60 l/s design duty, but the table tabulates to 120 l/s.
            List<string> refusals = Resolve(adjacencyCluster, Catalogue(capacity_Lps: 100), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, out _);

            Assert.Contains(refusals, x => x.Contains("exceeds the selected unit's"));
            Assert.Empty(coolingSettings);
        }

        [Fact]
        public void A_law_that_is_not_two_points_refuses()
        {
            AdjacencyCluster adjacencyCluster = new();
            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "Flat 1");
            Select(adjacencyCluster, airHandlingUnit, "Fixture Maker", "FIXTURE-C");

            List<string> refusals = Resolve(adjacencyCluster, Catalogue(lawTemperatures: "[21]"), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, out _);

            Assert.NotEmpty(refusals);
            Assert.Empty(coolingSettings);
        }

        [Fact]
        public void An_unavailable_catalogue_refuses()
        {
            List<string> refusals = Resolve(new AdjacencyCluster(), Path.Combine(directory, "missing"), out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, out _);

            Assert.Single(refusals);
            Assert.Empty(coolingSettings);
        }

        // =====================================================================================================
        // The evidence row
        // =====================================================================================================

        private static PartOIteration3CoolingEvidence Row()
        {
            return new PartOIteration3CoolingEvidence(
                Guid.NewGuid(), "Flat 1", "Fixture Maker", "FIXTURE-C", null, "COOL-1", source,
                150, 150, 60, 60, "table", new string('A', 64), 120, 48, 21, 25, 0.4, 1.0, 21, "rules");
        }

        private static void Outcome(PartOIteration3CoolingEvidence row, int heating = 0, int gateViolation = 0, int outOfRange = 0)
        {
            row.RecordOutcome(8760, 500, heating, 8000, gateViolation, outOfRange, 12, 0, 48, 50, 120, 300, 1e-6, 0.02);
        }

        [Fact]
        public void A_row_is_complete_only_bound_evidenced_and_free_of_refused_behaviour()
        {
            PartOIteration3CoolingEvidence row = Row();
            Assert.False(row.IsComplete);

            Assert.True(row.Bind(Guid.NewGuid()));
            Assert.False(row.IsComplete);
            Assert.False(row.Bind(Guid.NewGuid()));

            Outcome(row);
            Assert.True(row.IsComplete);

            foreach (Action<PartOIteration3CoolingEvidence> defect in new Action<PartOIteration3CoolingEvidence>[] { x => Outcome(x, heating: 1), x => Outcome(x, gateViolation: 1), x => Outcome(x, outOfRange: 1) })
            {
                PartOIteration3CoolingEvidence row_Defect = Row();
                row_Defect.Bind(Guid.NewGuid());
                defect(row_Defect);
                Assert.False(row_Defect.IsComplete);
            }
        }

        [Fact]
        public void A_row_and_a_record_carrying_it_survive_a_round_trip()
        {
            PartOIteration3CoolingEvidence row = Row();
            row.Bind(Guid.NewGuid());
            Outcome(row);

            PartOIteration3CoolingEvidence roundTrip = PartOIteration3CoolingEvidence.FromJsonObject(row.ToJsonObject());
            Assert.Equal(row.ToJsonObject().ToJsonString(), roundTrip.ToJsonObject().ToJsonString());
            Assert.True(roundTrip.IsComplete);

            PartOIteration3Record partOIteration3Record = new() { BehaviourMode = PartOIteration3BehaviourMode.SelectedProductCooling };
            partOIteration3Record.Add(row);

            PartOIteration3Record partOIteration3Record_RoundTrip = PartOIteration3Record.Parse(partOIteration3Record.ToString());
            Assert.Equal(PartOIteration3BehaviourMode.SelectedProductCooling, partOIteration3Record_RoundTrip.BehaviourMode);
            Assert.Equal(row.ToJsonObject().ToJsonString(), Assert.Single(partOIteration3Record_RoundTrip.Cooling).ToJsonObject().ToJsonString());

            //A record written before the cooling mode existed reads as no cooling at all.
            Assert.Empty(PartOIteration3Record.Parse(new PartOIteration3Record().ToString()).Cooling);
        }

        // =====================================================================================================
        // Binding, outcome, history
        // =====================================================================================================

        [Fact]
        public void A_resolved_row_with_no_materialised_branch_refuses()
        {
            List<string> refusals = Query.PartOIteration3CoolingBindings([Row()], new MechanicalVentilationMaterialisation(null, null, null, null));

            Assert.Contains(refusals, x => x.Contains("no recirculation cooling branch was materialised"));
        }

        [Fact]
        public void No_complete_route_evidence_refuses_every_outcome()
        {
            List<string> refusals = Query.PartOIteration3CoolingOutcomes([Row()], null);

            Assert.Single(refusals);
        }

        [Fact]
        public void The_cooling_mode_writes_its_own_documents_and_never_the_B0_controls()
        {
            PartOIteration3Paths paths_B0 = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd");
            PartOIteration3Paths paths_B4 = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProductCooling);

            //B0 exactly as before the cooling mode existed.
            Assert.Equal("Flat1" + PartOIteration3Paths.Suffix_CandidateB, paths_B0.ProjectName_CandidateB);
            Assert.Equal("C:\\out\\Iteration3\\tas\\Flat1-It3B.tpd", paths_B0.Path_TPD);

            //The selected-product method has its own documents too, so it no longer overwrites B0's and the two
            //results can be kept side by side.
            Assert.Equal("C:\\out\\Iteration3\\tas\\Flat1-It3BP.tpd", PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProduct).Path_TPD);

            Assert.Equal("Flat1" + PartOIteration3Paths.Suffix_CandidateB_Cooling, paths_B4.ProjectName_CandidateB);
            Assert.Equal("C:\\out\\Iteration3\\diagnostics\\Flat1-It3B4-OperatingAirFlow.csv", paths_B4.Path_OperatingAirFlow);
            Assert.Contains(paths_B4.Path_OperatingAirFlow, paths_B4.Paths_CandidateB);

            //No Candidate B document is shared between the two - and neither is the record: each method keeps its own.
            Assert.NotEqual(paths_B0.Path_Record, paths_B4.Path_Record);

            foreach (string path in new[] { paths_B4.Path_TBD_ThermalSource, paths_B4.Path_TSD_ThermalSource, paths_B4.Path_TPD, paths_B4.Path_TBD_Bridge, paths_B4.Path_TSD_Bridge, paths_B4.Path_Model_CandidateB, paths_B4.Path_TM59Report_CandidateB, paths_B4.Path_Record })
            {
                Assert.DoesNotContain(path, paths_B0.Paths_CandidateB);
            }
        }

        [Fact]
        public void The_history_states_OperatingAirFlow_and_nothing_of_DesignAirFlow()
        {
            string csv = Query.PartOIteration3OperatingAirFlowCsv(null, [Row()]);

            Assert.StartsWith("AirHandlingUnit,Guid_AirHandlingUnit,Guid_AirSystem,Hour,OperatingAirFlow_Lps", csv);
            Assert.DoesNotContain("Design", csv);
        }
    }
}
