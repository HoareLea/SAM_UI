// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// What the equipment assignment table costs on a real project rather than on the three-dwelling
    /// acceptance fixture.
    ///
    /// <para><b>The property, stated structurally</b></para>
    /// <para>
    /// <c>PartOEquipmentAssignmentSet</c> reads the model <b>once</b>, when it is built, and never again.
    /// Every later operation - converting authority, editing a dwelling, narrowing the pool, asking what a
    /// picker should offer, calculating a suggestion - works on captured numbers and a catalogue index. That
    /// is asserted the strongest way available: the model is <b>emptied</b> after the table is built, and
    /// every operation still answers correctly. Anything that reached back for a design duty, a space or a
    /// zone would fail immediately rather than merely getting slower.
    /// </para>
    ///
    /// <para><b>And once as a quadratic tripwire</b></para>
    /// <para>
    /// The bound below is deliberately enormous relative to the real cost - the expected work is a few
    /// hundred thousand dictionary probes, which is milliseconds. It is not a performance target and it
    /// will not flake on a loaded machine; it exists so that a change which re-derived duties per row, or
    /// rescanned the catalogue per dwelling, or resolved each dwelling against every other, fails here
    /// instead of being discovered on a real project. The same reasoning, and the same generosity, as
    /// <c>SAM.Tests.ShellIsClosedTests</c>' own bounded-runtime assertion.
    /// </para>
    /// </summary>
    public class PartOEquipmentAssignmentScalingTests
    {
        /// <summary>Enough dwellings that a per-dwelling model rescan would be obvious.</summary>
        private const int count_Dwelling = 2000;

        /// <summary>A catalogue far larger than anything shipped today - tens to hundreds is the brief.</summary>
        private const int count_Product = 200;

        /// <summary>
        /// <b>The table reads the model once.</b> Built over a real cluster, then the cluster is emptied -
        /// and converting to manual, narrowing the pool, asking for candidates, reading a suggestion and
        /// editing a dwelling all still answer correctly, because none of them touches a model.
        /// </summary>
        [Fact]
        public void TheTable_ReadsTheModelOnceAndNeverAgain()
        {
            AdjacencyCluster adjacencyCluster = new();

            List<AirHandlingUnit> airHandlingUnits = [];
            Dictionary<Guid, string> dictionary_DwellingName = [];

            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = Descriptors(2);

            for (int i = 0; i < 3; i++)
            {
                AirHandlingUnit airHandlingUnit = Analytical.Create.AirHandlingUnit(string.Format("MVHR-{0:00}", i));

                airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, ventilationUnitCapacityDescriptors[0].VentilationUnitReference);

                adjacencyCluster.AddObject(airHandlingUnit);

                airHandlingUnits.Add(airHandlingUnit);
                dictionary_DwellingName[airHandlingUnit.Guid] = string.Format("Flat {0}", i);
            }

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = PartOEquipmentAssignmentSet.Create(
                adjacencyCluster,
                airHandlingUnits,
                [],
                dictionary_DwellingName,
                ventilationUnitCapacityDescriptors,
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts));

            Assert.Equal(3, partOEquipmentAssignmentSet.Assignments.Count);

            //THE MODEL GOES AWAY. Everything below has to work without it.
            foreach (AirHandlingUnit airHandlingUnit in adjacencyCluster.GetObjects<AirHandlingUnit>())
            {
                adjacencyCluster.RemoveObject<AirHandlingUnit>(airHandlingUnit.Guid);
            }

            Assert.Empty(adjacencyCluster.GetObjects<AirHandlingUnit>() ?? []);

            Guid guid = airHandlingUnits[0].Guid;

            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.True(partOEquipmentAssignmentSet.IsManual);
            Assert.Equal(3, partOEquipmentAssignmentSet.Assignments.Count);

            //Capabilities still resolve - out of the catalogue index, which is where they always came from.
            Assert.Equal(150, partOEquipmentAssignmentSet.Assignments[0].MaximumSupply_Lps);

            //A picker still knows what to offer.
            Assert.Equal(2, partOEquipmentAssignmentSet.Candidates(guid).Count);

            //A pool change still flags rather than reassigns.
            partOEquipmentAssignmentSet.SetAllowedVentilationUnitReferences([ventilationUnitCapacityDescriptors[1].VentilationUnitReference]);

            Assert.True(partOEquipmentAssignmentSet.Assignments[0].IsOutsideAllowedPool);
            Assert.Equal(150, partOEquipmentAssignmentSet.Assignments[0].MaximumSupply_Lps);

            //And an edit still lands on the row.
            Assert.True(partOEquipmentAssignmentSet.Assign(guid, ventilationUnitCapacityDescriptors[1].VentilationUnitReference, out _));
            Assert.Equal(190, partOEquipmentAssignmentSet.Assignments[0].MaximumSupply_Lps);

            //The one operation that IS a model write refuses cleanly against a model that no longer holds
            //the unit, rather than writing into nothing.
            Assert.False(partOEquipmentAssignmentSet.Commit(adjacencyCluster, out _, out List<string> refusals));
            Assert.NotEmpty(refusals);
        }

        /// <summary>
        /// Two thousand dwellings against a two hundred product catalogue: built, fully re-evaluated twice
        /// over a pool change, every dwelling's candidates asked for, and a hundred dwellings edited. The
        /// answers are asserted, and the whole thing is bounded.
        /// </summary>
        [Fact]
        public void TwoThousandDwellings_AgainstTwoHundredProducts_StayLinear()
        {
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = Descriptors(count_Product);

            List<PartOEquipmentAssignment> assignments = [];

            for (int i = 0; i < count_Dwelling; i++)
            {
                //Duties spread across the catalogue's range so that the answers differ dwelling by dwelling -
                //a table where every row answered the same could be produced by a broken lookup.
                double duty_Lps = 20 + (i % 100);

                assignments.Add(new PartOEquipmentAssignment(
                    Guid.NewGuid(),
                    string.Format("MVHR-{0:0000}", i),
                    string.Format("Flat {0} MVHR", i),
                    string.Format("Flat {0}", i),
                    duty_Lps,
                    duty_Lps,
                    ventilationUnitCapacityDescriptors[0].VentilationUnitReference));
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(assignments, ventilationUnitCapacityDescriptors, new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            Assert.Equal(count_Dwelling, partOEquipmentAssignmentSet.Assignments.Count);

            //Every row resolved its own capability, and none is unknown.
            Assert.All(partOEquipmentAssignmentSet.Assignments, x => Assert.Equal(150, x.MaximumSupply_Lps));

            //A full re-evaluation over a narrowed pool, twice - the operation a tick in the catalogue grid
            //triggers, and the one a quadratic implementation would fail on.
            partOEquipmentAssignmentSet.SetAllowedVentilationUnitReferences([ventilationUnitCapacityDescriptors[1].VentilationUnitReference]);
            partOEquipmentAssignmentSet.SetAllowedVentilationUnitReferences(Descriptors(count_Product).ConvertAll(x => x.VentilationUnitReference));

            //Every dwelling's picker asked what it should offer.
            foreach (PartOEquipmentAssignment partOEquipmentAssignment in partOEquipmentAssignmentSet.Assignments)
            {
                Assert.Equal(count_Product, partOEquipmentAssignmentSet.Candidates(partOEquipmentAssignment.Guid_AirHandlingUnit).Count);
            }

            //And a hundred dwellings overridden, each of which re-evaluates its own row alone.
            List<PartOEquipmentAssignment> assignments_Edited = partOEquipmentAssignmentSet.Assignments;

            for (int i = 0; i < 100; i++)
            {
                Assert.True(partOEquipmentAssignmentSet.Assign(assignments_Edited[i].Guid_AirHandlingUnit, ventilationUnitCapacityDescriptors[1].VentilationUnitReference, out _));
            }

            stopwatch.Stop();

            Assert.Equal(190, partOEquipmentAssignmentSet.Assignments[0].MaximumSupply_Lps);
            Assert.Equal(150, partOEquipmentAssignmentSet.Assignments[count_Dwelling - 1].MaximumSupply_Lps);

            Assert.True(
                stopwatch.ElapsedMilliseconds < 10000,
                string.Format("Expected the assignment layer to stay linear in dwellings and products; {0} dwellings against {1} products took {2} ms.", count_Dwelling, count_Product, stopwatch.ElapsedMilliseconds));
        }

        /// <summary>
        /// A catalogue that gives one identity two different capacities makes that identity <b>unknown</b>
        /// rather than either of them - so a large catalogue with a duplicated line cannot make a dwelling's
        /// adequacy depend on the order the file was read in.
        /// </summary>
        [Fact]
        public void ADuplicatedIdentityWithTwoCapacities_IsUnknownRatherThanEither()
        {
            VentilationUnitReference ventilationUnitReference = new("Test", "Duplicated", null);

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [new PartOEquipmentAssignment(Guid.NewGuid(), "MVHR-01", "Flat 1 MVHR", "Flat 1", 50, 50, ventilationUnitReference)],
                [
                    new VentilationUnitCapacityDescriptor(ventilationUnitReference, 150, 150, 10),
                    new VentilationUnitCapacityDescriptor(ventilationUnitReference, 190, 190, 10),
                ],
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            PartOEquipmentAssignment partOEquipmentAssignment = partOEquipmentAssignmentSet.Assignments[0];

            Assert.Equal(PartOEquipmentAssignmentStatus.CapacityUnknown, partOEquipmentAssignment.Status);
            Assert.True(double.IsNaN(partOEquipmentAssignment.MaximumSupply_Lps));
        }

        /// <summary>
        /// An exact duplicate line answers the same question the same way and is not a conflict - a
        /// catalogue with a repeated entry still resolves.
        /// </summary>
        [Fact]
        public void AnExactlyRepeatedIdentity_StillResolves()
        {
            VentilationUnitReference ventilationUnitReference = new("Test", "Repeated", null);

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [new PartOEquipmentAssignment(Guid.NewGuid(), "MVHR-01", "Flat 1 MVHR", "Flat 1", 50, 50, ventilationUnitReference)],
                [
                    new VentilationUnitCapacityDescriptor(ventilationUnitReference, 150, 150, 10),
                    new VentilationUnitCapacityDescriptor(ventilationUnitReference, 150, 150, 10),
                ],
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            Assert.Equal(PartOEquipmentAssignmentStatus.Ok, partOEquipmentAssignmentSet.Assignments[0].Status);
            Assert.Equal(150, partOEquipmentAssignmentSet.Assignments[0].MaximumSupply_Lps);
        }

        /// <summary>
        /// Two products whose manufacturer and model would run together into the same text are kept apart -
        /// "Nuaire Group" / "X" is not "Nuaire" / "Group X". A catalogue index that joined the identity
        /// fields with a space would give them one capacity between them.
        /// </summary>
        [Fact]
        public void TwoIdentitiesThatWouldRunTogether_KeepTheirOwnCapacities()
        {
            VentilationUnitReference ventilationUnitReference_1 = new("Nuaire Group", "X", null);
            VentilationUnitReference ventilationUnitReference_2 = new("Nuaire", "Group X", null);

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = new(
                [
                    new PartOEquipmentAssignment(Guid.NewGuid(), "MVHR-01", "Flat 1 MVHR", "Flat 1", 50, 50, ventilationUnitReference_1),
                    new PartOEquipmentAssignment(Guid.NewGuid(), "MVHR-02", "Flat 2 MVHR", "Flat 2", 50, 50, ventilationUnitReference_2),
                ],
                [
                    new VentilationUnitCapacityDescriptor(ventilationUnitReference_1, 150, 150, 10),
                    new VentilationUnitCapacityDescriptor(ventilationUnitReference_2, 190, 190, 20),
                ],
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            Assert.Equal(150, partOEquipmentAssignmentSet.Assignments[0].MaximumSupply_Lps);
            Assert.Equal(190, partOEquipmentAssignmentSet.Assignments[1].MaximumSupply_Lps);
        }

        /// <summary>
        /// A catalogue of <paramref name="count"/> products, the first two at the shipped 150 and 190 l/s so
        /// that the assertions above read in the capacities the acceptance walk uses.
        /// </summary>
        private static List<VentilationUnitCapacityDescriptor> Descriptors(int count)
        {
            List<VentilationUnitCapacityDescriptor> result =
            [
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Nuaire", "MRXBOXAB-ECO5-AECV", "MR-ECO-COOL-V"), 150, 150, 10),
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Nuaire", "XBC15", null), 190, 190, 20),
            ];

            for (int i = result.Count; i < count; i++)
            {
                //Above the real ladder, so a filler product can never become the answer to a duty the two
                //shipped products should have answered.
                result.Add(new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", string.Format("MVHR-{0:0000}", i), null), 200 + i, 200 + i, 100 + i));
            }

            return result;
        }
    }
}
