// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-3: the Builder's pane browser - panes of the model, the default library and the panel's remembered / added sources (the SAME
    /// <see cref="ThermalSourceCatalog"/>, no second framework), found by name / display name / category, sorted, large lists projected once off
    /// the UI thread. Sources are stand-ins; nothing reads Tas or the user's settings.
    /// </summary>
    public sealed class GlazingPaneBrowserTests
    {
        private static void WaitUntil(Func<bool> condition, string what = "the condition")
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), "Timed out waiting for " + what);
                Thread.Sleep(10);
            }
        }

        private static GlazingPaneBrowser Browser(ThermalSourceCatalog catalog = null, params GlazingSource[] fixedSources)
        {
            GlazingPaneBrowser result = new GlazingPaneBrowser(fixedSources.Length == 0 ? new[] { BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model) } : fixedSources, catalog, null, TimeSpan.Zero);
            // Without a UI dispatcher the projection's continuation runs on the worker: a source is ready (its panes set) a moment before the list
            // is filtered, so wait for the projection's own task as well.
            WaitUntil(() => result.Sources.Count != 0 && result.Sources.Where(x => !x.IsFile).All(x => x.IsReady) && result.LastWork.IsCompleted, "the fixed sources to be read");
            return result;
        }

        private static string[] Names(GlazingPaneBrowser browser) => browser.Entries.Select(x => x.Name).ToArray();

        [Fact]
        public void The_model_and_the_default_library_are_sources_and_only_glass_panes_are_listed()
        {
            GlazingSource library = new GlazingSource(GlazingSourceKind.Library, "Default library", BuilderUiFixture.Manager(BuilderFixture.SeedMaterials()));
            GlazingPaneBrowser browser = Browser(null, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model), library);

            Assert.Equal(new[] { "This model's panes", "SAM default library" }, browser.Sources.Select(x => x.Label));
            Assert.Equal("This model's panes", browser.SelectedSource.Label);

            // The gas, the frame material and everything that is not glass stay out.
            Assert.Equal(new[] { "Clear4", "LowE4", "LowE4 Reversed", "Tint6" }.OrderBy(x => x), Names(browser).OrderBy(x => x));

            browser.SelectedSource = browser.Sources[1];
            Assert.Equal(new[] { "Clear4", "Tint6" }, Names(browser).OrderBy(x => x));
            Assert.Equal("Showing 2 of 2 panes.", browser.CountText);
        }

        [Fact]
        public void My_glazing_systems_is_not_a_pane_source()
        {
            GlazingSource user = new GlazingSource(GlazingSourceKind.User, "My glazing systems", BuilderUiFixture.Manager(BuilderFixture.SeedMaterials()));
            GlazingPaneBrowser browser = Browser(null, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model), user);

            Assert.Equal(new[] { "This model's panes" }, browser.Sources.Select(x => x.Label));
        }

        [Fact]
        public void Pane_entries_carry_the_values_a_person_chooses_by_and_a_source_by_name_only()
        {
            GlazingPaneBrowser browser = Browser();
            GlazingPaneEntry lowE = browser.Entries.Single(x => x.Name == BuilderFixture.LowE);

            Assert.Equal(4, lowE.ThicknessMillimetres, 6);
            Assert.Equal(0.52, lowE.SolarTransmittance, 6);
            Assert.Equal(0.82, lowE.LightTransmittance, 6);
            Assert.Equal(0.025, lowE.ExternalEmissivity, 6);
            Assert.Equal(0.84, lowE.InternalEmissivity, 6);
            Assert.Equal("Material Root\\Pilkington\\Low-e", lowE.Category);
            Assert.Contains("0.025", lowE.EmissivityText);
            Assert.Contains("Low-e", lowE.Tooltip);

            DraftPane pane = lowE.ToDraftPane();
            Assert.Equal(0.004, pane.Thickness, 6);
            Assert.Equal("This model's panes", pane.SourceLabel);
        }

        [Theory]
        [InlineData("tint", "Tint6")]
        [InlineData("TINT", "Tint6")]
        [InlineData("reversed", "LowE4 Reversed")]
        [InlineData("low-e", "LowE4,LowE4 Reversed")]
        [InlineData("pilkington float", "Clear4")]
        [InlineData("lowe reversed", "LowE4 Reversed")]
        [InlineData("  tinted   pilkington ", "Tint6")]
        [InlineData("nothing like this", "")]
        public void Search_finds_words_in_the_name_the_display_name_and_the_category(string search, string expected)
        {
            GlazingPaneBrowser browser = Browser();
            browser.SearchText = search;

            Assert.Equal(expected.Length == 0 ? new string[0] : expected.Split(','), Names(browser).OrderBy(x => x));
        }

        [Fact]
        public void An_empty_search_lists_everything_again_and_a_chosen_pane_that_is_filtered_out_is_unchosen()
        {
            GlazingPaneBrowser browser = Browser();
            browser.SelectedEntry = browser.Entries.Single(x => x.Name == "Tint6");

            browser.SearchText = "clear";
            Assert.Null(browser.SelectedEntry);
            Assert.Equal("Showing 1 of 4 panes.", browser.CountText);

            browser.SearchText = "zzz";
            Assert.Equal("No pane matches.", browser.CountText);

            browser.SearchText = string.Empty;
            Assert.Equal(4, browser.Entries.Count);
        }

        [Fact]
        public void Panes_sort_by_a_column_and_the_same_column_again_reverses_it()
        {
            GlazingPaneBrowser browser = Browser();

            browser.SortBy(GlazingPaneSortColumn.Thickness);
            Assert.Equal("Tint6", Names(browser).Last());
            Assert.Equal(4, browser.Entries.First().ThicknessMillimetres, 6);

            browser.SortBy(GlazingPaneSortColumn.Thickness);
            Assert.True(browser.SortDescending);
            Assert.Equal("Tint6", Names(browser).First());

            browser.SortBy(GlazingPaneSortColumn.Emissivity);
            Assert.False(browser.SortDescending);
            Assert.Equal(0.025, browser.Entries.First().ExternalEmissivity, 6);

            browser.SortBy(GlazingPaneSortColumn.Name);
            Assert.Equal(Names(browser).OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToArray(), Names(browser));
        }

        // ---- The shared source catalogue ---------------------------------------------------------------------------

        [Fact]
        public void A_remembered_file_is_listed_at_once_and_read_only_when_it_is_chosen()
        {
            string path = FakeSourceReader.Path("remembered.tcd");
            FakeSourceReader reader = new FakeSourceReader().Add(path, BuilderUiFixture.PaneSource("remembered.tcd"));
            ThermalSourceCatalog catalog = reader.Catalog(new InMemoryThermalSourceStore(path));
            GlazingPaneBrowser browser = Browser(catalog);

            GlazingPaneSource remembered = Assert.Single(browser.Sources, x => x.IsFile);
            Assert.Equal("remembered.tcd · remembered, read when chosen", remembered.Display);
            Assert.Empty(reader.Reads);
            Assert.Equal("This model's panes", browser.SelectedSource.Label);

            browser.SelectedSource = remembered;
            WaitUntil(() => remembered.IsReady, "the remembered file to be read");

            Assert.Single(reader.Reads);
            Assert.Equal("remembered.tcd · 4 panes", remembered.Display);
            Assert.Equal(new[] { "Clear4", "LowE4", "LowE4 Reversed", "Tint6" }, Names(browser).OrderBy(x => x));
            Assert.Equal("remembered.tcd", browser.Entries.First().SourceFileName);

            // Chosen again, or the panel asking for the same source: not read again.
            browser.SelectedSource = browser.Sources[0];
            browser.SelectedSource = remembered;
            catalog.EnsureLoadedAsync().Wait();
            Assert.Single(reader.Reads);
        }

        [Fact]
        public void A_file_already_read_is_the_default_source_but_a_remembered_unread_one_is_not()
        {
            string read = FakeSourceReader.Path("read.tcd");
            string remembered = FakeSourceReader.Path("remembered.tcd");
            FakeSourceReader reader = new FakeSourceReader().Add(read, BuilderUiFixture.PaneSource("read.tcd")).Add(remembered, BuilderUiFixture.PaneSource("remembered.tcd"));
            ThermalSourceCatalog catalog = reader.Catalog(new InMemoryThermalSourceStore(remembered));
            catalog.AddAsync(read).Wait();

            GlazingPaneBrowser browser = Browser(catalog);
            WaitUntil(() => browser.Sources.Any(x => x.Label == "read.tcd" && x.IsReady), "read.tcd to be projected");

            Assert.Equal("read.tcd", browser.SelectedSource.Label);
            Assert.Equal(4, browser.Entries.Count);

            // The remembered file was not read just because the Builder opened.
            Assert.Equal(new[] { read }, reader.Reads);
        }

        [Fact]
        public void Add_source_goes_through_the_shared_catalogue_is_remembered_there_and_is_chosen_once_read()
        {
            string path = FakeSourceReader.Path("added.tcd");
            FakeSourceReader reader = new FakeSourceReader().Add(path, BuilderUiFixture.PaneSource("added.tcd", extra: 20));
            InMemoryThermalSourceStore store = new InMemoryThermalSourceStore();
            ThermalSourceCatalog catalog = reader.Catalog(store);
            GlazingPaneBrowser browser = Browser(catalog);
            browser.PickSourceFile = () => path;

            browser.AddSourceAsync().Wait();
            WaitUntil(() => browser.SelectedSource?.IsFile == true && browser.SelectedSource.IsReady, "the added file to be chosen");

            // The panel's own source list is the same catalogue: it lists the file; the file is remembered once; the read happened once.
            Assert.Equal(path, Assert.Single(catalog.Entries).Path);
            Assert.Equal(1, store.Saves);
            Assert.Equal(new[] { path }, store.Paths);
            Assert.Single(reader.Reads);
            Assert.Equal("added.tcd · 24 panes", browser.SelectedSource.Display);
            Assert.Equal("Showing 24 of 24 panes.", browser.CountText);

            // A second browser over the same catalogue (another Builder, later) sees it without reading it again.
            GlazingPaneBrowser another = Browser(catalog);
            WaitUntil(() => another.Sources.Any(x => x.IsFile && x.IsReady), "the second browser to project the file");
            Assert.Single(reader.Reads);
        }

        [Fact]
        public void Cancelling_the_file_dialog_adds_nothing_and_a_missing_catalogue_adds_nothing()
        {
            FakeSourceReader reader = new FakeSourceReader();
            InMemoryThermalSourceStore store = new InMemoryThermalSourceStore();
            ThermalSourceCatalog catalog = reader.Catalog(store);
            GlazingPaneBrowser browser = Browser(catalog);
            browser.PickSourceFile = () => null;

            browser.AddSourceAsync().Wait();
            Assert.Empty(catalog.Entries);
            Assert.Equal(0, store.Saves);

            GlazingPaneBrowser without = Browser(null);
            without.PickSourceFile = () => FakeSourceReader.Path("x.tcd");
            without.AddSourceAsync().Wait();
            Assert.Single(without.Sources);
        }

        [Fact]
        public void A_file_without_panes_or_that_cannot_be_read_is_not_offered_and_a_forgotten_one_goes()
        {
            string empty = FakeSourceReader.Path("constructions-only.tcd");
            string broken = FakeSourceReader.Path("broken.tcd");
            string good = FakeSourceReader.Path("good.tcd");
            FakeSourceReader reader = new FakeSourceReader()
                .Add(empty, new GlazingSource(GlazingSourceKind.Loaded, "constructions-only.tcd", new ConstructionManager()))
                .Add(good, BuilderUiFixture.PaneSource("good.tcd"));
            reader.Failures[broken] = "not a database";
            ThermalSourceCatalog catalog = reader.Catalog(new InMemoryThermalSourceStore());
            GlazingPaneBrowser browser = Browser(catalog);

            browser.AddSourceAsync(empty).Wait();
            WaitUntil(() => browser.Notice != null, "the notice about the file without panes");
            Assert.Contains("constructions-only.tcd has no panes", browser.Notice);
            Assert.Equal("This model's panes", browser.SelectedSource.Label);

            browser.AddSourceAsync(broken).Wait();
            WaitUntil(() => browser.Notice != null && browser.Notice.Contains("broken.tcd"), "the notice about the unreadable file");
            Assert.Contains("not a database", browser.Notice);
            Assert.Equal("This model's panes", browser.SelectedSource.Label);

            browser.AddSourceAsync(good).Wait();
            Assert.Null(browser.Notice);
            WaitUntil(() => browser.SelectedSource?.Label == "good.tcd" && browser.SelectedSource.IsReady, "good.tcd to be chosen");
            WaitUntil(() => browser.Sources.Count == 2, "the sources without panes to be hidden");

            Assert.Equal(new[] { "This model's panes", "good.tcd" }, browser.Sources.Select(x => x.Label));

            // The panel's source list still shows the failures (that is the panel's job).
            Assert.Equal(3, catalog.Entries.Count);

            catalog.Remove(catalog.Entries.Single(x => x.Label == "good.tcd"));
            WaitUntil(() => browser.Sources.All(x => !x.IsFile), "the forgotten source to go");
            Assert.Equal("This model's panes", browser.SelectedSource.Label);
            Assert.NotEmpty(browser.Entries);
        }

        [Fact]
        public void A_source_still_being_read_is_listed_as_reading_and_never_blocks_the_browser()
        {
            string path = FakeSourceReader.Path("slow.tcd");
            FakeSourceReader reader = new FakeSourceReader().Add(path, BuilderUiFixture.PaneSource("slow.tcd")).Hold(path);
            ThermalSourceCatalog catalog = reader.Catalog(new InMemoryThermalSourceStore());
            GlazingPaneBrowser browser = Browser(catalog);

            browser.AddSourceAsync(path);
            WaitUntil(() => browser.Sources.Any(x => x.IsFile), "the slow source to be listed");

            // Chosen at once, and it says it is reading; the browser is not blocked.
            Assert.Equal("slow.tcd", browser.SelectedSource.Label);
            Assert.StartsWith("slow.tcd · reading", browser.Sources.Single(x => x.IsFile).Display);
            Assert.Contains("…", browser.CountText);
            Assert.Empty(browser.Entries);

            // The user goes back to the model's panes meanwhile; the late arrival does not take the choice back.
            browser.SelectedSource = browser.Sources.First(x => !x.IsFile);
            browser.SearchText = "tint";
            Assert.Equal(new[] { "Tint6" }, Names(browser));

            reader.Release(path);
            WaitUntil(() => browser.Sources.Single(x => x.IsFile).IsReady, "the slow source to finish");
            Assert.Equal("This model's panes", browser.SelectedSource.Label);
            Assert.Equal(new[] { "Tint6" }, Names(browser));
        }

        // ---- Large sources -----------------------------------------------------------------------------------------

        [Fact]
        public void A_full_size_database_is_projected_once_filtered_and_sorted_quickly()
        {
            GlazingSource big = BuilderUiFixture.PaneSource("IGDB-full.tcd", extra: 11664, kind: GlazingSourceKind.Model);
            Stopwatch stopwatch = Stopwatch.StartNew();
            GlazingPaneBrowser browser = Browser(null, big);
            TimeSpan projection = stopwatch.Elapsed;

            Assert.Equal(11668, browser.Entries.Count);
            Assert.Equal("Showing 11,668 of 11,668 panes.".Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator), browser.CountText);
            Assert.True(projection < TimeSpan.FromSeconds(10), "projection took " + projection);

            stopwatch.Restart();
            browser.SearchText = "pilkington";
            TimeSpan search = stopwatch.Elapsed;
            Assert.Equal(4, browser.Entries.Count);
            Assert.True(search < TimeSpan.FromSeconds(1), "search took " + search);

            browser.SearchText = "generated 11664";
            Assert.Equal("Generated 11664", Assert.Single(browser.Entries).Name);

            browser.SearchText = string.Empty;
            stopwatch.Restart();
            browser.SortBy(GlazingPaneSortColumn.SolarTransmittance);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), "sort took " + stopwatch.Elapsed);
            Assert.True(browser.Entries[0].SolarTransmittance <= browser.Entries[1].SolarTransmittance);
        }
    }
}
