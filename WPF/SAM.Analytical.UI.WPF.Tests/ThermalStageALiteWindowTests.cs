// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Thermal Stage A-lite, as a person meets it in the two windows: the scope (all N using it / only the M selected) is in the
    /// main area and not under Advanced, Keep name explains itself, a glazing candidate is marked before it is chosen, warnings
    /// are amber, and the candidate rows are 24 px with truncated text ending in an ellipsis. Cancel changes nothing; Apply is one
    /// Undo step.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ThermalStageALiteWindowTests
    {
        private static readonly Guid RoofGuid = new Guid("a0000000-0000-4000-8000-0000000000a1");

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private static SetUValueWindow UValueWindow(UIAnalyticalModel uIAnalyticalModel, Guid constructionGuid, IEnumerable<Guid> selected)
        {
            SetUValueWindow window = new SetUValueWindow(uIAnalyticalModel, constructionGuid, selected, new ImmediateUValueEvaluator())
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 0,
                Top = 0,
                ShowActivated = false,
            };

            window.ApplyFunction = x => Modify.SetUValue(uIAnalyticalModel, x, null);
            return window;
        }

        private static SetGlazingWindow GlazingWindow(UIAnalyticalModel uIAnalyticalModel, IEnumerable<Guid> selected)
        {
            SetGlazingWindow window = new SetGlazingWindow(uIAnalyticalModel, GlazingFixture.CurrentGuid, selected, new FakeGlazingEvaluator(), () => GlazingFixture.Library(GlazingFixture.RoofSystem(RoofGuid)))
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 0,
                Top = 0,
                ShowActivated = false,
            };

            window.ApplyFunction = x => Modify.SetGlazing(uIAnalyticalModel, x, Tas);
            return window;
        }

        private static T Control<T>(System.Windows.Window window, string name) where T : FrameworkElement
        {
            return (T)window.FindName(name);
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        private static void Settle(SetGlazingWindow window)
        {
            window.ViewModel.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            Flush();
        }

        private static void Type(SetUValueWindow window, string text)
        {
            Control<TextBox>(window, "textBox_Target").Text = text;
            window.ViewModel.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            Flush();
        }

        // Is the element inside the other, by the logical tree (the Advanced expander's content is not in the visual tree while collapsed).
        private static bool IsInside(DependencyObject element, DependencyObject ancestor)
        {
            for (DependencyObject parent = LogicalTreeHelper.GetParent(element); parent != null; parent = LogicalTreeHelper.GetParent(parent))
            {
                if (ReferenceEquals(parent, ancestor))
                {
                    return true;
                }
            }

            return false;
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

        // ---- The scope is in the main area ---------------------------------------------------------------------------

        [WpfFact]
        public void SetUValue_TheScopeIsInTheMainArea_NotUnderAdvanced_AndTheLabelsCarryTheCounts()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            SetUValueWindow window = UValueWindow(new UIAnalyticalModel(analyticalModel), construction.Guid, UValueFixture.PanelGuids(analyticalModel, construction).Take(3));

            try
            {
                window.Show();
                Flush();

                Expander advanced = Control<Expander>(window, "expander_Advanced");
                Assert.False(advanced.IsExpanded);

                RadioButton all = Control<RadioButton>(window, "radioButton_AllPanels");
                RadioButton selected = Control<RadioButton>(window, "radioButton_SelectedPanels");
                Assert.False(IsInside(all, advanced));
                Assert.False(IsInside(selected, advanced));
                Assert.True(all.IsVisible);
                Assert.True(selected.IsVisible);
                Assert.Equal("All 12 panels using it", all.Content);
                Assert.Equal("Only the 3 selected", selected.Content);
                Assert.True(selected.IsEnabled);

                // What stays under Advanced: the rare overrides.
                Assert.True(IsInside(Control<CheckBox>(window, "checkBox_KeepName"), advanced));
                Assert.True(IsInside(Control<CheckBox>(window, "checkBox_DontAssign"), advanced));
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void SetGlazing_TheScopeIsInTheMainArea_NotUnderAdvanced_AndTheLabelsCarryTheCounts()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = GlazingWindow(new UIAnalyticalModel(analyticalModel), GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(2));

            try
            {
                window.Show();
                Settle(window);

                Expander advanced = Control<Expander>(window, "expander_Advanced");
                Assert.False(advanced.IsExpanded);

                RadioButton all = Control<RadioButton>(window, "radioButton_AllApertures");
                RadioButton selected = Control<RadioButton>(window, "radioButton_SelectedApertures");
                Assert.False(IsInside(all, advanced));
                Assert.False(IsInside(selected, advanced));
                Assert.True(all.IsVisible);
                Assert.True(selected.IsVisible);
                Assert.Equal("All 20 apertures using it", all.Content);
                Assert.Equal("Only the 2 selected", selected.Content);

                Assert.True(IsInside(Control<CheckBox>(window, "checkBox_DontAssign"), advanced));
                Assert.True(IsInside(Control<CheckBox>(window, "checkBox_IncludeLibrary"), advanced));
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void SetGlazing_WhenSelectedApertureUsesAnotherSystem_OnlyTheSelectedIsDisabled_AndSaysWhy()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(windows: 4, other: GlazingFixture.System(GlazingFixture.BetterGuid, "Other", ApertureType.Window, GlazingFixture.Clear), others: 2);
            List<Guid> others = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.BetterGuid);
            SetGlazingWindow window = GlazingWindow(new UIAnalyticalModel(analyticalModel), others);

            try
            {
                window.Show();
                Settle(window);

                Assert.False(Control<RadioButton>(window, "radioButton_SelectedApertures").IsEnabled);
                Assert.Equal("No selected aperture uses GLZ.", Control<TextBlock>(window, "textBlock_ScopeReason").Text);
                Assert.Equal(Visibility.Visible, Control<TextBlock>(window, "textBlock_ScopeReason").Visibility);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Keep name ---------------------------------------------------------------------------------------------

        [WpfFact]
        public void SetUValue_KeepNameAndOnlyTheSelected_ExcludeEachOther_WithTheReasonShown()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            SetUValueWindow window = UValueWindow(new UIAnalyticalModel(analyticalModel), construction.Guid, UValueFixture.PanelGuids(analyticalModel, construction).Take(2));

            try
            {
                window.Show();
                Flush();
                Type(window, "0.3");

                CheckBox keepName = Control<CheckBox>(window, "checkBox_KeepName");
                Assert.True(keepName.IsEnabled);
                Assert.Equal(Visibility.Collapsed, Control<TextBlock>(window, "textBlock_KeepNameReason").Visibility);

                Control<RadioButton>(window, "radioButton_SelectedPanels").IsChecked = true;
                Flush();
                Assert.False(keepName.IsEnabled);
                Assert.Equal(Visibility.Visible, Control<TextBlock>(window, "textBlock_KeepNameReason").Visibility);
                Assert.Contains("cannot be combined with only the selected panels", Control<TextBlock>(window, "textBlock_KeepNameReason").Text);

                Control<RadioButton>(window, "radioButton_AllPanels").IsChecked = true;
                Flush();
                keepName.IsChecked = true;
                Flush();
                Assert.False(Control<RadioButton>(window, "radioButton_SelectedPanels").IsEnabled);
                Assert.Equal(Visibility.Visible, Control<TextBlock>(window, "textBlock_ScopeReason").Visibility);
                Assert.Equal("Applies to all 12 panels using SIM_EXT_SLD (2 selected), keeping the name.", Control<TextBlock>(window, "textBlock_Scope").Text);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void SetUValue_KeepNameIsDisabled_WhenAnotherConstructionSharesTheName()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, 12, 0, UValueFixture.Wall());
            SetUValueWindow window = UValueWindow(new UIAnalyticalModel(analyticalModel), construction.Guid, null);

            try
            {
                window.Show();
                Flush();

                Assert.False(Control<CheckBox>(window, "checkBox_KeepName").IsEnabled);
                Assert.Equal("Keep name is unavailable: 1 other construction is also named SIM_EXT_SLD and would change too.", Control<TextBlock>(window, "textBlock_KeepNameReason").Text);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- One scope, one Undo step; Cancel changes nothing ---------------------------------------------------------

        [WpfFact]
        public void SetUValue_ChoosingOnlyTheSelectedInTheMainArea_AppliesToThose_InOneUndoStep()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int modified = 0;
            int historyChanged = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            uIAnalyticalModel.HistoryChanged += (sender, e) => historyChanged++;
            SetUValueWindow window = UValueWindow(uIAnalyticalModel, construction.Guid, UValueFixture.PanelGuids(analyticalModel, construction).Take(3));

            try
            {
                window.Show();
                Flush();
                Type(window, "0.3");
                Control<RadioButton>(window, "radioButton_SelectedPanels").IsChecked = true;
                Flush();
                window.ViewModel.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Flush();

                Assert.True(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.True(window.Apply());

                Assert.Equal(ThermalApplyScope.SelectedOnly, window.Result.Scope);
                Assert.Equal(3, window.Result.PanelCount);
                Assert.Equal(1, modified);
                Assert.Equal(1, historyChanged);
                Assert.True(uIAnalyticalModel.CanUndo);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void SetUValue_Cancel_LeavesTheModelAndItsHistoryUntouched_EvenWithEveryOptionChanged()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            string before = uIAnalyticalModel.JSAMObject.ToJsonObject().ToJsonString();
            int modified = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            SetUValueWindow window = UValueWindow(uIAnalyticalModel, construction.Guid, UValueFixture.PanelGuids(analyticalModel, construction).Take(3));

            try
            {
                window.Show();
                Flush();
                Type(window, "0.3");
                Control<RadioButton>(window, "radioButton_SelectedPanels").IsChecked = true;
                Control<CheckBox>(window, "checkBox_DontAssign").IsChecked = true;
                Flush();

                Control<Button>(window, "button_Cancel").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Flush();

                Assert.Equal(0, modified);
                Assert.False(uIAnalyticalModel.CanUndo);
                Assert.Equal(before, uIAnalyticalModel.JSAMObject.ToJsonObject().ToJsonString());
                Assert.Null(window.Result);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Glazing: marked before it is chosen, amber, Cancel changes nothing -------------------------------------

        [WpfFact]
        public void SetGlazing_ACandidateIsMarkedInTheTableBeforeItIsChosen_AndTheWarningIsAmberAboveApply()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            string before = uIAnalyticalModel.JSAMObject.ToJsonObject().ToJsonString();
            int modified = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            SetGlazingWindow window = GlazingWindow(uIAnalyticalModel, null);

            try
            {
                window.Show();
                Settle(window);

                DataGrid grid = Control<DataGrid>(window, "dataGrid_Candidates");
                GlazingCandidateRow roof = grid.Items.Cast<GlazingCandidateRow>().First(x => x.Guid == RoofGuid);
                GlazingCandidateRow paneOnly = grid.Items.Cast<GlazingCandidateRow>().First(x => x.Guid == GlazingFixture.PaneOnlyGuid);

                // Marked in the table, with nothing chosen yet.
                Assert.Null(window.ViewModel.ProposedRow);
                Assert.Equal("⚠ made for roofs", roof.WarningText);
                Assert.Equal("⚠ no frame", paneOnly.WarningText);
                Assert.Contains(grid.Columns, x => (string)x.Header == "Check");

                // Chosen: the full warning above Apply, amber, and Apply stays enabled (a warning does not block).
                grid.SelectedItem = roof;
                Settle(window);
                window.UpdateLayout();

                ItemsControl warnings = Control<ItemsControl>(window, "itemsControl_Warnings");
                List<string> texts = warnings.Items.Cast<string>().ToList();
                Assert.Contains(texts, x => x.StartsWith("GLZ_Roof is made for roofs", StringComparison.Ordinal));
                Assert.True(Control<Button>(window, "button_Apply").IsEnabled);

                SolidColorBrush amber = (SolidColorBrush)window.FindResource("PartO.Brush.Warning");
                SolidColorBrush danger = (SolidColorBrush)window.FindResource("PartO.Brush.Danger");
                Assert.Equal(Color.FromRgb(0x9A, 0x67, 0x00), amber.Color);
                Assert.NotEqual(danger.Color, amber.Color);

                TextBlock glyph = Descendants<TextBlock>(warnings).First(x => x.Text == "⚠");
                Assert.Equal(amber.Color, ((SolidColorBrush)glyph.Foreground).Color);

                // Cancel: nothing was written, whatever was marked or chosen.
                Control<Button>(window, "button_Cancel").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Flush();
                Assert.Equal(0, modified);
                Assert.False(uIAnalyticalModel.CanUndo);
                Assert.Equal(before, uIAnalyticalModel.JSAMObject.ToJsonObject().ToJsonString());
                Assert.Null(window.Result);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void SetGlazing_AChosenSystemWithWarnings_AppliesToTheSelectedOnly_InOneUndoStep()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int modified = 0;
            int historyChanged = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            uIAnalyticalModel.HistoryChanged += (sender, e) => historyChanged++;
            SetGlazingWindow window = GlazingWindow(uIAnalyticalModel, GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(2));

            try
            {
                window.Show();
                Settle(window);

                Control<RadioButton>(window, "radioButton_SelectedApertures").IsChecked = true;
                Flush();
                Control<DataGrid>(window, "dataGrid_Candidates").SelectedItem = Control<DataGrid>(window, "dataGrid_Candidates").Items.Cast<GlazingCandidateRow>().First(x => x.Guid == GlazingFixture.PaneOnlyGuid);
                Settle(window);

                Assert.Contains(Control<ItemsControl>(window, "itemsControl_Warnings").Items.Cast<string>(), x => x.Contains("lose their frame"));
                Assert.True(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.True(window.Apply());

                Assert.Equal(ThermalApplyScope.SelectedOnly, window.Result.Scope);
                Assert.Equal(2, window.Result.ApertureCount);
                Assert.Equal(1, modified);
                Assert.Equal(1, historyChanged);
                Assert.True(uIAnalyticalModel.CanUndo);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Candidate rows --------------------------------------------------------------------------------------------

        [WpfFact]
        public void SetGlazing_TheCandidateRowsAre24Px_AndTextThatIsCutShortEndsInAnEllipsis()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = GlazingWindow(new UIAnalyticalModel(analyticalModel), null);

            try
            {
                window.Show();
                Settle(window);

                DataGrid grid = Control<DataGrid>(window, "dataGrid_Candidates");
                Assert.Equal(24, grid.RowHeight);

                foreach (string header in new[] { "System", "Source", "Pane", "Frame", "Status", "Check" })
                {
                    DataGridTextColumn column = (DataGridTextColumn)grid.Columns.First(x => (string)x.Header == header);
                    Style style = column.ElementStyle;
                    Assert.NotNull(style);
                    Assert.Contains(style.Setters.OfType<Setter>().Concat(style.BasedOn?.Setters.OfType<Setter>() ?? Enumerable.Empty<Setter>()), x => x.Property == TextBlock.TextTrimmingProperty && (TextTrimming)x.Value == TextTrimming.CharacterEllipsis);
                }

                // The numeric cells are centred in the taller row too.
                foreach (string header in new[] { "Ug", "Uf", "g", "Light", "Uw", "Margin" })
                {
                    DataGridTextColumn column = (DataGridTextColumn)grid.Columns.First(x => (string)x.Header == header);
                    Assert.Contains(column.ElementStyle.Setters.OfType<Setter>(), x => x.Property == FrameworkElement.VerticalAlignmentProperty && (VerticalAlignment)x.Value == VerticalAlignment.Center);
                }
            }
            finally
            {
                window.Close();
            }
        }
    }
}
