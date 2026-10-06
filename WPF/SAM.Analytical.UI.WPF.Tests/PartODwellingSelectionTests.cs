// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The dwelling scope of the Prepare Part O Iteration dialog: genuine multi-selection over every
    /// eligible dwelling, by zone identity, at a scale a real project reaches.
    /// <para>
    /// The selection model is tested separately from the window's rendering - the window tests at the end
    /// only pin that the two are wired together. Which zones are dwellings at all is
    /// <c>Query.PartFDwellingZones</c>' decision and is pinned in <c>PartOPresentationTests</c>; nothing
    /// here re-decides it.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartODwellingSelectionTests
    {
        private static Zone Dwelling(string name)
        {
            Zone result = new(name);
            result.SetValue(ZoneParameter.IsDwelling, true);

            return result;
        }

        private static PartODwellingSelection Selection(params Zone[] zones)
        {
            return new PartODwellingSelection(zones);
        }

        // ---------------------------------------------------------------------------------------------
        // Defaults
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// The default scope is every eligible dwelling: narrowing it is a deliberate act, never the
        /// starting state.
        /// </summary>
        [Fact]
        public void EveryEligibleDwelling_StartsSelected()
        {
            PartODwellingSelection selection = Selection(Dwelling("Flat 1"), Dwelling("Flat 2"), Dwelling("Flat 3"));

            Assert.Equal(3, selection.Count);
            Assert.Equal(3, selection.SelectedCount);
            Assert.Equal(3, selection.SelectedZones().Count);
        }

        [Fact]
        public void NoDwellings_IsAnEmptySelection_NotAnError()
        {
            PartODwellingSelection selection = Selection();

            Assert.Equal(0, selection.Count);
            Assert.Equal(0, selection.SelectedCount);
            Assert.Empty(selection.SelectedZones());
        }

        // ---------------------------------------------------------------------------------------------
        // Selection, by identity
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void DeselectingOneDwelling_LeavesTheOthersSelected()
        {
            Zone zone_Flat1 = Dwelling("Flat 1");
            Zone zone_Flat2 = Dwelling("Flat 2");
            Zone zone_Flat3 = Dwelling("Flat 3");

            PartODwellingSelection selection = Selection(zone_Flat1, zone_Flat2, zone_Flat3);

            PartODwellingSelection.Item item_Flat2 = Assert.Single(selection.Items, x => x.Guid == zone_Flat2.Guid);
            item_Flat2.IsSelected = false;

            List<Zone> selected = selection.SelectedZones();

            Assert.Equal(2, selected.Count);
            Assert.Contains(zone_Flat1.Guid, selected.ConvertAll(x => x.Guid));
            Assert.Contains(zone_Flat3.Guid, selected.ConvertAll(x => x.Guid));
            Assert.DoesNotContain(zone_Flat2.Guid, selected.ConvertAll(x => x.Guid));
        }

        /// <summary>
        /// Two dwellings called the same thing are two scopes. The selection authority is the zone's guid,
        /// so clearing one "Flat 1" leaves the other "Flat 1" selected.
        /// </summary>
        [Fact]
        public void DuplicateDisplayNames_AreStillDistinctSelections()
        {
            Zone zone_A = Dwelling("Flat 1");
            Zone zone_B = Dwelling("Flat 1");

            PartODwellingSelection selection = Selection(zone_A, zone_B);

            Assert.Equal(2, selection.Count);

            PartODwellingSelection.Item item_A = Assert.Single(selection.Items, x => x.Guid == zone_A.Guid);
            item_A.IsSelected = false;

            List<Zone> selected = selection.SelectedZones();

            Zone zone = Assert.Single(selected);
            Assert.Equal(zone_B.Guid, zone.Guid);
        }

        [Fact]
        public void SelectNone_ThenSelectAll()
        {
            PartODwellingSelection selection = Selection(Dwelling("Flat 1"), Dwelling("Flat 2"));

            selection.SetSelected(false);

            Assert.Equal(0, selection.SelectedCount);
            Assert.Empty(selection.SelectedZones());

            selection.SetSelected(true);

            Assert.Equal(2, selection.SelectedCount);
        }

        // ---------------------------------------------------------------------------------------------
        // Search and filtered bulk selection
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void Search_IsACaseInsensitiveSubstring_OverTheDiscoveredNames()
        {
            PartODwellingSelection selection = Selection(Dwelling("Block A - Flat 1"), Dwelling("Block A - Flat 2"), Dwelling("Block B - Flat 3"));

            selection.SearchText = "block a";

            int visible = 0;
            foreach (PartODwellingSelection.Item item in selection.Items)
            {
                if (selection.IsVisible(item))
                {
                    visible++;
                }
            }

            Assert.Equal(2, visible);

            selection.SearchText = string.Empty;

            Assert.All(selection.Items, x => Assert.True(selection.IsVisible(x)));
        }

        /// <summary>
        /// Select All / None act on what the search matches, so a large block can be narrowed by name and
        /// then taken or dropped as a group. What the search hides is untouched - filtered out of sight is
        /// not filtered out of the selection.
        /// </summary>
        [Fact]
        public void SelectNone_WithinASearch_LeavesTheHiddenDwellingsSelected()
        {
            PartODwellingSelection selection = Selection(Dwelling("Block A - Flat 1"), Dwelling("Block A - Flat 2"), Dwelling("Block B - Flat 3"));

            selection.SearchText = "Block A";
            selection.SetSelected(false);

            Assert.Equal(1, selection.SelectedCount);

            Zone zone = Assert.Single(selection.SelectedZones());
            Assert.Equal("Block B - Flat 3", zone.Name);

            //Clearing the search reveals the untouched state rather than a reset one.
            selection.SearchText = string.Empty;

            Assert.Equal(1, selection.SelectedCount);
            Assert.False(Assert.Single(selection.Items, x => x.Name == "Block A - Flat 1").IsSelected);

            selection.SetSelected(true);

            Assert.Equal(3, selection.SelectedCount);
        }

        /// <summary>A selection change raises one notification, so a listener can refresh once.</summary>
        [Fact]
        public void ABatchSelection_RaisesOneChangedNotification()
        {
            PartODwellingSelection selection = Selection(Dwelling("Flat 1"), Dwelling("Flat 2"), Dwelling("Flat 3"));

            int changes = 0;
            selection.SelectionChanged += (s, e) => changes++;

            selection.SetSelected(false);

            Assert.Equal(1, changes);
        }

        // ---------------------------------------------------------------------------------------------
        // Scale
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// A real project carries thousands of spaces and hundreds to thousands of dwelling zones. The
        /// selection model holds one lightweight record per dwelling - there is no per-space anything here -
        /// and search and bulk selection are passes over those records, so a block-scale scope is an
        /// ordinary interaction rather than a batch job.
        /// </summary>
        [Fact]
        public void ABlockScaleScope_SelectsAndFiltersWithoutRescanningAnything()
        {
            List<Zone> zones = [];
            for (int i = 1; i <= 2000; i++)
            {
                zones.Add(Dwelling(string.Format("Block {0} - Flat {1:000}", (i % 20) + 1, i)));
            }

            PartODwellingSelection selection = new(zones);

            Assert.Equal(2000, selection.Count);
            Assert.Equal(2000, selection.SelectedCount);

            selection.SearchText = "Block 5 -";
            selection.SetSelected(false);

            Assert.Equal(1900, selection.SelectedCount);

            //One dwelling back on, by identity, while the search is still narrowing the view. (i = 104 is
            //the first Flat in Block 5: the block is (i % 20) + 1.)
            PartODwellingSelection.Item item = Assert.Single(selection.Items, x => x.Name == "Block 5 - Flat 104");
            item.IsSelected = true;

            Assert.Equal(1901, selection.SelectedCount);

            selection.SearchText = string.Empty;
            selection.SetSelected(true);

            Assert.Equal(2000, selection.SelectedCount);
        }

        // ---------------------------------------------------------------------------------------------
        // The window, wired to the model
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// What the window hands to <c>Modify.PreparePartOIteration</c> is the selection - not the whole
        /// eligible set. This is the seam that makes the scope genuine rather than cosmetic.
        /// </summary>
        [WpfFact]
        public void TheWindow_Prepares_ExactlyTheSelectedDwellings()
        {
            Zone zone_Flat1 = Dwelling("Flat 1");
            Zone zone_Flat2 = Dwelling("Flat 2");
            Zone zone_Flat3 = Dwelling("Flat 3");

            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = [zone_Flat1, zone_Flat2, zone_Flat3],
            };

            //All three by default - the current behaviour for a user who changes nothing.
            Assert.Equal(3, partOIterationWindow.Zones_Dwelling.Count);

            partOIterationWindow.DwellingSelection.SearchText = "Flat 2";
            partOIterationWindow.DwellingSelection.SetSelected(false);
            partOIterationWindow.DwellingSelection.SearchText = string.Empty;

            List<Zone> zones_Dwelling = partOIterationWindow.Zones_Dwelling;

            Assert.Equal(2, zones_Dwelling.Count);
            Assert.DoesNotContain(zone_Flat2.Guid, zones_Dwelling.ConvertAll(x => x.Guid));
        }

        /// <summary>An empty selection cannot be accepted: there is nothing to prepare.</summary>
        [WpfFact]
        public void TheWindow_RefusesToAcceptAnEmptyScope()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = [Dwelling("Flat 1"), Dwelling("Flat 2")],
            };

            Assert.True(partOIterationWindow.CanAccept);
            Assert.Contains("2 of 2", partOIterationWindow.SelectionDescription);

            partOIterationWindow.DwellingSelection.SetSelected(false);

            Assert.False(partOIterationWindow.CanAccept);
            Assert.Contains("0 of 2", partOIterationWindow.SelectionDescription);
        }

        /// <summary>
        /// The classification report is untouched by selection: an out-of-scope zone is still named with its
        /// reason, and it never becomes selectable.
        /// </summary>
        [WpfFact]
        public void TheWindow_ReportsWhatIsOutOfScope_AlongsideTheSelection()
        {
            Zone zone_Flat1 = Dwelling("Flat 1");

            Zone zone_Corridor = new("Corridor_1");
            zone_Corridor.SetValue(ZoneParameter.IsDwelling, false);

            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = [zone_Flat1, zone_Corridor],
            };

            Assert.Single(partOIterationWindow.DwellingSelection.Items);
            Assert.Contains("Corridor_1", partOIterationWindow.ScopeDescription);
            Assert.Contains("marked not a dwelling", partOIterationWindow.ScopeDescription);
        }
    }
}
