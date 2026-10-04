// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR4: "My constructions" (<see cref="UserConstructionLibrary"/>) as a source of every opaque row's alternatives. Its constructions are ordinary
    /// candidates - the same evaluator, cache, ±10 % window, 30-row limit, opaque guard and material check - listed as <b>My constructions</b> after the
    /// model and the default library and before the added sources, the first source of a Guid winning. The open list follows the library's
    /// <see cref="UserConstructionLibrary.Changed"/> on the thread it was created on (a Save, a Rename, a Remove), and a choice that was removed falls
    /// back to the generated variant. Reading, refreshing and choosing never touch the model; Apply is the existing construction change (one model
    /// change, one Undo) and its report says where the construction came from. These run as the app does - an STA thread with the WPF dispatcher
    /// (<c>[WpfFact]</c>) - and wait by pumping it, never by blocking it.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserConstructionCandidateTests : IDisposable
    {
        private readonly string directory = UserConstructionFixture.TempDirectory();
        private readonly UserConstructionLibrary library;

        public UserConstructionCandidateTests()
        {
            library = UserConstructionFixture.Library(directory);
        }

        public void Dispose()
        {
            UserConstructionFixture.Delete(directory);
        }

        // ---- Helpers -----------------------------------------------------------------------------------------------------

        private sealed class Setup : IDisposable
        {
            public AnalyticalModel Model;
            public Construction Current, Thick;
            public UValueViewModel UValue;
            public ConstructionAlternatives Alternatives;
            public FakeConstructionUValueEvaluator Evaluator;
            public List<Guid> Threads = new List<Guid>();
            public List<int> ThreadIds = new List<int>();

            public void Dispose()
            {
                Alternatives?.Dispose();
                UValue?.Dispose();
            }
        }

        private Setup Create(string target = "0.18", ThermalSourceCatalog catalog = null, UserConstructionLibrary user = null, bool defaultLibrary = true, Func<Setup, IEnumerable<Construction>> write = null)
        {
            Setup setup = new Setup();
            setup.Model = AlternativesFixture.Model(out setup.Current, out setup.Thick, out _, out _);
            if (write != null)
            {
                Write(write(setup));
            }

            setup.Evaluator = new FakeConstructionUValueEvaluator();
            setup.UValue = new UValueViewModel(setup.Model, setup.Current.Guid, new List<Guid>(), new ImmediateUValueEvaluator());
            setup.UValue.TargetText = target;
            setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));

            GlazingSource source = defaultLibrary ? AlternativesFixture.Library() : null;
            setup.Alternatives = new ConstructionAlternatives(setup.Model, setup.UValue, setup.Evaluator, new ConstructionUValueCache(), () => source, catalog, user ?? library);
            setup.Alternatives.PropertyChanged += (sender, e) => setup.ThreadIds.Add(Environment.CurrentManagedThreadId);
            setup.Alternatives.Refresh();
            setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));
            setup.ThreadIds.Clear();
            return setup;
        }

        private Construction Save(string name, double wool, string panelType = null)
        {
            Construction construction = UserConstructionFixture.Wall("Source", wool);
            if (panelType != null)
            {
                construction.SetValue(ConstructionParameter.DefaultPanelType, panelType);
            }

            UserConstructionSaveResult result = library.Save(construction, UserConstructionFixture.Materials(), name, UserConstructionFixture.Provenance(), new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        // A library file written by hand, to give constructions chosen Guids.
        private void Write(IEnumerable<Construction> constructions, IEnumerable<IMaterial> materials = null)
        {
            UserConstructionFixture.WriteLibrary(library, materials ?? UserConstructionFixture.Materials().GetMaterials(), constructions);
        }

        private static Construction WithGuid(Guid guid, string name, double wool)
        {
            return new Construction(guid, UserConstructionFixture.Wall(name, wool), name);
        }

        private static string[] Names(Setup setup) => setup.Alternatives.Rows.Select(x => x.Name).ToArray();

        private static string Hash(string path) => File.Exists(path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "absent";

        private static string Json(AnalyticalModel model) => model.ToJsonObject().ToJsonString();

        private static async Task Pump(Func<bool> condition, string what)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), "Timed out waiting for " + what);
                await Task.Delay(10);
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            }
        }

        // ---- The list ------------------------------------------------------------------------------------------------------

        [Fact]
        public void Saved_constructions_are_listed_as_My_constructions_with_their_U_value_where_they_come_from_and_that_they_are_not_in_the_model_yet()
        {
            Construction meets = Save("USER_MEETS", 0.125);
            Save("USER_FAR", 0.05);

            using (Setup setup = Create())
            {
                ConstructionAlternativeRow row = setup.Alternatives.Rows.Single(x => x.Name == "USER_MEETS");
                Assert.Equal(ConstructionAlternativeKind.User, row.Kind);
                Assert.Equal("My constructions", row.KindText);
                Assert.Equal("My constructions", row.SourceLabel);
                Assert.Equal("My constructions · not in the model yet", row.OriginText);
                Assert.Equal(meets.Guid, row.Guid);
                Assert.Equal(UValueFixture.U(0.125), row.ThermalTransmittance, 6);
                Assert.True(row.Meets);
                Assert.True(row.CanApply);
                Assert.Equal(0, row.UsedBy);
                Assert.Contains("125 I01_Mineral Wool", row.BuildUp);

                // The ±10 % window is the existing one: far above the target is not offered.
                Assert.DoesNotContain("USER_FAR", Names(setup));
                Assert.Equal("USER_MEETS (…" + meets.Guid.ToString().Substring(30) + ")", row.Tooltip.Split(Environment.NewLine.ToCharArray())[0]);
            }
        }

        [Fact]
        public void The_order_is_model_then_default_library_then_My_constructions_then_added_sources_and_the_first_guid_wins()
        {
            // A saved construction that is the model's (same Guid), the default library's, an added source's, and one of its own.
            Write(new[]
            {
                WithGuid(Guid.NewGuid(), "USER_OWN", 0.125),
                WithGuid(AlternativesFixture.LibraryThickGuid, "USER_LIBDUP", 0.13),
                WithGuid(SourceFixture.LoadedThickGuid, "USER_LOADEDDUP", 0.128),
            });

            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("database.tcd");
            reader.Add(path, SourceFixture.Loaded());
            ThermalSourceCatalog catalog = reader.Catalog();
            catalog.AddAsync(path).Wait(TimeSpan.FromSeconds(10));

            using (Setup setup = Create(catalog: catalog))
            {
                ConstructionAlternativeKind Kind(string name) => setup.Alternatives.Rows.Single(x => x.Name == name).Kind;

                Assert.Equal(ConstructionAlternativeKind.Model, setup.Alternatives.Rows.Single(x => x.Name == "MODEL_THICK" && x.Kind == ConstructionAlternativeKind.Model).Kind);
                Assert.Equal(ConstructionAlternativeKind.Library, Kind("LIB_THICK"));
                Assert.Equal(ConstructionAlternativeKind.User, Kind("USER_OWN"));
                Assert.Equal(ConstructionAlternativeKind.Loaded, Kind("LOADED_AEROGEL"));

                // The default library keeps a Guid the user's copy also has; the user's keeps one an added source also has: nothing is offered twice.
                Assert.DoesNotContain("USER_LIBDUP", Names(setup));
                Assert.Equal(AlternativesFixture.LibraryThickGuid, setup.Alternatives.Rows.Single(x => x.Name == "LIB_THICK").Guid);
                Assert.Contains("USER_LOADEDDUP", Names(setup));
                Assert.DoesNotContain("LOADED_THICK", Names(setup));
                Assert.Equal(SourceFixture.LoadedThickGuid, setup.Alternatives.Rows.Single(x => x.Name == "USER_LOADEDDUP").Guid);
                Assert.Equal(setup.Alternatives.Rows.Where(x => x.Guid.HasValue).Count(), setup.Alternatives.Rows.Where(x => x.Guid.HasValue).Select(x => x.Guid).Distinct().Count());

                // The pools are calculated as one batch each: the model's, the library's, My constructions', the added source's.
                Assert.Equal(4, setup.Evaluator.Requests.Count);
            }
        }

        [Fact]
        public void A_construction_the_model_already_has_by_guid_is_the_models_even_when_it_was_saved_from_there()
        {
            // The saved construction has the Guid of the model's MODEL_THICK (it was saved from there and the model still has it).
            using (Setup setup = Create(write: s => new[] { WithGuid(s.Thick.Guid, "USER_COPY_OF_MODEL", 0.12) }))
            {
                Assert.DoesNotContain("USER_COPY_OF_MODEL", Names(setup));
                Assert.Contains(setup.Alternatives.Rows, x => x.Guid == setup.Thick.Guid && x.Kind == ConstructionAlternativeKind.Model);
            }
        }

        [Fact]
        public void Saved_constructions_follow_the_existing_target_window_made_for_notes_and_the_row_limit()
        {
            Save("USER_MEETS", 0.125);                  // U 0.177: meets 0.18
            Save("USER_CLOSE", 0.115);                  // U 0.1905: within 10 %
            Save("USER_FAR", 0.05);                     // U 0.377: not offered
            Save("USER_ROOF", 0.135, "Roof");           // U 0.1654: meets, but made for roofs

            using (Setup setup = Create(defaultLibrary: false))
            {
                ConstructionAlternativeRow meets = setup.Alternatives.Rows.Single(x => x.Name == "USER_MEETS");
                ConstructionAlternativeRow close = setup.Alternatives.Rows.Single(x => x.Name == "USER_CLOSE");
                ConstructionAlternativeRow roof = setup.Alternatives.Rows.Single(x => x.Name == "USER_ROOF");
                Assert.True(meets.Meets);
                Assert.False(close.Meets);
                Assert.Contains("above the target", close.StatusText);
                Assert.Contains(roof.Warnings, x => x.Contains("made for roofs"));
                Assert.DoesNotContain("USER_FAR", Names(setup));

                // The same ordering rule as every pool: those that meet first, the ones made for the panels' own group before the others, then the near misses.
                string[] user = setup.Alternatives.Rows.Where(x => x.Kind == ConstructionAlternativeKind.User).Select(x => x.Name).ToArray();
                Assert.Equal(new[] { "USER_MEETS", "USER_ROOF", "USER_CLOSE" }, user);
            }

            for (int i = 0; i < 35; i++)
            {
                Save("USER_MANY_" + i, 0.14 + i * 0.0001);
            }

            using (Setup setup = Create())
            {
                Assert.Equal(ConstructionAlternatives.MaxRows, setup.Alternatives.ExistingCount);
            }
        }

        [Fact]
        public void A_transparent_or_gas_construction_in_the_file_is_never_offered_as_an_opaque_alternative_and_the_good_one_still_is()
        {
            Construction glass = UserConstructionFixture.Glass(out MaterialLibrary glassMaterials);
            Construction gas = UserConstructionFixture.GasOnly(out MaterialLibrary gasMaterials);
            List<IMaterial> materials = UserConstructionFixture.Materials().GetMaterials().Concat(glassMaterials.GetMaterials()).Concat(gasMaterials.GetMaterials()).GroupBy(x => x.Name).Select(x => x.First()).ToList();
            Write(new[] { glass, gas, WithGuid(Guid.NewGuid(), "USER_GOOD", 0.125) }, materials);

            using (Setup setup = Create())
            {
                Assert.Contains("USER_GOOD", Names(setup));
                Assert.DoesNotContain("GLZ", Names(setup));
                Assert.DoesNotContain("GAS", Names(setup));
            }
        }

        [Fact]
        public void A_saved_construction_whose_material_differs_from_the_models_is_listed_but_blocked_with_the_reason_and_the_model_is_untouched()
        {
            List<IMaterial> materials = UserConstructionFixture.Materials().GetMaterials().Where(x => x.Name != UValueFixture.Wool).Concat(new[] { (IMaterial)UserConstructionFixture.OtherWool() }).ToList();
            Write(new[] { WithGuid(Guid.NewGuid(), "USER_CLASH", 0.125) }, materials);

            using (Setup setup = Create())
            {
                string before = Json(setup.Model);
                ConstructionAlternativeRow row = setup.Alternatives.Rows.Single(x => x.Name == "USER_CLASH");

                Assert.False(row.CanApply);
                Assert.Contains("differs from the model's material of the same name", row.BlockReason);
                Assert.Contains(row.BlockReason, row.WarningText);

                setup.Alternatives.SelectedRow = row;
                Assert.Null(setup.Alternatives.CreateRequest());
                Assert.False(setup.Alternatives.ApplyEnabled);
                Assert.Equal(before, Json(setup.Model));
            }
        }

        [Fact]
        public void An_unreadable_library_is_a_note_the_other_sources_still_work_and_the_file_is_left_as_it_is()
        {
            File.WriteAllText(library.Path, "this is not a library");
            string before = Hash(library.Path);

            using (Setup setup = Create())
            {
                Assert.Contains(setup.Alternatives.Notes, x => x.StartsWith("My constructions could not be used"));
                Assert.Contains("LIB_THICK", Names(setup));
                Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Kind == ConstructionAlternativeKind.User);
                Assert.Equal(ConstructionAlternativesStatus.Ready, setup.Alternatives.Status);
            }

            Assert.Equal(before, Hash(library.Path));
        }

        [Fact]
        public void With_no_library_file_there_are_no_notes_nothing_is_created_and_reading_changes_nothing_in_the_model()
        {
            using (Setup setup = Create())
            {
                string before = Json(setup.Model);
                setup.Alternatives.Refresh();

                Assert.Empty(setup.Alternatives.Notes);
                Assert.False(File.Exists(library.Path));
                Assert.False(File.Exists(library.ArchivePath));
                Assert.Equal(before, Json(setup.Model));
            }
        }

        // ---- Following the library -------------------------------------------------------------------------------------------

        [WpfFact]
        public async Task A_save_reaches_the_open_list_which_asks_tas_only_for_the_new_construction()
        {
            using (Setup setup = Create())
            {
                Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Kind == ConstructionAlternativeKind.User);
                int constructions = setup.Evaluator.ConstructionCount;
                Assert.Equal(1, UserConstructionFixture.Subscribers(library));

                // A wool thickness no other construction of the fixture has: the U-values are cached by content, so a copy of a known one is never asked twice.
                Construction saved = Save("USER_NEW", 0.1275);
                await Pump(() => setup.Alternatives.Rows.Any(x => x.Guid == saved.Guid), "the saved construction to appear");

                ConstructionAlternativeRow row = Assert.Single(setup.Alternatives.Rows, x => x.Guid == saved.Guid);
                Assert.Equal(ConstructionAlternativeKind.User, row.Kind);
                Assert.Equal("USER_NEW", row.Name);
                Assert.Equal(constructions + 1, setup.Evaluator.ConstructionCount);
            }

            Assert.Equal(0, UserConstructionFixture.Subscribers(library));
        }

        [WpfFact]
        public async Task A_rename_reaches_the_open_list_with_the_same_values_and_keeps_the_choice_and_a_remove_drops_it_and_resets_the_choice_to_the_generated_variant()
        {
            Construction a = Save("Old name", 0.125);
            Construction b = Save("Stays", 0.14);

            using (Setup setup = Create())
            {
                setup.Alternatives.SelectedGuid = a.Guid;
                Assert.True(setup.Alternatives.ExistingChosen);
                double u = setup.Alternatives.Rows.Single(x => x.Guid == a.Guid).ThermalTransmittance;
                int asked = setup.Evaluator.ConstructionCount;

                Assert.True(library.Rename(a.Guid, "New name").Succeeded);
                await Pump(() => setup.Alternatives.Rows.Any(x => x.Guid == a.Guid && x.Name == "New name"), "the renamed construction to appear");

                ConstructionAlternativeRow renamed = setup.Alternatives.Rows.Single(x => x.Guid == a.Guid);
                Assert.Equal(u, renamed.ThermalTransmittance);
                Assert.Equal(asked, setup.Evaluator.ConstructionCount);
                Assert.Equal(a.Guid, setup.Alternatives.SelectedGuid);
                Assert.Contains("New name", setup.Alternatives.PreviewText);

                Assert.True(library.Remove(a.Guid).Succeeded);
                await Pump(() => setup.Alternatives.Rows.All(x => x.Guid != a.Guid), "the removed construction to leave the list");

                Assert.Null(setup.Alternatives.SelectedGuid);
                Assert.True(setup.Alternatives.SelectedRow.IsGenerated);
                Assert.False(setup.Alternatives.ExistingChosen);
                Assert.Contains(setup.Alternatives.Rows, x => x.Guid == b.Guid);
                Assert.Equal(asked, setup.Evaluator.ConstructionCount);
            }
        }

        [WpfFact]
        public async Task A_change_made_on_another_thread_refreshes_the_list_on_the_thread_it_was_created_on()
        {
            using (Setup setup = Create())
            {
                int thread = Environment.CurrentManagedThreadId;
                Construction saved = await Task.Run(() => Save("FROM_ANOTHER_THREAD", 0.125));

                await Pump(() => setup.Alternatives.Rows.Any(x => x.Guid == saved.Guid), "the construction saved on another thread to appear");

                Assert.NotEmpty(setup.ThreadIds);
                Assert.All(setup.ThreadIds, x => Assert.Equal(thread, x));

                Construction removed = saved;
                await Task.Run(() => library.Remove(removed.Guid));
                await Pump(() => setup.Alternatives.Rows.All(x => x.Guid != removed.Guid), "the construction removed on another thread to leave");
                Assert.All(setup.ThreadIds, x => Assert.Equal(thread, x));
            }
        }

        [WpfFact]
        public async Task A_disposed_list_ignores_the_library_and_a_change_still_in_flight_when_it_is_disposed_does_nothing()
        {
            Setup setup = Create();

            // A change from another thread is posted to this one; the list is disposed before the post runs (this thread is blocked until the save is done).
            Construction inFlight = Task.Run(() => Save("IN_FLIGHT", 0.125)).GetAwaiter().GetResult();
            setup.Alternatives.Dispose();
            await Pump(() => true, "the posted refresh to run");
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Guid == inFlight.Guid);

            // And a change after the dispose reaches nobody.
            Construction late = Save("LATE", 0.125);
            await Pump(() => true, "nothing");
            Assert.Equal(0, UserConstructionFixture.Subscribers(library));
            Assert.DoesNotContain(setup.Alternatives.Rows, x => x.Guid == late.Guid);
            setup.UValue.Dispose();
        }

        [Fact]
        public void A_target_that_is_not_typed_asks_nothing_and_a_library_change_before_the_list_is_built_is_read_when_it_is()
        {
            using (Setup setup = Create(target: string.Empty))
            {
                Assert.Equal(ConstructionAlternativesStatus.Idle, setup.Alternatives.Status);
                Save("USER_EARLY", 0.125);
                Assert.Empty(setup.Alternatives.Rows);
                Assert.Equal(0, setup.Evaluator.Requests.Count);

                setup.UValue.TargetText = "0.18";
                setup.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                setup.Alternatives.Refresh();
                setup.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));

                Assert.Contains("USER_EARLY", Names(setup));
            }
        }
    }
}
