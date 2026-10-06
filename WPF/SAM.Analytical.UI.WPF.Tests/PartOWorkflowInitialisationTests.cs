// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>How many times opening the Part O hub inspects the analytical model.</b>
    ///
    /// <para><b>The defect</b></para>
    /// <para>
    /// Opening the hub is one gesture and it moved seven inspection inputs one at a time, each through a
    /// control event the window correctly answers with a full inspection:
    /// </para>
    /// <list type="number">
    /// <item>the constructor, once the controls are settled;</item>
    /// <item>the analytical model;</item>
    /// <item>the Part O run;</item>
    /// <item>the ventilation unit catalogue;</item>
    /// <item>the session capabilities;</item>
    /// <item><c>Restore</c> putting the scenario back, through the combo's <c>SelectionChanged</c>;</item>
    /// <item><c>Restore</c> putting the scope back, through the other combo's;</item>
    /// <item><c>Restore</c> putting the saved dwelling scope back, through
    /// <c>PartODwellingSelection.SelectionChanged</c>;</item>
    /// <item><c>Restore</c>'s own closing refresh.</item>
    /// </list>
    /// <para>
    /// <b>Nine inspections to show one window</b>, eight of them of an initial state that had already been
    /// superseded before anybody could see it. Each one walks the dwelling scope, so on a five thousand space
    /// project the whole cost was paid nine times over.
    /// </para>
    /// <para>
    /// <b>Nine is measured, not counted off the source.</b> <see cref="Constructed"/> deliberately exercises
    /// all nine triggers - which is why it restores the LAST scenario rather than the first; see there - and
    /// removing the deferral from <c>PartOWorkflowWindow</c> makes
    /// <see cref="OpeningTheHubOnABlock_IsStillOneInspection"/> report exactly nine on this fixture.
    /// </para>
    ///
    /// <para><b>What these assert, and what they must not let slip</b></para>
    /// <para>
    /// Section 1: opening the hub inspects <b>once</b>, over the fully restored state, whatever the size of
    /// the model. Section 2 is the half that matters more - after initialisation the window is eager again,
    /// and every genuine change of scenario, scope, dwelling selection, model, run, catalogue or capability
    /// still inspects immediately. Cheapening the opening must not have cheapened the real path.
    /// </para>
    /// <para>
    /// Counted, never timed. <c>InspectionCount</c> is exact and identical on every machine.
    /// </para>
    /// <para>
    /// <b>Nothing here is about what an inspection reports.</b> That is unchanged and is pinned in
    /// <c>PartOWorkflowTests</c>; this file is about how often it is asked, and
    /// <c>PartOWorkflowRefreshTests</c> is about the interactions that must not ask at all.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowInitialisationTests
    {
        /// <summary>How many dwellings a "hundreds of dwellings" case carries. A real block reaches this.</summary>
        private const int Dwellings = 500;

        private readonly ITestOutputHelper _output;

        public PartOWorkflowInitialisationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // -------------------------------------------------------------------------------------------------
        // 1. Opening the hub is one inspection
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>The whole claim.</b> Constructed, given a model, a run, a catalogue and its capabilities, then
        /// restored to a saved scenario, scope, dwelling set and optimisation settings - and the analytical
        /// model is inspected exactly once, at the end.
        /// </summary>
        [WpfFact]
        public void OpeningTheHub_InspectsTheModelExactlyOnce()
        {
            AnalyticalModel analyticalModel = Model(12);

            PartOWorkflowWindow partOWorkflowWindow = Constructed(analyticalModel, out List<Guid> guids_Restored);

            //Nothing at all before initialisation is completed: the states above are ones nobody will see.
            Assert.Equal(0, partOWorkflowWindow.InspectionCount);

            partOWorkflowWindow.CompleteInitialisation();

            Assert.Equal(1, partOWorkflowWindow.InspectionCount);

            //And it is an inspection of the RESTORED state, not of the blank one the constructor left.
            Assert.NotNull(partOWorkflowWindow.Inspection);
            Assert.Equal(PartOWorkflowScope.SelectedDwellings, partOWorkflowWindow.Scope);
            Assert.Equal(guids_Restored.Count, partOWorkflowWindow.Zones_Dwelling.Count);
            Assert.Contains(string.Format("{0} of 12", guids_Restored.Count), partOWorkflowWindow.ScopeDescription, StringComparison.Ordinal);
            Assert.Contains(string.Format("{0} of 12 eligible dwelling zones in scope", guids_Restored.Count), Stage(partOWorkflowWindow, PartOWorkflowStage.DwellingScope).Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// The count does not depend on the size of the model. A block of five hundred dwellings restored to a
        /// narrowed scope is still one inspection - which is the whole point, because on a model that size
        /// each one is the expensive thing.
        /// </summary>
        [WpfFact]
        public void OpeningTheHubOnABlock_IsStillOneInspection()
        {
            AnalyticalModel analyticalModel = Model(Dwellings);

            PartOWorkflowWindow partOWorkflowWindow = Constructed(analyticalModel, out List<Guid> guids_Restored);

            partOWorkflowWindow.CompleteInitialisation();

            _output.WriteLine("{0} dwellings, {1} restored: {2} inspection(s)", Dwellings, guids_Restored.Count, partOWorkflowWindow.InspectionCount);

            Assert.Equal(1, partOWorkflowWindow.InspectionCount);
            Assert.Equal(guids_Restored.Count, partOWorkflowWindow.Zones_Dwelling.Count);
        }

        /// <summary>
        /// <b>Completing initialisation twice is not two inspections.</b> The caller calls it; so does the
        /// window when it is shown or when anything derived is read. All of those together are still one.
        /// </summary>
        [WpfFact]
        public void CompletingInitialisationTwice_IsStillOneInspection()
        {
            PartOWorkflowWindow partOWorkflowWindow = Constructed(Model(12), out List<Guid> _);

            partOWorkflowWindow.CompleteInitialisation();
            partOWorkflowWindow.CompleteInitialisation();

            Assert.Equal(1, partOWorkflowWindow.InspectionCount);
        }

        /// <summary>
        /// A caller that never completes initialisation is <b>not</b> left looking at a window derived from
        /// nothing: reading anything the inspection writes pays the deferred inspection first.
        /// <para>
        /// This is what keeps the deferral safe. It is also why every existing test in
        /// <c>PartOWorkflowRefreshTests</c>, none of which calls <c>CompleteInitialisation</c>, still reads
        /// exactly the status list it did before.
        /// </para>
        /// </summary>
        [WpfFact]
        public void ReadingTheStatusWithoutCompletingInitialisation_PaysTheDeferredInspection()
        {
            PartOWorkflowWindow partOWorkflowWindow = Constructed(Model(12), out List<Guid> guids_Restored);

            Assert.Equal(0, partOWorkflowWindow.InspectionCount);

            //The first read of anything derived.
            Assert.NotNull(partOWorkflowWindow.Inspection);

            Assert.Equal(1, partOWorkflowWindow.InspectionCount);
            Assert.Contains(string.Format("{0} of 12", guids_Restored.Count), partOWorkflowWindow.ScopeDescription, StringComparison.Ordinal);

            //And reading more of it does not inspect again.
            _ = partOWorkflowWindow.CanRun;
            _ = partOWorkflowWindow.BlockerDescription;
            _ = partOWorkflowWindow.ScenarioDescription;
            _ = partOWorkflowWindow.OptimisationDescription;
            _ = partOWorkflowWindow.SelectionDescription;

            Assert.Equal(1, partOWorkflowWindow.InspectionCount);
        }

        /// <summary>
        /// A dialog nothing was set on inspects once, not zero times - the deferral defers an inspection that
        /// was owed; it does not remove one. The constructor's own refresh is that owed inspection.
        /// </summary>
        [WpfFact]
        public void AnEmptyDialog_StillInspectsOnce()
        {
            PartOWorkflowWindow partOWorkflowWindow = new();

            partOWorkflowWindow.CompleteInitialisation();

            Assert.Equal(1, partOWorkflowWindow.InspectionCount);

            //And it says what a dialog with no model says, rather than nothing.
            Assert.False(partOWorkflowWindow.CanRun);
            Assert.Contains("No analytical model is open", partOWorkflowWindow.BlockerDescription, StringComparison.Ordinal);
        }

        // -------------------------------------------------------------------------------------------------
        // 2. After initialisation, a genuine change still inspects - immediately
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Every inspection input still inspects, once each, the moment it moves. This is the half of the
        /// change that could do damage, so each input is moved separately and counted.
        /// </summary>
        [WpfFact]
        public void AfterInitialisation_EveryInspectionInputStillInspectsWhenItMoves()
        {
            AnalyticalModel analyticalModel = Model(12);

            PartOWorkflowWindow partOWorkflowWindow = Constructed(analyticalModel, out List<Guid> _);

            partOWorkflowWindow.CompleteInitialisation();

            int count = partOWorkflowWindow.InspectionCount;

            Assert.Equal(1, count);

            //The scope.
            partOWorkflowWindow.Scope = PartOWorkflowScope.SelectedDwellingsIsolated;
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);

            //The dwelling selection - one gesture, one inspection, however many rows it moves.
            partOWorkflowWindow.DwellingSelection.SetSelected(true);
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);

            partOWorkflowWindow.DwellingSelection.Items[0].IsSelected = false;
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);

            //The run.
            partOWorkflowWindow.PartORun = new PartORun();
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);

            //The session capabilities.
            partOWorkflowWindow.Capabilities = new PartOWorkflowCapabilities();
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);

            //The catalogue.
            partOWorkflowWindow.VentilationUnitCatalogue = null;
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);

            //The equipment selection. A preparation input - it decides which products a preparation
            //selects - so it inspects, and ONCE. Setting it writes to the hosted control, which raises its
            //own change event; answering the window's own write as well would buy a second pass over every
            //dwelling in scope for one gesture. See PartOWorkflowWindow.writing_EquipmentSelection.
            partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling);
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);
            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOWorkflowWindow.Mode);

            //The model. Setting it rebuilds the dwelling list, and that is one inspection too.
            partOWorkflowWindow.AnalyticalModel = Model(3);
            Assert.Equal(++count, partOWorkflowWindow.InspectionCount);

            Assert.Equal(3, partOWorkflowWindow.DwellingSelection.Count);
        }

        /// <summary>
        /// And a later <c>Restore</c> - the hub reopening after an action, with the choices carried across -
        /// still inspects. It is no longer initialisation, so it is not deferred.
        /// </summary>
        [WpfFact]
        public void AfterInitialisation_ARestoreStillInspects()
        {
            AnalyticalModel analyticalModel = Model(12);

            PartOWorkflowWindow partOWorkflowWindow = Constructed(analyticalModel, out List<Guid> _);

            partOWorkflowWindow.CompleteInitialisation();

            PartOWorkflowInspection inspection = partOWorkflowWindow.Inspection;

            int count = partOWorkflowWindow.InspectionCount;

            partOWorkflowWindow.Restore(null, PartOWorkflowScope.AllDwellings, null);

            Assert.True(partOWorkflowWindow.InspectionCount > count, "A restore after initialisation did not inspect the model at all.");
            Assert.NotSame(inspection, partOWorkflowWindow.Inspection);
            Assert.Equal(12, partOWorkflowWindow.Zones_Dwelling.Count);
        }

        /// <summary>
        /// A workflow input that cannot move a single stage still inspects nothing, before or after
        /// initialisation - the split <c>PartOWorkflowRefreshTests</c> established, restated here as a count
        /// rather than as object identity.
        /// </summary>
        [WpfFact]
        public void AWorkflowOnlyInput_StillInspectsNothing()
        {
            AnalyticalModel analyticalModel = Model(12);

            PartOWorkflowWindow partOWorkflowWindow = Constructed(analyticalModel, out List<Guid> _);

            partOWorkflowWindow.CompleteInitialisation();

            int count = partOWorkflowWindow.InspectionCount;

            partOWorkflowWindow.textBox_OutputDirectory.Text = @"C:\TasOut\other";
            partOWorkflowWindow.SearchText = "Flat 007";
            partOWorkflowWindow.SearchText = string.Empty;

            Assert.Equal(count, partOWorkflowWindow.InspectionCount);
        }

        // ---- Fixture --------------------------------------------------------------------------------------

        /// <summary>
        /// The dialog set up <b>exactly the way <c>Modify.RunPartOWorkflow</c> sets it up</b>: the four
        /// properties in the same order, then a restore of a saved scenario, scope and dwelling set.
        /// Anything less faithful would be counting a path production does not take.
        /// </summary>
        private static PartOWorkflowWindow Constructed(AnalyticalModel analyticalModel, out List<Guid> guids_Restored)
        {
            PartOWorkflowWindow result = new()
            {
                //Order matters: the model builds the dwelling list the restored selection is applied to.
                AnalyticalModel = analyticalModel,
                PartORun = new PartORun(),
                VentilationUnitCatalogue = null,
                Capabilities = new PartOWorkflowCapabilities(),
            };

            //A narrowed saved scope: the first seven dwellings, or all of them on a smaller model.
            guids_Restored = [];

            for (int i = 0; i < System.Math.Min(7, result.DwellingSelection.Count); i++)
            {
                guids_Restored.Add(result.DwellingSelection.Items[i].Guid);
            }

            //The LAST scenario, not the first. The combo already sits on the first, so restoring that one
            //writes nothing and raises no SelectionChanged - and a fixture that quietly skipped one of the
            //nine triggers would be measuring an easier case than the one production takes. A person who ran
            //Iteration 2 and reopened the hub is restoring a scenario that really moves.
            List<PartOWorkflowScenario> scenarios = PartOWorkflowScenario.Scenarios;

            result.Restore(scenarios[scenarios.Count - 1], PartOWorkflowScope.SelectedDwellings, guids_Restored);

            return result;
        }

        private static PartOWorkflowStageState Stage(PartOWorkflowWindow partOWorkflowWindow, PartOWorkflowStage partOWorkflowStage)
        {
            foreach (PartOWorkflowStageState partOWorkflowStageState in partOWorkflowWindow.Inspection.Stages)
            {
                if (partOWorkflowStageState.Stage == partOWorkflowStage)
                {
                    return partOWorkflowStageState;
                }
            }

            Assert.Fail(string.Format("The inspection reported no {0} stage at all.", partOWorkflowStage));

            return null;
        }

        /// <summary>
        /// A block of <paramref name="count"/> dwellings, one TM59-classifiable bedroom each with the
        /// Approved Document F continuous requirement a mechanical route is realized from. Following
        /// <c>PartOWorkflowRefreshTests.Model</c>, so what is counted here is comparable with what is counted
        /// there.
        /// </summary>
        private static AnalyticalModel Model(int count)
        {
            AdjacencyCluster adjacencyCluster = new();

            for (int i = 1; i <= count; i++)
            {
                Zone zone = new(string.Format("Flat {0:000}", i));
                zone.SetValue(ZoneParameter.IsDwelling, true);

                adjacencyCluster.AddObject(zone);

                Space space = new(string.Format("Bedroom {0:000}", i), null)
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
    }
}
