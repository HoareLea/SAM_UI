// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Analytical.UI.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The equipment-selection workflow: catalogue visibility, the permitted pool, convert to manual,
    /// per-dwelling override, and capacity validation.</b>
    ///
    /// <para><b>The gap these tests close</b></para>
    /// <para>
    /// Native testing confirmed that the UI showed the product each dwelling had been given but never the
    /// catalogue: an engineer could not see what products existed, their capacities, which were eligible,
    /// what the alternatives were, or how to assign a different one. Understanding the available products
    /// meant opening the catalogue JSON. Sections A and B below are what make that impossible again.
    /// </para>
    ///
    /// <para><b>The invariants everything else here defends</b></para>
    /// <list type="bullet">
    /// <item>Changing selected equipment changes NO airflow - not the Approved Document F requirement, not
    /// the design airflow, not the design transfer airflow, not the operating airflow.</item>
    /// <item>A pool is a candidate constraint and never an assignment.</item>
    /// <item>A suggestion is a value, never a mutation.</item>
    /// <item>An insufficient assignment is reported and held - never accommodated by reducing the design,
    /// and never replaced by a bigger product.</item>
    /// <item>Narrowing the pool flags an authored assignment; it never rewrites one.</item>
    /// </list>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOEquipmentSelectionUXTests
    {
        private const string model_MRXBOX = "MRXBOXAB-ECO5-AECV";

        private const string model_XBC15 = "XBC15";

        // =================================================================================================
        // A. Catalogue visibility - the confirmed gap
        // =================================================================================================

        /// <summary>
        /// <b>The fix, stated as a test.</b> The iteration window shows every catalogue product with its
        /// manufacturer, model and both maximum airflows - so an engineer can see what exists and what it
        /// can move without opening a JSON file or a separate diagnostic tool.
        /// </summary>
        [WpfFact]
        public void TheIterationWindow_ShowsEveryProductWithItsCapacities()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                VentilationUnitCatalogue = Catalogue(),
            };

            List<PartOCatalogueProductRow> rows = partOIterationWindow.CatalogueProductRows;

            Assert.Equal(2, rows.Count);

            PartOCatalogueProductRow row_MRXBOX = Row(rows, model_MRXBOX);
            PartOCatalogueProductRow row_XBC15 = Row(rows, model_XBC15);

            Assert.Equal("Nuaire", row_MRXBOX.Manufacturer);
            Assert.Equal(150, row_MRXBOX.MaximumSupply_Lps);
            Assert.Equal(150, row_MRXBOX.MaximumExtract_Lps);

            Assert.Equal("Nuaire", row_XBC15.Manufacturer);
            Assert.Equal(190, row_XBC15.MaximumSupply_Lps);
            Assert.Equal(190, row_XBC15.MaximumExtract_Lps);
        }

        /// <summary>
        /// The same, against the catalogue SAM actually ships - so the native acceptance walk sees the two
        /// real products at their published capacities. Skipped rather than failed where the resource is not
        /// deployed beside the test host, exactly as the other shipped-catalogue tests are.
        /// </summary>
        [WpfFact]
        public void TheShippedCatalogue_ShowsTheTwoRealProducts()
        {
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

            if (ventilationUnitCatalogue.State != VentilationUnitCatalogueState.Selectable)
            {
                return;
            }

            PartOIterationWindow partOIterationWindow = new()
            {
                VentilationUnitCatalogue = ventilationUnitCatalogue,
            };

            List<PartOCatalogueProductRow> rows = partOIterationWindow.CatalogueProductRows;

            Assert.Equal(150, Row(rows, model_MRXBOX).MaximumSupply_Lps);
            Assert.Equal(150, Row(rows, model_MRXBOX).MaximumExtract_Lps);
            Assert.Equal(190, Row(rows, model_XBC15).MaximumSupply_Lps);
            Assert.Equal(190, Row(rows, model_XBC15).MaximumExtract_Lps);
        }

        /// <summary>
        /// A catalogue that could not be read shows no products and disables the equipment controls - and
        /// the iteration can still be prepared, because a missing catalogue is not a statement that no
        /// product could serve these dwellings.
        /// </summary>
        [WpfFact]
        public void AnUnreadableCatalogue_ShowsNoProductsAndBlocksNothingElse()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                VentilationUnitCatalogue = VentilationUnitCatalogue.Read(Path.Combine(Path.GetTempPath(), string.Format("SAM_NoCatalogue_{0}", Guid.NewGuid()))),
            };

            Assert.Empty(partOIterationWindow.CatalogueProductRows);
            Assert.False(partOIterationWindow.SelectVentilationUnit);
        }

        // =================================================================================================
        // B. Mode and pool, as the window states them
        // =================================================================================================

        /// <summary>
        /// Under "Automatic - all catalogue products" every product is permitted, and the whole catalogue is
        /// the candidate set.
        /// </summary>
        [WpfFact]
        public void AutomaticAllProducts_PermitsEveryProduct()
        {
            PartOIterationWindow partOIterationWindow = Window();

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticAllProducts, partOIterationWindow.Mode);

            PartOEquipmentSelection partOEquipmentSelection = partOIterationWindow.EquipmentSelection;

            Assert.Equal(2, partOEquipmentSelection.CandidateDescriptors(Descriptors()).Count);
        }

        /// <summary>
        /// Unticking a product under the pooled mode removes it from the candidate set, and the smallest
        /// capable rule then answers out of what is left - the XBC15 at a duty the MRXBOX could have served.
        /// </summary>
        [WpfFact]
        public void UntickingAProduct_RemovesItFromTheCandidateSet()
        {
            PartOIterationWindow partOIterationWindow = Window(PartOEquipmentSelectionMode.AutomaticSelectedPool);

            Row(partOIterationWindow.CatalogueProductRows, model_MRXBOX).IsUsed = false;

            PartOEquipmentSelection partOEquipmentSelection = partOIterationWindow.EquipmentSelection;

            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = partOEquipmentSelection.CandidateDescriptors(Descriptors());

            Assert.Equal(model_XBC15, Assert.Single(ventilationUnitCapacityDescriptors).VentilationUnitReference.Model);

            //And the rule, over exactly that, puts the larger product on a 150 l/s duty.
            Assert.Equal(model_XBC15, ventilationUnitCapacityDescriptors.SelectSmallestCapableVentilationUnit(150, 150).VentilationUnitReference.Model);
        }

        /// <summary>
        /// <b>An empty pool is reported, never widened.</b> The window says the iteration will not prepare
        /// and says explicitly that SAM will not fall back to the rest of the catalogue - because a pool
        /// that silently became the whole catalogue would produce an answer that looked entirely correct.
        /// </summary>
        [WpfFact]
        public void AnEmptyPool_IsReportedAndNeverWidened()
        {
            PartOIterationWindow partOIterationWindow = Window(PartOEquipmentSelectionMode.AutomaticSelectedPool);

            foreach (PartOCatalogueProductRow partOCatalogueProductRow in partOIterationWindow.CatalogueProductRows)
            {
                partOCatalogueProductRow.IsUsed = false;
            }

            Assert.Empty(partOIterationWindow.EquipmentSelection.CandidateDescriptors(Descriptors()));

            Assert.Contains("will not prepare", partOIterationWindow.CatalogueDescription);
            Assert.Contains("will not fall back", partOIterationWindow.CatalogueDescription);
        }

        /// <summary>
        /// Under "Automatic - all" the ticks are all set and cannot be edited: every product is eligible by
        /// definition, so an editable tick would offer a choice the mode does not have - but the catalogue
        /// stays on screen, because that visibility is the whole point.
        /// </summary>
        [WpfFact]
        public void UnderAutomaticAll_TheCatalogueIsShownAndLocked()
        {
            PartOIterationWindow partOIterationWindow = Window(PartOEquipmentSelectionMode.AutomaticSelectedPool);

            Row(partOIterationWindow.CatalogueProductRows, model_MRXBOX).IsUsed = false;

            //Back to "all": the untick is overridden, because the mode says every product is a candidate.
            partOIterationWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts);

            Assert.All(partOIterationWindow.CatalogueProductRows, x => Assert.True(x.IsUsed));
            Assert.NotEmpty(partOIterationWindow.CatalogueProductRows);
        }

        /// <summary>
        /// <b>The dialog does not lose the configuration.</b> A mode and a pool set on the window are read
        /// back off it unchanged, which is the mechanism by which a project's own preselection survives
        /// closing and reopening the Part O dialog.
        /// </summary>
        [WpfFact]
        public void TheConfiguration_RoundTripsThroughTheWindow()
        {
            PartOIterationWindow partOIterationWindow = Window();

            partOIterationWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [XBC15Reference()]);

            PartOEquipmentSelection partOEquipmentSelection = partOIterationWindow.EquipmentSelection;

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOEquipmentSelection.Mode);
            Assert.Equal(model_XBC15, Assert.Single(partOEquipmentSelection.AllowedVentilationUnitReferences).Model);
        }

        /// <summary>Manual authority round trips too, and does not revert to an automatic mode.</summary>
        [WpfFact]
        public void ManualAuthority_RoundTripsThroughTheWindow_AndDoesNotRevert()
        {
            PartOIterationWindow partOIterationWindow = Window();

            partOIterationWindow.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [MRXBOXReference()]);

            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOIterationWindow.Mode);
            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOIterationWindow.EquipmentSelection.Mode);

            //And manual mode runs no rule, which is how the identities already on the model survive.
            Assert.Null(partOIterationWindow.EquipmentSelection.CandidateDescriptors(Descriptors()));
        }

        /// <summary>
        /// A restored pool naming a product the current catalogue no longer holds keeps no permission
        /// nothing can act on - there is no row to tick, so it drops out rather than lingering as a
        /// permission for a product that cannot be offered or resolved.
        /// </summary>
        [WpfFact]
        public void ARestoredPool_NamingAnAbsentProduct_DropsIt()
        {
            PartOIterationWindow partOIterationWindow = Window();

            partOIterationWindow.EquipmentSelection = new PartOEquipmentSelection(
                PartOEquipmentSelectionMode.AutomaticSelectedPool,
                [XBC15Reference(), new VentilationUnitReference("Nuaire", "XBC-Withdrawn", null)]);

            Assert.Equal(model_XBC15, Assert.Single(partOIterationWindow.EquipmentSelection.AllowedVentilationUnitReferences).Model);
        }

        /// <summary>
        /// <b>The wiring invariant the whole design turns on.</b> The pool narrows the CANDIDATE set while
        /// the preparation context keeps the WHOLE catalogue - because that context is the capability lookup
        /// Iteration 2B reads to find what each dwelling's already-selected product is rated at. Narrowing
        /// it would make a dwelling assigned a product that has since left the pool report an unknown
        /// capacity, and 2B would lose the ceiling it stops at.
        /// </summary>
        [Fact]
        public void ThePoolNarrowsCandidates_WhileTheContextKeepsTheWholeCatalogue()
        {
            PartOEquipmentSelection partOEquipmentSelection = new(PartOEquipmentSelectionMode.AutomaticSelectedPool, [MRXBOXReference()]);

            Assert.Single(partOEquipmentSelection.CandidateDescriptors(Descriptors()));

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, [], [], Descriptors())
            {
                EquipmentSelection = partOEquipmentSelection,
            };

            Assert.Equal(2, partOPreparationContext.VentilationUnitCapacityDescriptors.Count);
            Assert.True(partOPreparationContext.HasVentilationUnitCatalogue);

            //And the withdrawn-from-pool product still resolves its own rating out of that context.
            Assert.Contains(partOPreparationContext.VentilationUnitCapacityDescriptors, x => x.VentilationUnitReference.Matches(XBC15Reference()));
        }

        // =================================================================================================
        // C. Convert to Manual - an authority change, and nothing else
        // =================================================================================================

        /// <summary>
        /// <b>Convert to Manual preserves every dwelling's product exactly.</b> Three dwellings automatically
        /// given the MRXBOX are still three MRXBOX dwellings afterwards; nothing is reselected, nothing
        /// smaller or "better" is chosen, and no design duty moves.
        /// </summary>
        [Fact]
        public void ConvertToManual_PreservesEveryIdentityExactly()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = AutomaticSet();

            List<string> models_Before = Models(partOEquipmentAssignmentSet);
            List<string> duties_Before = Duties(partOEquipmentAssignmentSet);

            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.True(partOEquipmentAssignmentSet.IsManual);
            Assert.Equal([model_MRXBOX, model_MRXBOX, model_MRXBOX], models_Before);
            Assert.Equal(models_Before, Models(partOEquipmentAssignmentSet));
            Assert.Equal(duties_Before, Duties(partOEquipmentAssignmentSet));
        }

        /// <summary>
        /// And it writes NOTHING. Conversion changes no identity, so there is no changed row to commit and a
        /// model adopting a converted table is bit-for-bit the model that was prepared.
        /// </summary>
        [Fact]
        public void ConvertToManual_WritesNothingToTheModel()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = AutomaticSet();

            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.False(partOEquipmentAssignmentSet.HasChanges);

            AdjacencyCluster adjacencyCluster = Cluster(out _);

            Assert.True(partOEquipmentAssignmentSet.Commit(adjacencyCluster, out List<string> notes, out List<string> refusals));

            Assert.Empty(notes);
            Assert.Empty(refusals);
        }

        /// <summary>
        /// A dwelling with no valid assignment is <b>exposed as such</b> by conversion, never given a
        /// plausible product to make the table look complete.
        /// </summary>
        [Fact]
        public void ConvertToManual_ExposesADwellingWithNoAssignment()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [
                    Assignment("Flat 1", 63, 63, MRXBOXReference()),
                    Assignment("Flat 2", 63, 63, null),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts));

            partOEquipmentAssignmentSet.ConvertToManual();

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 2");

            Assert.Equal(PartOEquipmentAssignmentStatus.NotAssigned, partOEquipmentAssignment.Status);
            Assert.False(partOEquipmentAssignment.IsAssigned);
            Assert.Contains("No product is assigned", partOEquipmentAssignment.Description);

            //And nothing was invented for it.
            Assert.Null(partOEquipmentAssignment.VentilationUnitReference);
        }

        // =================================================================================================
        // D. Per-dwelling override
        // =================================================================================================

        /// <summary>
        /// <b>Overriding one dwelling changes that dwelling and no other.</b> Flat 1 moves from the MRXBOX to
        /// the XBC15 and resolves 190 l/s; Flats 2 and 3 stay on the MRXBOX at 150. No design duty moves,
        /// because a different box is not a statement about how much air a dwelling moves.
        /// </summary>
        [Fact]
        public void OverridingOneDwelling_ChangesThatDwellingAlone_AndNoAirflow()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = AutomaticSet();

            partOEquipmentAssignmentSet.ConvertToManual();

            List<string> duties_Before = Duties(partOEquipmentAssignmentSet);

            Assert.True(partOEquipmentAssignmentSet.Assign(DwellingGuid("Flat 1"), XBC15Reference(), out string refusal));
            Assert.Null(refusal);

            Assert.Equal(model_XBC15, Assignment(partOEquipmentAssignmentSet, "Flat 1").VentilationUnitReference.Model);
            Assert.Equal(190, Assignment(partOEquipmentAssignmentSet, "Flat 1").MaximumSupply_Lps);

            Assert.Equal(model_MRXBOX, Assignment(partOEquipmentAssignmentSet, "Flat 2").VentilationUnitReference.Model);
            Assert.Equal(model_MRXBOX, Assignment(partOEquipmentAssignmentSet, "Flat 3").VentilationUnitReference.Model);
            Assert.Equal(150, Assignment(partOEquipmentAssignmentSet, "Flat 2").MaximumSupply_Lps);

            //Not one design duty moved.
            Assert.Equal(duties_Before, Duties(partOEquipmentAssignmentSet));

            //Headroom grew because the BOX grew, and headroom is deliberately not taken up: Flat 1 is
            //designed at 23 l/s and the XBC15 can move 190, so 167 l/s sits unused.
            Assert.Equal(167, Assignment(partOEquipmentAssignmentSet, "Flat 1").SupplyHeadroom_Lps);
        }

        /// <summary>
        /// <b>A deliberately oversized capable product is kept.</b> Automatic selection answers the MRXBOX at
        /// these duties; an engineer who states the XBC15 has made a valid engineering decision, and nothing
        /// corrects it back to the smallest capable product.
        /// </summary>
        [Fact]
        public void ADeliberatelyOversizedChoice_IsNeverCorrectedBack()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = AutomaticSet();

            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.True(partOEquipmentAssignmentSet.Assign(DwellingGuid("Flat 2"), XBC15Reference(), out _));

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 2");

            Assert.Equal(model_XBC15, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.Equal(PartOEquipmentAssignmentStatus.Ok, partOEquipmentAssignment.Status);

            //An OK row is offered no suggestion at all - a dwelling that works does not need to be told a
            //different box exists, and offering one would read as a correction of a sound decision.
            Assert.Null(partOEquipmentAssignment.Suggestion);
        }

        /// <summary>A product that identifies nothing is refused, and the existing assignment survives.</summary>
        [Fact]
        public void AnIdentitylessProduct_IsRefused()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = AutomaticSet();

            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.False(partOEquipmentAssignmentSet.Assign(DwellingGuid("Flat 1"), new VentilationUnitReference(), out string refusal));
            Assert.False(string.IsNullOrWhiteSpace(refusal));

            Assert.Equal(model_MRXBOX, Assignment(partOEquipmentAssignmentSet, "Flat 1").VentilationUnitReference.Model);
        }

        // =================================================================================================
        // E. Capacity validation - insufficient is held and reported, and a suggestion is only ever offered
        // =================================================================================================

        /// <summary>
        /// <b>175 l/s with the 150 l/s MRXBOX assigned.</b> The status is Insufficient, the assignment stays
        /// the MRXBOX, the design duty is NOT reduced to 150, no larger product is substituted, and the
        /// XBC15 is offered as a suggestion.
        /// </summary>
        [Fact]
        public void AnInsufficientAssignment_IsHeldAndReported_WithASuggestion()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [Assignment("Flat 1", 175, 175, MRXBOXReference())],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 1");

            Assert.Equal(PartOEquipmentAssignmentStatus.Insufficient, partOEquipmentAssignment.Status);

            //Held.
            Assert.Equal(model_MRXBOX, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.Equal(150, partOEquipmentAssignment.MaximumSupply_Lps);

            //The design duty was not cut back to make the box fit.
            Assert.Equal(175, partOEquipmentAssignment.DesignSupplyDuty_Lps);
            Assert.Equal(175, partOEquipmentAssignment.DesignExtractDuty_Lps);

            //Suggested, and named.
            Assert.NotNull(partOEquipmentAssignment.Suggestion);
            Assert.Equal(model_XBC15, partOEquipmentAssignment.Suggestion.VentilationUnitReference.Model);
            Assert.Equal(190, partOEquipmentAssignment.Suggestion.MaximumSupplyFlowRate_Lps);

            //And said in words an engineer can act on.
            Assert.Contains(model_XBC15, partOEquipmentAssignment.Description);
            Assert.Contains("assignment stands", partOEquipmentAssignment.Description);
        }

        /// <summary>
        /// <b>Suggestion is not mutation.</b> Calculating one changed no identity, so there is nothing to
        /// commit - the model is untouched until the engineer takes it.
        /// </summary>
        [Fact]
        public void ASuggestion_MutatesNothing()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [Assignment("Flat 1", 175, 175, MRXBOXReference())],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            Assert.NotNull(Assignment(partOEquipmentAssignmentSet, "Flat 1").Suggestion);

            Assert.False(partOEquipmentAssignmentSet.HasChanges);
            Assert.Equal(model_MRXBOX, Assignment(partOEquipmentAssignmentSet, "Flat 1").VentilationUnitReference.Model);
        }

        /// <summary>Taking the suggestion is an explicit act, and then the identity moves.</summary>
        [Fact]
        public void TakingTheSuggestion_IsExplicit_AndThenResolves190()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [Assignment("Flat 1", 175, 175, MRXBOXReference())],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            Assert.True(partOEquipmentAssignmentSet.AssignSuggested(DwellingGuid("Flat 1"), out string refusal));
            Assert.Null(refusal);

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 1");

            Assert.Equal(model_XBC15, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.Equal(190, partOEquipmentAssignment.MaximumSupply_Lps);
            Assert.Equal(PartOEquipmentAssignmentStatus.Ok, partOEquipmentAssignment.Status);

            //Still 175 l/s of design duty. Taking a bigger box did not raise the design.
            Assert.Equal(175, partOEquipmentAssignment.DesignSupplyDuty_Lps);
        }

        /// <summary>
        /// An insufficient assignment with nothing capable permitted gets no suggestion, and is still held.
        /// There is no "least bad" product, and the design is still not reduced.
        /// </summary>
        [Fact]
        public void WithNoCapableProductPermitted_NothingIsSuggested_AndTheAssignmentStillStands()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [Assignment("Flat 1", 175, 175, MRXBOXReference())],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [MRXBOXReference()]));

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 1");

            Assert.Equal(PartOEquipmentAssignmentStatus.Insufficient, partOEquipmentAssignment.Status);
            Assert.Null(partOEquipmentAssignment.Suggestion);
            Assert.Equal(model_MRXBOX, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.Equal(175, partOEquipmentAssignment.DesignSupplyDuty_Lps);
        }

        /// <summary>
        /// A product the current catalogue does not hold is <b>capacity unknown</b>, which is not a pass and
        /// not a failure of the design - it is a catalogue problem, and it is named as one.
        /// </summary>
        [Fact]
        public void AProductTheCatalogueDoesNotHold_IsUnknownAndNotAPass()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [Assignment("Flat 1", 63, 63, new VentilationUnitReference("Nuaire", "XBC-Withdrawn", null))],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 1");

            Assert.Equal(PartOEquipmentAssignmentStatus.CapacityUnknown, partOEquipmentAssignment.Status);
            Assert.True(double.IsNaN(partOEquipmentAssignment.MaximumSupply_Lps));
            Assert.Contains("unknown", partOEquipmentAssignment.Description);
        }

        /// <summary>
        /// A dwelling with NO design duty is unknown rather than met - every non-negative capacity satisfies
        /// a duty of zero, and reporting that as OK would say the plant is fine for a dwelling that
        /// currently moves no air.
        /// </summary>
        [Fact]
        public void NoDesignDuty_IsUnknownRatherThanMet()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [Assignment("Flat 1", double.NaN, double.NaN, MRXBOXReference())],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            Assert.Equal(PartOEquipmentAssignmentStatus.CapacityUnknown, Assignment(partOEquipmentAssignmentSet, "Flat 1").Status);
        }

        // =================================================================================================
        // F. The pool versus an authored assignment
        // =================================================================================================

        /// <summary>
        /// <b>Narrowing the pool after an assignment exists flags it and rewrites nothing.</b> Flat 1 keeps
        /// the XBC15 it was authored with, is marked as outside the current pool, and the engineer decides -
        /// SAM does not silently delete or replace an authored manual assignment.
        /// </summary>
        [Fact]
        public void NarrowingThePool_FlagsAnAuthoredAssignment_AndRewritesNothing()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = AutomaticSet();

            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.True(partOEquipmentAssignmentSet.Assign(DwellingGuid("Flat 1"), XBC15Reference(), out _));

            List<string> duties_Before = Duties(partOEquipmentAssignmentSet);

            //The XBC15 leaves the permitted set.
            partOEquipmentAssignmentSet.SetAllowedVentilationUnitReferences([MRXBOXReference()]);

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 1");

            //Kept, flagged, and still rated - the capability lookup is the whole catalogue, not the pool.
            Assert.Equal(model_XBC15, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.True(partOEquipmentAssignment.IsOutsideAllowedPool);
            Assert.Equal(190, partOEquipmentAssignment.MaximumSupply_Lps);
            Assert.Equal(PartOEquipmentAssignmentStatus.Ok, partOEquipmentAssignment.Status);
            Assert.Contains("outside the current allowed pool", partOEquipmentAssignment.Description);

            //Nothing else moved, and no other dwelling was touched.
            Assert.Equal(duties_Before, Duties(partOEquipmentAssignmentSet));
            Assert.Equal(model_MRXBOX, Assignment(partOEquipmentAssignmentSet, "Flat 2").VentilationUnitReference.Model);
            Assert.False(Assignment(partOEquipmentAssignmentSet, "Flat 2").IsOutsideAllowedPool);
        }

        /// <summary>
        /// And that dwelling's own product stays pickable, so a pool change cannot make an authored
        /// assignment impossible to re-choose from the picker it is already showing.
        /// </summary>
        [Fact]
        public void ADwellingsOwnProductOutsideThePool_StaysInItsCandidates()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = AutomaticSet();

            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.True(partOEquipmentAssignmentSet.Assign(DwellingGuid("Flat 1"), XBC15Reference(), out _));

            partOEquipmentAssignmentSet.SetAllowedVentilationUnitReferences([MRXBOXReference()]);

            List<string> models_Flat1 = partOEquipmentAssignmentSet.Candidates(DwellingGuid("Flat 1")).ConvertAll(x => x.VentilationUnitReference.Model);

            Assert.Contains(model_MRXBOX, models_Flat1);
            Assert.Contains(model_XBC15, models_Flat1);

            //Flat 2, which holds a permitted product, is offered the pool alone.
            Assert.Equal([model_MRXBOX], partOEquipmentAssignmentSet.Candidates(DwellingGuid("Flat 2")).ConvertAll(x => x.VentilationUnitReference.Model));
        }

        // =================================================================================================
        // G. Commit - the one write, and only what changed
        // =================================================================================================

        /// <summary>
        /// Commit writes the changed row and leaves the others entirely alone - so adopting a table where
        /// one dwelling was overridden touches one air handling unit.
        /// </summary>
        [Fact]
        public void Commit_WritesOnlyTheChangedRow()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out List<AirHandlingUnit> airHandlingUnits);

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = PartOEquipmentAssignmentSet.Create(
                adjacencyCluster,
                airHandlingUnits,
                [],
                Names(airHandlingUnits),
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            //Every unit already carries the MRXBOX - see Cluster.
            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal(model_MRXBOX, x.VentilationUnitReference.Model));

            Guid guid = airHandlingUnits[0].Guid;

            Assert.True(partOEquipmentAssignmentSet.Assign(guid, XBC15Reference(), out _));
            Assert.True(partOEquipmentAssignmentSet.Commit(adjacencyCluster, out List<string> notes, out List<string> refusals));

            Assert.Empty(refusals);
            Assert.Single(notes);

            foreach (AirHandlingUnit airHandlingUnit in adjacencyCluster.GetObjects<AirHandlingUnit>())
            {
                Assert.Equal(
                    airHandlingUnit.Guid == guid ? model_XBC15 : model_MRXBOX,
                    airHandlingUnit.SelectedVentilationUnitReference()?.Model);
            }
        }

        // =================================================================================================
        // H. The preparation window
        // =================================================================================================

        /// <summary>
        /// The assignment grid is read-only under an automatic authority - its rows are results - and
        /// becomes editable once the engineer has converted to manual.
        /// </summary>
        [WpfFact]
        public void TheAssignmentGrid_IsReadOnlyUntilConverted()
        {
            PartOPreparationWindow partOPreparationWindow = new()
            {
                EquipmentAssignmentSet = AutomaticSet(),
            };

            Assert.False(partOPreparationWindow.IsEquipmentEditable);
            Assert.True(partOPreparationWindow.CanConvertToManual);

            partOPreparationWindow.ConvertToManual();

            Assert.True(partOPreparationWindow.IsEquipmentEditable);

            //And it cannot be converted twice.
            Assert.False(partOPreparationWindow.CanConvertToManual);
        }

        /// <summary>
        /// Conversion through the window preserves every product exactly, and the rows say so - the grid an
        /// engineer is looking at is the same three dwellings on the same three products.
        /// </summary>
        [WpfFact]
        public void TheWindowsConversion_PreservesEveryProduct()
        {
            PartOPreparationWindow partOPreparationWindow = new()
            {
                EquipmentAssignmentSet = AutomaticSet(),
            };

            List<string> products_Before = partOPreparationWindow.EquipmentRows.ConvertAll(x => x.SelectedProduct);

            partOPreparationWindow.ConvertToManual();

            Assert.Equal(products_Before, partOPreparationWindow.EquipmentRows.ConvertAll(x => x.SelectedProduct));
            Assert.All(partOPreparationWindow.EquipmentRows, x => Assert.Contains(model_MRXBOX, x.SelectedProduct));
            Assert.False(partOPreparationWindow.EquipmentAssignmentSet.HasChanges);
        }

        /// <summary>
        /// Conversion is not offered where there is no automatic answer to convert - an Iteration 1a
        /// preparation has no equipment table at all.
        /// </summary>
        [WpfFact]
        public void ConvertToManual_IsNotOfferedWithoutAnAssignment()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            Assert.False(partOPreparationWindow.CanConvertToManual);
            Assert.False(partOPreparationWindow.IsEquipmentEditable);

            partOPreparationWindow.EquipmentAssignmentSet = new PartOEquipmentAssignmentSet(
                [Assignment("Flat 1", 63, 63, null)],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts));

            Assert.False(partOPreparationWindow.CanConvertToManual);
        }

        /// <summary>
        /// A row's editability follows the authority, which is what makes the picker inert in the automatic
        /// modes and live in manual mode.
        /// </summary>
        [WpfFact]
        public void ARowsPicker_IsInertUntilTheEngineerHasAuthority()
        {
            PartOPreparationWindow partOPreparationWindow = new()
            {
                EquipmentAssignmentSet = AutomaticSet(),
            };

            Assert.All(partOPreparationWindow.EquipmentRows, x => Assert.False(x.IsEditable));

            partOPreparationWindow.ConvertToManual();

            Assert.All(partOPreparationWindow.EquipmentRows, x => Assert.True(x.IsEditable));
            Assert.All(partOPreparationWindow.EquipmentRows, x => Assert.NotEmpty(x.Candidates));
        }

        // =================================================================================================
        // Fixtures
        // =================================================================================================

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

        /// <summary>
        /// The accepted fixture's shape, as an automatic result: three dwellings at 23/23, 63/63 and 63/63
        /// l/s, all given the MRXBOX because it is the smallest product that can serve any of them.
        /// </summary>
        private static PartOEquipmentAssignmentSet AutomaticSet()
        {
            return new PartOEquipmentAssignmentSet(
                [
                    Assignment("Flat 1", 23, 23, MRXBOXReference()),
                    Assignment("Flat 2", 63, 63, MRXBOXReference()),
                    Assignment("Flat 3", 63, 63, MRXBOXReference()),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts));
        }

        /// <summary>
        /// One dwelling's captured row. The guid is derived from the dwelling's name so a test can address a
        /// dwelling by name without holding a table of guids.
        /// </summary>
        private static PartOEquipmentAssignment Assignment(string dwelling, double supplyDuty_Lps, double extractDuty_Lps, VentilationUnitReference ventilationUnitReference)
        {
            return new PartOEquipmentAssignment(DwellingGuid(dwelling), string.Format("MVHR-{0}", dwelling), string.Format("{0} MVHR", dwelling), dwelling, supplyDuty_Lps, extractDuty_Lps, ventilationUnitReference);
        }

        private static PartOEquipmentAssignment Assignment(PartOEquipmentAssignmentSet partOEquipmentAssignmentSet, string dwelling)
        {
            PartOEquipmentAssignment result = partOEquipmentAssignmentSet.Assignments.Find(x => x.DwellingName == dwelling);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>A stable guid per dwelling name, so the fixtures are addressable and repeatable.</summary>
        private static Guid DwellingGuid(string dwelling)
        {
            byte[] bytes = new byte[16];

            for (int i = 0; i < dwelling.Length && i < 16; i++)
            {
                bytes[i] = (byte)dwelling[i];
            }

            return new Guid(bytes);
        }

        private static List<string> Models(PartOEquipmentAssignmentSet partOEquipmentAssignmentSet)
        {
            return partOEquipmentAssignmentSet.Assignments.ConvertAll(x => x.VentilationUnitReference?.Model);
        }

        /// <summary>
        /// Every dwelling's design duty on both sides, so a whole table can be compared before and after any
        /// equipment change. This is the airflow-authority assertion, in one line.
        /// </summary>
        private static List<string> Duties(PartOEquipmentAssignmentSet partOEquipmentAssignmentSet)
        {
            return partOEquipmentAssignmentSet.Assignments.ConvertAll(x => string.Format("{0}: {1:0.######}/{2:0.######}", x.DwellingName, x.DesignSupplyDuty_Lps, x.DesignExtractDuty_Lps));
        }

        /// <summary>
        /// Three air handling units in a real cluster, each already carrying the MRXBOX, for the one test
        /// that commits. No systems and no terminals: what commit does is write an identity, and a duty is
        /// not part of that.
        /// </summary>
        private static AdjacencyCluster Cluster(out List<AirHandlingUnit> airHandlingUnits)
        {
            AdjacencyCluster result = new();

            airHandlingUnits = [];

            foreach (string name in new[] { "MVHR-01", "MVHR-02", "MVHR-03" })
            {
                AirHandlingUnit airHandlingUnit = Analytical.Create.AirHandlingUnit(name);

                airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, MRXBOXReference());

                result.AddObject(airHandlingUnit);

                airHandlingUnits.Add(airHandlingUnit);
            }

            return result;
        }

        private static Dictionary<Guid, string> Names(List<AirHandlingUnit> airHandlingUnits)
        {
            Dictionary<Guid, string> result = [];

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                result[airHandlingUnit.Guid] = airHandlingUnit.Name;
            }

            return result;
        }

        /// <summary>The iteration window over the two-product catalogue, in a stated mode.</summary>
        private static PartOIterationWindow Window(PartOEquipmentSelectionMode partOEquipmentSelectionMode = PartOEquipmentSelectionMode.AutomaticAllProducts)
        {
            PartOIterationWindow result = new()
            {
                VentilationUnitCatalogue = Catalogue(),
            };

            result.EquipmentSelection = new PartOEquipmentSelection(partOEquipmentSelectionMode);

            return result;
        }

        private static PartOCatalogueProductRow Row(List<PartOCatalogueProductRow> rows, string model)
        {
            PartOCatalogueProductRow result = rows.Find(x => x.Model == model);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>
        /// The two shipped products, written to a temporary directory and read back through the production
        /// reader - so the window is given a real catalogue rather than a stub, and the capacities under test
        /// are the ones the shipped file states. That they match the shipped file is asserted in
        /// SAM_Systems' own VentilationUnitCatalogueTests.
        /// </summary>
        private static VentilationUnitCatalogue Catalogue()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_EquipmentSelection_{0}", Guid.NewGuid()));

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
