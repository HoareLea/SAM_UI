// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Prepare &amp; Run exposes and uses the SAME equipment-selection configuration as Prepare
    /// Iteration.</b>
    ///
    /// <para><b>The defect these tests close</b></para>
    /// <para>
    /// Native testing found Prepare &amp; Run still describing the old generic Iteration 2 behaviour - "the
    /// smallest capable manufacturer unit is selected per dwelling" - on a project the engineer had put
    /// under <b>manual</b> authority, where no selection rule runs at all. It also offered no way to choose
    /// a pool or edit one from that workflow. An engineer reading that line would have believed a selection
    /// had happened that had not.
    /// </para>
    ///
    /// <para><b>One configuration, two windows</b></para>
    /// <para>
    /// There is no Prepare &amp; Run pool and no Prepare Iteration pool. There is the PROJECT's
    /// <c>AnalyticalModelParameter.PartOEquipmentSelection</c>, and both windows host the same
    /// <c>PartOEquipmentSelectionControl</c> over it - which is why a change made in one is seen by the
    /// other, and why neither can describe the same mode differently.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowEquipmentSelectionTests
    {
        private const string model_MRXBOX = "MRXBOXAB-ECO5-AECV";

        private const string model_XBC15 = "XBC15";

        // =================================================================================================
        // 1. What Prepare & Run loads
        // =================================================================================================

        /// <summary>
        /// A project that has never stated a preference opens on the historic default - automatic over every
        /// selectable product - with the whole catalogue visible.
        /// </summary>
        [WpfFact]
        public void PrepareAndRun_LoadsTheHistoricDefault_AndShowsTheCatalogue()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticAllProducts, partOWorkflowWindow.Mode);

            List<PartOCatalogueProductRow> rows = partOWorkflowWindow.CatalogueProductRows;

            Assert.Equal(2, rows.Count);
            Assert.Equal(150, Row(rows, model_MRXBOX).MaximumSupply_Lps);
            Assert.Equal(190, Row(rows, model_XBC15).MaximumSupply_Lps);

            //Every product ticked, and locked - the mode has no choice to offer, but the catalogue is still
            //there to be read.
            Assert.All(rows, x => Assert.True(x.IsUsed));
            Assert.False(partOWorkflowWindow.IsPoolEditable);
        }

        /// <summary>
        /// A project already under a narrowed pool opens on that pool, with the excluded product visibly
        /// unticked - not on the default.
        /// </summary>
        [WpfFact]
        public void PrepareAndRun_LoadsTheProjectsExistingPool()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, XBC15Reference())));

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOWorkflowWindow.Mode);

            Assert.False(Row(partOWorkflowWindow.CatalogueProductRows, model_MRXBOX).IsUsed);
            Assert.True(Row(partOWorkflowWindow.CatalogueProductRows, model_XBC15).IsUsed);

            //And the pool is editable here, which is the other half of the gap: the pool could not be
            //changed from this workflow at all.
            Assert.True(partOWorkflowWindow.IsPoolEditable);
        }

        /// <summary>
        /// <b>A manual project opened through Prepare &amp; Run stays manual.</b> No selection rule runs -
        /// the candidate set is absent, which is what <c>Modify.PreparePartOIteration</c> reads as "select
        /// nothing and leave every existing identity alone".
        /// </summary>
        [WpfFact]
        public void AManualProject_OpenedThroughPrepareAndRun_StaysManual_AndRunsNoRule()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(Pool(PartOEquipmentSelectionMode.ManualPerDwelling, XBC15Reference())));

            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOWorkflowWindow.Mode);

            PartOEquipmentSelection partOEquipmentSelection = partOWorkflowWindow.Request.EquipmentSelection;

            Assert.NotNull(partOEquipmentSelection);
            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOEquipmentSelection.Mode);

            //THE assertion: nothing to select from, so nothing is selected and the authored identities on
            //the air handling units are what the preparation keeps.
            Assert.Null(partOEquipmentSelection.CandidateDescriptors(Descriptors()));
        }

        /// <summary>
        /// And the identity a manual project authored is untouched by opening the window - reading a
        /// configuration is not a preparation.
        /// </summary>
        [WpfFact]
        public void OpeningPrepareAndRun_PreservesAuthoredIdentities()
        {
            AnalyticalModel analyticalModel = Model(Pool(PartOEquipmentSelectionMode.ManualPerDwelling), assignXBC15: true);

            Window(analyticalModel);

            AirHandlingUnit airHandlingUnit = Assert.Single(analyticalModel.AdjacencyCluster.GetObjects<AirHandlingUnit>());

            Assert.Equal(model_XBC15, airHandlingUnit.SelectedVentilationUnitReference()?.Model);
        }

        // =================================================================================================
        // 2 and 3. The same configuration, in both directions
        // =================================================================================================

        /// <summary>
        /// <b>Prepare &amp; Run to Prepare Iteration.</b> A pool chosen in Prepare &amp; Run, stamped on the
        /// project as the preparation stamps it, is the pool the single-command window then opens on.
        /// </summary>
        [WpfFact]
        public void APoolChosenInPrepareAndRun_IsWhatPrepareIterationOpensOn()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            //The engineer narrows the pool here, in the workflow that could not do it before.
            partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool);
            Row(partOWorkflowWindow.CatalogueProductRows, model_MRXBOX).IsUsed = false;

            AnalyticalModel analyticalModel = Stamped(partOWorkflowWindow.EquipmentSelection);

            PartOIterationWindow partOIterationWindow = IterationWindow(analyticalModel);

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOIterationWindow.Mode);
            Assert.Equal(model_XBC15, Assert.Single(partOIterationWindow.EquipmentSelection.AllowedVentilationUnitReferences).Model);
            Assert.False(Row(partOIterationWindow.CatalogueProductRows, model_MRXBOX).IsUsed);
        }

        /// <summary>
        /// <b>And back.</b> A pool chosen in Prepare Iteration, stamped on the project, is the pool
        /// Prepare &amp; Run then opens on.
        /// </summary>
        [WpfFact]
        public void APoolChosenInPrepareIteration_IsWhatPrepareAndRunOpensOn()
        {
            PartOIterationWindow partOIterationWindow = IterationWindow(Model(null));

            partOIterationWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool);
            Row(partOIterationWindow.CatalogueProductRows, model_XBC15).IsUsed = false;

            PartOWorkflowWindow partOWorkflowWindow = Window(Stamped(partOIterationWindow.EquipmentSelection));

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOWorkflowWindow.Mode);
            Assert.Equal(model_MRXBOX, Assert.Single(partOWorkflowWindow.EquipmentSelection.AllowedVentilationUnitReferences).Model);
            Assert.False(Row(partOWorkflowWindow.CatalogueProductRows, model_XBC15).IsUsed);
        }

        /// <summary>
        /// Manual authority travels the same way, which is the case the defect was actually found on: the
        /// window that used to describe a smallest-capable selection now opens on Manual and says so.
        /// </summary>
        [WpfFact]
        public void ManualAuthorityChosenInPrepareIteration_IsWhatPrepareAndRunOpensOn()
        {
            PartOIterationWindow partOIterationWindow = IterationWindow(Model(null));

            partOIterationWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling);

            PartOWorkflowWindow partOWorkflowWindow = Window(Stamped(partOIterationWindow.EquipmentSelection));

            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOWorkflowWindow.Mode);
            Assert.Contains("Manual per dwelling", partOWorkflowWindow.ScenarioDescription);
        }

        /// <summary>
        /// The window's request carries what the window states, so the preparation runs under it rather than
        /// under a default that would have reselected everything.
        /// </summary>
        [WpfFact]
        public void TheRequest_CarriesWhatTheWindowStates()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            partOWorkflowWindow.EquipmentSelection = Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, XBC15Reference());

            PartOEquipmentSelection partOEquipmentSelection = partOWorkflowWindow.Request.EquipmentSelection;

            Assert.NotNull(partOEquipmentSelection);
            Assert.Equal(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOEquipmentSelection.Mode);
            Assert.Equal(model_XBC15, Assert.Single(partOEquipmentSelection.AllowedVentilationUnitReferences).Model);
        }

        // =================================================================================================
        // 4, 5 and 6. What the pool then selects - the production rule, over exactly what was permitted
        // =================================================================================================

        /// <summary>
        /// A pool of the XBC15 alone puts the XBC15 on the accepted fixture's dwellings, at duties the
        /// MRXBOX could have served. That is the point of a pool.
        /// </summary>
        [WpfTheory]
        [InlineData(30)]
        [InlineData(63)]
        public void APoolOfXBC15_SelectsXBC15_AtTheAcceptedDuties(double duty_Lps)
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            partOWorkflowWindow.EquipmentSelection = Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, XBC15Reference());

            VentilationUnitSelection ventilationUnitSelection = Select(partOWorkflowWindow, duty_Lps);

            Assert.True(ventilationUnitSelection.IsSelected);
            Assert.Equal(model_XBC15, ventilationUnitSelection.VentilationUnitReference.Model);
        }

        /// <summary>
        /// A pool of the MRXBOX alone, against a duty beyond its 150 l/s, refuses - and does not reach past
        /// the pool for the XBC15 that could have served it.
        /// </summary>
        [WpfFact]
        public void APoolOfMRXBOX_BeyondItsRating_Refuses_WithNoXBC15Fallback()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            partOWorkflowWindow.EquipmentSelection = Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, MRXBOXReference());

            VentilationUnitSelection ventilationUnitSelection = Select(partOWorkflowWindow, 175);

            Assert.False(ventilationUnitSelection.IsSelected);
            Assert.Null(ventilationUnitSelection.VentilationUnitReference);

            //The pool's own ceiling is what the refusal is about, and the excluded product is not named as
            //an answer.
            Assert.Contains("150", ventilationUnitSelection.Reason);
            Assert.DoesNotContain(model_XBC15, ventilationUnitSelection.Reason);
        }

        /// <summary>
        /// <b>An empty pool in Prepare &amp; Run refuses explicitly, and never widens.</b> The window says
        /// the run will not prepare and says why, and the candidate set is empty rather than the catalogue.
        /// </summary>
        [WpfFact]
        public void AnEmptyPool_RefusesExplicitly_AndNeverWidens()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool);

            foreach (PartOCatalogueProductRow partOCatalogueProductRow in partOWorkflowWindow.CatalogueProductRows)
            {
                partOCatalogueProductRow.IsUsed = false;
            }

            Assert.Empty(partOWorkflowWindow.EquipmentSelection.CandidateDescriptors(Descriptors()));

            VentilationUnitSelection ventilationUnitSelection = Select(partOWorkflowWindow, 30);

            Assert.False(ventilationUnitSelection.IsSelected);
            Assert.Contains("No ventilation unit product was offered", ventilationUnitSelection.Reason);

            Assert.Contains("will not prepare", partOWorkflowWindow.CatalogueDescription);
            Assert.Contains("will not fall back", partOWorkflowWindow.CatalogueDescription);
        }

        // =================================================================================================
        // 8. The dynamic description - the misleading line, fixed
        // =================================================================================================

        /// <summary>
        /// The scenario description states the ACTIVE mode. Each of the three reads differently, and the
        /// manual one no longer claims a selection runs.
        /// </summary>
        [WpfFact]
        public void TheScenarioDescription_StatesTheActiveMode()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts);

            Assert.Contains("Automatic - all catalogue products", partOWorkflowWindow.ScenarioDescription);
            Assert.Contains("full selectable catalogue", partOWorkflowWindow.ScenarioDescription);
            Assert.Contains("ceiling", partOWorkflowWindow.ScenarioDescription);

            partOWorkflowWindow.EquipmentSelection = Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, XBC15Reference());

            Assert.Contains("Automatic - selected pool", partOWorkflowWindow.ScenarioDescription);
            Assert.Contains("No fallback to the full catalogue", partOWorkflowWindow.ScenarioDescription);

            partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling);

            Assert.Contains("Manual per dwelling", partOWorkflowWindow.ScenarioDescription);
            Assert.Contains("preserved", partOWorkflowWindow.ScenarioDescription);
            Assert.Contains("No automatic equipment selection runs", partOWorkflowWindow.ScenarioDescription);
        }

        /// <summary>
        /// <b>The regression, stated directly.</b> Under manual authority the window must not claim a
        /// smallest-capable selection - which is exactly the sentence it used to print unconditionally.
        /// </summary>
        [WpfFact]
        public void UnderManualAuthority_TheDescriptionNeverClaimsASelectionRan()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(Pool(PartOEquipmentSelectionMode.ManualPerDwelling)));

            Assert.DoesNotContain("smallest capable", partOWorkflowWindow.ScenarioDescription);
        }

        /// <summary>An empty pool says so in the scenario line too, not only under the grid.</summary>
        [WpfFact]
        public void AnEmptyPool_SaysSoInTheScenarioDescription()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(null));

            partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool);

            foreach (PartOCatalogueProductRow partOCatalogueProductRow in partOWorkflowWindow.CatalogueProductRows)
            {
                partOCatalogueProductRow.IsUsed = false;
            }

            Assert.Contains("no product permitted", partOWorkflowWindow.ScenarioDescription);
            Assert.Contains("will not prepare", partOWorkflowWindow.ScenarioDescription);
        }

        // =================================================================================================
        // 9. Changing the mode or the pool changes no airflow
        // =================================================================================================

        /// <summary>
        /// <b>Configuring equipment selection is not an engineering change.</b> Walking all three modes and
        /// re-ticking the pool leaves every Approved Document F requirement, every design airflow and every
        /// runtime airflow in the model exactly where it was - because this window states an intent and
        /// writes nothing.
        /// </summary>
        [WpfFact]
        public void ChangingTheModeOrThePool_ChangesNoAirflow()
        {
            AnalyticalModel analyticalModel = Model(null, assignXBC15: true);

            string before = analyticalModel.AdjacencyCluster.ToJsonObject().ToJsonString();

            PartOWorkflowWindow partOWorkflowWindow = Window(analyticalModel);

            foreach (PartOEquipmentSelectionMode partOEquipmentSelectionMode in new[]
            {
                PartOEquipmentSelectionMode.AutomaticSelectedPool,
                PartOEquipmentSelectionMode.ManualPerDwelling,
                PartOEquipmentSelectionMode.AutomaticAllProducts,
            })
            {
                partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(partOEquipmentSelectionMode);

                foreach (PartOCatalogueProductRow partOCatalogueProductRow in partOWorkflowWindow.CatalogueProductRows)
                {
                    partOCatalogueProductRow.IsUsed = !partOCatalogueProductRow.IsUsed;
                }
            }

            //Byte for byte: no requirement, no design airflow, no transfer airflow, no runtime airflow and
            //no selected identity moved.
            Assert.Equal(before, analyticalModel.AdjacencyCluster.ToJsonObject().ToJsonString());
        }

        // =================================================================================================
        // Preparation reuse - the silent way a new mode could have been ignored
        // =================================================================================================

        /// <summary>
        /// <b>A prepared iteration is not reused for a different equipment configuration.</b> Prepare &amp;
        /// Run may simulate an already-prepared model instead of preparing again; reusing one prepared under
        /// "automatic - all" for a request that has since narrowed the pool would simulate the OLD products
        /// while the window reported the new configuration. Nothing would fail - the answer would just be
        /// about different equipment than the screen claimed.
        /// </summary>
        [Fact]
        public void APreparationUnderADifferentMode_IsNotReused()
        {
            List<Zone> zones = [Zone("Flat 1")];

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, Strategies(zones), Descriptors())
            {
                EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts),
            };

            //Same iteration, same scope, same strategies, same catalogue offered - so everything the match
            //looked at before this change is identical.
            Assert.True(Reusable(partOPreparationContext, new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts), zones));

            //Only the mode moved.
            Assert.False(Reusable(partOPreparationContext, new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling), zones));

            //Only the pool moved.
            Assert.False(Reusable(partOPreparationContext, Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, XBC15Reference()), zones));
        }

        /// <summary>
        /// And the same pool listed the other way round IS reused - order carries no meaning, so refusing
        /// reuse over it would repeat a whole preparation for nothing.
        /// </summary>
        [Fact]
        public void TheSamePoolInAnotherOrder_IsStillReused()
        {
            List<Zone> zones = [Zone("Flat 1")];

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, Strategies(zones), Descriptors())
            {
                EquipmentSelection = Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, MRXBOXReference(), XBC15Reference()),
            };

            Assert.True(Reusable(partOPreparationContext, Pool(PartOEquipmentSelectionMode.AutomaticSelectedPool, XBC15Reference(), MRXBOXReference()), zones));
        }

        /// <summary>
        /// A preparation made before a mode existed carries none, and reads as the historic default - so an
        /// older session's prepared iteration is still reusable by a request that states that default.
        /// </summary>
        [Fact]
        public void APreparationThatRecordsNoMode_ReadsAsTheHistoricDefault()
        {
            List<Zone> zones = [Zone("Flat 1")];

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, Strategies(zones), Descriptors());

            Assert.Null(partOPreparationContext.EquipmentSelection);

            Assert.True(Reusable(partOPreparationContext, new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts), zones));
            Assert.True(Reusable(partOPreparationContext, null, zones));
            Assert.False(Reusable(partOPreparationContext, new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling), zones));
        }

        /// <summary>
        /// On an Iteration 1a request the mode is moot - no equipment is selected either way - so reuse is
        /// not refused over it. Refusing would repeat a whole preparation, and on an isolated run re-derive
        /// its geometry, for a difference that does not exist.
        /// </summary>
        [Fact]
        public void OnAnIteration1aRequest_TheModeDoesNotRefuseReuse()
        {
            List<Zone> zones = [Zone("Flat 1")];

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, Strategies(zones), null)
            {
                EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts),
            };

            Assert.False(partOPreparationContext.HasVentilationUnitCatalogue);

            Assert.True(Reusable(partOPreparationContext, new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling), zones, selectVentilationUnit: false));
        }

        // =================================================================================================
        // Fixtures
        // =================================================================================================

        /// <summary>Whether the inspection would reuse a prepared iteration for this request.</summary>
        private static bool Reusable(PartOPreparationContext partOPreparationContext, PartOEquipmentSelection? partOEquipmentSelection, List<Zone> zones, bool selectVentilationUnit = true)
        {
            PartORun partORun = new();

            //One overheating scenario, because a preparation that stated none is not a prepared run at
            //all - PartORun.Prepare refuses it, and the reuse question would never be reached.
            Assert.True(partORun.Prepare(Model(null), [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)], partOPreparationContext));

            PartOWorkflowRequest partOWorkflowRequest = new(Option(), PartOWorkflowScope.SelectedDwellings, zones, selectVentilationUnit)
            {
                EquipmentSelection = partOEquipmentSelection,
            };

            PartOWorkflowInspection partOWorkflowInspection = PartOWorkflowInspection.Inspect(Model(null), partOWorkflowRequest, partORun, new PartOWorkflowCapabilities { EquipmentAvailable = true }, null);

            return partOWorkflowInspection.ReusePreparation;
        }

        private static PartOVentilationStrategyOption Option()
        {
            return PartOVentilationStrategyOption.Options.Find(x => x.PartOVentilationMode == PartOVentilationMode.MVHR);
        }

        private static Dictionary<Guid, string> Strategies(List<Zone> zones)
        {
            Dictionary<Guid, string> result = [];

            foreach (Zone zone in zones)
            {
                result[zone.Guid] = Option().VentilationStrategy;
            }

            return result;
        }

        private static Zone Zone(string name)
        {
            Zone result = new(name);

            result.SetValue(ZoneParameter.IsDwelling, true);

            return result;
        }

        /// <summary>The production selection rule, over exactly what the window currently permits.</summary>
        private static VentilationUnitSelection Select(PartOWorkflowWindow partOWorkflowWindow, double duty_Lps)
        {
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = partOWorkflowWindow.EquipmentSelection.CandidateDescriptors(Descriptors());

            Assert.NotNull(ventilationUnitCapacityDescriptors);

            return ventilationUnitCapacityDescriptors.SelectSmallestCapableVentilationUnit(duty_Lps, duty_Lps);
        }

        /// <summary>The Prepare &amp; Run window over the two-product catalogue, set up as production sets it up.</summary>
        private static PartOWorkflowWindow Window(AnalyticalModel analyticalModel)
        {
            PartOWorkflowWindow result = new()
            {
                //Order matters, and production uses this order: the model builds the dwelling list, then the
                //catalogue arrives. Both orders have to seed the pool - see the catalogue setter.
                AnalyticalModel = analyticalModel,
                PartORun = new PartORun(),
                VentilationUnitCatalogue = Catalogue(),
                Capabilities = new PartOWorkflowCapabilities { EquipmentAvailable = true },
            };

            //The Iteration 2 scenario, so equipment selection is in play at all.
            result.Restore(PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit), PartOWorkflowScope.AllDwellings, null);

            result.CompleteInitialisation();

            return result;
        }

        private static PartOIterationWindow IterationWindow(AnalyticalModel analyticalModel)
        {
            PartOIterationWindow result = new()
            {
                Zones = analyticalModel.GetZones() ?? [],
                VentilationUnitCatalogue = Catalogue(),
            };

            //Exactly what Modify.PreparePartOIteration does: catalogue first, then the project's own
            //preselection, because the pool is restored by ticking catalogue rows.
            result.EquipmentSelection = analyticalModel.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection);

            return result;
        }

        /// <summary>
        /// A project stamped with a preselection, as <c>Modify.PreparePartOIteration</c> stamps it onto the
        /// prepared model - which is the mechanism by which the two windows see one configuration.
        /// </summary>
        private static AnalyticalModel Stamped(PartOEquipmentSelection partOEquipmentSelection)
        {
            return Model(partOEquipmentSelection);
        }

        /// <summary>
        /// One dwelling with one Approved Document F sized room, and optionally a stamped preselection and
        /// an air handling unit already authored as the XBC15.
        /// </summary>
        private static AnalyticalModel Model(PartOEquipmentSelection? partOEquipmentSelection, bool assignXBC15 = false)
        {
            AdjacencyCluster adjacencyCluster = new();

            Zone zone = Zone("Flat 1");

            adjacencyCluster.AddObject(zone);

            Space space = new("Bedroom", null)
            {
                InternalCondition = new InternalCondition("TM59_Double Bedroom"),
            };

            PartFVentilationTerminalRequirement partFVentilationTerminalRequirement = new(space.Name + " requirement", space.Guid, PartFTerminalRole.Supply)
            {
                ContinuousDesignFlowRate_Lps = 13,
            };

            PartFSpaceData partFSpaceData = new();
            partFSpaceData.Terminals.Add(partFVentilationTerminalRequirement);

            space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);

            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddRelation(zone, space);

            if (assignXBC15)
            {
                AirHandlingUnit airHandlingUnit = Analytical.Create.AirHandlingUnit("MVHR-01");

                airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, XBC15Reference());

                adjacencyCluster.AddObject(airHandlingUnit);
            }

            AnalyticalModel result = new("Block", null, null, null, adjacencyCluster, new MaterialLibrary("Materials"), new ProfileLibrary("Profiles"));

            if (partOEquipmentSelection is not null)
            {
                result.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, partOEquipmentSelection);
            }

            return result;
        }

        private static PartOEquipmentSelection Pool(PartOEquipmentSelectionMode partOEquipmentSelectionMode, params VentilationUnitReference[] ventilationUnitReferences)
        {
            return new PartOEquipmentSelection(partOEquipmentSelectionMode, ventilationUnitReferences);
        }

        private static List<VentilationUnitCapacityDescriptor> Descriptors()
        {
            return
            [
                new VentilationUnitCapacityDescriptor(MRXBOXReference(), 150, 150, 10),
                new VentilationUnitCapacityDescriptor(XBC15Reference(), 190, 190, 20),
            ];
        }

        private static VentilationUnitReference MRXBOXReference()
        {
            return new VentilationUnitReference("Nuaire", model_MRXBOX, "MR-ECO-COOL-V");
        }

        private static VentilationUnitReference XBC15Reference()
        {
            return new VentilationUnitReference("Nuaire", model_XBC15, null);
        }

        private static PartOCatalogueProductRow Row(List<PartOCatalogueProductRow> rows, string model)
        {
            PartOCatalogueProductRow result = rows.Find(x => x.Model == model);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>
        /// The two shipped products, written to a temporary directory and read back through the production
        /// reader - so the windows are given a real catalogue rather than a stub.
        /// </summary>
        private static VentilationUnitCatalogue Catalogue()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_WorkflowEquipment_{0}", Guid.NewGuid()));

            Directory.CreateDirectory(directory);

            File.WriteAllText(Path.Combine(directory, "VentilationUnitCatalogue.JSON"), """
            {
              "Schema": "VentilationUnitCatalogue:v1",
              "Templates": [
                {
                  "_type": "SAM.Analytical.VentilationUnitTemplate,SAM.Analytical",
                  "Name": "Nuaire MRXBOXAB-ECO5-AECV (MR-ECO-COOL-V)",
                  "VentilationUnitReference": {
                    "_type": "SAM.Analytical.VentilationUnitReference,SAM.Analytical",
                    "Name": "Nuaire MRXBOXAB-ECO5-AECV",
                    "Manufacturer": "Nuaire",
                    "Model": "MRXBOXAB-ECO5-AECV",
                    "Reference": "MR-ECO-COOL-V"
                  },
                  "Source": "Written by this test at the shipped catalogue's published capacity. A template needs a traceable source to be valid at all.",
                  "MaximumSupplyFlowRate_Lps": 150,
                  "MaximumExtractFlowRate_Lps": 150,
                  "Rank": 10
                },
                {
                  "_type": "SAM.Analytical.VentilationUnitTemplate,SAM.Analytical",
                  "Name": "Nuaire XBOXER XBC15",
                  "VentilationUnitReference": {
                    "_type": "SAM.Analytical.VentilationUnitReference,SAM.Analytical",
                    "Name": "Nuaire XBC15",
                    "Manufacturer": "Nuaire",
                    "Model": "XBC15"
                  },
                  "Source": "Written by this test at the shipped catalogue's published capacity. A template needs a traceable source to be valid at all.",
                  "MaximumSupplyFlowRate_Lps": 190,
                  "MaximumExtractFlowRate_Lps": 190,
                  "Rank": 20
                }
              ]
            }
            """);

            return VentilationUnitCatalogue.Read(directory);
        }
    }
}
