// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The Iteration 3 work is the same shape at nine rooms and at five thousand.</b>
    ///
    /// <para><b>How that is asserted, and why not with a clock</b></para>
    /// <para>
    /// Two ways, neither of which depends on how fast the machine is.
    /// </para>
    /// <list type="number">
    /// <item><b>A counting dictionary.</b> The comparison joins its two series by guid; handing it a
    /// dictionary that counts its own lookups proves the join is <b>exactly two lookups per room</b>
    /// whatever the room count. A scan would grow with the room count and this would see it - it is a
    /// structural proof, not a measurement.</item>
    /// <item><b>Allocation ratios across doublings</b>, this repository's established scaling evidence:
    /// linear work sits near 2 when the model doubles and quadratic work near 4. Bytes allocated are the
    /// same on every machine; milliseconds are not.</item>
    /// </list>
    /// <para>
    /// The wall clock appears once, in a benchmark that prints numbers and asserts nothing.
    /// </para>
    /// </summary>
    public class PartOIteration3ScalingTests
    {
        private readonly ITestOutputHelper _output;

        public PartOIteration3ScalingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        //-------------------------------------------------------------------------------------------------
        //1. The join, proved structurally
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A dictionary that counts the lookups made against it. Everything else is delegated, so what is
        /// measured is the caller's behaviour and not this type's.
        /// </summary>
        private class CountingDictionary : IDictionary<Guid, double[]>
        {
            private readonly Dictionary<Guid, double[]> dictionary = [];

            internal int Count_TryGetValue { get; private set; }

            internal int Count_Enumerated { get; private set; }

            public bool TryGetValue(Guid key, out double[] value)
            {
                Count_TryGetValue++;

                return dictionary.TryGetValue(key, out value);
            }

            public IEnumerator<KeyValuePair<Guid, double[]>> GetEnumerator()
            {
                Count_Enumerated++;

                return dictionary.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                Count_Enumerated++;

                return dictionary.GetEnumerator();
            }

            public double[] this[Guid key] { get => dictionary[key]; set => dictionary[key] = value; }

            public ICollection<Guid> Keys => dictionary.Keys;

            public ICollection<double[]> Values => dictionary.Values;

            public int Count => dictionary.Count;

            public bool IsReadOnly => false;

            public void Add(Guid key, double[] value) => dictionary.Add(key, value);

            public void Add(KeyValuePair<Guid, double[]> item) => dictionary.Add(item.Key, item.Value);

            public void Clear() => dictionary.Clear();

            public bool Contains(KeyValuePair<Guid, double[]> item) => dictionary.ContainsKey(item.Key);

            public bool ContainsKey(Guid key) => dictionary.ContainsKey(key);

            public void CopyTo(KeyValuePair<Guid, double[]>[] array, int arrayIndex) => throw new NotSupportedException();

            public bool Remove(Guid key) => dictionary.Remove(key);

            public bool Remove(KeyValuePair<Guid, double[]> item) => dictionary.Remove(item.Key);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(1000)]
        [InlineData(5000)]
        public void The_comparison_joins_its_two_series_with_exactly_one_lookup_per_room_per_side(int count)
        {
            List<PartOIteration3Room> rooms = [];

            CountingDictionary series_A = new();
            CountingDictionary series_B = new();

            for (int i = 0; i < count; i++)
            {
                Guid guid = Guid.NewGuid();

                //Every room named the same, deliberately: a join that fell back to a name would collapse
                //five thousand rooms into one.
                rooms.Add(new PartOIteration3Room(guid, "Bedroom 2", Guid.Empty, "Flat"));

                series_A[guid] = [20.0, 21.0];
                series_B[guid] = [21.0, 22.0];
            }

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(rooms, series_A, series_B, [], out List<string> refusals);

            Assert.Empty(refusals);
            Assert.Equal(count, partOIteration3Comparison.Rooms.Count);

            //Exactly one lookup per room per side, and no enumeration of either dictionary at all.
            Assert.Equal(count, series_A.Count_TryGetValue);
            Assert.Equal(count, series_B.Count_TryGetValue);
            Assert.Equal(0, series_A.Count_Enumerated);
            Assert.Equal(0, series_B.Count_Enumerated);
        }

        //-------------------------------------------------------------------------------------------------
        //2. Allocation across doublings
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A block of <paramref name="count"/> dwellings, one room each, each with its own MVHR system -
        /// so the system count grows with the room count, which is the shape that makes a per-system walk
        /// of the model quadratic.
        /// </summary>
        private static AdjacencyCluster Block(int count, out List<Guid> guids_VentilationSystem, out List<Zone> zones, out List<Guid> guids_Space)
        {
            AdjacencyCluster result = new();

            guids_VentilationSystem = [];
            zones = [];
            guids_Space = [];

            for (int i = 0; i < count; i++)
            {
                VentilationSystem ventilationSystem = PartOIteration3Fixture.VentilationSystem(result, string.Format("MVHR-{0:00000}", i), "MVHR", out AirHandlingUnit _);

                //Every room named identically. Anything matching by name would see one room.
                Space space = PartOIteration3Fixture.Space(result, "Bedroom 2");

                PartOIteration3Fixture.Terminal(result, ventilationSystem, space, FlowClassification.Supply, 13.0);

                result.AddRelation(ventilationSystem, space);

                zones.Add(PartOIteration3Fixture.Zone(result, string.Format("Flat {0:00000}", i), space));

                guids_VentilationSystem.Add(ventilationSystem.Guid);
                guids_Space.Add(space.Guid);
            }

            //And one authored system the iteration did not build, carrying no duty - the SAM #114 shape.
            PartOIteration3Fixture.VentilationSystem_Unnamed(result, "NV");

            return result;
        }

        private static long Allocated(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();

            action();

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void Scoping_the_systems_and_indexing_the_rooms_grow_linearly_with_the_block()
        {
            int[] counts = [625, 1250, 2500, 5000];

            long[] allocated_Scope = new long[counts.Length];
            long[] allocated_Rooms = new long[counts.Length];

            for (int i = 0; i < counts.Length; i++)
            {
                AdjacencyCluster adjacencyCluster = Block(counts[i], out List<Guid> guids_VentilationSystem, out List<Zone> zones, out List<Guid> guids_Space);

                allocated_Rooms[i] = Allocated(() => Query.PartOIteration3Rooms(adjacencyCluster, zones));
                allocated_Scope[i] = Allocated(() => Query.PartOIteration3SystemScope(adjacencyCluster, guids_VentilationSystem, guids_Space));
            }

            for (int i = 0; i < counts.Length; i++)
            {
                _output.WriteLine("dwellings={0,5}  system scope={1,14:N0} bytes  room index={2,14:N0} bytes", counts[i], allocated_Scope[i], allocated_Rooms[i]);
            }

            for (int i = 1; i < counts.Length; i++)
            {
                double ratio_Scope = (double)allocated_Scope[i] / allocated_Scope[i - 1];
                double ratio_Rooms = (double)allocated_Rooms[i] / allocated_Rooms[i - 1];

                _output.WriteLine("{0} -> {1}: system scope x{2:0.00}, room index x{3:0.00}", counts[i - 1], counts[i], ratio_Scope, ratio_Rooms);

                Assert.True(
                    ratio_Scope < 2.6,
                    string.Format("Doubling the block from {0} to {1} dwellings multiplied the system scope's allocation by {2:0.00}. Linear work sits near 2 and quadratic work near 4, so something is walking the model per system again.", counts[i - 1], counts[i], ratio_Scope));

                Assert.True(
                    ratio_Rooms < 2.6,
                    string.Format("Doubling the block from {0} to {1} dwellings multiplied the room index's allocation by {2:0.00}.", counts[i - 1], counts[i], ratio_Rooms));
            }
        }

        /// <summary>
        /// The grid rows, at the size a real block reaches. Three criteria per room is the ordinary Part O
        /// case, so five thousand rooms is fifteen thousand rows, and the join from a criterion to its
        /// room's statistics has to be a lookup rather than a scan.
        /// </summary>
        [Fact]
        public void Building_the_grid_rows_grows_linearly_with_the_rooms()
        {
            int[] counts = [625, 1250, 2500, 5000];

            long[] allocated = new long[counts.Length];

            for (int i = 0; i < counts.Length; i++)
            {
                PartOIteration3Comparison partOIteration3Comparison = Comparison(counts[i]);

                allocated[i] = Allocated(() => PartOIteration3Row.Rows(partOIteration3Comparison));
            }

            for (int i = 0; i < counts.Length; i++)
            {
                _output.WriteLine("rooms={0,5}  rows={1,14:N0} bytes", counts[i], allocated[i]);
            }

            for (int i = 1; i < counts.Length; i++)
            {
                double ratio = (double)allocated[i] / allocated[i - 1];

                Assert.True(
                    ratio < 2.6,
                    string.Format("Doubling the block from {0} to {1} rooms multiplied the row build's allocation by {2:0.00}.", counts[i - 1], counts[i], ratio));
            }
        }

        private static PartOIteration3Comparison Comparison(int count)
        {
            List<PartOIteration3Room> rooms = [];
            List<PartOIteration3CriterionComparison> criteria = [];

            Dictionary<Guid, double[]> series_A = [];
            Dictionary<Guid, double[]> series_B = [];

            for (int i = 0; i < count; i++)
            {
                Guid guid = Guid.NewGuid();
                Guid guid_Dwelling = Guid.NewGuid();

                rooms.Add(new PartOIteration3Room(guid, "Bedroom 2", guid_Dwelling, "Flat"));

                for (int j = 0; j < 3; j++)
                {
                    criteria.Add(new PartOIteration3CriterionComparison(guid, "Bedroom 2", guid_Dwelling, "Flat", string.Format("TM59 Criterion {0}", j), true, 1, 2, TM59ComplianceStatus.Pass, 1, 2, TM59ComplianceStatus.Pass));
                }

                series_A[guid] = [20.0, 21.0];
                series_B[guid] = [21.0, 22.0];
            }

            return PartOIteration3Comparison.Create(rooms, series_A, series_B, criteria, out List<string> _);
        }

        /// <summary>Local wall clock at the sizes real projects reach, for the report. Asserts nothing.</summary>
        [Fact]
        [Trait("Category", "Benchmark")]
        public void Benchmark()
        {
            _output.WriteLine("{0,6} {1,16} {2,14} {3,12}", "rooms", "system scope (ms)", "rooms (ms)", "rows (ms)");

            foreach (int count in new[] { 100, 1000, 5000 })
            {
                AdjacencyCluster adjacencyCluster = Block(count, out List<Guid> guids_VentilationSystem, out List<Zone> zones, out List<Guid> guids_Space);

                Stopwatch stopwatch = Stopwatch.StartNew();
                Query.PartOIteration3SystemScope(adjacencyCluster, guids_VentilationSystem, guids_Space);
                long scope = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                Query.PartOIteration3Rooms(adjacencyCluster, zones);
                long index = stopwatch.ElapsedMilliseconds;

                PartOIteration3Comparison partOIteration3Comparison = Comparison(count);

                stopwatch.Restart();
                PartOIteration3Row.Rows(partOIteration3Comparison);
                long rows = stopwatch.ElapsedMilliseconds;

                _output.WriteLine("{0,6} {1,16} {2,14} {3,12}", count, scope, index, rows);
            }
        }
    }
}
