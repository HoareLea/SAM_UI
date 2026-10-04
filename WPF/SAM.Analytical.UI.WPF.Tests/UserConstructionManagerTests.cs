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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR4: the Constructions tab of "My library". The model-free view-model (<see cref="UserConstructionLibraryViewModel"/>) lists the saved constructions
    /// with what was recorded at save (build-up, U-value and its heat-flow basis, date, where they were saved from), applies the library's naming rule while
    /// typing, renames (label only), removes only after a confirmation (to the archive), reports an unreadable library as a note, and follows the library's
    /// Changed on the thread it was created on. The real window shows it in a tab next to Glazing systems, with an empty state, no opaque edit, and no
    /// analytical model anywhere: nothing it does can change a model or add an Undo step. These run on an STA thread with the WPF dispatcher
    /// (<c>[WpfFact]</c>); each test uses its own temporary library.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserConstructionManagerTests : IDisposable
    {
        private readonly string directory = UserConstructionFixture.TempDirectory();
        private readonly UserConstructionLibrary library;

        public UserConstructionManagerTests()
        {
            library = UserConstructionFixture.Library(directory);
        }

        public void Dispose()
        {
            UserConstructionFixture.Delete(directory);
        }

        private Construction Save(string name, double wool = 0.13, UserConstructionProvenance provenance = null)
        {
            UserConstructionSaveResult result = library.Save(UserConstructionFixture.Wall("Source", wool), UserConstructionFixture.Materials(), name, provenance ?? UserConstructionFixture.Provenance(), new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        private static string Hash(string path) => File.Exists(path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "absent";

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        private static async Task Pump(Func<bool> condition, string what)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), "Timed out waiting for " + what);
                await Task.Delay(10);
                Flush();
            }
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
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

        private static T ById<T>(DependencyObject root, string id) where T : FrameworkElement
        {
            return Descendants<T>(root).FirstOrDefault(x => AutomationProperties.GetAutomationId(x) == id);
        }

        private static void Press(Button button)
        {
            Assert.True(button.IsEnabled, button.Content + " is disabled");
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Flush();
        }

        // ---- The view-model ----------------------------------------------------------------------------------------------------

        [WpfFact]
        public void Rows_say_what_each_construction_is_made_of_its_U_value_and_basis_when_it_was_saved_and_where_it_came_from()
        {
            UserConstructionProvenance provenance = UserConstructionFixture.Provenance(UserConstructionOrigin.GeneratedVariant, "SIM_EXT_SLD");
            Construction generated = Save("Generated wall", 0.13, provenance);
            Save("Plain wall", 0.2, new UserConstructionProvenance() { SavedFrom = UserConstructionOrigin.AddedSource, SavedFromSource = "Constructions.tcd", BasedOnName = "NCM wall" });
            UserConstructionFixture.WriteLibrary(library, library.Read().ConstructionManager.Materials, library.Read().Constructions.Concat(new[] { new Construction(Guid.NewGuid(), UserConstructionFixture.Wall("Handmade", 0.1), "Handmade") }));

            using (UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(library))
            {
                Assert.Equal(new[] { "Generated wall", "Handmade", "Plain wall" }, viewModel.Rows.Select(x => x.Name).ToArray());
                Assert.Equal("3 saved constructions", viewModel.CountText);

                UserConstructionEntryRow row = viewModel.Rows.Single(x => x.Guid == generated.Guid);
                Assert.Equal("50 Air / 12 Cement Particleboard / 130 I01_Mineral Wool / 50 Air / 3 Rainscreen", row.BuildUp);
                Assert.Equal("U 0.180 W/m²K", row.UValueText);
                Assert.Contains("Horizontal heat flow", row.HeatFlowBasisText);
                Assert.Equal("2026-10-04", row.SavedText);
                Assert.Equal("generated variant · based on SIM_EXT_SLD", row.SourceText);
                Assert.Equal(generated.Guid.ToString().Substring(30), row.ShortId);
                Assert.Contains("Saved from: generated variant", row.DetailsText);
                Assert.Contains("Target: 0.180 W/m²K", row.DetailsText);
                Assert.Contains("Route: Test route", row.DetailsText);
                Assert.Contains("Engine: " + UserConstructionProvenance.TasEngine, row.DetailsText);
                Assert.Contains("Model: Project X", row.DetailsText);

                Assert.Equal("added source Constructions.tcd · based on NCM wall", viewModel.Rows.Single(x => x.Name == "Plain wall").SourceText);
                UserConstructionEntryRow handmade = viewModel.Rows.Single(x => x.Name == "Handmade");
                Assert.Equal("U not recorded", handmade.UValueText);
                Assert.Equal("no provenance", handmade.SourceText);
                Assert.Equal(string.Empty, handmade.SavedText);
                Assert.Contains("no provenance", handmade.DetailsText);
            }
        }

        [WpfFact]
        public void An_empty_library_has_an_empty_state_and_nothing_to_rename_or_remove()
        {
            using (UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(library))
            {
                Assert.Empty(viewModel.Rows);
                Assert.False(viewModel.HasRows);
                Assert.True(viewModel.ShowEmptyText);
                Assert.Contains("Save to My constructions", viewModel.EmptyText);
                Assert.Equal("0 saved constructions", viewModel.CountText);
                Assert.False(viewModel.CanRename);
                Assert.False(viewModel.CanRemove);
                Assert.False(viewModel.BeginRename());
                Assert.False(viewModel.Remove(x => true));
                Assert.Equal(UserConstructionLibraryState.Missing, viewModel.State);
                Assert.False(File.Exists(library.Path));
            }
        }

        [WpfFact]
        public void Rename_applies_the_librarys_rule_while_typing_changes_the_label_only_and_keeps_the_selection()
        {
            Construction a = Save("Alpha");
            Construction b = Save("Beta", 0.2);
            ConstructionManager before = library.Read().ConstructionManager;

            using (UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(library))
            {
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == a.Guid);
                Assert.True(viewModel.BeginRename());
                Assert.Equal("Alpha", viewModel.RenameText);
                Assert.True(viewModel.CanCommitRename);   // the same name is no clash with itself

                viewModel.RenameText = " beta ";
                Assert.Contains("already in My constructions", viewModel.RenameError);
                Assert.False(viewModel.CanCommitRename);
                Assert.False(viewModel.CommitRename());
                viewModel.RenameText = "   ";
                Assert.Contains("needs a name", viewModel.RenameError);

                viewModel.RenameText = "  Gamma ";
                Assert.True(viewModel.CanCommitRename);
                Assert.True(viewModel.CommitRename());

                Assert.False(viewModel.IsRenaming);
                Assert.Equal("Renamed to 'Gamma'.", viewModel.Message);
                Assert.Equal(a.Guid, viewModel.SelectedRow?.Guid);
                Assert.Equal("Gamma", viewModel.SelectedRow.Name);
                Construction renamed = library.Read().Constructions.Single(x => x.Guid == a.Guid);
                Assert.Equal(before.Constructions.Single(x => x.Guid == a.Guid).ToJsonObject().ToJsonString().Replace("\"Alpha\"", "\"Gamma\""), renamed.ToJsonObject().ToJsonString());
                Assert.Equal("Beta", library.Read().Constructions.Single(x => x.Guid == b.Guid).Name);

                // Cancelling an edit changes nothing.
                viewModel.BeginRename();
                viewModel.RenameText = "Never";
                viewModel.CancelRename();
                Assert.Equal("Gamma", library.Read().Constructions.Single(x => x.Guid == a.Guid).Name);
            }
        }

        [WpfFact]
        public void Remove_asks_first_says_what_will_happen_and_moves_the_construction_to_the_archive()
        {
            Construction a = Save("Alpha");
            Save("Beta", 0.2);

            using (UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(library))
            {
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == a.Guid);
                string asked = null;

                Assert.False(viewModel.Remove(text => { asked = text; return false; }));
                Assert.Contains("'Alpha'", asked);
                Assert.Contains("[" + a.Guid.ToString().Substring(30) + "]", asked);
                Assert.Contains("from My constructions", asked);
                Assert.Contains("Models that already use it keep their own copy", asked);
                Assert.Contains("not deleted", asked);
                Assert.Contains("Constructions.removed.json", asked);
                Assert.Equal(2, library.Read().Constructions.Count);
                Assert.False(File.Exists(library.ArchivePath));

                Assert.False(viewModel.Remove(null));
                Assert.True(viewModel.Remove(text => true));

                Assert.Equal("Removed 'Alpha'; it is kept in the archive.", viewModel.Message);
                Assert.Equal(new[] { "Beta" }, viewModel.Rows.Select(x => x.Name).ToArray());
                Assert.Null(viewModel.SelectedRow);
                Assert.Contains(UserConstructionFixture.Constructions(UserConstructionFixture.Read(library.ArchivePath)), x => x.Guid == a.Guid);
            }
        }

        [WpfFact]
        public void An_unreadable_library_is_a_note_nothing_can_be_changed_and_the_file_is_left_as_it_is()
        {
            File.WriteAllText(library.Path, "this is not a library");
            string before = Hash(library.Path);

            using (UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(library))
            {
                Assert.Equal(UserConstructionLibraryState.Unreadable, viewModel.State);
                Assert.True(viewModel.HasNote);
                Assert.StartsWith("My constructions could not be used", viewModel.Note);
                Assert.False(viewModel.CanChange);
                Assert.False(viewModel.ShowEmptyText);
                Assert.Empty(viewModel.Rows);
                Assert.False(viewModel.BeginRename());
                Assert.False(viewModel.Remove(x => true));
            }

            Assert.Equal(before, Hash(library.Path));
        }

        [WpfFact]
        public async Task It_follows_the_library_also_when_the_change_comes_from_another_thread_and_stops_when_disposed()
        {
            Construction a = Save("Alpha");
            UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(library);
            int thread = Environment.CurrentManagedThreadId;
            List<int> threads = new List<int>();
            viewModel.PropertyChanged += (sender, e) => threads.Add(Environment.CurrentManagedThreadId);
            Assert.Equal(1, UserConstructionFixture.Subscribers(library));

            Construction b = await Task.Run(() => Save("Beta", 0.2));
            await Pump(() => viewModel.Rows.Any(x => x.Guid == b.Guid), "the construction saved on another thread to appear");
            Assert.All(threads, x => Assert.Equal(thread, x));

            viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == a.Guid);
            await Task.Run(() => library.Remove(b.Guid));
            await Pump(() => viewModel.Rows.All(x => x.Guid != b.Guid), "the removed construction to leave");
            Assert.Equal(a.Guid, viewModel.SelectedRow?.Guid);

            viewModel.Dispose();
            Assert.Equal(0, UserConstructionFixture.Subscribers(library));
            Save("Gamma", 0.3);
            Flush();
            Assert.DoesNotContain(viewModel.Rows, x => x.Name == "Gamma");
        }

        [Fact]
        public void The_view_models_and_rows_hold_no_analytical_model()
        {
            Type[] types = { typeof(UserConstructionLibraryViewModel), typeof(UserConstructionEntryRow), typeof(UserLibraryWindow) };
            foreach (Type type in types)
            {
                foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    Type memberType = member is FieldInfo field ? field.FieldType : member is PropertyInfo property ? property.PropertyType : null;
                    if (memberType == null)
                    {
                        continue;
                    }

                    Assert.False(typeof(AnalyticalModel).IsAssignableFrom(memberType) || typeof(UIAnalyticalModel).IsAssignableFrom(memberType) || typeof(AdjacencyCluster).IsAssignableFrom(memberType), type.Name + "." + member.Name + " holds a model");
                }
            }
        }

        // ---- The window ---------------------------------------------------------------------------------------------------------

        private UserLibraryWindow Show(out UserLibraryViewModel glazing, bool withConstructions = true)
        {
            UserGlazingLibrary glazingLibrary = BuilderFixture.Library(directory);
            glazing = new UserLibraryViewModel(glazingLibrary) { Constructions = withConstructions ? new UserConstructionLibraryViewModel(library) : null };
            UserLibraryWindow window = new UserLibraryWindow(glazing) { Left = 0, Top = 0, ShowActivated = false };
            window.Show();
            Flush();
            return window;
        }

        [WpfFact]
        public void The_window_has_a_Constructions_tab_beside_Glazing_systems_with_the_rows_the_details_and_the_empty_state()
        {
            UserLibraryWindow window = Show(out UserLibraryViewModel glazing);
            try
            {
                TabControl tabs = ById<TabControl>(window, "tabControl_Library");
                Assert.Equal(new[] { "Glazing systems", "Constructions" }, tabs.Items.OfType<TabItem>().Select(x => x.Header.ToString()).ToArray());
                Assert.Equal(Visibility.Visible, ((TabItem)tabs.Items[1]).Visibility);
                Assert.Same(tabs.Items[0], tabs.SelectedItem);

                tabs.SelectedItem = tabs.Items[1];
                Flush();
                Assert.Contains("No constructions saved yet", ById<TextBlock>(window, "textBlock_ConstructionsEmpty").Text);
                Assert.True(ById<TextBlock>(window, "textBlock_ConstructionsEmpty").IsVisible);
                Assert.Equal("0 saved constructions", ById<TextBlock>(window, "textBlock_ConstructionsCount").Text);
                Assert.False(ById<Button>(window, "button_ConstructionRename").IsEnabled);
                Assert.False(ById<Button>(window, "button_ConstructionRemove").IsEnabled);

                Construction saved = Save("My wall");
                Flush();
                ListView list = ById<ListView>(window, "listView_Constructions");
                Assert.Equal(1, list.Items.Count);
                Assert.False(ById<TextBlock>(window, "textBlock_ConstructionsEmpty").IsVisible);

                window.ConstructionsViewModel.SelectedRow = window.ConstructionsViewModel.Rows.Single();
                Flush();
                string details = ById<TextBlock>(window, "textBlock_ConstructionDetails").Text;
                Assert.Contains("My wall", details);
                Assert.Contains("Saved from: model", details);
                Assert.Contains("U-value at save: 0.180 W/m²K", details);

                // There is no opaque edit: the only commands are Rename, Remove and Close.
                Assert.Null(ById<Button>(window, "button_ConstructionOpenInBuilder"));
                Assert.Equal(saved.Guid, window.ConstructionsViewModel.SelectedRow.Guid);
            }
            finally
            {
                window.Close();
            }

            Assert.Equal(0, UserConstructionFixture.Subscribers(library));
        }

        [WpfFact]
        public void Rename_and_Remove_work_from_the_tab_and_a_remove_needs_the_confirmation()
        {
            Construction a = Save("Alpha");
            Save("Beta", 0.2);
            UserLibraryWindow window = Show(out UserLibraryViewModel glazing);
            try
            {
                string asked = null;
                bool answer = false;
                window.Confirm = text => { asked = text; return answer; };
                ((TabControl)ById<TabControl>(window, "tabControl_Library")).SelectedIndex = 1;
                Flush();
                UserConstructionLibraryViewModel viewModel = window.ConstructionsViewModel;
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Guid == a.Guid);
                Flush();

                Press(ById<Button>(window, "button_ConstructionRename"));
                TextBox box = ById<TextBox>(window, "textBox_ConstructionRename");
                Assert.True(ById<StackPanel>(window, "stackPanel_ConstructionRename").IsVisible);
                box.Text = "beta";
                Flush();
                Assert.Contains("already in My constructions", ById<TextBlock>(window, "textBlock_ConstructionRenameError").Text);
                Assert.False(ById<Button>(window, "button_ConstructionRenameOk").IsEnabled);
                box.Text = "Gamma";
                Flush();
                Press(ById<Button>(window, "button_ConstructionRenameOk"));
                Assert.Equal("Gamma", library.Read().Constructions.Single(x => x.Guid == a.Guid).Name);
                Assert.Equal("Renamed to 'Gamma'.", ById<TextBlock>(window, "textBlock_ConstructionsMessage").Text);

                Press(ById<Button>(window, "button_ConstructionRemove"));
                Assert.Contains("'Gamma'", asked);
                Assert.Equal(2, library.Read().Constructions.Count);

                answer = true;
                Press(ById<Button>(window, "button_ConstructionRemove"));
                Assert.Equal(new[] { "Beta" }, library.Read().Constructions.Select(x => x.Name).ToArray());
                Assert.True(File.Exists(library.ArchivePath));
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void A_window_without_My_constructions_leaves_the_tab_out_and_the_glazing_tab_is_unchanged()
        {
            UserLibraryWindow window = Show(out UserLibraryViewModel glazing, withConstructions: false);
            try
            {
                TabControl tabs = ById<TabControl>(window, "tabControl_Library");
                Assert.Equal(Visibility.Collapsed, ((TabItem)tabs.Items[1]).Visibility);
                Assert.Null(window.ConstructionsViewModel);
                Assert.NotNull(ById<ListView>(window, "listView_Systems"));
                Assert.Equal("Glazing systems", ById<TextBlock>(window, "textBlock_Section").Text);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- The panel's entry point --------------------------------------------------------------------------------------------

        [WpfFact]
        public void My_library_from_the_panel_carries_the_Constructions_tab_and_opening_it_saves_or_changes_nothing()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            int history = 0;
            ui.Modified += (sender, e) => modified++;
            ui.HistoryChanged += (sender, e) => history++;
            string before = parts.Model.ToJsonObject().ToJsonString();
            Save("Saved before");
            string hash = Hash(library.Path);

            ThermalPerformanceControl control = new ThermalPerformanceControl(UserConstructionFixture.Services(library));
            UserLibraryViewModel captured = null;
            List<string> names = null;
            control.ShowLibrary = viewModel =>
            {
                captured = viewModel;
                names = viewModel.Constructions?.Rows.Select(x => x.Name).ToList();
                return true;
            };

            Assert.True(control.OpenLibrary());

            Assert.NotNull(captured.Constructions);
            Assert.Equal(new[] { "Saved before" }, names);
            Assert.Equal(0, UserConstructionFixture.Subscribers(library));   // disposed with the view-model
            Assert.Equal(hash, Hash(library.Path));
            Assert.Equal(before, ui.JSAMObject.ToJsonObject().ToJsonString());
            Assert.Equal(0, modified);
            Assert.Equal(0, history);
            Assert.False(ui.CanUndo);
        }
    }
}
