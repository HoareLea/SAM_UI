// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR2 in real WPF: the "My library" window over its view-model (named controls, the list, details, inline rename with the library's
    /// rule, Remove only after the confirmation, Open in Builder, the empty and unreadable states, following the library) and the Thermal Performance
    /// panel's entry points - the "My library…" button, the Builder opened from it, and the candidate list's context menu (New system based on this…,
    /// Rename…, Remove…). Library and Builder operations never touch the analytical model, its history or the Undo stack. The real XAML runs over the
    /// fake Tas and a temporary user library; nothing reads the user's files.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserLibraryWindowTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;

        public UserLibraryWindowTests()
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

        private ApertureConstruction Save(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        private static string Json(AnalyticalModel model) => model.ToJsonObject().ToJsonString();

        // ---- The window ----------------------------------------------------------------------------------------------------

        private UserLibraryWindow Show(out UserLibraryViewModel viewModel)
        {
            viewModel = new UserLibraryViewModel(library);
            UserLibraryWindow window = new UserLibraryWindow(viewModel) { Left = 0, Top = 0, ShowActivated = false };
            window.Show();
            Flush();
            return window;
        }

        [WpfFact]
        public void The_window_has_named_controls_a_sensible_minimum_size_and_lists_the_saved_systems()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("My double"));
            UserLibraryWindow window = Show(out UserLibraryViewModel viewModel);
            try
            {
                Assert.True(window.MinWidth >= 700 && window.MinHeight >= 440);
                Assert.Equal("My library", window.Title);
                Assert.Equal(ResizeMode.CanResize, window.ResizeMode);
                Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
                Assert.False(window.ShowInTaskbar);

                foreach (string id in new[]
                {
                    "listView_Systems", "button_Rename", "button_Remove", "button_OpenInBuilder", "button_Close", "textBlock_Details", "textBlock_Count", "textBlock_Archive", "textBlock_Message",
                    "textBox_Rename", "button_RenameOk", "button_RenameCancel", "textBlock_RenameError", "textBlock_Empty", "textBlock_Note",
                })
                {
                    FrameworkElement element = ById<FrameworkElement>(window, id);
                    Assert.True(element != null, "no control " + id);
                    Assert.False(string.IsNullOrEmpty(AutomationProperties.GetAutomationId(element)));
                }

                ListView list = ById<ListView>(window, "listView_Systems");
                Assert.Equal("Saved glazing systems", AutomationProperties.GetName(list));
                Assert.Equal(new[] { saved.Guid }, list.Items.Cast<UserLibraryEntryRow>().Select(x => x.Guid));
                Assert.Equal("1 saved system", ById<TextBlock>(window, "textBlock_Count").Text);
                Assert.False(ById<TextBlock>(window, "textBlock_Empty").IsVisible);
                Assert.False(ById<Button>(window, "button_Rename").IsEnabled);

                // Selecting a system shows its details and enables the commands.
                list.SelectedItem = list.Items[0];
                Flush();
                Assert.Contains("My double  [", ById<TextBlock>(window, "textBlock_Details").Text);
                Assert.Contains("At save: Ug 1.05", ById<TextBlock>(window, "textBlock_Details").Text);
                Assert.True(ById<Button>(window, "button_Rename").IsEnabled);
                Assert.True(ById<Button>(window, "button_Remove").IsEnabled);
                Assert.True(ById<Button>(window, "button_OpenInBuilder").IsEnabled);
                Assert.False(ById<FrameworkElement>(window, "stackPanel_Rename").IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void An_empty_library_says_how_to_save_one_and_an_unreadable_one_is_a_note_with_nothing_to_press()
        {
            UserLibraryWindow empty = Show(out _);
            try
            {
                Assert.True(ById<TextBlock>(empty, "textBlock_Empty").IsVisible);
                Assert.Contains("Save as predefined", ById<TextBlock>(empty, "textBlock_Empty").Text);
                Assert.False(ById<TextBlock>(empty, "textBlock_Note").IsVisible);
            }
            finally
            {
                empty.Close();
            }

            File.WriteAllText(library.Path, "{ this is not json");
            UserLibraryWindow broken = Show(out _);
            try
            {
                Assert.True(ById<TextBlock>(broken, "textBlock_Note").IsVisible);
                Assert.StartsWith("My glazing systems could not be used:", ById<TextBlock>(broken, "textBlock_Note").Text);
                Assert.False(ById<TextBlock>(broken, "textBlock_Empty").IsVisible);
                Assert.False(ById<Button>(broken, "button_Rename").IsEnabled);
                Assert.False(ById<Button>(broken, "button_Remove").IsEnabled);
                Assert.False(ById<Button>(broken, "button_OpenInBuilder").IsEnabled);
            }
            finally
            {
                broken.Close();
            }

            Assert.Equal("{ this is not json", File.ReadAllText(library.Path));
        }

        [WpfFact]
        public void Rename_is_inline_shows_the_rule_as_you_type_and_changes_only_the_name()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            Save(BuilderFixture.Triple("Second"));
            UserLibraryWindow window = Show(out UserLibraryViewModel viewModel);
            try
            {
                ListView list = ById<ListView>(window, "listView_Systems");
                list.SelectedItem = list.Items.Cast<UserLibraryEntryRow>().Single(x => x.Guid == first.Guid);
                Flush();

                Press(ById<Button>(window, "button_Rename"));
                FrameworkElement panel = ById<FrameworkElement>(window, "stackPanel_Rename");
                TextBox box = ById<TextBox>(window, "textBox_Rename");
                Assert.True(panel.IsVisible);
                Assert.Equal("First", box.Text);

                box.Text = "second";
                Flush();
                Assert.Contains("is already in My glazing systems", ById<TextBlock>(window, "textBlock_RenameError").Text);
                Assert.False(ById<Button>(window, "button_RenameOk").IsEnabled);

                box.Text = "Renamed";
                Flush();
                Assert.Equal(string.Empty, ById<TextBlock>(window, "textBlock_RenameError").Text);
                Press(ById<Button>(window, "button_RenameOk"));

                Assert.False(panel.IsVisible);
                Assert.Equal("Renamed", library.Read().Systems.Single(x => x.Guid == first.Guid).Name);
                Assert.Equal(new[] { "Renamed", "Second" }, list.Items.Cast<UserLibraryEntryRow>().Select(x => x.Name));
                Assert.Equal("Renamed to 'Renamed'.", ById<TextBlock>(window, "textBlock_Message").Text);
                Assert.Equal(first.Guid, ((UserLibraryEntryRow)list.SelectedItem).Guid);

                // Cancel leaves the name alone.
                Press(ById<Button>(window, "button_Rename"));
                ById<TextBox>(window, "textBox_Rename").Text = "Not this";
                Press(ById<Button>(window, "button_RenameCancel"));
                Assert.False(panel.IsVisible);
                Assert.Equal("Renamed", library.Read().Systems.Single(x => x.Guid == first.Guid).Name);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void Remove_asks_first_and_only_a_yes_moves_the_system_to_the_archive()
        {
            ApertureConstruction doomed = Save(BuilderFixture.Double("Doomed"));
            UserLibraryWindow window = Show(out _);
            try
            {
                string asked = null;
                bool answer = false;
                window.Confirm = text =>
                {
                    asked = text;
                    return answer;
                };

                ListView list = ById<ListView>(window, "listView_Systems");
                list.SelectedItem = list.Items[0];
                Flush();

                Press(ById<Button>(window, "button_Remove"));
                Assert.Contains("Remove 'Doomed'", asked);
                Assert.Single(library.Read().Systems);
                Assert.False(File.Exists(library.ArchivePath));

                answer = true;
                Press(ById<Button>(window, "button_Remove"));
                Assert.Empty(library.Read().Systems);
                Assert.True(File.Exists(library.ArchivePath));
                Assert.Empty(list.Items);
                Assert.True(ById<TextBlock>(window, "textBlock_Empty").IsVisible);
                Assert.Equal("Removed 'Doomed'; it is kept in the archive.", ById<TextBlock>(window, "textBlock_Message").Text);
                Assert.Equal(doomed.Guid, UserLibraryFile.Parse(File.ReadAllText(library.ArchivePath), out _).ApertureConstructions.Single().Guid);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public async Task The_open_window_follows_a_save_from_another_thread_and_Open_in_Builder_is_a_request_for_the_selected_system()
        {
            ApertureConstruction first = Save(BuilderFixture.Double("First"));
            UserLibraryWindow window = Show(out UserLibraryViewModel viewModel);
            try
            {
                ListView list = ById<ListView>(window, "listView_Systems");
                ApertureConstruction second = await Task.Run(() => Save(BuilderFixture.Triple("Second")));
                await Pump(() => list.Items.Count == 2, "the new system to be listed");
                Assert.Equal("2 saved systems", ById<TextBlock>(window, "textBlock_Count").Text);

                UserLibraryEntryRow requested = null;
                viewModel.OpenInBuilderRequested += (sender, row) => requested = row;
                list.SelectedItem = list.Items.Cast<UserLibraryEntryRow>().Single(x => x.Guid == second.Guid);
                Flush();
                Press(ById<Button>(window, "button_OpenInBuilder"));

                Assert.Equal(second.Guid, requested?.Guid);
                Assert.Equal(2, library.Read().Systems.Count);
                Assert.NotEqual(first.Guid, requested.Guid);
            }
            finally
            {
                window.Close();
            }

            Assert.Equal(0, Subscribers());
        }

        private int Subscribers()
        {
            System.Reflection.FieldInfo field = typeof(UserGlazingLibrary).GetField(nameof(UserGlazingLibrary.Changed), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (field.GetValue(library) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        // ---- The Thermal Performance panel -----------------------------------------------------------------------------------

        private sealed class Panel : IDisposable
        {
            public ThermalPerformanceControl Control;
            public System.Windows.Window Host;
            public UIAnalyticalModel Ui;
            public ThermalParts Parts;
            public ThermalRowEditor Editor;
            public string Before;
            public int Modified;
            public int History;

            public void Dispose()
            {
                Host.Close();
            }
        }

        private Panel OpenPanel(bool openList = true)
        {
            Panel result = new Panel() { Parts = ThermalFixture.Build() };
            result.Ui = new UIAnalyticalModel(result.Parts.Model);
            result.Ui.Modified += (sender, e) => result.Modified++;
            result.Ui.HistoryChanged += (sender, e) => result.History++;
            result.Before = Json(result.Ui.JSAMObject);

            FakeGlazingEvaluator candidates = new FakeGlazingEvaluator();
            result.Control = new ThermalPerformanceControl(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => candidates, () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(),
                () => null, () => new FakeSourceReader().Catalog(), () => library, () => new DraftGlazingEvaluator(new FakeDraftTas(), TimeSpan.Zero, null, BuilderFixture.Options()), BuilderFixture.Options()));
            result.Control.Applier = set => Modify.ApplyThermalChange(result.Ui, set, x => { }, null);
            result.Host = new System.Windows.Window { Content = result.Control, Left = 0, Top = 0, Width = 380, Height = 900, ShowActivated = false };
            result.Host.Show();

            AnalyticalModel model = result.Ui.JSAMObject;
            result.Control.Update(model, new List<SAMObject>() { model.AdjacencyCluster.GetAperture(result.Parts.Windows[0]) });
            Flush();
            result.Editor = result.Control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
            if (openList)
            {
                Press(Descendants<Button>(result.Control).First(x => AutomationProperties.GetAutomationId(x) == "button_Change" && ReferenceEquals(x.DataContext, result.Editor)));
                result.Editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Flush();
            }

            return result;
        }

        private void AssertModelUntouched(Panel panel)
        {
            Assert.Equal(panel.Before, Json(panel.Ui.JSAMObject));
            Assert.Equal(0, panel.Modified);
            Assert.Equal(0, panel.History);
            Assert.False(panel.Ui.CanUndo);
        }

        // The context menu of a candidate row, opened as a right-click opens it (its items then take the row as their data context).
        private static ContextMenu OpenMenu(Panel panel, Guid guid)
        {
            ListBox list = Descendants<ListBox>(panel.Control).First(x => ReferenceEquals(x.DataContext, panel.Editor) && AutomationProperties.GetAutomationId(x) == "listBox_Candidates");
            GlazingCandidateRow row = panel.Editor.Candidates.Single(x => x.Guid == guid);
            list.ScrollIntoView(row);
            list.UpdateLayout();
            Flush();
            ListBoxItem item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(row);
            Assert.NotNull(item);
            item.ApplyTemplate();
            item.UpdateLayout();
            Flush();
            StackPanel content = Descendants<StackPanel>(panel.Control).First(x => ReferenceEquals(x.DataContext, row) && x.ContextMenu != null);
            content.ContextMenu.PlacementTarget = content;
            content.ContextMenu.IsOpen = true;
            Flush();
            return content.ContextMenu;
        }

        private static MenuItem Item(ContextMenu menu, string id) => menu.Items.OfType<MenuItem>().Single(x => AutomationProperties.GetAutomationId(x) == id);

        private static void Click(MenuItem item)
        {
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Flush();
        }

        [WpfFact]
        public void The_My_library_button_opens_the_manager_over_the_panels_library_without_touching_the_model()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Mine"));
            using (Panel panel = OpenPanel(openList: false))
            {
                Button button = ById<Button>(panel.Control, "button_MyLibrary");
                Assert.NotNull(button);
                Assert.Equal("My library…", button.Content);

                UserLibraryViewModel shown = null;
                int subscribers = -1;
                panel.Control.ShowLibrary = viewModel =>
                {
                    shown = viewModel;
                    subscribers = Subscribers();
                    return true;
                };

                Press(button);

                Assert.NotNull(shown);
                Assert.Equal(new[] { saved.Guid }, shown.Rows.Select(x => x.Guid));
                Assert.Same(library, shown.Library);
                Assert.Equal(1, subscribers);
                Assert.Equal(0, Subscribers());                  // disposed when the manager is closed
                AssertModelUntouched(panel);
            }
        }

        [WpfFact]
        public void Open_in_Builder_from_the_manager_opens_that_system_for_editing_and_opening_it_saves_nothing()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Mine"));
            using (Panel panel = OpenPanel(openList: false))
            {
                GlazingBuilderViewModel builder = null;
                panel.Control.ShowBuilder = viewModel =>
                {
                    builder = viewModel;
                    Assert.True(viewModel.IsEditing);
                    Assert.Equal("Mine", viewModel.Name);
                    Assert.Equal("Editing a copy of Mine · saving creates a new system", viewModel.StatusText);
                    Assert.Equal(saved.Guid, viewModel.Draft.BasedOnGuid);
                    Assert.Equal("P,G,P", string.Join(",", viewModel.Layers.Select(x => x.IsPane ? "P" : "G")));
                    return null;
                };
                panel.Control.ShowLibrary = viewModel =>
                {
                    viewModel.SelectedRow = viewModel.Rows.Single();
                    Assert.True(viewModel.RequestOpenInBuilder());
                    return true;
                };

                Press(ById<Button>(panel.Control, "button_MyLibrary"));

                Assert.NotNull(builder);
                Assert.Equal(1, library.Read().Systems.Count);   // opening it saved nothing
                AssertModelUntouched(panel);
            }
        }

        // A right mouse button press on a candidate as WPF's input manager delivers it: the tunnelling PreviewMouseDown, then the bubbling MouseDown with the
        // same arguments (so a handled preview reaches the container handled).
        private static void RightButtonDown(UIElement element)
        {
            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) { RoutedEvent = Mouse.PreviewMouseDownEvent, Source = element };
            element.RaiseEvent(args);
            args.RoutedEvent = Mouse.MouseDownEvent;
            element.RaiseEvent(args);
            Flush();
        }

        [WpfFact]
        public void Right_clicking_a_candidate_to_open_its_context_menu_neither_selects_nor_chooses_it()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Mine"));
            using (Panel panel = OpenPanel())
            {
                ListBox list = Descendants<ListBox>(panel.Control).First(x => ReferenceEquals(x.DataContext, panel.Editor) && AutomationProperties.GetAutomationId(x) == "listBox_Candidates");
                Assert.Null(list.SelectedItem);

                foreach (Guid guid in new[] { saved.Guid, GlazingFixture.BetterGuid })
                {
                    ContextMenu menu = OpenMenu(panel, guid);
                    menu.IsOpen = false;
                    Flush();
                    GlazingCandidateRow row = panel.Editor.Candidates.Single(x => x.Guid == guid);
                    TextBlock text = Descendants<TextBlock>(panel.Control).First(x => ReferenceEquals(x.DataContext, row));
                    Assert.NotNull(list.ItemContainerGenerator.ContainerFromItem(row));

                    RightButtonDown(text);

                    Assert.Null(list.SelectedItem);                      // the press that opens the menu does not select the candidate ...
                    Assert.Null(panel.Editor.SelectedCandidate);         // ... so it is not the row's pending change
                    Assert.False(panel.Editor.HasRequest);
                    Assert.Equal(0, panel.Control.ViewModel.Session.ChangeCount);
                }

                AssertModelUntouched(panel);
            }
        }

        [WpfFact]
        public void The_context_menu_New_system_based_on_this_seeds_the_Builder_from_the_right_clicked_candidate_without_choosing_it()
        {
            Save(BuilderFixture.Double("Mine"));
            using (Panel panel = OpenPanel())
            {
                GlazingBuilderViewModel builder = null;
                panel.Control.ShowBuilder = viewModel =>
                {
                    builder = viewModel;
                    return null;
                };

                // A candidate of the default library that is not the chosen one (nothing is chosen).
                Assert.Null(panel.Editor.SelectedCandidate);
                ContextMenu menu = OpenMenu(panel, GlazingFixture.BetterGuid);
                MenuItem based = Item(menu, "menuItem_NewBasedOn");
                Assert.Equal(GlazingFixture.BetterGuid, ((GlazingCandidateRow)based.DataContext).Guid);
                Click(based);

                Assert.NotNull(builder);
                Assert.Equal(GlazingFixture.BetterGuid, builder.Draft.BasedOnGuid);
                Assert.Equal(GlazingFixture.LowE, builder.Layers[2].Pane.OriginalName);
                Assert.Null(panel.Editor.SelectedCandidate);          // seeding did not make it the row's pending change
                Assert.False(panel.Editor.HasRequest);
                Assert.Equal(0, panel.Control.ViewModel.Session.ChangeCount);

                // Rename… and Remove… are only for the user's own systems.
                Assert.Equal(Visibility.Collapsed, Item(menu, "menuItem_RenameUser").Visibility);
                Assert.Equal(Visibility.Collapsed, Item(menu, "menuItem_RemoveUser").Visibility);
                menu.IsOpen = false;
                AssertModelUntouched(panel);
            }
        }

        [WpfFact]
        public void The_context_menu_Open_in_Builder_edits_a_user_system_while_New_system_based_on_this_makes_a_plain_copy_of_it()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Mine"));
            using (Panel panel = OpenPanel())
            {
                List<GlazingBuilderViewModel> shown = new List<GlazingBuilderViewModel>();
                List<(bool Editing, string Name)> seen = new List<(bool, string)>();
                panel.Control.ShowBuilder = builder =>
                {
                    seen.Add((builder.IsEditing, builder.Name));
                    return null;
                };

                ContextMenu menu = OpenMenu(panel, saved.Guid);
                Assert.Equal(Visibility.Visible, Item(menu, "menuItem_OpenInBuilder").Visibility);
                Click(Item(menu, "menuItem_OpenInBuilder"));
                Click(Item(menu, "menuItem_NewBasedOn"));
                menu.IsOpen = false;

                Assert.Equal(new[] { (true, "Mine"), (false, "Mine (copy)") }, seen);
                Assert.Null(panel.Editor.SelectedCandidate);

                // Not for the default library's systems.
                ContextMenu other = OpenMenu(panel, GlazingFixture.BetterGuid);
                Assert.Equal(Visibility.Collapsed, Item(other, "menuItem_OpenInBuilder").Visibility);
                other.IsOpen = false;
                Assert.Equal(1, library.Read().Systems.Count);
                AssertModelUntouched(panel);
            }
        }

        [WpfFact]
        public async Task The_context_menu_Rename_opens_the_manager_on_that_system_ready_to_rename_and_the_open_list_follows()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Old name"));
            using (Panel panel = OpenPanel())
            {
                ContextMenu menu = OpenMenu(panel, saved.Guid);
                Assert.Equal(Visibility.Visible, Item(menu, "menuItem_RenameUser").Visibility);
                Assert.Equal(Visibility.Visible, Item(menu, "menuItem_RemoveUser").Visibility);

                bool renaming = false;
                panel.Control.ShowLibrary = viewModel =>
                {
                    renaming = viewModel.IsRenaming && viewModel.SelectedRow?.Guid == saved.Guid && viewModel.RenameText == "Old name";
                    viewModel.RenameText = "New name";
                    Assert.True(viewModel.CommitRename());
                    return true;
                };

                Click(Item(menu, "menuItem_RenameUser"));
                menu.IsOpen = false;
                Assert.True(renaming);

                await Pump(() => panel.Editor.Candidates.Any(x => x.Guid == saved.Guid && x.Name == "New name"), "the open list to show the new name");
                Assert.Single(panel.Editor.Candidates, x => x.Guid == saved.Guid);
                AssertModelUntouched(panel);
            }
        }

        [WpfFact]
        public async Task The_context_menu_Remove_asks_first_and_then_the_open_list_drops_the_system()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Doomed"));
            using (Panel panel = OpenPanel())
            {
                string asked = null;
                bool answer = false;
                panel.Control.ConfirmRemove = text =>
                {
                    asked = text;
                    return answer;
                };
                ContextMenu menu = OpenMenu(panel, saved.Guid);

                Click(Item(menu, "menuItem_RemoveUser"));
                Assert.Contains("Remove 'Doomed'", asked);
                Assert.Single(library.Read().Systems);
                Assert.Contains(panel.Editor.Candidates, x => x.Guid == saved.Guid);

                answer = true;
                Click(Item(menu, "menuItem_RemoveUser"));
                menu.IsOpen = false;

                Assert.Empty(library.Read().Systems);
                Assert.True(File.Exists(library.ArchivePath));
                await Pump(() => panel.Editor.Candidates.All(x => x.Guid != saved.Guid), "the removed system to leave the open list");
                AssertModelUntouched(panel);
            }
        }
    }
}
