// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// "Set U-value" (opaque constructions): one progressive window over <see cref="UValueViewModel"/>. Opened from
    /// the 3D right-click on panels, the Edit > Constructions toolbar, and Tools > U Value Calculator (picker
    /// pre-opened). One Apply is one Undo step (<c>Modify.SetUValue</c>); afterwards the window shows the scoped
    /// check and saves the U-VALUE CHANGE report next to the model, neither of which writes to the model.
    /// </summary>
    public partial class SetUValueWindow : System.Windows.Window
    {
        /// <summary>One construction in the picker.</summary>
        public sealed class ConstructionItem
        {
            public Guid Guid { get; set; }

            public string Name { get; set; }

            public int PanelCount { get; set; }

            public string Display => string.Format(CultureInfo.CurrentCulture, "{0}   ({1} {2})", Name, PanelCount, PanelCount == 1 ? "panel" : "panels");
        }

        /// <summary>One choice in an Advanced list; a null value means "automatic".</summary>
        public sealed class OptionItem
        {
            public object Value { get; set; }

            public string Display { get; set; }
        }

        private readonly UIAnalyticalModel uIAnalyticalModel;
        private readonly AnalyticalModel analyticalModel;
        private readonly List<Guid> selectedPanelGuids;
        private readonly IUValueEvaluator evaluator;
        private readonly bool ownsEvaluator;
        private readonly bool openPicker;

        private UValueViewModel viewModel;
        private bool updating;
        private bool targetSelected;
        private SetUValueResult result;
        private string reportText;

        public SetUValueWindow()
        {
            InitializeComponent();
        }

        /// <param name="uIAnalyticalModel">The model the change is applied to.</param>
        /// <param name="constructionGuid">The construction to start with; null opens the picker.</param>
        /// <param name="selectedPanelGuids">The selected panels (from the 3D view), if any.</param>
        public SetUValueWindow(UIAnalyticalModel uIAnalyticalModel, Guid? constructionGuid, IEnumerable<Guid> selectedPanelGuids)
            : this(uIAnalyticalModel, constructionGuid, selectedPanelGuids, null)
        {
        }

        /// <param name="evaluator">The U-value calculation; null for the real Tas evaluator (owned and disposed by the window).</param>
        internal SetUValueWindow(UIAnalyticalModel uIAnalyticalModel, Guid? constructionGuid, IEnumerable<Guid> selectedPanelGuids, IUValueEvaluator evaluator)
            : this()
        {
            this.uIAnalyticalModel = uIAnalyticalModel ?? throw new ArgumentNullException(nameof(uIAnalyticalModel));
            analyticalModel = uIAnalyticalModel.JSAMObject ?? throw new ArgumentException("There is no model.", nameof(uIAnalyticalModel));
            this.selectedPanelGuids = (selectedPanelGuids ?? Enumerable.Empty<Guid>()).Distinct().ToList();

            ownsEvaluator = evaluator == null;
            this.evaluator = evaluator ?? new TasUValueEvaluator();

            ApplyFunction = x => this.uIAnalyticalModel.SetUValue(x);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster ?? new AdjacencyCluster();
            List<ConstructionItem> items = new List<ConstructionItem>();
            HashSet<Guid> guids = new HashSet<Guid>();
            foreach (Construction construction in adjacencyCluster.GetConstructions() ?? new List<Construction>())
            {
                if (construction == null || !guids.Add(construction.Guid))
                {
                    continue;
                }

                items.Add(new ConstructionItem() { Guid = construction.Guid, Name = construction.Name, PanelCount = adjacencyCluster.GetPanels(construction)?.Count ?? 0 });
            }

            items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase));

            comboBox_HeatFlow.ItemsSource = new List<OptionItem>()
            {
                new OptionItem() { Value = null, Display = "Automatic" },
                new OptionItem() { Value = HeatFlowDirection.Horizontal, Display = "Horizontal (walls)" },
                new OptionItem() { Value = HeatFlowDirection.Up, Display = "Up (roofs)" },
                new OptionItem() { Value = HeatFlowDirection.Down, Display = "Down (floors)" },
            };

            updating = true;
            comboBox_Construction.ItemsSource = items;
            comboBox_Construction.SelectedItem = constructionGuid == null ? null : items.Find(x => x.Guid == constructionGuid.Value);
            updating = false;

            openPicker = comboBox_Construction.SelectedItem == null;

            Loaded += SetUValueWindow_Loaded;
            Closed += SetUValueWindow_Closed;

            Open(comboBox_Construction.SelectedItem as ConstructionItem);
        }

        /// <summary>What Apply does; the app applies to the model, tests substitute it.</summary>
        internal Func<SetUValueRequest, SetUValueResult> ApplyFunction { get; set; }

        /// <summary>The view-model for the chosen construction; null until one is chosen.</summary>
        public UValueViewModel ViewModel => viewModel;

        /// <summary>The applied change; null until Apply succeeded.</summary>
        public SetUValueResult Result => result;

        /// <summary>The scoped check after Apply; null until then.</summary>
        public UValueCheckSummary CheckSummary { get; private set; }

        /// <summary>Where the report was saved; null when it was not (unsaved model or a write failure).</summary>
        public string ReportPath { get; private set; }

        /// <summary>The report text (what Copy All copies); null until Apply succeeded.</summary>
        public string ReportText => reportText;

        // ---- Construction ---------------------------------------------------------------------------------

        private void Open(ConstructionItem constructionItem)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.Dispose();
                viewModel = null;
            }

            if (constructionItem != null)
            {
                viewModel = new UValueViewModel(analyticalModel, constructionItem.Guid, selectedPanelGuids, evaluator);
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                targetSelected = false;
            }

            ResetAdvanced();
            Render();

            _ = viewModel?.InitializeAsync();
        }

        private void ResetAdvanced()
        {
            updating = true;
            try
            {
                List<OptionItem> layers = new List<OptionItem>();
                if (viewModel != null)
                {
                    UValueLayerRow automatic = viewModel.Layers.FirstOrDefault(x => x.Index == viewModel.AutomaticLayerIndex);
                    layers.Add(new OptionItem() { Value = null, Display = automatic == null ? "Automatic (none adjustable)" : string.Format(CultureInfo.CurrentCulture, "Automatic ({0})", automatic.Name) });
                    layers.AddRange(viewModel.Layers.Select(x => new OptionItem() { Value = x.Index, Display = string.Format(CultureInfo.CurrentCulture, "{0}. {1}  {2:0.#} mm", x.Index + 1, x.Name, x.ThicknessBefore) }));
                }

                comboBox_Layer.ItemsSource = layers;
                comboBox_Layer.SelectedIndex = layers.Count == 0 ? -1 : 0;
                comboBox_HeatFlow.SelectedIndex = 0;
                textBox_MinThickness.Text = (UValueViewModel.DefaultMinThickness * 1000).ToString("0.#", CultureInfo.CurrentCulture);
                textBox_MaxThickness.Text = (UValueViewModel.DefaultMaxThickness * 1000).ToString("0.#", CultureInfo.CurrentCulture);
                radioButton_NewConstruction.IsChecked = true;
                radioButton_AllPanels.IsChecked = true;
                radioButton_SelectedPanels.IsEnabled = viewModel != null && viewModel.SelectedPanelsCount > 0;
            }
            finally
            {
                updating = false;
            }
        }

        // ---- Rendering ------------------------------------------------------------------------------------

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(Render));
                return;
            }

            Render();
        }

        private void Render()
        {
            bool applied = result != null && result.Succeeded;
            bool hasViewModel = viewModel != null;

            textBox_Target.IsEnabled = hasViewModel && !applied;
            expander_Advanced.IsEnabled = hasViewModel && !applied;
            comboBox_Construction.IsEnabled = !applied;

            if (!hasViewModel)
            {
                textBlock_ConstructionFacts.Text = "Choose the construction whose U-value should change (typing jumps to a name).";
                textBlock_SelectionNote.Visibility = Visibility.Collapsed;
                textBlock_Range.Text = string.Empty;
                textBlock_Layer.Text = string.Empty;
                SetStatus("–", "Choose a construction.", Brushes_Muted());
                SetTable(double.NaN, double.NaN, double.NaN, string.Empty);
                dataGrid_Layers.ItemsSource = null;
                textBlock_Timing.Text = string.Empty;
                textBlock_Scope.Text = string.Empty;
                textBlock_Result.Text = string.Empty;
                itemsControl_Warnings.ItemsSource = null;
                textBlock_Block.Visibility = Visibility.Collapsed;
                button_Apply.IsEnabled = false;
                return;
            }

            UValueViewModel vm = viewModel;

            textBlock_ConstructionFacts.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Current U {0} W/m²K  ·  used by {1} {2}{3}  ·  {4} heat flow{5}",
                U(vm.CurrentThermalTransmittance),
                vm.PanelsUsingCount,
                vm.PanelsUsingCount == 1 ? "panel" : "panels",
                vm.SelectedPanelsCount > 0 ? string.Format(CultureInfo.CurrentCulture, " ({0} selected)", vm.SelectedPanelsCount) : string.Empty,
                vm.HeatFlowDirection.ToString().ToLowerInvariant(),
                vm.HeatFlowBasis.FromPanels ? string.Format(CultureInfo.CurrentCulture, " ({0} panels)", vm.HeatFlowBasis.PanelType) : string.Empty);

            int otherSelected = selectedPanelGuids.Count - vm.SelectedPanelsCount;
            textBlock_SelectionNote.Text = string.Format(CultureInfo.CurrentCulture, "{0} selected {1} other constructions and {2} not affected.", otherSelected, otherSelected == 1 ? "panel uses" : "panels use", otherSelected == 1 ? "is" : "are");
            textBlock_SelectionNote.Visibility = otherSelected > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (!updating && textBox_Target.Text != vm.TargetText)
            {
                updating = true;
                textBox_Target.Text = vm.TargetText;
                updating = false;
                if (!targetSelected && textBox_Target.IsKeyboardFocusWithin)
                {
                    textBox_Target.SelectAll();
                }
            }

            textBlock_Range.Text = double.IsNaN(vm.MinThicknessThermalTransmittance) || double.IsNaN(vm.MaxThicknessThermalTransmittance)
                ? string.Empty
                : string.Format(CultureInfo.CurrentCulture, "reachable {0} – {1}", U(vm.MaxThicknessThermalTransmittance), U(vm.MinThicknessThermalTransmittance));

            textBlock_Layer.Text = vm.LayerSentence ?? string.Empty;

            switch (vm.Status)
            {
                case UValuePreviewStatus.Calculating:
                    SetStatus("○", "Calculating…", Brushes_Muted());
                    break;

                case UValuePreviewStatus.Reached:
                    UValueLayerRow adjusted = vm.PreviewRows.FirstOrDefault(x => x.Adjusted);
                    SetStatus("✓", string.Format(CultureInfo.CurrentCulture, "Reached: U {0} W/m²K with {1} {2:0.#} mm (was {3:0.#} mm).", U(vm.CalculatedThermalTransmittance), adjusted?.Name, adjusted?.ThicknessAfter, adjusted?.ThicknessBefore), (Brush)FindResource("PartO.Brush.Success"));
                    break;

                case UValuePreviewStatus.Unreachable:
                case UValuePreviewStatus.Failed:
                    SetStatus("✕", vm.StatusMessage, (Brush)FindResource("PartO.Brush.Danger"));
                    break;

                default:
                    SetStatus("–", vm.StatusMessage ?? (vm.IsBusy ? "Calculating the current U-value…" : "Type a target U-value."), Brushes_Muted());
                    break;
            }

            string rowStatus = vm.Status == UValuePreviewStatus.Reached ? "✓ Reached" : vm.Status == UValuePreviewStatus.Unreachable ? "✕ Not reachable" : vm.Status == UValuePreviewStatus.Calculating ? "○" : "–";
            SetTable(vm.CalculatedThermalTransmittance, vm.TargetThermalTransmittance, vm.Margin, rowStatus);

            dataGrid_Layers.ItemsSource = vm.PreviewRows;
            textBlock_Timing.Text = vm.LastEvaluationMilliseconds < 0
                ? string.Empty
                : vm.LastEvaluationMilliseconds == 0
                    ? "Answered from the reachable range already calculated (no new Tas calculation)."
                    : string.Format(CultureInfo.CurrentCulture, "Last calculation {0} ms (Tas TCD).", vm.LastEvaluationMilliseconds);

            textBlock_Scope.Text = vm.ScopeText;
            textBlock_Result.Text = vm.ResultText;
            itemsControl_Warnings.ItemsSource = vm.Warnings;

            string block = vm.Status == UValuePreviewStatus.Reached ? vm.ApplyBlockReason : null;
            textBlock_Block.Text = block ?? string.Empty;
            textBlock_Block.Visibility = block == null ? Visibility.Collapsed : Visibility.Visible;

            button_Apply.IsEnabled = !applied && vm.ApplyEnabled;
        }

        private Brush Brushes_Muted()
        {
            return (Brush)FindResource("PartO.Brush.Muted");
        }

        private void SetStatus(string glyph, string text, Brush brush)
        {
            textBlock_StatusGlyph.Text = glyph;
            textBlock_StatusGlyph.Foreground = brush;
            textBlock_Status.Text = text ?? string.Empty;
        }

        private void SetTable(double actual, double target, double margin, string status)
        {
            textBlock_Actual.Text = U(actual);
            textBlock_TargetValue.Text = U(target);
            textBlock_Margin.Text = double.IsNaN(margin) ? "–" : margin.ToString("+0.000;-0.000;0.000", CultureInfo.CurrentCulture);
            textBlock_RowStatus.Text = status;
        }

        private static string U(double value)
        {
            return double.IsNaN(value) ? "–" : value.ToString("0.000", CultureInfo.CurrentCulture);
        }

        // ---- Input ----------------------------------------------------------------------------------------

        private void SetUValueWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (openPicker)
            {
                comboBox_Construction.Focus();
                comboBox_Construction.IsDropDownOpen = true;
                return;
            }

            textBox_Target.Focus();
            textBox_Target.SelectAll();
        }

        private void comboBox_Construction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || !IsInitialized || uIAnalyticalModel == null)
            {
                return;
            }

            Open(comboBox_Construction.SelectedItem as ConstructionItem);
            if (viewModel != null)
            {
                textBox_Target.Focus();
            }
        }

        private void textBox_Target_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            targetSelected = true;
            viewModel.TargetText = textBox_Target.Text;
        }

        private void comboBox_Layer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            viewModel.LayerIndexOverride = (comboBox_Layer.SelectedItem as OptionItem)?.Value as int?;
        }

        private void comboBox_HeatFlow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            viewModel.HeatFlowDirectionOverride = (comboBox_HeatFlow.SelectedItem as OptionItem)?.Value as HeatFlowDirection?;
        }

        private void textBox_Thickness_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            if (sender == textBox_MinThickness && TryMillimetres(textBox_MinThickness.Text, out double min))
            {
                viewModel.MinThickness = min / 1000;
            }

            if (sender == textBox_MaxThickness && TryMillimetres(textBox_MaxThickness.Text, out double max))
            {
                viewModel.MaxThickness = max / 1000;
            }
        }

        private static bool TryMillimetres(string text, out double value)
        {
            return (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) && value > 0;
        }

        private void radioButton_Options_Checked(object sender, RoutedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            viewModel.ApplyMode = radioButton_ModifyInPlace.IsChecked == true ? UValueApplyMode.ModifyInPlace : UValueApplyMode.NewConstruction;
            viewModel.ApplyScope = radioButton_SelectedPanels.IsChecked == true ? UValueApplyScope.SelectedPanels : radioButton_DontAssign.IsChecked == true ? UValueApplyScope.DontAssign : UValueApplyScope.AllPanels;

            // Modifying in place always affects every panel using the construction.
            bool inPlace = viewModel.ApplyMode == UValueApplyMode.ModifyInPlace;
            radioButton_AllPanels.IsEnabled = !inPlace;
            radioButton_SelectedPanels.IsEnabled = !inPlace && viewModel.SelectedPanelsCount > 0;
            radioButton_DontAssign.IsEnabled = !inPlace;
            Render();
        }

        // ---- Apply ----------------------------------------------------------------------------------------

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        /// <summary>Applies the previewed change (Apply / Enter). Does nothing unless the preview can be applied.</summary>
        internal bool Apply()
        {
            SetUValueRequest request = viewModel?.CreateRequest();
            if (request == null || result != null || ApplyFunction == null)
            {
                return false;
            }

            SetUValueResult result_Apply;
            Cursor cursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                result_Apply = ApplyFunction(request);
            }
            finally
            {
                Mouse.OverrideCursor = cursor;
            }

            if (result_Apply == null || !result_Apply.Succeeded)
            {
                SetStatus("✕", result_Apply?.Error ?? "The U-value change could not be applied.", (Brush)FindResource("PartO.Brush.Danger"));
                return false;
            }

            result = result_Apply;

            // The scoped check and the report read the model; neither writes to it.
            CheckSummary = Query.UValueCheckSummary(uIAnalyticalModel.JSAMObject, result);
            reportText = Query.UValueChangeReportText(result, CheckSummary, uIAnalyticalModel.Path);

            string reportLine;
            if (Modify.SaveUValueChangeReport(uIAnalyticalModel.Path, result.AppliedAt, reportText, out string path_Report, out string refusal))
            {
                ReportPath = path_Report;
                reportLine = string.Format(CultureInfo.CurrentCulture, "Report saved: {0}", path_Report);
            }
            else
            {
                reportLine = refusal;
            }

            textBlock_Applied.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Applied: {0} now has U {1} W/m²K ({2}). One Undo reverts it.",
                result.Construction.Name,
                U(result.NewThermalTransmittance),
                result.Scope == UValueApplyScope.DontAssign
                    ? "not assigned to any panel"
                    : string.Format(CultureInfo.CurrentCulture, "{0} {1}", result.PanelCount, result.PanelCount == 1 ? "panel" : "panels"));

            Brush brush = (Brush)FindResource(CheckSummary.Errors > 0 ? "PartO.Brush.Danger" : CheckSummary.Warnings > 0 ? "PartO.Brush.Danger" : "PartO.Brush.Success");
            border_Applied.BorderBrush = brush;
            textBlock_CheckGlyph.Text = CheckSummary.Glyph;
            textBlock_CheckGlyph.Foreground = brush;
            textBlock_Check.Text = "Check: " + CheckSummary.Text;
            textBlock_Report.Text = reportLine;
            border_Applied.Visibility = Visibility.Visible;

            button_CopyAll.Visibility = Visibility.Visible;
            button_Apply.Visibility = Visibility.Collapsed;
            button_Cancel.Content = "Close";
            button_Cancel.IsDefault = true;

            Render();
            button_Cancel.Focus();
            return true;
        }

        private void button_Details_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSummary == null)
            {
                return;
            }

            LogWindow logWindow = new LogWindow(CheckSummary.Log) { Owner = this, Title = "Set U-value check" };
            logWindow.ShowDialog();
        }

        private void button_CopyAll_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(reportText))
            {
                return;
            }

            try
            {
                Clipboard.SetText(reportText);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // The clipboard is held by another process; the report is still on disk when the model is saved.
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = result != null && result.Succeeded;
            }
            catch (InvalidOperationException)
            {
                // Shown modeless (tests): there is no dialog result to set.
                Close();
            }
        }

        private void SetUValueWindow_Closed(object sender, EventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.Dispose();
                viewModel = null;
            }

            if (ownsEvaluator)
            {
                (evaluator as IDisposable)?.Dispose();
            }
        }
    }
}
