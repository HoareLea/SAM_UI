// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    [Collection(WpfCollection.Name)]
    public class PartOIteration3RoomBindingTests : IDisposable
    {
        private readonly string directory = PartOIteration3Fixture.Directory_Temp();

        public void Dispose()
        {
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
        }

        private (PartORun Run, VentilationUnitCatalogue Catalogue, List<Zone> Zones, List<AirHandlingUnit> Units, List<Space> Stats) Prepared(int dwellingCount = 3, bool alternateStatRooms = false)
        {
            VentilationUnitCatalogue catalogue = VentilationUnitCatalogue.Read();
            Assert.Equal(VentilationUnitCatalogueState.Selectable, catalogue.State);
            VentilationUnitTemplate template = catalogue.Templates.Single(x => x.MechanicalVentilationGuidanceSettings(out string _) is not null);

            AdjacencyCluster cluster = new();
            List<Zone> zones = [];
            List<AirHandlingUnit> units = [];
            List<Space> stats = [];
            List<Guid> systemGuids = [];
            PartODwellingStrategySet selections = new();

            for (int i = 0; i < dwellingCount; i++)
            {
                VentilationSystem system = PartOIteration3Fixture.VentilationSystem(cluster, $"Unit {i}", "MVHR", out AirHandlingUnit unit);
                Space stat = PartOIteration3Fixture.Space(cluster, $"Control {i}");
                Space extract = PartOIteration3Fixture.Space(cluster, $"Extract {i}");
                PartOIteration3Fixture.Terminal(cluster, system, stat, FlowClassification.Supply, 30);
                Space alternateStat = alternateStatRooms && i % 2 == 1
                    ? PartOIteration3Fixture.Space(cluster, $"Alternate control {i}") : null;
                if (alternateStat is not null)
                {
                    PartOIteration3Fixture.Terminal(cluster, system, alternateStat, FlowClassification.Supply, 10);
                    cluster.AddRelation(system, alternateStat);
                }
                PartOIteration3Fixture.Terminal(cluster, system, extract, FlowClassification.Extract, alternateStat is null ? 30 : 40);
                cluster.AddRelation(system, stat);
                cluster.AddRelation(system, extract);
                Zone zone = alternateStat is null
                    ? PartOIteration3Fixture.Zone(cluster, $"Dwelling {i}", stat, extract)
                    : PartOIteration3Fixture.Zone(cluster, $"Dwelling {i}", stat, alternateStat, extract);

                unit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, template.VentilationUnitReference);
                cluster.AddObject(unit);
                Space selectedStat = alternateStat ?? stat;
                selections.Set(new PartODwellingStrategy(zone.Guid, PartOVentilationMode.MVHR,
                    template.VentilationUnitReference, PartOActiveCooling.SupplyAirCooling)
                { CoolingStatSpaceGuid = selectedStat.Guid });
                zones.Add(zone);
                units.Add(unit);
                stats.Add(selectedStat);
                systemGuids.Add(system.Guid);
            }

            AnalyticalModel prepared = PartOIteration3Fixture.Model(cluster);
            prepared.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, selections);
            PartORun run = new();
            Assert.True(run.Prepare(prepared, PartOIteration3Fixture.Scenarios(),
                new PartOPreparationContext(PartOIteration.BasePassive, zones, null, null), systemGuids));

            string path = Path.Combine(directory, "reference.tsd");
            Assert.True(run.ExpectResults(path));
            File.WriteAllText(path, "reference result");
            AnalyticalModel result = PartOIteration3Fixture.Model(new AdjacencyCluster(cluster));
            result.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, selections);
            Assert.True(run.Complete(result, path, PartOIteration3Fixture.SimulationContext(directory), out string refusal), refusal);
            return (run, catalogue, zones, units, stats);
        }

        [WpfFact]
        public void Three_saved_rooms_reach_guidance_preflight_materialisation_input_and_evidence()
        {
            var test = Prepared();
            PartOIteration3Eligibility eligibility = Query.PartOIteration3Eligibility(test.Run, test.Run.IsAssessable(out string refusal), refusal);
            Assert.True(eligibility.CanRun, eligibility.Refusal_Run);
            PartOIteration3Preflight preflight = Query.PartOIteration3Preflight(test.Run, eligibility,
                PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, test.Catalogue);
            Assert.True(preflight.CanRun, string.Join(" | ", preflight.Refusals));

            AdjacencyCluster scoped = Query.PartOIteration3ScopedCluster(test.Run, out List<string> scopeRefusals);
            Assert.Empty(scopeRefusals);
            PartODwellingStrategySet saved = test.Run.AnalyticalModel_Prepared.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            Assert.Empty(Query.PartOIteration3GuidanceResolution(scoped, test.Run.PreparationContext.Zones, saved,
                test.Catalogue, out Dictionary<Guid, MechanicalVentilationGuidanceSettings> settings,
                out List<string> _, out List<PartOIteration3GuidanceEvidence> evidence));

            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(test.Stats[i].Guid, settings[test.Units[i].Guid].CoolingStatSpaceGuid);
                Assert.Equal(test.Stats[i].Guid, evidence.Single(x => x.Guid_AirHandlingUnit == test.Units[i].Guid).Guid_CoolingStatSpace);
            }

            MechanicalVentilationMaterialisation materialised = new PartOIteration3Pipeline().Materialise(scoped,
                test.Zones.SelectMany(x => PartOIteration3Fixture.Spaces(scoped, x)), null, null, settings);
            Assert.True(materialised.IsMaterialised, string.Join(" | ", materialised.Refusals));
            Assert.Equal(3, settings.Count);
            Assert.Equal(3, evidence.Count);
        }

        [WpfTheory]
        [InlineData(1)]
        [InlineData(4)]
        public void ChangedDwellingCountAndStatRoomChoices_ReachGuidanceAndMaterialisation(int dwellingCount)
        {
            var test = Prepared(dwellingCount, alternateStatRooms: true);
            PartOIteration3Eligibility eligibility = Query.PartOIteration3Eligibility(test.Run,
                test.Run.IsAssessable(out string refusal), refusal);
            PartOIteration3Preflight preflight = Query.PartOIteration3Preflight(test.Run, eligibility,
                PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, test.Catalogue);
            Assert.True(preflight.CanRun, string.Join(" | ", preflight.Refusals));

            AdjacencyCluster scoped = Query.PartOIteration3ScopedCluster(test.Run, out List<string> scopeRefusals);
            Assert.Empty(scopeRefusals);
            PartODwellingStrategySet selections = test.Run.AnalyticalModel_Prepared.GetValue<PartODwellingStrategySet>(
                Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            Assert.Empty(Query.PartOIteration3GuidanceResolution(scoped, test.Zones, selections, test.Catalogue,
                out Dictionary<Guid, MechanicalVentilationGuidanceSettings> settings,
                out List<string> _, out List<PartOIteration3GuidanceEvidence> evidence));

            Assert.Equal(dwellingCount, evidence.Count);
            for (int i = 0; i < dwellingCount; i++)
            {
                Assert.Equal(test.Stats[i].Guid, settings[test.Units[i].Guid].CoolingStatSpaceGuid);
                Assert.Equal(test.Stats[i].Guid, evidence.Single(x => x.Guid_AirHandlingUnit == test.Units[i].Guid).Guid_CoolingStatSpace);
            }

            MechanicalVentilationMaterialisation materialised = new PartOIteration3Pipeline().Materialise(scoped,
                test.Zones.SelectMany(x => PartOIteration3Fixture.Spaces(scoped, x)), null, null, settings);
            Assert.True(materialised.IsMaterialised, string.Join(" | ", materialised.Refusals));
        }

        [WpfTheory]
        [InlineData("missing")]
        [InlineData("unknown")]
        [InlineData("other dwelling")]
        public void Invalid_room_refuses_preflight_and_disables_Run(string caseName)
        {
            var test = Prepared();
            Guid invalid = caseName switch
            {
                "missing" => Guid.Empty,
                "unknown" => Guid.NewGuid(),
                _ => test.Stats[1].Guid,
            };
            PartODwellingStrategySet saved = test.Run.AnalyticalModel_Prepared.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            PartODwellingStrategy strategy = saved.Strategy(test.Zones[0].Guid);
            strategy.CoolingStatSpaceGuid = invalid;
            saved.Set(strategy);
            test.Run.AnalyticalModel_Prepared.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, saved);

            PartOIteration3Eligibility eligibility = Query.PartOIteration3Eligibility(test.Run, test.Run.IsAssessable(out string refusal), refusal);
            PartOIteration3Preflight preflight = Query.PartOIteration3Preflight(test.Run, eligibility,
                PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, test.Catalogue);
            Assert.False(preflight.CanRun);
            Assert.Contains(preflight.Refusals, x => x.Contains(test.Units[0].Name) && x.Contains("cooling control room"));

            PartOWorkflowWindow window = new()
            {
                PartORun = test.Run,
                VentilationUnitCatalogue = test.Catalogue,
                Iteration3Eligibility = eligibility,
                Iteration3Mode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
            };
            window.CompleteInitialisation();
            Assert.False(window.CanRunIteration3);
            Assert.Contains("cooling control room", window.Iteration3RefusalText);
        }
    }
}
