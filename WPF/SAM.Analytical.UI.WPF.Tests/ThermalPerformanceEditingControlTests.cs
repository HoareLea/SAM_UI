// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Thermal Stage C in the control as a person meets it: type a target into the wall row, see the preview, the scope and the check
    /// before Apply, press Apply and get one commit and a one-line result; open Change... on the window row and choose a system;
    /// Discard changes nothing. Runs the real XAML over the fake calculations.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ThermalPerformanceEditingControlTests
    {
        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private sealed class Host
        {
            public ThermalParts Parts;
            public UIAnalyticalModel Ui;
            public ThermalPerformanceControl Control;
            public System.Windows.Window Window;
            public int Modified;
            public int HistoryChanged;
            public List<Guid> Selected = new List<Guid>();
        }

        private static Host Open(IEnumerable<int> wallIndexes, IEnumerable<int> windowIndexes, Func<GlazingSource> constructionLibrary = null)
        {
            Host host = new Host() { Parts = ThermalFixture.Build() };
            host.Ui = new UIAnalyticalModel(host.Parts.Model);
            host.Ui.Modified += (sender, e) => host.Modified++;
            host.Ui.HistoryChanged += (sender, e) => host.HistoryChanged++;

            host.Control = new ThermalPerformanceControl(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), constructionLibrary ?? (() => null), () => new ThermalSourceCatalog(new InMemoryThermalSourceStore())));
            host.Control.Applier = set => Modify.ApplyThermalChange(host.Ui, set, x => { }, Tas);

            // The way the analytical window drives it: the model it has now, the selection of the view, and Modified -> Update.
            host.Selected.AddRange(wallIndexes.Select(x => host.Parts.WallPanels[x]));
            host.Selected.AddRange(windowIndexes.Select(x => host.Parts.Windows[x]));
            host.Ui.Modified += (sender, e) => Refresh(host, true);

            host.Window = new System.Windows.Window { Content = host.Control, Left = 0, Top = 0, Width = 380, Height = 900, ShowActivated = false };
            host.Window.Show();
            Refresh(host, false);
            return host;
        }

        private static void Refresh(Host host, bool modelChanged)
        {
            AnalyticalModel model = host.Ui.JSAMObject;
            List<SAMObject> selected = new List<SAMObject>();
            selected.AddRange(model.AdjacencyCluster.GetPanels().Where(x => host.Selected.Contains(x.Guid)));
            selected.AddRange(model.AdjacencyCluster.GetApertures().Where(x => host.Selected.Contains(x.Guid)));
            host.Control.Update(model, selected, modelChanged);
            Flush();
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
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
            return Descendants<T>(root).FirstOrDefault(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id);
        }

        private static T ByName<T>(DependencyObject root, string id, string name) where T : FrameworkElement
        {
            return Descendants<T>(root).FirstOrDefault(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id && System.Windows.Automation.AutomationProperties.GetName(x) == name);
        }

        private static void Press(Button button)
        {
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Flush();
        }

        private static void Type(Host host, string target)
        {
            TextBox textBox = ByName<TextBox>(host.Control, "textBox_Target", "Target U " + host.Parts.Wall.Name);
            Assert.NotNull(textBox);
            textBox.Text = target;
            Flush();
            ThermalRowEditor editor = host.Control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionGuid == host.Parts.Wall.Guid).Editor;
            editor.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            Flush();
        }

        private static string Text(Host host, string id)
        {
            return ById<TextBlock>(host.Control, id)?.Text;
        }

        [WpfFact]
        public void Typing_a_target_shows_the_preview_the_scope_and_the_check_and_Apply_commits_once()
        {
            Host host = Open(new[] { 0, 1, 2 }, Array.Empty<int>());
            try
            {
                Button apply = ById<Button>(host.Control, "button_Apply");
                Assert.NotEqual(Visibility.Visible, ((StackPanel)host.Control.FindName("stackPanel_Pending")).Visibility);

                Type(host, "0.30");

                Assert.Contains("→", Text(host, "textBlock_Preview"));
                Assert.Contains("mm", Text(host, "textBlock_Preview"));
                Assert.Equal("Before apply: ✓ No new warnings", Text(host, "textBlock_Check"));
                Assert.Equal("1 change · 12 elements", Text(host, "textBlock_ChangeSummary"));
                Assert.Contains("pinned", Text(host, "textBlock_Pinned"));
                Assert.True(apply.IsEnabled);
                Assert.False(host.Control.FindName("radioButton_Selection") is RadioButton { IsEnabled: true }, "the mode cannot be switched under an edit");

                Press(apply);

                Assert.Equal(1, host.Modified);
                Assert.Equal(1, host.HistoryChanged);
                Assert.True(host.Ui.CanUndo);
                Assert.Contains("now", Text(host, "textBlock_Result"));
                Assert.EndsWith("One Undo reverts it.", Text(host, "textBlock_Result").Split('\n')[0].Trim());
                Assert.False(host.Control.ViewModel.Session.IsPending);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void Changing_the_selection_while_a_target_is_typed_does_not_change_the_pinned_scope()
        {
            Host host = Open(new[] { 0, 1, 2 }, Array.Empty<int>());
            try
            {
                Type(host, "0.30");
                string summary = Text(host, "textBlock_ChangeSummary");

                host.Selected.Clear();
                host.Selected.AddRange(host.Parts.WallPanels.Skip(6));
                Refresh(host, false);

                Assert.Equal(summary, Text(host, "textBlock_ChangeSummary"));
                Assert.True(host.Control.ViewModel.Session.IsPending);
                Assert.Equal(host.Parts.WallPanels.Take(3).OrderBy(x => x), host.Control.ViewModel.Session.BuildChangeSet().UValueRequests.Single().SelectedPanelGuids.OrderBy(x => x));
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void An_Undo_or_other_edit_of_the_model_clears_the_pending_change_and_the_panel_says_so()
        {
            Host host = Open(new[] { 0, 1, 2 }, Array.Empty<int>());
            try
            {
                Type(host, "0.30");

                // Another editor replaces the model.
                host.Ui.SetJSAMObject(host.Ui.JSAMObject, new SAM.Core.UI.FullModification());
                Flush();

                Assert.False(host.Control.ViewModel.Session.IsPending);
                Assert.Contains("discarded", Text(host, "textBlock_Notice"));
                Assert.Equal(Visibility.Collapsed, ((StackPanel)host.Control.FindName("stackPanel_Pending")).Visibility);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void Discard_changes_nothing()
        {
            Host host = Open(new[] { 0, 1, 2 }, Array.Empty<int>());
            try
            {
                Type(host, "0.30");

                Press(ById<Button>(host.Control, "button_Discard"));

                Assert.Equal(0, host.Modified);
                Assert.Equal(0, host.HistoryChanged);
                Assert.False(host.Ui.CanUndo);
                Assert.False(host.Control.ViewModel.Session.IsPending);
                Assert.Equal(string.Empty, ByName<TextBox>(host.Control, "textBox_Target", "Target U " + host.Parts.Wall.Name).Text);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void A_wall_and_a_window_are_changed_together_with_one_Apply()
        {
            Host host = Open(new[] { 0, 1, 2 }, new[] { 0, 1 });
            try
            {
                Type(host, "0.30");

                Button change = ByName<Button>(host.Control, "button_Change", "Change " + GlazingFixture.CurrentName);
                Assert.NotNull(change);
                Press(change);

                ThermalRowEditor windows = host.Control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
                windows.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Flush();

                // Every row carries a (collapsed) list of systems; the one of the window row is the one whose editor it is.
                ListBox list = Descendants<ListBox>(host.Control).First(x => ReferenceEquals(x.DataContext, windows) && System.Windows.Automation.AutomationProperties.GetAutomationId(x) == "listBox_Candidates");
                Assert.True(list.IsVisible);
                Assert.Equal(windows.Candidates.Count, list.Items.Count);
                Assert.Contains(windows.Candidates, x => x.Guid == GlazingFixture.BetterGuid);
                list.SelectedItem = windows.Candidates.First(x => x.Guid == GlazingFixture.BetterGuid);
                Flush();

                Assert.Equal("2 changes · 18 elements", Text(host, "textBlock_ChangeSummary"));
                Assert.StartsWith("Before apply:", Text(host, "textBlock_Check"));

                Press(ById<Button>(host.Control, "button_Apply"));

                Assert.Equal(1, host.Modified);
                Assert.Equal(1, host.HistoryChanged);
                Assert.All(host.Ui.JSAMObject.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.BetterGuid, x.TypeGuid));
                Assert.Contains("apertures now", Text(host, "textBlock_Result"));
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void Typing_a_target_lists_the_existing_constructions_beside_the_generated_variant_and_choosing_one_applies_it_as_one_commit()
        {
            Host host = Open(new[] { 0, 1, 2 }, Array.Empty<int>(), () => AlternativesFixture.Library());
            try
            {
                ThermalRowEditor editor = host.Control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionGuid == host.Parts.Wall.Guid).Editor;
                Assert.Null(editor.Alternatives);

                Type(host, "0.18");
                editor.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));
                Flush();

                // The list is under the target, with the generated variant first and a count line; nothing is chosen for the user.
                ListBox list = ById<ListBox>(host.Control, "listBox_Alternatives");
                Assert.NotNull(list);
                Assert.True(list.IsVisible);
                Assert.Equal(editor.AlternativeRows.Count, list.Items.Count);
                Assert.True(((ConstructionAlternativeRow)list.Items[0]).IsGenerated);
                Assert.Same(list.Items[0], list.SelectedItem);
                Assert.Contains("existing constructions meet U 0.18", Text(host, "textBlock_AlternativesCount"));
                Assert.Contains("→", Text(host, "textBlock_Preview"));
                Assert.Equal("1 change · 12 elements", Text(host, "textBlock_ChangeSummary"));

                // Choosing an existing one makes it the row's change: the preview names it and Apply assigns it.
                list.SelectedItem = editor.AlternativeRows.Single(x => x.Guid == AlternativesFixture.LibraryThickGuid);
                Flush();
                Assert.Contains("LIB_THICK (Library)", Text(host, "textBlock_Preview"));
                Assert.Equal("1 change · 12 elements", Text(host, "textBlock_ChangeSummary"));
                Assert.StartsWith("Before apply:", Text(host, "textBlock_Check"));

                Press(ById<Button>(host.Control, "button_Apply"));

                Assert.Equal(1, host.Modified);
                Assert.Equal(1, host.HistoryChanged);
                Assert.Equal(12, host.Ui.JSAMObject.AdjacencyCluster.GetPanels().Count(x => x.TypeGuid == AlternativesFixture.LibraryThickGuid));
                Assert.Contains("12 panels now LIB_THICK", Text(host, "textBlock_Result"));
            }
            finally
            {
                host.Window.Close();
            }
        }
    }
}
