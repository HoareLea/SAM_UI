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
    /// "Set glazing": one progressive window over <see cref="GlazingViewModel"/>. Glazing is selection, not calculation:
    /// the table compares complete glazing systems (model, default library, files loaded for this window) by Ug, Uf, g,
    /// light transmittance and overall Uw. Opened from the 3D right-click on apertures, the Edit > Aperture
    /// Constructions toolbar and Tools > Glazing Calculator. One Apply is one Undo step (<c>Modify.SetGlazing</c>);
    /// afterwards the window shows the scoped check and saves the GLAZING CHANGE report next to the model, neither of
    /// which writes to the model. "Load more glazing..." only adds to the window's own pool: Cancel leaves the model and
    /// its Undo history untouched.
    /// </summary>
    public partial class SetGlazingWindow : System.Windows.Window
    {
        /// <summary>One aperture construction in the picker.</summary>
        public sealed class ApertureConstructionItem
        {
            public Guid Guid { get; set; }

            public string Name { get; set; }

            public int ApertureCount { get; set; }

            public ApertureType ApertureType { get; set; }

            public string Display => string.Format(CultureInfo.CurrentCulture, "{0}   ({1} {2}, {3})", Name, ApertureCount, ApertureCount == 1 ? "aperture" : "apertures", ApertureType.ToString().ToLowerInvariant());

            // The text a screen reader and type-ahead use for the item.
            public override string ToString() => Display;
        }

        private readonly UIAnalyticalModel uIAnalyticalModel;
        private readonly AnalyticalModel analyticalModel;
        private readonly List<Guid> selectedApertureGuids;
        private readonly IGlazingEvaluator evaluator;
        private readonly bool ownsEvaluator;
        private readonly bool openPicker;
        private readonly Func<GlazingSource> createLibrary;
        private readonly List<GlazingSource> loadedSources = new List<GlazingSource>();

        private GlazingViewModel viewModel;
        private bool updating;
        private bool loading;
        private string loadMessage;
        private SetGlazingResult result;
        private string reportText;

        public SetGlazingWindow()
        {
            InitializeComponent();
        }

        /// <param name="uIAnalyticalModel">The model the change is applied to.</param>
        /// <param name="apertureConstructionGuid">The aperture construction to start with; null opens the picker.</param>
        /// <param name="selectedApertureGuids">The selected apertures (from the 3D view), if any.</param>
        public SetGlazingWindow(UIAnalyticalModel uIAnalyticalModel, Guid? apertureConstructionGuid, IEnumerable<Guid> selectedApertureGuids)
            : this(uIAnalyticalModel, apertureConstructionGuid, selectedApertureGuids, null, null)
        {
        }

        /// <param name="evaluator">The glazing calculation; null for the real Tas evaluator (owned and disposed by the window).</param>
        /// <param name="createLibrary">The default library as a source; null for the real default library (tests pass their own).</param>
        internal SetGlazingWindow(UIAnalyticalModel uIAnalyticalModel, Guid? apertureConstructionGuid, IEnumerable<Guid> selectedApertureGuids, IGlazingEvaluator evaluator, Func<GlazingSource> createLibrary)
            : this()
        {
            this.uIAnalyticalModel = uIAnalyticalModel ?? throw new ArgumentNullException(nameof(uIAnalyticalModel));
            analyticalModel = uIAnalyticalModel.JSAMObject ?? throw new ArgumentException("There is no model.", nameof(uIAnalyticalModel));
            this.selectedApertureGuids = (selectedApertureGuids ?? Enumerable.Empty<Guid>()).Distinct().ToList();
            this.createLibrary = createLibrary ?? CreateDefaultLibrary;

            ownsEvaluator = evaluator == null;
            this.evaluator = evaluator ?? new TasGlazingEvaluator();

            ApplyFunction = x => this.uIAnalyticalModel.SetGlazing(x);
            PickFile = PickFileWithDialog;

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster ?? new AdjacencyCluster();
            List<Aperture> apertures = adjacencyCluster.GetApertures() ?? new List<Aperture>();

            List<ApertureConstructionItem> items = new List<ApertureConstructionItem>();
            HashSet<Guid> guids = new HashSet<Guid>();
            foreach (ApertureConstruction apertureConstruction in adjacencyCluster.GetApertureConstructions() ?? new List<ApertureConstruction>())
            {
                if (apertureConstruction == null || !guids.Add(apertureConstruction.Guid))
                {
                    continue;
                }

                items.Add(new ApertureConstructionItem() { Guid = apertureConstruction.Guid, Name = apertureConstruction.Name, ApertureType = apertureConstruction.ApertureType, ApertureCount = apertures.Count(x => x.TypeGuid == apertureConstruction.Guid) });
            }

            items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase));

            updating = true;
            comboBox_ApertureConstruction.ItemsSource = items;
            comboBox_ApertureConstruction.SelectedItem = apertureConstructionGuid == null ? null : items.Find(x => x.Guid == apertureConstructionGuid.Value);
            updating = false;

            openPicker = comboBox_ApertureConstruction.SelectedItem == null;

            Loaded += SetGlazingWindow_Loaded;
            Closed += SetGlazingWindow_Closed;

            Open(comboBox_ApertureConstruction.SelectedItem as ApertureConstructionItem);
        }

        /// <summary>What Apply does; the app applies to the model, tests substitute it.</summary>
        internal Func<SetGlazingRequest, SetGlazingResult> ApplyFunction { get; set; }

        /// <summary>Chooses the file "Load more glazing..." reads (null to cancel); the app shows a file dialog, tests substitute it.</summary>
        internal Func<string> PickFile { get; set; }

        /// <summary>The view-model for the chosen aperture construction; null until one is chosen.</summary>
        public GlazingViewModel ViewModel => viewModel;

        /// <summary>The applied change; null until Apply succeeded.</summary>
        public SetGlazingResult Result => result;

        /// <summary>The scoped check after Apply; null until then.</summary>
        public UValueCheckSummary CheckSummary { get; private set; }

        /// <summary>Where the report was saved; null when it was not (unsaved model or a write failure).</summary>
        public string ReportPath { get; private set; }

        /// <summary>The report text (what Copy All copies); null until Apply succeeded.</summary>
        public string ReportText => reportText;

        private static GlazingSource CreateDefaultLibrary()
        {
            try
            {
                return GlazingSource.FromDefaultLibrary();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ---- Aperture construction ------------------------------------------------------------------------

        private void Open(ApertureConstructionItem item)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.Dispose();
                viewModel = null;
            }

            if (item != null)
            {
                viewModel = new GlazingViewModel(analyticalModel, item.Guid, selectedApertureGuids, evaluator, createLibrary());
                viewModel.PropertyChanged += ViewModel_PropertyChanged;

                // The window-local pool survives a change of construction when the systems still fit its aperture type.
                foreach (GlazingSource source in loadedSources.Where(x => x.GetApertureConstructions(item.ApertureType).Count > 0))
                {
                    _ = viewModel.AddSourceAsync(source);
                }
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
                textBox_Target.Text = string.Empty;
                textBox_MinG.Text = string.Empty;
                textBox_MaxG.Text = string.Empty;
                textBox_MinLight.Text = string.Empty;
                radioButton_AllApertures.IsChecked = true;
                checkBox_DontAssign.IsChecked = false;
                checkBox_IncludeLibrary.IsChecked = true;
                checkBox_IncludeLoaded.IsChecked = true;
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
                if (e.PropertyName == nameof(GlazingViewModel.Rows))
                {
                    Dispatcher.BeginInvoke(new Action(Render));
                }

                return;
            }

            // A change raises every derived property in a row; render once, on the first of them (Rows).
            if (e.PropertyName == nameof(GlazingViewModel.Rows))
            {
                Render();
            }
        }

        private void Render()
        {
            bool applied = result != null && result.Succeeded;
            bool hasViewModel = viewModel != null;

            expander_Advanced.IsEnabled = hasViewModel && !applied;
            comboBox_ApertureConstruction.IsEnabled = !applied && !loading;
            textBox_Target.IsEnabled = hasViewModel && !applied;
            textBox_MinG.IsEnabled = textBox_Target.IsEnabled;
            textBox_MaxG.IsEnabled = textBox_Target.IsEnabled;
            textBox_MinLight.IsEnabled = textBox_Target.IsEnabled;
            button_LoadMore.IsEnabled = hasViewModel && !applied && !loading;
            dataGrid_Candidates.IsEnabled = hasViewModel && !applied;

            textBlock_Load.Text = loadMessage ?? string.Empty;

            if (!hasViewModel)
            {
                textBlock_Facts.Text = "Choose the glazing to replace (typing jumps to a name).";
                textBlock_SelectionNote.Visibility = Visibility.Collapsed;
                itemsControl_Notes.ItemsSource = null;
                SetStatus("–", "Choose an aperture construction.", Brushes_Muted());
                dataGrid_Candidates.ItemsSource = null;
                textBlock_Count.Text = string.Empty;
                SetSummary(null, null, double.NaN, double.NaN, string.Empty);
                textBlock_Change.Text = string.Empty;
                textBlock_Scope.Text = string.Empty;
                textBlock_Result.Text = string.Empty;
                itemsControl_Warnings.ItemsSource = null;
                textBlock_Block.Visibility = Visibility.Collapsed;
                textBlock_ScopeReason.Visibility = Visibility.Collapsed;
                radioButton_AllApertures.IsEnabled = false;
                radioButton_SelectedApertures.IsEnabled = false;
                button_Apply.IsEnabled = false;
                return;
            }

            GlazingViewModel vm = viewModel;
            GlazingCandidateRow current = vm.CurrentRow;

            textBlock_Facts.Text = string.Format(
                CultureInfo.CurrentCulture,
                "{0}  ·  used by {1} {2}{3}",
                current == null || current.Values == null
                    ? "Current values being calculated…"
                    : string.Format(CultureInfo.CurrentCulture, "Current Ug {0} · Uf {1} · g {2} · light {3} · Uw {4} W/m²K  ·  frame: {5}", current.UgText, current.UfText, current.GText, current.LightTransmittanceText, current.UwText, current.HasFrame ? "yes" : "none"),
                vm.AperturesUsingCount,
                vm.AperturesUsingCount == 1 ? "aperture" : "apertures",
                vm.SelectedAperturesCount > 0 ? string.Format(CultureInfo.CurrentCulture, " ({0} selected)", vm.SelectedAperturesCount) : string.Empty);

            textBlock_SelectionNote.Visibility = Visibility.Collapsed;
            itemsControl_Notes.ItemsSource = vm.Notes;

            switch (vm.Status)
            {
                case GlazingPreviewStatus.Calculating:
                    SetStatus("○", "Calculating the glazing values with Tas…", Brushes_Muted());
                    break;

                case GlazingPreviewStatus.Failed:
                    SetStatus("✕", vm.StatusMessage ?? "The glazing values could not be calculated.", (Brush)FindResource("PartO.Brush.Danger"));
                    break;

                default:
                    if (vm.ProposedRow == null)
                    {
                        SetStatus("–", "Choose a glazing system from the table, or type a target Uw.", Brushes_Muted());
                    }
                    else
                    {
                        SetStatus("✓", string.Format(CultureInfo.CurrentCulture, "Chosen: {0}.", vm.ProposedRow.Name), (Brush)FindResource("PartO.Brush.Success"));
                    }

                    break;
            }

            // The table: replace the rows, keep the chosen one selected.
            updating = true;
            try
            {
                dataGrid_Candidates.ItemsSource = vm.Rows;
                GlazingCandidateRow proposed = vm.ProposedRow;
                dataGrid_Candidates.SelectedItem = proposed;
                if (proposed != null)
                {
                    dataGrid_Candidates.ScrollIntoView(proposed);
                }
            }
            finally
            {
                updating = false;
            }

            textBlock_Count.Text = vm.CandidateCountText + (vm.LastEvaluationMilliseconds >= 0 ? string.Format(CultureInfo.CurrentCulture, "  Values calculated by Tas in {0} ms.", vm.LastEvaluationMilliseconds) : string.Empty);

            SetSummary(current, vm.ProposedRow, vm.Target, vm.Margin, vm.ComparisonStatus);
            textBlock_Change.Text = vm.ChangeText ?? string.Empty;

            SyncScope(vm, applied);

            textBlock_Scope.Text = vm.ScopeText;
            textBlock_Result.Text = vm.ResultText;
            itemsControl_Warnings.ItemsSource = vm.Warnings;

            string block = vm.ApplyBlockReason;
            textBlock_Block.Text = block ?? string.Empty;
            textBlock_Block.Visibility = block == null ? Visibility.Collapsed : Visibility.Visible;

            button_Apply.IsEnabled = !applied && vm.ApplyEnabled;
        }

        // The scope radios (main area) from the view-model: the labels carry the counts, and "only the selected" is disabled
        // with the reason beside it when no selected aperture uses the system (or "Don't assign" makes the scope moot).
        private void SyncScope(GlazingViewModel vm, bool applied)
        {
            bool dontAssign = checkBox_DontAssign.IsChecked == true;

            radioButton_AllApertures.Content = vm.Scope.AllLabel;
            radioButton_SelectedApertures.Content = vm.Scope.SelectedLabel;
            radioButton_AllApertures.IsEnabled = !applied && !dontAssign;

            string reason = vm.Scope.SelectedUnavailableReason;
            radioButton_SelectedApertures.IsEnabled = !applied && !dontAssign && reason == null;
            radioButton_SelectedApertures.ToolTip = reason;

            // Say why only when there is something to explain: apertures are selected but none uses the system.
            bool explain = reason != null && !dontAssign && selectedApertureGuids.Count > 0;
            textBlock_ScopeReason.Text = explain ? reason : string.Empty;
            textBlock_ScopeReason.Visibility = explain ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetSummary(GlazingCandidateRow current, GlazingCandidateRow proposed, double target, double margin, string status)
        {
            textBlock_Current.Text = current?.UwText ?? "–";
            textBlock_Proposed.Text = proposed?.UwText ?? "–";
            textBlock_TargetValue.Text = double.IsNaN(target) ? "–" : "≤ " + target.ToString("0.00", CultureInfo.CurrentCulture);
            textBlock_Margin.Text = double.IsNaN(margin) ? "–" : margin.ToString("+0.00;-0.00;0.00", CultureInfo.CurrentCulture);
            textBlock_RowStatus.Text = status;
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

        // ---- Input ----------------------------------------------------------------------------------------

        private void SetGlazingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (openPicker)
            {
                comboBox_ApertureConstruction.Focus();
                comboBox_ApertureConstruction.IsDropDownOpen = true;
                return;
            }

            textBox_Target.Focus();
            textBox_Target.SelectAll();
        }

        private void comboBox_ApertureConstruction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || !IsInitialized || uIAnalyticalModel == null)
            {
                return;
            }

            Open(comboBox_ApertureConstruction.SelectedItem as ApertureConstructionItem);
            if (viewModel != null)
            {
                textBox_Target.Focus();
            }
        }

        private void textBox_Filter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            if (sender == textBox_Target)
            {
                viewModel.TargetText = textBox_Target.Text;
            }
            else if (sender == textBox_MinG)
            {
                viewModel.MinGText = textBox_MinG.Text;
            }
            else if (sender == textBox_MaxG)
            {
                viewModel.MaxGText = textBox_MaxG.Text;
            }
            else if (sender == textBox_MinLight)
            {
                viewModel.MinLightText = textBox_MinLight.Text;
            }
        }

        private void dataGrid_Candidates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || viewModel == null || result != null)
            {
                return;
            }

            viewModel.SelectedGuid = (dataGrid_Candidates.SelectedItem as GlazingCandidateRow)?.Guid;
        }

        private void radioButton_Scope_Checked(object sender, RoutedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            viewModel.ApplyScope = checkBox_DontAssign.IsChecked == true ? ThermalApplyScope.DontAssign : radioButton_SelectedApertures.IsChecked == true ? ThermalApplyScope.SelectedOnly : ThermalApplyScope.AllUsing;
            Render();
        }

        private void checkBox_Include_Changed(object sender, RoutedEventArgs e)
        {
            if (updating || viewModel == null)
            {
                return;
            }

            viewModel.IncludeLibrary = checkBox_IncludeLibrary.IsChecked == true;
            viewModel.IncludeLoaded = checkBox_IncludeLoaded.IsChecked == true;
        }

        // ---- Load more glazing ----------------------------------------------------------------------------

        private void button_LoadMore_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadMoreAsync();
        }

        /// <summary>
        /// "Load more glazing...": asks for a file, reads it on a worker thread (a large .tcd takes about a minute the first
        /// time, a fraction of a second from the cache) and adds its systems to the window-local pool. Nothing is written to
        /// the model; Cancel afterwards leaves the model and its Undo history untouched.
        /// </summary>
        internal async System.Threading.Tasks.Task LoadMoreAsync()
        {
            if (viewModel == null || loading || result != null)
            {
                return;
            }

            string path = PickFile?.Invoke();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            loading = true;
            loadMessage = string.Format(CultureInfo.CurrentCulture, "Reading {0}…", System.IO.Path.GetFileName(path));
            Render();

            Cursor cursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Progress<string> progress = new Progress<string>(x =>
                {
                    loadMessage = x;
                    textBlock_Load.Text = x;
                });

                GlazingSource source = await Query.ReadGlazingSourceAsync(path, viewModel.ApertureType, progress);
                int count = source.GetApertureConstructions(viewModel.ApertureType).Count;

                loadedSources.Add(source);
                if (count > 0)
                {
                    await viewModel.AddSourceAsync(source);
                    loadMessage = string.Format(CultureInfo.CurrentCulture, "Loaded {0} {1} from {2}. They are used in this window only until you apply one.", count, count == 1 ? "system" : "systems", source.Label);
                }
                else
                {
                    // Nothing to add, but the note says why (e.g. a pane library has no glazing systems).
                    await viewModel.AddSourceAsync(source);
                    loadMessage = source.Note ?? string.Format(CultureInfo.CurrentCulture, "{0} contains no glazing systems.", source.Label);
                }
            }
            finally
            {
                Mouse.OverrideCursor = cursor;
                loading = false;
                Render();
            }
        }

        private string PickFileWithDialog()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load more glazing",
                Filter = "Tas Construction Databases (*.tcd)|*.tcd|json files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
            };

            string directory = @"C:\Users\Public\Documents\Tas Data\Databases";
            if (System.IO.Directory.Exists(directory))
            {
                openFileDialog.InitialDirectory = directory;
            }

            return openFileDialog.ShowDialog(this) == true ? openFileDialog.FileName : null;
        }

        // ---- Apply ----------------------------------------------------------------------------------------

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        /// <summary>Applies the chosen glazing (Apply / Enter). Does nothing unless it can be applied.</summary>
        internal bool Apply()
        {
            SetGlazingRequest request = viewModel?.CreateRequest();
            if (request == null || result != null || ApplyFunction == null)
            {
                return false;
            }

            SetGlazingResult result_Apply;
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
                SetStatus("✕", result_Apply?.Error ?? "The glazing change could not be applied.", (Brush)FindResource("PartO.Brush.Danger"));
                return false;
            }

            result = result_Apply;

            // The scoped check and the report read the model; neither writes to it.
            CheckSummary = Query.GlazingCheckSummary(uIAnalyticalModel.JSAMObject, result);
            reportText = Query.GlazingChangeReportText(result, CheckSummary, uIAnalyticalModel.Path);

            string reportLine;
            if (Modify.SaveGlazingChangeReport(uIAnalyticalModel.Path, result.AppliedAt, reportText, out string path_Report, out string refusal))
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
                "Applied: {0} {1}. One Undo reverts it.",
                result.ApertureConstruction.Name,
                result.Scope == ThermalApplyScope.DontAssign
                    ? "is in the model, not assigned to any aperture"
                    : string.Format(CultureInfo.CurrentCulture, "now glazes {0} {1}", result.ApertureCount, result.ApertureCount == 1 ? "aperture" : "apertures"));

            Brush brush = (Brush)FindResource(CheckSummary.Errors > 0 ? "PartO.Brush.Danger" : CheckSummary.Warnings > 0 ? "PartO.Brush.Warning" : "PartO.Brush.Success");
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

            LogWindow logWindow = new LogWindow(CheckSummary.Log) { Owner = this, Title = "Set glazing check" };
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

        private void SetGlazingWindow_Closed(object sender, EventArgs e)
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
