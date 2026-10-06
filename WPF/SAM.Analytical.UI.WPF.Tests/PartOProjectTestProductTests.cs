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
    /// <b>The optional project test ventilation unit, in the interface that states it.</b>
    ///
    /// <para><b>What it is for</b></para>
    /// <para>
    /// Trying "what would a 165 l/s unit do here" without editing shipped manufacturer data and without
    /// inventing a fictional Nuaire entry that every later report would be unable to tell from real data.
    /// One made-up unit, this project only, obviously marked.
    /// </para>
    ///
    /// <para><b>The two things that make it safe rather than convenient</b></para>
    /// <list type="number">
    /// <item>It never joins "Automatic - all catalogue products", so every historic answer is unmoved and a
    /// capacity left typed in a box cannot silently size a project.</item>
    /// <item>Its <b>name is its identity</b>, so it can never disappear from underneath the dwellings
    /// assigned to it: while anything holds it, the enable tick and the name are locked. Its capacities
    /// stay editable, because re-rating a what-if is the entire point of it and moves no identity.</item>
    /// </list>
    ///
    /// <para><b>And the pool can never hold a stale identity</b></para>
    /// <para>
    /// Because the permitted pool is <i>derived from the catalogue rows</i> rather than stored beside them,
    /// and the test product is one of those rows. Renaming it replaces the row; disabling it removes the
    /// row; either way the pool that is read out afterwards is simply the rows that are there. Sections C
    /// and D below are that property, stated as tests.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOProjectTestProductTests
    {
        private const string model_MRXBOX = "MRXBOXAB-ECO5-AECV";

        private const string model_XBC15 = "XBC15";

        private const string name_Test = "Test unit";

        // =================================================================================================
        // A. It is visible, and visibly not manufacturer data
        // =================================================================================================

        /// <summary>
        /// A stated test product appears as a catalogue row whose Origin says <b>Project test</b> and whose
        /// manufacturer field is the literal words "Project test". A made-up capacity sitting in the same
        /// grid as transcribed manufacturer data has to be unmistakable for it.
        /// </summary>
        [WpfFact]
        public void AStatedTestProduct_AppearsAsARowThatSaysItIsAProjectTest()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();

            PartOCatalogueProductRow partOCatalogueProductRow = Row(control, name_Test);

            Assert.True(partOCatalogueProductRow.IsProjectTest);
            Assert.Equal("Project test", partOCatalogueProductRow.Origin);
            Assert.Equal(PartOProjectTestVentilationUnit.Manufacturer, partOCatalogueProductRow.Manufacturer);
            Assert.Equal(165, partOCatalogueProductRow.MaximumSupply_Lps);
            Assert.Equal(170, partOCatalogueProductRow.MaximumExtract_Lps);

            //And the manufacturer rows say they are catalogue rows.
            Assert.False(Row(control, model_MRXBOX).IsProjectTest);
            Assert.Equal("Catalogue", Row(control, model_MRXBOX).Origin);
        }

        /// <summary>
        /// A project that states none has no such row, and the control reads exactly as it did before this
        /// feature existed.
        /// </summary>
        [WpfFact]
        public void AProjectThatStatesNone_HasNoSuchRow()
        {
            PartOEquipmentSelectionControl control = Control();

            Assert.Null(control.ProjectTestVentilationUnit);
            Assert.Equal(2, control.CatalogueProductRows.Count);
            Assert.All(control.CatalogueProductRows, x => Assert.False(x.IsProjectTest));

            Assert.Contains("No project test product", control.ProjectTestDescription);
        }

        /// <summary>
        /// An incomplete statement contributes no row and says in words what is missing - rather than
        /// contributing a product at a guessed capacity, or failing silently.
        /// </summary>
        [WpfTheory]
        [InlineData("", 165, 170)]
        [InlineData(name_Test, double.NaN, 170)]
        [InlineData(name_Test, 165, 0)]
        public void AnIncompleteStatement_ContributesNoRowAndSaysWhy(string name, double maximumSupply_Lps, double maximumExtract_Lps)
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = new PartOProjectTestVentilationUnit(name, maximumSupply_Lps, maximumExtract_Lps);

            Assert.Equal(2, control.CatalogueProductRows.Count);
            Assert.All(control.CatalogueProductRows, x => Assert.False(x.IsProjectTest));

            Assert.False(string.IsNullOrWhiteSpace(control.ProjectTestDescription));
        }

        // =================================================================================================
        // B. "Automatic - all catalogue products" is untouched
        // =================================================================================================

        /// <summary>
        /// Under the all-products mode the control says, in words, that the test product does not take part
        /// - because that is the one thing about it an engineer could reasonably assume the other way round.
        /// </summary>
        [WpfFact]
        public void UnderAutomaticAllProducts_TheControlSaysTheTestProductDoesNotTakePart()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts);

            Assert.Contains("does NOT take part", control.ProjectTestDescription);

            //And the domain agrees: it is not a candidate, whatever the ticks say.
            Assert.Equal(2, control.EquipmentSelection.CandidateDescriptors(Descriptors(), Analytical.Query.CapacityDescriptors(control.ProjectTestVentilationUnit)).Count);
        }

        /// <summary>
        /// And in the pooled mode the wording changes to what actually decides: whether it is ticked.
        /// </summary>
        [WpfFact]
        public void UnderTheSelectedPool_TheControlSaysItTakesPartWhereTicked()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [TestUnit().VentilationUnitReference]);

            Assert.Contains("ticked above or assigned by hand", control.ProjectTestDescription);

            //And it is the one permitted product, so it is the one candidate.
            VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = Assert.Single(
                control.EquipmentSelection.CandidateDescriptors(Descriptors(), Analytical.Query.CapacityDescriptors(control.ProjectTestVentilationUnit)));

            Assert.Equal(name_Test, ventilationUnitCapacityDescriptor.VentilationUnitReference.Model);
        }

        // =================================================================================================
        // C. Renaming an unassigned test product leaves no stale identity in the pool
        // =================================================================================================

        /// <summary>
        /// <b>Renaming a pooled test product moves its permission with it.</b> The identity is derived from
        /// the name, so a rename is a new identity - and the pool has to end up holding the new one and not
        /// the old one, in one step. The tick the engineer gave it survives, because an engineer correcting
        /// a typo must not silently drop their own product out of their own pool.
        /// </summary>
        [WpfFact]
        public void RenamingAnUnassignedTestProduct_MovesItsPermissionAndLeavesNoStaleIdentity()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [TestUnit().VentilationUnitReference]);

            Assert.True(Row(control, name_Test).IsUsed);

            //The rename.
            control.ProjectTestVentilationUnit = new PartOProjectTestVentilationUnit("Test unit B", 165, 170);

            //Still permitted, under the new name.
            Assert.True(Row(control, "Test unit B").IsUsed);

            List<string> models = Models(control.EquipmentSelection);

            Assert.Contains("Test unit B", models);

            //And the old identity is GONE - not merely unticked, absent.
            Assert.DoesNotContain(name_Test, models);
            Assert.DoesNotContain(control.CatalogueProductRows, x => x.Model == name_Test);
        }

        /// <summary>
        /// A rename does not silently permit a test product the engineer had un-ticked either: the tick is
        /// carried across in whatever state it was in.
        /// </summary>
        [WpfFact]
        public void RenamingAnUntickedTestProduct_LeavesItUnticked()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();

            //Pooled, permitting only the MRXBOX - so the test product's row is not ticked.
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [MRXBOXReference()]);

            Assert.False(Row(control, name_Test).IsUsed);

            control.ProjectTestVentilationUnit = new PartOProjectTestVentilationUnit("Test unit B", 165, 170);

            Assert.False(Row(control, "Test unit B").IsUsed);
            Assert.DoesNotContain("Test unit B", Models(control.EquipmentSelection));
        }

        // =================================================================================================
        // D. Disabling an unassigned test product removes its permission
        // =================================================================================================

        /// <summary>
        /// <b>Disabling a pooled test product removes it from the permitted pool in the same step.</b>
        /// Nothing invisible survives in the project's configuration - which is what a pool held as its own
        /// list, kept in step by hand, would eventually have left behind.
        /// </summary>
        [WpfFact]
        public void DisablingAnUnassignedTestProduct_RemovesItFromThePool()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [TestUnit().VentilationUnitReference, MRXBOXReference()]);

            Assert.Contains(name_Test, Models(control.EquipmentSelection));

            control.ProjectTestVentilationUnit = null;

            List<string> models = Models(control.EquipmentSelection);

            Assert.DoesNotContain(name_Test, models);

            //The manufacturer product the engineer permitted is untouched.
            Assert.Contains(model_MRXBOX, models);
            Assert.DoesNotContain(model_XBC15, models);
        }

        /// <summary>
        /// And what the project would be saved with holds no trace of it: the statement written back is
        /// what the control derives from its rows, so a save/reopen round trip cannot carry a stale
        /// project-test reference.
        /// </summary>
        [WpfFact]
        public void AfterDisabling_ASaveAndReopenRoundTripHoldsNoStaleTestReference()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [TestUnit().VentilationUnitReference, MRXBOXReference()]);

            control.ProjectTestVentilationUnit = null;

            //Stamped on a project and read back, exactly as Modify.PreparePartOIteration does it.
            AnalyticalModel analyticalModel = new("Block", null, null, null, new AdjacencyCluster());

            analyticalModel.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, control.EquipmentSelection);

            AnalyticalModel analyticalModel_Read = new(analyticalModel.ToJsonObject());

            PartOEquipmentSelection partOEquipmentSelection = analyticalModel_Read.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection);

            Assert.DoesNotContain(partOEquipmentSelection.AllowedVentilationUnitReferences, x => x.Manufacturer == PartOProjectTestVentilationUnit.Manufacturer);
            Assert.DoesNotContain(name_Test, Models(partOEquipmentSelection));
        }

        /// <summary>
        /// A project reopened with a pool that names a project-test identity nothing states any more ticks
        /// nothing and drops it - defence in depth behind the fact that such an identity could never have
        /// selected anything anyway, having no descriptor to contribute.
        /// </summary>
        [WpfFact]
        public void APoolNamingATestProductNothingStates_TicksNothingAndDropsIt()
        {
            PartOEquipmentSelectionControl control = Control();

            //No test product stated - and a pool that names one.
            control.EquipmentSelection = new PartOEquipmentSelection(
                PartOEquipmentSelectionMode.AutomaticSelectedPool,
                [new VentilationUnitReference(PartOProjectTestVentilationUnit.Manufacturer, "A what-if from a previous session", null), MRXBOXReference()]);

            List<string> models = Models(control.EquipmentSelection);

            Assert.DoesNotContain("A what-if from a previous session", models);
            Assert.Equal([model_MRXBOX], models);
        }

        // =================================================================================================
        // E. An assigned test product cannot disappear from underneath its dwellings
        // =================================================================================================

        /// <summary>
        /// <b>While dwellings hold it, the enable tick and the name are locked</b> - both would change the
        /// identity those dwellings carry - and the control says how many dwellings are involved rather
        /// than merely disabling something.
        /// </summary>
        [WpfFact]
        public void WhileAssigned_TheEnableTickAndTheNameAreLockedAndTheCountIsStated()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.ProjectTestVentilationUnitAssignmentCount = 3;

            Assert.False(control.IsProjectTestIdentityEditable);

            Assert.Contains("assigned to 3 dwelling(s)", control.ProjectTestDescription);
            Assert.Contains("Reassign those dwellings", control.ProjectTestDescription);
        }

        /// <summary>
        /// <b>And its capacities stay editable.</b> Trying 155, then 165, then 175 l/s against an assigned
        /// unit is the whole point of the feature: the identity never moves, no dwelling is reassigned, and
        /// the ceiling simply re-resolves.
        /// </summary>
        [WpfFact]
        public void WhileAssigned_TheCapacitiesStayEditableAndTheIdentityNeverMoves()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.ProjectTestVentilationUnitAssignmentCount = 3;

            Assert.True(control.IsProjectTestCapacityEditable);

            VentilationUnitReference ventilationUnitReference = control.ProjectTestVentilationUnit.VentilationUnitReference;

            //The re-rating.
            control.ProjectTestVentilationUnit = new PartOProjectTestVentilationUnit(name_Test, 175, 180);

            //Same identity - so the dwellings that hold it still resolve, and the pool entry still matches.
            Assert.True(ventilationUnitReference.Matches(control.ProjectTestVentilationUnit.VentilationUnitReference));

            //New capability.
            Assert.Equal(175, Row(control, name_Test).MaximumSupply_Lps);
            Assert.Equal(180, Row(control, name_Test).MaximumExtract_Lps);
        }

        /// <summary>
        /// A re-rating leaves an assigned, pooled test product still permitted - the pool holds an identity,
        /// and the identity did not move.
        /// </summary>
        [WpfFact]
        public void ARerating_LeavesAnAssignedPooledTestProductStillPermitted()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.ProjectTestVentilationUnitAssignmentCount = 1;
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [TestUnit().VentilationUnitReference]);

            control.ProjectTestVentilationUnit = new PartOProjectTestVentilationUnit(name_Test, 175, 180);

            Assert.Contains(name_Test, Models(control.EquipmentSelection));
        }

        /// <summary>
        /// With nothing assigned, the identity is editable again - so an engineer who has reassigned those
        /// dwellings can then remove the what-if.
        /// </summary>
        [WpfFact]
        public void WithNothingAssigned_TheIdentityIsEditableAgain()
        {
            PartOEquipmentSelectionControl control = Control();

            control.ProjectTestVentilationUnit = TestUnit();
            control.ProjectTestVentilationUnitAssignmentCount = 2;

            Assert.False(control.IsProjectTestIdentityEditable);

            control.ProjectTestVentilationUnitAssignmentCount = 0;

            Assert.True(control.IsProjectTestIdentityEditable);
        }

        /// <summary>
        /// The count is a fact about the SAVED project, and this is the query that reads it - by identity,
        /// never by guid, because a reference is minted fresh every time one is read.
        /// </summary>
        [Fact]
        public void TheAssignmentCount_IsReadOffTheModelByIdentity()
        {
            AnalyticalModel analyticalModel = Model(TestUnit(), TestUnit().VentilationUnitReference, 3);

            Assert.Equal(3, Query.PartOVentilationUnitAssignmentCount(analyticalModel, TestUnit().VentilationUnitReference));

            //A different product, and a product nothing holds.
            Assert.Equal(0, Query.PartOVentilationUnitAssignmentCount(analyticalModel, MRXBOXReference()));
            Assert.Equal(0, Query.PartOVentilationUnitAssignmentCount(analyticalModel, new PartOProjectTestVentilationUnit("Something else", 165, 170).VentilationUnitReference));

            //And nothing to ask about is nothing to count.
            Assert.Equal(0, Query.PartOVentilationUnitAssignmentCount(analyticalModel, null));
            Assert.Equal(0, Query.PartOVentilationUnitAssignmentCount(null, TestUnit().VentilationUnitReference));
        }

        // =================================================================================================
        // F. One statement, both workflows
        // =================================================================================================

        /// <summary>
        /// A test product stated in the project is what <b>Prepare &amp; Run</b> opens on, and what it
        /// carries in its request - the same project-scoped statement, in the same shared control.
        /// </summary>
        [WpfFact]
        public void AProjectsTestProduct_IsWhatPrepareAndRunOpensOn_AndWhatItsRequestCarries()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(TestUnit()));

            Assert.NotNull(partOWorkflowWindow.ProjectTestVentilationUnit);
            Assert.Equal(name_Test, partOWorkflowWindow.ProjectTestVentilationUnit.Name);
            Assert.Equal(165, partOWorkflowWindow.ProjectTestVentilationUnit.MaximumSupplyFlowRate_Lps);

            Assert.True(partOWorkflowWindow.Request.ProjectTestVentilationUnit?.Matches(TestUnit()));
        }

        /// <summary>And the same statement is what <b>Prepare Iteration</b> opens on.</summary>
        [WpfFact]
        public void AProjectsTestProduct_IsWhatPrepareIterationOpensOn()
        {
            PartOIterationWindow partOIterationWindow = IterationWindow(Model(TestUnit()));

            Assert.NotNull(partOIterationWindow.ProjectTestVentilationUnit);
            Assert.Equal(name_Test, partOIterationWindow.ProjectTestVentilationUnit.Name);
            Assert.Equal(170, partOIterationWindow.ProjectTestVentilationUnit.MaximumExtractFlowRate_Lps);
        }

        /// <summary>
        /// A project that states none opens with none in both workflows - the historic case, and no request
        /// invents one.
        /// </summary>
        [WpfFact]
        public void AProjectThatStatesNone_OpensWithNoneInBothWorkflows()
        {
            Assert.Null(Window(Model(null)).ProjectTestVentilationUnit);
            Assert.Null(Window(Model(null)).Request.ProjectTestVentilationUnit);
            Assert.Null(IterationWindow(Model(null)).ProjectTestVentilationUnit);
        }

        /// <summary>
        /// A test product ticked into the pool in Prepare &amp; Run is permitted there, and the pool it
        /// states names the test product's identity - so what Prepare Iteration is handed next is the same
        /// permitted set.
        /// </summary>
        [WpfFact]
        public void ATestProductPooledInPrepareAndRun_IsInThePoolItStates()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Model(TestUnit()));

            partOWorkflowWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [TestUnit().VentilationUnitReference]);

            Assert.Equal([name_Test], Models(partOWorkflowWindow.EquipmentSelection));
        }

        // =================================================================================================
        // G. Reuse - a re-rated what-if is a different what-if
        // =================================================================================================

        /// <summary>
        /// <b>A preparation made under one test capacity is not reused for another.</b> The rating is not a
        /// selection input, but it IS the ceiling Iteration 2B stops at for any dwelling assigned to the
        /// product - so reusing a preparation made at 165 l/s for a request that now says 175 would
        /// optimise against the old ceiling while the dialog reported the new one.
        /// </summary>
        [Fact]
        public void APreparationUnderADifferentTestCapacity_IsNotReused()
        {
            List<Zone> zones = [Zone("Flat 1")];

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, Strategies(zones), Descriptors())
            {
                EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling),
                ProjectTestVentilationUnit = TestUnit(),
            };

            //Same statement: reused.
            Assert.True(Reusable(partOPreparationContext, TestUnit(), zones));

            //Re-rated: not reused.
            Assert.False(Reusable(partOPreparationContext, new PartOProjectTestVentilationUnit(name_Test, 175, 170), zones));
            Assert.False(Reusable(partOPreparationContext, new PartOProjectTestVentilationUnit(name_Test, 165, 180), zones));

            //Renamed: not reused either - it is a different product.
            Assert.False(Reusable(partOPreparationContext, new PartOProjectTestVentilationUnit("Test unit B", 165, 170), zones));

            //Withdrawn: not reused.
            Assert.False(Reusable(partOPreparationContext, null, zones));
        }

        /// <summary>
        /// And a preparation that recorded none is reused by a request that states none - which is every
        /// project that has never used this feature, so it must cost nothing.
        /// </summary>
        [Fact]
        public void APreparationThatRecordsNoTestProduct_IsReusedByARequestThatStatesNone()
        {
            List<Zone> zones = [Zone("Flat 1")];

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, Strategies(zones), Descriptors())
            {
                EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts),
            };

            Assert.Null(partOPreparationContext.ProjectTestVentilationUnit);

            Assert.True(Reusable(partOPreparationContext, null, zones));

            //Stating one where the preparation had none is a difference.
            Assert.False(Reusable(partOPreparationContext, TestUnit(), zones));
        }

        // =================================================================================================
        // H. A manually assigned test product resolves its own rating
        // =================================================================================================

        /// <summary>
        /// A dwelling assigned the test product resolves its capacity, gets its own headroom, and is
        /// reported as sound - indistinguishable from a dwelling holding a manufacturer product, which is
        /// exactly the point.
        /// </summary>
        [Fact]
        public void ADwellingAssignedTheTestProduct_ResolvesItsRatingLikeAnyOther()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [
                    new PartOEquipmentAssignment(Guid.NewGuid(), "MVHR-01", "Flat 1 MVHR", "Flat 1", 160, 160, TestUnit().VentilationUnitReference),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling),
                Analytical.Query.CapacityDescriptors(TestUnit()));

            PartOEquipmentAssignment partOEquipmentAssignment = Assert.Single(partOEquipmentAssignmentSet.Assignments);

            Assert.Equal(PartOEquipmentAssignmentStatus.Ok, partOEquipmentAssignment.Status);
            Assert.Equal(165, partOEquipmentAssignment.MaximumSupply_Lps);
            Assert.Equal(170, partOEquipmentAssignment.MaximumExtract_Lps);
            Assert.Equal(5, partOEquipmentAssignment.SupplyHeadroom_Lps);
            Assert.Equal(10, partOEquipmentAssignment.ExtractHeadroom_Lps);
            Assert.False(partOEquipmentAssignment.IsOutsideAllowedPool);

            //And the picker offers it.
            Assert.Contains(partOEquipmentAssignmentSet.AllowedCandidates, x => x.VentilationUnitReference.Model == name_Test);
        }

        /// <summary>
        /// A dwelling assigned it beyond its rating is reported insufficient and <b>keeps it</b> - the
        /// what-if is not silently replaced by the XBC15 that would have served, and the dwelling's design
        /// airflow is not reduced to fit.
        /// </summary>
        [Fact]
        public void ADwellingBeyondTheTestProductsRating_IsReportedAndKeepsIt()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [
                    new PartOEquipmentAssignment(Guid.NewGuid(), "MVHR-01", "Flat 1 MVHR", "Flat 1", 180, 180, TestUnit().VentilationUnitReference),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling),
                Analytical.Query.CapacityDescriptors(TestUnit()));

            PartOEquipmentAssignment partOEquipmentAssignment = Assert.Single(partOEquipmentAssignmentSet.Assignments);

            Assert.Equal(PartOEquipmentAssignmentStatus.Insufficient, partOEquipmentAssignment.Status);
            Assert.Equal(name_Test, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.Equal(180, partOEquipmentAssignment.DesignSupplyDuty_Lps);

            //A suggestion is a value, not a mutation.
            Assert.Equal(model_XBC15, partOEquipmentAssignment.Suggestion?.VentilationUnitReference.Model);
            Assert.Equal(name_Test, partOEquipmentAssignment.VentilationUnitReference.Model);
        }

        /// <summary>
        /// Under the all-products mode the assignment table does not offer the test product either - those
        /// rows are an automatic rule's results, and the rule was never offered it.
        /// </summary>
        [Fact]
        public void UnderAutomaticAllProducts_TheAssignmentTableDoesNotOfferTheTestProduct()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [
                    new PartOEquipmentAssignment(Guid.NewGuid(), "MVHR-01", "Flat 1 MVHR", "Flat 1", 30, 30, MRXBOXReference()),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts),
                Analytical.Query.CapacityDescriptors(TestUnit()));

            Assert.Equal(2, partOEquipmentAssignmentSet.AllowedCandidates.Count);
            Assert.DoesNotContain(partOEquipmentAssignmentSet.AllowedCandidates, x => x.VentilationUnitReference.Model == name_Test);
        }

        // =================================================================================================
        // Fixtures
        // =================================================================================================

        /// <summary>The what-if the native acceptance script types in: 165 supply, 170 extract.</summary>
        private static PartOProjectTestVentilationUnit TestUnit()
        {
            return new PartOProjectTestVentilationUnit(name_Test, 165, 170);
        }

        /// <summary>The two shipped products at their shipped capacities and ranks.</summary>
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

        /// <summary>The shared control over the two-product catalogue, set up as production sets it up.</summary>
        private static PartOEquipmentSelectionControl Control()
        {
            return new PartOEquipmentSelectionControl
            {
                VentilationUnitCatalogue = Catalogue(),
            };
        }

        private static PartOCatalogueProductRow Row(PartOEquipmentSelectionControl control, string model)
        {
            PartOCatalogueProductRow result = control.CatalogueProductRows.Find(x => x.Model == model);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>The permitted pool's product models, sorted, so a pool can be asserted in one line.</summary>
        private static List<string> Models(PartOEquipmentSelection partOEquipmentSelection)
        {
            List<string> result = partOEquipmentSelection.AllowedVentilationUnitReferences.ConvertAll(x => x.Model);

            result.Sort(StringComparer.Ordinal);

            return result;
        }

        /// <summary>
        /// A project that states a test product, and optionally has dwellings fitted with a product - which
        /// is what makes removing it refusable.
        /// </summary>
        private static AnalyticalModel Model(PartOProjectTestVentilationUnit partOProjectTestVentilationUnit, VentilationUnitReference assign = null, int count = 0)
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

            for (int i = 0; i < count; i++)
            {
                AirHandlingUnit airHandlingUnit = Analytical.Create.AirHandlingUnit(string.Format("MVHR-{0:00}", i + 1));

                airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, assign);

                adjacencyCluster.AddObject(airHandlingUnit);
            }

            AnalyticalModel result = new("Block", null, null, null, adjacencyCluster, new MaterialLibrary("Materials"), new ProfileLibrary("Profiles"));

            if (partOProjectTestVentilationUnit is not null)
            {
                result.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, partOProjectTestVentilationUnit);
            }

            return result;
        }

        private static Zone Zone(string name)
        {
            Zone result = new(name);

            result.SetValue(ZoneParameter.IsDwelling, true);

            return result;
        }

        /// <summary>The Prepare &amp; Run window, set up as production sets it up.</summary>
        private static PartOWorkflowWindow Window(AnalyticalModel analyticalModel)
        {
            PartOWorkflowWindow result = new()
            {
                AnalyticalModel = analyticalModel,
                PartORun = new PartORun(),
                VentilationUnitCatalogue = Catalogue(),
                Capabilities = new PartOWorkflowCapabilities { EquipmentAvailable = true },
            };

            result.Restore(PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit), PartOWorkflowScope.AllDwellings, null);

            result.CompleteInitialisation();

            return result;
        }

        /// <summary>
        /// The Prepare Iteration window, set up exactly as <c>Modify.PreparePartOIteration</c> does it -
        /// catalogue, then the project's test product, then its preselection. That order is load-bearing:
        /// a permitted product is restored by ticking its row, and the test product has no row until it has
        /// been stated.
        /// </summary>
        private static PartOIterationWindow IterationWindow(AnalyticalModel analyticalModel)
        {
            PartOIterationWindow result = new()
            {
                Zones = analyticalModel.GetZones() ?? [],
                VentilationUnitCatalogue = Catalogue(),
            };

            result.ProjectTestVentilationUnit = analyticalModel.GetValue<PartOProjectTestVentilationUnit>(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit);
            result.ProjectTestVentilationUnitAssignmentCount = Query.PartOVentilationUnitAssignmentCount(analyticalModel, result.ProjectTestVentilationUnit?.VentilationUnitReference);
            result.EquipmentSelection = analyticalModel.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection);

            return result;
        }

        /// <summary>Whether the inspection would reuse a prepared iteration for this request.</summary>
        private static bool Reusable(PartOPreparationContext partOPreparationContext, PartOProjectTestVentilationUnit partOProjectTestVentilationUnit, List<Zone> zones)
        {
            PartORun partORun = new();

            AnalyticalModel analyticalModel = Model(null);

            Assert.True(partORun.Prepare(analyticalModel, [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)], partOPreparationContext));

            PartOWorkflowRequest partOWorkflowRequest = new(Option(), PartOWorkflowScope.SelectedDwellings, zones, true)
            {
                EquipmentSelection = partOPreparationContext.EquipmentSelection,
                ProjectTestVentilationUnit = partOProjectTestVentilationUnit,
            };

            return PartOWorkflowInspection.Inspect(analyticalModel, partOWorkflowRequest, partORun, new PartOWorkflowCapabilities { EquipmentAvailable = true }, null).ReusePreparation;
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

        /// <summary>
        /// The two shipped products, written to a temporary directory and read back through the production
        /// reader - so the control is given a real catalogue rather than a stub.
        /// </summary>
        private static VentilationUnitCatalogue Catalogue()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_ProjectTestProduct_{0}", Guid.NewGuid()));

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
