// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The Dwelling / Zone column on the Part O space table.</b>
    ///
    /// <para><b>The gap it closes</b></para>
    /// <para>
    /// Native testing on a multi-dwelling project found the space table listing <c>Studio 1_0</c>,
    /// <c>Corridor_1</c>, <c>Bathroom_2</c>, <c>Bedroom 2_3</c> ... with no way to tell which flat any of
    /// them belonged to. On three flats that is inconvenient; on a real block it makes the table unusable.
    /// </para>
    ///
    /// <para><b>Looked up, never inferred</b></para>
    /// <para>
    /// The name comes from the model's own zone-space relationships. It is never taken from a space's name,
    /// its prefix, an index or the row order - each of which happens to look right on a demonstration model
    /// and is wrong on a real one.
    /// </para>
    ///
    /// <para><b>And the fallback is gated twice</b></para>
    /// <para>
    /// A space outside every dwelling is named after the zone that groups it only if that zone is in the
    /// same <c>ZoneCategory</c> as the dwellings in scope <b>and</b> is a common-space zone of that
    /// category. A fire, thermal, system-grouping or reporting zone therefore cannot reach this column: a
    /// deterministic wrong answer - "Fire compartment 3" under a heading that says "Dwelling / Zone" -
    /// would be worse than no answer, because it reads as a statement about the model.
    /// </para>
    ///
    /// <para><b>Presentation only</b></para>
    /// <para>
    /// Nothing here changes zone membership, Part O scope, the Approved Document F requirement, the design
    /// airflow, the design transfer airflow, the operating airflow, the equipment or any result.
    /// </para>
    /// </summary>
    public class PartOSpaceRowTests
    {
        private const string category_Flats = "Flats";

        // =================================================================================================
        // A. Part O dwellings
        // =================================================================================================

        /// <summary>
        /// Every space of Flat 1 reads "Flat 1", every space of Flat 2 reads "Flat 2", and every space of
        /// Flat 3 reads "Flat 3" - with the name <b>repeated on every row</b> rather than merged or blanked,
        /// because this is a table engineers filter, sort and paste into a spreadsheet.
        /// </summary>
        [Fact]
        public void EverySpaceOfADwelling_ReadsThatDwelling_OnEveryRow()
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            Assert.Equal("Flat 1", Name(adjacencyCluster, dictionary, "Studio 1_0"));
            Assert.Equal("Flat 1", Name(adjacencyCluster, dictionary, "Bathroom_2"));
            Assert.Equal("Flat 1", Name(adjacencyCluster, dictionary, "Kitchen_4"));

            Assert.Equal("Flat 2", Name(adjacencyCluster, dictionary, "Bedroom 2_3"));
            Assert.Equal("Flat 2", Name(adjacencyCluster, dictionary, "Kitchen_7"));

            Assert.Equal("Flat 3", Name(adjacencyCluster, dictionary, "Ensuite_8"));
        }

        /// <summary>
        /// And that reaches the row an engineer actually reads, in its first column.
        /// </summary>
        [Fact]
        public void TheRow_CarriesTheDwellingItWasGiven()
        {
            Assert.Equal("Flat 1", new PartOSpaceRow(new Space("Bathroom_2"), "Flat 1").Dwelling);
        }

        // =================================================================================================
        // B. The gated fallback - a corridor, and nothing else
        // =================================================================================================

        /// <summary>
        /// A corridor that is a common-space zone of the dwellings' own category reads "Corridor". It is
        /// outside every dwelling, it is not attributed to one, and it is not left blank either - the
        /// building's communal space is a real thing with a real name.
        /// </summary>
        [Fact]
        public void ACorridorOfTheDwellingsOwnCategory_ReadsThatCorridor()
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            Assert.Equal("Corridor", Name(adjacencyCluster, dictionary, "Corridor_1"));
        }

        /// <summary>
        /// <b>The test the whole gating exists for.</b> A space whose only zone is an unrelated
        /// classification - a fire compartment, a thermal zone, a system grouping, a reporting zone, each in
        /// its own category - reads as an absence. Never that zone's name.
        /// </summary>
        [Theory]
        [InlineData("Fire compartment 3", "Fire")]
        [InlineData("Thermal zone 12", "Thermal")]
        [InlineData("AHU-01 served spaces", "Systems")]
        [InlineData("Block A reporting", "Reporting")]
        public void ASpaceWhoseOnlyZoneIsAnUnrelatedClassification_ReadsAsAnAbsence(string name_Zone, string zoneCategory)
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            Space space = new("Plant room");

            adjacencyCluster.AddObject(space);

            Zone zone = new(name_Zone);
            zone.SetValue(ZoneParameter.ZoneCategory, zoneCategory);

            adjacencyCluster.AddObject(zone);
            adjacencyCluster.AddRelation(zone, space);

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            Assert.False(dictionary.ContainsKey(space.Guid));

            //And the row shows the em dash rather than the zone's name.
            PartOSpaceRow partOSpaceRow = new(space, dictionary.TryGetValue(space.Guid, out string name) ? name : null);

            Assert.Equal(PartOSpaceRow.Unresolved, partOSpaceRow.Dwelling);
            Assert.DoesNotContain(name_Zone, partOSpaceRow.Dwelling);
        }

        /// <summary>
        /// A space in no zone at all reads as an absence too, and never as an invented dwelling.
        /// </summary>
        [Fact]
        public void ASpaceInNoZone_ReadsAsAnAbsence()
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            Space space = new("Riser");

            adjacencyCluster.AddObject(space);

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            Assert.False(dictionary.ContainsKey(space.Guid));

            Assert.Equal(PartOSpaceRow.Unresolved, new PartOSpaceRow(space).Dwelling);
        }

        /// <summary>
        /// A space in two qualifying common-space zones is an <b>ambiguity</b>, and is reported as an
        /// absence rather than resolved by picking whichever sorted first. Both names would be defensible
        /// and neither is authoritative, so nothing is invented - and the two are never concatenated.
        /// </summary>
        [Fact]
        public void ASpaceInTwoCommonSpaceZones_ReadsAsAnAbsenceRatherThanAGuess()
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            Space space = new("Lobby");

            adjacencyCluster.AddObject(space);

            foreach (string name_Zone in new[] { "Circulation", "Landlord areas" })
            {
                Zone zone = new(name_Zone);
                zone.SetValue(ZoneParameter.ZoneCategory, category_Flats);
                zone.SetValue(ZoneParameter.IsDwelling, false);

                adjacencyCluster.AddObject(zone);
                adjacencyCluster.AddRelation(zone, space);
            }

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            Assert.False(dictionary.ContainsKey(space.Guid));

            string dwelling = new PartOSpaceRow(space, dictionary.TryGetValue(space.Guid, out string name) ? name : null).Dwelling;

            Assert.Equal(PartOSpaceRow.Unresolved, dwelling);
            Assert.DoesNotContain("Circulation", dwelling);
            Assert.DoesNotContain("Landlord", dwelling);
        }

        // =================================================================================================
        // C. Part O dwelling membership has absolute precedence
        // =================================================================================================

        /// <summary>
        /// A space that belongs to Flat 1 <b>and</b> to some other classification reads "Flat 1". Part O
        /// dwelling membership wins outright, whatever the other zone is and whatever order the zones are
        /// in.
        /// </summary>
        [Fact]
        public void ADwellingSpaceThatIsAlsoClassifiedElsewhere_StillReadsItsDwelling()
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            Space space = Space(adjacencyCluster, "Bathroom_2");

            //A second classification over the same space, in its own category and in the dwellings' one.
            foreach (KeyValuePair<string, string> keyValuePair in new Dictionary<string, string> { { "Fire compartment 3", "Fire" }, { "Circulation", category_Flats } })
            {
                Zone zone = new(keyValuePair.Key);
                zone.SetValue(ZoneParameter.ZoneCategory, keyValuePair.Value);
                zone.SetValue(ZoneParameter.IsDwelling, false);

                adjacencyCluster.AddObject(zone);
                adjacencyCluster.AddRelation(zone, space);
            }

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            Assert.Equal("Flat 1", dictionary[space.Guid]);
        }

        /// <summary>
        /// A dwelling zone that exists in the model but is <b>not in the current preparation scope</b> does
        /// not name its spaces - the column reports the run that was prepared, not the whole building.
        /// </summary>
        [Fact]
        public void ADwellingOutsideTheCurrentScope_DoesNotNameItsSpaces()
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            //Only Flat 1 is in scope.
            List<Zone> zones_Scope = zones_Dwelling.FindAll(x => x.Name == "Flat 1");

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Scope);

            Assert.Equal("Flat 1", Name(adjacencyCluster, dictionary, "Bathroom_2"));

            //Flat 2's spaces are out of scope: not attributed to Flat 2, and not attributed to Flat 1.
            Assert.False(dictionary.ContainsKey(Space(adjacencyCluster, "Bedroom 2_3").Guid));
        }

        // =================================================================================================
        // D. The engineering columns are untouched
        // =================================================================================================

        /// <summary>
        /// The four existing columns are exactly what they were: the new column is handed a string and
        /// reads nothing else. Approved Document F requirement absent is <see cref="double.NaN"/> and never
        /// zero, and the design airflows come back through the queries the simulation uses.
        /// </summary>
        [Fact]
        public void TheEngineeringColumns_AreUnchangedByTheNewOne()
        {
            Space space = new("Bathroom_2");

            PartOSpaceRow partOSpaceRow_Before = new(space);
            PartOSpaceRow partOSpaceRow_After = new(space, "Flat 1");

            Assert.Equal(partOSpaceRow_Before.Name, partOSpaceRow_After.Name);
            Assert.Equal(partOSpaceRow_Before.PartFRequired_Lps, partOSpaceRow_After.PartFRequired_Lps);
            Assert.Equal(partOSpaceRow_Before.DesignSupply_Lps, partOSpaceRow_After.DesignSupply_Lps);
            Assert.Equal(partOSpaceRow_Before.DesignExtract_Lps, partOSpaceRow_After.DesignExtract_Lps);

            //A space that was never sized has no requirement, which is not a requirement of nothing.
            Assert.True(double.IsNaN(partOSpaceRow_After.PartFRequired_Lps));
        }

        /// <summary>
        /// Resolving the names changes nothing about the model: the same zones, the same spaces, the same
        /// memberships afterwards. It is a read.
        /// </summary>
        [Fact]
        public void ResolvingTheNames_ChangesNothingAboutTheModel()
        {
            AdjacencyCluster adjacencyCluster = Fixture(out List<Zone> zones_Dwelling);

            string json_Before = Core.Convert.ToString(adjacencyCluster);

            Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            Assert.Equal(json_Before, Core.Convert.ToString(adjacencyCluster));
        }

        /// <summary>A null model resolves nothing and throws nothing.</summary>
        [Fact]
        public void ANullModel_ResolvesNothing()
        {
            Assert.Empty(Modify.DwellingNames_Space(null, []));
        }

        // =================================================================================================
        // E. Scaling - no per-space model rescan
        // =================================================================================================

        /// <summary>
        /// Five thousand spaces over a thousand dwellings: the map is built in one pass over the zones, and
        /// every row is then an O(1) probe.
        /// <para>
        /// The generous bound is a smoke test for an accidental quadratic, not a benchmark. What it catches
        /// is the obvious wrong implementation - <c>GetZones(space)</c> per row, which materialises each
        /// space's whole related set to find its zones - and the worse one, rebuilding the zone collection
        /// inside the row loop.
        /// </para>
        /// </summary>
        [Fact]
        public void FiveThousandSpaces_ResolveInOnePass()
        {
            AdjacencyCluster adjacencyCluster = new();

            List<Zone> zones_Dwelling = [];

            List<Space> spaces = [];

            for (int i = 0; i < 1000; i++)
            {
                Zone zone = new(string.Format("Flat {0:0000}", i));
                zone.SetValue(ZoneParameter.ZoneCategory, category_Flats);
                zone.SetValue(ZoneParameter.IsDwelling, true);

                adjacencyCluster.AddObject(zone);

                zones_Dwelling.Add(zone);

                for (int j = 0; j < 5; j++)
                {
                    Space space = new(string.Format("Room {0}_{1}", i, j));

                    adjacencyCluster.AddObject(space);
                    adjacencyCluster.AddRelation(zone, space);

                    spaces.Add(space);
                }
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Dictionary<Guid, string> dictionary = Modify.DwellingNames_Space(adjacencyCluster, zones_Dwelling);

            List<PartOSpaceRow> partOSpaceRows = spaces.ConvertAll(x => new PartOSpaceRow(x, dictionary.TryGetValue(x.Guid, out string name) ? name : null));

            stopwatch.Stop();

            Assert.Equal(5000, dictionary.Count);
            Assert.Equal(5000, partOSpaceRows.Count);

            Assert.True(stopwatch.ElapsedMilliseconds < 15000, string.Format("Resolving 5000 spaces took {0} ms.", stopwatch.ElapsedMilliseconds));

            Assert.Equal("Flat 0000", partOSpaceRows[0].Dwelling);
            Assert.Equal("Flat 0999", partOSpaceRows[4999].Dwelling);
        }

        // =================================================================================================
        // Fixtures
        // =================================================================================================

        /// <summary>
        /// The shape native testing reported, as a model: three flats and a communal corridor, all in the
        /// one "Flats" category, with the corridor explicitly marked as not a dwelling - which is what
        /// <c>ZoneParameter.IsDwelling</c> exists for and what real models do.
        /// <code>
        /// Flat 1    Studio 1_0  Bathroom_2  Kitchen_4  Ensuite_5
        /// Flat 2    Bedroom 2_3 Kitchen_7
        /// Flat 3    Bedroom 2_6 Ensuite_8
        /// Corridor  Corridor_1
        /// </code>
        /// </summary>
        private static AdjacencyCluster Fixture(out List<Zone> zones_Dwelling)
        {
            AdjacencyCluster result = new();

            zones_Dwelling = [];

            Dictionary<string, string[]> dictionary = new()
            {
                { "Flat 1", ["Studio 1_0", "Bathroom_2", "Kitchen_4", "Ensuite_5"] },
                { "Flat 2", ["Bedroom 2_3", "Kitchen_7"] },
                { "Flat 3", ["Bedroom 2_6", "Ensuite_8"] },
            };

            foreach (KeyValuePair<string, string[]> keyValuePair in dictionary)
            {
                Zone zone = new(keyValuePair.Key);
                zone.SetValue(ZoneParameter.ZoneCategory, category_Flats);
                zone.SetValue(ZoneParameter.IsDwelling, true);

                result.AddObject(zone);

                zones_Dwelling.Add(zone);

                foreach (string name in keyValuePair.Value)
                {
                    Space space = new(name);

                    result.AddObject(space);
                    result.AddRelation(zone, space);
                }
            }

            //The communal corridor: same category, explicitly not a dwelling.
            Zone zone_Corridor = new("Corridor");
            zone_Corridor.SetValue(ZoneParameter.ZoneCategory, category_Flats);
            zone_Corridor.SetValue(ZoneParameter.IsDwelling, false);

            result.AddObject(zone_Corridor);

            Space space_Corridor = new("Corridor_1");

            result.AddObject(space_Corridor);
            result.AddRelation(zone_Corridor, space_Corridor);

            return result;
        }

        private static Space Space(AdjacencyCluster adjacencyCluster, string name)
        {
            Space result = (adjacencyCluster.GetSpaces() ?? []).Find(x => x?.Name == name);

            Assert.NotNull(result);

            return result;
        }

        private static string Name(AdjacencyCluster adjacencyCluster, Dictionary<Guid, string> dictionary, string name_Space)
        {
            return dictionary.TryGetValue(Space(adjacencyCluster, name_Space).Guid, out string result) ? result : PartOSpaceRow.Unresolved;
        }
    }
}
