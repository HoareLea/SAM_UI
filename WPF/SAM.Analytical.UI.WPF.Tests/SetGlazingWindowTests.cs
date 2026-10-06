// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value PR3: the "Set glazing" window as a person meets it - rendered at its smallest size, typed into, applied,
    /// "Load more glazing..." then cancelled - on the fixture model with the evaluator on <see cref="FakeGlazingEvaluator"/>
    /// and Apply without the Tas aperture-parameter calculation. Real-app acceptance is in the PR record.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class SetGlazingWindowTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_SetGlazingWindowTests_" + Guid.NewGuid().ToString("N"));

        public SetGlazingWindowTests()
        {
            Directory.CreateDirectory(directory);
            GlazingSourceCache.Directory = Path.Combine(directory, "cache");
        }

        public void Dispose()
        {
            GlazingSourceCache.Directory = null;
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private static SetGlazingWindow Window(UIAnalyticalModel uIAnalyticalModel, Guid? apertureConstructionGuid, IEnumerable<Guid> selected = null, FakeGlazingEvaluator evaluator = null)
        {
            SetGlazingWindow window = new SetGlazingWindow(uIAnalyticalModel, apertureConstructionGuid, selected, evaluator ?? new FakeGlazingEvaluator(), () => GlazingFixture.Library())
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

        private static void Type(SetGlazingWindow window, string text)
        {
            Control<TextBox>(window, "textBox_Target").Text = text;
            Settle(window);
        }

        private static string Text(SetGlazingWindow window, string name) => Control<TextBlock>(window, name).Text;

        private static int Rows(SetGlazingWindow window) => Control<DataGrid>(window, "dataGrid_Candidates").Items.Count;

        // ---- Opening -------------------------------------------------------------------------------------------

        [WpfFact]
        public void OpenedFromApertures_ShowsTheCurrentSystem_TheTable_AndTheInlineScope_ApplyDisabled()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(3).ToList();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid, selected);

            try
            {
                window.Show();
                Settle(window);

                Assert.Equal(GlazingFixture.CurrentGuid, ((SetGlazingWindow.ApertureConstructionItem)Control<ComboBox>(window, "comboBox_ApertureConstruction").SelectedItem).Guid);
                Assert.StartsWith("Current Ug 1.40 · Uf 2.00 · g 0.60 · light 0.78 · Uw 1.47 W/m²K  ·  frame: yes  ·  used by 20 apertures (3 selected)", Text(window, "textBlock_Facts"));
                Assert.Equal("Applies to 20 apertures using GLZ (3 selected).", Text(window, "textBlock_Scope"));
                Assert.Equal(4, Rows(window));
                Assert.Equal("Showing 4 of 4 systems.", Text(window, "textBlock_Count").Substring(0, "Showing 4 of 4 systems.".Length));
                Assert.False(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.False(Control<Expander>(window, "expander_Advanced").IsExpanded);
                Assert.Equal("Choose a glazing system from the table.", Text(window, "textBlock_Result"));

                // Nothing chosen yet: a neutral dash, not a tick.
                Assert.Equal("–", Text(window, "textBlock_StatusGlyph"));
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void OpenedFromTools_HasNoSystemYet_AndNothingToApply()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), null);

            try
            {
                window.Show();
                Flush();

                Assert.Null(window.ViewModel);
                Assert.Null(Control<ComboBox>(window, "comboBox_ApertureConstruction").SelectedItem);
                Assert.Single(Control<ComboBox>(window, "comboBox_ApertureConstruction").Items);
                Assert.Equal("Choose an aperture construction.", Text(window, "textBlock_Status"));
                Assert.False(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.False(Control<Button>(window, "button_LoadMore").IsEnabled);
                Assert.False(window.Apply());
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void ChoosingAnApertureConstruction_InThePicker_OpensIt()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), null);

            try
            {
                window.Show();
                Control<ComboBox>(window, "comboBox_ApertureConstruction").SelectedIndex = 0;
                Settle(window);

                Assert.Equal(GlazingFixture.CurrentGuid, window.ViewModel.ApertureConstructionGuid);
                Assert.Equal(4, Rows(window));
                Assert.Equal("GLZ   (20 apertures, window)", ((SetGlazingWindow.ApertureConstructionItem)Control<ComboBox>(window, "comboBox_ApertureConstruction").SelectedItem).Display);
                Assert.Equal("GLZ   (20 apertures, window)", Control<ComboBox>(window, "comboBox_ApertureConstruction").SelectedItem.ToString());
            }
            finally
            {
                window.Close();
            }
        }

        // ---- The comparison ------------------------------------------------------------------------------------

        [WpfFact]
        public void TypingATarget_FiltersTheTable_ChoosesTheBestSystem_AndFillsTheComparison()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid);

            try
            {
                window.Show();
                Settle(window);

                Type(window, "1.25");

                Assert.Equal(3, Rows(window));
                Assert.Equal(GlazingFixture.BetterGuid, (Control<DataGrid>(window, "dataGrid_Candidates").SelectedItem as GlazingCandidateRow)?.Guid);
                Assert.Equal("1.47", Text(window, "textBlock_Current"));
                Assert.Equal("1.20", Text(window, "textBlock_Proposed"));
                Assert.Equal("≤ 1.25", Text(window, "textBlock_TargetValue"));
                Assert.Equal("+0.05", Text(window, "textBlock_Margin"));
                Assert.Equal("✓ Meets target", Text(window, "textBlock_RowStatus"));
                Assert.StartsWith("Ug 1.40 → 1.10", Text(window, "textBlock_Change"));
                Assert.Equal("Adds GLZ 2 and 1 material to the model; GLZ stays unchanged.", Text(window, "textBlock_Result"));
                Assert.True(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.True(Control<Button>(window, "button_Apply").IsDefault);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void ClickingARow_ChoosesItsSystem_AndABlockedOneExplainsWhy()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid);

            try
            {
                window.Show();
                Settle(window);

                DataGrid grid = Control<DataGrid>(window, "dataGrid_Candidates");
                grid.SelectedItem = grid.Items.Cast<GlazingCandidateRow>().Single(x => x.Guid == GlazingFixture.MissingMaterialGuid);
                Flush();
                Assert.Equal(GlazingFixture.MissingMaterialGuid, window.ViewModel.SelectedGuid);
                Assert.False(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.Equal(Visibility.Visible, Control<TextBlock>(window, "textBlock_Block").Visibility);
                Assert.Contains("'Mystery'", Text(window, "textBlock_Block"));

                grid.SelectedItem = grid.Items.Cast<GlazingCandidateRow>().Single(x => x.Guid == GlazingFixture.BetterGuid);
                Flush();
                Assert.True(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.Equal(Visibility.Collapsed, Control<TextBlock>(window, "textBlock_Block").Visibility);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void TheTasFailing_IsShownInTheStatus_AndNothingCanBeApplied()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator() { Fail = true });

            try
            {
                window.Show();
                Settle(window);

                Assert.Equal("✕", Text(window, "textBlock_StatusGlyph"));
                Assert.Equal("Tas could not calculate the glazing values.", Text(window, "textBlock_Status"));
                Assert.False(Control<Button>(window, "button_Apply").IsEnabled);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Apply ---------------------------------------------------------------------------------------------

        [WpfFact]
        public void Apply_ChangesTheModelOnce_ShowsTheCheck_AndSavesTheReportBesideTheModel()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel) { Path = Path.Combine(directory, "model.sam") };
            int modified = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            SetGlazingWindow window = Window(uIAnalyticalModel, GlazingFixture.CurrentGuid);

            try
            {
                window.Show();
                Settle(window);
                Type(window, "1.25");

                Assert.True(window.Apply(), Text(window, "textBlock_Status"));
                Flush();

                Assert.Equal(1, modified);
                Assert.Equal(20, uIAnalyticalModel.JSAMObject.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == GlazingFixture.BetterGuid));
                Assert.Equal(Visibility.Visible, Control<Border>(window, "border_Applied").Visibility);
                Assert.Equal("Applied: GLZ 2 now glazes 20 apertures. One Undo reverts it.", Text(window, "textBlock_Applied"));
                Assert.Equal("Check: " + window.CheckSummary.Text, Text(window, "textBlock_Check"));
                Assert.True(File.Exists(window.ReportPath));
                Assert.Equal(directory, Path.GetDirectoryName(window.ReportPath));
                Assert.Equal(window.ReportText, File.ReadAllText(window.ReportPath));
                Assert.StartsWith("GLAZING CHANGE", window.ReportText);
                Assert.Equal("Report saved: " + window.ReportPath, Text(window, "textBlock_Report"));

                // Done: nothing more to apply; Copy All offered; Enter now closes.
                Assert.Equal(Visibility.Collapsed, Control<Button>(window, "button_Apply").Visibility);
                Assert.Equal(Visibility.Visible, Control<Button>(window, "button_CopyAll").Visibility);
                Assert.Equal("Close", Control<Button>(window, "button_Cancel").Content);
                Assert.True(Control<Button>(window, "button_Cancel").IsDefault);
                Assert.False(Control<TextBox>(window, "textBox_Target").IsEnabled);
                Assert.False(Control<Button>(window, "button_LoadMore").IsEnabled);
                Assert.False(window.Apply());
                Assert.Equal(1, modified);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void Apply_OnAnUnsavedModel_OffersOnlyCopyAll()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid);

            try
            {
                window.Show();
                Settle(window);
                Type(window, "1.25");
                Assert.True(window.Apply(), Text(window, "textBlock_Status"));

                Assert.Null(window.ReportPath);
                Assert.Contains("Copy All", Text(window, "textBlock_Report"));
                Assert.Equal(Visibility.Visible, Control<Button>(window, "button_CopyAll").Visibility);
                Assert.StartsWith("GLAZING CHANGE", window.ReportText);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void AFailedApply_SaysWhy_AndLeavesTheModelAndTheHistoryAlone()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            SetGlazingWindow window = Window(uIAnalyticalModel, GlazingFixture.CurrentGuid);
            window.ApplyFunction = x => new SetGlazingResult("Nothing could be changed.");

            try
            {
                window.Show();
                Settle(window);
                Type(window, "1.25");

                Assert.False(window.Apply());
                Assert.Equal("Nothing could be changed.", Text(window, "textBlock_Status"));
                Assert.Equal(Visibility.Collapsed, Control<Border>(window, "border_Applied").Visibility);
                Assert.False(uIAnalyticalModel.CanUndo);
                Assert.Null(window.Result);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void AdvancedOptions_ReachTheViewModel()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(2).ToList();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid, selected);

            try
            {
                window.Show();
                Settle(window);

                Control<RadioButton>(window, "radioButton_SelectedApertures").IsChecked = true;
                Flush();
                Assert.Equal(ThermalApplyScope.SelectedOnly, window.ViewModel.ApplyScope);
                Assert.Equal("Applies to 2 selected apertures of the 20 using GLZ.", Text(window, "textBlock_Scope"));

                Control<CheckBox>(window, "checkBox_DontAssign").IsChecked = true;
                Flush();
                Assert.Equal(ThermalApplyScope.DontAssign, window.ViewModel.ApplyScope);

                Control<CheckBox>(window, "checkBox_IncludeLibrary").IsChecked = false;
                Flush();
                Assert.False(window.ViewModel.IncludeLibrary);
                Assert.Equal(1, Rows(window));
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void TheSelectedOnlyChoice_IsDisabled_WhenNothingIsSelected()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid);

            try
            {
                window.Show();
                Settle(window);

                Assert.False(Control<RadioButton>(window, "radioButton_SelectedApertures").IsEnabled);
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Load more glazing ---------------------------------------------------------------------------------

        private string LoadedDatabase(ConstructionManager constructionManager)
        {
            string path = Path.Combine(directory, "more.tcd");
            File.WriteAllText(path, "not a real database; the cache answers for it");
            GlazingSourceCache.Write(path, constructionManager);
            return path;
        }

        private static ConstructionManager ConvertedSystems()
        {
            ConstructionManager constructionManager = new ConstructionManager();
            constructionManager.Add(GlazingFixture.ClearGlass());
            constructionManager.Add(GlazingFixture.LowEGlass());
            constructionManager.Add(GlazingFixture.ArgonGas());
            constructionManager.Add(new Construction(new Guid("d0000000-0000-4000-8000-000000000001"), "triple", new List<ConstructionLayer>() { new ConstructionLayer(GlazingFixture.LowE, 0.006), new ConstructionLayer(GlazingFixture.Argon, 0.012), new ConstructionLayer(GlazingFixture.Clear, 0.006) }));
            return constructionManager;
        }

        [WpfFact]
        public async System.Threading.Tasks.Task LoadMore_AddsTheFilesSystemsToTheWindowOnly_AndCancelLeavesTheModelAndTheHistoryUntouched()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            string before = uIAnalyticalModel.JSAMObject.ToJsonObject().ToJsonString();
            int modified = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            SetGlazingWindow window = Window(uIAnalyticalModel, GlazingFixture.CurrentGuid);
            string path = LoadedDatabase(ConvertedSystems());
            window.PickFile = () => path;

            try
            {
                window.Show();
                Settle(window);
                Assert.Equal(4, Rows(window));

                await window.LoadMoreAsync();
                Settle(window);

                Assert.Equal(5, Rows(window));
                Assert.Contains(Control<DataGrid>(window, "dataGrid_Candidates").Items.Cast<GlazingCandidateRow>(), x => x.Name == "triple" && x.SourceLabel == "more.tcd");
                Assert.StartsWith("Loaded 1 system from more.tcd.", Text(window, "textBlock_Load"));
                Assert.True(Control<Button>(window, "button_LoadMore").IsEnabled);

                // Cancel: the model and its Undo history are exactly as before.
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
        public async System.Threading.Tasks.Task LoadMore_OfAPaneLibrary_SaysPlainlyThatItHasNoSystems()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid);
            ConstructionManager panes = new ConstructionManager();
            panes.Add(GlazingFixture.ClearGlass());
            panes.Add(GlazingFixture.LowEGlass());
            string path = LoadedDatabase(panes);
            window.PickFile = () => path;

            try
            {
                window.Show();
                Settle(window);

                await window.LoadMoreAsync();
                Settle(window);

                Assert.Equal(4, Rows(window));
                Assert.Equal("more.tcd contains 2 panes and no glazing systems; it is a library of single panes, which do not define a Ug on their own.", Text(window, "textBlock_Load"));
                Assert.Single(Control<ItemsControl>(window, "itemsControl_Notes").Items);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public async System.Threading.Tasks.Task LoadMore_WhenTheFileDialogIsCancelled_ChangesNothing()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid);
            window.PickFile = () => null;

            try
            {
                window.Show();
                Settle(window);

                await window.LoadMoreAsync();

                Assert.Equal(4, Rows(window));
                Assert.Equal(string.Empty, Text(window, "textBlock_Load"));
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public async System.Threading.Tasks.Task ALoadedSystem_CanBeChosenAndApplied_WithItsMaterials()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            SetGlazingWindow window = Window(uIAnalyticalModel, GlazingFixture.CurrentGuid);
            string path = LoadedDatabase(ConvertedSystems());
            window.PickFile = () => path;

            try
            {
                window.Show();
                Settle(window);
                await window.LoadMoreAsync();
                Settle(window);

                DataGrid grid = Control<DataGrid>(window, "dataGrid_Candidates");
                grid.SelectedItem = grid.Items.Cast<GlazingCandidateRow>().Single(x => x.Name == "triple");
                Flush();
                Assert.True(Control<Button>(window, "button_Apply").IsEnabled);

                // The window's ApplyFunction asks Tas (stand-in) about the loaded system; the loaded source is passed on.
                Assert.True(window.Apply(), Text(window, "textBlock_Status"));
                Assert.Equal(20, uIAnalyticalModel.JSAMObject.AdjacencyCluster.GetApertures().Count(x => x.ApertureConstruction.Name == "triple"));
                Assert.NotNull(uIAnalyticalModel.JSAMObject.MaterialLibrary.GetMaterial(GlazingFixture.LowE));
            }
            finally
            {
                window.Close();
            }
        }

        // ---- Layout --------------------------------------------------------------------------------------------

        [WpfFact]
        public void AtItsSmallestSize_TheActionBarIsInsideTheWindow()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            SetGlazingWindow window = Window(new UIAnalyticalModel(analyticalModel), GlazingFixture.CurrentGuid, GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(1));
            window.Width = window.MinWidth;
            window.Height = window.MinHeight;

            try
            {
                window.Show();
                Settle(window);
                Type(window, "1.25");
                window.UpdateLayout();

                FrameworkElement content = (FrameworkElement)window.Content;
                Button button_Cancel = Control<Button>(window, "button_Cancel");
                Point point = button_Cancel.TranslatePoint(new Point(button_Cancel.ActualWidth, button_Cancel.ActualHeight), content);

                Assert.True(point.Y > 0, "the window was not laid out");
                Assert.True(point.Y <= content.ActualHeight + 0.5, string.Format("the Cancel button's bottom edge is at {0}, below the content height {1}", point.Y, content.ActualHeight));
                Assert.True(point.X <= content.ActualWidth + 0.5, string.Format("the Cancel button's right edge is at {0}, outside the content width {1}", point.X, content.ActualWidth));
            }
            finally
            {
                window.Close();
            }
        }
    }
}
