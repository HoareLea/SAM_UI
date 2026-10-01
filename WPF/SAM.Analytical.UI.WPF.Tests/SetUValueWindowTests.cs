// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
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
    /// U-value PR2b: the "Set U-value" window as a person meets it - rendered at its smallest size, typed into,
    /// applied - on the fixture model with the evaluator on <see cref="FakeTas"/> and Apply without the Tas
    /// thermal-parameter refresh. Real-app acceptance is in the PR record.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class SetUValueWindowTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_SetUValueWindowTests_" + Guid.NewGuid().ToString("N"));

        public SetUValueWindowTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private static SetUValueWindow Window(UIAnalyticalModel uIAnalyticalModel, Guid? constructionGuid, IEnumerable<Guid> selected = null)
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

        private static T Control<T>(System.Windows.Window window, string name) where T : FrameworkElement
        {
            return (T)window.FindName(name);
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        private static void Type(SetUValueWindow window, string text)
        {
            Control<TextBox>(window, "textBox_Target").Text = text;
            window.ViewModel.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            Flush();
        }

        [WpfFact]
        public void OpenedFromPanels_ShowsTheConstructionAndCurrentU_TargetPrefilled_ApplyDisabled()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            List<Guid> selected = UValueFixture.PanelGuids(analyticalModel, construction).Take(3).ToList();
            SetUValueWindow window = Window(uIAnalyticalModel, construction.Guid, selected);

            try
            {
                window.Show();
                Flush();

                Assert.Equal(construction.Guid, ((SetUValueWindow.ConstructionItem)Control<ComboBox>(window, "comboBox_Construction").SelectedItem).Guid);
                Assert.Equal("0.26", Control<TextBox>(window, "textBox_Target").Text);
                Assert.StartsWith("Current U 0.260 W/m²K  ·  used by 12 panels (3 selected)", Control<TextBlock>(window, "textBlock_ConstructionFacts").Text);
                Assert.Equal("Applies to 12 panels using SIM_EXT_SLD (3 selected).", Control<TextBlock>(window, "textBlock_Scope").Text);
                Assert.Equal("I01_Mineral Wool 80 mm will be adjusted; other layers stay fixed.", Control<TextBlock>(window, "textBlock_Layer").Text);
                Assert.False(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.False(Control<Expander>(window, "expander_Advanced").IsExpanded);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void OpenedFromTools_HasNoConstructionYet_AndNothingToApply()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction _);
            SetUValueWindow window = Window(new UIAnalyticalModel(analyticalModel), null);

            try
            {
                window.Show();
                Flush();

                Assert.Null(window.ViewModel);
                Assert.Null(Control<ComboBox>(window, "comboBox_Construction").SelectedItem);
                Assert.Single(Control<ComboBox>(window, "comboBox_Construction").Items);
                Assert.Equal("Choose a construction.", Control<TextBlock>(window, "textBlock_Status").Text);
                Assert.False(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.False(window.Apply());
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void ChoosingAConstruction_InThePicker_OpensIt()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            SetUValueWindow window = Window(new UIAnalyticalModel(analyticalModel), null);

            try
            {
                window.Show();
                Control<ComboBox>(window, "comboBox_Construction").SelectedIndex = 0;
                Flush();

                Assert.Equal(construction.Guid, window.ViewModel.ConstructionGuid);
                Assert.Equal("0.26", Control<TextBox>(window, "textBox_Target").Text);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void TypingAReachableTarget_PreviewsAndEnablesApply_AnUnreachableOneDoesNot()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            SetUValueWindow window = Window(new UIAnalyticalModel(analyticalModel), construction.Guid);

            try
            {
                window.Show();
                Flush();

                Type(window, "0.3");
                Assert.StartsWith("Reached: U 0.300 W/m²K with I01_Mineral Wool 67 mm (was 80 mm).", Control<TextBlock>(window, "textBlock_Status").Text);
                Assert.Equal("0.300", Control<TextBlock>(window, "textBlock_Actual").Text);
                Assert.True(Control<Button>(window, "button_Apply").IsEnabled);
                Assert.True(Control<Button>(window, "button_Apply").IsDefault);

                Type(window, "0.01");
                Assert.Contains("Best achievable: U 0.025 W/m²K at 1000 mm.", Control<TextBlock>(window, "textBlock_Status").Text);
                Assert.False(Control<Button>(window, "button_Apply").IsEnabled);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void Apply_ChangesTheModelOnce_ShowsTheCheck_AndSavesTheReportBesideTheModel()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel) { Path = Path.Combine(directory, "model.sam") };
            int modified = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            SetUValueWindow window = Window(uIAnalyticalModel, construction.Guid);

            try
            {
                window.Show();
                Flush();
                Type(window, "0.3");

                Assert.True(window.Apply());
                Flush();

                Assert.Equal(1, modified);
                Assert.Equal(12, uIAnalyticalModel.JSAMObject.AdjacencyCluster.GetPanels(window.Result.Construction).Count);
                Assert.Equal(Visibility.Visible, Control<Border>(window, "border_Applied").Visibility);
                Assert.Equal("Check: " + window.CheckSummary.Text, Control<TextBlock>(window, "textBlock_Check").Text);
                Assert.True(File.Exists(window.ReportPath));
                Assert.Equal(directory, Path.GetDirectoryName(window.ReportPath));
                Assert.Equal(window.ReportText, File.ReadAllText(window.ReportPath));
                Assert.Equal("Report saved: " + window.ReportPath, Control<TextBlock>(window, "textBlock_Report").Text);

                // Done: nothing more to apply; Copy All offered; Enter now closes.
                Assert.Equal(Visibility.Collapsed, Control<Button>(window, "button_Apply").Visibility);
                Assert.Equal(Visibility.Visible, Control<Button>(window, "button_CopyAll").Visibility);
                Assert.Equal("Close", Control<Button>(window, "button_Cancel").Content);
                Assert.True(Control<Button>(window, "button_Cancel").IsDefault);
                Assert.False(Control<TextBox>(window, "textBox_Target").IsEnabled);
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
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            SetUValueWindow window = Window(new UIAnalyticalModel(analyticalModel), construction.Guid);

            try
            {
                window.Show();
                Flush();
                Type(window, "0.3");
                Assert.True(window.Apply());

                Assert.Null(window.ReportPath);
                Assert.Contains("Copy All", Control<TextBlock>(window, "textBlock_Report").Text);
                Assert.Equal(Visibility.Visible, Control<Button>(window, "button_CopyAll").Visibility);
                Assert.StartsWith("U-VALUE CHANGE", window.ReportText);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void AdvancedOptions_ReachTheViewModel()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            List<Guid> selected = UValueFixture.PanelGuids(analyticalModel, construction).Take(2).ToList();
            SetUValueWindow window = Window(new UIAnalyticalModel(analyticalModel), construction.Guid, selected);

            try
            {
                window.Show();
                Flush();
                Type(window, "0.3");

                Control<RadioButton>(window, "radioButton_SelectedPanels").IsChecked = true;
                Flush();
                Assert.Equal(UValueApplyScope.SelectedPanels, window.ViewModel.ApplyScope);
                Assert.Equal("Applies to 2 selected panels of the 12 using SIM_EXT_SLD.", Control<TextBlock>(window, "textBlock_Scope").Text);

                Control<RadioButton>(window, "radioButton_ModifyInPlace").IsChecked = true;
                Flush();
                Assert.Equal(UValueApplyMode.ModifyInPlace, window.ViewModel.ApplyMode);
                Assert.False(Control<RadioButton>(window, "radioButton_DontAssign").IsEnabled);

                Control<TextBox>(window, "textBox_MaxThickness").Text = "100";
                window.ViewModel.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Flush();
                Assert.Equal(0.1, window.ViewModel.MaxThickness, 9);

                Control<ComboBox>(window, "comboBox_Layer").SelectedIndex = 2; // "2. Cement Particleboard"
                Assert.Equal(1, window.ViewModel.LayerIndexOverride);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void AtItsSmallestSize_TheActionBarIsInsideTheWindow()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction, walls: 12, roofs: 2);
            SetUValueWindow window = Window(new UIAnalyticalModel(analyticalModel), construction.Guid, UValueFixture.PanelGuids(analyticalModel, construction).Take(1));
            window.Width = window.MinWidth;
            window.Height = window.MinHeight;

            try
            {
                window.Show();
                Flush();
                Type(window, "0.01");
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
