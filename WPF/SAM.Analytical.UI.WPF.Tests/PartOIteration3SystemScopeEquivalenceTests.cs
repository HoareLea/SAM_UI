// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>TEMPORARY PR-1 PIN.</b> Proves that moving the Iteration 3 system-scope rule from SAM_UI into SAM is
    /// output-identical for Iteration 3: every scenario is run through the production
    /// <see cref="Query.PartOIteration3SystemScope"/> and through <see cref="PartOIteration3SystemScopeOracle"/>, a verbatim
    /// copy of the rule as it stood before the move, and every observable output must be equal - retained and removed
    /// identities in order, notes and refusals character for character, and the working copy's contents.
    /// <para>Deleted with the oracle once the proof is recorded in the PR-1 record.</para>
    /// </summary>
    public class PartOIteration3SystemScopeEquivalenceTests
    {
        private static readonly System.Text.Json.JsonSerializerOptions jsonSerializerOptions = new() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals, TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() };

        private static string Json(AdjacencyCluster adjacencyCluster)
        {
            return adjacencyCluster?.ToJsonObject()?.ToJsonString(jsonSerializerOptions);
        }

        private static void AssertEquivalent(AdjacencyCluster adjacencyCluster, IEnumerable<Guid> guids_Prepared, IEnumerable<Guid> guids_Space_Dwelling)
        {
            List<Guid> guids_Prepared_Temp = guids_Prepared?.ToList();
            List<Guid> guids_Space_Dwelling_Temp = guids_Space_Dwelling?.ToList();

            string json_Before = Json(adjacencyCluster);

            PartOIteration3SystemScopeOracleResult expected = PartOIteration3SystemScopeOracle.Scope(adjacencyCluster, guids_Prepared_Temp, guids_Space_Dwelling_Temp);
            PartOIteration3SystemScope actual = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared_Temp, guids_Space_Dwelling_Temp);

            Assert.Equal(expected.IsScoped, actual.IsScoped);
            Assert.Equal(expected.Guids_Retained, actual.Guids_Retained);
            Assert.Equal(expected.Guids_Removed, actual.Guids_Removed);
            Assert.Equal(expected.Notes, actual.Notes);
            Assert.Equal(expected.Refusals, actual.Refusals);
            Assert.Equal(expected.ToString(), actual.ToString());
            Assert.Equal(expected.AdjacencyCluster is null, actual.AdjacencyCluster is null);

            if (expected.AdjacencyCluster is not null)
            {
                Assert.NotSame(adjacencyCluster, actual.AdjacencyCluster);
                Assert.Equal(Json(expected.AdjacencyCluster), Json(actual.AdjacencyCluster));
            }

            //Neither may touch the design.
            Assert.Equal(json_Before, Json(adjacencyCluster));
        }

        private static AdjacencyCluster Canonical(out List<Guid> guids_Prepared, out List<Guid> guids_Space_Dwelling, out Space space_Corridor, out VentilationSystem ventilationSystem_LegacyMV, out List<Zone> zones)
        {
            AdjacencyCluster result = PartOIteration3Fixture.Design(out guids_Prepared, out zones);

            PartOIteration3Fixture.VentilationSystem_Unnamed(result, "NV");
            PartOIteration3Fixture.VentilationSystem_Unnamed(result, "UV");
            ventilationSystem_LegacyMV = PartOIteration3Fixture.VentilationSystem(result, "AHU1", "MV", out AirHandlingUnit _);
            space_Corridor = PartOIteration3Fixture.Space(result, "Corridor");
            result.AddRelation(ventilationSystem_LegacyMV, space_Corridor);

            guids_Space_Dwelling = [];
            foreach (Zone zone in zones)
            {
                foreach (Space space in PartOIteration3Fixture.Spaces(result, zone))
                {
                    guids_Space_Dwelling.Add(space.Guid);
                }
            }

            return result;
        }

        [Fact]
        public void Canonical_shape()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Guid> guids_Space, out Space _, out VentilationSystem _, out List<Zone> _);

            AssertEquivalent(adjacencyCluster, guids_Prepared, guids_Space);
        }

        [Theory]
        [InlineData(FlowClassification.Supply, 20.0, false)]
        [InlineData(FlowClassification.Extract, 9.0, true)]
        [InlineData(FlowClassification.Undefined, 5.0, false)]
        [InlineData(FlowClassification.Supply, -4.0, true)]
        [InlineData(FlowClassification.Supply, double.PositiveInfinity, false)]
        [InlineData(FlowClassification.Supply, double.NaN, true)]
        [InlineData(FlowClassification.Supply, 0.0, false)]
        [InlineData(FlowClassification.Supply, null, true)]
        public void A_legacy_terminal_in_or_outside_the_dwellings(FlowClassification flowClassification, double? designFlowRate_Lps, bool inDwelling)
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Guid> guids_Space, out Space space_Corridor, out VentilationSystem ventilationSystem_LegacyMV, out List<Zone> zones);

            Space space = inDwelling ? PartOIteration3Fixture.Spaces(adjacencyCluster, zones[0])[0] : space_Corridor;
            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem_LegacyMV, space, flowClassification, designFlowRate_Lps);

            AssertEquivalent(adjacencyCluster, guids_Prepared, guids_Space);
        }

        [Fact]
        public void A_terminal_serving_two_rooms_and_several_systems_with_duty()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Guid> guids_Space, out Space space_Corridor, out VentilationSystem ventilationSystem_LegacyMV, out List<Zone> zones);

            VentilationTerminal ventilationTerminal = PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem_LegacyMV, space_Corridor, FlowClassification.Supply, 11.0);
            adjacencyCluster.AddRelation(ventilationTerminal, PartOIteration3Fixture.Spaces(adjacencyCluster, zones[1])[2]);

            VentilationSystem ventilationSystem_Second = PartOIteration3Fixture.VentilationSystem(adjacencyCluster, "AHU2", "MV", out AirHandlingUnit _);
            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem_Second, PartOIteration3Fixture.Spaces(adjacencyCluster, zones[0])[1], FlowClassification.Extract, 7.0);

            VentilationTerminal ventilationTerminal_Orphan = new("orphan", FlowClassification.Supply, 15.0);
            adjacencyCluster.AddObject(ventilationTerminal_Orphan);
            adjacencyCluster.AddRelation(ventilationSystem_Second, ventilationTerminal_Orphan);

            AssertEquivalent(adjacencyCluster, guids_Prepared, guids_Space);
        }

        [Fact]
        public void Identity_edge_cases()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Guid> guids_Space, out Space _, out VentilationSystem _, out List<Zone> _);

            AssertEquivalent(adjacencyCluster, [], guids_Space);
            AssertEquivalent(adjacencyCluster, null, guids_Space);
            AssertEquivalent(adjacencyCluster, [Guid.Empty], guids_Space);
            AssertEquivalent(adjacencyCluster, [.. guids_Prepared, Guid.Empty, guids_Prepared[0]], guids_Space);
            AssertEquivalent(adjacencyCluster, [.. guids_Prepared, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")], guids_Space);
            AssertEquivalent(adjacencyCluster, guids_Prepared, null);
            AssertEquivalent(adjacencyCluster, guids_Prepared, []);
            AssertEquivalent(null, guids_Prepared, guids_Space);
            AssertEquivalent(null, null, null);
        }

        [Fact]
        public void Same_display_names()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids_Prepared, out List<Zone> zones);
            PartOIteration3Fixture.VentilationSystem(adjacencyCluster, "MVHR-01", "MVHR", out AirHandlingUnit _);

            List<Guid> guids_Space = [];
            zones.ForEach(x => PartOIteration3Fixture.Spaces(adjacencyCluster, x).ForEach(y => guids_Space.Add(y.Guid)));

            AssertEquivalent(adjacencyCluster, guids_Prepared, guids_Space);
        }

        /// <summary>
        /// Seeded random models: a random number of built and authored systems, each authored one with a random number
        /// of terminals of random classification and airflow (including none, zero, NaN, infinite and negative),
        /// related to random dwelling rooms, random outside rooms, both, or none.
        /// </summary>
        [Theory]
        [MemberData(nameof(Seeds))]
        public void Random_models(int seed)
        {
            Random random = new(seed);

            AdjacencyCluster adjacencyCluster = new();

            List<Space> spaces_Dwelling = [];
            List<Space> spaces_Outside = [];
            List<Guid> guids_Prepared = [];

            int count_Dwelling = random.Next(1, 4);
            for (int i = 0; i < count_Dwelling; i++)
            {
                VentilationSystem ventilationSystem = PartOIteration3Fixture.VentilationSystem(adjacencyCluster, string.Format("MVHR-{0}", i), "MVHR", out AirHandlingUnit _);
                Space space_Supply = PartOIteration3Fixture.Space(adjacencyCluster, "Bedroom");
                Space space_Extract = PartOIteration3Fixture.Space(adjacencyCluster, "Bathroom");
                PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem, space_Supply, FlowClassification.Supply, 10 + i);
                PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem, space_Extract, FlowClassification.Extract, 10 + i);
                PartOIteration3Fixture.Zone(adjacencyCluster, string.Format("Flat {0}", i), space_Supply, space_Extract);
                spaces_Dwelling.Add(space_Supply);
                spaces_Dwelling.Add(space_Extract);
                guids_Prepared.Add(ventilationSystem.Guid);
            }

            int count_Outside = random.Next(0, 3);
            for (int i = 0; i < count_Outside; i++)
            {
                spaces_Outside.Add(PartOIteration3Fixture.Space(adjacencyCluster, string.Format("Common {0}", i)));
            }

            double?[] flows = [null, 0.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity, -3.0, 0.5, 12.0, 30.0];
            FlowClassification[] flowClassifications = [FlowClassification.Supply, FlowClassification.Extract, FlowClassification.Undefined];

            int count_Authored = random.Next(0, 6);
            for (int i = 0; i < count_Authored; i++)
            {
                VentilationSystem ventilationSystem = random.Next(2) == 0
                    ? PartOIteration3Fixture.VentilationSystem_Unnamed(adjacencyCluster, random.Next(2) == 0 ? "NV" : "UV")
                    : PartOIteration3Fixture.VentilationSystem(adjacencyCluster, string.Format("AHU{0}", i), "MV", out AirHandlingUnit _);

                int count_Terminal = random.Next(0, 4);
                for (int j = 0; j < count_Terminal; j++)
                {
                    VentilationTerminal ventilationTerminal = new(string.Format("T{0}.{1}", i, j), flowClassifications[random.Next(flowClassifications.Length)], flows[random.Next(flows.Length)]);
                    adjacencyCluster.AddObject(ventilationTerminal);
                    adjacencyCluster.AddRelation(ventilationSystem, ventilationTerminal);

                    int where = random.Next(4);
                    if ((where == 0 || where == 2) && spaces_Dwelling.Count != 0)
                    {
                        adjacencyCluster.AddRelation(ventilationTerminal, spaces_Dwelling[random.Next(spaces_Dwelling.Count)]);
                    }

                    if ((where == 1 || where == 2) && spaces_Outside.Count != 0)
                    {
                        adjacencyCluster.AddRelation(ventilationTerminal, spaces_Outside[random.Next(spaces_Outside.Count)]);
                    }
                }
            }

            List<Guid> guids_Prepared_Offered = random.Next(10) switch
            {
                0 => [],
                1 => [.. guids_Prepared, Guid.NewGuid()],
                2 => [.. guids_Prepared.Take(Math.Max(0, guids_Prepared.Count - 1))],
                _ => guids_Prepared,
            };

            AssertEquivalent(adjacencyCluster, guids_Prepared_Offered, spaces_Dwelling.ConvertAll(x => x.Guid));
        }

        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 1; i <= 300; i++)
            {
                yield return [i];
            }
        }
    }
}
