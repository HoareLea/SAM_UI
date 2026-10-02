// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Thermal Stage D2: "Add source..." in the Thermal Performance panel, for opaque constructions AND glazing systems. One reader (the existing
    /// import and JSON cache) gives one pool per file with both; the catalog lists the sources, remembers their paths, reads them when first needed
    /// and never touches a model; the candidates of both kinds come from it; only the chosen construction or system and the materials it lacks
    /// enter the model, on Apply.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ThermalSourceTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_ThermalSourceTests_" + Guid.NewGuid().ToString("N"));

        public ThermalSourceTests()
        {
            Directory.CreateDirectory(directory);
            GlazingSourceCache.Directory = Path.Combine(directory, "cache");
        }

        public void Dispose()
        {
            GlazingSourceCache.Directory = null;
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private static readonly Guid DoubleGuid = new Guid("c1000000-0000-4000-8000-000000000011");
        private static readonly Guid SingleGuid = new Guid("c1000000-0000-4000-8000-000000000012");
        private static readonly Guid DoorGuid = new Guid("c1000000-0000-4000-8000-000000000013");
        private static readonly Guid WallGuid = new Guid("c1000000-0000-4000-8000-000000000014");

        // What the SAM_Tas importer returns for a database with two glazing systems, a timber door, a wall and an empty construction.
        private static ConstructionManager Converted()
        {
            ConstructionManager constructionManager = new ConstructionManager();
            constructionManager.Add(GlazingFixture.ClearGlass());
            constructionManager.Add(GlazingFixture.ArgonGas());
            constructionManager.Add(new OpaqueMaterial(Guid.NewGuid(), "Timber", "Timber", "Timber", 0.13, 1200, 500));

            Construction glazing = new Construction(DoubleGuid, "opti\\4", new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.Clear, 0.006), new ConstructionLayer(GlazingFixture.Argon, 0.012), new ConstructionLayer(GlazingFixture.Clear, 0.006) });
            glazing.SetValue(ConstructionParameter.Description, "Low-e double");
            constructionManager.Add(glazing);
            constructionManager.Add(new Construction(SingleGuid, "single", new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.Clear, 0.006) }));
            constructionManager.Add(new Construction(DoorGuid, "timber door", new List<ConstructionLayer>() { new ConstructionLayer("Timber", 0.044) }));
            constructionManager.Add(new Construction(WallGuid, "timber wall", new List<ConstructionLayer>() { new ConstructionLayer("Timber", 0.2) }));
            constructionManager.Add(new Construction(Guid.NewGuid(), "empty", new List<ConstructionLayer>()));
            return constructionManager;
        }

        private string Database(string name, ConstructionManager cached)
        {
            string path = Path.Combine(directory, name);
            File.WriteAllText(path, "not a real database; the cache answers for it");
            if (cached != null)
            {
                GlazingSourceCache.Write(path, cached);
            }

            return path;
        }

        // ---- The reader: one file, both kinds ---------------------------------------------------------------------

        [Fact]
        public void A_database_gives_its_constructions_and_its_window_and_door_systems_with_their_guids_and_materials()
        {
            string path = Database("everything.tcd", Converted());

            GlazingSource source = Query.ReadThermalSource(path);

            Assert.Equal(GlazingSourceKind.Loaded, source.Kind);
            Assert.Equal("everything.tcd", source.Label);
            Assert.Null(source.Note);

            // Opaque candidates: every construction with layers (the empty one is left out).
            Assert.Equal(new[] { DoubleGuid, SingleGuid, DoorGuid, WallGuid }.OrderBy(x => x), source.GetConstructions().Select(x => x.Guid).OrderBy(x => x));

            // Glazing candidates: a transparent construction is a window system, any other a door system - the same rule as the glazing reader.
            Assert.Equal(new[] { DoubleGuid, SingleGuid }.OrderBy(x => x), source.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid).OrderBy(x => x));
            Assert.Equal(new[] { DoorGuid, WallGuid }.OrderBy(x => x), source.GetApertureConstructions(ApertureType.Door).Select(x => x.Guid).OrderBy(x => x));
            Assert.True(source.GetApertureConstructions(ApertureType.Window).Single(x => x.Guid == DoubleGuid).TryGetValue(ApertureConstructionParameter.Description, out string description));
            Assert.Equal("Low-e double", description);

            Assert.Contains("Timber", source.GetMaterials().Keys);
            Assert.Contains(GlazingFixture.Clear, source.GetMaterials().Keys);
        }

        [Fact]
        public void The_same_file_loaded_twice_gives_the_same_candidates_and_shares_the_glazing_readers_cache()
        {
            string path = Database("everything.tcd", Converted());
            List<string> messages = new List<string>();

            GlazingSource glazing = Query.ReadGlazingSource(path, ApertureType.Window);
            GlazingSource first = Query.ReadThermalSource(path, new SynchronousProgress(messages));
            GlazingSource second = Query.ReadThermalSource(path);

            Assert.Equal(first.GetConstructions().Select(x => x.Guid).OrderBy(x => x), second.GetConstructions().Select(x => x.Guid).OrderBy(x => x));
            Assert.Equal(glazing.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid).OrderBy(x => x), first.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid).OrderBy(x => x));
            // Read from the cache the glazing reader fills: the converted copy is shared, not converted twice.
            Assert.Equal(new[] { "Reading everything.tcd from the cache…" }, messages.ToArray());
        }

        [Fact]
        public void A_pane_library_says_it_has_no_constructions_or_systems_and_a_missing_or_broken_file_is_not_an_exception()
        {
            ConstructionManager panes = new ConstructionManager();
            panes.Add(GlazingFixture.ClearGlass());
            panes.Add(GlazingFixture.LowEGlass());
            string path = Database("igdb.tcd", panes);

            GlazingSource source = Query.ReadThermalSource(path);
            Assert.Empty(source.GetConstructions());
            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.Equal("igdb.tcd contains 2 panes and no constructions or glazing systems; it is a library of single panes, which do not define a U-value on their own.", source.Note);

            Assert.Equal("The file could not be found.", Query.ReadThermalSource(Path.Combine(directory, "nothing.tcd")).Note);
            Assert.Equal("The file could not be found.", Query.ReadThermalSource(string.Empty).Note);

            string broken = Path.Combine(directory, "broken.json");
            File.WriteAllText(broken, "{ this is not json");
            GlazingSource brokenSource = Query.ReadThermalSource(broken);
            Assert.Empty(brokenSource.GetConstructions());
            Assert.NotNull(brokenSource.Note);
        }

        [Fact]
        public void A_json_file_of_constructions_and_aperture_constructions_is_read_with_its_materials()
        {
            ConstructionManager constructionManager = new ConstructionManager(new[] { GlazingFixture.System(DoorGuid, "Door", ApertureType.Door, GlazingFixture.Clear) }, new[] { AlternativesFixture.Wall("JSON_WALL", 0.1) }, AlternativesFixture.LibraryMaterials());
            string path = Path.Combine(directory, "mixed.json");
            Assert.True(SAM.Core.Convert.ToFile(constructionManager, path));

            GlazingSource source = Query.ReadThermalSource(path);

            Assert.Equal("mixed.json", source.Label);
            Assert.Null(source.Note);
            Assert.Equal(new[] { "JSON_WALL" }, source.GetConstructions().Select(x => x.Name).ToArray());
            Assert.Contains(DoorGuid, source.GetApertureConstructions(ApertureType.Door).Select(x => x.Guid));
            Assert.Contains(AlternativesFixture.Aerogel, source.GetMaterials().Keys);
        }

        [Fact]
        public async Task The_async_read_gives_the_same_source_on_its_own_thread()
        {
            string path = Database("everything.tcd", Converted());

            GlazingSource source = await Query.ReadThermalSourceAsync(path);

            Assert.Equal(4, source.GetConstructions().Count);
        }

        private sealed class SynchronousProgress : IProgress<string>
        {
            private readonly List<string> messages;

            public SynchronousProgress(List<string> messages)
            {
                this.messages = messages;
            }

            public void Report(string value) => messages.Add(value);
        }

        // ---- The catalog -----------------------------------------------------------------------------------------

        [Fact]
        public async Task Adding_a_source_lists_it_reads_it_remembers_the_path_and_tells_what_it_holds()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            InMemoryThermalSourceStore store = new InMemoryThermalSourceStore();
            ThermalSourceCatalog catalog = reader.Catalog(store);
            int changed = 0;
            catalog.SourcesChanged += (sender, e) => changed++;

            Assert.Empty(catalog.Entries);

            await catalog.AddAsync(path);

            ThermalSourceEntry entry = Assert.Single(catalog.Entries);
            Assert.Equal(ThermalSourceState.Ready, entry.State);
            Assert.Equal("database.tcd", entry.Label);
            Assert.Equal(Path.GetFullPath(path), entry.Path);
            Assert.True(entry.HasContent);
            Assert.Equal("3 constructions · 1 window system · 1 door system", entry.StatusText);
            Assert.Single(catalog.ReadySources);
            Assert.Equal(1, changed);
            Assert.Equal(new[] { Path.GetFullPath(path) }, store.Paths.ToArray());
            Assert.False(catalog.IsLoading);
        }

        [Fact]
        public async Task The_same_file_is_one_source_however_it_is_spelled_and_is_read_and_remembered_once()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            InMemoryThermalSourceStore store = new InMemoryThermalSourceStore();
            ThermalSourceCatalog catalog = reader.Catalog(store);

            await catalog.AddAsync(path);
            await catalog.AddAsync(path.ToUpperInvariant());
            await catalog.AddAsync(Path.Combine(Path.GetDirectoryName(path), "..", "SAM_ThermalSources", "database.tcd"));
            await catalog.AddAsync("  ");
            await catalog.AddAsync(null);

            Assert.Single(catalog.Entries);
            Assert.Single(reader.Reads);
            Assert.Equal(1, store.Saves);
        }

        [Fact]
        public async Task A_remembered_source_is_listed_at_once_but_read_only_when_a_row_first_needs_candidates()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("remembered.tcd");
            reader.Add(path, SourceFixture.Loaded("remembered.tcd"));
            ThermalSourceCatalog catalog = reader.Catalog(new InMemoryThermalSourceStore(path));

            ThermalSourceEntry entry = Assert.Single(catalog.Entries);
            Assert.Equal(ThermalSourceState.Pending, entry.State);
            Assert.Equal("Remembered · read when needed", entry.StatusText);
            Assert.Empty(reader.Reads);
            Assert.Empty(catalog.ReadySources);

            await catalog.EnsureLoadedAsync();

            Assert.Equal(ThermalSourceState.Ready, entry.State);
            Assert.Single(reader.Reads);
            Assert.Single(catalog.ReadySources);

            // Asking again reads nothing more.
            await catalog.EnsureLoadedAsync();
            Assert.Single(reader.Reads);
        }

        [Fact]
        public async Task A_source_that_cannot_be_read_stays_listed_as_failed_with_the_reason_until_it_is_forgotten()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("broken.tcd");
            reader.Failures[Path.GetFullPath(path)] = "TCD is not installed";
            InMemoryThermalSourceStore store = new InMemoryThermalSourceStore();
            ThermalSourceCatalog catalog = reader.Catalog(store);

            await catalog.AddAsync(path);

            ThermalSourceEntry entry = Assert.Single(catalog.Entries);
            Assert.Equal(ThermalSourceState.Failed, entry.State);
            Assert.Equal("TCD is not installed", entry.StatusText);
            Assert.Empty(catalog.ReadySources);
            Assert.Single(store.Paths);

            // Adding it again tries again.
            reader.Failures.Clear();
            reader.Add(path, SourceFixture.Loaded("broken.tcd"));
            await catalog.AddAsync(path);
            Assert.Equal(ThermalSourceState.Ready, entry.State);
            Assert.Equal(2, reader.Reads.Count);

            catalog.Remove(entry);
            Assert.Empty(catalog.Entries);
            Assert.Empty(store.Paths);
        }

        [Fact]
        public async Task A_source_with_nothing_to_offer_says_why_and_adds_no_candidates()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("panes.tcd");
            reader.Add(path, new GlazingSource(GlazingSourceKind.Loaded, "panes.tcd", new ConstructionManager()) { Note = "panes.tcd contains 3 panes and no constructions or glazing systems; it is a library of single panes, which do not define a U-value on their own." });
            ThermalSourceCatalog catalog = reader.Catalog();

            await catalog.AddAsync(path);

            ThermalSourceEntry entry = Assert.Single(catalog.Entries);
            Assert.Equal(ThermalSourceState.Ready, entry.State);
            Assert.False(entry.HasContent);
            Assert.StartsWith("panes.tcd contains 3 panes", entry.StatusText);
            Assert.Empty(catalog.ReadySources);
        }

        [Fact]
        public async Task Forgetting_a_source_removes_it_from_the_list_the_memory_and_the_candidates_without_touching_anything_else()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path_1 = FakeSourceReader.Path("one.tcd");
            string path_2 = FakeSourceReader.Path("two.tcd");
            reader.Add(path_1, SourceFixture.Loaded("one.tcd")).Add(path_2, SourceFixture.Loaded("two.tcd"));
            InMemoryThermalSourceStore store = new InMemoryThermalSourceStore();
            ThermalSourceCatalog catalog = reader.Catalog(store);
            await catalog.AddAsync(path_1);
            await catalog.AddAsync(path_2);
            int changed = 0;
            catalog.SourcesChanged += (sender, e) => changed++;

            // Sources keep the order they were added in: the first of a construction wins.
            Assert.Equal(new[] { "one.tcd", "two.tcd" }, catalog.ReadySources.Select(x => x.Label).ToArray());

            catalog.Remove(catalog.Entries[0]);

            Assert.Equal(new[] { "two.tcd" }, catalog.ReadySources.Select(x => x.Label).ToArray());
            Assert.Equal(new[] { Path.GetFullPath(path_2) }, store.Paths.ToArray());
            Assert.Equal(1, changed);
            Assert.Equal(2, reader.Reads.Count);
        }

        [Fact]
        public async Task A_source_forgotten_while_it_is_being_read_never_arrives()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("slow.tcd");
            reader.Add(path, SourceFixture.Loaded("slow.tcd")).Hold(path);
            ThermalSourceCatalog catalog = reader.Catalog();

            Task adding = catalog.AddAsync(path);
            ThermalSourceEntry entry = Assert.Single(catalog.Entries);
            Assert.Equal(ThermalSourceState.Loading, entry.State);
            Assert.True(catalog.IsLoading);

            catalog.Remove(entry);
            reader.Release(path);
            await adding;

            Assert.Empty(catalog.Entries);
            Assert.Empty(catalog.ReadySources);
            Assert.False(catalog.IsLoading);
        }

        // ---- Opaque candidates from the sources -----------------------------------------------------------------

        private sealed class Setup : IDisposable
        {
            public AnalyticalModel Model;
            public Construction Current;
            public UValueViewModel UValue;
            public ConstructionAlternatives Alternatives;
            public FakeConstructionUValueEvaluator Evaluator = new FakeConstructionUValueEvaluator();
            public List<Guid> WallPanels;
            public string JsonBefore;

            public void Dispose()
            {
                Alternatives?.Dispose();
                UValue?.Dispose();
            }
        }

        private static Setup OpenAlternatives(ThermalSourceCatalog catalog, string target = "0.18", bool useLibrary = true)
        {
            Setup setup = new Setup();
            setup.Model = AlternativesFixture.Model(out setup.Current, out _, out _, out _);
            setup.WallPanels = setup.Model.AdjacencyCluster.GetPanels(setup.Current).Select(x => x.Guid).ToList();
            setup.JsonBefore = setup.Model.ToJsonObject().ToJsonString();
            setup.UValue = new UValueViewModel(setup.Model, setup.Current.Guid, new List<Guid>(), new ImmediateUValueEvaluator());
            setup.UValue.TargetText = target;
            setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            GlazingSource library = useLibrary ? AlternativesFixture.Library() : null;
            setup.Alternatives = new ConstructionAlternatives(setup.Model, setup.UValue, setup.Evaluator, new ConstructionUValueCache(), () => library, catalog);
            setup.Alternatives.Refresh();
            setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));
            return setup;
        }

        [Fact]
        public async Task The_constructions_of_an_added_source_are_candidates_beside_the_models_and_the_librarys_and_are_marked_by_their_file()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            ThermalSourceCatalog catalog = reader.Catalog();
            await catalog.AddAsync(path);

            using (Setup setup = OpenAlternatives(catalog))
            {
                string[] names = setup.Alternatives.Rows.Select(x => x.Name).ToArray();
                // Target 0.18: LOADED_THICK 0.173, LOADED_AEROGEL 0.158 meet it, LOADED_CLASH 0.182 is within 10 %.
                Assert.Contains("LOADED_THICK", names);
                Assert.Contains("LOADED_AEROGEL", names);
                Assert.Contains("LOADED_CLASH", names);

                ConstructionAlternativeRow row = setup.Alternatives.Rows.Single(x => x.Name == "LOADED_THICK");
                Assert.Equal(ConstructionAlternativeKind.Loaded, row.Kind);
                Assert.Equal("database.tcd", row.SourceLabel);
                Assert.Equal("database.tcd", row.KindText);
                Assert.Equal("database.tcd · not in the model yet", row.OriginText);
                Assert.True(row.CanApply);
                Assert.Equal(UValueFixture.U(0.128), row.ThermalTransmittance, 6);

                Assert.Contains(setup.Alternatives.Rows.Single(x => x.Name == "LOADED_AEROGEL").Warnings, x => x.Contains("Adds 1 material to the model: Aerogel"));

                // The library's and the model's are still there, and the three pools were calculated as three batches.
                Assert.Contains("LIB_THICK", names);
                Assert.Contains(setup.Alternatives.Rows, x => x.Kind == ConstructionAlternativeKind.Model);
                Assert.Equal(3, setup.Evaluator.Requests.Count);
            }
        }

        [Fact]
        public async Task Loading_a_source_changes_nothing_in_the_model_and_a_loaded_material_clash_blocks_the_candidate()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("clash.tcd");
            reader.Add(path, SourceFixture.Loaded("clash.tcd", differentWool: true));
            ThermalSourceCatalog catalog = reader.Catalog();

            await catalog.AddAsync(path);
            using (Setup setup = OpenAlternatives(catalog))
            {
                ConstructionAlternativeRow row = setup.Alternatives.Rows.Single(x => x.Name == "LOADED_THICK");
                Assert.False(row.CanApply);
                Assert.Contains("differs from the model's material", row.BlockReason);

                // Listing the source's candidates (and selecting one) leaves the model exactly as it was.
                setup.Alternatives.SelectedRow = setup.Alternatives.Rows.Single(x => x.Name == "LOADED_AEROGEL");
                Assert.Equal(setup.JsonBefore, setup.Model.ToJsonObject().ToJsonString());
                Assert.Null(setup.Model.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
                Assert.DoesNotContain(setup.Model.AdjacencyCluster.GetConstructions(), x => x.Name.StartsWith("LOADED_"));
            }
        }

        [Fact]
        public async Task A_construction_in_several_sources_is_one_candidate_the_first_source_winning()
        {
            // The loaded database holds a construction with the Guid of the library's LIB_THICK: it is offered once, as the library's.
            GlazingSource loaded = SourceFixture.Loaded();
            List<Construction> constructions = loaded.GetConstructions();
            constructions.Add(new Construction(AlternativesFixture.LibraryThickGuid, AlternativesFixture.Wall("LOADED_DUPLICATE", 0.13), "LOADED_DUPLICATE"));
            GlazingSource duplicate = new GlazingSource(GlazingSourceKind.Loaded, "dup.tcd", new ConstructionManager(null, constructions, loaded.ConstructionManager.MaterialLibrary));

            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("dup.tcd");
            reader.Add(path, duplicate);
            ThermalSourceCatalog catalog = reader.Catalog();
            await catalog.AddAsync(path);

            using (Setup setup = OpenAlternatives(catalog))
            {
                Assert.Single(setup.Alternatives.Rows.Where(x => x.Guid == AlternativesFixture.LibraryThickGuid));
                Assert.Equal(ConstructionAlternativeKind.Library, setup.Alternatives.Rows.Single(x => x.Guid == AlternativesFixture.LibraryThickGuid).Kind);
                Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Name == "LOADED_DUPLICATE");
            }
        }

        [Fact]
        public async Task A_remembered_source_is_read_when_the_first_target_is_typed_and_the_list_follows_it_as_it_arrives()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("remembered.tcd");
            reader.Add(path, SourceFixture.Loaded("remembered.tcd")).Hold(path);
            ThermalSourceCatalog catalog = reader.Catalog(new InMemoryThermalSourceStore(path));

            // Looking at the model reads nothing.
            Assert.Empty(reader.Reads);

            using (Setup setup = new Setup())
            {
                setup.Model = AlternativesFixture.Model(out setup.Current, out _, out _, out _);
                setup.UValue = new UValueViewModel(setup.Model, setup.Current.Guid, new List<Guid>(), new ImmediateUValueEvaluator());
                setup.UValue.TargetText = "0.18";
                setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                GlazingSource library = AlternativesFixture.Library();
                setup.Alternatives = new ConstructionAlternatives(setup.Model, setup.UValue, setup.Evaluator, new ConstructionUValueCache(), () => library, catalog);
                setup.Alternatives.Refresh();

                // The first target asks for the sources; while one is being read the list says so and offers what it has.
                Assert.Single(reader.Reads);
                Assert.True(catalog.IsLoading);
                Assert.Equal(ConstructionAlternativesStatus.Calculating, setup.Alternatives.Status);
                Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Name == "LOADED_THICK");

                reader.Release(path);
                await catalog.EnsureLoadedAsync();
                setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));

                Assert.False(catalog.IsLoading);
                Assert.Equal(ConstructionAlternativesStatus.Ready, setup.Alternatives.Status);
                Assert.Contains(setup.Alternatives.Rows, x => x.Name == "LOADED_THICK" && x.Kind == ConstructionAlternativeKind.Loaded);
            }
        }

        [Fact]
        public async Task Forgetting_a_source_takes_its_constructions_out_of_the_list_and_a_chosen_one_back_to_the_generated_variant()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            ThermalSourceCatalog catalog = reader.Catalog();
            await catalog.AddAsync(path);

            using (Setup setup = OpenAlternatives(catalog))
            {
                setup.Alternatives.SelectedRow = setup.Alternatives.Rows.Single(x => x.Name == "LOADED_THICK");
                Assert.True(setup.Alternatives.ExistingChosen);

                catalog.Remove(catalog.Entries[0]);
                setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));

                Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Kind == ConstructionAlternativeKind.Loaded);
                Assert.False(setup.Alternatives.ExistingChosen);
                Assert.True(setup.Alternatives.SelectedRow.IsGenerated);
            }
        }

        // ---- Apply: only the choice and what it lacks enter ----------------------------------------------------

        [Fact]
        public async Task Choosing_a_loaded_construction_adds_only_it_and_the_materials_the_model_lacks_on_apply_and_one_undo_removes_them()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            ThermalSourceCatalog catalog = reader.Catalog();
            await catalog.AddAsync(path);

            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            ui.Modified += (sender, e) => modified++;

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => catalog)))
            {
                viewModel.Update(ui.JSAMObject, new List<SAMObject>(ui.JSAMObject.AdjacencyCluster.GetPanels().Where(x => parts.WallPanels.Take(3).Contains(x.Guid))));
                ThermalRowEditor wall = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == parts.Wall.Name).Editor;
                wall.TargetText = "0.18";
                wall.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                wall.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));

                Assert.Null(ui.JSAMObject.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
                Assert.DoesNotContain(ui.JSAMObject.AdjacencyCluster.GetConstructions(), x => x.Guid == SourceFixture.LoadedAerogelGuid);

                wall.SelectedAlternative = wall.AlternativeRows.Single(x => x.Guid == SourceFixture.LoadedAerogelGuid);
                Assert.True(wall.HasRequest);

                ThermalChangeResult result = viewModel.Apply(set => Modify.ApplyThermalChange(ui, set, x => { }, null));

                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(1, modified);
                Assert.Single(result.ConstructionResults);
                Assert.Equal(new[] { AlternativesFixture.Aerogel }, result.ConstructionResults[0].MaterialNamesAdded.ToArray());
                Assert.True(result.ConstructionResults[0].ConstructionAdded);
                Assert.Equal("database.tcd", result.ConstructionResults[0].SourceLabel);
                Assert.Equal(GlazingSourceKind.Loaded, result.ConstructionResults[0].SourceKind);
                Assert.NotNull(ui.JSAMObject.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
                Assert.Equal(12, ui.JSAMObject.AdjacencyCluster.GetPanels().Count(x => x.TypeGuid == SourceFixture.LoadedAerogelGuid));
                // Nothing else of the source came along.
                Assert.DoesNotContain(ui.JSAMObject.AdjacencyCluster.GetConstructions(), x => x.Guid == SourceFixture.LoadedThickGuid || x.Guid == SourceFixture.LoadedClashGuid);

                Assert.True(ui.Undo());
                for (int i = 0; i < 100 && ui.JSAMObject.AdjacencyCluster.GetPanels(parts.Wall).Count != 12; i++)
                {
                    System.Threading.Thread.Sleep(50);
                }

                Assert.Equal(12, ui.JSAMObject.AdjacencyCluster.GetPanels(parts.Wall).Count);
                Assert.Null(ui.JSAMObject.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
                Assert.DoesNotContain(ui.JSAMObject.AdjacencyCluster.GetConstructions(), x => x.Guid == SourceFixture.LoadedAerogelGuid);
            }
        }

        // ---- Glazing candidates from the same sources --------------------------------------------------------

        [Fact]
        public async Task The_window_systems_of_an_added_source_join_a_glazing_rows_list_whether_the_source_was_read_before_or_arrives_after_it_opened()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path_1 = FakeSourceReader.Path("early.tcd");
            string path_2 = FakeSourceReader.Path("late.tcd");
            GlazingSource early = SourceFixture.Loaded("early.tcd");
            GlazingSource late = new GlazingSource(GlazingSourceKind.Loaded, "late.tcd", new ConstructionManager(new[] { GlazingFixture.System(new Guid("e2000000-0000-4000-8000-000000000001"), "LATE_WINDOW", ApertureType.Window, GlazingFixture.Clear, frame: false) }, null, GlazingFixture.ModelMaterials()));
            reader.Add(path_1, early).Add(path_2, late);
            ThermalSourceCatalog catalog = reader.Catalog();
            await catalog.AddAsync(path_1);

            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => catalog)))
            {
                viewModel.Update(parts.Model, new List<SAMObject>(parts.Windows.Take(2).Select(x => (SAMObject)parts.Model.AdjacencyCluster.GetAperture(x))));
                ThermalRowEditor window = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;

                window.OpenChange();
                window.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));

                // The source read before: its window system is a candidate (loaded, marked by its file); its door system is not (this is a window row).
                Assert.Contains(window.Candidates, x => x.Guid == SourceFixture.LoadedWindowGuid && x.Candidate.Kind == GlazingSourceKind.Loaded && x.SourceLabel == "early.tcd");
                Assert.DoesNotContain(window.Candidates, x => x.Guid == SourceFixture.LoadedDoorGuid);
                Assert.DoesNotContain(window.Candidates, x => x.Name == "LATE_WINDOW");

                // A source that arrives while the list is open is added to it.
                await catalog.AddAsync(path_2);
                window.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Assert.Contains(window.Candidates, x => x.Name == "LATE_WINDOW" && x.SourceLabel == "late.tcd");

                // Choosing one is the existing glazing flow: the system and the materials it lacks enter the model on apply, the apertures change.
                window.Glazing.SelectedGuid = SourceFixture.LoadedWindowGuid;
                Assert.True(window.HasRequest);
                Assert.Equal(1, viewModel.Session.ChangeCount);
            }
        }

        [Fact]
        public async Task Closing_a_glazing_list_stops_following_the_sources_and_a_source_is_not_added_twice()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            ThermalSourceCatalog catalog = reader.Catalog();
            await catalog.AddAsync(path);

            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => catalog)))
            {
                viewModel.Update(parts.Model, new List<SAMObject>(parts.Windows.Take(2).Select(x => (SAMObject)parts.Model.AdjacencyCluster.GetAperture(x))));
                ThermalRowEditor window = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;

                window.OpenChange();
                window.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Assert.Single(window.Candidates.Where(x => x.Guid == SourceFixture.LoadedWindowGuid));

                // Another read of an already-listed file changes nothing; closing ends the following.
                await catalog.AddAsync(path);
                Assert.Single(window.Candidates.Where(x => x.Guid == SourceFixture.LoadedWindowGuid));

                window.CloseChange();
                Assert.Null(window.Glazing);
                await catalog.AddAsync(FakeSourceReader.Path("another.tcd"));
            }
        }

        // ---- The control: Add source..., the list, forget ---------------------------------------------------------

        [WpfFact]
        public async Task The_panel_adds_a_source_from_the_file_it_is_given_lists_it_with_what_it_holds_and_forgets_it()
        {
            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            InMemoryThermalSourceStore store = new InMemoryThermalSourceStore();

            using (ThermalPerformanceControl control = new ThermalPerformanceControl(SourceFixture.Services(reader, store)))
            {
                System.Windows.Window window = new System.Windows.Window { Content = control, Left = 0, Top = 0, Width = 380, Height = 900, ShowActivated = false };
                window.Show();
                try
                {
                    System.Windows.Controls.Button add = (System.Windows.Controls.Button)control.FindName("button_AddSource");
                    System.Windows.Controls.ItemsControl list = (System.Windows.Controls.ItemsControl)control.FindName("itemsControl_Sources");
                    System.Windows.Controls.TextBlock hint = (System.Windows.Controls.TextBlock)control.FindName("textBlock_SourcesHint");
                    Assert.NotNull(add);
                    Assert.Equal(0, list.Items.Count);
                    Assert.Equal("Candidates come from the model and the default library.", hint.Text);

                    // Cancelling the dialog adds nothing.
                    control.PickSourceFile = () => null;
                    Press(add);
                    Assert.Equal(0, list.Items.Count);
                    Assert.Empty(reader.Reads);

                    control.PickSourceFile = () => path;
                    Press(add);
                    await control.ViewModel.Session.Services.Sources.EnsureLoadedAsync();
                    for (int i = 0; i < 100 && control.ViewModel.Session.Services.Sources.Entries.Any(x => x.State != ThermalSourceState.Ready); i++)
                    {
                        await Task.Delay(20);
                    }

                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                    Assert.Equal(1, list.Items.Count);
                    ThermalSourceEntry entry = (ThermalSourceEntry)list.Items[0];
                    Assert.Equal("database.tcd", entry.Label);
                    Assert.Equal("3 constructions · 1 window system · 1 door system", entry.StatusText);
                    Assert.Equal("Candidates come from the model, the default library and:", hint.Text);
                    Assert.Equal(new[] { Path.GetFullPath(path) }, store.Paths.ToArray());

                    // Forget it.
                    System.Windows.Controls.Button forget = Descendants<System.Windows.Controls.Button>(list).First(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == "button_ForgetSource");
                    Assert.Equal("Forget database.tcd", System.Windows.Automation.AutomationProperties.GetName(forget));
                    Press(forget);

                    Assert.Empty(control.ViewModel.Session.Services.Sources.Entries);
                    Assert.Equal(0, list.Items.Count);
                    Assert.Empty(store.Paths);
                    Assert.Equal("Candidates come from the model and the default library.", hint.Text);
                }
                finally
                {
                    window.Close();
                }
            }
        }

        // A button pressed the way automation does: the Click is dispatched, so the dispatcher is pumped before the test looks at the result.
        private static void Press(System.Windows.Controls.Button button)
        {
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(button).GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private static IEnumerable<T> Descendants<T>(System.Windows.DependencyObject root) where T : System.Windows.DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
            {
                System.Windows.DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (T descendant in Descendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
