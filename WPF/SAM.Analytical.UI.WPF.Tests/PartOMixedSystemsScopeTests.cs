// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>PR-1: the Mixed Design Systems route is scoped by identity, and Check design asks what Build &amp; Run asks.</b>
    ///
    /// <para><b>The production defect</b></para>
    /// <para>
    /// One cooled dwelling puts the whole mixed model on the TAS Systems route. The mixed SAM_Systems call was handed the
    /// WHOLE materialised cluster, and SAM_Systems requires every ventilation system it is handed to name an air handling
    /// unit - so the <c>AddMechanicalSystems</c> template's <c>NV 1</c> / <c>UV 1</c>, which by definition have none,
    /// refused a correct design ("names no air handling unit"). Every earlier Mixed fixture was system-free, so none
    /// could see it. The fixture here is the production shape: the real <c>Modify.AddMechanicalSystems</c> over the
    /// representative mixed block, beside a cooled dwelling.
    /// </para>
    ///
    /// <para><b>The boundary</b></para>
    /// <para>
    /// <c>MV 1</c> serves an unzoned plant room so SAM's authored-plant classification (PR-2) is not what is tested, and
    /// PR-1 is the caller building the right input; PR-3 (the last section) is SAM_Systems honouring the same scope when it
    /// is stated, so the whole cluster can no longer refuse. No TAS.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOMixedSystemsScopeTests
    {
        internal const string PlantRoom = "Plant Room";

        private static readonly VentilationUnitReference Reference = PartOMixedCoolingTests.Reference;

        internal static readonly VentilationUnitCapacityDescriptor Descriptor = PartOMixedCoolingTests.Descriptor;

        internal static VentilationUnitTemplate Template() => PartOMixedCoolingTests.Template();

        /// <summary>The system types the <c>AddMechanicalSystems</c> template names, resolved by name as SAM does.</summary>
        private static Core.SystemTypeLibrary SystemTypeLibrary(string type_Plant)
        {
            Core.SystemTypeLibrary result = new("PR-1 fixture");
            foreach (string name in new[] { "NV", "UV", type_Plant }.Distinct())
            {
                result.Add(new VentilationSystemType(name, name));
            }

            result.Add(new CoolingSystemType("AHU", "AHU"));
            result.Add(new CoolingSystemType("FCU", "FCU"));
            result.Add(new HeatingSystemType("RAD", "RAD"));

            return result;
        }

        /// <summary>
        /// The representative mixed block (Flat 01 natural, Flat 02 MVHR, Flat 03 MVHR cooled, a communal corridor) with
        /// the <c>AddMechanicalSystems</c> scaffolding a real project carries: dwellings natural (<c>NV 1</c>, no unit),
        /// the corridor uncontrolled (<c>UV 1</c>, no unit), an unzoned plant room mechanical (<c>MV 1</c> naming
        /// <c>AHU1</c>), cooling <c>FCU 1</c> / <c>AHU 1</c> and heating <c>RAD 1</c>. None carries a design terminal.
        /// </summary>
        internal static AnalyticalModel Scaffolded(string type_Plant = "MV", double? plantSupply_Lps = null, bool cooled = true)
        {
            AnalyticalModel analyticalModel = PartOMixedDesignFixture.Baseline();
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Space space_Plant = new(PlantRoom);
            space_Plant.SetValue(SpaceParameter.Area, 8.0);
            space_Plant.SetValue(SpaceParameter.Volume, 20.0);
            space_Plant.InternalCondition = new InternalCondition(PlantRoom + " IC");
            adjacencyCluster.AddObject(space_Plant);

            foreach (Space space in adjacencyCluster.GetSpaces())
            {
                InternalCondition internalCondition = space.InternalCondition is null ? new InternalCondition(space.Name + " IC") : new InternalCondition(space.InternalCondition);

                bool plant = space.Name == PlantRoom;
                internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, plant ? type_Plant : space.Name == PartOMixedDesignFixture.Corridor ? "UV" : "NV");
                internalCondition.SetValue(InternalConditionParameter.CoolingSystemTypeName, plant ? "AHU" : "FCU");
                internalCondition.SetValue(InternalConditionParameter.HeatingSystemTypeName, "RAD");

                space.InternalCondition = internalCondition;
                adjacencyCluster.AddObject(space);
            }

            Assert.NotEmpty(adjacencyCluster.AddMechanicalSystems(SystemTypeLibrary(type_Plant), null, "AHU1", "AHU1"));

            if (plantSupply_Lps is not null)
            {
                VentilationSystem ventilationSystem_Plant = adjacencyCluster.GetObjects<VentilationSystem>().Single(x => x.FullName == type_Plant + " 1");
                VentilationTerminal ventilationTerminal = new("Plant Room Supply", FlowClassification.Supply, plantSupply_Lps);
                adjacencyCluster.AddObject(ventilationTerminal);
                adjacencyCluster.AddRelation(ventilationSystem_Plant, ventilationTerminal);
                adjacencyCluster.AddRelation(ventilationTerminal, adjacencyCluster.GetSpaces().Single(x => x.Name == PlantRoom));
            }

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            return PartOMixedDesignFixture.WithStrategies(analyticalModel, x => x.Name switch
            {
                "Flat 01" => PartOMixedDesignFixture.Natural(x),
                "Flat 02" => new PartODwellingStrategy(x.Guid, PartOVentilationMode.MVHR, Reference),
                _ => new PartODwellingStrategy(x.Guid, PartOVentilationMode.MVHR, Reference, cooled ? PartOActiveCooling.SupplyAirCooling : PartOActiveCooling.None),
            });
        }

        internal static PartOMaterialisation Materialise(AnalyticalModel baseline)
        {
            PartOMaterialisation result = baseline.MaterialisePartODwellingStrategies([Descriptor], null, [Template()]);
            Assert.True(result.IsMaterialised, result.Refusal);

            return result;
        }

        private static string Json(AnalyticalModel analyticalModel) => analyticalModel.ToJsonObject().ToJsonString();

        /// <summary>Build &amp; Run's production path, stopped by cancellation the moment its Systems preflight is behind it.</summary>
        internal static PartOStrategySetRun BuildToThePreflight(AnalyticalModel baseline, PartOMixedDesignFixture.FakeSimulator izam)
        {
            using CancellationTokenSource cancellationTokenSource = new();

            Modify.BuildAndRunPartOMixedDesign(baseline, true, [Descriptor], PartOMixedDesignFixture.Context("Block_PR1"), cancellationTokenSource.Token, out PartOStrategySetRun partOStrategySetRun, izam.Simulate, cancellationTokenSource.Cancel, [Template()], null);

            return partOStrategySetRun;
        }

        // =================================================================================================
        // The production shape, and the defect it reproduces
        // =================================================================================================

        [Fact]
        public void Fixture_IsTheAddMechanicalSystemsShape_OnTheSystemsRoute()
        {
            AnalyticalModel baseline = Scaffolded();
            AdjacencyCluster adjacencyCluster = baseline.AdjacencyCluster;

            Assert.Equal(["MV 1", "NV 1", "UV 1"], adjacencyCluster.GetObjects<VentilationSystem>().ConvertAll(x => x.FullName).OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(adjacencyCluster.GetObjects<VentilationSystem>().FindAll(x => x.FullName != "MV 1"), x => Assert.True(string.IsNullOrEmpty(x.GetValue<string>(VentilationSystemParameter.SupplyUnitName))));
            Assert.Single(adjacencyCluster.GetObjects<AirHandlingUnit>(), x => x.Name == "AHU1");
            Assert.Equal(["AHU 1", "FCU 1"], adjacencyCluster.GetObjects<CoolingSystem>().ConvertAll(x => x.FullName).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(["RAD 1"], adjacencyCluster.GetObjects<HeatingSystem>().ConvertAll(x => x.FullName));

            PartOMaterialisation partOMaterialisation = Materialise(baseline);
            Assert.Equal(PartOSimulationRoute.Systems, partOMaterialisation.Route);
            Assert.Equal(2, partOMaterialisation.Record.VentilationSystemGuids.Count);
        }

        /// <summary>
        /// <b>Pre-fix proof.</b> Handing SAM_Systems the whole cluster - what the Mixed Systems route did before PR-1 - is
        /// refused by the unit-less NV / UV system. This is the owner's production refusal, reproduced headlessly.
        /// </summary>
        [Fact]
        public void TheWholeCluster_IsRefusedBySamSystems_ForAUnitLessNaturalOrUncontrolledSystem()
        {
            PartOMaterialisation partOMaterialisation = Materialise(Scaffolded());
            PartOMixedSystemsCall partOMixedSystemsCall = Query.PartOMixedSystemsCall(partOMaterialisation, [Template()]);
            Assert.True(partOMixedSystemsCall.IsValid, string.Join(" | ", partOMixedSystemsCall.Refusals));

            MechanicalVentilationMaterialisation unscoped = new PartOIteration3Pipeline().MaterialiseMixed(partOMaterialisation.AnalyticalModel.AdjacencyCluster, partOMixedSystemsCall.Spaces, partOMixedSystemsCall.GuidanceSettings);

            Assert.False(unscoped.IsMaterialised);
            Assert.Contains(unscoped.Refusals, x => x.Contains("names no air handling unit") && (x.Contains("'NV") || x.Contains("'UV")));
        }

        // =================================================================================================
        // The fix
        // =================================================================================================

        [Fact]
        public void ThePreflight_LeavesNVUVAndInertMVOutOfTheSamSystemsInput_AndMaterialisesOnlyThePartOSystems()
        {
            AnalyticalModel baseline = Scaffolded();
            PartOMaterialisation partOMaterialisation = Materialise(baseline);
            string json_Materialised = Json(partOMaterialisation.AnalyticalModel);

            PartOSystemsMaterialisationScope scope = Query.PartOMixedSystemsScope(partOMaterialisation);
            Assert.True(scope.IsScoped, scope.Refusal);

            List<Guid> guids_Built = [.. partOMaterialisation.Record.VentilationSystemGuids.Values.OrderBy(x => x)];
            Assert.Equal(guids_Built, scope.Guids_Retained);

            List<VentilationSystem> scaffolding = partOMaterialisation.AnalyticalModel.AdjacencyCluster.GetObjects<VentilationSystem>().FindAll(x => !guids_Built.Contains(x.Guid));
            Assert.Equal(["MV 1", "NV 1", "UV 1"], scaffolding.ConvertAll(x => x.FullName).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(scaffolding.ConvertAll(x => x.Guid).OrderBy(x => x), scope.Guids_Removed);
            Assert.Equal(guids_Built, scope.AdjacencyCluster.GetObjects<VentilationSystem>().ConvertAll(x => x.Guid).OrderBy(x => x));

            //The production preflight: the real SAM_Systems call over the scoped input.
            MechanicalVentilationMaterialisation? mixed = Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, [Template()], new PartOIteration3Pipeline(), out string? refusal, out List<string> notes);
            Assert.True(mixed is not null, refusal);

            //One air system per MVHR dwelling unit and nothing from NV / UV / MV or the cooling / heating templates.
            SystemPlantRoom systemPlantRoom = Assert.Single(mixed!.SystemEnergyCentre.GetSystemPlantRooms());
            Assert.Equal(2, systemPlantRoom.GetSystems<AirSystem>().Count);
            Assert.Single(mixed.GuidanceCoolings);

            //The evidence says what was left out, by the names an engineer sees.
            Assert.Contains(notes, x => x.Contains("'NV 1'"));
            Assert.Contains(notes, x => x.Contains("'UV 1'"));
            Assert.Contains(notes, x => x.Contains("'MV 1'"));

            //Derived state only: the materialised model still carries every authored system.
            Assert.Equal(json_Materialised, Json(partOMaterialisation.AnalyticalModel));
            Assert.Equal(5, partOMaterialisation.AnalyticalModel.AdjacencyCluster.GetObjects<VentilationSystem>().Count);
        }

        /// <summary>
        /// Membership is by identity. A template system given the familiar Part O label (<c>MVHR 1</c>, with a unit) is not
        /// the Part O design; the systems Part O built are, whatever they are called.
        /// </summary>
        [Fact]
        public void TheScope_FollowsTheRecordsIdentities_NotFamiliarLabels()
        {
            PartOMaterialisation partOMaterialisation = Materialise(Scaffolded(type_Plant: "MVHR"));

            VentilationSystem ventilationSystem_Template = partOMaterialisation.AnalyticalModel.AdjacencyCluster.GetObjects<VentilationSystem>().Single(x => x.FullName == "MVHR 1");
            Assert.DoesNotContain(ventilationSystem_Template.Guid, partOMaterialisation.Record.VentilationSystemGuids.Values);

            PartOSystemsMaterialisationScope scope = Query.PartOMixedSystemsScope(partOMaterialisation);

            Assert.True(scope.IsScoped, scope.Refusal);
            Assert.Contains(ventilationSystem_Template.Guid, scope.Guids_Removed);
            Assert.Equal(partOMaterialisation.Record.VentilationSystemGuids.Values.OrderBy(x => x), scope.Guids_Retained);
        }

        [Fact]
        public void TheCoolingAndHeatingTemplates_AreNotConsumedByTheVentilationScope()
        {
            PartOMaterialisation partOMaterialisation = Materialise(Scaffolded());
            AdjacencyCluster adjacencyCluster = partOMaterialisation.AnalyticalModel.AdjacencyCluster;

            List<Guid> guids_Template = [.. adjacencyCluster.GetObjects<CoolingSystem>().ConvertAll(x => x.Guid), .. adjacencyCluster.GetObjects<HeatingSystem>().ConvertAll(x => x.Guid)];
            Assert.Equal(3, guids_Template.Count);

            PartOSystemsMaterialisationScope scope = Query.PartOMixedSystemsScope(partOMaterialisation);

            Assert.True(scope.IsScoped, scope.Refusal);
            Assert.DoesNotContain(scope.Guids_Retained, guids_Template.Contains);
            Assert.DoesNotContain(scope.Guids_Removed, guids_Template.Contains);
            Assert.Equal(guids_Template.OrderBy(x => x), scope.AdjacencyCluster.GetObjects<CoolingSystem>().ConvertAll(x => x.Guid).Concat(scope.AdjacencyCluster.GetObjects<HeatingSystem>().ConvertAll(x => x.Guid)).OrderBy(x => x));
        }

        // =================================================================================================
        // Authored effective duty keeps refusing
        // =================================================================================================

        /// <summary>
        /// Iteration 3's safety rule, carried into Mixed unchanged: an authored mechanical system with a real 30 l/s duty in
        /// a room outside the dwellings would lose it on the Systems route (the no-IZAM source removes mechanical
        /// ventilation model-wide). SAM's materialisation does not judge the unzoned room, so the scope is the guard.
        /// </summary>
        [Fact]
        public void AnAuthoredMechanicalDutyOutsideTheDwellings_Refuses_AtCheckAndAtBuild_WithTheSameText()
        {
            AnalyticalModel baseline = Scaffolded(plantSupply_Lps: 30.0);
            string json_Baseline = Json(baseline);

            PartOMixedDesignCheck partOMixedDesignCheck = Modify.CheckPartOMixedDesign(baseline, [Descriptor], [Template()]);

            Assert.True(partOMixedDesignCheck.IsMaterialised, partOMixedDesignCheck.Materialisation?.Refusal);
            Assert.True(partOMixedDesignCheck.SystemsChecked);
            Assert.False(partOMixedDesignCheck.Passed);
            Assert.Contains("'MV 1'", partOMixedDesignCheck.Refusal_Systems);
            Assert.Contains("outside the assessed dwellings", partOMixedDesignCheck.Refusal_Systems);
            Assert.Contains("30 l/s", partOMixedDesignCheck.Refusal_Systems);
            Assert.DoesNotContain("names no air handling unit", partOMixedDesignCheck.Refusal_Systems);

            PartOMixedDesignFixture.FakeSimulator izam = new();
            PartOStrategySetRun partOStrategySetRun = BuildToThePreflight(baseline, izam);

            Assert.True(partOStrategySetRun.IsMaterialised);
            Assert.False(partOStrategySetRun.Simulation!.Cancelled);
            Assert.Equal(partOMixedDesignCheck.Refusal_Systems, partOStrategySetRun.Simulation.Refusal);
            Assert.Empty(izam.Models);

            Assert.Equal(json_Baseline, Json(baseline));
        }

        // =================================================================================================
        // Check == Build
        // =================================================================================================

        [Fact]
        public void AValidScope_PassesCheck_AndBuildGetsPastTheSamePreflight_WithTheSameNotes()
        {
            AnalyticalModel baseline = Scaffolded();
            string json_Baseline = Json(baseline);

            PartOMixedDesignCheck partOMixedDesignCheck = Modify.CheckPartOMixedDesign(baseline, [Descriptor], [Template()]);

            Assert.True(partOMixedDesignCheck.SystemsChecked);
            Assert.True(partOMixedDesignCheck.Passed, partOMixedDesignCheck.Refusal_Systems);
            Assert.Equal(4, partOMixedDesignCheck.Notes_Systems.Count);

            PartOMixedDesignFixture.FakeSimulator izam = new();
            PartOStrategySetRun partOStrategySetRun = BuildToThePreflight(baseline, izam);

            //Build's first step passed (no refusal); it stopped only because it was cancelled before TAS.
            Assert.True(partOStrategySetRun.IsMaterialised);
            Assert.True(partOStrategySetRun.Simulation!.Cancelled);
            Assert.Null(partOStrategySetRun.Simulation.Refusal);
            Assert.Equal(partOMixedDesignCheck.Notes_Systems, partOStrategySetRun.Simulation.Notes_SystemsScope);

            //Evidence, not warnings: the run's own warnings stay first in its notes.
            Assert.DoesNotContain(partOStrategySetRun.Simulation.Notes, x => x.Contains("left out of the TAS Systems materialisation input"));
            Assert.Empty(izam.Models);

            Assert.Equal(json_Baseline, Json(baseline));
        }

        /// <summary>The system-free fixture every earlier Mixed test used still passes both.</summary>
        [Fact]
        public void ASystemFreeCooledDesign_StillPassesCheck_AndTheSamePreflight()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => x.Name == "Flat 01" ? PartOMixedDesignFixture.Natural(x) : new PartODwellingStrategy(x.Guid, PartOVentilationMode.MVHR, Reference, PartOActiveCooling.SupplyAirCooling));

            PartOMixedDesignCheck partOMixedDesignCheck = Modify.CheckPartOMixedDesign(baseline, [Descriptor], [Template()]);

            Assert.True(partOMixedDesignCheck.SystemsChecked);
            Assert.True(partOMixedDesignCheck.Passed, partOMixedDesignCheck.Refusal_Systems);

            PartOStrategySetRun partOStrategySetRun = BuildToThePreflight(baseline, new PartOMixedDesignFixture.FakeSimulator());
            Assert.True(partOStrategySetRun.Simulation!.Cancelled);
            Assert.Null(partOStrategySetRun.Simulation.Refusal);
        }

        /// <summary>An uncooled design stays on the IZAM route, where no SAM_Systems graph is built - by Check or by Build.</summary>
        [Fact]
        public void AnUncooledDesign_HasNoSystemsPreflight()
        {
            PartOMixedDesignCheck partOMixedDesignCheck = Modify.CheckPartOMixedDesign(Scaffolded(cooled: false), [Descriptor], [Template()]);

            Assert.True(partOMixedDesignCheck.Passed, partOMixedDesignCheck.Materialisation?.Refusal);
            Assert.False(partOMixedDesignCheck.SystemsChecked);
            Assert.Equal(PartOSimulationRoute.Izam, partOMixedDesignCheck.Materialisation!.Route);
        }

        // =================================================================================================
        // PR-3: SAM_Systems honours the stated scope
        // =================================================================================================

        /// <summary>
        /// The whole materialised cluster - <c>NV 1</c>, <c>UV 1</c>, <c>MV 1 → AHU1</c> and all - handed to SAM_Systems with
        /// SAM's retained scope materialises the two Part O systems only, exactly as the working copy does. The model's
        /// other systems are never read, so the cluster that refuses unscoped (above) no longer can.
        /// </summary>
        [Fact]
        public void TheWholeCluster_WithSamsScope_MaterialisesOnlyThePartOSystems_ExactlyAsTheWorkingCopy()
        {
            PartOMaterialisation partOMaterialisation = Materialise(Scaffolded());
            AdjacencyCluster adjacencyCluster = partOMaterialisation.AnalyticalModel.AdjacencyCluster;
            string json_Materialised = Json(partOMaterialisation.AnalyticalModel);

            PartOMixedSystemsCall partOMixedSystemsCall = Query.PartOMixedSystemsCall(partOMaterialisation, [Template()]);
            PartOSystemsMaterialisationScope scope = Query.PartOMixedSystemsScope(partOMaterialisation);
            Assert.True(scope.IsScoped, scope.Refusal);

            MechanicalVentilationMaterialisation whole = new PartOIteration3Pipeline().MaterialiseMixed(adjacencyCluster, partOMixedSystemsCall.Spaces, partOMixedSystemsCall.GuidanceSettings, scope.Guids_Retained);
            MechanicalVentilationMaterialisation workingCopy = new PartOIteration3Pipeline().MaterialiseMixed(scope.AdjacencyCluster, partOMixedSystemsCall.Spaces, partOMixedSystemsCall.GuidanceSettings);

            Assert.True(whole.IsMaterialised, string.Join(" | ", whole.Refusals));
            Assert.True(workingCopy.IsMaterialised, string.Join(" | ", workingCopy.Refusals));

            //Only the Part O units: AHU1 stays on the model and is not materialised.
            List<Guid> guids_Unit = [.. whole.Bindings.Where(x => x.BindingType == MechanicalVentilationBindingType.AirSystem).Select(x => x.Guid_Analytical).OrderBy(x => x)];
            Assert.Equal(partOMaterialisation.Record.VentilationSystemGuids.Values.Select(x => adjacencyCluster.AirHandlingUnit(adjacencyCluster.GetObject<VentilationSystem>(x))!.Guid).OrderBy(x => x), guids_Unit);
            Assert.DoesNotContain(adjacencyCluster.GetObjects<AirHandlingUnit>().Single(x => x.Name == "AHU1").Guid, guids_Unit);

            //The same graph, the same identities.
            Assert.Equal(workingCopy.SystemEnergyCentre.ToJsonObject().ToJsonString(), whole.SystemEnergyCentre.ToJsonObject().ToJsonString());
            Assert.Equal(workingCopy.Bindings.Select(x => x.ToString()), whole.Bindings.Select(x => x.ToString()));

            //Iteration 3's Materialise forwards the scope the same way.
            Assert.False(new PartOIteration3Pipeline().Materialise(adjacencyCluster, partOMixedSystemsCall.Spaces).IsMaterialised);
            MechanicalVentilationMaterialisation whole_Iteration3 = new PartOIteration3Pipeline().Materialise(adjacencyCluster, partOMixedSystemsCall.Spaces, null, null, null, scope.Guids_Retained);
            Assert.True(whole_Iteration3.IsMaterialised, string.Join(" | ", whole_Iteration3.Refusals));
            Assert.Equal(new PartOIteration3Pipeline().Materialise(scope.AdjacencyCluster, partOMixedSystemsCall.Spaces).SystemEnergyCentre.ToJsonObject().ToJsonString(), whole_Iteration3.SystemEnergyCentre.ToJsonObject().ToJsonString());

            Assert.Equal(json_Materialised, Json(partOMaterialisation.AnalyticalModel));
        }

        /// <summary>The ONE preflight (Check and Build) tells SAM_Systems exactly the systems SAM's record says Part O built.</summary>
        [Fact]
        public void ThePreflight_TellsSamSystemsExactlyTheSystemsPartOBuilt()
        {
            PartOMaterialisation partOMaterialisation = Materialise(Scaffolded());
            List<Guid>? guids_Told = null;
            bool told = false;
            PartOIteration3Pipeline capturingPipeline = new() { ScopeObserver = x => { told = true; guids_Told = x is null ? null : [.. x.OrderBy(g => g)]; } };

            MechanicalVentilationMaterialisation? mixed = Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, [Template()], capturingPipeline, out string? refusal, out List<string> _);

            Assert.True(mixed is not null, refusal);
            Assert.True(told);
            Assert.NotNull(guids_Told);
            Assert.Equal(partOMaterialisation.Record.VentilationSystemGuids.Values.OrderBy(x => x), guids_Told);
        }

        /// <summary>
        /// Stating the scope added members; it replaced none. The pre-PR-3 public signatures still exist, no production
        /// method is virtual for a test's sake, and the interface's original member stays the one an implementer must write
        /// (the scoped one has a default body), so an implementation that predates the scope still binds.
        /// </summary>
        [Fact]
        public void TheScopeIsAddedNotSubstituted_PublicSignaturesAreUnchanged()
        {
            Type type = typeof(PartOIteration3Pipeline);
            Type[] types_Materialise = [typeof(AdjacencyCluster), typeof(IEnumerable<Space>), typeof(IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings>), typeof(IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings>), typeof(IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings>)];

            Assert.NotNull(type.GetMethod(nameof(PartOIteration3Pipeline.Materialise), types_Materialise));
            System.Reflection.MethodInfo? mixed = type.GetMethod(nameof(PartOIteration3Pipeline.MaterialiseMixed), [typeof(AdjacencyCluster), typeof(IEnumerable<Space>), typeof(IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings>)]);
            Assert.NotNull(mixed);
            Assert.DoesNotContain(type.GetMethods(), x => x.DeclaringType == type && x.IsVirtual && !x.IsFinal);

            System.Reflection.MethodInfo? original = typeof(IPartOIteration3Pipeline).GetMethod(nameof(IPartOIteration3Pipeline.Materialise), types_Materialise);
            System.Reflection.MethodInfo? scoped = typeof(IPartOIteration3Pipeline).GetMethod(nameof(IPartOIteration3Pipeline.Materialise), [.. types_Materialise, typeof(IEnumerable<Guid>)]);
            Assert.True(original!.IsAbstract);
            Assert.False(scoped!.IsAbstract);
        }

        /// <summary>A materialisation refusal is still reported by SAM's own structured refusals, and no preflight runs.</summary>
        [Fact]
        public void AMaterialisationRefusal_IsSamsAndSkipsThePreflight()
        {
            PartOMixedDesignCheck partOMixedDesignCheck = Modify.CheckPartOMixedDesign(Scaffolded(), [Descriptor], null);

            Assert.False(partOMixedDesignCheck.IsMaterialised);
            Assert.False(partOMixedDesignCheck.SystemsChecked);
            Assert.Null(partOMixedDesignCheck.Refusal_Systems);
            Assert.NotEmpty(partOMixedDesignCheck.Materialisation!.Refusals);
        }
    }
}
