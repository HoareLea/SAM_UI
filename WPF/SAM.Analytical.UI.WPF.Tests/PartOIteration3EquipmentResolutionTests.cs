// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b><c>Query.PartOIteration3EquipmentResolution</c> in isolation - PR5A (SAM#111 plan §J).</b>
    ///
    /// <para>
    /// No <c>PartORun</c>, no pipeline, no TAS - just an adjacency cluster carrying real
    /// <c>AirHandlingUnit</c> objects wired to a real design duty, a catalogue read from a real temporary
    /// file through the same reader production uses, and the resolver's own refusal and success rules.
    /// <see cref="PartOIteration3RunTests"/> proves the orchestration around this call; these tests prove
    /// what the call itself decides.
    /// </para>
    /// </summary>
    public class PartOIteration3EquipmentResolutionTests : IDisposable
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
                //A temporary directory that will not delete is not a test failure.
            }
        }

        private const string manufacturer = "Test Fixture";

        private const string model = "FIXTURE-60";

        private const string source = "Test Fixture, Certified Performance, v.1 - not a real product";

        //-------------------------------------------------------------------------------------------------
        //A real air handling unit, wired to a real, balanced design duty through a real ventilation system
        //and real terminals - the same production types and relations Query.AirHandlingUnitDesignDuty
        //reads, not a stand-in for them.
        //-------------------------------------------------------------------------------------------------

        private static AirHandlingUnit Unit(AdjacencyCluster adjacencyCluster, string name, double supplyDuty_Lps = 60.0, double extractDuty_Lps = 60.0)
        {
            VentilationSystem ventilationSystem = PartOIteration3Fixture.VentilationSystem(adjacencyCluster, name, "MVHR", out AirHandlingUnit airHandlingUnit);

            Space space_Supply = PartOIteration3Fixture.Space(adjacencyCluster, name + " Bedroom");
            Space space_Extract = PartOIteration3Fixture.Space(adjacencyCluster, name + " Bathroom");

            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem, space_Supply, FlowClassification.Supply, supplyDuty_Lps);
            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem, space_Extract, FlowClassification.Extract, extractDuty_Lps);

            return airHandlingUnit;
        }

        /// <summary>Stamps a selected product identity directly - standing in for Iteration 2's own selection authority, which this resolver only ever reads.</summary>
        private static void Select(AdjacencyCluster adjacencyCluster, AirHandlingUnit airHandlingUnit, VentilationUnitReference ventilationUnitReference)
        {
            airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, ventilationUnitReference);

            adjacencyCluster.AddObject(airHandlingUnit);
        }

        private static PartOIteration3EquipmentEvidence Evidence(Guid guid_AirHandlingUnit, string name)
        {
            return new PartOIteration3EquipmentEvidence(
                guid_AirHandlingUnit, name, manufacturer, model, null, source,
                60.0, 0.86, "SupplyTemperatureEfficiency", false, null,
                0.62, "TotalBothFans", false, null);
        }

        //-------------------------------------------------------------------------------------------------
        //A real catalogue, read from a real temporary file through VentilationUnitCatalogue.Read - the
        //same reader production calls. Modelled on SAM_Systems' own VentilationUnitCatalogueV2Tests fixture.
        //-------------------------------------------------------------------------------------------------

        private string Catalogue(bool heatRecovery = true, bool fanPerformance = true, string reference = null)
        {
            List<string> fields =
            [
                "\"_type\": \"SAM.Analytical.VentilationUnitTemplate,SAM.Analytical\"",
                "\"VentilationUnitReference\": { \"_type\": \"SAM.Analytical.VentilationUnitReference,SAM.Analytical\", \"Manufacturer\": \"" + manufacturer + "\", \"Model\": \"" + model + "\"" + (reference is null ? string.Empty : ", \"Reference\": \"" + reference + "\"") + " }",
                "\"MaximumSupplyFlowRate_Lps\": 150",
                "\"MaximumExtractFlowRate_Lps\": 150",
                "\"Source\": \"" + source + "\"",
                "\"Rank\": 0",
            ];

            if (heatRecovery)
            {
                fields.Add(
                    "\"HeatRecoveryPerformance\": { \"_type\": \"SAM.Analytical.HeatRecoveryPerformance,SAM.Analytical\"," +
                    "\"PerformanceTable\": { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceTable,SAM.Analytical\"," +
                    "\"Axes\": [ { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceAxis,SAM.Analytical\", \"Name\": \"AirFlowRate\", \"Unit\": \"l/s\", \"Values\": [30,60,90,150] } ]," +
                    "\"Outputs\": [ { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceOutput,SAM.Analytical\", \"Name\": \"SensibleHeatRecoveryEfficiency\", \"Unit\": \"-\", \"Values\": [0.90,0.86,0.82,0.74] } ] }," +
                    "\"HeatRecoveryEfficiencyBasis\": \"SupplyTemperatureEfficiency\", \"PerformanceDomainPolicy\": \"Refuse\", \"Source\": \"" + source + "\" }");
            }

            if (fanPerformance)
            {
                fields.Add(
                    "\"FanPerformance\": { \"_type\": \"SAM.Analytical.FanPerformance,SAM.Analytical\"," +
                    "\"PerformanceTable\": { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceTable,SAM.Analytical\"," +
                    "\"Axes\": [ { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceAxis,SAM.Analytical\", \"Name\": \"AirFlowRate\", \"Unit\": \"l/s\", \"Values\": [30,60,90,150] } ]," +
                    "\"Outputs\": [ { \"_type\": \"SAM.Analytical.VentilationUnitPerformanceOutput,SAM.Analytical\", \"Name\": \"SpecificFanPower\", \"Unit\": \"W/(l/s)\", \"Values\": [0.50,0.62,0.80,1.40] } ] }," +
                    "\"SpecificFanPowerBasis\": \"TotalBothFans\", \"PerformanceDomainPolicy\": \"Refuse\", \"Source\": \"" + source + "\" }");
            }

            string entry = "{ " + string.Join(",", fields) + " }";

            File.WriteAllText(
                Path.Combine(directory, SAM.Analytical.Systems.Query.VentilationUnitCatalogueFileName),
                "{ \"Schema\": \"VentilationUnitCatalogue:v2\", \"Templates\": [" + entry + "] }");

            return directory;
        }

        //-------------------------------------------------------------------------------------------------
        //Refusals
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void An_unavailable_catalogue_refuses_by_the_exact_path_it_tried_to_read()
        {
            string directory_Missing = Path.Combine(directory, "missing");
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(directory_Missing);

            List<string> refusals = Query.PartOIteration3EquipmentResolution(new AdjacencyCluster(), ventilationUnitCatalogue, out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains(ventilationUnitCatalogue.Path, refusals[0]);
            Assert.Contains("could not be read", refusals[0]);
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Fact]
        public void An_empty_design_refuses_naming_the_absence_of_any_air_handling_unit()
        {
            AdjacencyCluster adjacencyCluster = new();

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains("no air handling unit", refusals[0]);
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Fact]
        public void A_unit_with_no_selected_product_refuses_naming_the_unit_by_identity()
        {
            AdjacencyCluster adjacencyCluster = new();

            Unit(adjacencyCluster, "MVHR-01");

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains("MVHR-01", refusals[0]);
            Assert.Contains("no selected ventilation unit product", refusals[0]);
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Fact]
        public void A_selected_reference_the_catalogue_does_not_hold_refuses_and_substitutes_nothing()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "MVHR-01");
            Select(adjacencyCluster, airHandlingUnit, new VentilationUnitReference(manufacturer, "NOT-IN-CATALOGUE", null));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains("NOT-IN-CATALOGUE", refusals[0]);
            Assert.Contains("does not hold", refusals[0]);
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Fact]
        public void A_catalogue_product_without_a_traceable_source_refuses_before_materialisation()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "MVHR-01");
            Select(adjacencyCluster, airHandlingUnit, new VentilationUnitReference(manufacturer, model, null));

            string directory_Catalogue = Catalogue();
            string path_Catalogue = Path.Combine(directory_Catalogue, SAM.Analytical.Systems.Query.VentilationUnitCatalogueFileName);
            File.WriteAllText(path_Catalogue, File.ReadAllText(path_Catalogue).Replace("\"Source\": \"" + source + "\"", "\"Source\": \"\""));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(directory_Catalogue), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains(path_Catalogue, refusals[0]);
            Assert.Contains("catalogue could not be read", refusals[0]);
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void A_selected_product_missing_certified_data_refuses_citing_the_resolvers_own_text(bool heatRecovery, bool fanPerformance)
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "MVHR-01");
            Select(adjacencyCluster, airHandlingUnit, new VentilationUnitReference(manufacturer, model, null));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue(heatRecovery, fanPerformance)), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains("MVHR-01", refusals[0]);
            Assert.Contains(heatRecovery ? "fan performance" : "heat recovery performance", refusals[0]);
            Assert.Contains("states no", refusals[0]);
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Fact]
        public void An_unbalanced_design_duty_refuses_both_quantities_and_substitutes_neither()
        {
            AdjacencyCluster adjacencyCluster = new();

            //60 l/s supply against 90 l/s extract - a real design duty this resolver certifies nothing about.
            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "MVHR-01", supplyDuty_Lps: 60.0, extractDuty_Lps: 90.0);
            Select(adjacencyCluster, airHandlingUnit, new VentilationUnitReference(manufacturer, model, null));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            //Both quantities are certified at one balanced airflow, so an unbalanced duty refuses both -
            //once for heat recovery, once for fan performance - never one substituted for the other.
            Assert.Equal(2, refusals.Count);
            Assert.All(refusals, x => Assert.Contains("unbalanced", x));
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Fact]
        public void An_undersized_selected_product_refuses_without_reselection_or_settings()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "MVHR-01", supplyDuty_Lps: 160.0, extractDuty_Lps: 160.0);
            Select(adjacencyCluster, airHandlingUnit, new VentilationUnitReference(manufacturer, model, null));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains("MVHR-01", refusals[0]);
            Assert.Contains("150", refusals[0]);
            Assert.Contains("160", refusals[0]);
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        [Fact]
        public void One_unit_failing_to_resolve_clears_every_units_settings_and_evidence_all_or_nothing()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit_Resolves = Unit(adjacencyCluster, "MVHR-01");
            Select(adjacencyCluster, airHandlingUnit_Resolves, new VentilationUnitReference(manufacturer, model, null));

            //A second unit, of the same product, but with no design duty at all - so it cannot resolve.
            PartOIteration3Fixture.VentilationSystem(adjacencyCluster, "MVHR-02", "MVHR", out AirHandlingUnit airHandlingUnit_Refuses);
            Select(adjacencyCluster, airHandlingUnit_Refuses, new VentilationUnitReference(manufacturer, model, null));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Single(refusals);
            Assert.Contains("MVHR-02", refusals[0]);
            Assert.Contains("no design duty", refusals[0]);

            //The all-or-nothing rule: MVHR-01 resolved perfectly well, and its settings are cleared anyway.
            Assert.Empty(unitSettings);
            Assert.Empty(equipment);
        }

        //-------------------------------------------------------------------------------------------------
        //Success
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_fully_resolved_unit_produces_the_declared_settings_and_the_certified_evidence()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "MVHR-01");
            Select(adjacencyCluster, airHandlingUnit, new VentilationUnitReference(manufacturer, model, "REV-1"));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue(reference: "REV-1")), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> notes);

            Assert.Empty(refusals);

            MechanicalVentilationUnitSettings mechanicalVentilationUnitSettings = Assert.Single(unitSettings).Value;

            //60 l/s is an exact published operating point: 0.86 sensible efficiency, 0.62 W/(l/s) specific
            //fan power - no interpolation, no clamping, nothing to round.
            Assert.Equal(0.86, mechanicalVentilationUnitSettings.HeatRecoverySensibleEfficiency);

            //The declared equal split of a certified TOTAL, per plan §C - never two certified halves.
            Assert.Equal(310.0, mechanicalVentilationUnitSettings.SupplyFanPressure_Pa);
            Assert.Equal(310.0, mechanicalVentilationUnitSettings.ExtractFanPressure_Pa);
            Assert.Equal(1.0, mechanicalVentilationUnitSettings.FanOverallEfficiency);

            //The declared fan-heat assumption, not a certified figure - see the resolver's own remarks.
            Assert.Equal(1.0, mechanicalVentilationUnitSettings.SupplyFanHeatGainFactor);
            Assert.Equal(1.0, mechanicalVentilationUnitSettings.ExtractFanHeatGainFactor);

            PartOIteration3EquipmentEvidence partOIteration3EquipmentEvidence = Assert.Single(equipment);

            Assert.Equal(airHandlingUnit.Guid, partOIteration3EquipmentEvidence.Guid_AirHandlingUnit);
            Assert.Equal("MVHR-01", partOIteration3EquipmentEvidence.Name_AirHandlingUnit);
            Assert.Equal(manufacturer, partOIteration3EquipmentEvidence.Manufacturer);
            Assert.Equal(model, partOIteration3EquipmentEvidence.Model);
            Assert.Equal("REV-1", partOIteration3EquipmentEvidence.Reference);
            Assert.Equal(source, partOIteration3EquipmentEvidence.Source);
            Assert.Equal(60.0, partOIteration3EquipmentEvidence.DesignAirFlowRate_Lps);
            Assert.Equal(150.0, partOIteration3EquipmentEvidence.MaximumSupplyFlowRate_Lps);
            Assert.Equal(150.0, partOIteration3EquipmentEvidence.MaximumExtractFlowRate_Lps);
            Assert.Equal(60.0, partOIteration3EquipmentEvidence.DesignSupplyFlowRate_Lps);
            Assert.Equal(60.0, partOIteration3EquipmentEvidence.DesignExtractFlowRate_Lps);
            Assert.Contains("DesignAirFlow", partOIteration3EquipmentEvidence.OperatingAirFlowBasis);
            Assert.Equal(0.86, partOIteration3EquipmentEvidence.SensibleHeatRecoveryEfficiency);
            Assert.False(partOIteration3EquipmentEvidence.HeatRecoveryClampedToDomain);
            Assert.Equal(0.62, partOIteration3EquipmentEvidence.SpecificFanPower_WPerLps);
            Assert.False(partOIteration3EquipmentEvidence.FanPerformanceClampedToDomain);
            Assert.Equal(310.0, partOIteration3EquipmentEvidence.SupplyFanPressure_Pa);
            Assert.Equal(310.0, partOIteration3EquipmentEvidence.ExtractFanPressure_Pa);
            Assert.Equal(1.0, partOIteration3EquipmentEvidence.FanOverallEfficiency);
            Assert.Equal(1.0, partOIteration3EquipmentEvidence.SupplyFanHeatGainFactor);
            Assert.Equal(1.0, partOIteration3EquipmentEvidence.ExtractFanHeatGainFactor);
            Assert.Contains("split equally", partOIteration3EquipmentEvidence.FanPowerSplitRule);
            Assert.Contains("Declared assumption", partOIteration3EquipmentEvidence.FanHeatGainAssumption);
            Assert.True(partOIteration3EquipmentEvidence.IsResolved);
            Assert.False(partOIteration3EquipmentEvidence.IsComplete);

            Guid guid_AirSystem = Guid.NewGuid();
            Assert.True(partOIteration3EquipmentEvidence.BindAirSystem(guid_AirSystem));
            Assert.True(partOIteration3EquipmentEvidence.BindAirSystem(guid_AirSystem));
            Assert.False(partOIteration3EquipmentEvidence.BindAirSystem(Guid.NewGuid()));
            Assert.Equal(guid_AirSystem, partOIteration3EquipmentEvidence.Guid_AirSystem);
            Assert.True(partOIteration3EquipmentEvidence.IsComplete);

            Assert.Contains(notes, x => x.Contains("MVHR-01"));
        }

        [Fact]
        public void Catalogue_metadata_names_and_hashes_the_exact_file_read()
        {
            string directory_Catalogue = Catalogue();
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(directory_Catalogue);

            Assert.Equal(Path.GetFullPath(directory_Catalogue), ventilationUnitCatalogue.Directory);
            Assert.Equal(Path.Combine(Path.GetFullPath(directory_Catalogue), SAM.Analytical.Systems.Query.VentilationUnitCatalogueFileName), ventilationUnitCatalogue.Path);
            Assert.Equal("VentilationUnitCatalogue:v2", ventilationUnitCatalogue.Schema);
            Assert.Matches("^[0-9A-F]{64}$", ventilationUnitCatalogue.Sha256);
        }

        [Fact]
        public void Equipment_lineage_binds_each_AHU_to_its_air_system_and_ignores_other_binding_types()
        {
            Guid guid_AirHandlingUnit_1 = Guid.NewGuid();
            Guid guid_AirHandlingUnit_2 = Guid.NewGuid();
            Guid guid_AirSystem_1 = Guid.NewGuid();
            Guid guid_AirSystem_2 = Guid.NewGuid();

            PartOIteration3EquipmentEvidence equipment_1 = Evidence(guid_AirHandlingUnit_1, "MVHR-01");
            PartOIteration3EquipmentEvidence equipment_2 = Evidence(guid_AirHandlingUnit_2, "MVHR-02");

            List<string> refusals = Query.PartOIteration3EquipmentBindings(
                [equipment_1, equipment_2],
                [
                    new MechanicalVentilationBinding(MechanicalVentilationBindingType.SystemSpace, guid_AirHandlingUnit_1, Guid.NewGuid()),
                    new MechanicalVentilationBinding(MechanicalVentilationBindingType.AirSystem, guid_AirHandlingUnit_1, guid_AirSystem_1),
                    new MechanicalVentilationBinding(MechanicalVentilationBindingType.AirSystem, guid_AirHandlingUnit_2, guid_AirSystem_2),
                ]);

            Assert.Empty(refusals);
            Assert.Equal(guid_AirSystem_1, equipment_1.Guid_AirSystem);
            Assert.Equal(guid_AirSystem_2, equipment_2.Guid_AirSystem);
        }

        [Fact]
        public void Missing_or_conflicting_air_system_lineage_refuses_by_AHU()
        {
            Guid guid_AirHandlingUnit_Conflicting = Guid.NewGuid();
            Guid guid_AirHandlingUnit_Missing = Guid.NewGuid();

            List<string> refusals = Query.PartOIteration3EquipmentBindings(
                [Evidence(guid_AirHandlingUnit_Conflicting, "MVHR-01"), Evidence(guid_AirHandlingUnit_Missing, "MVHR-02")],
                [
                    new MechanicalVentilationBinding(MechanicalVentilationBindingType.AirSystem, guid_AirHandlingUnit_Conflicting, Guid.NewGuid()),
                    new MechanicalVentilationBinding(MechanicalVentilationBindingType.AirSystem, guid_AirHandlingUnit_Conflicting, Guid.NewGuid()),
                ]);

            Assert.Equal(2, refusals.Count);
            Assert.Contains(refusals, x => x.Contains(guid_AirHandlingUnit_Conflicting.ToString()) && x.Contains("more than one"));
            Assert.Contains(refusals, x => x.Contains("MVHR-02") && x.Contains("no single"));
        }

        /// <summary>
        /// SAM #114 scope leaves a scoped-out system's AirHandlingUnit object in the working copy. Only the
        /// units a retained ventilation system names are materialised, so only they are resolved - a
        /// scoped-out unit with no selection neither refuses the attempt nor receives settings.
        /// </summary>
        [Fact]
        public void A_unit_no_retained_ventilation_system_names_is_neither_required_nor_configured()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster, "MVHR-01");
            Select(adjacencyCluster, airHandlingUnit, new VentilationUnitReference(manufacturer, model, null));

            AirHandlingUnit airHandlingUnit_ScopedOut = Analytical.Create.AirHandlingUnit("MVHR-SCOPED-OUT");
            adjacencyCluster.AddObject(airHandlingUnit_ScopedOut);

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> notes);

            Assert.Empty(refusals);
            Assert.Equal(airHandlingUnit.Guid, Assert.Single(unitSettings).Key);
            Assert.Equal(airHandlingUnit.Guid, Assert.Single(equipment).Guid_AirHandlingUnit);
            Assert.DoesNotContain(airHandlingUnit_ScopedOut.Guid, unitSettings.Keys);
            Assert.Contains(notes, x => x.Contains("MVHR-SCOPED-OUT") && x.Contains("not materialised"));
        }

        [Fact]
        public void One_air_system_bound_by_two_AHUs_refuses_before_simulation()
        {
            Guid guid_AirHandlingUnit_1 = Guid.NewGuid();
            Guid guid_AirHandlingUnit_2 = Guid.NewGuid();
            Guid guid_AirSystem_Shared = Guid.NewGuid();

            List<string> refusals = Query.PartOIteration3EquipmentBindings(
                [Evidence(guid_AirHandlingUnit_1, "MVHR-01"), Evidence(guid_AirHandlingUnit_2, "MVHR-02")],
                [
                    new MechanicalVentilationBinding(MechanicalVentilationBindingType.AirSystem, guid_AirHandlingUnit_1, guid_AirSystem_Shared),
                    new MechanicalVentilationBinding(MechanicalVentilationBindingType.AirSystem, guid_AirHandlingUnit_2, guid_AirSystem_Shared),
                ]);

            Assert.Contains(refusals, x => x.Contains(guid_AirSystem_Shared.ToString()) && x.Contains("more than one air handling unit"));
        }

        private static PartOIteration3EquipmentEvidence Evidence_Complete(double designAirFlowRate_Lps, string heatRecoveryEfficiencyBasis, string specificFanPowerBasis)
        {
            PartOIteration3EquipmentEvidence result = new(
                Guid.NewGuid(), "MVHR-01", manufacturer, model, null, source,
                designAirFlowRate_Lps, 0.86, heatRecoveryEfficiencyBasis, false, null,
                0.62, specificFanPowerBasis, false, null,
                150.0, 150.0, 60.0, 60.0, null, null,
                "DesignAirFlow × constant yearly schedule 1.0",
                310.0, 310.0, 1.0, 1.0, 1.0,
                "Certified total-both-fans SFP split equally",
                "Declared assumption");

            Assert.True(result.BindAirSystem(Guid.NewGuid()));

            return result;
        }

        /// <summary>
        /// A persisted row that has lost its lookup coordinate or the basis of either certified figure cannot
        /// state what its numbers mean, so it is not complete - and a review refuses it.
        /// </summary>
        [Fact]
        public void Evidence_without_its_lookup_coordinate_or_bases_is_not_complete()
        {
            Assert.True(Evidence_Complete(60.0, "SupplyTemperatureEfficiency", "TotalBothFans").IsComplete);

            Assert.False(Evidence_Complete(double.NaN, "SupplyTemperatureEfficiency", "TotalBothFans").IsComplete);
            Assert.False(Evidence_Complete(60.0, null, "TotalBothFans").IsComplete);
            Assert.False(Evidence_Complete(60.0, "SupplyTemperatureEfficiency", null).IsComplete);
            Assert.False(Evidence_Complete(60.0, "Undefined", "TotalBothFans").IsComplete);
            Assert.False(Evidence_Complete(60.0, "SupplyTemperatureEfficiency", "Undefined").IsComplete);
        }

        [Fact]
        public void Two_units_of_the_same_selected_product_both_resolve_independently()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit_1 = Unit(adjacencyCluster, "MVHR-01");
            Select(adjacencyCluster, airHandlingUnit_1, new VentilationUnitReference(manufacturer, model, null));

            AirHandlingUnit airHandlingUnit_2 = Unit(adjacencyCluster, "MVHR-02");
            Select(adjacencyCluster, airHandlingUnit_2, new VentilationUnitReference(manufacturer, model, null));

            List<string> refusals = Query.PartOIteration3EquipmentResolution(adjacencyCluster, VentilationUnitCatalogue.Read(Catalogue()), out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, out List<PartOIteration3EquipmentEvidence> equipment, out List<string> _);

            Assert.Empty(refusals);
            Assert.Equal(2, unitSettings.Count);
            Assert.Equal(2, equipment.Count);
            Assert.Contains(airHandlingUnit_1.Guid, unitSettings.Keys);
            Assert.Contains(airHandlingUnit_2.Guid, unitSettings.Keys);
            Assert.True(equipment[0].Guid_AirHandlingUnit.CompareTo(equipment[1].Guid_AirHandlingUnit) < 0);
        }
    }
}
