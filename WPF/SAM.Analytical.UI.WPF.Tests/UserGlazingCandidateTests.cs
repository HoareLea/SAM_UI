// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-2: "My glazing systems" (the E0-1 user library) as a source of every glazing <c>Change…</c> list of the Thermal Performance panel.
    /// Its complete systems are ordinary candidates (same view-model, evaluator, material checks), after the model and the default library and
    /// before the loaded sources, the first source of a Guid winning; the list follows <see cref="UserGlazingLibrary.Changed"/> without duplicates
    /// and can be told to choose a system by Guid (for E0-3); reading, refreshing, choosing and checking never touch the model; Apply is the
    /// existing glazing change (one model change, one Undo) and its report names the Guid, the source and how a Builder system was built. A
    /// library that cannot be read is a note, never an error of the panel and never overwritten. Each test uses its own temporary library file.
    /// </summary>
    public sealed class UserGlazingCandidateTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;

        public UserGlazingCandidateTests()
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

        // ---- Helpers ----------------------------------------------------------------------------------------------

        private ApertureConstruction Save(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        // A library file written by hand (to give a system a chosen Guid or material); the same ConstructionManager JSON the library writes.
        private void WriteLibrary(MaterialLibrary materials, params ApertureConstruction[] systems)
        {
            ConstructionManager constructionManager = new ConstructionManager(systems, null, materials) { Name = UserGlazingLibrary.LibraryName };
            File.WriteAllText(library.Path, constructionManager.ToJsonObject().ToJsonString());
        }

        private static MaterialLibrary Materials(bool differentClear = false)
        {
            MaterialLibrary result = new MaterialLibrary("User");
            result.Add(GlazingFixture.ClearGlass(differentClear ? 0.5 : 1));
            result.Add(GlazingFixture.ArgonGas());
            result.Add(GlazingFixture.FrameOpaque());
            result.Add(GlazingFixture.LowEGlass());
            return result;
        }

        private string Hash() => File.Exists(library.Path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(library.Path))) : "absent";

        private ThermalEditServices Services(IGlazingEvaluator evaluator = null, ThermalSourceCatalog catalog = null, UserGlazingLibrary user = null)
        {
            return new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => evaluator ?? new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => catalog ?? new FakeSourceReader().Catalog(), () => user ?? library);
        }

        // The panel on the model with the first window selected; the window row's list opened and calculated.
        private static ThermalRowEditor OpenWindow(ThermalPerformanceViewModel viewModel, AnalyticalModel model, ThermalParts parts, string target = null)
        {
            viewModel.Update(model, new List<SAMObject>() { model.AdjacencyCluster.GetAperture(parts.Windows[0]) });
            ThermalRowEditor window = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
            window.OpenChange();
            Wait(window);
            if (target != null)
            {
                window.GlazingTargetText = target;
            }

            return window;
        }

        private static void Wait(ThermalRowEditor window)
        {
            Assert.True(window.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10)));
        }

        private static int Subscribers(UserGlazingLibrary userGlazingLibrary)
        {
            FieldInfo field = typeof(UserGlazingLibrary).GetField(nameof(UserGlazingLibrary.Changed), BindingFlags.Instance | BindingFlags.NonPublic);
            return (field.GetValue(userGlazingLibrary) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        private static string Json(AnalyticalModel model) => model.ToJsonObject().ToJsonString();

        // ---- The source -------------------------------------------------------------------------------------------

        [Fact]
        public void No_library_file_is_an_empty_source_without_a_note_and_nothing_is_created()
        {
            GlazingSource source = GlazingSource.FromUserLibrary(library);

            Assert.Equal(GlazingSourceKind.User, source.Kind);
            Assert.Equal("My glazing systems", source.Label);
            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.Null(source.Note);
            Assert.False(File.Exists(library.Path));
        }

        [Fact]
        public void An_empty_library_is_an_empty_source_and_a_null_library_is_one_too()
        {
            WriteLibrary(new MaterialLibrary("User"));

            GlazingSource source = GlazingSource.FromUserLibrary(library);
            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.Null(source.Note);

            GlazingSource none = GlazingSource.FromUserLibrary(null);
            Assert.Equal(GlazingSourceKind.User, none.Kind);
            Assert.Empty(none.GetApertureConstructions(ApertureType.Window));
        }

        [Fact]
        public void Saved_systems_are_the_sources_complete_systems_with_their_materials()
        {
            ApertureConstruction single = Save(BuilderFixture.Double());
            GlazingSource one = GlazingSource.FromUserLibrary(library);
            Assert.Equal(new[] { single.Guid }, one.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid));

            ApertureConstruction triple = Save(BuilderFixture.Triple());
            GlazingSource two = GlazingSource.FromUserLibrary(library);
            Assert.Equal(new[] { single.Guid, triple.Guid }.OrderBy(x => x), two.GetApertureConstructions(ApertureType.Window).Select(x => x.Guid).OrderBy(x => x));

            // Every layer's material is in the source: Tas can calculate them and Apply can add what the model lacks.
            Dictionary<string, IMaterial> materials = two.GetMaterials();
            Assert.All(triple.PaneConstructionLayers.Concat(triple.FrameConstructionLayers), x => Assert.True(materials.ContainsKey(x.Name), x.Name));
        }

        [Theory]
        [InlineData("{ this is not json")]
        [InlineData("")]
        [InlineData("{\"_type\":\"SAM.Analytical.Panel\"}")]
        public void A_library_that_cannot_be_read_is_an_empty_source_with_a_note_and_is_left_as_it_is(string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(library.Path));
            File.WriteAllText(library.Path, text);
            string before = Hash();

            GlazingSource source = GlazingSource.FromUserLibrary(library);

            Assert.Equal(GlazingSourceKind.User, source.Kind);
            Assert.Empty(source.GetApertureConstructions(ApertureType.Window));
            Assert.StartsWith("My glazing systems could not be used:", source.Note);
            Assert.Contains("The other sources still work.", source.Note);
            Assert.Equal(before, Hash());
        }

        [Fact]
        public void The_services_offer_the_shared_library_by_default_created_on_first_use()
        {
            Assert.Same(UserGlazingLibrary.Shared, new ThermalEditServices().UserGlazing);
            Assert.Same(library, Services().UserGlazing);

            // The test assembly never reads the user's own file.
            Assert.NotEqual(UserGlazingLibrary.DefaultPath, UserGlazingLibrary.Shared.Path);
        }

        [Fact]
        public void The_pool_is_model_then_default_library_then_my_glazing_systems_then_loaded_sources_whatever_the_order_they_arrive()
        {
            Save(BuilderFixture.Double());
            using (GlazingViewModel viewModel = GlazingFixture.ViewModel(GlazingFixture.Model(2)))
            {
                viewModel.AddSourceAsync(GlazingFixture.LoadedGood()).Wait();
                viewModel.SetUserSourceAsync(GlazingSource.FromUserLibrary(library)).Wait();
                viewModel.AddSourceAsync(GlazingFixture.Loaded()).Wait();

                Assert.Equal(new[] { GlazingSourceKind.Model, GlazingSourceKind.Library, GlazingSourceKind.User, GlazingSourceKind.Loaded, GlazingSourceKind.Loaded }, viewModel.Sources.Select(x => x.Kind));
                Assert.Equal(new[] { "good.json", "loaded.json" }, viewModel.Sources.Where(x => x.Kind == GlazingSourceKind.Loaded).Select(x => x.Label));
            }

            Assert.True(GlazingSource.Rank(GlazingSourceKind.Library) < GlazingSource.Rank(GlazingSourceKind.User));
            Assert.True(GlazingSource.Rank(GlazingSourceKind.User) < GlazingSource.Rank(GlazingSourceKind.Loaded));
        }

        // ---- Precedence and identity -------------------------------------------------------------------------------

        [Fact]
        public void The_same_guid_in_the_model_and_in_my_glazing_systems_is_one_row_from_the_model()
        {
            WriteLibrary(Materials(), GlazingFixture.System(GlazingFixture.CurrentGuid, "Renamed in my library", ApertureType.Window, GlazingFixture.LowE));

            using (GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(2), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(), GlazingSource.FromUserLibrary(library)))
            {
                viewModel.InitializeAsync().Wait();

                GlazingCandidateRow row = Assert.Single(viewModel.Rows, x => x.Guid == GlazingFixture.CurrentGuid);
                Assert.Equal(GlazingSourceKind.Model, row.Candidate.Kind);
                Assert.Equal(GlazingFixture.CurrentName, row.Name);
            }
        }

        [Fact]
        public void The_same_guid_in_the_default_library_and_in_my_glazing_systems_is_one_row_from_the_library_whatever_its_name()
        {
            WriteLibrary(Materials(), GlazingFixture.System(GlazingFixture.BetterGuid, "my better glz", ApertureType.Window, GlazingFixture.LowE));

            using (GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(2), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(), GlazingSource.FromUserLibrary(library)))
            {
                viewModel.InitializeAsync().Wait();

                GlazingCandidateRow row = Assert.Single(viewModel.Rows, x => x.Guid == GlazingFixture.BetterGuid);
                Assert.Equal(GlazingSourceKind.Library, row.Candidate.Kind);
                Assert.Equal(GlazingFixture.CurrentName, row.Name);
                Assert.DoesNotContain(viewModel.Rows, x => x.Name == "my better glz");
            }
        }

        [Fact]
        public void The_same_guid_in_my_glazing_systems_and_a_loaded_source_is_one_row_from_my_library_even_when_the_loaded_one_came_first()
        {
            WriteLibrary(Materials(), GlazingFixture.System(GlazingFixture.LoadedGuid, "MINE", ApertureType.Window, GlazingFixture.LowE));
            FakeGlazingEvaluator evaluator = new FakeGlazingEvaluator();

            using (GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(2), GlazingFixture.CurrentGuid, null, evaluator, GlazingFixture.Library()))
            {
                viewModel.InitializeAsync().Wait();
                viewModel.AddSourceAsync(GlazingFixture.LoadedGood()).Wait();
                Assert.Equal(GlazingSourceKind.Loaded, Assert.Single(viewModel.Rows, x => x.Guid == GlazingFixture.LoadedGuid).Candidate.Kind);

                viewModel.SetUserSourceAsync(GlazingSource.FromUserLibrary(library)).Wait();

                GlazingCandidateRow row = Assert.Single(viewModel.Rows, x => x.Guid == GlazingFixture.LoadedGuid);
                Assert.Equal(GlazingSourceKind.User, row.Candidate.Kind);
                Assert.Equal("MINE", row.Name);
                Assert.NotNull(row.Values);

                // The system now comes from another source, so it was calculated again from that source.
                Assert.Same(row.Candidate.Source, evaluator.Requests.Last().Batches.Single(x => x.Guids.Contains(GlazingFixture.LoadedGuid)).Source);
            }
        }

        [Fact]
        public void A_system_listed_twice_in_my_library_is_one_row()
        {
            ApertureConstruction system = GlazingFixture.System(new Guid("e0200000-0000-4000-8000-000000000001"), "TWICE", ApertureType.Window, GlazingFixture.LowE);
            WriteLibrary(Materials(), system, new ApertureConstruction(system));

            using (GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(2), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(), GlazingSource.FromUserLibrary(library)))
            {
                viewModel.InitializeAsync().Wait();
                Assert.Single(viewModel.Rows, x => x.Guid == system.Guid);
            }
        }

        [Fact]
        public void A_material_of_the_same_name_is_reused_when_identical_and_blocks_the_system_when_it_differs()
        {
            Guid guid = new Guid("e0200000-0000-4000-8000-000000000002");
            WriteLibrary(Materials(), GlazingFixture.System(guid, "SAME_CLEAR", ApertureType.Window, GlazingFixture.LowE));
            using (GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(2), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(), GlazingSource.FromUserLibrary(library)))
            {
                viewModel.InitializeAsync().Wait();
                GlazingCandidateRow row = viewModel.Rows.Single(x => x.Guid == guid);
                Assert.True(row.CanApply);
                Assert.Equal(new[] { GlazingFixture.LowE }, row.Candidate.MaterialsToAdd.Select(x => x.Name));
            }

            WriteLibrary(Materials(differentClear: true), GlazingFixture.System(guid, "OTHER_CLEAR", ApertureType.Window, GlazingFixture.LowE));
            using (GlazingViewModel viewModel = new GlazingViewModel(GlazingFixture.Model(2), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(), GlazingSource.FromUserLibrary(library)))
            {
                viewModel.InitializeAsync().Wait();
                GlazingCandidateRow row = viewModel.Rows.Single(x => x.Guid == guid);
                Assert.False(row.CanApply);
                Assert.True(row.Candidate.MaterialDiffers);

                viewModel.SelectedGuid = guid;
                Assert.Contains("differs from the model's material", viewModel.ApplyBlockReason);
                Assert.Null(viewModel.CreateRequest());
            }
        }

        // ---- The Change… list -------------------------------------------------------------------------------------

        [Fact]
        public void Opening_a_glazing_list_lists_my_systems_as_ordinary_calculated_candidates()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double());
            ThermalParts parts = ThermalFixture.Build();

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);

                GlazingCandidateRow row = Assert.Single(window.Candidates, x => x.Guid == saved.Guid);
                Assert.Equal(GlazingSourceKind.User, row.Candidate.Kind);
                Assert.Equal("My glazing systems", row.SourceLabel);
                Assert.Equal("E0 Double", row.Name);
                Assert.NotNull(row.Values);
                Assert.Equal("1.50", row.UgText);
                Assert.Equal("0.55", row.GText);
                Assert.Equal("0.75", row.LightTransmittanceText);
                Assert.Equal("2.00", row.UfText);
                Assert.True(row.CanApply);
                Assert.False(window.HasGlazingNotes);

                // The other sources are still there.
                Assert.Contains(window.Candidates, x => x.Candidate.Kind == GlazingSourceKind.Library);
                Assert.Contains(window.Candidates, x => x.IsCurrent);
            }
        }

        [Fact]
        public void Refreshing_again_and_again_keeps_one_row_per_system_and_asks_tas_nothing_new()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double());
            ThermalParts parts = ThermalFixture.Build();
            FakeGlazingEvaluator evaluator = new FakeGlazingEvaluator();

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services(evaluator)))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                int rows = window.Candidates.Count;
                int asked = evaluator.GuidsRequested;

                for (int i = 0; i < 3; i++)
                {
                    window.RefreshUserGlazingAsync().Wait();
                }

                Assert.Equal(rows, window.Candidates.Count);
                Assert.Single(window.Candidates, x => x.Guid == saved.Guid);
                Assert.Equal(asked, evaluator.GuidsRequested);
                Assert.Single(window.Glazing.Sources, x => x.Kind == GlazingSourceKind.User);
            }
        }

        [Fact]
        public void A_system_saved_while_the_list_is_open_joins_it_once_and_nothing_else_moves()
        {
            ApertureConstruction first = Save(BuilderFixture.Double());
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            int history = 0;
            ui.Modified += (sender, e) => modified++;
            ui.HistoryChanged += (sender, e) => history++;
            string before = Json(ui.JSAMObject);
            FakeGlazingEvaluator evaluator = new FakeGlazingEvaluator();

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services(evaluator)))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);
                List<Guid> listed = window.Candidates.Select(x => x.Guid).ToList();
                Assert.Contains(first.Guid, listed);

                // The library's Changed (raised by Save on this thread) refreshes the open list at once.
                ApertureConstruction second = Save(BuilderFixture.Triple());
                Wait(window);

                Assert.Single(window.Candidates, x => x.Guid == second.Guid && x.Candidate.Kind == GlazingSourceKind.User && x.Values != null);
                Assert.Single(window.Candidates, x => x.Guid == first.Guid);
                Assert.Equal(listed.Count + 1, window.Candidates.Count);
                Assert.All(listed, x => Assert.Single(window.Candidates, y => y.Guid == x));

                // Only the new system was calculated.
                Assert.Equal(new[] { second.Guid }, evaluator.Requests.Last().Batches.SelectMany(x => x.Guids));
            }

            Assert.Equal(before, Json(ui.JSAMObject));
            Assert.Equal(0, modified);
            Assert.Equal(0, history);
            Assert.False(ui.CanUndo);
        }

        [Fact]
        public void A_save_on_another_thread_reaches_the_open_list()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                ApertureConstruction saved = Task.Run(() => Save(BuilderFixture.Double())).Result;

                for (int i = 0; i < 200 && !window.Candidates.Any(x => x.Guid == saved.Guid && x.Values != null); i++)
                {
                    Thread.Sleep(25);
                }

                Assert.Single(window.Candidates, x => x.Guid == saved.Guid && x.Values != null);
            }
        }

        [Fact]
        public void Closing_discarding_or_applying_ends_the_following_of_the_library()
        {
            Save(BuilderFixture.Double());
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);
                Assert.Equal(1, Subscribers(library));

                window.CloseChange();
                Assert.Equal(0, Subscribers(library));
                Assert.Null(window.Glazing);

                // A save now reaches nobody and breaks nothing.
                Save(BuilderFixture.Triple());
                Assert.Null(window.Glazing);
                Assert.Empty(window.Candidates);
                window.RefreshUserGlazingAsync().Wait();

                // Reopened: followed once again; Discard ends it too.
                window.OpenChange();
                Wait(window);
                Assert.Equal(1, Subscribers(library));
                viewModel.Session.Discard();
                Assert.Equal(0, Subscribers(library));

                // Applied: the edit ends with it.
                window = OpenWindow(viewModel, ui.JSAMObject, parts);
                Assert.Equal(1, Subscribers(library));
                window.Glazing.SelectedGuid = GlazingFixture.BetterGuid;
                ThermalChangeResult result = viewModel.Apply(set => Modify.ApplyThermalChange(ui, set, x => { }, null));
                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(0, Subscribers(library));
            }
        }

        // ---- Choose by Guid (for the Builder's Save in E0-3) ---------------------------------------------------------

        [Fact]
        public void A_system_asked_for_by_guid_is_chosen_when_the_refresh_brings_it()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                Assert.Null(window.SelectedCandidate);

                // Saved without telling anyone (as another library instance would): the refresh brings it and chooses it.
                ApertureConstruction saved = SaveQuietly(BuilderFixture.Double());
                Assert.DoesNotContain(window.Candidates, x => x.Guid == saved.Guid);
                window.RefreshUserGlazingAsync(saved.Guid).Wait();

                Assert.Equal(saved.Guid, window.SelectedCandidate?.Guid);
                Assert.True(window.HasRequest);
                Assert.Equal(1, viewModel.Session.ChangeCount);
                Assert.Equal(GlazingSourceKind.User, viewModel.Session.BuildChangeSet().GlazingRequests.Single().Source.Kind);
            }
        }

        // A save through a second library on the same file: the panel's library raises nothing.
        private ApertureConstruction SaveQuietly(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = BuilderFixture.Library(directory).Save(draft);
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        [Fact]
        public void Choosing_before_the_save_arrives_and_choosing_one_already_listed_both_work()
        {
            ApertureConstruction listed = Save(BuilderFixture.Double());
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);

                // Already listed: chosen at once.
                window.SelectGlazing(listed.Guid);
                Assert.Equal(listed.Guid, window.SelectedCandidate?.Guid);

                // Asked for before the list has it: the choice waits (nothing else changes) and is made when the refresh brings it.
                ApertureConstruction later = SaveQuietly(BuilderFixture.Triple());
                window.SelectGlazing(later.Guid);
                Assert.Equal(listed.Guid, window.SelectedCandidate?.Guid);
                Assert.DoesNotContain(window.Candidates, x => x.Guid == later.Guid);

                window.RefreshUserGlazingAsync().Wait();
                Assert.Equal(later.Guid, window.SelectedCandidate?.Guid);

                // Saved through the panel's library (Changed refreshes the list at once), then asked for: chosen at once.
                ApertureConstruction saved = Save(BuilderFixture.Double("E0 Double 2"));
                window.SelectGlazing(saved.Guid);
                Assert.Equal(saved.Guid, window.SelectedCandidate?.Guid);
            }
        }

        [Fact]
        public void A_system_chosen_by_guid_is_shown_despite_the_target_only_while_it_is_the_choice()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                // Uw at most 1.0: the user system (Ug 1.50) is filtered out like any other.
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts, "1.0");
                ApertureConstruction saved = Save(BuilderFixture.Double());
                Wait(window);
                Assert.DoesNotContain(window.Candidates, x => x.Guid == saved.Guid);
                Assert.Null(window.Glazing.PinnedGuid);

                window.SelectGlazing(saved.Guid);

                GlazingCandidateRow row = Assert.Single(window.Candidates, x => x.Guid == saved.Guid);
                Assert.False(row.Passes);
                Assert.Equal(saved.Guid, window.SelectedCandidate?.Guid);
                Assert.Equal(saved.Guid, window.Glazing.PinnedGuid);
                Assert.Equal("✕ above target", row.StatusText);

                // Still there after the list refreshes for other reasons.
                window.RefreshUserGlazingAsync().Wait();
                window.GlazingTargetText = "0.99";
                Assert.Single(window.Candidates, x => x.Guid == saved.Guid);
                Assert.Equal(saved.Guid, window.SelectedCandidate?.Guid);

                // Another choice (here: none) ends it: the target hides the system again.
                window.Glazing.SelectedGuid = null;
                Assert.Null(window.Glazing.PinnedGuid);
                Assert.DoesNotContain(window.Candidates, x => x.Guid == saved.Guid);
                window.RefreshUserGlazingAsync().Wait();
                Assert.DoesNotContain(window.Candidates, x => x.Guid == saved.Guid);
            }
        }

        [Fact]
        public void Without_a_request_the_list_filters_and_chooses_exactly_as_before()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double());
            AnalyticalModel model = GlazingFixture.Model(3);

            using (GlazingViewModel plain = GlazingFixture.ViewModel(model))
            using (GlazingViewModel withUser = new GlazingViewModel(model, GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(), GlazingSource.FromUserLibrary(library)))
            {
                plain.InitializeAsync().Wait();
                withUser.InitializeAsync().Wait();

                foreach (string target in new[] { string.Empty, "1.4", "1.0" })
                {
                    plain.TargetText = target;
                    withUser.TargetText = target;

                    Assert.Equal(plain.Rows.Select(x => x.Guid), withUser.Rows.Where(x => x.Guid != saved.Guid).Select(x => x.Guid));
                    Assert.Equal(plain.SelectedGuid, withUser.SelectedGuid);
                    Assert.Null(withUser.PinnedGuid);
                }
            }
        }

        // ---- Nothing is written before Apply -------------------------------------------------------------------------

        [Fact]
        public void Reading_refreshing_choosing_and_checking_change_neither_the_model_nor_its_history_nor_the_library()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double());
            string library_Before = Hash();
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
                window.RefreshUserGlazingAsync().Wait();
                window.ScopeSelected = true;
                window.SelectGlazing(saved.Guid);

                // The preview and the check before Apply ran (on clones).
                Assert.True(window.HasRequest);
                Assert.NotNull(viewModel.Session.Diff);
                Assert.Null(viewModel.Session.ProposalError);
                Assert.Equal(1, viewModel.Session.ElementCount);
                Assert.Contains("Adds E0 Double and 4 materials to the model", window.ResultText);

                window.CloseChange();
            }

            Assert.Equal(before, Json(ui.JSAMObject));
            Assert.Equal(0, modified);
            Assert.Equal(0, history);
            Assert.False(ui.CanUndo);
            Assert.Equal(library_Before, Hash());
        }

        // ---- Apply and Undo ------------------------------------------------------------------------------------------

        [Fact]
        public void Applying_my_system_is_the_ordinary_glazing_change_one_commit_one_undo_and_the_library_keeps_it()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double());
            ApertureConstruction other = Save(BuilderFixture.Triple());
            string library_Before = Hash();

            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            ui.Modified += (sender, e) => modified++;
            // What Undo restores: the model as its history snapshot stored it (SAM's JSON round trip, which drops NaN-valued parameters).
            string before_Snapshot = Json(new AnalyticalModel(ui.JSAMObject.ToJsonObject()));
            int materials_Before = ui.JSAMObject.MaterialLibrary.GetMaterials().Count;
            List<string> materials_Saved = saved.PaneConstructionLayers.Concat(saved.FrameConstructionLayers).Select(x => x.Name).Distinct().ToList();

            ThermalChangeSet applied = null;
            ThermalChangeResult result;
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);
                window.ScopeSelected = true;
                window.SelectedCandidate = window.Candidates.Single(x => x.Guid == saved.Guid);
                Assert.Equal(0, modified);

                result = viewModel.Apply(set =>
                {
                    applied = set;
                    return Modify.ApplyThermalChange(ui, set, x => { }, null);
                });
            }

            // The existing change set and glazing core: one request, one model change, one Undo step.
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
            Assert.Equal(GlazingSourceKind.User, glazing.SourceKind);
            Assert.NotNull(glazing.BuilderProvenance);
            Assert.Equal(materials_Saved.OrderBy(x => x), glazing.MaterialNamesAdded.OrderBy(x => x));

            // Only the chosen system and its missing materials entered; only the selected window changed.
            AnalyticalModel changed = ui.JSAMObject;
            Assert.Contains(changed.AdjacencyCluster.GetApertureConstructions(), x => x.Guid == saved.Guid);
            Assert.DoesNotContain(changed.AdjacencyCluster.GetApertureConstructions(), x => x.Guid == other.Guid);
            Assert.Equal(materials_Before + materials_Saved.Count, changed.MaterialLibrary.GetMaterials().Count);
            Assert.Equal(new[] { parts.Windows[0] }, changed.AdjacencyCluster.GetApertures().Where(x => x.TypeGuid == saved.Guid).Select(x => x.Guid));
            Assert.Equal(parts.Windows.Count - 1, changed.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == GlazingFixture.CurrentGuid));
            Assert.NotNull(changed.AdjacencyCluster.GetApertureConstructions().First(x => x.Guid == saved.Guid).GetParameterSet(GlazingBuilderProvenance.ParameterSetName));
            Assert.Equal(library_Before, Hash());

            // One Undo restores the model exactly: no orphan system or material; the library still has both systems.
            Assert.True(ui.Undo());
            for (int i = 0; i < 100 && ui.JSAMObject.AdjacencyCluster.GetApertures().Any(x => x.TypeGuid == saved.Guid); i++)
            {
                Thread.Sleep(50);
            }

            Assert.Equal(before_Snapshot, Json(ui.JSAMObject));
            Assert.False(ui.CanUndo);
            Assert.DoesNotContain(ui.JSAMObject.AdjacencyCluster.GetApertureConstructions(), x => x.Guid == saved.Guid);
            Assert.All(materials_Saved, x => Assert.Null(ui.JSAMObject.MaterialLibrary.GetMaterial(x)));
            Assert.Equal(library_Before, Hash());
            Assert.Equal(new[] { saved.Guid, other.Guid }.OrderBy(x => x), library.Read().Systems.Select(x => x.Guid).OrderBy(x => x));

            // A new list offers it again.
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);
                Assert.Single(window.Candidates, x => x.Guid == saved.Guid && x.Candidate.Kind == GlazingSourceKind.User);
            }
        }

        // ---- A library that cannot be read ---------------------------------------------------------------------------

        [Fact]
        public void A_corrupt_library_is_a_note_in_the_list_and_every_other_source_and_apply_still_work()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(library.Path));
            File.WriteAllText(library.Path, "{ \"_type\": \"SAM.Core.ConstructionManager\", broken");
            string library_Before = Hash();

            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            ThermalSourceCatalog catalog = reader.Catalog();
            catalog.AddAsync(path).Wait();

            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            ui.Modified += (sender, e) => modified++;

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services(catalog: catalog)))
            {
                ThermalRowEditor window = OpenWindow(viewModel, ui.JSAMObject, parts);

                Assert.True(window.HasGlazingNotes);
                Assert.StartsWith("My glazing systems could not be used:", window.GlazingNotesText);
                Assert.Equal(GlazingPreviewStatus.Ready, window.Glazing.Status);
                Assert.Contains(window.Candidates, x => x.IsCurrent);
                Assert.Contains(window.Candidates, x => x.Candidate.Kind == GlazingSourceKind.Library);
                Assert.Contains(window.Candidates, x => x.Guid == SourceFixture.LoadedWindowGuid && x.Candidate.Kind == GlazingSourceKind.Loaded);
                Assert.DoesNotContain(window.Candidates, x => x.Candidate.Kind == GlazingSourceKind.User);

                window.Glazing.SelectedGuid = GlazingFixture.BetterGuid;
                Assert.True(window.HasRequest);
                Assert.Null(window.BlockReason);
                ThermalChangeResult result = viewModel.Apply(set => Modify.ApplyThermalChange(ui, set, x => { }, null));
                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(1, modified);
            }

            Assert.Equal(library_Before, Hash());
        }

        [Fact]
        public void A_missing_library_says_nothing_and_creates_nothing()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = OpenWindow(viewModel, parts.Model, parts);
                window.RefreshUserGlazingAsync().Wait();

                Assert.False(window.HasGlazingNotes);
                Assert.Equal(string.Empty, window.GlazingNotesText);
                Assert.Equal(GlazingPreviewStatus.Ready, window.Glazing.Status);
                Assert.DoesNotContain(window.Candidates, x => x.Candidate.Kind == GlazingSourceKind.User);
            }

            Assert.False(File.Exists(library.Path));
        }

        // ---- The GLAZING CHANGE report -------------------------------------------------------------------------------

        private static SetGlazingResult Applied(ApertureConstruction chosen, GlazingSource source)
        {
            AnalyticalModel model = GlazingFixture.Model(2);
            SetGlazingRequest request = new SetGlazingRequest()
            {
                SourceApertureConstructionGuid = GlazingFixture.CurrentGuid,
                ApertureConstruction = chosen,
                MaterialsToAdd = source.GetMaterials().Values.Where(x => model.MaterialLibrary.GetMaterial(x.Name) == null).ToList(),
                Scope = ThermalApplyScope.AllUsing,
                Values = new GlazingValues(1.05, 0.52, 0.75, 1.8),
                OldValues = GlazingFixture.Values[GlazingFixture.CurrentGuid],
                Source = source,
            };

            Modify.SetGlazing(model, request, null, out SetGlazingResult result);
            Assert.True(result.Succeeded, result.Error);
            return result;
        }

        [Fact]
        public void The_report_of_a_builder_system_names_its_guid_my_library_and_how_it_was_built_and_keeps_everything_it_had()
        {
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Frame.Width = 0.05;
            draft.BasedOnName = "SEED_GLZ";
            draft.BasedOnGuid = BuilderFixture.SeedGuid;
            ApertureConstruction saved = Save(draft);
            SetGlazingResult result = Applied(saved, GlazingSource.FromUserLibrary(library));

            string text = Query.GlazingChangeReportText(result, Query.GlazingCheckSummary(GlazingFixture.Model(2), result), null);

            Assert.Contains("Guid:         " + saved.Guid, text);
            Assert.Contains("Source:       My glazing systems (user glazing library)", text);
            Assert.Contains("Built from:   SAM Glazing System Builder, saved 2026-10-02 12:00 UTC; based on SEED_GLZ (" + BuilderFixture.SeedGuid + "); intended for WallExternal", text);
            Assert.Contains("Panes (outside -> inside): 1. " + BuilderFixture.Clear + " [4 mm, from " + BuilderFixture.Source + "] | 2. " + BuilderFixture.LowE + " [4 mm, from " + BuilderFixture.Source + "]", text);
            Assert.Contains("Gaps (outside -> inside): 1. Argon 16 mm (HTC ", text);
            Assert.Contains("W/m2K at 90 deg)", text);
            Assert.Contains("Frame: copied from SEED_GLZ, width 50 mm", text);

            // Everything the report had before is still there.
            foreach (string label in new[] { "Model:", "Applied:", "Method:", "Glazing:", "Pane:", "Frame:", "Ug:", "Uf:", "g:", "Light:", "Uw:", "Scope:", "Materials:", "CHECK (" })
            {
                Assert.Contains(Environment.NewLine + label, text);
            }

            Assert.Contains("GLZ -> E0 Double (added to the model; GLZ unchanged)", text);
            Assert.Contains("1.400 -> 1.050 W/m2K", text);
            Assert.Contains("4 added to the Material Library", text);

            // No folder of this machine: not the library's, not the temporary one.
            Assert.DoesNotContain(directory, text);
            Assert.DoesNotContain(":\\", text);
        }

        [Fact]
        public void The_report_never_shows_a_folder_even_when_a_label_was_given_as_a_path()
        {
            GlazingBuilderProvenance provenance = new GlazingBuilderProvenance()
            {
                CreatedUtc = new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc),
                BasedOnName = "SIM_EXT_GLZ",
                Panes = new List<GlazingBuilderPaneRecord>()
                {
                    new GlazingBuilderPaneRecord() { Position = 1, OriginalName = "Optifloat Clear 4mm", Material = GlazingFixture.LowE, SourceLabel = @"C:\Users\someone\OneDrive - Contoso\Glass\IGDB v76.tcd", Thickness = 0.004 },
                    new GlazingBuilderPaneRecord() { Position = 2, OriginalName = "K Glass", Material = GlazingFixture.Clear, SourceFile = @"\\server\share\people\someone\panes.json", Reversed = true, Thickness = 0.004 },
                },
                Gaps = new List<GlazingBuilderGapRecord>() { new GlazingBuilderGapRecord() { Position = 1, Gas = "Argon", Thickness = 0.012, HeatTransferCoefficient = 1.403, TiltDegrees = 90 } },
                Frame = "None",
            };

            ApertureConstruction chosen = GlazingFixture.System(new Guid("e0200000-0000-4000-8000-000000000003"), "PATHS", ApertureType.Window, GlazingFixture.LowE);
            chosen.Add(provenance.ToParameterSet());
            GlazingSource source = new GlazingSource(GlazingSourceKind.Loaded, "/home/someone/Company/glazing.json", new ConstructionManager(new[] { chosen }, null, Materials()));

            string text = Query.GlazingChangeReportText(Applied(chosen, source), null, null);

            Assert.Contains("Source:       glazing.json (added source)", text);
            Assert.Contains("1. Optifloat Clear 4mm [4 mm, from IGDB v76.tcd, saved as " + GlazingFixture.LowE + "]", text);
            Assert.Contains("2. K Glass (reversed) [4 mm, from panes.json, saved as " + GlazingFixture.Clear + "]", text);
            Assert.Contains("1. Argon 12 mm (HTC 1.403 W/m2K at 90 deg)", text);
            Assert.Contains("Frame: none", text);
            Assert.Contains("based on SIM_EXT_GLZ", text);
            foreach (string leak in new[] { "someone", "OneDrive", "Contoso", "server", "share", "home", "Company", ":\\", "\\\\" })
            {
                Assert.DoesNotContain(leak, text);
            }
        }

        [Theory]
        [InlineData(GlazingSourceKind.Model, "Model", "Model (existing model system)")]
        [InlineData(GlazingSourceKind.Library, "Default library", "Default library (SAM default library)")]
        [InlineData(GlazingSourceKind.Loaded, "database.tcd", "database.tcd (added source)")]
        public void The_report_of_any_other_system_names_its_source_and_says_it_was_not_built_with_the_builder(GlazingSourceKind kind, string label, string line)
        {
            ApertureConstruction chosen = GlazingFixture.System(GlazingFixture.LoadedGuid, "GLZ_Good", ApertureType.Window, GlazingFixture.LowE);
            GlazingSource source = new GlazingSource(kind, label, new ConstructionManager(new[] { chosen }, null, Materials()));

            string text = Query.GlazingChangeReportText(Applied(chosen, source), null, null);

            Assert.Contains("Guid:         " + GlazingFixture.LoadedGuid, text);
            Assert.Contains("Source:       " + line, text);
            Assert.Contains("Built from:   not made with the Glazing System Builder (no Builder provenance)", text);
        }

        [Fact]
        public void A_report_without_a_known_source_says_so()
        {
            ApertureConstruction chosen = GlazingFixture.System(GlazingFixture.LoadedGuid, "GLZ_Good", ApertureType.Window, GlazingFixture.LowE);
            SetGlazingResult result = Applied(chosen, new GlazingSource(GlazingSourceKind.Loaded, null, new ConstructionManager(new[] { chosen }, null, Materials())));

            Assert.Contains("Source:       ? (added source)", Query.GlazingChangeReportText(result, null, null));
        }
    }
}
