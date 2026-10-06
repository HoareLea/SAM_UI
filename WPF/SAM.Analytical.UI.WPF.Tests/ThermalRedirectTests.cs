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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage F2 (<c>documentation/Thermal-StageF-Final-Convergence.md</c>): the 3D right-click "Set U-value..." / "Set glazing..." - an element-assignment journey
    /// the Thermal Performance panel covers after F1 - open the panel for the selected elements instead of the Set windows. The menu items keep their names and
    /// headers; the panel follows the selection and the row of the elements starts editing (target U / <c>Change…</c>). Nothing is calculated for the model or
    /// written by the route; Apply is still one change and one Undo.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ThermalRedirectTests
    {
        // ---- The context-menu items ----------------------------------------------------------------------------------------

        [WpfFact]
        public void Set_U_value_and_Set_glazing_keep_their_names_carry_only_their_kind_of_element_and_say_where_they_go()
        {
            ThermalParts parts = ThermalFixture.Build();
            List<SAMObject> selection = new List<SAMObject>();
            selection.AddRange(parts.Model.AdjacencyCluster.GetPanels().Where(x => parts.WallPanels.Take(2).Contains(x.Guid)));
            selection.AddRange(parts.Model.AdjacencyCluster.GetApertures().Where(x => parts.Windows.Take(3).Contains(x.Guid)));
            selection.Add(selection[0]);

            int clicks = 0;
            MenuItem uValue = Create.MenuItem_ThermalPerformance(selection, false, (sender, e) => clicks++);
            MenuItem glazing = Create.MenuItem_ThermalPerformance(selection, true, null);

            Assert.Equal("MenuItem_SetUValue", uValue.Name);
            Assert.Equal("Set U-value...", uValue.Header);
            Assert.Equal(parts.WallPanels.Take(2), ((IEnumerable<SAMObject>)uValue.Tag).Select(x => x.Guid));
            Assert.Contains("Thermal Performance", (string)uValue.ToolTip);
            Assert.True(uValue.IsEnabled);

            Assert.Equal("MenuItem_SetGlazing", glazing.Name);
            Assert.Equal("Set glazing...", glazing.Header);
            Assert.Equal(parts.Windows.Take(3), ((IEnumerable<SAMObject>)glazing.Tag).Select(x => x.Guid));
            Assert.Contains("Thermal Performance", (string)glazing.ToolTip);

            uValue.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, clicks);

            Assert.False(Create.MenuItem_ThermalPerformance(new List<SAMObject>(), true, null).IsEnabled);
        }

        // ---- The panel: BeginEdit -------------------------------------------------------------------------------------------

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
            public ImmediateUValueEvaluator UValues = new ImmediateUValueEvaluator();
            public int Modified;
            public string Json;
        }

        private static Host Open()
        {
            Host host = new Host() { Parts = ThermalFixture.Build() };
            host.Ui = new UIAnalyticalModel(host.Parts.Model);
            host.Ui.Modified += (sender, e) => host.Modified++;
            host.Control = new ThermalPerformanceControl(new ThermalEditServices(() => host.UValues, () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore())));
            host.Control.Applier = set => Modify.ApplyThermalChange(host.Ui, set, x => { }, Tas);
            host.Window = new System.Windows.Window { Content = host.Control, Left = 0, Top = 0, Width = 380, Height = 1000, ShowActivated = false };
            host.Window.Show();
            host.Json = host.Ui.JSAMObject.ToJsonObject().ToJsonString();
            return host;
        }

        // What the analytical window does: the panel gets the view's selection, then the route asks for the row of the right-clicked elements.
        private static ThermalRowEditor RightClick(Host host, IEnumerable<Guid> selection, IEnumerable<Guid> elements)
        {
            AnalyticalModel model = host.Ui.JSAMObject;
            HashSet<Guid> guids = new HashSet<Guid>(selection);
            List<SAMObject> selected = model.AdjacencyCluster.GetPanels().Where(x => guids.Contains(x.Guid)).Cast<SAMObject>().Concat(model.AdjacencyCluster.GetApertures().Where(x => guids.Contains(x.Guid))).ToList();
            host.Control.Update(model, selected);
            ThermalRowEditor editor = host.Control.BeginEdit(elements);
            editor?.Glazing?.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            Flush();
            return editor;
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
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

        private static T Of<T>(Host host, ThermalRowEditor editor, string id) where T : FrameworkElement
        {
            return Descendants<T>(host.Control).FirstOrDefault(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id && ReferenceEquals(x.DataContext, editor));
        }

        private static void AssertModelUntouched(Host host)
        {
            Assert.Equal(0, host.Modified);
            Assert.False(host.Ui.CanUndo);
            Assert.Equal(host.Json, host.Ui.JSAMObject.ToJsonObject().ToJsonString());
        }

        [WpfFact]
        public void Set_glazing_on_selected_windows_opens_Change_on_their_row_and_Apply_is_one_change_scoped_to_the_selection()
        {
            Host host = Open();
            try
            {
                List<Guid> windows = host.Parts.Windows.Take(2).ToList();
                ThermalRowEditor editor = RightClick(host, windows, windows);

                Assert.NotNull(editor);
                Assert.True(editor.IsAperture);
                Assert.True(editor.ChangeOpen);
                Assert.True(Of<ListBox>(host, editor, "listBox_Candidates").IsVisible);
                Assert.Equal("2 elements selected · 1 construction", host.Control.ViewModel.Summary);
                Assert.Equal(new[] { GlazingFixture.CurrentGuid }, host.Control.ViewModel.Groups.SelectMany(x => x.Rows).Select(x => x.ConstructionGuid));
                Assert.Empty(host.UValues.Requests);
                AssertModelUntouched(host);

                // The rest is the panel's ordinary journey: choose, only the selected, Apply = one change, one Undo step.
                editor.SelectedCandidate = editor.Candidates.Single(x => x.Guid == GlazingFixture.BetterGuid);
                editor.ScopeSelected = true;
                Flush();
                Assert.Equal("1 change · 2 elements", host.Control.ViewModel.Session.SummaryText);
                host.Control.ViewModel.Apply(host.Control.Applier);

                Assert.Equal(1, host.Modified);
                Assert.True(host.Ui.CanUndo);
                Assert.Equal(windows.OrderBy(x => x), host.Ui.JSAMObject.AdjacencyCluster.GetApertures().Where(x => x.TypeGuid == GlazingFixture.BetterGuid).Select(x => x.Guid).OrderBy(x => x));
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void Set_U_value_on_selected_walls_puts_the_cursor_in_their_target_U_without_starting_a_calculation()
        {
            Host host = Open();
            try
            {
                List<Guid> walls = host.Parts.WallPanels.Take(3).ToList();
                ThermalRowEditor editor = RightClick(host, walls.Concat(host.Parts.Windows.Take(1)), walls);

                Assert.NotNull(editor);
                Assert.False(editor.IsAperture);
                Assert.Equal(host.Parts.Wall.Guid, editor.Row.ConstructionGuid);
                Assert.False(editor.IsEdited);

                TextBox target = Of<TextBox>(host, editor, "textBox_Target");
                Assert.NotNull(target);
                Assert.Same(target, FocusManager.GetFocusedElement(FocusManager.GetFocusScope(target)));

                // The window row of the same selection is shown but not opened; nothing was asked of Tas and nothing written.
                Assert.Equal(2, host.Control.ViewModel.Groups.SelectMany(x => x.Rows).Count());
                Assert.False(host.Control.ViewModel.Groups.SelectMany(x => x.Rows).Single(x => x.IsAperture).Editor.ChangeOpen);
                Assert.Empty(host.UValues.Requests);
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void Elements_of_two_constructions_show_both_rows_and_start_nothing()
        {
            Host host = Open();
            try
            {
                AnalyticalModel model = host.Ui.JSAMObject;
                List<Guid> panels = host.Parts.WallPanels.Take(2).Concat(model.AdjacencyCluster.GetPanels().Where(x => x.Construction?.Guid == host.Parts.OtherWall.Guid).Take(1).Select(x => x.Guid)).ToList();

                ThermalRowEditor editor = RightClick(host, panels, panels);

                Assert.Null(editor);
                Assert.Equal(2, host.Control.ViewModel.Groups.SelectMany(x => x.Rows).Count());
                Assert.All(host.Control.ViewModel.Groups.SelectMany(x => x.Rows), x => Assert.False(x.Editor.IsEdited));
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void From_the_whole_envelope_the_panel_returns_to_the_selection()
        {
            Host host = Open();
            try
            {
                host.Control.Update(host.Ui.JSAMObject, new List<SAMObject>());
                host.Control.ViewModel.Mode = ThermalPerformanceMode.WholeEnvelope;

                List<Guid> windows = host.Parts.Windows.Take(1).ToList();
                ThermalRowEditor editor = RightClick(host, windows, windows);

                Assert.Equal(ThermalPerformanceMode.Selection, host.Control.ViewModel.Mode);
                Assert.NotNull(editor);
                Assert.True(editor.ChangeOpen);
                Assert.Equal(new[] { host.Parts.Windows[0] }, editor.Row.SelectedGuids);
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void While_an_edit_is_pending_its_pinned_rows_stay_and_only_a_pinned_row_can_be_opened()
        {
            Host host = Open();
            try
            {
                // A pending target on the walls, with one window in the same (pinned) selection.
                List<Guid> walls = host.Parts.WallPanels.Take(3).ToList();
                List<Guid> window = host.Parts.Windows.Take(1).ToList();
                ThermalRowEditor wall = RightClick(host, walls.Concat(window), walls);
                wall.TargetText = "0.30";
                wall.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Flush();
                Assert.True(host.Control.ViewModel.Session.IsPending);

                // Right-click Set glazing on that window: its pinned row opens Change; the wall edit is kept.
                ThermalRowEditor glazing = RightClick(host, window, window);
                Assert.NotNull(glazing);
                Assert.True(glazing.ChangeOpen);
                Assert.Equal("0.30", wall.TargetText);

                // Elements outside the pinned rows: nothing opens, the rows stay pinned.
                List<Guid> other = host.Parts.Windows.Skip(4).Take(1).ToList();
                Assert.Null(RightClick(host, other, other));
                Assert.Equal(new[] { host.Parts.Windows[0] }, host.Control.ViewModel.Groups.SelectMany(x => x.Rows).Single(x => x.IsAperture).SelectedGuids);
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }
    }
}
