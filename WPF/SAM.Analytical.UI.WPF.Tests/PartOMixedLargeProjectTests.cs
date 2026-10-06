// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Mixed Part O PR4 (SAM PR0 §F, SAM_UI PR3A §14.1): the completed mixed design - Natural, uncooled MVHR and cooled
    /// MVHR dwellings - at the target scale of 500 dwellings of 10 spaces (5,000 spaces), with no TAS. Every layer before
    /// TAS is timed separately: the session, SAM's materialisation (with the product templates), the ONE SAM_Systems graph
    /// the Systems route builds, and the evidence check a reopened project runs.
    /// <para>
    /// What must hold exactly is structural: one analytical model, a truthful scenario per dwelling, manufacturer-guidance
    /// cooling (one DX coil) on exactly the cooled dwellings' air systems, and no growth of the saved or materialised model
    /// however often the design is rebuilt. The time bounds are loose sanity limits for a CI machine, not budgets; the
    /// measured times are written to the test output.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOMixedLargeProjectTests
    {
        private const int Count_Dwelling = 500;
        private const int SpacesPerDwelling = 10;

        private readonly ITestOutputHelper testOutputHelper;

        public PartOMixedLargeProjectTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        private static PartOMixedDesignSession Session(AnalyticalModel analyticalModel)
        {
            PartOMixedDesignSession result = new(analyticalModel, null, [PartOMixedCoolingTests.Descriptor], null, [PartOMixedCoolingTests.Template()]) { SimulationCaseKey = PartOMixedDesignFixture.CaseKey };
            result.CatalogueOffered = true;
            return result;
        }

        private static PartOMaterialisation Materialise(AnalyticalModel analyticalModel) => analyticalModel.MaterialisePartODwellingStrategies([PartOMixedCoolingTests.Descriptor], null, [PartOMixedCoolingTests.Template()]);

        private static int Length(AnalyticalModel analyticalModel) => analyticalModel.ToJsonObject().ToJsonString().Length;

        /// <summary>The replicated project the licensed PR4 acceptance runs on: every block its own rooms, zones and guids.</summary>
        [Fact]
        public void Replicate_MakesIndependentBlocks_ThatSamBuildsLikeTheOriginal()
        {
            AnalyticalModel source = PartOMixedDesignFixture.Baseline(3);
            AnalyticalModel large = PartOMixedLargeProjectFixture.Replicate(source, 4);

            List<Space> spaces = large.AdjacencyCluster.GetSpaces();
            List<Zone> zones = large.AdjacencyCluster.GetZones();
            Assert.Equal(4 * source.AdjacencyCluster.GetSpaces().Count, spaces.Count);
            Assert.Equal(4 * source.AdjacencyCluster.GetZones().Count, zones.Count);
            Assert.Equal(4 * source.AdjacencyCluster.GetPanels().Count, large.AdjacencyCluster.GetPanels().Count);
            Assert.Equal(spaces.Count, spaces.Select(x => x.Name).Distinct().Count());
            Assert.Equal(spaces.Count, spaces.Select(x => x.Guid).Distinct().Count());
            Assert.Equal("Flat 02", PartOMixedLargeProjectFixture.Original("Flat 02 #3"));
            Assert.Equal(3, PartOMixedLargeProjectFixture.Block("Flat 02 #3"));
            Assert.Equal(0, PartOMixedLargeProjectFixture.Block("Flat 02"));

            //Each copied room keeps its own dwelling and its own Part F terminal, renamed with it.
            foreach (Space space in spaces)
            {
                Zone zone = Assert.Single(large.AdjacencyCluster.GetRelatedObjects<Zone>(space));
                Assert.Equal(PartOMixedLargeProjectFixture.Block(space.Name), PartOMixedLargeProjectFixture.Block(zone.Name));

                PartFSpaceData? partFSpaceData = space.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData);
                partFSpaceData?.Terminals.ForEach(x => Assert.Equal((x.SpaceGuid, x.SpaceName), (space.Guid, space.Name)));
            }

            Assert.True(large.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> refusals), string.Join(" | ", refusals?.Select(x => x.Message) ?? []));

            //SAM builds every block as it builds the original.
            AnalyticalModel withStrategies = PartOMixedDesignFixture.WithStrategies(large, x => PartOMixedLargeProjectFixture.Original(x.Name) switch
            {
                PartOMixedDesignFixture.Corridor => null, //a common zone is never a strategy row
                "Flat 01" => PartOMixedDesignFixture.Natural(x),
                _ => PartOMixedDesignFixture.Mvhr(x),
            });
            PartOMaterialisation partOMaterialisation = withStrategies.MaterialisePartODwellingStrategies();
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);
            Assert.Equal(4 * 2, partOMaterialisation.VentilationSystems.Count);

            //The source is untouched.
            Assert.Equal(3 * 2 + 1, source.AdjacencyCluster.GetSpaces().Count);
        }

        [Fact]
        public void MixedCooledDesign_At5000Spaces_IsOneModelWithCoolingOnlyWhereIntended_AndDoesNotGrow()
        {
            AnalyticalModel baseline_Clean = PartOMixedDesignFixture.Baseline(Count_Dwelling, corridor: true, spacesPerFlat: SpacesPerDwelling);
            Assert.Equal(Count_Dwelling * SpacesPerDwelling + 1, baseline_Clean.AdjacencyCluster.GetSpaces().Count);
            string json_Clean = baseline_Clean.ToJsonObject().ToJsonString();

            // ---- Session: thirds of Natural / uncooled MVHR / cooled MVHR, by bulk assignment ----
            Stopwatch stopwatch = Stopwatch.StartNew();
            PartOMixedDesignSession session = Session(baseline_Clean);
            TimeSpan open = stopwatch.Elapsed;
            Assert.Equal(Count_Dwelling, session.Rows.Count);

            List<PartOMixedDwellingRow> natural = [.. session.Rows.Where((x, i) => i % 3 == 0)];
            List<PartOMixedDwellingRow> cooled = [.. session.Rows.Where((x, i) => i % 3 == 2)];
            List<PartOMixedDwellingRow> mvhr = [.. session.Rows.Where((x, i) => i % 3 == 1)];

            stopwatch.Restart();
            Assert.Null(session.SetMvhr(session.Rows, PartOMixedCoolingTests.Reference));
            Assert.Null(session.SetNatural(natural));
            Assert.Null(session.SetCooling(cooled, true));
            TimeSpan assign = stopwatch.Elapsed;

            PartOMixedReadiness readiness = session.Readiness();
            Assert.Equal(natural.Count, readiness.Natural);
            Assert.Equal(mvhr.Count + cooled.Count, readiness.Mvhr);

            AnalyticalModel baseline = session.WithSelection();
            PartODwellingStrategySet selected = baseline.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            foreach (PartOMixedDwellingRow row in cooled)
            {
                PartODwellingStrategy strategy = selected.Strategy(row.ZoneGuid);
                strategy.CoolingStatSpaceGuid = session.CoolingControlRooms(row).Single(x => x.Name.EndsWith(" Bedroom", StringComparison.Ordinal)).Guid;
                selected.Set(strategy);
            }
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, selected);
            session.Rebase(baseline);
            int length_Saved = Length(baseline);

            // ---- SAM: ONE complete mixed model, on the Systems route, a truthful scenario per dwelling ----
            stopwatch.Restart();
            PartOMaterialisation partOMaterialisation = Materialise(baseline);
            TimeSpan materialise = stopwatch.Elapsed;

            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);
            Assert.Equal(PartOSimulationRoute.Systems, partOMaterialisation.Route);
            Assert.Equal(mvhr.Count + cooled.Count, partOMaterialisation.VentilationSystems.Count);
            Assert.Equal(cooled.Count, partOMaterialisation.Record!.CooledDwellings.Count);
            Assert.Equal([.. cooled.Select(x => x.ZoneGuid).OrderBy(x => x)], [.. partOMaterialisation.Record.CooledDwellings.Select(x => x.ZoneGuid).OrderBy(x => x)]);

            Dictionary<Guid, PartOIteration> iteration_By_Zone = partOMaterialisation.OverheatingScenarios.ToDictionary(x => x.ZoneGuid, x => x.Iteration);
            Assert.All(natural, x => Assert.Equal(PartOIteration.BaseNaturalVentilation, iteration_By_Zone[x.ZoneGuid]));
            Assert.All(mvhr, x => Assert.Equal(PartOIteration.BasePassive, iteration_By_Zone[x.ZoneGuid]));
            Assert.All(cooled, x => Assert.Equal(PartOIteration.ActiveTrimCooling, iteration_By_Zone[x.ZoneGuid]));
            Assert.Equal(PartOIteration.DwellingIndependent, iteration_By_Zone[PartOMixedDesignFixture.Zone(baseline, PartOMixedDesignFixture.Corridor).Guid]);

            // ---- SAM_Systems: ONE graph; manufacturer guidance and a DX coil on exactly the cooled units' air systems ----
            stopwatch.Restart();
            MechanicalVentilationMaterialisation? mechanicalVentilationMaterialisation = Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, [PartOMixedCoolingTests.Template()], new PartOIteration3Pipeline(), out string? refusal);
            TimeSpan systems = stopwatch.Elapsed;
            Assert.True(mechanicalVentilationMaterialisation is not null, refusal);

            SystemPlantRoom systemPlantRoom = Assert.Single(mechanicalVentilationMaterialisation!.SystemEnergyCentre.GetSystemPlantRooms());
            Assert.Equal(cooled.Count, mechanicalVentilationMaterialisation.GuidanceCoolings.Count);

            HashSet<Guid> guids_Unit_Cooled = [.. partOMaterialisation.Record.CooledDwellings.Select(x => x.AirHandlingUnitGuid)];
            List<MechanicalVentilationBinding> bindings_AirSystem = [.. mechanicalVentilationMaterialisation.Bindings.Where(x => x.BindingType == MechanicalVentilationBindingType.AirSystem)];
            Assert.Equal(mvhr.Count + cooled.Count, bindings_AirSystem.Count);

            stopwatch.Restart();
            Dictionary<Guid, AirSystem> airSystem_By_Guid = (systemPlantRoom.GetSystems<AirSystem>() ?? []).ToDictionary(x => x.Guid);
            int coils_Cooled = 0, coils_Elsewhere = 0;
            foreach (MechanicalVentilationBinding binding in bindings_AirSystem)
            {
                int coils = (systemPlantRoom.GetSystemComponents<ISystemComponent>(airSystem_By_Guid[binding.Guid_Systems]) ?? []).Count(x => x is SystemDXCoil);
                if (guids_Unit_Cooled.Contains(binding.Guid_Analytical))
                {
                    Assert.Equal(1, coils);
                    coils_Cooled += coils;
                }
                else
                {
                    coils_Elsewhere += coils;
                }
            }
            TimeSpan coilCount = stopwatch.Elapsed;

            Assert.Equal(cooled.Count, coils_Cooled);
            Assert.Equal(0, coils_Elsewhere);

            // ---- Build & Run at scale (TAS stand-in): the evidence a reopened project checks, and its cost ----
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOStrategySetSimulation Systems(PartOMaterialisation materialisation, IReadOnlyList<VentilationUnitTemplate>? templates, PartOSimulationContext context, CancellationToken cancellationToken)
            {
                PartOStrategySetSimulation result = fakeSimulator.Simulate(materialisation.AnalyticalModel, [.. materialisation.OverheatingScenarios], context, cancellationToken);
                result.Route = PartOSimulationRoute.Systems;
                result.Path_TPD = System.IO.Path.ChangeExtension(result.Path_TSD, ".tpd");
                return result;
            }

            stopwatch.Restart();
            PartOMixedRunEvidence? evidence = Modify.BuildAndRunPartOMixedDesign(baseline, true, [PartOMixedCoolingTests.Descriptor], PartOMixedDesignFixture.Context("Block_Large"), CancellationToken.None, out PartOStrategySetRun run, fakeSimulator.Simulate, null, [PartOMixedCoolingTests.Template()], Systems);
            TimeSpan build = stopwatch.Elapsed;
            Assert.NotNull(evidence);
            Assert.Equal(PartOSimulationRoute.Systems, evidence!.Route);
            Assert.Empty(fakeSimulator.Models.Skip(1)); //ONE simulation of ONE model - never a hybrid second route

            stopwatch.Restart();
            Assert.True(evidence.IsCurrent(baseline, [PartOMixedCoolingTests.Descriptor], [PartOMixedCoolingTests.Template()], out string? reason), reason);
            TimeSpan current = stopwatch.Elapsed;

            // ---- No growth: rebuilding, turning cooling off and on again, never grows either model ----
            int length_Materialised = Length(partOMaterialisation.AnalyticalModel);
            Assert.Equal(length_Materialised, Length(Materialise(baseline).AnalyticalModel));

            Assert.Null(session.SetCooling(cooled, false));
            AnalyticalModel baseline_Uncooled = session.WithSelection();
            PartOMaterialisation uncooled = Materialise(baseline_Uncooled);
            Assert.True(uncooled.IsMaterialised, uncooled.Refusal);
            Assert.Equal(PartOSimulationRoute.Izam, uncooled.Route);
            Assert.Empty(uncooled.Record!.CooledDwellings);
            Assert.DoesNotContain(uncooled.OverheatingScenarios, x => x.Iteration == PartOIteration.ActiveTrimCooling);
            Assert.All(uncooled.AnalyticalModel.AdjacencyCluster.GetObjects<AirHandlingUnit>() ?? [], x => Assert.True(double.IsNaN(x.SummerSupplyTemperature)));
            Assert.False(evidence.IsCurrent(baseline_Uncooled, [PartOMixedCoolingTests.Descriptor], [PartOMixedCoolingTests.Template()], out _));

            Assert.Null(session.SetCooling(cooled, true));
            AnalyticalModel baseline_Again = session.WithSelection();
            Assert.Equal(length_Saved, Length(baseline_Again));
            Assert.Equal(length_Materialised, Length(Materialise(baseline_Again).AnalyticalModel));
            Assert.True(evidence.IsCurrent(baseline_Again, [PartOMixedCoolingTests.Descriptor], [PartOMixedCoolingTests.Template()], out reason), reason);

            //The clean source is never touched.
            Assert.Equal(json_Clean, baseline_Clean.ToJsonObject().ToJsonString());

            testOutputHelper.WriteLine(
                "5,000 spaces / 500 dwellings ({0} Natural, {1} MVHR, {2} MVHR + cooling): session open {3:0} ms; bulk assignment {4:0} ms; SAM materialisation {5:0.0} s; SAM_Systems graph ({6} air systems, {7} cooled) {8:0.0} s; DX-coil check {9:0} ms; Build & Run with TAS stand-in {10:0.0} s; evidence current check {11:0.0} s; saved model {12:N0} chars, materialised {13:N0} chars (unchanged over rebuilds).",
                natural.Count, mvhr.Count, cooled.Count, open.TotalMilliseconds, assign.TotalMilliseconds, materialise.TotalSeconds, bindings_AirSystem.Count, coils_Cooled, systems.TotalSeconds, coilCount.TotalMilliseconds, build.TotalSeconds, current.TotalSeconds, length_Saved, length_Materialised);

            Assert.True(materialise < TimeSpan.FromMinutes(5), materialise.ToString());
            Assert.True(systems < TimeSpan.FromMinutes(5), systems.ToString());
            Assert.True(current < TimeSpan.FromMinutes(2), current.ToString());
        }
    }
}
