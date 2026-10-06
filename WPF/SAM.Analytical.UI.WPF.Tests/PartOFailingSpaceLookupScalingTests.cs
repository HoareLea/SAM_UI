// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// How an Iteration 2B round finds the rooms it is about to act on - and that finding them no longer
    /// walks the whole model once per room.
    ///
    /// <para><b>Three sites, one shape of defect</b></para>
    /// <list type="number">
    /// <item><c>Query.PartOOptimisationTargets</c> resolved each failing room with
    /// <c>(adjacencyCluster.GetSpaces() ?? []).Find(...)</c> <b>inside</b> the loop over the failures.
    /// <c>GetSpaces()</c> rebuilds the model's whole space list on every call, so a round that failed widely
    /// on a block-scale model rebuilt it once per failure.</item>
    /// <item><c>Query.PartODwellingSpaceGuids</c> resolved each requested dwelling zone with a linear
    /// <c>Find</c> over the model's whole zone list - quadratic on a block, where the dwelling count grows
    /// with the room count.</item>
    /// <item><c>Modify.PartialAssessment</c> named each unassessed in-scope room with a linear <c>Find</c>
    /// over the model's space list.</item>
    /// </list>
    ///
    /// <para><b>What replaced them, and why it is not a snapshot</b></para>
    /// <para>
    /// <c>AdjacencyCluster.GetObject&lt;Space&gt;(guid)</c> and <c>GetObject&lt;Zone&gt;(guid)</c> - the
    /// cluster's own O(1) authority, read live on every call. Nothing is indexed ahead of time, so there is
    /// no snapshot to go stale against the model being optimised, and no parallel lookup system beside the
    /// cluster's own.
    /// </para>
    ///
    /// <para><b>What these assert</b></para>
    /// <para>
    /// Section 1 proves the two resolutions are the same answer - by reference, over every object of a model
    /// and over the edges that are not one. Section 2 measures the work. Nothing here has a time limit;
    /// <see cref="Benchmark"/> prints numbers and asserts nothing.
    /// </para>
    /// <para>
    /// What the round <i>decides</i> with these rooms is unchanged and is pinned in
    /// <c>PartOOptimisationTests</c>. This file is about how it finds them.
    /// </para>
    /// </summary>
    public class PartOFailingSpaceLookupScalingTests
    {
        private readonly ITestOutputHelper _output;

        public PartOFailingSpaceLookupScalingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // -------------------------------------------------------------------------------------------------
        // 1. The O(1) authority is the linear search, exactly
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// For every space and every zone of the model, the O(1) authority returns the <b>same object</b> the
        /// linear search returns.
        /// </summary>
        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(5000)]
        public void TheO1Authority_ReturnsTheSameInstancesTheLinearSearchesReturn(int rooms)
        {
            AnalyticalModel analyticalModel = Model(rooms, out List<Zone> _);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            foreach (Space space in adjacencyCluster.GetSpaces())
            {
                Assert.Same(Space_Oracle(adjacencyCluster, space.Guid), adjacencyCluster.GetObject<Space>(space.Guid));
            }

            foreach (Zone zone in adjacencyCluster.GetZones())
            {
                Assert.Same(Zone_Oracle(adjacencyCluster, zone.Guid), adjacencyCluster.GetObject<Zone>(zone.Guid));
            }
        }

        /// <summary>
        /// And both answer null for exactly what the linear searches answer null for - an unknown guid, the
        /// empty guid, and a guid belonging to an object of the other type.
        /// <para>
        /// The cross-type case is the one worth stating: a zone's guid resolving as a space would make a
        /// failing room look present when it is not, and a space's guid resolving as a zone would put rooms
        /// into a dwelling scope that never contained them.
        /// </para>
        /// </summary>
        [Fact]
        public void TheO1Authority_AnswersNullForExactlyWhatTheLinearSearchesAnswerNullFor()
        {
            AnalyticalModel analyticalModel = Model(50, out List<Zone> zones);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Space space = adjacencyCluster.GetSpaces()[0];

            foreach (Guid guid in new[] { Guid.NewGuid(), Guid.Empty, zones[0].Guid })
            {
                Assert.Null(Space_Oracle(adjacencyCluster, guid));
                Assert.Null(adjacencyCluster.GetObject<Space>(guid));
            }

            foreach (Guid guid in new[] { Guid.NewGuid(), Guid.Empty, space.Guid })
            {
                Assert.Null(Zone_Oracle(adjacencyCluster, guid));
                Assert.Null(adjacencyCluster.GetObject<Zone>(guid));
            }
        }

        /// <summary>
        /// The target selection itself: every failing room in scope is targeted, a failing room the model no
        /// longer holds is reported as untargetable rather than dropped, and a failing room outside the
        /// dwelling scope is excluded by scope. All three at block scale, where the lookup used to cost.
        /// </summary>
        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        public void TheTargetSelection_IsUnchangedAtBlockScale(int rooms)
        {
            AnalyticalModel analyticalModel = Model(rooms, out List<Zone> zones);

            List<Space> spaces = analyticalModel.AdjacencyCluster.GetSpaces();

            List<PartOTM59SpaceResult> partOTM59SpaceResults = [];

            //Every room in the dwelling scope fails, plus one that belongs to no dwelling and one that is not
            //in the model at all.
            foreach (Space space in spaces)
            {
                partOTM59SpaceResults.Add(Fail(space.Guid, space.Name));
            }

            partOTM59SpaceResults.Add(Fail(Guid.NewGuid(), "A room from another model"));

            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(analyticalModel, partOTM59SpaceResults, zones, 5);

            //Every room of the scope carries a supply terminal, so every one is a target at design + step.
            Assert.Equal(rooms, partOOptimisationTargetSelection.Targets.Count);

            foreach (DesignAirFlowTarget designAirFlowTarget in partOOptimisationTargetSelection.Targets)
            {
                Assert.Equal(FlowClassification.Supply, designAirFlowTarget.FlowClassification);
                Assert.Equal(25, designAirFlowTarget.DesignFlowRate_Lps, 6);
            }

            //The corridor is in the model but in no dwelling; the invented guid is in neither.
            Assert.Equal(2, partOOptimisationTargetSelection.NotOptimisable.Count);
            Assert.Contains(partOOptimisationTargetSelection.NotOptimisable, x => x.Contains("is not in the model being optimised", StringComparison.Ordinal));
            Assert.Contains(partOOptimisationTargetSelection.NotOptimisable, x => x.Contains("outside the current Part O dwelling scope", StringComparison.Ordinal));
        }

        /// <summary>
        /// And the subset-pass guard still names the rooms it names - by the model's name where it holds one,
        /// and by guid where it does not.
        /// </summary>
        [Fact]
        public void TheSubsetPassGuard_StillNamesTheRoomsItNames()
        {
            AnalyticalModel analyticalModel = Model(40, out List<Zone> zones);

            List<Space> spaces = analyticalModel.AdjacencyCluster.GetSpaces();

            Space space_Unassessed = spaces[3];

            Guid guid_Missing = Guid.NewGuid();

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, [], null);

            //One in-scope room produced nothing; one guid the model does not hold at all, which the scope
            //filter drops before the name lookup is reached.
            PartOTM59Assessment partOTM59Assessment = new(null, null, null, null, [space_Unassessed.Guid, guid_Missing], null);

            string refusal = Modify.PartialAssessment(analyticalModel, partOPreparationContext, partOTM59Assessment);

            Assert.NotNull(refusal);
            Assert.Contains(string.Format("'{0}'", space_Unassessed.Name), refusal, StringComparison.Ordinal);
            Assert.DoesNotContain(guid_Missing.ToString(), refusal, StringComparison.Ordinal);
        }

        // -------------------------------------------------------------------------------------------------
        // 2. The work is linear in the model
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Doubling the block roughly doubles what selecting the round's targets allocates. The per-failure
        /// and per-zone resolutions it replaced roughly quadruple, and that ratio is measured beside it
        /// rather than asserted from memory.
        /// </summary>
        [Fact]
        public void SelectingTheRoundsTargets_AllocatesLinearlyWithTheBlock()
        {
            int[] counts = [400, 800, 1600];

            List<long> allocated = [];
            List<long> allocated_Oracle = [];

            foreach (int count in counts)
            {
                AnalyticalModel analyticalModel = Model(count, out List<Zone> zones);

                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

                List<PartOTM59SpaceResult> partOTM59SpaceResults = [];
                foreach (Space space in adjacencyCluster.GetSpaces())
                {
                    partOTM59SpaceResults.Add(Fail(space.Guid, space.Name));
                }

                allocated.Add(Allocated(() => Query.PartOOptimisationTargets(analyticalModel, partOTM59SpaceResults, zones, 5)));
                allocated_Oracle.Add(Allocated(() =>
                {
                    foreach (Zone zone in zones)
                    {
                        Zone_Oracle(adjacencyCluster, zone.Guid);
                    }

                    foreach (PartOTM59SpaceResult partOTM59SpaceResult in partOTM59SpaceResults)
                    {
                        Space_Oracle(adjacencyCluster, partOTM59SpaceResult.SpaceGuid_Design);
                    }
                }));
            }

            for (int i = 0; i < counts.Length; i++)
            {
                _output.WriteLine("rooms={0,5}  target selection={1,14:N0} bytes  the resolutions it replaced={2,16:N0} bytes", counts[i], allocated[i], allocated_Oracle[i]);
            }

            for (int i = 1; i < counts.Length; i++)
            {
                double ratio = (double)allocated[i] / allocated[i - 1];
                double ratio_Oracle = (double)allocated_Oracle[i] / allocated_Oracle[i - 1];

                _output.WriteLine("{0} -> {1}: target selection x{2:0.00}, the resolutions it replaced x{3:0.00}", counts[i - 1], counts[i], ratio, ratio_Oracle);

                Assert.True(
                    ratio < 2.6,
                    string.Format("Doubling the block from {0} to {1} rooms multiplied the target selection's allocation by {2:0.00}. Linear work sits near 2 and quadratic work near 4, so something is walking the whole model per failure again.", counts[i - 1], counts[i], ratio));
            }
        }

        /// <summary>
        /// Local wall clock at the sizes the real projects reach, for the report. Asserts nothing about time.
        /// </summary>
        [Fact]
        [Trait("Category", "Benchmark")]
        public void Benchmark()
        {
            _output.WriteLine("{0,6} {1,22} {2,26}", "rooms", "target selection (ms)", "the resolutions it replaced");

            foreach (int count in new[] { 100, 500, 1000, 5000 })
            {
                AnalyticalModel analyticalModel = Model(count, out List<Zone> zones);

                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

                List<PartOTM59SpaceResult> partOTM59SpaceResults = [];
                foreach (Space space in adjacencyCluster.GetSpaces())
                {
                    partOTM59SpaceResults.Add(Fail(space.Guid, space.Name));
                }

                //Warmed, so the first size measured is not paying for the JIT of every method below it.
                Query.PartOOptimisationTargets(analyticalModel, partOTM59SpaceResults, zones, 5);

                Stopwatch stopwatch = Stopwatch.StartNew();
                Query.PartOOptimisationTargets(analyticalModel, partOTM59SpaceResults, zones, 5);
                stopwatch.Stop();
                double elapsed = stopwatch.Elapsed.TotalMilliseconds;

                stopwatch.Restart();
                foreach (Zone zone in zones)
                {
                    Zone_Oracle(adjacencyCluster, zone.Guid);
                }

                foreach (PartOTM59SpaceResult partOTM59SpaceResult in partOTM59SpaceResults)
                {
                    Space_Oracle(adjacencyCluster, partOTM59SpaceResult.SpaceGuid_Design);
                }

                stopwatch.Stop();

                _output.WriteLine("{0,6} {1,22:0.0} {2,26:0.0}", count, elapsed, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        // ---- Fixture --------------------------------------------------------------------------------------

        /// <summary>The space resolution <b>exactly as it was written</b>: the whole list rebuilt, then walked.</summary>
        private static Space Space_Oracle(AdjacencyCluster adjacencyCluster, Guid guid)
        {
            return (adjacencyCluster.GetSpaces() ?? []).Find(x => x is not null && x.Guid == guid);
        }

        /// <summary>The zone resolution <b>exactly as it was written</b>.</summary>
        private static Zone Zone_Oracle(AdjacencyCluster adjacencyCluster, Guid guid)
        {
            return (adjacencyCluster.GetZones() ?? []).Find(x => x is not null && x.Guid == guid);
        }

        private static PartOTM59SpaceResult Fail(Guid guid, string name)
        {
            return new PartOTM59SpaceResult(guid, name, ">26 C hours", 300, 142, TM59ComplianceStatus.Fail, true);
        }

        private static long Allocated(Action action)
        {
            //Warmed first, so the measurement is the work and not the JIT.
            action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();

            action();

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        /// <summary>
        /// A block of <paramref name="rooms"/> rooms in dwellings of five, each with a 20 l/s design supply
        /// terminal - plus one communal corridor that belongs to no dwelling, so the "outside the scope"
        /// branch is exercised at every size.
        /// </summary>
        private static AnalyticalModel Model(int rooms, out List<Zone> zones)
        {
            AdjacencyCluster adjacencyCluster = new();

            zones = [];

            Zone zone = null;

            for (int i = 0; i < rooms; i++)
            {
                if (i % 5 == 0)
                {
                    zone = new Zone(string.Format("Flat {0:0000}", (i / 5) + 1));
                    zone.SetValue(ZoneParameter.IsDwelling, true);

                    adjacencyCluster.AddObject(zone);

                    zones.Add(zone);
                }

                Space space = new(string.Format("Bedroom {0:00000}", i + 1));

                adjacencyCluster.AddObject(space);
                adjacencyCluster.AddRelation(zone, space);

                VentilationTerminal ventilationTerminal = new(space.Name + " terminal", FlowClassification.Supply, 20);

                adjacencyCluster.AddObject(ventilationTerminal);
                adjacencyCluster.AddRelation(ventilationTerminal, space);
            }

            //In the model, in no dwelling zone: the communal corridor a round must not target.
            adjacencyCluster.AddObject(new Space("Corridor"));

            return new AnalyticalModel("Block", null, null, null, adjacencyCluster, null, null);
        }
    }
}
