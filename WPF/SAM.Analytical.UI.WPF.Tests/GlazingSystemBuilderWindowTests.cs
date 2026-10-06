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
    /// Stage E0-3 in real WPF: the Glazing System Builder window over its view-model (named controls, minimum size, a virtualised pane list, keyboard
    /// reorder, Save closing only after success, Cancel) and the Thermal Performance panel's <c>Create new…</c> opening it. The real XAML runs over the
    /// fake Tas and a temporary user library; nothing reads the user's files.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class GlazingSystemBuilderWindowTests
    {
        private static string NewDirectory() => BuilderFixture.TempDirectory();

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

        private sealed class Opened : IDisposable
        {
            public string Directory;
            public UserGlazingLibrary Library;
            public GlazingBuilderViewModel ViewModel;
            public GlazingSystemBuilderWindow Window;
            public FakeDraftTas Tas = new FakeDraftTas();

            public void Dispose()
            {
                if (Window.IsLoaded)
                {
                    Window.Close();
                }

                try
                {
                    System.IO.Directory.Delete(Directory, true);
                }
                catch (Exception)
                {
                }
            }
        }

        private static async Task<Opened> Open(int extraPanes = 0, bool withSeed = true)
        {
            Opened result = new Opened() { Directory = NewDirectory() };
            result.Library = BuilderFixture.Library(result.Directory);
            result.ViewModel = new GlazingBuilderViewModel(BuilderUiFixture.Options(result.Library, result.Tas, null, null, withSeed, BuilderUiFixture.PaneSource(extra: extraPanes, kind: GlazingSourceKind.Model)));
            result.Window = new GlazingSystemBuilderWindow(result.ViewModel) { Left = 0, Top = 0, ShowActivated = false };
            result.Window.Show();
            Flush();
            await Pump(() => result.ViewModel.Panes.Entries.Count > 0 && result.ViewModel.PerformanceState != GlazingBuilderPerformanceState.Calculating, "the Builder to be ready");
            return result;
        }

        // ---- The window ---------------------------------------------------------------------------------------------

        [WpfFact]
        public async Task The_window_has_named_controls_a_sensible_minimum_size_and_no_expander()
        {
            using (Opened opened = await Open())
            {
                GlazingSystemBuilderWindow window = opened.Window;

                Assert.True(window.MinWidth >= 900 && window.MinHeight >= 600);
                Assert.Equal(ResizeMode.CanResize, window.ResizeMode);
                Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
                Assert.Equal("Glazing System Builder", window.Title);
                Assert.False(window.ShowInTaskbar);
                Assert.True(window.ActualWidth >= window.MinWidth);

                foreach (string id in new[]
                {
                    "textBox_Name", "comboBox_IntendedUse", "listBox_Layers", "button_AddPane", "button_ReplacePane", "button_AddGap", "button_Remove", "button_MoveUp", "button_MoveDown",
                    "button_Reverse", "comboBox_Frame", "textBox_FrameWidth", "comboBox_PaneSource", "button_AddSource", "textBox_Search", "listView_Panes", "textBlock_Ug", "textBlock_G",
                    "textBlock_LT", "textBlock_Uf", "textBlock_ReferenceUw", "listBox_Issues", "textBlock_ValidationSummary", "button_Save", "button_Cancel",
                })
                {
                    FrameworkElement element = ById<FrameworkElement>(window, id);
                    Assert.True(element != null, id + " is missing");

                    // A screen reader needs a name: an explicit one, or the control's own text.
                    string name = AutomationProperties.GetName(element);
                    string content = (element as ContentControl)?.Content as string;
                    Assert.True(!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(content) || element is TextBlock || element is TextBox || element is ListBox, id + " has no accessible name");
                }

                // The two lists announce what they are; no collapsed Expander hides a row template (the Stage C finding).
                Assert.Equal("Build-up, outside to inside", AutomationProperties.GetName(ById<ListBox>(window, "listBox_Layers")));
                Assert.Equal("Panes of the chosen source", AutomationProperties.GetName(ById<ListView>(window, "listView_Panes")));
                Assert.Empty(Descendants<Expander>(window));

                // The performance labels say what the numbers are (and the stored glazing U-value is never called Uw).
                Assert.Contains("centre of pane", AutomationProperties.GetName(ById<TextBlock>(window, "textBlock_Ug")));
                Assert.Contains("frame layers", AutomationProperties.GetName(ById<TextBlock>(window, "textBlock_Uf")));
                Assert.StartsWith("Uw example", ById<TextBlock>(window, "textBlock_ReferenceUw").Text);
                Assert.Contains("no spacer Ψ", ById<TextBlock>(window, "textBlock_ReferenceUw").Text);
            }
        }

        // PR5: the frame's layers are worked on in the real window; the additional heat transfer is a label, never a box.
        [WpfFact]
        public async Task The_frame_layers_are_edited_with_named_controls_and_the_additional_heat_transfer_is_a_read_only_label()
        {
            using (Opened opened = await Open())
            {
                GlazingSystemBuilderWindow window = opened.Window;

                foreach (string id in new[] { "listBox_FrameLayers", "button_AddFrameLayer", "button_ReplaceFrameMaterial", "button_RemoveFrameLayer", "button_FrameLayerUp", "button_FrameLayerDown", "textBox_FrameMaterialSearch", "comboBox_FrameMaterial", "textBlock_FrameDepth", "textBlock_FrameAdditionalHeatTransfer", "textBlock_FrameAdditionalHeatTransferNote" })
                {
                    Assert.True(ById<FrameworkElement>(window, id) != null, id + " is missing");
                }

                // The additional heat transfer is shown, labelled read-only, in a TextBlock - and no editor of it exists anywhere in the window.
                TextBlock additional = ById<TextBlock>(window, "textBlock_FrameAdditionalHeatTransfer");
                Assert.Equal("10 %", additional.Text);
                Assert.Contains("read-only", AutomationProperties.GetName(additional));
                Assert.DoesNotContain(Descendants<TextBox>(window), x => (AutomationProperties.GetAutomationId(x) ?? string.Empty).Contains("AdditionalHeatTransfer") || (AutomationProperties.GetName(x) ?? string.Empty).Contains("additional heat transfer", StringComparison.OrdinalIgnoreCase));

                // The seed's frame (one 70 mm layer) is listed, with its thickness in a box; adding a layer from the chosen material shows a second row.
                ListBox list = ById<ListBox>(window, "listBox_FrameLayers");
                Assert.Single(list.Items);
                opened.ViewModel.SelectedFrameMaterial = opened.ViewModel.FrameMaterials.First();
                Flush();
                Button add = ById<Button>(window, "button_AddFrameLayer");
                Assert.True(add.IsEnabled);
                Press(add);
                await Pump(() => list.Items.Count == 2 && opened.ViewModel.PerformanceState != GlazingBuilderPerformanceState.Calculating, "the second frame layer");
                Assert.True(opened.ViewModel.Draft.Frame.IsEdited);

                TextBox thickness = Descendants<TextBox>(list).First(x => AutomationProperties.GetAutomationId(x) == "textBox_FrameLayerThickness");
                thickness.Text = "55";
                thickness.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                Flush();
                Assert.Equal(0.055, opened.ViewModel.Draft.Frame.EditableLayers[0].Thickness, 9);

                Press(ById<Button>(window, "button_RemoveFrameLayer"));
                Assert.Equal(1, list.Items.Count);                                      // the layer just added was the selected one
                Assert.Empty(opened.Library.Read().Systems);                           // nothing was saved
            }
        }

        [WpfFact]
        public async Task The_build_up_shows_each_layer_with_a_name_a_gap_has_its_own_boxes_and_the_status_is_in_words()
        {
            using (Opened opened = await Open())
            {
                ListBox list = ById<ListBox>(opened.Window, "listBox_Layers");
                Assert.Equal(3, list.Items.Count);
                Assert.Equal("OUTSIDE", ById<TextBlock>(opened.Window, "textBlock_Outside").Text);
                Assert.Equal("INSIDE", ById<TextBlock>(opened.Window, "textBlock_Inside").Text);

                List<ListBoxItem> items = Descendants<ListBoxItem>(list).ToList();
                Assert.StartsWith("Pane 1:", AutomationProperties.GetName(items[0]));
                Assert.StartsWith("Gap 1:", AutomationProperties.GetName(items[1]));
                Assert.StartsWith("Pane 2:", AutomationProperties.GetName(items[2]));

                ComboBox gas = ById<ComboBox>(items[1], "comboBox_GapGas");
                TextBox width = ById<TextBox>(items[1], "textBox_GapWidth");
                Assert.Equal("Air", (gas.SelectedItem as GlazingGasOption)?.Display);
                Assert.Equal("12", width.Text);
                Assert.Equal("Gap 1 gas", AutomationProperties.GetName(gas));
                Assert.Equal("Gap 1 width in millimetres", AutomationProperties.GetName(width));

                // Editing a box edits the draft (after Tab / focus loss) and the Builder keeps working.
                width.Text = "16";
                width.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                Flush();
                Assert.Equal(0.016, ((DraftGap)opened.ViewModel.Draft.Layers[1]).Thickness, 6);

                Assert.StartsWith("New · based on SEED_GLZ", ById<TextBlock>(opened.Window, "textBlock_Status").Text);
                Assert.StartsWith("✓", ById<TextBlock>(opened.Window, "textBlock_ValidationSummary").Text);
            }
        }

        [WpfFact]
        public async Task The_pane_list_is_virtualised_so_a_full_database_stays_responsive()
        {
            using (Opened opened = await Open(extraPanes: 11664))
            {
                ListView panes = ById<ListView>(opened.Window, "listView_Panes");
                Assert.Equal(11668, panes.Items.Count);
                Assert.True(VirtualizingPanel.GetIsVirtualizing(panes));
                Assert.True(ScrollViewer.GetCanContentScroll(panes));

                Flush();
                int realised = Descendants<ListViewItem>(panes).Count();
                Assert.InRange(realised, 1, 200);

                // Searching narrows the list without rebuilding the window.
                TextBox search = ById<TextBox>(opened.Window, "textBox_Search");
                search.Text = "pilkington";
                await Pump(() => opened.ViewModel.Panes.Entries.Count == 4, "the search");
                Assert.Equal(4, panes.Items.Count);
                Assert.Equal("Showing 4 of 11,668 panes.".Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator), ById<TextBlock>(opened.Window, "textBlock_PaneCount").Text);
            }
        }

        [WpfFact]
        public async Task The_buttons_add_replace_reverse_move_and_remove_layers_and_the_list_follows()
        {
            using (Opened opened = await Open())
            {
                GlazingBuilderViewModel viewModel = opened.ViewModel;
                ListBox layers = ById<ListBox>(opened.Window, "listBox_Layers");
                ListView panes = ById<ListView>(opened.Window, "listView_Panes");
                Button add = ById<Button>(opened.Window, "button_AddPane");
                Button replace = ById<Button>(opened.Window, "button_ReplacePane");

                // Nothing chosen in the browser: the pane buttons are off.
                Assert.False(add.IsEnabled);
                Assert.False(replace.IsEnabled);

                panes.SelectedItem = viewModel.Panes.Entries.Single(x => x.Name == BuilderFixture.LowE);
                Flush();
                Assert.True(add.IsEnabled);
                Assert.False(replace.IsEnabled);

                Press(add);
                Assert.Equal(5, layers.Items.Count);
                Assert.Equal(4, viewModel.SelectedLayer.Index);
                Assert.True(replace.IsEnabled);
                Assert.True(ById<Button>(opened.Window, "button_Reverse").IsEnabled);
                Assert.False(ById<Button>(opened.Window, "button_MoveDown").IsEnabled);

                Press(ById<Button>(opened.Window, "button_Reverse"));
                Assert.True(viewModel.SelectedLayer.Pane.Reversed);
                Assert.Contains("reversed", viewModel.SelectedLayer.Detail);

                Press(ById<Button>(opened.Window, "button_MoveUp"));
                Assert.Equal(3, viewModel.SelectedLayer.Index);

                Press(ById<Button>(opened.Window, "button_Remove"));
                Assert.Equal(4, layers.Items.Count);

                Press(ById<Button>(opened.Window, "button_AddGap"));
                Assert.Equal(5, layers.Items.Count);
            }
        }

        [WpfFact]
        public async Task Alt_Up_and_Alt_Down_reorder_and_Delete_removes_but_typing_in_a_gap_box_is_left_alone()
        {
            using (Opened opened = await Open())
            {
                GlazingBuilderViewModel viewModel = opened.ViewModel;
                GlazingSystemBuilderWindow window = opened.Window;
                ListBox list = ById<ListBox>(window, "listBox_Layers");

                viewModel.Panes.SelectedEntry = viewModel.Panes.Entries.Single(x => x.Name == BuilderFixture.LowE);
                viewModel.AddPane();
                Flush();
                string before = string.Join("|", viewModel.Draft.Panes.Select(x => x.OriginalName));
                Assert.Equal("Clear4|Clear4|LowE4", before);

                // Alt+Up on the selected low-e pane (the list is the source).
                Assert.True(window.HandleLayerKey(Key.Up, ModifierKeys.Alt, list));
                Assert.Equal(3, viewModel.SelectedLayer.Index);
                Assert.True(window.HandleLayerKey(Key.Up, ModifierKeys.Alt, list));
                Assert.Equal("Clear4|LowE4|Clear4", string.Join("|", viewModel.Draft.Panes.Select(x => x.OriginalName)));
                Assert.True(window.HandleLayerKey(Key.Down, ModifierKeys.Alt, list));
                Assert.Equal(3, viewModel.SelectedLayer.Index);

                // At the end of the list the key is still taken (no beep through to the list) but nothing moves.
                viewModel.SelectedLayer = viewModel.Layers[0];
                Assert.True(window.HandleLayerKey(Key.Up, ModifierKeys.Alt, list));
                Assert.Equal(0, viewModel.SelectedLayer.Index);

                // A plain arrow is the list's own; Delete removes the selected layer.
                Assert.False(window.HandleLayerKey(Key.Down, ModifierKeys.None, list));
                viewModel.SelectedLayer = viewModel.Layers[4];
                Assert.True(window.HandleLayerKey(Key.Delete, ModifierKeys.None, list));
                Assert.Equal(4, viewModel.Layers.Count);

                // Typing in a gap's boxes is never taken.
                await Pump(() => Descendants<TextBox>(list).Any(x => AutomationProperties.GetAutomationId(x) == "textBox_GapWidth"), "the rebuilt list to show its gaps");
                TextBox width = Descendants<TextBox>(list).First(x => AutomationProperties.GetAutomationId(x) == "textBox_GapWidth");
                Assert.False(window.HandleLayerKey(Key.Delete, ModifierKeys.None, width));
                Assert.False(window.HandleLayerKey(Key.Up, ModifierKeys.Alt, width));
                Assert.Equal(4, viewModel.Layers.Count);
            }
        }

        [WpfFact]
        public async Task Save_is_off_while_the_check_has_errors_and_names_the_findings()
        {
            using (Opened opened = await Open())
            {
                Button save = ById<Button>(opened.Window, "button_Save");
                TextBox name = ById<TextBox>(opened.Window, "textBox_Name");
                Assert.True(save.IsEnabled);
                Assert.Equal("SEED_GLZ (copy)", name.Text);

                name.Text = string.Empty;
                Flush();
                Assert.False(save.IsEnabled);
                Assert.StartsWith("✕ 1 error", ById<TextBlock>(opened.Window, "textBlock_ValidationSummary").Text);
                ListBox issues = ById<ListBox>(opened.Window, "listBox_Issues");
                Assert.Contains(issues.Items.Cast<GlazingBuilderIssueRow>(), x => x.Text.StartsWith("✕ Error:") && x.Text.Contains("needs a name"));

                name.Text = "E0 Double";
                Flush();
                Assert.True(save.IsEnabled);
            }
        }

        // ---- Save and Cancel -----------------------------------------------------------------------------------------

        [WpfFact]
        public async Task Save_as_predefined_writes_the_library_and_only_then_closes_the_window()
        {
            using (Opened opened = await Open())
            {
                bool closed = false;
                opened.Window.Closed += (sender, e) => closed = true;
                int saved = 0;
                opened.ViewModel.Saved += (sender, e) => saved++;

                ById<TextBox>(opened.Window, "textBox_Name").Text = "E0 Double";
                Flush();
                Assert.False(File.Exists(opened.Library.Path));

                Press(ById<Button>(opened.Window, "button_Save"));
                await Pump(() => closed, "the window to close after the save");

                Assert.Equal(1, saved);
                Assert.Equal("E0 Double", Assert.Single(opened.Library.Read().Systems).Name);
                Assert.False(opened.Window.IsLoaded);
            }
        }

        [WpfFact]
        public async Task A_Save_that_fails_keeps_the_window_open_and_says_why()
        {
            using (Opened opened = await Open())
            {
                Directory.CreateDirectory(Path.GetDirectoryName(opened.Library.Path));
                File.WriteAllText(opened.Library.Path, "{ not a library");
                bool closed = false;
                opened.Window.Closed += (sender, e) => closed = true;

                Press(ById<Button>(opened.Window, "button_Save"));
                await Pump(() => !string.IsNullOrEmpty(opened.ViewModel.SaveError) && !opened.ViewModel.IsSaving, "the failed save");

                Assert.False(closed);
                Assert.True(opened.Window.IsLoaded);
                Assert.False(string.IsNullOrWhiteSpace(ById<TextBlock>(opened.Window, "textBlock_SaveError").Text));
                Assert.Equal("{ not a library", File.ReadAllText(opened.Library.Path));
                Assert.True(ById<Button>(opened.Window, "button_Save").IsEnabled);
            }
        }

        [WpfFact]
        public async Task Cancel_closes_the_window_and_writes_nothing()
        {
            using (Opened opened = await Open())
            {
                bool closed = false;
                opened.Window.Closed += (sender, e) => closed = true;
                opened.ViewModel.AddGap();
                ById<TextBox>(opened.Window, "textBox_Name").Text = "Never saved";
                Flush();

                Press(ById<Button>(opened.Window, "button_Cancel"));
                await Pump(() => closed, "the window to close");

                Assert.False(File.Exists(opened.Library.Path));
                Assert.Null(opened.ViewModel.SavedSystem);
            }
        }

        [WpfFact]
        public async Task Closing_the_window_disposes_the_Builders_own_calculation()
        {
            using (Opened opened = await Open())
            {
                opened.Window.Close();
                Flush();

                // The view-model is disposed with the window: a late edit asks nothing of Tas.
                int calls = opened.Tas.Calls;
                opened.ViewModel.Layers[1].WidthText = "14";
                Assert.Equal(calls, opened.Tas.Calls);
            }
        }

        // ---- Create new… in the Thermal Performance panel ------------------------------------------------------------

        [WpfFact]
        public async Task Create_new_opens_the_Builder_saves_selects_the_new_system_and_Apply_is_one_undo()
        {
            string directory = NewDirectory();
            try
            {
                UserGlazingLibrary library = BuilderFixture.Library(directory);
                FakeGlazingEvaluator candidates = new FakeGlazingEvaluator();
                ThermalParts parts = ThermalFixture.Build();
                UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
                int modified = 0;
                int history = 0;
                ui.Modified += (sender, e) => modified++;
                ui.HistoryChanged += (sender, e) => history++;
                string before = ui.JSAMObject.ToJsonObject().ToJsonString();

                ThermalPerformanceControl control = new ThermalPerformanceControl(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => candidates, () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(),
                    () => null, () => new FakeSourceReader().Catalog(), () => library, () => new DraftGlazingEvaluator(new FakeDraftTas(), TimeSpan.Zero, null, BuilderFixture.Options()), BuilderFixture.Options()));
                control.Applier = set => Modify.ApplyThermalChange(ui, set, x => { }, null);
                System.Windows.Window host = new System.Windows.Window { Content = control, Left = 0, Top = 0, Width = 380, Height = 900, ShowActivated = false };
                host.Show();
                try
                {
                    AnalyticalModel model = ui.JSAMObject;
                    control.Update(model, new List<SAMObject>() { model.AdjacencyCluster.GetAperture(parts.Windows[0]) });
                    Flush();

                    // The button is not there until the list of systems is open.
                    ThermalRowEditor editor = control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
                    Button createNew = Descendants<Button>(control).First(x => AutomationProperties.GetAutomationId(x) == "button_CreateNew" && ReferenceEquals(x.DataContext, editor));
                    Assert.False(createNew.IsVisible);

                    Press(Descendants<Button>(control).First(x => AutomationProperties.GetAutomationId(x) == "button_Change" && ReferenceEquals(x.DataContext, editor)));
                    editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                    Flush();
                    Assert.True(createNew.IsVisible);
                    Assert.Equal("Create a new glazing system", AutomationProperties.GetName(createNew));

                    GlazingBuilderViewModel shown = null;
                    control.ShowBuilder = builder =>
                    {
                        shown = builder;
                        return null;
                    };

                    // Opened, and closed without saving: nothing changed.
                    Press(createNew);
                    Assert.NotNull(shown);
                    Assert.Equal("GLZ (copy)", shown.Name);
                    Assert.Null(editor.SelectedCandidate);
                    Assert.False(File.Exists(library.Path));
                    Assert.Equal(before, ui.JSAMObject.ToJsonObject().ToJsonString());

                    // Opened again, built and saved inside the (stand-in) dialog: the list refreshes, the new system is chosen.
                    Guid saved = Guid.Empty;
                    control.ShowBuilder = builder =>
                    {
                        // The pane lists are read in the background and arrive through the dispatcher: let them (a person's click comes later than this).
                        GlazingPaneSource library_Panes = builder.Panes.Sources.Single(x => x.Label == "SAM default library");
                        for (int i = 0; i < 2000 && !library_Panes.IsReady; i++)
                        {
                            Flush();
                            System.Threading.Thread.Sleep(5);
                        }

                        builder.Panes.SelectedSource = library_Panes;
                        builder.Panes.SelectedEntry = builder.Panes.Entries.Single(x => x.Name == GlazingFixture.LowE);
                        builder.SelectedLayer = builder.Layers[0];
                        builder.ReplacePane();
                        builder.Name = "E0 Double";
                        Task<bool> save = builder.SaveAsync();
                        for (int i = 0; i < 2000 && !save.IsCompleted; i++)
                        {
                            Flush();
                            System.Threading.Thread.Sleep(5);
                        }

                        Assert.True(save.IsCompleted && save.Result);
                        saved = builder.SavedSystem.Guid;
                        return true;
                    };

                    Press(createNew);
                    await Pump(() => editor.SelectedCandidate?.Guid == saved && saved != Guid.Empty, "the new system to be chosen");
                    editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                    Flush();

                    Assert.Equal("E0 Double", editor.SelectedCandidate.Name);
                    Assert.Single(editor.Candidates, x => x.Guid == saved);
                    Assert.Equal(0, modified);
                    Assert.Equal(0, history);
                    Assert.False(ui.CanUndo);
                    Assert.Equal(before, ui.JSAMObject.ToJsonObject().ToJsonString());

                    // Apply to the one selected window: one model change, one Undo.
                    editor.ScopeSelected = true;
                    Flush();
                    Press(ById<Button>(control, "button_Apply"));
                    Assert.Equal(1, modified);
                    Assert.True(ui.CanUndo);
                    Assert.Equal(new[] { parts.Windows[0] }, ui.JSAMObject.AdjacencyCluster.GetApertures().Where(x => x.TypeGuid == saved).Select(x => x.Guid));
                }
                finally
                {
                    host.Close();
                    control.Dispose();
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
