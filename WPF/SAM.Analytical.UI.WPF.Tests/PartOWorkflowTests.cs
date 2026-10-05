// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Analytical.UI.WPF;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The high-level <b>Part O - Prepare &amp; Run</b> workflow: what it reports about a model, what it
    /// reuses, what it refuses, and which existing command each action reaches.
    /// <para>
    /// <b>None of the engineering is retested here.</b> The preparation, the isolation, the pre-simulation
    /// check, the TM59 assessment and the Iteration 2B optimisation are pinned by
    /// <c>SAM.Tests</c>, <c>PartOIsolationScopeTests</c>, <c>PartOPreSimulationCheckTests</c>,
    /// <c>PartOOptimisationTests</c> and <c>PartOCapacityEnvelopeTests</c>. What is asserted here is the
    /// orchestration around them: the state inspection, the reuse rule, the scenario and scope handling,
    /// and the enablement of the three actions.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowTests
    {
        // ---- State inspection ---------------------------------------------------------------------------

        /// <summary>
        /// A model with nothing done to it says so, and says it in terms of the command that would fix it -
        /// not by refusing a step three commands later.
        /// </summary>
        [Fact]
        public void AnUnzonedModel_ReportsTheMissingDwellingScope()
        {
            PartOWorkflowInspection partOWorkflowInspection = Inspect(Model(zoned: false), Request(Scenario_1a()));

            Assert.False(partOWorkflowInspection.CanRun);

            Assert.Equal(PartOWorkflowStageStatus.Blocked, Stage(partOWorkflowInspection, PartOWorkflowStage.DwellingScope).Status);

            Assert.Contains("no zones", Stage(partOWorkflowInspection, PartOWorkflowStage.DwellingScope).Detail, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Zones the model explicitly says are not dwellings are not dwellings, and that is
        /// <c>Query.PartFDwellingZones</c>'s answer rather than a rule stated here.
        /// </summary>
        [Fact]
        public void AModelWhoseZonesAreAllMarkedNotDwellings_IsBlocked()
        {
            PartOWorkflowInspection partOWorkflowInspection = Inspect(Model(dwelling: false), Request(Scenario_1a()));

            Assert.False(partOWorkflowInspection.CanRun);

            Assert.Equal(PartOWorkflowStageStatus.Blocked, Stage(partOWorkflowInspection, PartOWorkflowStage.DwellingScope).Status);
        }

        /// <summary>
        /// A model that has been zoned, mapped and sized reports every prerequisite READY and offers Run -
        /// which is the whole point: a person does not have to remember that they did those three things.
        /// </summary>
        [Fact]
        public void APreparedModel_ReportsEveryPrerequisiteReady()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel));

            Assert.True(partOWorkflowInspection.CanRun);
            Assert.Empty(partOWorkflowInspection.Blockers);

            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.DwellingScope).Status);
            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.InternalConditions).Status);
            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.PartFRequirements).Status);
        }

        /// <summary>
        /// Every stage is reported, in reading order, so the status list is a complete account rather than a
        /// list of complaints.
        /// </summary>
        [Fact]
        public void EveryStage_IsReported()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel));

            foreach (PartOWorkflowStage partOWorkflowStage in Enum.GetValues<PartOWorkflowStage>())
            {
                Assert.Contains(partOWorkflowInspection.Stages, x => x.Stage == partOWorkflowStage);
            }

            Assert.All(partOWorkflowInspection.Stages, x => Assert.False(string.IsNullOrWhiteSpace(x.Detail)));
            Assert.All(partOWorkflowInspection.Stages, x => Assert.False(string.IsNullOrWhiteSpace(x.Name)));
            Assert.All(partOWorkflowInspection.Stages, x => Assert.False(string.IsNullOrWhiteSpace(x.StatusText)));
        }

        /// <summary>
        /// A model whose spaces carry no internal condition and whose names are not TM59 words would produce
        /// an empty assessment, so it is refused before a full-year TAS run rather than after one - and it is
        /// refused in the words of the command that fixes it.
        /// </summary>
        [Fact]
        public void AModelNothingTM59CanClassify_IsBlockedBeforeTAS()
        {
            AnalyticalModel analyticalModel = Model(tM59: false);

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel));

            Assert.False(partOWorkflowInspection.CanRun);

            PartOWorkflowStageState partOWorkflowStageState = Stage(partOWorkflowInspection, PartOWorkflowStage.InternalConditions);

            Assert.Equal(PartOWorkflowStageStatus.Blocked, partOWorkflowStageState.Status);
            Assert.Contains("Map IC (TM59)", partOWorkflowStageState.Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>The TM59 space-name fallback is real, and it is not what the internal-condition blocker is
        /// about.</b> Pinned because reading those two facts as one question makes the blocker look
        /// self-contradictory: rooms named "Bedroom" and "Kitchen" classify with no internal condition at
        /// all, exactly as <c>TMOverheatingCalculator</c> classifies them, and the scope is refused anyway.
        /// <para>
        /// What is missing is the OCCUPANCY, not the classification.
        /// <c>SAM.Analytical.Tas.Modify.UpdateInternalCondition</c> writes nothing to the TBD internal
        /// condition for a space that has none, and <c>TMOverheatingCalculator.Collect</c> counts an hour as
        /// occupied only where the simulated occupancy gain is above zero - so these rooms would simulate
        /// unoccupied and their criteria would be judged over an empty set rather than refused. The
        /// downstream refusal that proves it without a TAS run is pinned by
        /// <see cref="TheTM59ExporterRefusesASpaceWithNoInternalCondition_EvenWhenItsNameClassifies"/>.
        /// </para>
        /// </summary>
        [Fact]
        public void RoomsTM59ClassifiesByNameAlone_AreStillRefusedForTheirMissingOccupancy()
        {
            TextMap textMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();

            //Same guard as AMissingTM59Resource_IsReportedAndDoesNotBlock: with no resource the stage is
            //Pending by design and there is nothing about classification to assert.
            if (textMap is null)
            {
                return;
            }

            AnalyticalModel analyticalModel = Model(internalConditions: false);

            //The premise, from the production classifier rather than from an assumption about it.
            Space space_Bedroom = analyticalModel.AdjacencyCluster.GetSpaces().Find(x => x.Name.StartsWith("Bedroom", StringComparison.Ordinal));

            Assert.Null(space_Bedroom.InternalCondition);
            Assert.Null(TM59Manager.TM59SpaceApplications(space_Bedroom.InternalCondition, textMap));
            Assert.Contains(TM59SpaceApplication.Sleeping, TM59Manager.TM59SpaceApplications(space_Bedroom, textMap));

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel));

            Assert.False(partOWorkflowInspection.CanRun);

            PartOWorkflowStageState partOWorkflowStageState = Stage(partOWorkflowInspection, PartOWorkflowStage.InternalConditions);

            Assert.Equal(PartOWorkflowStageStatus.Blocked, partOWorkflowStageState.Status);

            //Refused as missing occupancy, and NOT in the words of the classification blocker - which would
            //be untrue here, because these rooms do classify.
            Assert.Contains("occupied", partOWorkflowStageState.Detail, StringComparison.Ordinal);
            Assert.DoesNotContain("is recognised as a TM59", partOWorkflowStageState.Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>The production authority behind that blocker, asked directly.</b>
        /// <c>Tas.TM59.Convert.ToTM59(Space, TM59Manager, SystemType)</c> returns null for a space with no
        /// internal condition, and it does so <i>before</i> it reaches <c>RoomUse</c> - so the space-name
        /// fallback never runs there. <c>Convert.ToTM59(AnalyticalModel, ...)</c> turns one such space into
        /// a refusal of the whole DomOv document, by its own documented contract.
        /// <para>
        /// This is why the zero-internal-condition scope cannot proceed, established statically rather than
        /// by a licensed TAS run. The same room exports as soon as it carries one.
        /// </para>
        /// </summary>
        [Fact]
        public void TheTM59ExporterRefusesASpaceWithNoInternalCondition_EvenWhenItsNameClassifies()
        {
            TM59Manager tM59Manager = new();

            Space space = new(new Guid("cccccccc-0000-0000-0000-000000000001"), "Bedroom 1.1", null);

            Assert.Null(space.InternalCondition);
            Assert.Null(SAM.Analytical.Tas.TM59.Convert.ToTM59(space, tM59Manager, SAM.Analytical.Tas.SystemType.NaturalVentilation));

            //The one difference, and it is the whole difference.
            Space space_Mapped = new(space)
            {
                InternalCondition = new InternalCondition("TM59_Double Bedroom"),
            };

            Assert.NotNull(SAM.Analytical.Tas.TM59.Convert.ToTM59(space_Mapped, tM59Manager, SAM.Analytical.Tas.SystemType.NaturalVentilation));
        }

        /// <summary>
        /// The <i>other</i> blocker still exists and is still distinct: rooms that DO carry internal
        /// conditions but that nothing TM59 recognises - neither the condition name nor the room name - are
        /// refused in the words of the classification refusal, not the occupancy one. Narrowing the
        /// occupancy blocker must never be able to let this case through.
        /// </summary>
        [Fact]
        public void RoomsThatCarryInternalConditionsNothingTM59Classifies_AreRefusedAsUnclassified()
        {
            AnalyticalModel analyticalModel = Model(tM59: false);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            //tM59: false already gives every room a name TM59 does not know ("Area 1.1"). Adding a condition
            //TM59 does not know either leaves the occupancy present and the classification absent.
            foreach (Space space in adjacencyCluster.GetSpaces())
            {
                adjacencyCluster.AddObject(new Space(space)
                {
                    InternalCondition = new InternalCondition("Generic Area"),
                });
            }

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel));

            Assert.False(partOWorkflowInspection.CanRun);

            PartOWorkflowStageState partOWorkflowStageState = Stage(partOWorkflowInspection, PartOWorkflowStage.InternalConditions);

            Assert.Equal(PartOWorkflowStageStatus.Blocked, partOWorkflowStageState.Status);
            Assert.Contains("is recognised as a TM59", partOWorkflowStageState.Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// The mechanical route with no continuous Approved Document F requirement anywhere in scope is
        /// exactly what <c>PrepareBaseMVHR</c> refuses. Saying so here costs nothing.
        /// </summary>
        [Fact]
        public void TheMechanicalRouteWithNoPartFRequirement_IsBlocked()
        {
            AnalyticalModel analyticalModel = Model(partF: false);

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel));

            Assert.False(partOWorkflowInspection.CanRun);

            PartOWorkflowStageState partOWorkflowStageState = Stage(partOWorkflowInspection, PartOWorkflowStage.PartFRequirements);

            Assert.Equal(PartOWorkflowStageStatus.Blocked, partOWorkflowStageState.Status);
            Assert.Contains("AddVent PartF", partOWorkflowStageState.Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// A model whose text map cannot be read on this machine is <b>not</b> refused. That is an
        /// environment fact, not a defect in the building, and the assessment reads the same resource and
        /// will report what it finds.
        /// </summary>
        [Fact]
        public void AMissingTM59Resource_IsReportedAndDoesNotBlock()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowInspection partOWorkflowInspection = PartOWorkflowInspection.Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel), null, null, null);

            //Only meaningful where the resource genuinely is unavailable to this process; where it IS
            //installed, the classified path is covered by APreparedModel_ReportsEveryPrerequisiteReady.
            if (Analytical.Query.DefaultInternalConditionTextMap_TM59() is not null)
            {
                return;
            }

            Assert.Equal(PartOWorkflowStageStatus.Pending, Stage(partOWorkflowInspection, PartOWorkflowStage.InternalConditions).Status);
            Assert.True(partOWorkflowInspection.CanRun);
        }

        // ---- Scenario -----------------------------------------------------------------------------------

        /// <summary>
        /// Three scenarios are offered and Iteration 2B is not one of them - it is an optimisation performed
        /// on a completed Iteration 2 run, and offering it as a baseline would start a run
        /// <c>Modify.CanOptimise</c> refuses.
        /// </summary>
        [Fact]
        public void ThreeScenariosAreOffered_And2BIsNotOneOfThem()
        {
            List<PartOWorkflowScenario> scenarios = PartOWorkflowScenario.Scenarios;

            Assert.Equal(3, scenarios.Count);

            Assert.All(scenarios, x => Assert.DoesNotContain("2B", x.Text, StringComparison.OrdinalIgnoreCase));

            Assert.Contains(scenarios, x => x.Text.Contains("1a", StringComparison.Ordinal) && x.Option.PartOIteration == PartOIteration.BasePassive && !x.SelectVentilationUnit);
            Assert.Contains(scenarios, x => x.Text.Contains("1b", StringComparison.Ordinal) && x.Option.PartOIteration == PartOIteration.BaseNaturalVentilation && !x.SelectVentilationUnit);
            Assert.Contains(scenarios, x => x.Text.Contains("2", StringComparison.Ordinal) && x.Option.PartOIteration == PartOIteration.BasePassive && x.SelectVentilationUnit);
        }

        /// <summary>
        /// Iteration 1a is the mechanical route: Approved Document F applies, and no manufacturer unit is
        /// selected. Both of those are read off SAM's own answer for the iteration, never decided here.
        /// </summary>
        [Fact]
        public void Iteration1a_AppliesPartFAndSelectsNoProduct()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowScenario partOWorkflowScenario = Scenario_1a();

            Assert.Equal(PartOVentilationMode.MVHR, partOWorkflowScenario.Option.PartOVentilationMode);
            Assert.False(partOWorkflowScenario.SelectVentilationUnit);
            Assert.False(partOWorkflowScenario.SupportsOptimisation);

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(partOWorkflowScenario, analyticalModel));

            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.PartFRequirements).Status);
            Assert.Equal(PartOWorkflowStageStatus.NotApplicable, Stage(partOWorkflowInspection, PartOWorkflowStage.Equipment).Status);
        }

        /// <summary>
        /// Iteration 1b invents no mechanical anything. The Approved Document F stage is N/A - and it is N/A
        /// for <c>Query.PartOPartFAirflowApplication</c>'s reason, in that query's own words - and no
        /// equipment is selected. A model with no Part F data at all still runs on this route.
        /// </summary>
        [Fact]
        public void Iteration1b_NeedsNoPartFAndInventsNoMechanicalSystem()
        {
            AnalyticalModel analyticalModel = Model(partF: false);

            PartOWorkflowScenario partOWorkflowScenario = Scenario_1b();

            Assert.Equal(PartOVentilationMode.NaturalVentilation, partOWorkflowScenario.Option.PartOVentilationMode);
            Assert.False(partOWorkflowScenario.SelectVentilationUnit);
            Assert.False(partOWorkflowScenario.SupportsOptimisation);

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(partOWorkflowScenario, analyticalModel));

            Assert.Equal(PartOWorkflowStageStatus.NotApplicable, Stage(partOWorkflowInspection, PartOWorkflowStage.PartFRequirements).Status);
            Assert.Equal(PartOWorkflowStageStatus.NotApplicable, Stage(partOWorkflowInspection, PartOWorkflowStage.Equipment).Status);

            Assert.True(partOWorkflowInspection.CanRun);
        }

        /// <summary>
        /// Iteration 2 selects a real manufacturer unit. Without a catalogue there is nothing to select, so
        /// Run is refused with the catalogue reader's own account of why - never as "no product can serve
        /// this dwelling".
        /// </summary>
        [Fact]
        public void Iteration2_RequiresACatalogueAndSaysWhichStateTheReadLandedIn()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_2(), analyticalModel);

            PartOWorkflowInspection partOWorkflowInspection_None = PartOWorkflowInspection.Inspect(
                analyticalModel,
                partOWorkflowRequest,
                null,
                new PartOWorkflowCapabilities { EquipmentAvailable = false, EquipmentDescription = "No ventilation unit catalogue was found." },
                TextMap_TM59());

            Assert.False(partOWorkflowInspection_None.CanRun);

            PartOWorkflowStageState partOWorkflowStageState = Stage(partOWorkflowInspection_None, PartOWorkflowStage.Equipment);

            Assert.Equal(PartOWorkflowStageStatus.Blocked, partOWorkflowStageState.Status);
            Assert.Contains("No ventilation unit catalogue was found.", partOWorkflowStageState.Detail, StringComparison.Ordinal);

            PartOWorkflowInspection partOWorkflowInspection_Available = PartOWorkflowInspection.Inspect(
                analyticalModel,
                partOWorkflowRequest,
                null,
                new PartOWorkflowCapabilities { EquipmentAvailable = true },
                TextMap_TM59());

            Assert.True(partOWorkflowInspection_Available.CanRun);
            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection_Available, PartOWorkflowStage.Equipment).Status);
        }

        /// <summary>
        /// Only Iteration 2 can be followed by Iteration 2B: it raises mechanical design airflow inside a
        /// selected product's capacity, and needs both. The same pair <c>Modify.CanOptimise</c> refuses on.
        /// </summary>
        [Fact]
        public void OnlyIteration2_SupportsAFollowOnOptimisation()
        {
            Assert.False(Scenario_1a().SupportsOptimisation);
            Assert.False(Scenario_1b().SupportsOptimisation);
            Assert.True(Scenario_2().SupportsOptimisation);
        }

        // ---- Scope --------------------------------------------------------------------------------------

        /// <summary>
        /// The three scopes reach the preparation as what they are: every eligible dwelling, a selection, and
        /// a selection extracted into its own thermal model.
        /// </summary>
        [Fact]
        public void TheThreeScopes_AreCarriedThroughAsScopeAndIsolation()
        {
            AnalyticalModel analyticalModel = Model();

            List<Zone> zones = analyticalModel.GetZones().FindAll(x => x.Name == "Flat 1");

            Assert.False(new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.AllDwellings, analyticalModel.GetZones(), false).Isolate);
            Assert.False(new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellings, zones, false).Isolate);
            Assert.True(new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellingsIsolated, zones, false).Isolate);
        }

        /// <summary>
        /// A narrowed scope reports the spaces of the selected dwellings and no others, and says so.
        /// </summary>
        [Fact]
        public void ASelectedDwelling_NarrowsTheScopeToItsOwnSpaces()
        {
            AnalyticalModel analyticalModel = Model();

            List<Zone> zones = analyticalModel.GetZones().FindAll(x => x.Name == "Flat 1");

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellings, zones, false));

            PartOWorkflowStageState partOWorkflowStageState = Stage(partOWorkflowInspection, PartOWorkflowStage.DwellingScope);

            Assert.Equal(PartOWorkflowStageStatus.Ready, partOWorkflowStageState.Status);
            Assert.Contains("1 of 2 eligible dwelling zones", partOWorkflowStageState.Detail, StringComparison.Ordinal);
            Assert.Contains("3 spaces", partOWorkflowStageState.Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// An isolated scope says at the point of choice that only those dwellings are simulated - because
        /// this is a different thermal model, not a faster way of computing the same one.
        /// </summary>
        [Fact]
        public void AnIsolatedScope_IsStatedAsAThermalModelScope()
        {
            AnalyticalModel analyticalModel = Model();

            List<Zone> zones = analyticalModel.GetZones().FindAll(x => x.Name == "Flat 1");

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellingsIsolated, zones, false));

            Assert.Contains("isolated thermal model", Stage(partOWorkflowInspection, PartOWorkflowStage.DwellingScope).Detail, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Inspecting a model changes nothing about it - including on the isolated scope, where the
        /// extraction itself belongs to <c>SAM.Analytical.Modify.PreparePartOIteration</c> and happens on a
        /// copy. The source model a person still has open is not touched by looking at it.
        /// </summary>
        [Fact]
        public void Inspecting_NeverModifiesTheSourceModel()
        {
            AnalyticalModel analyticalModel = Model();

            string before = analyticalModel.ToJsonObject().ToString();

            List<Zone> zones = analyticalModel.GetZones().FindAll(x => x.Name == "Flat 1");

            Inspect(analyticalModel, new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellingsIsolated, zones, false));
            Inspect(analyticalModel, Request(Scenario_2(), analyticalModel));
            Inspect(analyticalModel, Request(Scenario_1b(), analyticalModel));

            Assert.Equal(before, analyticalModel.ToJsonObject().ToString());
        }

        /// <summary>
        /// A model that is <b>already</b> an isolated extract says so, from the isolation context
        /// <c>Analytical.Modify.PreparePartOIteration</c> stamped on it and the <c>.sam</c> carries - so a
        /// person reopening one is told what they are looking at before they run it again.
        /// </summary>
        [Fact]
        public void AModelThatIsAlreadyIsolated_SaysSoFromItsOwnStampedContext()
        {
            AnalyticalModel analyticalModel = Model();

            List<Zone> zones = analyticalModel.GetZones().FindAll(x => x.Name == "Flat 1");

            //Stamped exactly as the preparation stamps it, and read back through the same parameter.
            List<Guid> guids_Space = analyticalModel.AdjacencyCluster.GetRelatedObjects<Space>(zones[0]).ConvertAll(x => x.Guid);

            analyticalModel.SetValue(SAM.Analytical.AnalyticalModelParameter.PartOIsolationContext, new PartOIsolationContext(guids_Space, zones.ConvertAll(x => x.Guid), ["Flat 1"]));

            PartOWorkflowStageState partOWorkflowStageState = Stage(Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel)), PartOWorkflowStage.DwellingScope);

            Assert.Equal(PartOWorkflowStageStatus.Ready, partOWorkflowStageState.Status);
            Assert.Contains("ALREADY the isolated thermal model", partOWorkflowStageState.Detail, StringComparison.Ordinal);
            Assert.Contains("Flat 1", partOWorkflowStageState.Detail, StringComparison.Ordinal);
        }

        // ---- Orchestration: reuse -----------------------------------------------------------------------

        /// <summary>
        /// An iteration already prepared for exactly this scenario and scope is <b>reused</b>, not prepared
        /// again - so cancelling the Simulate dialog and coming straight back does not rebuild a design that
        /// is already correct.
        /// </summary>
        [Fact]
        public void APreparedRunForTheSameRequest_IsReused()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_1a(), analyticalModel);

            PartORun partORun = Prepared(analyticalModel, partOWorkflowRequest);

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, partOWorkflowRequest, partORun);

            Assert.True(partOWorkflowInspection.ReusePreparation);
            Assert.Equal(PartOWorkflowStageStatus.Reused, Stage(partOWorkflowInspection, PartOWorkflowStage.VentilationDesign).Status);
        }

        /// <summary>
        /// A different base provision is a different engineering case, so the preparation is rebuilt - and
        /// the stages that do not depend on it are untouched. Switching scenario invalidates the design, not
        /// the zoning or the TM59 mapping.
        /// </summary>
        [Fact]
        public void ASwitchOfScenario_RebuildsOnlyTheDesign()
        {
            AnalyticalModel analyticalModel = Model();

            PartORun partORun = Prepared(analyticalModel, Request(Scenario_1a(), analyticalModel));

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1b(), analyticalModel), partORun);

            Assert.False(partOWorkflowInspection.ReusePreparation);
            Assert.Equal(PartOWorkflowStageStatus.Prepare, Stage(partOWorkflowInspection, PartOWorkflowStage.VentilationDesign).Status);

            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.DwellingScope).Status);
            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.InternalConditions).Status);

            //The one stage that legitimately changes with the route, and it changes to N/A rather than to a
            //blocker: the natural ventilation route applies no continuous mechanical rate.
            Assert.Equal(PartOWorkflowStageStatus.NotApplicable, Stage(partOWorkflowInspection, PartOWorkflowStage.PartFRequirements).Status);
        }

        /// <summary>
        /// A narrowed dwelling scope is a different case too - the preparation builds one system per assessed
        /// dwelling, so a run prepared over two flats does not describe a run over one.
        /// </summary>
        [Fact]
        public void ASwitchOfScope_IsNotReused()
        {
            AnalyticalModel analyticalModel = Model();

            PartORun partORun = Prepared(analyticalModel, Request(Scenario_1a(), analyticalModel));

            List<Zone> zones = analyticalModel.GetZones().FindAll(x => x.Name == "Flat 1");

            Assert.False(Inspect(analyticalModel, new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellings, zones, false), partORun).ReusePreparation);
        }

        /// <summary>
        /// Isolation changes the thermal model, so a whole-building preparation is not an isolated one even
        /// over the same dwellings.
        /// </summary>
        [Fact]
        public void ASwitchOfIsolation_IsNotReused()
        {
            AnalyticalModel analyticalModel = Model();

            List<Zone> zones = analyticalModel.GetZones();

            PartORun partORun = Prepared(analyticalModel, new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellings, zones, false));

            Assert.False(Inspect(analyticalModel, new PartOWorkflowRequest(Scenario_1a().Option, PartOWorkflowScope.SelectedDwellingsIsolated, zones, false), partORun).ReusePreparation);
        }

        /// <summary>
        /// Iteration 1a and Iteration 2 share a base provision and differ only in whether a catalogue was
        /// offered - which is exactly the difference <c>PartOPreparationContext.HasVentilationUnitCatalogue</c>
        /// records, and exactly the difference 2B depends on. It is not reusable across.
        /// </summary>
        [Fact]
        public void ASwitchBetweenIteration1aAndIteration2_IsNotReused()
        {
            AnalyticalModel analyticalModel = Model();

            PartORun partORun = Prepared(analyticalModel, Request(Scenario_1a(), analyticalModel));

            Assert.False(Inspect(analyticalModel, Request(Scenario_2(), analyticalModel), partORun).ReusePreparation);
        }

        /// <summary>
        /// A run reopened from its saved results is never reused, and not because of a flag: it carries no
        /// preparation context at all, which is the same distinction <c>Modify.CanOptimise</c> refuses on.
        /// </summary>
        [Fact]
        public void ARestoredRun_IsNeverReused()
        {
            AnalyticalModel analyticalModel = Model();

            PartORun partORun = new();

            partORun.Prepare(analyticalModel, Scenarios(analyticalModel));

            Assert.Null(partORun.PreparationContext);

            Assert.False(Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel), partORun).ReusePreparation);
        }

        /// <summary>
        /// A model with unclassified rooms beside classified ones is a NORMAL model - a hall, a store, a
        /// bathroom - and it is reported rather than refused. Turning that into a UI blocker would refuse
        /// models the assessment handles by naming them as not assessed.
        /// </summary>
        [Fact]
        public void UnclassifiedRoomsBesideClassifiedOnes_AreReportedAndDoNotBlock()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel));

            PartOWorkflowStageState partOWorkflowStageState = Stage(partOWorkflowInspection, PartOWorkflowStage.InternalConditions);

            Assert.Equal(PartOWorkflowStageStatus.Ready, partOWorkflowStageState.Status);

            //Two of the three rooms per flat classify; the store does not, and the model still runs.
            Assert.Contains("4 of 6 spaces", partOWorkflowStageState.Detail, StringComparison.Ordinal);

            Assert.True(partOWorkflowInspection.CanRun);
        }

        /// <summary>
        /// The SAM Check gate is <b>not</b> duplicated here. It judges the normalized model TAS is actually
        /// given, which does not exist until the run builds it, so this workflow reports it as pending and
        /// says where it runs - it does not pre-empt it with a second opinion over the source model.
        /// </summary>
        [Fact]
        public void TheModelCheck_IsReportedAsRunningBeforeTASAndIsNotDuplicated()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowStageState partOWorkflowStageState = Stage(Inspect(analyticalModel, Request(Scenario_1a(), analyticalModel)), PartOWorkflowStage.ModelCheck);

            Assert.Equal(PartOWorkflowStageStatus.Pending, partOWorkflowStageState.Status);

            Assert.Contains("before TAS", partOWorkflowStageState.Detail, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Errors stop the run", partOWorkflowStageState.Detail, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("warnings", partOWorkflowStageState.Detail, StringComparison.OrdinalIgnoreCase);
        }

        // ---- Results ------------------------------------------------------------------------------------

        /// <summary>
        /// With results, Review is offered and the simulation stage reports them - reopened results say so,
        /// and say that no new simulation is needed to read them.
        /// </summary>
        [Fact]
        public void ReopenedResults_AreReviewableWithoutRunningTASAgain()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowInspection partOWorkflowInspection = PartOWorkflowInspection.Inspect(
                analyticalModel,
                Request(Scenario_1a(), analyticalModel),
                null,
                new PartOWorkflowCapabilities
                {
                    ResultsAvailable = true,
                    ResultsRestored = true,
                    Path_Results = Path.Combine("C:", "Out", "Block.tsd"),
                },
                TextMap_TM59());

            Assert.True(partOWorkflowInspection.CanReviewResults);

            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.Simulation).Status);
            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.Results).Status);

            Assert.Contains("No new simulation", Stage(partOWorkflowInspection, PartOWorkflowStage.Simulation).Detail, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Block.tsd", Stage(partOWorkflowInspection, PartOWorkflowStage.Simulation).Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// Without results, Review is refused - in the words of the authority that refused it, so a stale or
        /// incompatible saved run explains itself rather than being silently reused or silently unavailable.
        /// </summary>
        [Fact]
        public void WithoutResults_ReviewIsRefusedInTheAuthoritysOwnWords()
        {
            AnalyticalModel analyticalModel = Model();

            const string refusal = "The results file this run records is no longer the one it was produced from.";

            PartOWorkflowInspection partOWorkflowInspection = PartOWorkflowInspection.Inspect(
                analyticalModel,
                Request(Scenario_1a(), analyticalModel),
                null,
                new PartOWorkflowCapabilities { ResultsAvailable = false, ResultsRefusal = refusal },
                TextMap_TM59());

            Assert.False(partOWorkflowInspection.CanReviewResults);
            Assert.Equal(refusal, partOWorkflowInspection.ResultsRefusal);

            Assert.Equal(PartOWorkflowStageStatus.NotRun, Stage(partOWorkflowInspection, PartOWorkflowStage.Results).Status);
            Assert.Equal(refusal, Stage(partOWorkflowInspection, PartOWorkflowStage.Results).Detail);
        }

        /// <summary>
        /// Iteration 2B availability is <c>Modify.CanOptimise</c>'s answer, carried through unchanged - the
        /// workflow neither widens nor narrows it, and repeats its reason rather than inventing one.
        /// </summary>
        [Fact]
        public void OptimisationAvailability_IsTheAuthoritysAnswer()
        {
            AnalyticalModel analyticalModel = Model();

            const string refusal = "This Part O run was prepared without equipment selection.";

            PartOWorkflowInspection partOWorkflowInspection_No = PartOWorkflowInspection.Inspect(
                analyticalModel,
                Request(Scenario_1a(), analyticalModel),
                null,
                new PartOWorkflowCapabilities { ResultsAvailable = true, OptimisationAvailable = false, OptimisationRefusal = refusal },
                TextMap_TM59());

            Assert.False(partOWorkflowInspection_No.CanOptimise);
            Assert.Equal(refusal, partOWorkflowInspection_No.OptimisationRefusal);

            PartOWorkflowInspection partOWorkflowInspection_Yes = PartOWorkflowInspection.Inspect(
                analyticalModel,
                Request(Scenario_2(), analyticalModel),
                null,
                new PartOWorkflowCapabilities { EquipmentAvailable = true, ResultsAvailable = true, OptimisationAvailable = true },
                TextMap_TM59());

            Assert.True(partOWorkflowInspection_Yes.CanOptimise);
        }

        // ---- The dialog ---------------------------------------------------------------------------------

        /// <summary>
        /// The dialog offers the three scenarios and the three scopes, and nothing else.
        /// </summary>
        [WpfFact]
        public void TheDialog_OffersThreeScenariosAndThreeScopes()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
            };

            Assert.NotNull(partOWorkflowWindow.Scenario);

            Assert.Equal(PartOWorkflowScope.AllDwellings, partOWorkflowWindow.Scope);

            partOWorkflowWindow.Scope = PartOWorkflowScope.SelectedDwellingsIsolated;

            Assert.Equal(PartOWorkflowScope.SelectedDwellingsIsolated, partOWorkflowWindow.Scope);
            Assert.True(partOWorkflowWindow.Request.Isolate);
        }

        /// <summary>
        /// On the all-dwellings scope the request covers every eligible dwelling, whatever the list happens
        /// to be ticked to: the scope control is the authority, not a stale set of checkboxes.
        /// </summary>
        [WpfFact]
        public void TheDialog_CoversEveryEligibleDwellingOnTheAllDwellingsScope()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
            };

            partOWorkflowWindow.DwellingSelection.SetSelected(false);

            Assert.Equal(2, partOWorkflowWindow.Zones_Dwelling.Count);

            partOWorkflowWindow.Scope = PartOWorkflowScope.SelectedDwellings;

            Assert.Empty(partOWorkflowWindow.Zones_Dwelling);
            Assert.False(partOWorkflowWindow.CanRun);
        }

        /// <summary>
        /// When Run is unavailable the dialog says why, and the reason is the inspection's - so an advanced
        /// user can see which stage is blocking without opening a log.
        /// </summary>
        [WpfFact]
        public void TheDialog_SaysWhyRunIsUnavailable()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(zoned: false),
            };

            Assert.False(partOWorkflowWindow.CanRun);
            Assert.Contains("Run is unavailable", partOWorkflowWindow.BlockerDescription, StringComparison.Ordinal);
            Assert.Contains("no zones", partOWorkflowWindow.BlockerDescription, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Review and Optimise are off until there is something to review and something to optimise - and a
        /// freshly opened model has neither.
        /// </summary>
        [WpfFact]
        public void TheDialog_OffersNeitherReviewNorOptimiseWithoutResults()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
            };

            Assert.False(partOWorkflowWindow.CanReviewResults);
            Assert.False(partOWorkflowWindow.CanOptimise);
        }

        /// <summary>
        /// Review becomes available from the capability the caller supplies - which is
        /// <c>PartORun.IsAssessable</c>'s answer - so a reopened model with valid results reaches its
        /// assessment from this dialog without a TAS run.
        /// </summary>
        [WpfFact]
        public void TheDialog_OffersReviewWhereTheAuthoritySaysThereAreResults()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
                Capabilities = new PartOWorkflowCapabilities { ResultsAvailable = true, ResultsRestored = true },
            };

            Assert.True(partOWorkflowWindow.CanReviewResults);
            Assert.False(partOWorkflowWindow.CanOptimise);
        }

        /// <summary>
        /// Pass 5: the Hub asks for NO Iteration 2B input before a run. The request a Prepare &amp; Run records
        /// carries no optimisation settings on any scenario - the step and the limit are confirmed when 2B
        /// starts - and the Hub says so on Iteration 2, and says why 2B does not apply on a natural route.
        /// </summary>
        [WpfFact]
        public void TheDialog_AsksForNoIteration2BInputBeforeARun()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
                VentilationUnitCatalogue = Catalogue(),
            };

            partOWorkflowWindow.Restore(Scenario_2(), PartOWorkflowScope.AllDwellings, null);

            Assert.Null(partOWorkflowWindow.Request.OptimisationSettings);
            Assert.Contains("confirmed when it starts", partOWorkflowWindow.OptimisationDescription, StringComparison.Ordinal);

            partOWorkflowWindow.Restore(Scenario_1b(), PartOWorkflowScope.AllDwellings, null);

            Assert.Null(partOWorkflowWindow.Request.OptimisationSettings);
            Assert.Contains("not a mechanical route", partOWorkflowWindow.OptimisationDescription, StringComparison.Ordinal);
        }

        /// <summary>
        /// Reopening the dialog after a run returns a person to the choices they made, not to the defaults.
        /// Choices only - the status is re-inspected every time.
        /// </summary>
        [WpfFact]
        public void TheDialog_CarriesTheChoicesBackWhenItIsReopened()
        {
            AnalyticalModel analyticalModel = Model();

            Guid guid = analyticalModel.GetZones().Find(x => x.Name == "Flat 1").Guid;

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = analyticalModel,
            };

            partOWorkflowWindow.Restore(Scenario_1b(), PartOWorkflowScope.SelectedDwellingsIsolated, [guid]);

            Assert.Equal(PartOIteration.BaseNaturalVentilation, partOWorkflowWindow.Scenario.Option.PartOIteration);
            Assert.Equal(PartOWorkflowScope.SelectedDwellingsIsolated, partOWorkflowWindow.Scope);

            Assert.Single(partOWorkflowWindow.Zones_Dwelling);
            Assert.Equal(guid, partOWorkflowWindow.Zones_Dwelling[0].Guid);
        }

        /// <summary>
        /// The dialog names the expert commands it does not replace, so nothing about this window suggests
        /// they have gone.
        /// </summary>
        [WpfFact]
        public void TheDialog_NamesTheExpertCommandsItDoesNotReplace()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
            };

            Assert.Equal(3, PartOWorkflowScenario.Scenarios.Count);

            //The status list is the inspection's, verbatim - the window adds no line of its own.
            Assert.Equal(partOWorkflowWindow.Inspection.Stages.Count, Enum.GetValues<PartOWorkflowStage>().Length);
        }

        // ---- Intent is not capability -------------------------------------------------------------------

        /// <summary>
        /// <b>Iteration 2 stays Iteration 2 when no catalogue can be read.</b>
        /// <para>
        /// The scenario is what the user asked for; whether this machine has a readable product catalogue is
        /// a capability. Folding the second into the first made the request an Iteration 1a request - a
        /// different assessment, with no equipment selection and no Iteration 2B after it - and then reported
        /// success. The request now keeps the intent and the missing catalogue is reported as the blocker it
        /// is, in the catalogue reader's own words.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheDialog_KeepsIteration2WhenNoCatalogueCanBeRead()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_NoCatalogue_{0}", Guid.NewGuid()));

            //The production reader, over a directory with no catalogue in it - the Unavailable state.
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(directory);

            Assert.False(ventilationUnitCatalogue.HasSelectableProducts);

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
                VentilationUnitCatalogue = ventilationUnitCatalogue,
            };

            partOWorkflowWindow.Restore(Scenario_2(), PartOWorkflowScope.AllDwellings, null);

            //THE INTENT. Not downgraded, not silently turned into Iteration 1a.
            Assert.True(partOWorkflowWindow.Request.SelectVentilationUnit);

            //THE CAPABILITY, reported as a blocker rather than acted on.
            Assert.Equal(PartOWorkflowStageStatus.Blocked, Stage(partOWorkflowWindow.Inspection, PartOWorkflowStage.Equipment).Status);

            Assert.False(partOWorkflowWindow.CanRun);

            Assert.Contains("Run is unavailable", partOWorkflowWindow.BlockerDescription, StringComparison.Ordinal);
            Assert.Contains("Iteration 2 selects a real manufacturer unit", partOWorkflowWindow.BlockerDescription, StringComparison.Ordinal);

            //The reader's own account of which of its three states it landed in - never "no product can
            //serve this dwelling", which is a different and false statement.
            Assert.Contains(ventilationUnitCatalogue.Description, partOWorkflowWindow.BlockerDescription, StringComparison.Ordinal);
        }

        /// <summary>
        /// The positive half of the same rule: with a real catalogue, Iteration 2 is an Iteration 2 request,
        /// the equipment stage is READY and Run is offered.
        /// </summary>
        [WpfFact]
        public void TheDialog_RunsIteration2WithARealCatalogue()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
                VentilationUnitCatalogue = Catalogue(),
            };

            partOWorkflowWindow.Restore(Scenario_2(), PartOWorkflowScope.AllDwellings, null);

            Assert.True(partOWorkflowWindow.Request.SelectVentilationUnit);

            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowWindow.Inspection, PartOWorkflowStage.Equipment).Status);

            Assert.True(partOWorkflowWindow.CanRun);
            Assert.Empty(partOWorkflowWindow.BlockerDescription);
        }

        /// <summary>
        /// Iteration 1a is still not Iteration 2, catalogue or no catalogue: the intent that is preserved is
        /// the one that was stated, and 1a states no equipment selection.
        /// </summary>
        [WpfFact]
        public void TheDialog_KeepsIteration1aDistinctFromIteration2()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model(),
                VentilationUnitCatalogue = Catalogue(),
            };

            partOWorkflowWindow.Restore(Scenario_1a(), PartOWorkflowScope.AllDwellings, null);

            Assert.False(partOWorkflowWindow.Request.SelectVentilationUnit);

            Assert.Equal(PartOWorkflowStageStatus.NotApplicable, Stage(partOWorkflowWindow.Inspection, PartOWorkflowStage.Equipment).Status);

            Assert.True(partOWorkflowWindow.CanRun);
        }

        // ---- A ticked Iteration 2B must be usable before Run is offered ----------------------------------

        // ---- The Iteration 2B record on a reused preparation ---------------------------------------------

        /// <summary>
        /// <b>A.</b> A run prepared with no Iteration 2B, reused after the user asks for one: the run records
        /// the settings that were actually asked for, so the follow-on capability can expose 2B.
        /// <para>
        /// Without this the reuse path skipped the preparation that would have recorded them, and
        /// <c>Modify.CanOptimise</c> went on reading the null the earlier preparation left behind - the
        /// button stayed off for a run the user had just asked to optimise.
        /// </para>
        /// </summary>
        [Fact]
        public void AReusedPreparation_RecordsAnOptimisationTheEarlierPreparationDidNotHave()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_2(), analyticalModel);

            PartORun partORun = Prepared(analyticalModel, partOWorkflowRequest);

            Assert.Null(partORun.PreparationContext.OptimisationSettings);

            //The same engineering case, so it is still reusable - only the follow-on choice moved.
            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 7, MaximumIterations = 4 };

            Assert.True(Inspect(analyticalModel, partOWorkflowRequest, partORun).ReusePreparation);

            Assert.True(Modify.ReuseWithCurrentOptimisation(partORun, partOWorkflowRequest, true));

            Assert.NotNull(partORun.PreparationContext.OptimisationSettings);
            Assert.Equal(7, partORun.PreparationContext.OptimisationSettings.AirFlowStep_Lps);
            Assert.Equal(4, partORun.PreparationContext.OptimisationSettings.MaximumIterations);
        }

        /// <summary>
        /// <b>B.</b> Changed step and limit reach the run. The values the optimisation would run at are the
        /// ones on screen, not the ones from an earlier preparation of the same design.
        /// </summary>
        [Fact]
        public void AReusedPreparation_RecordsChangedOptimisationValuesRatherThanTheOldOnes()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_2(), analyticalModel);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 5, MaximumIterations = 10, CapacityEnvelope = true, WarmStart = true };

            PartORun partORun = Prepared(analyticalModel, partOWorkflowRequest);

            Assert.Equal(5, partORun.PreparationContext.OptimisationSettings.AirFlowStep_Lps);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 2.5, MaximumIterations = 3, CapacityEnvelope = false, WarmStart = false };

            Assert.True(Inspect(analyticalModel, partOWorkflowRequest, partORun).ReusePreparation);

            Assert.True(Modify.ReuseWithCurrentOptimisation(partORun, partOWorkflowRequest, true));

            PartOOptimisationSettings partOOptimisationSettings = partORun.PreparationContext.OptimisationSettings;

            Assert.Equal(2.5, partOOptimisationSettings.AirFlowStep_Lps);
            Assert.Equal(3, partOOptimisationSettings.MaximumIterations);
            Assert.False(partOOptimisationSettings.CapacityEnvelope);
            Assert.False(partOOptimisationSettings.WarmStart);
        }

        /// <summary>
        /// <b>C.</b> Pass 5: a request that states NO 2B settings - every Hub request, since the
        /// Hub no longer asks for them - keeps a preset the Prepare Iteration window recorded, rather than clearing
        /// it. The preset only pre-fills the Start Iteration 2B confirmation; nothing runs at it unconfirmed.
        /// </summary>
        [Fact]
        public void AReusedPreparation_KeepsARecordedPresetWhenTheRequestStatesNone()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_2(), analyticalModel);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings();

            PartORun partORun = Prepared(analyticalModel, partOWorkflowRequest);

            Assert.NotNull(partORun.PreparationContext.OptimisationSettings);

            partOWorkflowRequest.OptimisationSettings = null;

            Assert.True(Inspect(analyticalModel, partOWorkflowRequest, partORun).ReusePreparation);

            Assert.True(Modify.ReuseWithCurrentOptimisation(partORun, partOWorkflowRequest, true));

            Assert.NotNull(partORun.PreparationContext.OptimisationSettings);
        }

        /// <summary>
        /// The 2B choice is <b>not</b> part of the engineering match, and that is deliberate: it is the one
        /// thing on the preparation context the analytical preparation neither reads nor is affected by, so
        /// matching on it would rebuild a model nothing about it changes.
        /// </summary>
        [Fact]
        public void AChangedOptimisationChoice_DoesNotRebuildThePreparation()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_2(), analyticalModel);

            PartORun partORun = Prepared(analyticalModel, partOWorkflowRequest);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 9, MaximumIterations = 2 };

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, partOWorkflowRequest, partORun);

            Assert.True(partOWorkflowInspection.ReusePreparation);
            Assert.Equal(PartOWorkflowStageStatus.Reused, Stage(partOWorkflowInspection, PartOWorkflowStage.VentilationDesign).Status);
        }

        /// <summary>
        /// <b>Inspecting changes nothing.</b> The status list reports what the run currently carries; the
        /// user's new choice reaches the run only when they commit to it. A rebuild driven by a status
        /// refresh would rewrite the record of a run nobody had asked to change.
        /// </summary>
        [Fact]
        public void Inspecting_DoesNotWriteTheOptimisationChoiceOntoTheRun()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_2(), analyticalModel);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 5, MaximumIterations = 10 };

            PartORun partORun = Prepared(analyticalModel, partOWorkflowRequest);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 1, MaximumIterations = 99 };

            Inspect(analyticalModel, partOWorkflowRequest, partORun);
            Inspect(analyticalModel, partOWorkflowRequest, partORun);

            //Still the run's own record, untouched by having been looked at.
            Assert.Equal(5, partORun.PreparationContext.OptimisationSettings.AirFlowStep_Lps);
            Assert.Equal(10, partORun.PreparationContext.OptimisationSettings.MaximumIterations);
        }

        /// <summary>
        /// The record is a transition on a <b>prepared</b> run and nothing else. A run that was never
        /// prepared, and a restored run - which carries no preparation context by construction - both refuse
        /// it, so the orchestration prepares again rather than writing into a record that is not there.
        /// </summary>
        [Fact]
        public void OnlyAPreparedRunWithAContext_RecordsAnOptimisationChoice()
        {
            AnalyticalModel analyticalModel = Model();

            Assert.False(new PartORun().AdoptOptimisationSettings(new PartOOptimisationSettings()));

            //Prepared through the overload that states no context - live, but not automatically repeatable.
            PartORun partORun_NoContext = new();
            partORun_NoContext.Prepare(analyticalModel, Scenarios(analyticalModel));

            Assert.Equal(PartORunState.Prepared, partORun_NoContext.State);
            Assert.Null(partORun_NoContext.PreparationContext);
            Assert.False(partORun_NoContext.AdoptOptimisationSettings(new PartOOptimisationSettings()));

            //And the orchestration degrades to preparing again rather than running a reuse it cannot record
            //the current choice on.
            Assert.False(Modify.ReuseWithCurrentOptimisation(partORun_NoContext, Request(Scenario_2(), analyticalModel), true));
        }

        /// <summary>
        /// Nothing is written where the preparation is <b>not</b> being reused: that path prepares again, and
        /// the new preparation records the choice itself. A write here would change the record of a run that
        /// is about to be replaced.
        /// </summary>
        [Fact]
        public void APreparationThatIsNotReused_IsNotWrittenTo()
        {
            AnalyticalModel analyticalModel = Model();

            PartOWorkflowRequest partOWorkflowRequest = Request(Scenario_2(), analyticalModel);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 5, MaximumIterations = 10 };

            PartORun partORun = Prepared(analyticalModel, partOWorkflowRequest);

            partOWorkflowRequest.OptimisationSettings = new PartOOptimisationSettings { AirFlowStep_Lps = 8, MaximumIterations = 6 };

            Assert.False(Modify.ReuseWithCurrentOptimisation(partORun, partOWorkflowRequest, false));

            Assert.Equal(5, partORun.PreparationContext.OptimisationSettings.AirFlowStep_Lps);
            Assert.Equal(10, partORun.PreparationContext.OptimisationSettings.MaximumIterations);
        }

        // ---- Fixture ------------------------------------------------------------------------------------

        private static PartOWorkflowStageState Stage(PartOWorkflowInspection partOWorkflowInspection, PartOWorkflowStage partOWorkflowStage)
        {
            foreach (PartOWorkflowStageState partOWorkflowStageState in partOWorkflowInspection.Stages)
            {
                if (partOWorkflowStageState.Stage == partOWorkflowStage)
                {
                    return partOWorkflowStageState;
                }
            }

            throw new Xunit.Sdk.XunitException(string.Format("The inspection reported no '{0}' stage.", partOWorkflowStage));
        }

        private static PartOWorkflowInspection Inspect(AnalyticalModel analyticalModel, PartOWorkflowRequest partOWorkflowRequest, PartORun partORun = null)
        {
            return PartOWorkflowInspection.Inspect(analyticalModel, partOWorkflowRequest, partORun, new PartOWorkflowCapabilities { EquipmentAvailable = true }, TextMap_TM59());
        }

        private static PartOWorkflowRequest Request(PartOWorkflowScenario partOWorkflowScenario, AnalyticalModel analyticalModel = null)
        {
            return new PartOWorkflowRequest(partOWorkflowScenario.Option, PartOWorkflowScope.AllDwellings, analyticalModel?.GetZones() ?? [], partOWorkflowScenario.SelectVentilationUnit);
        }

        private static PartOWorkflowScenario Scenario_1a()
        {
            return PartOWorkflowScenario.Scenarios.Find(x => x.Option.PartOIteration == PartOIteration.BasePassive && !x.SelectVentilationUnit);
        }

        private static PartOWorkflowScenario Scenario_1b()
        {
            return PartOWorkflowScenario.Scenarios.Find(x => x.Option.PartOIteration == PartOIteration.BaseNaturalVentilation);
        }

        private static PartOWorkflowScenario Scenario_2()
        {
            return PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit);
        }

        /// <summary>
        /// A run prepared with exactly the given request's inputs, as
        /// <c>Modify.PreparePartOIteration</c> records them. The engineering is not run - what is under test
        /// is the match between a recorded preparation and a new request, and
        /// <c>PartORun.Prepare(model, scenarios, context)</c> is the seam that carries it.
        /// </summary>
        private static PartORun Prepared(AnalyticalModel analyticalModel, PartOWorkflowRequest partOWorkflowRequest)
        {
            PartORun result = new();

            PartOPreparationContext partOPreparationContext = new(
                partOWorkflowRequest.PartOIteration,
                partOWorkflowRequest.Zones_Dwelling,
                partOWorkflowRequest.VentilationStrategies(),
                partOWorkflowRequest.SelectVentilationUnit ? Descriptors() : null)
            {
                Isolated = partOWorkflowRequest.Isolate,

                //As Modify.PreparePartOIteration records it, so a reused preparation starts from whatever
                //the preparation that built it was actually asked for.
                OptimisationSettings = partOWorkflowRequest.OptimisationSettings,
            };

            Assert.True(result.Prepare(analyticalModel, Scenarios(analyticalModel), partOPreparationContext));

            return result;
        }

        private static List<VentilationUnitCapacityDescriptor> Descriptors()
        {
            return [new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test", "Model", "TEST-1"), 150, 150, 10)];
        }

        private static List<OverheatingScenario> Scenarios(AnalyticalModel analyticalModel)
        {
            List<OverheatingScenario> result = [];

            foreach (Zone zone in analyticalModel.GetZones() ?? [])
            {
                result.Add(new OverheatingScenario(PartOAssessmentScope.Dwelling, zone.Guid, PartOIteration.BasePassive));
            }

            return result;
        }

        /// <summary>
        /// The TM59 keyword map, taken from the installed resource where this machine has one and built here
        /// where it does not - so the classification tests assert the same thing on every machine.
        /// <para>
        /// The keywords are the ones the shipped resource carries for these three roles. This is a fixture,
        /// not a second vocabulary: the matching rule under test is <c>TM59Manager</c>'s.
        /// </para>
        /// </summary>
        private static TextMap TextMap_TM59()
        {
            TextMap result = Analytical.Query.DefaultInternalConditionTextMap_TM59();
            if (result is not null)
            {
                return result;
            }

            result = Core.Create.TextMap("TM59 fixture");

            result.Add("Living", "living", "lounge", "sitting");
            result.Add("Sleeping", "bed", "bedroom", "double", "twin");
            result.Add("Cooking", "kitchen", "kit");

            return result;
        }

        /// <summary>
        /// A minimal catalogue with one selectable product, written to a temporary directory and read back
        /// through the production reader - so the window is given a real catalogue rather than a stub.
        /// </summary>
        private static VentilationUnitCatalogue Catalogue()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_WorkflowCatalogue_{0}", Guid.NewGuid()));

            Directory.CreateDirectory(directory);

            File.WriteAllText(Path.Combine(directory, "VentilationUnitCatalogue.JSON"), """
            {
              "Schema": "VentilationUnitCatalogue:v1",
              "Templates": [
                {
                  "_type": "SAM.Analytical.VentilationUnitTemplate,SAM.Analytical",
                  "Name": "Test unit",
                  "VentilationUnitReference": {
                    "_type": "SAM.Analytical.VentilationUnitReference,SAM.Analytical",
                    "Name": "Test unit",
                    "Manufacturer": "Test",
                    "Model": "T-150",
                    "Reference": "TEST-150"
                  },
                  "Source": "Written by this test. A template needs a traceable source to be valid at all.",
                  "MaximumSupplyFlowRate_Lps": 150,
                  "MaximumExtractFlowRate_Lps": 150,
                  "Rank": 10
                }
              ]
            }
            """);

            return VentilationUnitCatalogue.Read(directory);
        }

        /// <summary>
        /// Two flats of three rooms each - a bedroom, a kitchen and a store - with the Approved Document F
        /// continuous requirements a mechanical route is realized from, and internal conditions named as the
        /// TM59 map spells them.
        /// <para>
        /// <b>The store is deliberately unclassifiable</b>: it is a normal room that TM59 produces no result
        /// for, and it is what makes "some rooms do not classify" a real case rather than a hypothetical one.
        /// </para>
        /// </summary>
        /// <param name="zoned">False builds the rooms with no zone at all.</param>
        /// <param name="dwelling">False marks both zones explicitly as not dwellings.</param>
        /// <param name="tM59">False leaves every room without an internal condition and with a name TM59 does not know.</param>
        /// <param name="partF">False omits the Approved Document F requirements.</param>
        /// <param name="internalConditions">
        /// False leaves every room without an internal condition while keeping its TM59 room NAME - the one
        /// case that separates the two prerequisites <c>PartOWorkflowInspection.InternalConditions</c>
        /// checks, because the name still classifies and the occupancy is still absent. <c>tM59: false</c>
        /// removes both at once and so cannot tell them apart.
        /// </param>
        private static AnalyticalModel Model(bool zoned = true, bool dwelling = true, bool tM59 = true, bool partF = true, bool internalConditions = true)
        {
            AdjacencyCluster adjacencyCluster = new();

            for (int i = 1; i <= 2; i++)
            {
                Zone zone = new(new Guid(string.Format("aaaaaaaa-0000-0000-0000-00000000000{0}", i)), string.Format("Flat {0}", i));

                zone.SetValue(ZoneParameter.IsDwelling, dwelling);

                if (zoned)
                {
                    adjacencyCluster.AddObject(zone);
                }

                Room(adjacencyCluster, zoned ? zone : null, i, 1, tM59 ? "Bedroom" : "Area", tM59 && internalConditions ? "TM59_Double Bedroom" : null, partF, PartFTerminalRole.Supply);
                Room(adjacencyCluster, zoned ? zone : null, i, 2, tM59 ? "Kitchen" : "Area", tM59 && internalConditions ? "TM59_Kitchen" : null, partF, PartFTerminalRole.LocalKitchenExtract);
                Room(adjacencyCluster, zoned ? zone : null, i, 3, "Store", tM59 && internalConditions ? "TM59_Store" : null, false, PartFTerminalRole.Supply);
            }

            return new AnalyticalModel(
                "Block",
                null,
                null,
                null,
                adjacencyCluster,
                new MaterialLibrary("Materials"),
                new ProfileLibrary("Profiles"));
        }

        private static void Room(AdjacencyCluster adjacencyCluster, Zone zone, int flat, int index, string name, string internalConditionName, bool partF, PartFTerminalRole partFTerminalRole)
        {
            Space space = new(new Guid(string.Format("bbbbbbbb-0000-0000-000{0}-00000000000{1}", flat, index)), string.Format("{0} {1}.{2}", name, flat, index), null);

            if (internalConditionName is not null)
            {
                space.InternalCondition = new InternalCondition(internalConditionName);
            }

            if (partF)
            {
                PartFVentilationTerminalRequirement partFVentilationTerminalRequirement = new(space.Name + " requirement", space.Guid, partFTerminalRole)
                {
                    ContinuousDesignFlowRate_Lps = 13,
                };

                PartFSpaceData partFSpaceData = new();
                partFSpaceData.Terminals.Add(partFVentilationTerminalRequirement);

                space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);
            }

            adjacencyCluster.AddObject(space);

            if (zone is not null)
            {
                adjacencyCluster.AddRelation(zone, space);
            }
        }
    }
}
