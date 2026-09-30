// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Tas;
using SAM.Analytical.UI;
using SAM.Core;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>PR-4: the design model is protected from Part O run output.</b>
    ///
    /// <para><b>The invariant (the approved model-state architecture)</b></para>
    /// <para>
    /// Part O changes the design model only through an explicit input the engineer confirmed. Preparation,
    /// simulation, results and optimisation never change it. Before PR-4 the window adopted every prepared and
    /// simulated model, so a plain Save wrote run output over the design <c>.sam</c>, and 1b and 2 derived from the
    /// previous case's result.
    /// </para>
    /// <para>
    /// These tests drive the production commands - <c>ConcludePartOReview</c>, <c>SimulatePartO</c> and the
    /// Iteration 2B command - over a real SAM preparation, with TAS replaced at the one workflow seam
    /// (<see cref="PartOWorkflowRunner"/>). The design model is captured after each explicit input and required
    /// to stay identical through everything else.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartODesignModelProtectionTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartODesignModel_" + Guid.NewGuid().ToString("N"));

        private readonly string directory_Root;

        private readonly string path_Design;

        //SimulatePartO remembers the Part O case in the process-wide setting, exactly as the dialog did. Put back
        //afterwards so no other test reads this one's folder.
        private readonly bool simulateOptions_Had;

        private readonly SimulateOptions? simulateOptions_Before;

        public PartODesignModelProtectionTests()
        {
            directory_Root = Path.Combine(directory, "PartO");
            path_Design = Path.Combine(directory, "model", "Block.sam");

            Directory.CreateDirectory(directory_Root);
            Directory.CreateDirectory(Path.GetDirectoryName(path_Design)!);

            simulateOptions_Had = ActiveSetting.Setting.TryGetValue(AnalyticalSettingParameter.SimulateOptions_PartO, out SimulateOptions simulateOptions);
            simulateOptions_Before = simulateOptions;
        }

        public void Dispose()
        {
            if (simulateOptions_Had)
            {
                ActiveSetting.Setting.SetValue(AnalyticalSettingParameter.SimulateOptions_PartO, simulateOptions_Before);
            }
            else
            {
                ActiveSetting.Setting.RemoveValue(AnalyticalSettingParameter.SimulateOptions_PartO);
            }

            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                //Best effort: a temp folder left behind is not a test result.
            }
        }

        // ---- A, B, C, E: the whole journey ---------------------------------------------------------------------

        /// <summary>
        /// 1a → 1b → 2 → 2B → Save. The design model is captured at the start and after the one explicit input
        /// (Iteration 2's equipment choices). Everything else leaves it identical, and the saved file is exactly
        /// that design.
        /// </summary>
        [WpfFact]
        public void The_design_model_is_unchanged_by_1a_1b_2_2B_and_a_normal_Save()
        {
            Journey journey = Open();

            string snapshot_Design = Snapshot(journey.UIAnalyticalModel);
            Assert.Null(UI.Query.PartODesignModelRefusal(journey.UIAnalyticalModel.JSAMObject));

            // ---- 1a --------------------------------------------------------------------------------------------
            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration1a));
            AssertDesign(journey, snapshot_Design, "1a preparation");

            List<Guid> guids_System_1a = journey.PartORun.Guids_VentilationSystem_Prepared;
            Assert.NotEmpty(guids_System_1a);

            string path_TSD_1a = Simulate(journey);
            AssertDesign(journey, snapshot_Design, "1a simulation");
            Assert.Contains(Path.Combine("Iteration1a", "tas"), path_TSD_1a, StringComparison.OrdinalIgnoreCase);

            //E: the result is the run's, reviewable, and not the open model.
            AssertReviewable(journey, path_TSD_1a);

            // ---- 1b: from the design, never from 1a's result (B) -----------------------------------------------
            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration1b));
            AssertDesign(journey, snapshot_Design, "1b preparation");
            AssertNoRunState(journey.PartORun.AnalyticalModel_Prepared, guids_System_1a, "1b prepared model");

            string path_TSD_1b = Simulate(journey);
            AssertDesign(journey, snapshot_Design, "1b simulation");
            Assert.Contains(Path.Combine("Iteration1b", "tas"), path_TSD_1b, StringComparison.OrdinalIgnoreCase);

            //The 1b .prepared.sam written beside its results carries no earlier case either.
            AssertNoRunState(Read(PartORunResume.Path_PreparedModel(path_TSD_1b)), guids_System_1a, "1b .prepared.sam");

            // ---- 2: the one explicit input, then the design again (C) ------------------------------------------
            PartOProjectTestVentilationUnit partOProjectTestVentilationUnit = new("PR-4 test unit", 60, 60);
            PartOEquipmentSelection partOEquipmentSelection = new(PartOEquipmentSelectionMode.AutomaticSelectedPool, [partOProjectTestVentilationUnit.VentilationUnitReference]);

            int modified_Before2 = journey.Modified;

            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration2, partOEquipmentSelection, partOProjectTestVentilationUnit));

            //Written once, deliberately, and the run survived its own input write.
            Assert.Equal(modified_Before2 + 1, journey.Modified);
            Assert.Equal(PartORunState.Prepared, journey.PartORun.State);

            AnalyticalModel analyticalModel_Design2 = journey.UIAnalyticalModel.JSAMObject;
            Assert.True(Same(partOEquipmentSelection, analyticalModel_Design2.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection)));
            Assert.True(Same(partOProjectTestVentilationUnit, analyticalModel_Design2.GetValue<PartOProjectTestVentilationUnit>(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit)));

            //...and those two inputs are the ONLY difference from the design as it was: the design as captured, with
            //exactly those two values set, is the design now.
            AnalyticalModel analyticalModel_Expected = Model(snapshot_Design);
            analyticalModel_Expected.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, partOEquipmentSelection);
            analyticalModel_Expected.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, partOProjectTestVentilationUnit);
            Assert.Equal(Snapshot(analyticalModel_Expected), Snapshot(analyticalModel_Design2));

            //The new baseline, captured after the explicit input commit.
            string snapshot_Design2 = Snapshot(journey.UIAnalyticalModel);
            AssertNoPartOOutput(analyticalModel_Design2, "design after the Iteration 2 input");

            //B: 2 is prepared from the design too - no 1a or 1b state, and its own new systems.
            AssertNoRunState(journey.PartORun.AnalyticalModel_Prepared, guids_System_1a, "2 prepared model");
            Assert.NotEmpty(journey.PartORun.Guids_VentilationSystem_Prepared);

            string path_TSD_2 = Simulate(journey);
            AssertDesign(journey, snapshot_Design2, "2 simulation");
            Assert.Contains(Path.Combine("Iteration2", "tas"), path_TSD_2, StringComparison.OrdinalIgnoreCase);
            AssertReviewable(journey, path_TSD_2);

            // ---- 2B: its parent is the Iteration 2 result; the design is not touched -------------------------
            int modified_Before2B = journey.Modified;
            AnalyticalModel analyticalModel_Parent = journey.PartORun.AnalyticalModel_Assessment;

            PartOOptimisationRun? partOOptimisationRun = Modify.RunPartOOptimisationResult(
                journey.UIAnalyticalModel,
                journey.PartORun,
                null,
                null,
                out PartOOptimisationSettings? partOOptimisationSettings_Confirmed,
                x => x.Settings,
                (partORun, partOOptimisationSettings) => OneRound(partORun, partOOptimisationSettings, analyticalModel_Parent),
                (_, _, _) => { });

            Assert.NotNull(partOOptimisationSettings_Confirmed);
            Assert.NotNull(partOOptimisationRun);
            Assert.Equal(modified_Before2B, journey.Modified);
            AssertDesign(journey, snapshot_Design2, "2B");

            //The accepted 2B design stays with the run and under Iteration2B.
            Assert.Equal(PartORunState.WorkflowCompleted, journey.PartORun.State);
            Assert.Same(partOOptimisationRun!.AnalyticalModel_LastValid, journey.PartORun.AnalyticalModel_Assessment);
            Assert.Contains(Path.Combine("Iteration2B", "tas"), journey.PartORun.Path_TSD, StringComparison.OrdinalIgnoreCase);
            Assert.NotSame(journey.PartORun.AnalyticalModel_Assessment, journey.UIAnalyticalModel.JSAMObject);

            // ---- normal Save --------------------------------------------------------------------------------
            //Save writes the design file, and what it writes is exactly the captured design. The comparison is of the
            //model the file holds: the .sam container itself is not byte-stable between two writes of one model.
            Assert.True(journey.UIAnalyticalModel.Save());
            Assert.Equal(path_Design, journey.UIAnalyticalModel.Path);

            AnalyticalModel analyticalModel_Saved = Read(path_Design);
            Assert.Equal(snapshot_Design2, Snapshot(analyticalModel_Saved));

            //Reopened, the saved file is a clean design model that carries the inputs and nothing a run made.
            AssertNoPartOOutput(analyticalModel_Saved, "saved design");
            Assert.Empty(Analytical.Query.PartOBaselineFindings(analyticalModel_Saved));
            Assert.Null(UI.Query.PartODesignModelRefusal(analyticalModel_Saved));
            Assert.True(Same(partOEquipmentSelection, analyticalModel_Saved.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection)));

            //A later request that states nothing inherits the saved project's choices - what the inputs are for.
            PartOWorkflowRequest partOWorkflowRequest = new(Scenario(PartOWorkflowScenario.Text_Iteration2).Option, PartOWorkflowScope.AllDwellings, PartOMixedDesignFixture.Dwellings(analyticalModel_Saved), true);
            Assert.True(Same(partOEquipmentSelection, Query.PartOEquipmentSelection(partOWorkflowRequest, analyticalModel_Saved)));
            Assert.True(Same(partOProjectTestVentilationUnit, Query.PartOProjectTestVentilationUnit(partOWorkflowRequest, analyticalModel_Saved)));
        }

        // ---- C: input persistence, focused -----------------------------------------------------------------------

        /// <summary>
        /// The inputs are written only where they changed. Confirming the project's existing choices fires no model
        /// replacement, and switching the test product off removes it from the design.
        /// </summary>
        [Fact]
        public void Inputs_are_written_to_the_design_model_only_where_they_changed()
        {
            PartOProjectTestVentilationUnit partOProjectTestVentilationUnit = new("Unit", 60, 60);
            PartOEquipmentSelection partOEquipmentSelection = new(PartOEquipmentSelectionMode.AutomaticSelectedPool, [partOProjectTestVentilationUnit.VentilationUnitReference]);

            AnalyticalModel analyticalModel = PartOMixedDesignFixture.Baseline(2);
            analyticalModel.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, partOEquipmentSelection);
            analyticalModel.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, partOProjectTestVentilationUnit);

            UIAnalyticalModel uIAnalyticalModel = new(analyticalModel);
            int modified = 0;
            uIAnalyticalModel.Modified += (s, e) => modified++;

            string snapshot = Snapshot(uIAnalyticalModel);

            //The same choices, as fresh objects: nothing to write.
            Assert.False(Modify.PersistPartOInputs(uIAnalyticalModel, null!, true, new PartOEquipmentSelection(partOEquipmentSelection), new PartOProjectTestVentilationUnit(partOProjectTestVentilationUnit)));
            Assert.Equal(0, modified);
            Assert.Equal(snapshot, Snapshot(uIAnalyticalModel));

            //An Iteration 1a/1b review states no equipment at all: nothing to write either.
            Assert.False(Modify.PersistPartOInputs(uIAnalyticalModel, null!, false, null, null));
            Assert.Equal(0, modified);

            //The test product switched off, on a review that selects products: removed, and nothing else moves.
            Assert.True(Modify.PersistPartOInputs(uIAnalyticalModel, null!, true, partOEquipmentSelection, null));
            Assert.Equal(1, modified);
            Assert.False(uIAnalyticalModel.JSAMObject.HasValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit));

            AnalyticalModel analyticalModel_Expected = new(analyticalModel);
            analyticalModel_Expected.RemoveValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit);
            Assert.Equal(Snapshot(analyticalModel_Expected), Snapshot(uIAnalyticalModel));

            //A changed mode: written.
            Assert.True(Modify.PersistPartOInputs(uIAnalyticalModel, null!, true, new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling), null));
            Assert.Equal(2, modified);
            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, uIAnalyticalModel.JSAMObject.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection).Mode);
        }

        /// <summary>A declined review writes nothing to the design model and prepares nothing.</summary>
        [Fact]
        public void A_declined_review_changes_nothing()
        {
            Journey journey = Open();
            string snapshot = Snapshot(journey.UIAnalyticalModel);

            PartOProjectTestVentilationUnit partOProjectTestVentilationUnit = new("Unit", 60, 60);
            PartOEquipmentSelection partOEquipmentSelection = new(PartOEquipmentSelectionMode.AutomaticSelectedPool, [partOProjectTestVentilationUnit.VentilationUnitReference]);

            Assert.Equal(PartOPreparationResult.Declined, Accept(journey, PartOWorkflowScenario.Text_Iteration2, partOEquipmentSelection, partOProjectTestVentilationUnit, accepted: false));

            Assert.Equal(0, journey.Modified);
            Assert.Equal(snapshot, Snapshot(journey.UIAnalyticalModel));
            Assert.NotEqual(PartORunState.Prepared, journey.PartORun.State);
        }

        // ---- Hand-picked per-dwelling products: Part O design input (owner decision on PR-4) --------------------

        /// <summary>
        /// Manual mode: the products chosen by hand for dwelling 1 (A) and dwelling 2 (B) are committed as design
        /// input, keyed by dwelling zone. Every later case is prepared from the design model - 1a and 1b select no
        /// product, as ever, and a later Iteration 2 materialises A and B onto its OWN new units from that input,
        /// with no earlier run's provenance, scenarios, results or systems anywhere in its source. Save and reopen
        /// keep them. Once committed, nothing but another explicit input changes the design model.
        /// </summary>
        [Fact]
        public void Hand_picked_products_are_design_input_and_every_case_rebuilds_them_from_the_design()
        {
            Journey journey = Open();
            (Zone zone_1, Zone zone_2) = Flats(journey.UIAnalyticalModel.JSAMObject);

            PartOEquipmentSelection partOEquipmentSelection_Manual = new(PartOEquipmentSelectionMode.ManualPerDwelling);

            // ---- 1. Choose A for dwelling 1 and B for dwelling 2, and commit --------------------------------------
            int modified_Before = journey.Modified;

            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration2, partOEquipmentSelection_Manual, catalogue: Catalogue(), edit: (x, preparation) =>
            {
                Assign(x, preparation, zone_1, product_A);
                Assign(x, preparation, zone_2, product_B);
            }));

            Assert.Equal(modified_Before + 1, journey.Modified);

            AnalyticalModel analyticalModel_Design = journey.UIAnalyticalModel.JSAMObject;
            PartOManualEquipmentSelection? partOManualEquipmentSelection = analyticalModel_Design.GetValue<PartOManualEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOManualEquipmentSelection);
            Assert.NotNull(partOManualEquipmentSelection);
            Assert.Equal(2, partOManualEquipmentSelection!.Count);
            Assert.Equal(0, VentilationUnitReference.Compare(product_A, partOManualEquipmentSelection.Product(zone_1.Guid)));
            Assert.Equal(0, VentilationUnitReference.Compare(product_B, partOManualEquipmentSelection.Product(zone_2.Guid)));
            AssertNoPartOOutput(analyticalModel_Design, "design after the manual commit");

            //The run's units carry the choice this review made.
            AssertProducts(journey.PartORun, zone_1, product_A, zone_2, product_B, "first Iteration 2");

            // ---- 5. From here on, calculations do not move the design ----------------------------------------------
            string snapshot_Design = Snapshot(journey.UIAnalyticalModel);

            Simulate(journey);
            AssertDesign(journey, snapshot_Design, "Iteration 2 simulation");

            List<Guid> guids_Unit_Earlier = [.. journey.PartORun.AnalyticalModel_Assessment.AdjacencyCluster.GetObjects<AirHandlingUnit>().Select(x => x.Guid)];
            List<Guid> guids_System_Earlier = journey.PartORun.Guids_VentilationSystem_Prepared;

            // ---- 3. 1a and 1b: prepared from the design, no product, the input untouched ------------------------
            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration1a));
            Assert.All(journey.PartORun.AnalyticalModel_Prepared.AdjacencyCluster.GetObjects<AirHandlingUnit>(), x => Assert.Null(Analytical.Query.SelectedVentilationUnitReference(x)));
            AssertDesign(journey, snapshot_Design, "1a preparation");
            Simulate(journey);
            AssertDesign(journey, snapshot_Design, "1a simulation");

            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration1b));
            AssertDesign(journey, snapshot_Design, "1b preparation");
            Simulate(journey);
            AssertDesign(journey, snapshot_Design, "1b simulation");

            // ---- 3. Iteration 2 again, with no edit: A and B from the design input alone --------------------------
            int modified_Before2 = journey.Modified;

            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration2, catalogue: Catalogue()));

            AssertProducts(journey.PartORun, zone_1, product_A, zone_2, product_B, "Iteration 2 after 1a and 1b");
            AssertNoRunState(journey.PartORun.AnalyticalModel_Prepared, guids_System_Earlier, "Iteration 2 after 1a and 1b");
            Assert.DoesNotContain(journey.PartORun.AnalyticalModel_Prepared.AdjacencyCluster.GetObjects<AirHandlingUnit>(), x => guids_Unit_Earlier.Contains(x.Guid));

            //Confirming the same choices writes nothing.
            Assert.Equal(modified_Before2, journey.Modified);
            AssertDesign(journey, snapshot_Design, "Iteration 2 preparation from the input");

            // ---- 4. Save, reopen, prepare again -------------------------------------------------------------------
            Assert.True(journey.UIAnalyticalModel.Save());

            Journey journey_Reopened = Reopen();
            Assert.Equal(snapshot_Design, Snapshot(journey_Reopened.UIAnalyticalModel));

            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey_Reopened, PartOWorkflowScenario.Text_Iteration2, catalogue: Catalogue()));
            AssertProducts(journey_Reopened.PartORun, zone_1, product_A, zone_2, product_B, "Iteration 2 after reopening");
            Assert.Equal(snapshot_Design, Snapshot(journey_Reopened.UIAnalyticalModel));
        }

        /// <summary>
        /// Changing, clearing and leaving Manual. Changing one dwelling's product replaces only that dwelling's
        /// input. An accepted automatic review clears every hand-picked product - the rule's answers are never
        /// stored as authored intent - and returning to Manual starts from nothing dormant.
        /// </summary>
        [Fact]
        public void Changing_a_choice_or_the_mode_keeps_only_what_the_engineer_last_confirmed()
        {
            Journey journey = Open();
            (Zone zone_1, Zone zone_2) = Flats(journey.UIAnalyticalModel.JSAMObject);

            PartOEquipmentSelection partOEquipmentSelection_Manual = new(PartOEquipmentSelectionMode.ManualPerDwelling);

            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration2, partOEquipmentSelection_Manual, catalogue: Catalogue(), edit: (x, preparation) =>
            {
                Assign(x, preparation, zone_1, product_A);
                Assign(x, preparation, zone_2, product_B);
            }));

            //Change dwelling 1 only: dwelling 2 keeps its product.
            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration2, catalogue: Catalogue(), edit: (x, preparation) => Assign(x, preparation, zone_1, product_B)));

            PartOManualEquipmentSelection? partOManualEquipmentSelection = Manual(journey);
            Assert.Equal(0, VentilationUnitReference.Compare(product_B, partOManualEquipmentSelection!.Product(zone_1.Guid)));
            Assert.Equal(0, VentilationUnitReference.Compare(product_B, partOManualEquipmentSelection.Product(zone_2.Guid)));

            //Automatic: the rule chooses, the hand-picked products are not applied, and the input is cleared - not
            //replaced by the rule's answers.
            PartOEquipmentSelection partOEquipmentSelection_Automatic = new(PartOEquipmentSelectionMode.AutomaticSelectedPool, [product_A]);

            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration2, partOEquipmentSelection_Automatic, catalogue: Catalogue()));

            AssertProducts(journey.PartORun, zone_1, product_A, zone_2, product_A, "automatic Iteration 2");
            Assert.Null(Manual(journey));
            Assert.Equal(PartOEquipmentSelectionMode.AutomaticSelectedPool, journey.UIAnalyticalModel.JSAMObject.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection).Mode);

            //Back to Manual: nothing was kept dormant, so no product is applied until one is chosen, and an unedited
            //manual review stores nothing.
            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration2, partOEquipmentSelection_Manual, catalogue: Catalogue()));

            Assert.All(journey.PartORun.AnalyticalModel_Prepared.AdjacencyCluster.GetObjects<AirHandlingUnit>(), x => Assert.Null(Analytical.Query.SelectedVentilationUnitReference(x)));
            Assert.Null(Manual(journey));
        }

        /// <summary>
        /// The persistence rule itself, per row: an assigned row sets its dwelling's product, an unassigned row
        /// clears it, and a dwelling outside the review's scope keeps its choice. The Review window has no control
        /// that blanks a row today, so this is pinned at the seam every review commits through.
        /// </summary>
        [Fact]
        public void An_unassigned_row_clears_its_dwelling_and_other_dwellings_keep_theirs()
        {
            AnalyticalModel analyticalModel = Design();
            (Zone zone_1, Zone zone_2) = Flats(analyticalModel);
            Guid guid_Zone_Outside = Guid.NewGuid();

            PartOManualEquipmentSelection partOManualEquipmentSelection_Existing = new();
            partOManualEquipmentSelection_Existing.Set(zone_1.Guid, product_A);
            partOManualEquipmentSelection_Existing.Set(zone_2.Guid, product_B);
            partOManualEquipmentSelection_Existing.Set(guid_Zone_Outside, product_A);

            //Dwelling 2's unit prepared with no product - an unassigned row.
            PartOManualEquipmentSelection partOManualEquipmentSelection_Prepared = new();
            partOManualEquipmentSelection_Prepared.Set(zone_1.Guid, product_A);

            List<Zone> zones = PartOMixedDesignFixture.Dwellings(analyticalModel);
            PartOIterationPreparation partOIterationPreparation = Analytical.Modify.PreparePartOIteration(analyticalModel, PartOIteration.BasePassive, zones, zones.ToDictionary(x => x.Guid, x => "MVHR"), null, false, partOManualEquipmentSelection_Prepared);
            Assert.True(partOIterationPreparation.Refusal is null, partOIterationPreparation.Refusal);

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = PartOEquipmentAssignmentSet.Create(partOIterationPreparation.AnalyticalModel.AdjacencyCluster, partOIterationPreparation.AirHandlingUnits, [], [], Catalogue(), new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            PartOManualEquipmentSelection? partOManualEquipmentSelection = Modify.ManualEquipmentSelection(partOEquipmentAssignmentSet, partOIterationPreparation, partOManualEquipmentSelection_Existing);

            Assert.NotNull(partOManualEquipmentSelection);
            Assert.Equal(0, VentilationUnitReference.Compare(product_A, partOManualEquipmentSelection!.Product(zone_1.Guid)));
            Assert.Null(partOManualEquipmentSelection.Product(zone_2.Guid));
            Assert.Equal(0, VentilationUnitReference.Compare(product_A, partOManualEquipmentSelection.Product(guid_Zone_Outside)));

            //The existing input handed in is not changed by computing the new one.
            Assert.Equal(0, VentilationUnitReference.Compare(product_B, partOManualEquipmentSelection_Existing.Product(zone_2.Guid)));

            //Under an automatic mode the table states no hand-picked product at all.
            partOEquipmentAssignmentSet.SetMode(PartOEquipmentSelectionMode.AutomaticAllProducts);
            Assert.Null(Modify.ManualEquipmentSelection(partOEquipmentAssignmentSet, partOIterationPreparation, partOManualEquipmentSelection_Existing));
        }

        /// <summary>
        /// Which preparations read the input: only a product-selecting review under manual authority. 1a/1b and
        /// automatic reviews never do, and a legacy model without the input reads as none.
        /// </summary>
        [Fact]
        public void Only_a_manual_product_selecting_review_reads_the_input()
        {
            AnalyticalModel analyticalModel = Design();
            (Zone zone_1, Zone _) = Flats(analyticalModel);

            PartOManualEquipmentSelection partOManualEquipmentSelection = new();
            partOManualEquipmentSelection.Set(zone_1.Guid, product_A);

            AnalyticalModel analyticalModel_WithInput = new(analyticalModel);
            analyticalModel_WithInput.SetValue(Analytical.AnalyticalModelParameter.PartOManualEquipmentSelection, partOManualEquipmentSelection);

            PartOEquipmentSelection partOEquipmentSelection_Manual = new(PartOEquipmentSelectionMode.ManualPerDwelling);

            Assert.NotNull(Modify.ManualEquipmentSelection(Request(analyticalModel_WithInput, PartOWorkflowScenario.Text_Iteration2), partOEquipmentSelection_Manual, analyticalModel_WithInput));
            Assert.Null(Modify.ManualEquipmentSelection(Request(analyticalModel_WithInput, PartOWorkflowScenario.Text_Iteration1a), partOEquipmentSelection_Manual, analyticalModel_WithInput));
            Assert.Null(Modify.ManualEquipmentSelection(Request(analyticalModel_WithInput, PartOWorkflowScenario.Text_Iteration2), new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts), analyticalModel_WithInput));
            Assert.Null(Modify.ManualEquipmentSelection(Request(analyticalModel_WithInput, PartOWorkflowScenario.Text_Iteration2), new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [product_A]), analyticalModel_WithInput));

            //Legacy: no input on the model is no hand-picked product.
            Assert.Null(Modify.ManualEquipmentSelection(Request(analyticalModel, PartOWorkflowScenario.Text_Iteration2), partOEquipmentSelection_Manual, analyticalModel));
        }

        // ---- D: an opened Part O result -------------------------------------------------------------------------

        /// <summary>
        /// A Part O result opened with File &gt; Open - the case's saved run model, or its <c>.prepared.sam</c> - is
        /// refused as a starting point, in the Hub and in Mixed Design, without guessing a design model. Its review
        /// still works.
        /// </summary>
        [Fact]
        public void An_opened_Part_O_result_is_refused_as_a_baseline_and_still_reviewable()
        {
            Journey journey = Open();
            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration1a));
            string path_TSD = Simulate(journey);

            foreach (string path_Result in new[] { Query.Path_PartORunModel(path_TSD), PartORunResume.Path_PreparedModel(path_TSD) })
            {
                AnalyticalModel analyticalModel_Result = Read(path_Result);

                string? refusal = UI.Query.PartODesignModelRefusal(analyticalModel_Result);
                Assert.NotNull(refusal);
                Assert.StartsWith(UI.Query.PartODesignModelRefusal_Lead, refusal);
                Assert.Contains("Remove Results", refusal);

                //What the window does on File > Open.
                PartORun partORun = new();
                bool restored = partORun.Restore(analyticalModel_Result, path_Result, out string _);

                //The Hub: Run is blocked with that reason, first; Review Results is whatever the restored run allows.
                PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(partORun, analyticalModel_Result, out _);
                Assert.Equal(refusal, partOWorkflowCapabilities.DesignModelRefusal);

                PartOWorkflowInspection partOWorkflowInspection = PartOWorkflowInspection.Inspect(analyticalModel_Result, Request(analyticalModel_Result, PartOWorkflowScenario.Text_Iteration1a), partORun, partOWorkflowCapabilities);
                Assert.False(partOWorkflowInspection.CanRun);
                Assert.Equal(refusal, partOWorkflowInspection.Blockers[0]);
                Assert.Equal(restored, partOWorkflowInspection.CanReviewResults);

                //Mixed Design already refuses it; it now says first that it is a result.
                PartOMixedDesignSession partOMixedDesignSession = new(analyticalModel_Result, path_Result, [], null);
                Assert.True(partOMixedDesignSession.IsPartOResult);
                Assert.StartsWith(UI.Query.PartODesignModelRefusal_Lead, partOMixedDesignSession.Readiness().Blockers[0]);
            }

            //The run model itself restores for review - the Results/TM59 path does not need it to be the design.
            AnalyticalModel analyticalModel_RunModel = Read(Query.Path_PartORunModel(path_TSD));
            PartORun partORun_Reopened = new();
            Assert.True(partORun_Reopened.Restore(analyticalModel_RunModel, Query.Path_PartORunModel(path_TSD), out string refusal_Restore), refusal_Restore);
            Assert.True(partORun_Reopened.IsAssessable(out string refusal_Assess), refusal_Assess);
            Assert.True(Modify.Capabilities(partORun_Reopened, analyticalModel_RunModel, out _).ResultsAvailable);

            //Nothing in the refusal dropped the review.
            Assert.Equal(PartORunState.WorkflowCompleted, partORun_Reopened.State);
        }

        /// <summary>
        /// The Hub window itself, set up as <c>RunPartOWorkflow</c> sets it up: over an opened Part O result it
        /// blocks Run and says why, first, and still offers Review Results; over the design model the refusal is
        /// absent. The window rebuilds its own capabilities for every inspection, which is where the native smoke
        /// found the refusal dropped.
        /// </summary>
        [WpfFact]
        public void The_Hub_window_blocks_Run_on_an_opened_result_and_still_offers_its_review()
        {
            Journey journey = Open();
            Assert.Equal(PartOPreparationResult.Adopted, Accept(journey, PartOWorkflowScenario.Text_Iteration1a));
            string path_TSD = Simulate(journey);

            string path_Result = Query.Path_PartORunModel(path_TSD);
            AnalyticalModel analyticalModel_Result = Read(path_Result);

            PartORun partORun = new();
            Assert.True(partORun.Restore(analyticalModel_Result, path_Result, out string refusal_Restore), refusal_Restore);

            PartOWorkflowWindow partOWorkflowWindow_Result = Hub(analyticalModel_Result, partORun);
            Assert.False(partOWorkflowWindow_Result.CanRun);
            Assert.Contains(UI.Query.PartODesignModelRefusal_Lead, partOWorkflowWindow_Result.BlockerDescription, StringComparison.Ordinal);
            Assert.True(partOWorkflowWindow_Result.CanReviewResults);
            partOWorkflowWindow_Result.Close();

            //The design model, with the run this session completed: no such refusal, and Review Results offered.
            PartOWorkflowWindow partOWorkflowWindow_Design = Hub(journey.UIAnalyticalModel.JSAMObject, journey.PartORun);
            Assert.DoesNotContain(UI.Query.PartODesignModelRefusal_Lead, partOWorkflowWindow_Design.BlockerDescription, StringComparison.Ordinal);
            Assert.True(partOWorkflowWindow_Design.CanReviewResults);
            partOWorkflowWindow_Design.Close();
        }

        /// <summary>
        /// Only Part O signals refuse. A design model that went through an ordinary energy simulation - result
        /// objects and TAS design days in its cluster - is still a design model for 1a/1b/2, although Mixed Design's
        /// stricter baseline check refuses it.
        /// </summary>
        [Fact]
        public void An_ordinary_simulation_result_is_not_a_Part_O_result()
        {
            AnalyticalModel analyticalModel = PartOMixedDesignFixture.Baseline(2);
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            foreach (Space space in adjacencyCluster.GetSpaces())
            {
                SpaceSimulationResult spaceSimulationResult = new(space.Name, "Tas", space.Guid.ToString());
                adjacencyCluster.AddObject(spaceSimulationResult);
                adjacencyCluster.AddRelation(space, spaceSimulationResult);
            }

            adjacencyCluster.AddObject(new DesignDay(new DesignDay("London ANN CLG", 2018, 7, 1), LoadType.Cooling));

            AnalyticalModel analyticalModel_Simulated = new(analyticalModel, adjacencyCluster);

            Assert.NotEmpty(Analytical.Query.PartOBaselineFindings(analyticalModel_Simulated));
            Assert.Null(UI.Query.PartODesignModelRefusal(analyticalModel_Simulated));

            PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(new PartORun(), analyticalModel_Simulated, out _);
            Assert.Null(partOWorkflowCapabilities.DesignModelRefusal);
            Assert.False(new PartOMixedDesignSession(analyticalModel_Simulated, null, [], null).IsPartOResult);
        }

        // -----------------------------------------------------------------------------------------------------------

        private sealed class Journey
        {
            public UIAnalyticalModel UIAnalyticalModel { get; init; } = null!;

            public PartORun PartORun { get; } = new();

            public int Modified { get; set; }
        }

        /// <summary>
        /// The design model, saved and opened as the window opens it, with the window's own run wiring: every
        /// replacement reaches the run as the window forwards it.
        /// </summary>
        private Journey Open()
        {
            Assert.True(Core.Convert.ToFile(new IJSAMObject[] { Design() }, path_Design));

            UIAnalyticalModel uIAnalyticalModel = new(path_Design);
            Assert.True(uIAnalyticalModel.Open());

            Journey result = new() { UIAnalyticalModel = uIAnalyticalModel };

            uIAnalyticalModel.Modified += (s, e) =>
            {
                result.Modified++;
                result.PartORun.NotifyModified(UI.Query.IsModelChange(e?.Modifications));
            };

            return result;
        }

        /// <summary>
        /// Two flats and a corridor, as the Mixed Design fixture builds them, with a layered partition so the Part O
        /// pre-simulation check lets the run through.
        /// </summary>
        private static AnalyticalModel Design()
        {
            AnalyticalModel analyticalModel = PartOMixedDesignFixture.Baseline(2);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Construction construction = new(Guid.NewGuid(), "Internal Partition", [new ConstructionLayer("Plasterboard", 0.1)]);
            foreach (Panel panel in adjacencyCluster.GetPanels())
            {
                adjacencyCluster.AddObject(Analytical.Create.Panel(panel, construction));
            }

            MaterialLibrary materialLibrary = new("Materials");
            materialLibrary.Add(new OpaqueMaterial(Guid.NewGuid(), "Plasterboard", "Plasterboard", "Fixture", 0.25, 900, 1000));

            return new AnalyticalModel(analyticalModel, adjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary);
        }

        /// <summary>The Hub over a model and a run, exactly as <c>RunPartOWorkflow</c> builds it for one showing.</summary>
        private PartOWorkflowWindow Hub(AnalyticalModel analyticalModel, PartORun partORun)
        {
            PartOWorkflowWindow result = new()
            {
                AnalyticalModel = analyticalModel,
                PartORun = partORun,
                Capabilities = Modify.Capabilities(partORun, analyticalModel, out PartOIteration3Eligibility? partOIteration3Eligibility),
                Iteration3Eligibility = partOIteration3Eligibility,
                SimulationCase = new PartOSimulationCase
                {
                    WeatherData = new WeatherData("Fixture", "Fixture", 51.5, -0.1, 25),
                    OutputDirectory = directory_Root,
                    SolarCalculationMethod = SolarCalculationMethod.TAS,
                },
            };

            result.Restore(Scenario(PartOWorkflowScenario.Text_Iteration1a), PartOWorkflowScope.AllDwellings, null);
            result.CompleteInitialisation();

            return result;
        }

        // ---- hand-picked product helpers -------------------------------------------------------------------------

        private static readonly VentilationUnitReference product_A = new("PR-4 Fixture", "Unit A", null);

        private static readonly VentilationUnitReference product_B = new("PR-4 Fixture", "Unit B", "B-01");

        /// <summary>Two fixture products that can serve either flat.</summary>
        private static List<VentilationUnitCapacityDescriptor> Catalogue()
        {
            return [new VentilationUnitCapacityDescriptor(product_A, 60, 60, 0), new VentilationUnitCapacityDescriptor(product_B, 80, 80, 0)];
        }

        private static (Zone, Zone) Flats(AnalyticalModel analyticalModel)
        {
            List<Zone> zones = PartOMixedDesignFixture.Dwellings(analyticalModel);
            Assert.Equal(2, zones.Count);

            return (zones[0], zones[1]);
        }

        /// <summary>What the engineer does in the table: assign a product to one dwelling's row, found by zone identity.</summary>
        private static void Assign(PartOEquipmentAssignmentSet partOEquipmentAssignmentSet, PartOIterationPreparation partOIterationPreparation, Zone zone, VentilationUnitReference ventilationUnitReference)
        {
            int index = partOIterationPreparation.DwellingZoneGuids.IndexOf(zone.Guid);
            Assert.True(index >= 0, "The preparation built no unit for the dwelling.");

            Assert.True(partOEquipmentAssignmentSet.Assign(partOIterationPreparation.AirHandlingUnits[index].Guid, ventilationUnitReference, out string refusal), refusal);
        }

        /// <summary>The product each dwelling's unit carries in the run's prepared model.</summary>
        private static void AssertProducts(PartORun partORun, Zone zone_1, VentilationUnitReference ventilationUnitReference_1, Zone zone_2, VentilationUnitReference ventilationUnitReference_2, string step)
        {
            AdjacencyCluster adjacencyCluster = partORun.AnalyticalModel_Prepared.AdjacencyCluster;

            Assert.True(VentilationUnitReference.Compare(ventilationUnitReference_1, Analytical.Query.SelectedVentilationUnitReference(Unit(adjacencyCluster, zone_1))) == 0, string.Format("{0}: dwelling 1", step));
            Assert.True(VentilationUnitReference.Compare(ventilationUnitReference_2, Analytical.Query.SelectedVentilationUnitReference(Unit(adjacencyCluster, zone_2))) == 0, string.Format("{0}: dwelling 2", step));
        }

        /// <summary>The Part O unit serving a dwelling zone's spaces, found through the zone - identity, not name.</summary>
        private static AirHandlingUnit Unit(AdjacencyCluster adjacencyCluster, Zone zone)
        {
            HashSet<Guid> guids_Space = [.. adjacencyCluster.GetRelatedObjects<Space>(adjacencyCluster.GetObject<Zone>(zone.Guid)).Select(x => x.Guid)];

            List<AirHandlingUnit> result = [];
            foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetObjects<VentilationSystem>() ?? [])
            {
                List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [];
                if (spaces.Count == 0 || !spaces.TrueForAll(x => guids_Space.Contains(x.Guid)))
                {
                    continue;
                }

                result.AddRange(adjacencyCluster.GetRelatedObjects<AirHandlingUnit>(ventilationSystem) ?? []);

                if (ventilationSystem.TryGetValue(VentilationSystemParameter.SupplyUnitName, out string name_Unit))
                {
                    result.AddRange((adjacencyCluster.GetObjects<AirHandlingUnit>() ?? []).FindAll(x => x.Name == name_Unit));
                }
            }

            return Assert.Single(result.GroupBy(x => x.Guid).Select(x => x.First()));
        }

        private static PartOManualEquipmentSelection? Manual(Journey journey)
        {
            return journey.UIAnalyticalModel.JSAMObject.GetValue<PartOManualEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOManualEquipmentSelection);
        }

        /// <summary>The saved design file opened again in a new session: a new window model and a new run.</summary>
        private Journey Reopen()
        {
            UIAnalyticalModel uIAnalyticalModel = new(path_Design);
            Assert.True(uIAnalyticalModel.Open());

            Journey result = new() { UIAnalyticalModel = uIAnalyticalModel };

            uIAnalyticalModel.Modified += (s, e) =>
            {
                result.Modified++;
                result.PartORun.NotifyModified(UI.Query.IsModelChange(e?.Modifications));
            };

            return result;
        }

        private static PartOWorkflowScenario Scenario(string text)
        {
            return PartOWorkflowScenario.Scenarios.Single(x => x.Text == text);
        }

        private static PartOWorkflowRequest Request(AnalyticalModel analyticalModel, string text_Scenario, PartOEquipmentSelection? partOEquipmentSelection = null, PartOProjectTestVentilationUnit? partOProjectTestVentilationUnit = null)
        {
            PartOWorkflowScenario partOWorkflowScenario = Scenario(text_Scenario);

            return new PartOWorkflowRequest(partOWorkflowScenario.Option, PartOWorkflowScope.AllDwellings, PartOMixedDesignFixture.Dwellings(analyticalModel), partOWorkflowScenario.SelectVentilationUnit)
            {
                EquipmentSelection = partOEquipmentSelection,
                ProjectTestVentilationUnit = partOProjectTestVentilationUnit,
            };
        }

        /// <summary>
        /// What <c>PrepareAndReviewPartOIteration</c> does up to the Review window - from the OPEN model, which is the
        /// design - and then the engineer's answer, through the production decision point.
        /// </summary>
        private static PartOPreparationResult Accept(Journey journey, string text_Scenario, PartOEquipmentSelection? partOEquipmentSelection = null, PartOProjectTestVentilationUnit? partOProjectTestVentilationUnit = null, bool accepted = true, Action<PartOEquipmentAssignmentSet, PartOIterationPreparation>? edit = null, List<VentilationUnitCapacityDescriptor>? catalogue = null)
        {
            AnalyticalModel analyticalModel = journey.UIAnalyticalModel.JSAMObject;

            PartOWorkflowRequest partOWorkflowRequest = Request(analyticalModel, text_Scenario, partOEquipmentSelection, partOProjectTestVentilationUnit);
            PartOIteration partOIteration = partOWorkflowRequest.Option.PartOIteration;
            Dictionary<Guid, string> dictionary_VentilationStrategy = partOWorkflowRequest.VentilationStrategies();

            PartOProjectTestVentilationUnit? partOProjectTestVentilationUnit_Resolved = Query.PartOProjectTestVentilationUnit(partOWorkflowRequest, analyticalModel);
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors_ProjectTest = Analytical.Query.CapacityDescriptors(partOProjectTestVentilationUnit_Resolved);
            PartOEquipmentSelection partOEquipmentSelection_Resolved = Query.PartOEquipmentSelection(partOWorkflowRequest, analyticalModel);

            List<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors_Candidate = partOWorkflowRequest.SelectVentilationUnit
                ? partOEquipmentSelection_Resolved.CandidateDescriptors(catalogue ?? [], ventilationUnitCapacityDescriptors_ProjectTest)
                : null;

            //The same rule production reads the design's hand-picked products by.
            PartOManualEquipmentSelection? partOManualEquipmentSelection = Modify.ManualEquipmentSelection(partOWorkflowRequest, partOEquipmentSelection_Resolved, analyticalModel);

            PartOIterationPreparation partOIterationPreparation = Analytical.Modify.PreparePartOIteration(analyticalModel, partOIteration, partOWorkflowRequest.Zones_Dwelling, dictionary_VentilationStrategy, ventilationUnitCapacityDescriptors_Candidate, false, partOManualEquipmentSelection);
            Assert.True(partOIterationPreparation.Refusal is null, partOIterationPreparation.Refusal);

            PartOPreparationContext partOPreparationContext = new(partOIteration, partOWorkflowRequest.Zones_Dwelling, dictionary_VentilationStrategy, partOWorkflowRequest.SelectVentilationUnit ? [.. catalogue ?? [], .. ventilationUnitCapacityDescriptors_ProjectTest] : null)
            {
                EquipmentSelection = partOEquipmentSelection_Resolved,
                ProjectTestVentilationUnit = partOProjectTestVentilationUnit_Resolved,
            };

            AnalyticalModel analyticalModel_Prepared = partOIterationPreparation.AnalyticalModel;
            AdjacencyCluster adjacencyCluster_Prepared = analyticalModel_Prepared.AdjacencyCluster;

            PartOEquipmentAssignmentSet? partOEquipmentAssignmentSet = partOWorkflowRequest.SelectVentilationUnit
                ? PartOEquipmentAssignmentSet.Create(adjacencyCluster_Prepared, partOIterationPreparation.AirHandlingUnits, [], [], catalogue ?? [], partOEquipmentSelection_Resolved, ventilationUnitCapacityDescriptors_ProjectTest)
                : null;

            //What the engineer does in the Review window's assignment table, if anything.
            if (partOEquipmentAssignmentSet is not null)
            {
                edit?.Invoke(partOEquipmentAssignmentSet, partOIterationPreparation);
            }

            return Modify.ConcludePartOReview(accepted, journey.UIAnalyticalModel, journey.PartORun, partOWorkflowRequest, analyticalModel_Prepared, adjacencyCluster_Prepared, partOIterationPreparation, partOPreparationContext, partOEquipmentAssignmentSet, partOProjectTestVentilationUnit_Resolved);
        }

        /// <summary>The Hub's simulation, through the production core, with TAS replaced at its workflow seam.</summary>
        private string Simulate(Journey journey)
        {
            PartOPreSimulationCheck partOPreSimulationCheck = PartOPreSimulationCheck.Gate(journey.PartORun, journey.PartORun.AnalyticalModel_Prepared);
            Assert.True(partOPreSimulationCheck is null || partOPreSimulationCheck.IsValid, partOPreSimulationCheck is null ? null : string.Join("\n", partOPreSimulationCheck.Errors.Select(x => x.Text)));

            PartOSimulationCase partOSimulationCase = new()
            {
                WeatherData = new WeatherData("Fixture", "Fixture", 51.5, -0.1, 25),
                OutputDirectory = directory_Root,
                SolarCalculationMethod = SolarCalculationMethod.TAS,
            };

            PartOSimulationOutcome partOSimulationOutcome = Modify.SimulatePartO(journey.UIAnalyticalModel, journey.PartORun, partOSimulationCase, Runner);

            Assert.True(partOSimulationOutcome.Completed, partOSimulationOutcome.Refusal ?? partOSimulationOutcome.Note_PartORun ?? partOSimulationOutcome.Message);
            Assert.Equal(PartORunState.WorkflowCompleted, journey.PartORun.State);

            return journey.PartORun.Path_TSD;
        }

        /// <summary>
        /// One Iteration 2B round as the optimiser runs it (<c>OptimisePartOTM59</c>): re-prepared from the
        /// Iteration 2 result, simulated under its own <c>-Opt01</c> name in Iteration 2B's folder, and completed
        /// into the run.
        /// </summary>
        private static PartOOptimisationRun OneRound(PartORun partORun, PartOOptimisationSettings partOOptimisationSettings, AnalyticalModel analyticalModel_Parent)
        {
            Assert.Same(analyticalModel_Parent, partORun.AnalyticalModel_Assessment);

            PartOPreparationContext partOPreparationContext = partORun.PreparationContext;
            PartOSimulationContext partOSimulationContext = partORun.SimulationContext;

            PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(PartOOutputPaths.Root(partOSimulationContext.OutputDirectory), PartOOutputCase.Iteration2B);
            Assert.Null(partOOutputPaths.TryCreateDirectories());
            partOSimulationContext = partOOutputPaths.SimulationContext(partOSimulationContext);

            AnalyticalModel analyticalModel_Round = new(analyticalModel_Parent, analyticalModel_Parent.AdjacencyCluster);

            PartOIterationPreparation partOIterationPreparation = Analytical.Modify.PreparePartOIteration(analyticalModel_Round, partOPreparationContext.PartOIteration, partOPreparationContext.Zones, partOPreparationContext.VentilationStrategies, null);
            Assert.True(partORun.Prepare(partOIterationPreparation, partOPreparationContext), partORun.InvalidationReason);

            AnalyticalModel analyticalModel_Workflow = Modify.RunPartOSimulation(partOIterationPreparation.AnalyticalModel, partOSimulationContext, partOSimulationContext.ProjectName_Iteration(1), partORun, CancellationToken.None, out _, out string path_TSD, out bool cancelled, out bool _, out _, out string refusal, null, Runner);
            Assert.True(refusal is null, refusal);
            Assert.False(cancelled);

            Assert.True(partORun.Complete(analyticalModel_Workflow, path_TSD, partOSimulationContext, out string refusal_Complete), refusal_Complete);

            return new PartOOptimisationRun(partOOptimisationSettings)
            {
                StopReason = PartOOptimisationStopReason.Passed,
                AnalyticalModel_LastValid = partORun.AnalyticalModel_Assessment,
                Path_TSD_LastValid = path_TSD,
            };
        }

        /// <summary>TAS, replaced: writes this run's own results file and hands the model back.</summary>
        private static AnalyticalModel? Runner(AnalyticalModel analyticalModel, WorkflowSettings workflowSettings, CancellationToken cancellationToken, out bool cancelled)
        {
            cancelled = false;

            File.WriteAllText(Path.ChangeExtension(workflowSettings.Path_TBD, "tsd"), string.Format("results - {0}", Guid.NewGuid()));

            return analyticalModel;
        }

        private static void AssertDesign(Journey journey, string snapshot, string step)
        {
            Assert.True(snapshot == Snapshot(journey.UIAnalyticalModel), string.Format("The design model changed at: {0}", step));
            AssertNoPartOOutput(journey.UIAnalyticalModel.JSAMObject, step);
        }

        /// <summary>E: the result is the run's, the Hub offers its review, and the design model is still a design model.</summary>
        private static void AssertReviewable(Journey journey, string path_TSD)
        {
            Assert.True(journey.PartORun.IsAssessable(out string refusal), refusal);
            Assert.Equal(path_TSD, Provenance(journey.PartORun.AnalyticalModel_Assessment)!.Path_TSD);

            PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(journey.PartORun, journey.UIAnalyticalModel.JSAMObject, out _);
            Assert.True(partOWorkflowCapabilities.ResultsAvailable, partOWorkflowCapabilities.ResultsRefusal);
            Assert.Null(partOWorkflowCapabilities.DesignModelRefusal);

            AnalyticalModel analyticalModel = journey.UIAnalyticalModel.JSAMObject;
            PartOWorkflowInspection partOWorkflowInspection = PartOWorkflowInspection.Inspect(analyticalModel, Request(analyticalModel, PartOWorkflowScenario.Text_Iteration1a), journey.PartORun, partOWorkflowCapabilities);
            Assert.True(partOWorkflowInspection.CanReviewResults);
            Assert.DoesNotContain(UI.Query.PartODesignModelRefusal_Lead, string.Join(" ", partOWorkflowInspection.Blockers));

            //The case's own run model is on disk and reopens for review.
            Assert.True(File.Exists(Query.Path_PartORunModel(path_TSD)));
        }

        /// <summary>No Part O output of any kind on a model that must be a design model.</summary>
        private static void AssertNoPartOOutput(AnalyticalModel analyticalModel, string step)
        {
            Assert.False(analyticalModel.HasValue(Analytical.AnalyticalModelParameter.OverheatingScenarios), step);
            Assert.False(analyticalModel.HasValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance), step);
            Assert.False(analyticalModel.HasValue(Analytical.AnalyticalModelParameter.PartOIsolationContext), step);
            Assert.Empty(analyticalModel.AdjacencyCluster.GetObjects<VentilationSystem>() ?? []);
            Assert.Empty(analyticalModel.AdjacencyCluster.GetObjects<AirHandlingUnit>() ?? []);
            Assert.Empty(analyticalModel.AdjacencyCluster.GetObjects<SpaceSimulationResult>() ?? []);
        }

        /// <summary>
        /// B: a prepared model carries no earlier case's run state - no provenance, no scenarios, no results, and
        /// none of the systems that case built.
        /// </summary>
        private static void AssertNoRunState(AnalyticalModel analyticalModel, IEnumerable<Guid> guids_System_Earlier, string what)
        {
            Assert.False(analyticalModel.HasValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance), what);
            Assert.False(analyticalModel.HasValue(Analytical.AnalyticalModelParameter.OverheatingScenarios), what);
            Assert.Empty(analyticalModel.AdjacencyCluster.GetObjects<SpaceSimulationResult>() ?? []);

            foreach (Guid guid in guids_System_Earlier)
            {
                Assert.Null(analyticalModel.AdjacencyCluster.GetObject<VentilationSystem>(guid));
            }

            Assert.DoesNotContain(Analytical.Query.PartOBaselineFindings(analyticalModel), x => x.Reason == PartOMaterialisationRefusalReason.RunOutputBaseline);
        }

        private static string Snapshot(UIAnalyticalModel uIAnalyticalModel)
        {
            return Snapshot(uIAnalyticalModel.JSAMObject);
        }

        private static string Snapshot(AnalyticalModel analyticalModel)
        {
            return analyticalModel.ToJsonObject().ToJsonString();
        }

        private static AnalyticalModel Model(string snapshot)
        {
            return new AnalyticalModel(System.Text.Json.Nodes.JsonNode.Parse(snapshot)!.AsObject());
        }

        private static bool Same(IJSAMObject? jSAMObject_1, IJSAMObject? jSAMObject_2)
        {
            return jSAMObject_1 is not null && jSAMObject_2 is not null && jSAMObject_1.ToJsonObject().ToJsonString() == jSAMObject_2.ToJsonObject().ToJsonString();
        }

        private static AnalyticalModel Read(string path)
        {
            List<AnalyticalModel>? analyticalModels = Core.Convert.ToSAM<AnalyticalModel>(path);
            Assert.NotNull(analyticalModels);

            return Assert.Single(analyticalModels!);
        }

        private static SimulationResultProvenance? Provenance(AnalyticalModel analyticalModel)
        {
            return analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out SimulationResultProvenance simulationResultProvenance) ? simulationResultProvenance : null;
        }
    }
}
