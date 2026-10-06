// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Bulk manual equipment assignment: one product, many dwellings, one deliberate act.</b>
    ///
    /// <para><b>Why it exists</b></para>
    /// <para>
    /// A real project has a hundred or a thousand flats, and authoring their equipment one row at a time is
    /// not a workflow. So a selection of rows can be assigned a product at once.
    /// </para>
    ///
    /// <para><b>Why it is a button and not a side effect of editing a cell</b></para>
    /// <para>
    /// The obvious alternative - a picker that writes its value into every highlighted row - is triggered by
    /// an ordinary mis-click and leaves no sign of having happened. So single-row editing stays single-row,
    /// and this is a named action beside a count of what it will touch.
    /// </para>
    ///
    /// <para><b>The invariants it must not break</b></para>
    /// <list type="bullet">
    /// <item>Unselected dwellings are untouched.</item>
    /// <item>Every selected dwelling is validated against its <b>own</b> design duty - one product can be
    /// sufficient for eleven flats and insufficient for the twelfth.</item>
    /// <item>An insufficient assignment stays assigned and is reported. The design airflow is not reduced
    /// to fit it, and a larger product is never substituted for it.</item>
    /// <item>No airflow of any kind moves - not the Approved Document F requirement, not the design
    /// airflow, not the design transfer airflow, not the operating airflow.</item>
    /// <item>Nothing reaches a model until Commit, and a cancelled dialog writes nothing at all.</item>
    /// <item>It stays linear in the number of selected dwellings.</item>
    /// </list>
    /// </summary>
    public class PartOEquipmentBulkAssignmentTests
    {
        private const string model_MRXBOX = "MRXBOXAB-ECO5-AECV";

        private const string model_XBC15 = "XBC15";

        // =================================================================================================
        // 1. The selected dwellings, and only those
        // =================================================================================================

        /// <summary>
        /// Flat 1 and Flat 2 selected, the XBC15 applied: both take it, and <b>Flat 3 is untouched</b> -
        /// still the MRXBOX, and still reporting no change of its own.
        /// </summary>
        [Fact]
        public void TwoSelectedDwellings_TakeTheProduct_AndTheThirdIsUntouched()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet();

            Assert.True(partOEquipmentAssignmentSet.Assign(
                [DwellingGuid("Flat 1"), DwellingGuid("Flat 2")],
                XBC15Reference(),
                out List<Guid> guids_Assigned,
                out List<string> refusals));

            Assert.Empty(refusals);
            Assert.Equal(2, guids_Assigned.Count);

            Assert.Equal(model_XBC15, Assignment(partOEquipmentAssignmentSet, "Flat 1").VentilationUnitReference.Model);
            Assert.Equal(model_XBC15, Assignment(partOEquipmentAssignmentSet, "Flat 2").VentilationUnitReference.Model);

            //The one that was not selected, and every derived column of it.
            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 3");

            Assert.Equal(model_MRXBOX, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.False(partOEquipmentAssignment.HasChanged);
            Assert.Equal(150, partOEquipmentAssignment.MaximumSupply_Lps);
        }

        /// <summary>
        /// A dwelling named twice in one operation is assigned once, and a dwelling that is not part of the
        /// table is refused by name rather than silently ignored.
        /// </summary>
        [Fact]
        public void ARepeatedDwellingIsAssignedOnce_AndAnUnknownOneIsRefused()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet();

            Assert.True(partOEquipmentAssignmentSet.Assign(
                [DwellingGuid("Flat 1"), DwellingGuid("Flat 1")],
                XBC15Reference(),
                out List<Guid> guids_Assigned,
                out List<string> _));

            Assert.Single(guids_Assigned);

            Assert.False(partOEquipmentAssignmentSet.Assign(
                [Guid.NewGuid()],
                XBC15Reference(),
                out List<Guid> guids_Assigned_Unknown,
                out List<string> refusals));

            Assert.Empty(guids_Assigned_Unknown);
            Assert.Single(refusals);
        }

        /// <summary>
        /// A product that identifies nothing is refused <b>once</b> for the whole operation rather than
        /// once per dwelling, and no dwelling's existing assignment moves.
        /// </summary>
        [Fact]
        public void AProductThatIdentifiesNothing_IsRefusedOnceAndChangesNothing()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet();

            List<string> models_Before = Models(partOEquipmentAssignmentSet);

            Assert.False(partOEquipmentAssignmentSet.Assign(
                [DwellingGuid("Flat 1"), DwellingGuid("Flat 2"), DwellingGuid("Flat 3")],
                new VentilationUnitReference(null, null, null),
                out List<Guid> guids_Assigned,
                out List<string> refusals));

            Assert.Empty(guids_Assigned);
            Assert.Single(refusals);

            Assert.Equal(models_Before, Models(partOEquipmentAssignmentSet));
        }

        // =================================================================================================
        // 2. Select all, and every dwelling still judged on its own duty
        // =================================================================================================

        /// <summary>
        /// Every dwelling selected and the MRXBOX applied: every <b>identity</b> updates, and the derived
        /// columns are recalculated <b>per dwelling</b> from that dwelling's own duty - so the headroom of a
        /// 23 l/s flat and a 63 l/s flat differ even though they hold the same box.
        /// </summary>
        [Fact]
        public void SelectAll_UpdatesEveryIdentity_AndRecalculatesEachDwellingIndependently()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet(XBC15Reference());

            Assert.True(partOEquipmentAssignmentSet.Assign(
                Guids(partOEquipmentAssignmentSet),
                MRXBOXReference(),
                out List<Guid> _,
                out List<string> refusals));

            Assert.Empty(refusals);

            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal(model_MRXBOX, x.VentilationUnitReference.Model));

            //One capability, three different answers about three different dwellings.
            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal(150, x.MaximumSupply_Lps));

            Assert.Equal(127, Assignment(partOEquipmentAssignmentSet, "Flat 1").SupplyHeadroom_Lps);
            Assert.Equal(87, Assignment(partOEquipmentAssignmentSet, "Flat 2").SupplyHeadroom_Lps);
            Assert.Equal(87, Assignment(partOEquipmentAssignmentSet, "Flat 3").SupplyHeadroom_Lps);
        }

        // =================================================================================================
        // 3. An insufficient assignment is held and reported, never accommodated
        // =================================================================================================

        /// <summary>
        /// <b>The most important test in this file.</b> One product applied to a selection where it does not
        /// fit one of the dwellings: that dwelling <b>keeps</b> the insufficient product, is reported
        /// INSUFFICIENT, has its design airflow left exactly where it was, and is <b>not</b> quietly given
        /// the larger box that would have fitted.
        /// </summary>
        [Fact]
        public void AnInsufficientProductStaysAssigned_IsReported_AndNothingIsSubstitutedOrReduced()
        {
            //Flat 3 needs 160/160 - beyond the MRXBOX's 150/150 and within the XBC15's 190/190.
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [
                    Assignment("Flat 1", 23, 23, XBC15Reference()),
                    Assignment("Flat 2", 63, 63, XBC15Reference()),
                    Assignment("Flat 3", 160, 160, XBC15Reference()),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            List<string> duties_Before = Duties(partOEquipmentAssignmentSet);

            Assert.True(partOEquipmentAssignmentSet.Assign(
                Guids(partOEquipmentAssignmentSet),
                MRXBOXReference(),
                out List<Guid> _,
                out List<string> refusals));

            //Accepted, not refused - an authored assignment is the engineer's decision and is reported.
            Assert.Empty(refusals);

            PartOEquipmentAssignment partOEquipmentAssignment = Assignment(partOEquipmentAssignmentSet, "Flat 3");

            //It is still the MRXBOX. Not the XBC15.
            Assert.Equal(model_MRXBOX, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.Equal(PartOEquipmentAssignmentStatus.Insufficient, partOEquipmentAssignment.Status);

            //Reported: negative headroom on both sides, and a suggestion offered as a VALUE.
            Assert.Equal(-10, partOEquipmentAssignment.SupplyHeadroom_Lps);
            Assert.Equal(-10, partOEquipmentAssignment.ExtractHeadroom_Lps);
            Assert.Equal(model_XBC15, partOEquipmentAssignment.Suggestion?.VentilationUnitReference.Model);

            //A suggestion is not a mutation: the assignment is still the insufficient one.
            Assert.Equal(model_MRXBOX, partOEquipmentAssignment.VentilationUnitReference.Model);

            //And no design duty moved anywhere in the table.
            Assert.Equal(duties_Before, Duties(partOEquipmentAssignmentSet));

            //The other two are fine, from the same one product.
            Assert.Equal(PartOEquipmentAssignmentStatus.Ok, Assignment(partOEquipmentAssignmentSet, "Flat 1").Status);
            Assert.Equal(PartOEquipmentAssignmentStatus.Ok, Assignment(partOEquipmentAssignmentSet, "Flat 2").Status);
        }

        /// <summary>
        /// A deliberately over-sized product applied in bulk is accepted as-is and is never reduced to the
        /// smallest capable one. Headroom is headroom.
        /// </summary>
        [Fact]
        public void ADeliberatelyOversizedProduct_IsAcceptedAndNeverReduced()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet();

            Assert.True(partOEquipmentAssignmentSet.Assign(Guids(partOEquipmentAssignmentSet), XBC15Reference(), out List<Guid> _, out List<string> _));

            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal(model_XBC15, x.VentilationUnitReference.Model));
            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal(PartOEquipmentAssignmentStatus.Ok, x.Status));

            Assert.Equal(167, Assignment(partOEquipmentAssignmentSet, "Flat 1").SupplyHeadroom_Lps);
        }

        // =================================================================================================
        // 4. and 5. Cancel writes nothing; Commit writes only what was intended
        // =================================================================================================

        /// <summary>
        /// A bulk assignment that is never committed leaves the model exactly as it was. This is the whole
        /// of Cancel: the set is pure, and declining is not reaching Commit.
        /// </summary>
        [Fact]
        public void ABulkAssignmentThatIsNeverCommitted_LeavesTheModelUnchanged()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out List<AirHandlingUnit> airHandlingUnits);

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = Set(adjacencyCluster, airHandlingUnits);

            Assert.True(partOEquipmentAssignmentSet.Assign(Guids(partOEquipmentAssignmentSet), XBC15Reference(), out List<Guid> _, out List<string> _));

            Assert.True(partOEquipmentAssignmentSet.HasChanges);

            //The table says XBC15 - and every unit in the model still says MRXBOX.
            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal(model_XBC15, x.VentilationUnitReference.Model));

            Assert.All(
                adjacencyCluster.GetObjects<AirHandlingUnit>(),
                x => Assert.Equal(model_MRXBOX, Analytical.Query.SelectedVentilationUnitReference(x)?.Model));
        }

        /// <summary>
        /// Commit writes the rows a bulk assignment changed, and <b>only</b> those - the dwelling that was
        /// not selected is not rewritten with the value it already had.
        /// </summary>
        [Fact]
        public void Commit_WritesOnlyTheDwellingsTheBulkAssignmentChanged()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out List<AirHandlingUnit> airHandlingUnits);

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = Set(adjacencyCluster, airHandlingUnits);

            //Two of the three.
            Assert.True(partOEquipmentAssignmentSet.Assign(
                [airHandlingUnits[0].Guid, airHandlingUnits[1].Guid],
                XBC15Reference(),
                out List<Guid> _,
                out List<string> _));

            Assert.True(partOEquipmentAssignmentSet.Commit(adjacencyCluster, out List<string> notes, out List<string> refusals));

            Assert.Empty(refusals);

            //One note per row actually written, and not one per row in the table.
            Assert.Equal(2, notes.Count);

            Assert.Equal(model_XBC15, Analytical.Query.SelectedVentilationUnitReference(adjacencyCluster.GetObject<AirHandlingUnit>(airHandlingUnits[0].Guid))?.Model);
            Assert.Equal(model_XBC15, Analytical.Query.SelectedVentilationUnitReference(adjacencyCluster.GetObject<AirHandlingUnit>(airHandlingUnits[1].Guid))?.Model);
            Assert.Equal(model_MRXBOX, Analytical.Query.SelectedVentilationUnitReference(adjacencyCluster.GetObject<AirHandlingUnit>(airHandlingUnits[2].Guid))?.Model);
        }

        // =================================================================================================
        // 6. No airflow of any kind moves
        // =================================================================================================

        /// <summary>
        /// <b>The airflow-authority assertion.</b> Every dwelling's design duty is identical before and
        /// after a bulk equipment edit - and it has to be, because these are four different quantities that
        /// happen to be measured in the same units.
        /// </summary>
        [Fact]
        public void ABulkAssignment_MovesNoAirflowOfAnyKind()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet();

            List<string> duties_Before = Duties(partOEquipmentAssignmentSet);

            Assert.True(partOEquipmentAssignmentSet.Assign(Guids(partOEquipmentAssignmentSet), XBC15Reference(), out List<Guid> _, out List<string> _));
            Assert.True(partOEquipmentAssignmentSet.Assign(Guids(partOEquipmentAssignmentSet), MRXBOXReference(), out List<Guid> _, out List<string> _));

            Assert.Equal(duties_Before, Duties(partOEquipmentAssignmentSet));
        }

        // =================================================================================================
        // 7. Scaling
        // =================================================================================================

        /// <summary>
        /// Two thousand dwellings against two hundred products: a bulk assignment over every row is one
        /// pass, and no model, zone, space or catalogue rescan happens per row.
        /// <para>
        /// The generous wall-clock bound is a smoke test for an accidental quadratic, not a benchmark - it
        /// is what catches a <c>List.Find</c> per row, or a whole-table refresh per assigned row, which is
        /// what this operation used to be built out of.
        /// </para>
        /// </summary>
        [Fact]
        public void TwoThousandDwellings_AssignedInOneOperation_StayLinear()
        {
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = [];

            for (int i = 0; i < 200; i++)
            {
                ventilationUnitCapacityDescriptors.Add(new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", string.Format("Product {0}", i), null), 100 + i, 100 + i, i));
            }

            List<PartOEquipmentAssignment> assignments = [];

            for (int i = 0; i < 2000; i++)
            {
                assignments.Add(Assignment(string.Format("Flat {0}", i), 30, 30, ventilationUnitCapacityDescriptors[0].VentilationUnitReference));
            }

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(assignments, ventilationUnitCapacityDescriptors, new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            List<Guid> guids = Guids(partOEquipmentAssignmentSet);

            Stopwatch stopwatch = Stopwatch.StartNew();

            Assert.True(partOEquipmentAssignmentSet.Assign(guids, ventilationUnitCapacityDescriptors[199].VentilationUnitReference, out List<Guid> guids_Assigned, out List<string> refusals));

            stopwatch.Stop();

            Assert.Empty(refusals);
            Assert.Equal(2000, guids_Assigned.Count);

            Assert.True(stopwatch.ElapsedMilliseconds < 10000, string.Format("A bulk assignment over 2000 dwellings took {0} ms.", stopwatch.ElapsedMilliseconds));

            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal("Product 199", x.VentilationUnitReference.Model));
        }

        /// <summary>
        /// Assigning a handful of rows out of a large table costs about what assigning a handful costs out
        /// of a small one: the rows that were not selected are not visited at all.
        /// </summary>
        [Fact]
        public void AssigningAFewRowsOutOfALargeTable_DoesNotVisitTheRest()
        {
            List<PartOEquipmentAssignment> assignments = [];

            for (int i = 0; i < 5000; i++)
            {
                assignments.Add(Assignment(string.Format("Flat {0}", i), 30, 30, MRXBOXReference()));
            }

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(assignments, Descriptors(), new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            Stopwatch stopwatch = Stopwatch.StartNew();

            Assert.True(partOEquipmentAssignmentSet.Assign(
                [DwellingGuid("Flat 0"), DwellingGuid("Flat 1"), DwellingGuid("Flat 2")],
                XBC15Reference(),
                out List<Guid> guids_Assigned,
                out List<string> _));

            stopwatch.Stop();

            Assert.Equal(3, guids_Assigned.Count);

            Assert.True(stopwatch.ElapsedMilliseconds < 2000, string.Format("Assigning 3 of 5000 dwellings took {0} ms.", stopwatch.ElapsedMilliseconds));

            //And the 4997 that were not selected still say what they said.
            Assert.Equal(model_MRXBOX, Assignment(partOEquipmentAssignmentSet, "Flat 3").VentilationUnitReference.Model);
            Assert.Equal(model_MRXBOX, Assignment(partOEquipmentAssignmentSet, "Flat 4999").VentilationUnitReference.Model);
        }

        // =================================================================================================
        // 8. The bulk product list is the project's permitted set
        // =================================================================================================

        /// <summary>
        /// The products a bulk assignment offers are the project's permitted set - and <b>not</b> the union
        /// of every dwelling's own out-of-pool product. An outsider belongs to the one dwelling that holds
        /// it, and offering a selection's historical outsiders as a project-wide choice would offer products
        /// the project does not permit.
        /// </summary>
        [Fact]
        public void TheBulkProductList_IsThePermittedSetAndNeverACollectionOfOutsiders()
        {
            //Flat 1 holds the XBC15; the project permits only the MRXBOX.
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [
                    Assignment("Flat 1", 23, 23, XBC15Reference()),
                    Assignment("Flat 2", 63, 63, MRXBOXReference()),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [MRXBOXReference()]));

            //That one dwelling's picker still offers its own outsider, so a pool change cannot make an
            //authored assignment unpickable.
            Assert.Equal(2, partOEquipmentAssignmentSet.Candidates(DwellingGuid("Flat 1")).Count);

            //The bulk list does not.
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = partOEquipmentAssignmentSet.AllowedCandidates;

            VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = Assert.Single(ventilationUnitCapacityDescriptors);

            Assert.Equal(model_MRXBOX, ventilationUnitCapacityDescriptor.VentilationUnitReference.Model);
        }

        /// <summary>
        /// And the bulk list is a copy: a caller that mutates what it was handed cannot narrow or widen the
        /// project's permitted set behind its back.
        /// </summary>
        [Fact]
        public void TheBulkProductList_IsACopy()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet();

            partOEquipmentAssignmentSet.AllowedCandidates.Clear();

            Assert.Equal(2, partOEquipmentAssignmentSet.AllowedCandidates.Count);
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
        /// The accepted fixture's shape under MANUAL authority: three dwellings at 23/23, 63/63 and 63/63
        /// l/s, each already holding a product.
        /// </summary>
        private static PartOEquipmentAssignmentSet ManualSet(VentilationUnitReference ventilationUnitReference = null)
        {
            ventilationUnitReference ??= MRXBOXReference();

            return new PartOEquipmentAssignmentSet(
                [
                    Assignment("Flat 1", 23, 23, ventilationUnitReference),
                    Assignment("Flat 2", 63, 63, ventilationUnitReference),
                    Assignment("Flat 3", 63, 63, ventilationUnitReference),
                ],
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));
        }

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

        /// <summary>Every dwelling in the table - what "select all" names.</summary>
        private static List<Guid> Guids(PartOEquipmentAssignmentSet partOEquipmentAssignmentSet)
        {
            return partOEquipmentAssignmentSet.Assignments.ConvertAll(x => x.Guid_AirHandlingUnit);
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
        /// Three air handling units in a real cluster, each already carrying the MRXBOX, for the tests that
        /// commit. No systems and no terminals: what commit does is write an identity.
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

        /// <summary>A manual-authority table over a real cluster, built the way the preparation builds it.</summary>
        private static PartOEquipmentAssignmentSet Set(AdjacencyCluster adjacencyCluster, List<AirHandlingUnit> airHandlingUnits)
        {
            Dictionary<Guid, string> dictionary_Name = [];

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                dictionary_Name[airHandlingUnit.Guid] = airHandlingUnit.Name;
            }

            return PartOEquipmentAssignmentSet.Create(
                adjacencyCluster,
                airHandlingUnits,
                dictionary_Name,
                dictionary_Name,
                Descriptors(),
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));
        }
    }
}
