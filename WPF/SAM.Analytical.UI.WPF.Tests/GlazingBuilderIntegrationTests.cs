// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-3 across the Thermal Performance panel: <c>Create new…</c> (the row editor's Builder) seeded from the chosen / current system, the
    /// Builder's Save as predefined refreshing the open candidate list ONCE through the library's own <see cref="UserGlazingLibrary.Changed"/> and
    /// choosing the new system by Guid, and Apply afterwards being the existing glazing change (one model change, one Undo). Opening, editing,
    /// previewing, saving and cancelling the Builder never touch the model, its history or the Undo stack; each test has its own library file.
    /// </summary>
    public sealed class GlazingBuilderIntegrationTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;
        private readonly FakeGlazingEvaluator candidates = new FakeGlazingEvaluator();

        public GlazingBuilderIntegrationTests()
        {
            library = BuilderFixture.Library(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
            }
        }

        private ThermalEditServices Services(Func<UserGlazingLibrary> user = null, FakeDraftTas tas = null)
        {
            return new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => candidates, () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null,
                () => new FakeSourceReader().Catalog(), user ?? (() => library), () => new DraftGlazingEvaluator(tas ?? new FakeDraftTas(), TimeSpan.Zero, null, BuilderFixture.Options()), BuilderFixture.Options());
        }

        private static ThermalRowEditor OpenWindow(ThermalPerformanceViewModel viewModel, AnalyticalModel model, ThermalParts parts, bool open = true)
        {
            viewModel.Update(model, new List<SAMObject>() { model.AdjacencyCluster.GetAperture(parts.Windows[0]) });
            ThermalRowEditor window = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
            if (open)
            {
                window.OpenChange();
                Wait(window);
            }

            return window;
        }

        private static void Wait(ThermalRowEditor window)
        {
            Assert.True(window.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10)));
        }

        private static void WaitUntil(Func<bool> condition, string what)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), "Timed out waiting for " + what);
                Thread.Sleep(10);
            }
        }

        private string Hash() => File.Exists(library.Path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(library.Path))) : "absent";

        private static string Json(AnalyticalModel model) => model.ToJsonObject().ToJsonString();

        // A double-glazed build from the default library's low-e pane: what a user does in the Builder.
        private static void Build(GlazingBuilderViewModel builder, string name)
        {
            WaitUntil(() => builder.Panes.Sources.Any(x => x.Label == "SAM default library" && x.IsReady), "the default library's panes");
            builder.Panes.SelectedSource = builder.Panes.Sources.Single(x => x.Label == "SAM default library");
            builder.Panes.SelectedEntry = builder.Panes.Entries.Single(x => x.Name == GlazingFixture.LowE);
            builder.SelectedLayer = builder.Layers[0];
            Assert.True(builder.ReplacePane());
            builder.Name = name;
            BuilderUiFixture.Settle(builder);
        }

        // ---- Create new… ---------------------------------------------------------------------------------------------

        [Fact]
        public void Create_new_is_offered_only_while_the_list_is_open_and_my_glazing_systems_is_available()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts, open: false);
                Assert.False(window.CanCreateNew);
                Assert.Null(window.CreateBuilder());

                window.OpenChange();
                Wait(window);
                Assert.True(window.CanCreateNew);

                window.CloseChange();
                Assert.False(window.CanCreateNew);
                Assert.Null(window.CreateBuilder());
            }

            // A host that cannot create the library has no Create new… (the list itself still works).
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services(() => throw new IOException("no documents folder"))))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                Assert.False(window.CanCreateNew);
                Assert.Null(window.CreateBuilder());
                Assert.NotEmpty(window.Candidates);
            }
        }

        [Fact]
        public void The_Builder_is_seeded_from_the_chosen_candidate_else_from_the_current_system()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);

                using (GlazingBuilderViewModel current = window.CreateBuilder())
                {
                    Assert.Equal("GLZ (copy)", current.Name);
                    Assert.Equal(GlazingFixture.CurrentGuid, current.Draft.BasedOnGuid);
                    Assert.Equal("P,G,P", string.Join(",", current.Layers.Select(x => x.IsPane ? "P" : "G")));
                    Assert.True(current.HasFrame);
                }

                // The better system (low-e outer pane) is chosen: the Builder starts from it.
                window.Glazing.SelectedGuid = GlazingFixture.BetterGuid;
                using (GlazingBuilderViewModel chosen = window.CreateBuilder())
                {
                    Assert.Equal(GlazingFixture.BetterGuid, chosen.Draft.BasedOnGuid);
                    Assert.Equal(GlazingFixture.LowE, chosen.Layers[2].Pane.OriginalName);
                }

                // The Builder has panes to choose from (the model's and the library's) and frames to copy; and it saves to the panel's library.
                using (GlazingBuilderViewModel builder = window.CreateBuilder())
                {
                    Assert.Equal(new[] { "This model's panes", "SAM default library" }, builder.Panes.Sources.Select(x => x.Label));
                    Assert.True(builder.CanSave);
                }
            }
        }

        // ---- Save: the list refreshes once, the new system is chosen, the model is untouched -----------------------

        [Fact]
        public void Save_refreshes_the_open_list_once_chooses_the_new_system_and_leaves_the_model_and_its_history_alone()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            int history = 0;
            ui.Modified += (sender, e) => modified++;
            ui.HistoryChanged += (sender, e) => history++;
            string before = Json(ui.JSAMObject);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);
                int rows = window.Candidates.Count;
                int requestsBefore = candidates.Requests.Count;

                GlazingBuilderViewModel builder = window.CreateBuilder();
                Build(builder, "E0 Double");

                // Editing and previewing changed nothing anywhere.
                Assert.Equal(before, Json(ui.JSAMObject));
                Assert.False(File.Exists(library.Path));
                Assert.Equal(rows, window.Candidates.Count);
                Assert.Equal(requestsBefore, candidates.Requests.Count);

                Assert.True(builder.SaveAsync().Result);
                Guid saved = builder.SavedSystem.Guid;
                WaitUntil(() => window.SelectedCandidate?.Guid == saved, "the new system to be chosen");
                Wait(window);
                builder.Dispose();

                // Exactly one new row, calculated once; the new system is the choice; no row was added by hand (it came from the library's refresh).
                Assert.Equal(rows + 1, window.Candidates.Count);
                Assert.Single(window.Candidates, x => x.Guid == saved);
                Assert.Equal(GlazingSourceKind.User, window.SelectedCandidate.Candidate.Kind);
                Assert.Equal("E0 Double", window.SelectedCandidate.Name);
                Assert.Equal(1, candidates.Requests.Skip(requestsBefore).SelectMany(x => x.Batches).SelectMany(x => x.Guids).Count(x => x == saved));
                Assert.Equal(1, candidates.Requests.Count - requestsBefore);
                Assert.True(window.HasRequest);
                Assert.Equal(1, viewModel.Session.ChangeCount);

                // The model, its history and the Undo stack are exactly as they were; only the user library changed.
                Assert.Equal(before, Json(ui.JSAMObject));
                Assert.Equal(0, modified);
                Assert.Equal(0, history);
                Assert.False(ui.CanUndo);
                Assert.Equal(saved, Assert.Single(library.Read().Systems).Guid);
            }
        }

        [Fact]
        public void Cancel_changes_neither_the_model_nor_my_glazing_systems_nor_the_list_and_creates_no_undo()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            int history = 0;
            ui.Modified += (sender, e) => modified++;
            ui.HistoryChanged += (sender, e) => history++;
            string before = Json(ui.JSAMObject);
            string library_Before = Hash();
            int changed = 0;
            library.Changed += (sender, e) => changed++;

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);
                List<Guid> rows = window.Candidates.Select(x => x.Guid).ToList();
                int requestsBefore = candidates.Requests.Count;

                using (GlazingBuilderViewModel builder = window.CreateBuilder())
                {
                    Build(builder, "Never saved");
                    builder.AddGap();
                    builder.ToggleReverse();
                    builder.SelectedFrame = builder.FrameChoices[0];
                }

                Assert.Equal(rows, window.Candidates.Select(x => x.Guid).ToList());
                Assert.Null(window.SelectedCandidate);
                Assert.False(window.HasRequest);
                Assert.Equal(0, viewModel.Session.ChangeCount);
                Assert.Equal(requestsBefore, candidates.Requests.Count);
            }

            Assert.Equal(before, Json(ui.JSAMObject));
            Assert.Equal(0, modified);
            Assert.Equal(0, history);
            Assert.False(ui.CanUndo);
            Assert.Equal(library_Before, Hash());
            Assert.Equal(0, changed);
        }

        [Fact]
        public void A_Save_that_fails_leaves_the_list_and_everything_else_as_it_was()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(library.Path));
            File.WriteAllText(library.Path, "{ not a library");
            string library_Before = Hash();
            ThermalParts parts = ThermalFixture.Build();

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                int rows = window.Candidates.Count;

                using (GlazingBuilderViewModel builder = window.CreateBuilder())
                {
                    Build(builder, "Doomed");
                    Assert.False(builder.SaveAsync().Result);
                    Assert.False(string.IsNullOrWhiteSpace(builder.SaveError));
                }

                Assert.Equal(rows, window.Candidates.Count);
                Assert.Null(window.SelectedCandidate);
            }

            Assert.Equal(library_Before, Hash());
        }

        // ---- Apply and Undo afterwards -----------------------------------------------------------------------------

        [Fact]
        public void Applying_the_saved_system_is_the_existing_glazing_change_one_model_change_one_undo()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            int history = 0;
            ui.Modified += (sender, e) => modified++;
            ui.HistoryChanged += (sender, e) => history++;
            string before_Snapshot = Json(new AnalyticalModel(ui.JSAMObject.ToJsonObject()));
            int materials_Before = ui.JSAMObject.MaterialLibrary.GetMaterials().Count;
            int constructions_Before = ui.JSAMObject.AdjacencyCluster.GetApertureConstructions().Count;

            ThermalChangeSet applied = null;
            ThermalChangeResult result;
            ApertureConstruction saved;
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);

                GlazingBuilderViewModel builder = window.CreateBuilder();
                Build(builder, "E0 Double");
                Assert.True(builder.SaveAsync().Result);
                saved = builder.SavedSystem;
                builder.Dispose();
                WaitUntil(() => window.SelectedCandidate?.Guid == saved.Guid, "the new system to be chosen");
                Wait(window);

                // Only the first window.
                window.ScopeSelected = true;
                Assert.Equal(0, modified);
                Assert.Equal(0, history);
                Assert.False(ui.CanUndo);

                result = viewModel.Apply(set =>
                {
                    applied = set;
                    return Modify.ApplyThermalChange(ui, set, x => { }, null);
                });
            }

            // The existing change set and glazing core: one request for the new system, one model change, one Undo step.
            Assert.True(result.Succeeded, result.Error);
            SetGlazingRequest request = Assert.Single(applied.GlazingRequests);
            Assert.Equal(saved.Guid, request.ApertureConstruction.Guid);
            Assert.Equal(GlazingSourceKind.User, request.Source.Kind);
            Assert.Empty(applied.UValueRequests);
            Assert.Equal(1, modified);
            Assert.True(ui.CanUndo);

            SetGlazingResult glazing = Assert.Single(result.GlazingResults);
            Assert.True(glazing.ApertureConstructionAdded);
            Assert.Equal("My glazing systems", glazing.SourceLabel);
            Assert.NotNull(glazing.BuilderProvenance);

            AnalyticalModel changed = ui.JSAMObject;
            Assert.Equal(constructions_Before + 1, changed.AdjacencyCluster.GetApertureConstructions().Count);
            Assert.Equal(new[] { parts.Windows[0] }, changed.AdjacencyCluster.GetApertures().Where(x => x.TypeGuid == saved.Guid).Select(x => x.Guid));
            Assert.True(changed.MaterialLibrary.GetMaterials().Count > materials_Before);
            Assert.All(glazing.MaterialNamesAdded, x => Assert.NotNull(changed.MaterialLibrary.GetMaterial(x)));

            // One Undo restores the model exactly (no orphan system or material); the library keeps the saved system.
            Assert.True(ui.Undo());
            for (int i = 0; i < 100 && ui.JSAMObject.AdjacencyCluster.GetApertures().Any(x => x.TypeGuid == saved.Guid); i++)
            {
                Thread.Sleep(50);
            }

            Assert.Equal(before_Snapshot, Json(ui.JSAMObject));
            Assert.False(ui.CanUndo);
            Assert.All(glazing.MaterialNamesAdded, x => Assert.Null(ui.JSAMObject.MaterialLibrary.GetMaterial(x)));
            Assert.Equal(saved.Guid, Assert.Single(library.Read().Systems).Guid);
        }

        [Fact]
        public void Two_systems_built_one_after_the_other_are_both_listed_and_only_the_chosen_one_is_applied()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            ui.Modified += (sender, e) => modified++;

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);

                Guid[] guids = new Guid[2];
                for (int i = 0; i < 2; i++)
                {
                    using (GlazingBuilderViewModel builder = window.CreateBuilder())
                    {
                        Build(builder, i == 0 ? "E0 Double" : "E0 Triple");
                        if (i == 1)
                        {
                            builder.Panes.SelectedEntry = builder.Panes.Entries.Single(x => x.Name == GlazingFixture.Clear);
                            builder.AddPane();
                            Assert.Equal("P,G,P,G,P", string.Join(",", builder.Layers.Select(x => x.IsPane ? "P" : "G")));
                            BuilderUiFixture.Settle(builder);
                        }

                        Assert.True(builder.SaveAsync().Result);
                        guids[i] = builder.SavedSystem.Guid;
                    }

                    WaitUntil(() => window.SelectedCandidate?.Guid == guids[i], "system " + (i + 1) + " to be chosen");
                    Wait(window);
                }

                Assert.NotEqual(guids[0], guids[1]);
                Assert.Equal(new[] { "E0 Double", "E0 Triple" }, window.Candidates.Where(x => x.Candidate.Kind == GlazingSourceKind.User).Select(x => x.Name).OrderBy(x => x));
                Assert.Equal(0, modified);
                Assert.False(ui.CanUndo);

                // The panel's choice follows the last build; choosing the first one again is an ordinary choice.
                window.SelectedCandidate = window.Candidates.Single(x => x.Guid == guids[0]);
                Assert.Equal(guids[0], viewModel.Session.BuildChangeSet().GlazingRequests.Single().ApertureConstruction.Guid);
            }
        }

        [Fact]
        public void A_fresh_panel_offers_what_was_saved_before_without_the_Builder()
        {
            ApertureConstruction saved;
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                using (GlazingBuilderViewModel builder = window.CreateBuilder())
                {
                    Build(builder, "E0 Double");
                    Assert.True(builder.SaveAsync().Result);
                    saved = builder.SavedSystem;
                }
            }

            // "Restart": a new services object and a new panel read the file.
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                GlazingCandidateRow row = Assert.Single(window.Candidates, x => x.Guid == saved.Guid);
                Assert.Equal(GlazingSourceKind.User, row.Candidate.Kind);
                Assert.Equal("E0 Double", row.Name);
                Assert.Null(window.SelectedCandidate);
            }
        }
    }
}
