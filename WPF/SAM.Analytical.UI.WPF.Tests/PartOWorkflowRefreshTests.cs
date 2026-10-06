// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Analytical.UI.WPF;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>When the Part O workflow dialog inspects the analytical model, and when it must not.</b>
    /// <para>
    /// The dialog answers a selection change by re-inspecting the model, which is correct - different
    /// dwellings are different spaces, different Approved Document F requirements and a different
    /// preparation match. What it must not do is pay that price for something that cannot have moved a
    /// single stage. Three paths did:
    /// </para>
    /// <list type="number">
    /// <item><c>Restore</c> flipped the saved scope one row at a time, and every flipped row was a
    /// selection notification of its own - so reopening a narrowed scope over a block ran the whole
    /// inspection once per dwelling.</item>
    /// <item>The Iteration 2B step, the iteration limit and the follow-on tick ran the full inspection on
    /// every keystroke, although the settings are deliberately outside the engineering-preparation match
    /// and nothing the inspection asks reads them.</item>
    /// <item>The search said in a comment that it was deliberately not a full refresh, and then set
    /// <c>SearchText</c> - which raised the same event the dialog answers with a full refresh. Every
    /// keystroke inspected the model anyway.</item>
    /// </list>
    /// <para>
    /// <b>Asserted by counting, never by timing.</b> A notification count and the identity of the
    /// <see cref="PartOWorkflowInspection"/> object on the window are exact and machine-independent; a
    /// stopwatch is neither. Nothing here asserts how long anything takes.
    /// </para>
    /// <para>
    /// <b>Nothing here is about the engineering.</b> What a full inspection reports is unchanged and is
    /// pinned in <c>PartOWorkflowTests</c>; this file is about how often it is asked.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowRefreshTests
    {
        /// <summary>How many dwellings a "hundreds of dwellings" case carries. A real block reaches this.</summary>
        private const int Dwellings = 500;

        // -------------------------------------------------------------------------------------------------
        // The selection model: a bulk restore is ONE selection change
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>1.</b> Restoring a narrowed scope over hundreds of dwellings raises one logical selection
        /// notification, not one per flipped row.
        /// <para>
        /// This is the whole defect: the listener is the workflow dialog, and it answers a selection change
        /// by inspecting the analytical model. Per-row notifications made a restore cost hundreds of
        /// inspections.
        /// </para>
        /// </summary>
        [Fact]
        public void ABulkRestoreOfHundredsOfDwellings_RaisesOneSelectionNotification()
        {
            List<Zone> zones = Dwelling_Zones(Dwellings);

            PartODwellingSelection partODwellingSelection = new(zones);

            Assert.Equal(Dwellings, partODwellingSelection.Count);
            Assert.Equal(Dwellings, partODwellingSelection.SelectedCount);

            //Every second dwelling - so the restore flips hundreds of rows in both directions and cannot
            //accidentally be a no-op.
            List<Guid> guids = [];
            for (int i = 0; i < zones.Count; i += 2)
            {
                guids.Add(zones[i].Guid);
            }

            int notifications = 0;
            partODwellingSelection.SelectionChanged += (s, e) => notifications++;

            partODwellingSelection.RestoreSelection(guids);

            Assert.Equal(1, notifications);

            Assert.Equal(guids.Count, partODwellingSelection.SelectedCount);
        }

        /// <summary>
        /// The batch is not silence: every row whose state actually moved still raises its own
        /// <c>PropertyChanged</c>, because that is what the bound checkbox is listening to. Suppressing the
        /// per-row notification instead of the per-row LOGICAL notification would leave the list showing
        /// the scope the user came from.
        /// </summary>
        [Fact]
        public void ABulkRestore_StillNotifiesEveryBoundRowThatMoved()
        {
            List<Zone> zones = Dwelling_Zones(Dwellings);

            PartODwellingSelection partODwellingSelection = new(zones);

            int properties = 0;
            foreach (PartODwellingSelection.Item item in partODwellingSelection.Items)
            {
                item.PropertyChanged += (s, e) => properties++;
            }

            //One dwelling kept: every other row moves from selected to cleared.
            partODwellingSelection.RestoreSelection([zones[0].Guid]);

            Assert.Equal(Dwellings - 1, properties);

            Assert.Equal(1, partODwellingSelection.SelectedCount);
        }

        /// <summary>
        /// The restore is by identity, exactly as every other selection on this class. Two dwellings that
        /// share a display name are two scopes, and restoring one must not restore the other.
        /// </summary>
        [Fact]
        public void ABulkRestore_IsByGuid_NeverByName()
        {
            Zone zone_A = Dwelling_Zone("Flat 1");
            Zone zone_B = Dwelling_Zone("Flat 1");

            PartODwellingSelection partODwellingSelection = new([zone_A, zone_B]);

            partODwellingSelection.RestoreSelection([zone_A.Guid]);

            Zone zone = Assert.Single(partODwellingSelection.SelectedZones());

            Assert.Equal(zone_A.Guid, zone.Guid);
        }

        /// <summary>
        /// A restore states the WHOLE scope, so a search left in the box cannot narrow what it is allowed to
        /// touch - unlike Select All / None, which are gestures on what the search matches.
        /// </summary>
        [Fact]
        public void ABulkRestore_IgnoresTheSearch_AndStatesTheWholeScope()
        {
            Zone zone_A1 = Dwelling_Zone("Block A - Flat 1");
            Zone zone_A2 = Dwelling_Zone("Block A - Flat 2");
            Zone zone_B3 = Dwelling_Zone("Block B - Flat 3");

            PartODwellingSelection partODwellingSelection = new([zone_A1, zone_A2, zone_B3])
            {
                SearchText = "Block A",
            };

            partODwellingSelection.RestoreSelection([zone_B3.Guid]);

            //The hidden dwelling is the one selected, and the two visible ones were cleared: the restore
            //spoke about all three.
            Zone zone = Assert.Single(partODwellingSelection.SelectedZones());

            Assert.Equal(zone_B3.Guid, zone.Guid);
        }

        /// <summary>
        /// Search text is not a selection change and must not be announced as one - the workflow dialog
        /// answers a selection change with a full inspection of the analytical model.
        /// </summary>
        [Fact]
        public void SearchText_RaisesItsOwnNotification_AndNeverTheSelectionOne()
        {
            PartODwellingSelection partODwellingSelection = new(Dwelling_Zones(3));

            int selectionChanges = 0;
            int searchChanges = 0;

            partODwellingSelection.SelectionChanged += (s, e) => selectionChanges++;
            partODwellingSelection.SearchTextChanged += (s, e) => searchChanges++;

            partODwellingSelection.SearchText = "Flat 2";

            Assert.Equal(0, selectionChanges);
            Assert.Equal(1, searchChanges);

            //And an unchanged text is not a change at all.
            partODwellingSelection.SearchText = "Flat 2";

            Assert.Equal(1, searchChanges);

            Assert.Equal(3, partODwellingSelection.SelectedCount);
        }

        // -------------------------------------------------------------------------------------------------
        // The dialog: Restore
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>2.</b> The real <c>PartOWorkflowWindow.Restore</c> path over a block-scale model: the narrowed
        /// scope comes back exactly, and it comes back as ONE selection change - so the inspection the
        /// dialog answers that change with runs once, not once per dwelling.
        /// </summary>
        [WpfFact]
        public void TheDialog_RestoresANarrowedScope_AsOneSelectionChange()
        {
            AnalyticalModel analyticalModel = Model(Dwellings);

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = analyticalModel,
            };

            Assert.Equal(Dwellings, partOWorkflowWindow.DwellingSelection.Count);

            //Seven of five hundred, taken by identity off the list the dialog is showing - and one guid that
            //is not an eligible dwelling at all, which a re-zoned model would carry.
            List<Guid> guids = [];
            for (int i = 0; i < 7; i++)
            {
                guids.Add(partOWorkflowWindow.DwellingSelection.Items[i].Guid);
            }

            guids.Add(Guid.NewGuid());

            int notifications = 0;
            partOWorkflowWindow.DwellingSelection.SelectionChanged += (s, e) => notifications++;

            partOWorkflowWindow.Restore(null, PartOWorkflowScope.SelectedDwellings, guids);

            Assert.Equal(1, notifications);

            //The restored scope, by guid. The guid the model no longer offers was dropped rather than
            //preparing a scope the preparation cannot honour.
            Assert.Equal(7, partOWorkflowWindow.DwellingSelection.SelectedCount);

            List<Zone> zones_Dwelling = partOWorkflowWindow.Zones_Dwelling;

            Assert.Equal(7, zones_Dwelling.Count);

            HashSet<Guid> guids_Restored = [.. zones_Dwelling.ConvertAll(x => x.Guid)];

            for (int i = 0; i < 7; i++)
            {
                Assert.Contains(partOWorkflowWindow.DwellingSelection.Items[i].Guid, guids_Restored);
            }

            Assert.Equal(PartOWorkflowScope.SelectedDwellings, partOWorkflowWindow.Scope);

            //And the status list is the restored scope's, not a stale one.
            Assert.NotNull(partOWorkflowWindow.Inspection);
            Assert.Contains("7 of 500", partOWorkflowWindow.ScopeDescription, StringComparison.Ordinal);
        }

        /// <summary>
        /// The restored selection is identical to what the row-by-row loop produced: every named guid
        /// selected, everything else cleared, whatever the list was ticked to beforehand.
        /// </summary>
        [WpfFact]
        public void TheDialog_RestoresTheSameScopeTheRowByRowLoopDid()
        {
            AnalyticalModel analyticalModel = Model(12);

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = analyticalModel,
            };

            //Start from a scope somebody had already narrowed differently, so the restore has to clear as
            //well as select.
            partOWorkflowWindow.DwellingSelection.SetSelected(false);
            partOWorkflowWindow.DwellingSelection.Items[3].IsSelected = true;

            List<Guid> guids = [partOWorkflowWindow.DwellingSelection.Items[0].Guid, partOWorkflowWindow.DwellingSelection.Items[9].Guid];

            partOWorkflowWindow.Restore(null, PartOWorkflowScope.SelectedDwellings, guids);

            for (int i = 0; i < partOWorkflowWindow.DwellingSelection.Count; i++)
            {
                Assert.Equal(guids.Contains(partOWorkflowWindow.DwellingSelection.Items[i].Guid), partOWorkflowWindow.DwellingSelection.Items[i].IsSelected);
            }
        }

        // -------------------------------------------------------------------------------------------------
        // The dialog: workflow-only input does not reinspect the model
        // -------------------------------------------------------------------------------------------------

        // -------------------------------------------------------------------------------------------------
        // The dialog: search
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>5.</b> Typing in the search narrows the bound list, updates the line that describes it, leaves
        /// every dwelling's selected state alone - and inspects nothing.
        /// <para>
        /// This is the path whose own comment claimed it was deliberately not a full refresh while
        /// <c>SearchText</c> raised the event the dialog answers with one.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TypingInTheSearch_FiltersTheListAndInspectsNothing()
        {
            AnalyticalModel analyticalModel = Model(40);

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = analyticalModel,
                Scope = PartOWorkflowScope.SelectedDwellings,
            };

            //A narrowed scope, so "selection intact" is a real claim rather than "everything is selected".
            partOWorkflowWindow.DwellingSelection.SetSelected(false);
            partOWorkflowWindow.DwellingSelection.Items[0].IsSelected = true;
            partOWorkflowWindow.DwellingSelection.Items[7].IsSelected = true;

            Guid guid_First = partOWorkflowWindow.DwellingSelection.Items[0].Guid;
            Guid guid_Eighth = partOWorkflowWindow.DwellingSelection.Items[7].Guid;

            Assert.Equal(40, partOWorkflowWindow.VisibleDwellingCount);

            PartOWorkflowInspection partOWorkflowInspection = partOWorkflowWindow.Inspection;

            Assert.NotNull(partOWorkflowInspection);

            //Typed one character at a time, as a person types it - every keystroke used to inspect.
            partOWorkflowWindow.SearchText = "F";
            partOWorkflowWindow.SearchText = "Fl";
            partOWorkflowWindow.SearchText = "Fla";
            partOWorkflowWindow.SearchText = "Flat 007";

            //The list a person sees really narrowed.
            Assert.Equal(1, partOWorkflowWindow.VisibleDwellingCount);

            //The line describing it moved, and says the search is narrowing the list.
            Assert.Contains("2 of 40 dwellings selected", partOWorkflowWindow.SelectionDescription, StringComparison.Ordinal);
            Assert.Contains("The search is narrowing the list", partOWorkflowWindow.SelectionDescription, StringComparison.Ordinal);

            //Not one dwelling's state changed - including the one the search hid.
            Assert.Equal(2, partOWorkflowWindow.DwellingSelection.SelectedCount);
            Assert.True(Assert.Single(partOWorkflowWindow.DwellingSelection.Items, x => x.Guid == guid_First).IsSelected);
            Assert.True(Assert.Single(partOWorkflowWindow.DwellingSelection.Items, x => x.Guid == guid_Eighth).IsSelected);

            Assert.Equal(2, partOWorkflowWindow.Zones_Dwelling.Count);

            //And no keystroke inspected the model.
            Assert.Same(partOWorkflowInspection, partOWorkflowWindow.Inspection);

            //Clearing it restores the whole view, still without an inspection.
            partOWorkflowWindow.SearchText = string.Empty;

            Assert.Equal(40, partOWorkflowWindow.VisibleDwellingCount);
            Assert.Equal(2, partOWorkflowWindow.DwellingSelection.SelectedCount);
            Assert.DoesNotContain("narrowing", partOWorkflowWindow.SelectionDescription, StringComparison.Ordinal);
            Assert.Same(partOWorkflowInspection, partOWorkflowWindow.Inspection);
        }

        // -------------------------------------------------------------------------------------------------
        // The dialog: a genuine inspection input still inspects
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>6.</b> The other half of the fix, and the one that matters most: an ACTUAL change of scope
        /// still re-inspects. Cheapening the UI-only paths must not have cheapened the real one.
        /// </summary>
        [WpfFact]
        public void ChangingTheDwellingSelection_StillInspectsTheModel()
        {
            AnalyticalModel analyticalModel = Model(12);

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = analyticalModel,
                Scope = PartOWorkflowScope.SelectedDwellings,
            };

            PartOWorkflowInspection partOWorkflowInspection = partOWorkflowWindow.Inspection;

            Assert.NotNull(partOWorkflowInspection);

            //One dwelling, by identity, exactly as the bound checkbox does it.
            partOWorkflowWindow.DwellingSelection.Items[0].IsSelected = false;

            Assert.NotSame(partOWorkflowInspection, partOWorkflowWindow.Inspection);
            Assert.Equal(11, partOWorkflowWindow.Zones_Dwelling.Count);

            //A bulk gesture too - one inspection for the gesture, and a new one.
            PartOWorkflowInspection partOWorkflowInspection_Narrowed = partOWorkflowWindow.Inspection;

            partOWorkflowWindow.DwellingSelection.SetSelected(false);

            Assert.NotSame(partOWorkflowInspection_Narrowed, partOWorkflowWindow.Inspection);

            //An empty scope is a blocker, and the dialog says so - which is the inspection having actually run.
            Assert.False(partOWorkflowWindow.CanRun);
            Assert.Contains("Run is unavailable", partOWorkflowWindow.BlockerDescription, StringComparison.Ordinal);
        }

        /// <summary>
        /// And so do the scenario and the scope controls: they change the request the inspection is made
        /// against.
        /// </summary>
        [WpfFact]
        public void ChangingTheScenarioOrTheScope_StillInspectsTheModel()
        {
            AnalyticalModel analyticalModel = Model(6);

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = analyticalModel,
            };

            PartOWorkflowInspection partOWorkflowInspection = partOWorkflowWindow.Inspection;

            Assert.NotNull(partOWorkflowInspection);

            partOWorkflowWindow.Scope = PartOWorkflowScope.SelectedDwellingsIsolated;

            Assert.NotSame(partOWorkflowInspection, partOWorkflowWindow.Inspection);

            PartOWorkflowInspection partOWorkflowInspection_Isolated = partOWorkflowWindow.Inspection;

            partOWorkflowWindow.Restore(Scenario_2(), PartOWorkflowScope.AllDwellings, null);

            Assert.NotSame(partOWorkflowInspection_Isolated, partOWorkflowWindow.Inspection);
        }

        // -------------------------------------------------------------------------------------------------
        // The other consumer of the selection model
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>7.</b> The existing Prepare Iteration picker shares
        /// <see cref="PartODwellingSelection"/> and is unaffected by the split.
        /// <para>
        /// It is the window that actually needed watching: it derives its one selection line from BOTH the
        /// selected count and whether a search is narrowing the list, and it had no search handler of its
        /// own - it relied entirely on the event <c>SearchText</c> raised. It now listens to both
        /// notifications, so its line moves on either, exactly as before.
        /// </para>
        /// </summary>
        [WpfFact]
        public void ThePrepareIterationPicker_StillDescribesBothItsSelectionAndItsSearch()
        {
            Zone zone_A1 = Dwelling_Zone("Block A - Flat 1");
            Zone zone_A2 = Dwelling_Zone("Block A - Flat 2");
            Zone zone_B3 = Dwelling_Zone("Block B - Flat 3");

            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = [zone_A1, zone_A2, zone_B3],
            };

            Assert.True(partOIterationWindow.CanAccept);
            Assert.Contains("3 of 3", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);
            Assert.DoesNotContain("narrowing", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);

            //A search: the line says so, and not one dwelling left the scope.
            partOIterationWindow.DwellingSelection.SearchText = "Block A";

            Assert.Contains("3 of 3", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);
            Assert.Contains("The search is narrowing the list", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);
            Assert.Equal(3, partOIterationWindow.Zones_Dwelling.Count);

            //None, within the search: the two visible ones go and the hidden one stays.
            partOIterationWindow.DwellingSelection.SetSelected(false);

            Assert.Contains("1 of 3", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);
            Assert.True(partOIterationWindow.CanAccept);

            Zone zone = Assert.Single(partOIterationWindow.Zones_Dwelling);
            Assert.Equal(zone_B3.Guid, zone.Guid);

            //Clearing the search reveals the untouched state, and the line loses the search sentence.
            partOIterationWindow.DwellingSelection.SearchText = string.Empty;

            Assert.Contains("1 of 3", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);
            Assert.DoesNotContain("narrowing", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);

            //And the empty-scope gate still closes on the whole set.
            partOIterationWindow.DwellingSelection.SetSelected(false);

            Assert.False(partOIterationWindow.CanAccept);
            Assert.Contains("0 of 3", partOIterationWindow.SelectionDescription, StringComparison.Ordinal);
        }

        // ---- Fixture ------------------------------------------------------------------------------------

        private static PartOWorkflowScenario Scenario_2()
        {
            return PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit);
        }

        private static Zone Dwelling_Zone(string name)
        {
            Zone result = new(name);
            result.SetValue(ZoneParameter.IsDwelling, true);

            return result;
        }

        private static List<Zone> Dwelling_Zones(int count)
        {
            List<Zone> result = [];

            for (int i = 1; i <= count; i++)
            {
                result.Add(Dwelling_Zone(string.Format("Flat {0:000}", i)));
            }

            return result;
        }

        /// <summary>
        /// A block of <paramref name="count"/> dwellings, one TM59-classifiable bedroom each with the
        /// Approved Document F continuous requirement a mechanical route is realized from.
        /// <para>
        /// One room per dwelling rather than three: what these tests count is how often the model is
        /// inspected, not what the inspection reports, and a smaller model makes the block-scale cases
        /// ordinary tests rather than a batch job.
        /// </para>
        /// </summary>
        private static AnalyticalModel Model(int count)
        {
            AdjacencyCluster adjacencyCluster = new();

            for (int i = 1; i <= count; i++)
            {
                Zone zone = new(string.Format("Flat {0:000}", i));
                zone.SetValue(ZoneParameter.IsDwelling, true);

                adjacencyCluster.AddObject(zone);

                Space space = new(string.Format("Bedroom {0:000}", i), null)
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
            }

            return new AnalyticalModel(
                "Block",
                null,
                null,
                null,
                adjacencyCluster,
                new MaterialLibrary("Materials"),
                new ProfileLibrary("Profiles"));
        }

        /// <summary>
        /// A minimal catalogue with one selectable product, written to a temporary directory and read back
        /// through the production reader - so the dialog is given a real catalogue rather than a stub.
        /// </summary>
        private static VentilationUnitCatalogue Catalogue()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_WorkflowRefreshCatalogue_{0}", Guid.NewGuid()));

            Directory.CreateDirectory(directory);

            File.WriteAllText(Path.Combine(directory, "VentilationUnitCatalogue.JSON"), """
            {
              "Schema": "VentilationUnitCatalogue:v1",
              "Templates": [
                {
                  "_type": "SAM.Analytical.VentilationUnitTemplate,SAM.Analytical",
                  "Name": "Test unit",
                  "VentilationUnitReference": {
                    "_type": "SAM.Analytical.VentilationUnitReference,SAM.Analytical",
                    "Name": "Test unit",
                    "Manufacturer": "Test",
                    "Model": "T-150",
                    "Reference": "TEST-150"
                  },
                  "Source": "Written by this test. A template needs a traceable source to be valid at all.",
                  "MaximumSupplyFlowRate_Lps": 150,
                  "MaximumExtractFlowRate_Lps": 150,
                  "Rank": 10
                }
              ]
            }
            """);

            return VentilationUnitCatalogue.Read(directory);
        }
    }
}
