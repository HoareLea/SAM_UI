// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR2: the model-free view-model of "My library" (<see cref="UserLibraryViewModel"/>) - the rows with what was recorded at save, the
    /// library's naming rule applied while typing, Rename (label only), Remove only after a confirmation (to the archive, with the text the user
    /// sees), the request to open a system in the Builder, an unreadable library (a note; nothing can be changed and nothing is written), following
    /// the library's Changed event on the thread it was created on, and the structural proof that it holds no analytical model. It subscribes to the
    /// library's Changed and its refresh is posted to the creating thread, so these run as the app does: on an STA thread with the WPF dispatcher
    /// (<c>[WpfFact]</c>), never under xUnit's multi-threaded context.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserLibraryViewModelTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;

        public UserLibraryViewModelTests()
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

        private ApertureConstruction Save(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        private static string Hash(string path) => File.Exists(path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "absent";

        private static int Subscribers(UserGlazingLibrary userGlazingLibrary)
        {
            FieldInfo field = typeof(UserGlazingLibrary).GetField(nameof(UserGlazingLibrary.Changed), BindingFlags.Instance | BindingFlags.NonPublic);
            return (field.GetValue(userGlazingLibrary) as Delegate)?.GetInvocationList().Length ?? 0;
        }

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

        // ---- What is listed ----------------------------------------------------------------------------------------------

        [WpfFact]
        public void A_missing_library_is_an_empty_list_that_says_how_to_save_one_and_creates_nothing()
        {
            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                Assert.Empty(viewModel.Rows);
                Assert.False(viewModel.HasRows);
                Assert.Equal(UserGlazingLibraryState.Missing, viewModel.State);
                Assert.True(viewModel.ShowEmptyText);
                Assert.Contains("Save as predefined", viewModel.EmptyText);
                Assert.False(viewModel.HasNote);
                Assert.True(viewModel.CanChange);
                Assert.False(viewModel.CanRename);
                Assert.False(viewModel.CanRemove);
                Assert.False(viewModel.CanOpenInBuilder);
                Assert.Equal("0 saved systems", viewModel.CountText);
                Assert.Contains("Glazing Systems.removed.json", viewModel.ArchiveText);
            }

            Assert.False(File.Exists(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
        }

        [WpfFact]
        public void Saved_systems_are_listed_by_name_with_what_was_recorded_when_they_were_saved()
        {
            GlazingSystemDraft draft = BuilderFixture.Double("B window");
            draft.BasedOnName = "SEED_GLZ";
            draft.BasedOnGuid = BuilderFixture.SeedGuid;
            draft.Frame.Width = 0.05;
            ApertureConstruction b = Save(draft);
            ApertureConstruction a = Save(BuilderFixture.Triple("a window"));

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                Assert.Equal(new[] { "a window", "B window" }, viewModel.Rows.Select(x => x.Name));
                Assert.Equal("2 saved systems", viewModel.CountText);
                Assert.False(viewModel.ShowEmptyText);

                UserLibraryEntryRow row = viewModel.Rows.Single(x => x.Guid == b.Guid);
                Assert.Equal(b.Guid.ToString().Substring(30), row.ShortId);
                Assert.Equal("Ug 1.05 · g 0.52 · LT 0.75 · Uf 1.80", row.ValuesText);
                Assert.Equal("2026-10-02", row.SavedText);
                Assert.Equal("based on SEED_GLZ", row.BasedOnText);
                Assert.Contains(BuilderFixture.LowE, row.PaneBuildUp);

                string details = row.DetailsText;
                Assert.Contains("B window  [" + row.ShortId + "]", details);
                Assert.Contains("At save: Ug 1.05", details);
                Assert.Contains("Tas TCD", details);
                Assert.Contains("SAM Glazing System Builder, saved 2026-10-02 12:00 UTC; based on SEED_GLZ (" + BuilderFixture.SeedGuid + ")", details);
                Assert.Contains("Panes (outside -> inside): 1. " + BuilderFixture.Clear, details);
                Assert.Contains("Gaps (outside -> inside): 1. Argon 16 mm", details);
                Assert.Contains("Frame: copied from SEED_GLZ, width 50 mm", details);

                // Labels and file names only: no folder of this machine.
                Assert.DoesNotContain(directory, details);
                Assert.DoesNotContain(":\\", details);
            }
        }

        [WpfFact]
        public void A_system_without_builder_provenance_says_so_instead_of_inventing_values()
        {
            ApertureConstruction foreign = GlazingFixture.System(new Guid("e2000000-0000-4000-8000-000000000001"), "Imported", ApertureType.Window, GlazingFixture.LowE);
            MaterialLibrary materials = new MaterialLibrary("User");
            materials.Add(GlazingFixture.ClearGlass());
            materials.Add(GlazingFixture.LowEGlass());
            materials.Add(GlazingFixture.ArgonGas());
            materials.Add(GlazingFixture.FrameOpaque());
            File.WriteAllText(library.Path, new ConstructionManager(new[] { foreign }, null, materials) { Name = UserGlazingLibrary.LibraryName }.ToJsonObject().ToJsonString());

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                UserLibraryEntryRow row = Assert.Single(viewModel.Rows);
                Assert.Equal("values not recorded", row.ValuesText);
                Assert.Equal(string.Empty, row.SavedText);
                Assert.Equal(string.Empty, row.BasedOnText);
                Assert.Contains("Not made with the Glazing System Builder", row.DetailsText);
                Assert.Null(row.Provenance);
            }
        }

        [WpfFact]
        public void An_unreadable_library_is_a_note_nothing_can_be_changed_and_the_file_is_left_as_it_is()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(library.Path));
            File.WriteAllText(library.Path, "{ this is not json");
            string hash = Hash(library.Path);

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                Assert.Equal(UserGlazingLibraryState.Unreadable, viewModel.State);
                Assert.True(viewModel.HasNote);
                Assert.StartsWith("My glazing systems could not be used:", viewModel.Note);
                Assert.False(viewModel.CanChange);
                Assert.False(viewModel.ShowEmptyText);
                Assert.Empty(viewModel.Rows);
                Assert.False(viewModel.BeginRename());
                Assert.False(viewModel.Remove(x => true));
                Assert.False(viewModel.RequestOpenInBuilder());
            }

            Assert.Equal(hash, Hash(library.Path));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.False(File.Exists(library.BackupPath));
        }

        // ---- Rename -----------------------------------------------------------------------------------------------------

        [WpfFact]
        public void Rename_applies_the_librarys_naming_rule_while_typing_and_then_changes_the_name_only()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            Save(BuilderFixture.Triple("Second"));
            string json = library.Read().Systems.Single(x => x.Guid == first.Guid).ToJsonObject().ToJsonString();

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                Assert.False(viewModel.BeginRename());                    // nothing selected
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == first.Guid);
                Assert.True(viewModel.BeginRename());
                Assert.True(viewModel.IsRenaming);
                Assert.Equal("First", viewModel.RenameText);
                Assert.True(viewModel.CanCommitRename);                   // its own name is fine

                viewModel.RenameText = "   ";
                Assert.Equal("The system needs a name.", viewModel.RenameError);
                Assert.False(viewModel.CanCommitRename);
                Assert.False(viewModel.CommitRename());

                viewModel.RenameText = "  second ";
                Assert.Equal("A system named 'second' is already in My glazing systems.", viewModel.RenameError);
                Assert.False(viewModel.CanCommitRename);

                viewModel.RenameText = "FIRST";                            // only the case
                Assert.Equal(string.Empty, viewModel.RenameError);

                viewModel.RenameText = "  Renamed  ";
                Assert.True(viewModel.CommitRename());

                Assert.False(viewModel.IsRenaming);
                Assert.Equal("Renamed to 'Renamed'.", viewModel.Message);
                Assert.Equal(new[] { "Renamed", "Second" }, viewModel.Rows.Select(x => x.Name));
                Assert.Equal(first.Guid, viewModel.SelectedRow?.Guid);        // the selection follows the Guid, not the name
            }

            JsonObjectComparison.AssertSameExceptName(json, library.Read().Systems.Single(x => x.Guid == first.Guid).ToJsonObject().ToJsonString(), "Renamed");
        }

        [WpfFact]
        public void Cancelling_a_rename_or_selecting_another_system_ends_it_and_writes_nothing()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            ApertureConstruction second = Save(BuilderFixture.Triple("Second"));
            string hash = Hash(library.Path);

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == first.Guid);
                viewModel.BeginRename();
                viewModel.RenameText = "Typed but cancelled";
                viewModel.CancelRename();
                Assert.False(viewModel.IsRenaming);

                viewModel.BeginRename();
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == second.Guid);
                Assert.False(viewModel.IsRenaming);
            }

            Assert.Equal(hash, Hash(library.Path));
        }

        [WpfFact]
        public void A_rename_the_library_refuses_is_a_message_and_changes_nothing()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            Save(BuilderFixture.Triple("Second"));

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == first.Guid);
                viewModel.BeginRename();
                viewModel.RenameText = "Taken meanwhile";

                // Another SAM process saves that name after the list was read (nothing tells this list): the library's check, under its lock, still stops it.
                Assert.True(BuilderFixture.Library(directory).Save(BuilderFixture.Triple("Taken meanwhile")).Succeeded);
                string hash = Hash(library.Path);
                viewModel.RenameText = "Taken meanwhile";

                Assert.False(viewModel.CommitRename());
                Assert.Contains("is already in My glazing systems", viewModel.Message);
                Assert.Equal(hash, Hash(library.Path));
                Assert.Equal("First", library.Read().Systems.Single(x => x.Guid == first.Guid).Name);
            }
        }

        // ---- Remove -----------------------------------------------------------------------------------------------------

        [WpfFact]
        public void Remove_asks_first_with_the_name_the_short_id_and_what_happens_and_does_nothing_unless_confirmed()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Doomed"));
            string hash = Hash(library.Path);

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                viewModel.SelectedRow = viewModel.Rows.Single();
                string asked = null;

                Assert.False(viewModel.Remove(text =>
                {
                    asked = text;
                    return false;
                }));

                Assert.Contains("'Doomed' [" + saved.Guid.ToString().Substring(30) + "]", asked);
                Assert.Contains("Models that already use it keep their own copy", asked);
                Assert.Contains("not deleted", asked);
                Assert.Contains("Glazing Systems.removed.json", asked);
                Assert.Equal(hash, Hash(library.Path));
                Assert.False(File.Exists(library.ArchivePath));
                Assert.Single(viewModel.Rows);

                Assert.False(viewModel.Remove(null));                      // no way to ask: not removed
                Assert.Equal(hash, Hash(library.Path));
            }
        }

        [WpfFact]
        public void A_confirmed_remove_moves_the_system_to_the_archive_and_the_list_follows()
        {
            ApertureConstruction doomed = Save(BuilderFixture.Double("Doomed"));
            ApertureConstruction stays = Save(BuilderFixture.Triple("Stays"));

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == doomed.Guid);

                Assert.True(viewModel.Remove(text => true));

                Assert.Equal(new[] { stays.Guid }, viewModel.Rows.Select(x => x.Guid));
                Assert.Null(viewModel.SelectedRow);
                Assert.False(viewModel.CanRemove);
                Assert.Equal("Removed 'Doomed'; it is kept in the archive.", viewModel.Message);
                Assert.Equal(new[] { doomed.Guid }, ReadSystems(library.ArchivePath).Select(x => x.Guid));

                // A row that is not the selected one can be removed too (the list's context menu).
                Assert.True(viewModel.Remove(text => true, viewModel.Rows.Single()));
                Assert.Empty(viewModel.Rows);
                Assert.True(viewModel.ShowEmptyText);
            }
        }

        [WpfFact]
        public void A_remove_that_fails_is_a_message_and_the_system_stays()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Stays"));
            library.BeforeWrite = path =>
            {
                if (path == library.ArchivePath)
                {
                    throw new IOException("archive disk full");
                }
            };

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                viewModel.SelectedRow = viewModel.Rows.Single();

                Assert.False(viewModel.Remove(text => true));

                Assert.Contains("Nothing was removed", viewModel.Message);
                Assert.Equal(new[] { saved.Guid }, viewModel.Rows.Select(x => x.Guid));
                Assert.Equal(saved.Guid, library.Read().Systems.Single().Guid);
            }
        }

        private static List<ApertureConstruction> ReadSystems(string path)
        {
            ConstructionManager constructionManager = UserLibraryFile.Parse(File.ReadAllText(path), out string error);
            Assert.True(constructionManager != null, error);
            return constructionManager.ApertureConstructions ?? new List<ApertureConstruction>();
        }

        // ---- Open in Builder --------------------------------------------------------------------------------------------

        [WpfFact]
        public void Open_in_Builder_is_a_request_for_the_selected_system_and_changes_nothing_here()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Seed me"));
            string hash = Hash(library.Path);

            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(library))
            {
                Assert.False(viewModel.RequestOpenInBuilder());            // nothing selected, nobody listening

                List<UserLibraryEntryRow> requests = new List<UserLibraryEntryRow>();
                viewModel.OpenInBuilderRequested += (sender, row) => requests.Add(row);
                Assert.False(viewModel.RequestOpenInBuilder());            // nothing selected

                viewModel.SelectedRow = viewModel.Rows.Single();
                Assert.True(viewModel.CanOpenInBuilder);
                Assert.True(viewModel.RequestOpenInBuilder());

                UserLibraryEntryRow requested = Assert.Single(requests);
                Assert.Equal(saved.Guid, requested.Guid);
                Assert.Equal(saved.Guid, requested.ApertureConstruction.Guid);
            }

            Assert.Equal(hash, Hash(library.Path));
        }

        // ---- Following the library ---------------------------------------------------------------------------------------

        [WpfFact]
        public async Task The_list_follows_the_librarys_changes_from_any_thread_and_stops_when_disposed()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            UserLibraryViewModel viewModel = new UserLibraryViewModel(library);
            Assert.Equal(1, Subscribers(library));
            viewModel.SelectedRow = viewModel.Rows.Single();

            // A save on another thread: the list refreshes on the thread it was created on, and keeps the selection.
            ApertureConstruction second = await Task.Run(() => Save(BuilderFixture.Triple("Second")));
            await Pump(() => viewModel.Rows.Count == 2, "the new system to be listed");
            Assert.Equal(first.Guid, viewModel.SelectedRow?.Guid);

            // A rename and a remove made elsewhere in this process.
            Assert.True(library.Rename(second.Guid, "Second, renamed").Succeeded);
            await Pump(() => viewModel.Rows.Any(x => x.Name == "Second, renamed"), "the rename to show");
            Assert.True(library.Remove(first.Guid).Succeeded);
            await Pump(() => viewModel.Rows.Count == 1, "the removed system to leave the list");
            Assert.Null(viewModel.SelectedRow);

            viewModel.Dispose();
            Assert.Equal(0, Subscribers(library));
            Save(BuilderFixture.Double("After dispose"));
            Assert.Single(viewModel.Rows);
        }

        // ---- It holds no model --------------------------------------------------------------------------------------------

        [Fact]
        public void The_view_model_and_its_window_hold_no_analytical_model()
        {
            Type[] types =
            {
                typeof(UserLibraryViewModel), typeof(UserLibraryEntryRow), typeof(UserLibraryWindow), typeof(UserGlazingLibrary), typeof(UserLibraryFile), typeof(UserLibraryArchive),
            };

            Assert.Empty(BuilderSurface.ModelReferences(types));

            // The scan is not vacuous: the same scan finds a model in a type that has one.
            Assert.NotEmpty(BuilderSurface.ModelReferences(typeof(ThermalPerformanceViewModel)));
        }
    }

    internal static class JsonObjectComparison
    {
        /// <summary>Asserts two ApertureConstruction JSON texts are identical once the first has its Name changed to <paramref name="name"/>.</summary>
        public static void AssertSameExceptName(string before, string after, string name)
        {
            System.Text.Json.Nodes.JsonObject json = (System.Text.Json.Nodes.JsonObject)System.Text.Json.Nodes.JsonNode.Parse(before);
            json["Name"] = name;
            Assert.Equal(json.ToJsonString(), after);
        }
    }
}
