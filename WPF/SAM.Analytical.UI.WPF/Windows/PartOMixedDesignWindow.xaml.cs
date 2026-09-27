// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Weather;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What the mixed-design window was closed to do.</summary>
    public enum PartOMixedDesignAction
    {
        None,

        /// <summary>Screen the chosen strategies (optional evidence).</summary>
        Screen,

        /// <summary>Ask SAM to materialise the selected design, without simulating, and list its refusals.</summary>
        Check,

        /// <summary>Write the selected strategies onto the model and reopen.</summary>
        Save,

        /// <summary>Write the selected strategies onto the model and close.</summary>
        SaveAndClose,

        /// <summary>Save the selection if needed, materialise from the baseline, simulate, assess.</summary>
        BuildAndRun,

        /// <summary>Open the current final mixed run's space-level TM59 result - the existing result window, no TAS run.</summary>
        ReviewFinal,
    }

    /// <summary>
    /// The mixed Part O design: one row per dwelling, its screening evidence, its suggestion, its selected strategy
    /// and its final mixed-run result, with scalable bulk editing.
    ///
    /// <para><b>A view over <see cref="PartOMixedDesignSession"/>, and nothing more</b></para>
    /// <para>
    /// Every rule - what a row shows, what an edit is allowed to do, what is stale, whether the design can be built - is
    /// the session's, so it is tested without this window. The window binds, filters and forwards. Like the Part O
    /// Hub it is modal and closed to act: the command runs the chosen action with its own progress window and reopens
    /// it with the same session, so what was on screen stays on screen.
    /// </para>
    /// </summary>
    public partial class PartOMixedDesignWindow : System.Windows.Window
    {
        private PartOMixedDesignSession? session;
        private ListCollectionView? listCollectionView;
        private bool loaded;
        private bool writing;
        private bool simulationCase_Stated;

        public PartOMixedDesignWindow()
        {
            InitializeComponent();

            foreach (PartOMixedDwellingFilter partOMixedDwellingFilter in Enum.GetValues(typeof(PartOMixedDwellingFilter)))
            {
                comboBox_Filter.Items.Add(new KeyValuePair<PartOMixedDwellingFilter, string>(partOMixedDwellingFilter, FilterText(partOMixedDwellingFilter)));
            }

            comboBox_Filter.DisplayMemberPath = "Value";
            comboBox_Filter.SelectedIndex = 0;

            //The weather selector the Simulate dialog and the Hub use, configured the same way.
            selectSAMObjectComboBoxControl_Weather.ValidateFunc = new Func<IJSAMObject, bool>(x => x is WeatherData);
            selectSAMObjectComboBoxControl_Weather.ReadFunc = new Func<string, IJSAMObject>(x => UI.Query.TryGetWeatherData(x, out WeatherData weatherData_Read) ? weatherData_Read : null);
            selectSAMObjectComboBoxControl_Weather.DialogFilter = "epw files (*.epw)|*.epw|TAS TBD files (*.tbd)|*.tbd|TAS TSD files (*.tsd)|*.tsd|TAS TWD files (*.twd)|*.twd|All files (*.*)|*.*";
            selectSAMObjectComboBoxControl_Weather.DialogFilterIndex = 1;
            selectSAMObjectComboBoxControl_Weather.SelectionChanged += (s, e) => RefreshSimulationCase();

            foreach (SolarCalculationMethod solarCalculationMethod in Enum.GetValues(typeof(SolarCalculationMethod)))
            {
                if (solarCalculationMethod != SolarCalculationMethod.Undefined)
                {
                    comboBox_SolarCalculationMethod.Items.Add(Core.Query.Description(solarCalculationMethod));
                }
            }

            comboBox_SolarCalculationMethod.SelectionChanged += (s, e) => RefreshSimulationCase();
            textBox_OutputDirectory.TextChanged += (s, e) => RefreshSimulationCase();

            button_OutputDirectory.Click += (s, e) =>
            {
                using System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new()
                {
                    SelectedPath = textBox_OutputDirectory.Text,
                };

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textBox_OutputDirectory.Text = folderBrowserDialog.SelectedPath;
                }
            };

            textBox_Search.TextChanged += (s, e) => RefreshView();
            comboBox_Filter.SelectionChanged += (s, e) => RefreshView();
            checkBox_Group.Checked += (s, e) => RefreshGrouping();
            checkBox_Group.Unchecked += (s, e) => RefreshGrouping();
            button_SelectShown.Click += (s, e) => dataGrid_Dwellings.SelectAll();
            dataGrid_Dwellings.SelectionChanged += (s, e) => RefreshSelection();

            checkBox_NaturalAllowed.Checked += (s, e) => ConstraintsChanged();
            checkBox_NaturalAllowed.Unchecked += (s, e) => ConstraintsChanged();
            checkBox_OptimisationAllowed.Checked += (s, e) => ConstraintsChanged();
            checkBox_OptimisationAllowed.Unchecked += (s, e) => ConstraintsChanged();
            checkBox_CatalogueOffered.Checked += (s, e) => CatalogueChanged();
            checkBox_CatalogueOffered.Unchecked += (s, e) => CatalogueChanged();

            button_SetNatural.Click += (s, e) => Edit(x => session!.SetNatural(x));
            button_SetMvhr.Click += (s, e) => Edit(x => session!.SetMvhr(x, (comboBox_Product.SelectedItem as ProductItem)?.Reference));
            button_SetRetained.Click += (s, e) => Edit(x => session!.SetRetainedDesign(x));
            button_AcceptOptimised.Click += (s, e) => AcceptOptimised();
            button_Clear.Click += (s, e) => Edit(x => { session!.Clear(x); return null; });
            button_ApplySuggestions.Click += (s, e) => ApplySuggestions();

            button_Screen.Click += (s, e) => Screen();
            button_Check.Click += (s, e) => Close(PartOMixedDesignAction.Check);
            button_Save.Click += (s, e) => Close(PartOMixedDesignAction.Save);
            button_Build.Click += (s, e) => Close(PartOMixedDesignAction.BuildAndRun);
            button_ReviewFinal.Click += (s, e) => Close(PartOMixedDesignAction.ReviewFinal);

            Loaded += (s, e) =>
            {
                loaded = true;
                RefreshSimulationCase();
                RefreshAll();
            };

            Closing += Window_Closing;
        }

        /// <summary>The session this window shows. Set before showing; kept by the command across showings.</summary>
        public PartOMixedDesignSession? Session
        {
            get => session;
            set
            {
                session = value;

                listCollectionView = session is null ? null : new ListCollectionView((IList)session.Rows)
                {
                    Filter = x => x is PartOMixedDwellingRow row && PartOMixedDesignSession.Matches(row, Filter, textBox_Search.Text),
                };

                dataGrid_Dwellings.ItemsSource = listCollectionView;

                writing = true;
                try
                {
                    checkBox_NaturalAllowed.IsChecked = session?.Constraints.NaturalVentilationAllowed ?? true;
                    checkBox_OptimisationAllowed.IsChecked = session?.Constraints.OptimisationAllowed ?? true;
                    checkBox_CatalogueOffered.IsChecked = session?.CatalogueOffered ?? false;
                    checkBox_CatalogueOffered.IsEnabled = session?.CatalogueHasProducts ?? false;
                }
                finally
                {
                    writing = false;
                }

                RefreshProducts();
                RefreshGrouping();

                if (loaded)
                {
                    RefreshAll();
                }
            }
        }

        /// <summary>What the window was closed to do.</summary>
        public PartOMixedDesignAction Action { get; private set; } = PartOMixedDesignAction.None;

        /// <summary>The strategies chosen in the screening configuration, where <see cref="Action"/> is Screen.</summary>
        public List<PartOScreeningStrategy> ScreeningStrategies { get; } = [];

        public PartOScreeningMode ScreeningMode { get; private set; } = PartOScreeningMode.Minimum;

        /// <summary>The line saying what the last action did. Shown above the summary.</summary>
        public string? LastOutcome
        {
            get => textBlock_Outcome.Text;
            set
            {
                textBlock_Outcome.Text = value ?? string.Empty;
                textBlock_Outcome.Visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public PartOMixedDwellingFilter Filter
        {
            get => comboBox_Filter.SelectedItem is KeyValuePair<PartOMixedDwellingFilter, string> keyValuePair ? keyValuePair.Key : PartOMixedDwellingFilter.All;
            set
            {
                foreach (object item in comboBox_Filter.Items)
                {
                    if (item is KeyValuePair<PartOMixedDwellingFilter, string> keyValuePair && keyValuePair.Key == value)
                    {
                        comboBox_Filter.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        public string SearchText
        {
            get => textBox_Search.Text ?? string.Empty;
            set => textBox_Search.Text = value ?? string.Empty;
        }

        public bool Grouped
        {
            get => checkBox_Group.IsChecked == true;
            set => checkBox_Group.IsChecked = value;
        }

        /// <summary>The rows the matrix currently shows, after search and filter.</summary>
        internal List<PartOMixedDwellingRow> ShownRows => listCollectionView?.Cast<PartOMixedDwellingRow>().ToList() ?? [];

        internal List<PartOMixedDwellingRow> SelectedRows => dataGrid_Dwellings.SelectedItems.OfType<PartOMixedDwellingRow>().ToList();

        internal System.Windows.Controls.DataGrid Grid_Dwellings => dataGrid_Dwellings;

        // ---- Simulation case ------------------------------------------------------------------------------------

        public PartOSimulationCase SimulationCase
        {
            get
            {
                return new PartOSimulationCase
                {
                    WeatherData = selectSAMObjectComboBoxControl_Weather.GetJSAMObject<WeatherData>(),
                    OutputDirectory = textBox_OutputDirectory.Text?.Trim(),
                    SolarCalculationMethod = comboBox_SolarCalculationMethod.SelectedItem is string text ? Core.Query.Enum<SolarCalculationMethod>(text) : SolarCalculationMethod.Undefined,
                };
            }

            set
            {
                simulationCase_Stated = value is not null;

                PartOSimulationCase partOSimulationCase = value ?? new PartOSimulationCase();

                writing = true;
                try
                {
                    if (partOSimulationCase.WeatherData is not null)
                    {
                        string text = string.IsNullOrWhiteSpace(partOSimulationCase.WeatherData.Name) ? SimulateControl.InternalText : partOSimulationCase.WeatherData.Name;

                        selectSAMObjectComboBoxControl_Weather.Add(text, new WeatherData(partOSimulationCase.WeatherData));
                        selectSAMObjectComboBoxControl_Weather.SelectedText = text;
                    }

                    textBox_OutputDirectory.Text = partOSimulationCase.OutputDirectory ?? string.Empty;
                    comboBox_SolarCalculationMethod.SelectedItem = Core.Query.Description(partOSimulationCase.SolarCalculationMethod == SolarCalculationMethod.Undefined ? SolarCalculationMethod.TAS : partOSimulationCase.SolarCalculationMethod);
                }
                finally
                {
                    writing = false;
                }

                RefreshSimulationCase();
            }
        }

        private string? SimulationCaseRefusal => simulationCase_Stated ? SimulationCase.Refusal() : null;

        private void RefreshSimulationCase()
        {
            if (!loaded || writing)
            {
                return;
            }

            PartOSimulationCase partOSimulationCase = SimulationCase;
            string? refusal = SimulationCaseRefusal;

            string weather = string.IsNullOrWhiteSpace(partOSimulationCase.WeatherData?.Name) ? "model weather" : partOSimulationCase.WeatherData!.Name;

            run_SimulationCaseSummary.Text = !simulationCase_Stated
                ? string.Empty
                : refusal is not null
                    ? string.Format(" — {0}", refusal)
                    : string.Format(" — {0} · {1} solar", weather, Core.Query.Description(partOSimulationCase.SolarCalculationMethod));

            run_SimulationCaseSummary.Foreground = refusal is not null ? Brushes.Firebrick : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));

            if (refusal is not null)
            {
                expander_SimulationCase.IsExpanded = true;
            }

            RefreshActions();
        }

        // ---- Refresh ----------------------------------------------------------------------------------------------

        private void RefreshAll()
        {
            if (!loaded || session is null)
            {
                return;
            }

            RefreshView();
            RefreshSummary();
            RefreshSelection();
        }

        private void RefreshView()
        {
            if (listCollectionView is null)
            {
                return;
            }

            listCollectionView.Refresh();

            textBlock_Shown.Text = string.Format("{0} of {1} shown", listCollectionView.Count, session?.Rows.Count ?? 0);
        }

        private void RefreshGrouping()
        {
            if (listCollectionView is null)
            {
                return;
            }

            using (listCollectionView.DeferRefresh())
            {
                listCollectionView.GroupDescriptions.Clear();
                if (checkBox_Group.IsChecked == true)
                {
                    listCollectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PartOMixedDwellingRow.Group)));
                }
            }
        }

        private void RefreshProducts()
        {
            comboBox_Product.Items.Clear();
            comboBox_Product.Items.Add(new ProductItem(null, "Automatic from the project's pool"));

            if (session is not null && session.CatalogueOffered)
            {
                foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in session.AllowedProducts)
                {
                    comboBox_Product.Items.Add(new ProductItem(ventilationUnitCapacityDescriptor.VentilationUnitReference, Query.PartOProductLabel(ventilationUnitCapacityDescriptor)));
                }
            }

            comboBox_Product.SelectedIndex = 0;
        }

        private void RefreshSummary()
        {
            if (session is null)
            {
                return;
            }

            // ---- Baseline -----------------------------------------------------------------------------------------

            if (session.IsCleanBaseline)
            {
                textBlock_Baseline.Text = string.Format("Baseline: clean — {0}; {1} not strategy rows (SAM includes assessed communal corridors automatically).{2}",
                    UI.Query.PartOCount(session.Rows.Count, "dwelling", "dwellings"),
                    UI.Query.PartOCount(session.CommonZoneCount, "other zone is", "other zones are"),
                    string.IsNullOrWhiteSpace(session.Path_Model) ? " The model has not been saved, so screening and results are kept for this session only." : string.Empty);
                textBlock_Baseline.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x60, 0x6A));
            }
            else
            {
                textBlock_Baseline.Text = string.Format("The open model is NOT a clean Part O baseline, so no mixed design can be built from it. Reopen the pre-Part-O model; nothing is cleaned or undone. SAM says: {0}",
                    string.Join(" ", session.BaselineFindings.Take(4).Select(x => string.Format("[{0}] {1}", Core.Query.Description(x.Reason), x.Message))) + (session.BaselineFindings.Count > 4 ? string.Format(" …and {0} more.", session.BaselineFindings.Count - 4) : string.Empty));
                textBlock_Baseline.Foreground = Brushes.Firebrick;
            }

            // ---- Readiness, final, screening ----------------------------------------------------------------------

            PartOMixedReadiness partOMixedReadiness = session.Readiness();
            textBlock_Readiness.Text = partOMixedReadiness.Text + (session.IsDirty ? " · unsaved changes" : string.Empty);

            textBlock_Final.Text = session.FinalText ?? "No mixed run yet.";
            textBlock_Final.FontStyle = session.FinalCurrent || session.State.FinalRun is null ? FontStyles.Normal : FontStyles.Italic;

            List<string> screening = [];
            foreach (PartOScreeningStrategy partOScreeningStrategy in UI.Query.PartOScreeningStrategies())
            {
                PartOScreeningEvidence? partOScreeningEvidence = session.State.ScreeningEvidence(partOScreeningStrategy);
                if (partOScreeningEvidence is null)
                {
                    continue;
                }

                string label = UI.Query.PartOScreeningStrategyLabel(partOScreeningStrategy);
                string? stale = session.ScreeningStale(partOScreeningStrategy);

                screening.Add(stale is null
                    ? string.Format("{0}: {1} pass of {2} screened", label, partOScreeningEvidence.Results.Count(x => x.Outcome == PartODwellingOutcome.Pass), partOScreeningEvidence.Count)
                    : string.Format("{0}: STALE", label));
            }

            textBlock_Screening.Text = screening.Count == 0 ? "Not screened. Screening is optional." : string.Join(" · ", screening);
            textBlock_Screening.ToolTip = string.Join("\n", UI.Query.PartOScreeningStrategies().Select(x => session.ScreeningStale(x)).Where(x => x is not null));

            // ---- Constraints and products -------------------------------------------------------------------------

            run_ConstraintsSummary.Text = string.Format(" — {0}{1} · {2}",
                session.Constraints.NaturalVentilationAllowed ? "natural allowed" : "mechanical required",
                session.Constraints.OptimisationAllowed ? string.Empty : " · no optimised airflow",
                session.CatalogueOffered ? "products from the catalogue" : "generic MVHR units");

            textBlock_ProductPool.Text = session.CatalogueOffered
                ? session.ProductPoolText
                : session.CatalogueHasProducts ? "Not offered: MVHR units stay generic (Approved Document F duty only), as Iteration 1a." : session.ProductPoolText;

            // ---- Refusals that name no single dwelling ------------------------------------------------------------

            List<PartOMaterialisationRefusal> refusals_Unplaced = [.. session.Refusals.Where(x => x.ZoneGuid is not Guid guid || session.Row(guid) is null)];
            textBlock_Refusals.Text = refusals_Unplaced.Count == 0
                ? string.Empty
                : string.Format("SAM refused the design: {0}", string.Join(" ", refusals_Unplaced.Take(6).Select(x => string.Format("[{0}{1}] {2}", Core.Query.Description(x.Reason), string.IsNullOrWhiteSpace(x.Subject) ? string.Empty : " · " + x.Subject, x.Message))) + (refusals_Unplaced.Count > 6 ? string.Format(" …and {0} more.", refusals_Unplaced.Count - 6) : string.Empty));
            textBlock_Refusals.Visibility = refusals_Unplaced.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            textBlock_Next.Text = Next(partOMixedReadiness);

            RefreshActions();
        }

        private string Next(PartOMixedReadiness partOMixedReadiness)
        {
            if (session is null)
            {
                return string.Empty;
            }

            if (partOMixedReadiness.Blockers.Count != 0)
            {
                return string.Format("Blocked: {0}", string.Join(" ", partOMixedReadiness.Blockers));
            }

            if (partOMixedReadiness.NotSelected != 0)
            {
                return string.Format("Next: select a strategy for {0} — show 'No strategy selected', select them, and assign in bulk. Screening is optional.", UI.Query.PartOCount(partOMixedReadiness.NotSelected, "dwelling", "dwellings"));
            }

            if (partOMixedReadiness.NeedsAttention != 0)
            {
                return string.Format("Next: resolve {0} — show 'Needs attention'.", UI.Query.PartOCount(partOMixedReadiness.NeedsAttention, "dwelling", "dwellings"));
            }

            if (session.FinalCurrent && session.State.FinalRun!.Count(PartODwellingOutcome.Fail) != 0)
            {
                return string.Format("Next: {0} failed the mixed run — show 'Failing in the final run', change their strategy, and build again. The other dwellings keep their selection; the whole design is rebuilt from the baseline.", UI.Query.PartOCount(session.State.FinalRun.Count(PartODwellingOutcome.Fail), "dwelling", "dwellings"));
            }

            if (session.FinalCurrent)
            {
                return "The final mixed run is current for this design.";
            }

            return "Next: Build & Run the mixed design. The selection is saved onto the model first, and the model is materialised as a copy - the open model stays the clean baseline.";
        }

        private void RefreshSelection()
        {
            if (session is null)
            {
                return;
            }

            int count = dataGrid_Dwellings.SelectedItems.Count;
            textBlock_SelectionCount.Text = count == 0 ? "No dwelling selected" : string.Format("{0} selected:", UI.Query.PartOCount(count, "dwelling", "dwellings"));

            bool any = count != 0;
            button_SetNatural.IsEnabled = any && session.Constraints.NaturalVentilationAllowed;
            button_SetNatural.ToolTip = session.Constraints.NaturalVentilationAllowed ? "Natural ventilation for the selected dwellings." : "The project requires mechanical ventilation.";
            button_SetMvhr.IsEnabled = any;
            comboBox_Product.IsEnabled = any;
            button_SetRetained.IsEnabled = any && session.Constraints.OptimisationAllowed;
            button_AcceptOptimised.IsEnabled = count == 1 && session.Constraints.OptimisationAllowed && session.IsCleanBaseline;
            button_AcceptOptimised.ToolTip = !session.Constraints.OptimisationAllowed
                ? "The project does not allow an optimised design airflow."
                : count != 1 ? "Select ONE dwelling whose completed Iteration 2B result you want to accept." : "Choose the completed Iteration 2B result model for this dwelling, review the design airflows it changes, and confirm.";
            button_Clear.IsEnabled = any;
            button_ApplySuggestions.IsEnabled = session.Rows.Any(x => x.SuggestionDiffers);
        }

        private void RefreshActions()
        {
            if (session is null)
            {
                return;
            }

            bool clean = session.IsCleanBaseline && session.Rows.Count != 0;
            string? refusal_Case = SimulationCaseRefusal;

            button_Screen.IsEnabled = clean && refusal_Case is null;
            button_Screen.ToolTip = !clean
                ? "The open model is not a clean Part O baseline, so it cannot be screened."
                : refusal_Case ?? "Runs selected Part O strategies to determine which dwellings pass. Screening does not change your selected final design until you apply or edit the suggestions.";

            button_Check.IsEnabled = clean;
            button_Save.IsEnabled = session.IsDirty;

            string? path_RunModel = session.State.FinalRun?.Path_RunModel;
            button_ReviewFinal.IsEnabled = session.FinalCurrent && !string.IsNullOrWhiteSpace(path_RunModel) && System.IO.File.Exists(path_RunModel);
            button_ReviewFinal.ToolTip = button_ReviewFinal.IsEnabled
                ? "Opens the space-level TM59 result of the final mixed run from its saved run model and results. Nothing is simulated."
                : session.FinalCurrent ? "The final mixed run's saved run model is not there, so its space-level result cannot be opened." : "There is no current final mixed run to open.";

            PartOMixedReadiness partOMixedReadiness = session.Readiness();
            button_Build.IsEnabled = partOMixedReadiness.CanBuild && refusal_Case is null;
            button_Build.ToolTip = button_Build.IsEnabled
                ? "Saves the selection onto the model if it changed, materialises ONE mixed model from the clean baseline as a copy, runs the full-year TAS simulation and assesses TM59 per dwelling."
                : refusal_Case ?? Next(partOMixedReadiness);
        }

        // ---- Editing ----------------------------------------------------------------------------------------------

        private void Edit(Func<List<PartOMixedDwellingRow>, string?> func)
        {
            if (session is null)
            {
                return;
            }

            string? message = func(SelectedRows);

            textBlock_BulkMessage.Text = message ?? string.Empty;
            textBlock_BulkMessage.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;

            RefreshView();
            RefreshSummary();
            RefreshSelection();
        }

        /// <summary>
        /// Accept optimised airflow: the person picks a completed Iteration 2B result model, SAM answers what accepting it
        /// for the selected dwelling would write (or why it refuses), the person confirms every change - Cancel is the
        /// default - and only then is it adopted. SAM owns every rule; this only asks and shows.
        /// </summary>
        private void AcceptOptimised()
        {
            if (session is null)
            {
                return;
            }

            List<PartOMixedDwellingRow> rows = SelectedRows;
            if (rows.Count != 1)
            {
                return;
            }

            PartOMixedDwellingRow row = rows[0];
            const string caption = "Part O — Accept optimised airflow";

            Microsoft.Win32.OpenFileDialog openFileDialog = AcceptOptimisedFileDialog(row.Name, textBox_OutputDirectory.Text, session.Path_Model);

            if (openFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            if (IsOpenModel(openFileDialog.FileName, session.Path_Model))
            {
                MessageBox.Show(this, string.Format("'{0}' is the open baseline model itself, not a completed Iteration 2B result. Choose the 2B result model (its '-Opt' round file). Nothing was accepted.", System.IO.Path.GetFileName(openFileDialog.FileName)), caption, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AnalyticalModel? analyticalModel_Source = null;
            try
            {
                analyticalModel_Source = Core.Convert.ToSAM<AnalyticalModel>(openFileDialog.FileName)?.FirstOrDefault(x => x is not null);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError(exception.ToString());
            }

            if (analyticalModel_Source is null)
            {
                MessageBox.Show(this, string.Format("'{0}' could not be read as a SAM analytical model. Nothing was accepted.", openFileDialog.FileName), caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PartODwellingDesignAcceptance partODwellingDesignAcceptance = session.PreviewAcceptDesign(row, analyticalModel_Source);
            if (!partODwellingDesignAcceptance.IsAccepted)
            {
                MessageBox.Show(this, string.Format("SAM cannot accept the design airflow for {0} from '{1}':\n\n• {2}\n\nNothing was accepted.", row.Name, System.IO.Path.GetFileName(openFileDialog.FileName), string.Join("\n• ", partODwellingDesignAcceptance.Refusals)), caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (partODwellingDesignAcceptance.Changes.Count == 0)
            {
                MessageBox.Show(this, string.Format("'{0}' states the same design airflow for {1} as the baseline already gives it, so there is no optimised airflow to accept. Nothing was accepted.", System.IO.Path.GetFileName(openFileDialog.FileName), row.Name), caption, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IEnumerable<string> lines = partODwellingDesignAcceptance.Changes.Select(x => string.Format("   {0} {1}: {2:0.##} → {3:0.##} l/s", x.SpaceName, Core.Query.Description(x.FlowClassification).ToLowerInvariant(), x.Before_Lps, x.After_Lps));

            string text = string.Format(
                "Accept the optimised design airflow for {0} from\n{1}?\n\nDesign airflow, current → accepted:\n{2}\n\nOnly {0}'s design terminals on the baseline change. No other dwelling is touched and nothing is simulated. {0} is then selected as Optimised MVHR (retained design); the whole mixed model is rebuilt from the baseline at the next Build & Run. Save selection keeps it on the model.",
                row.Name, openFileDialog.FileName, string.Join("\n", lines));

            if (MessageBox.Show(this, text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            string? refusal = session.AcceptDesign(row, partODwellingDesignAcceptance);
            string message = refusal ?? string.Format("Accepted the optimised airflow for {0} from {1} ({2}). Save selection to keep it on the model.", row.Name, System.IO.Path.GetFileName(openFileDialog.FileName), UI.Query.PartOCount(partODwellingDesignAcceptance.Changes.Count, "design airflow changed", "design airflows changed"));

            textBlock_BulkMessage.Text = message;
            textBlock_BulkMessage.Visibility = Visibility.Visible;

            RefreshAll();
        }

        /// <summary>
        /// The file dialog of Accept optimised airflow: it starts in the simulation case's output folder - where Part O runs,
        /// Iteration 2B's rounds included, write their result models - falling back to the model's folder, and its file name
        /// is empty, so the open baseline is never offered as the answer. No new persisted state.
        /// </summary>
        internal static Microsoft.Win32.OpenFileDialog AcceptOptimisedFileDialog(string name_Dwelling, string? directory_Output, string? path_Model)
        {
            string? directory = !string.IsNullOrWhiteSpace(directory_Output) && System.IO.Directory.Exists(directory_Output)
                ? directory_Output
                : string.IsNullOrWhiteSpace(path_Model) ? null : System.IO.Path.GetDirectoryName(path_Model);

            return new Microsoft.Win32.OpenFileDialog()
            {
                Title = string.Format("Accept optimised airflow for {0} — choose the completed Iteration 2B result model", name_Dwelling),
                Filter = "SAM model (*.sam)|*.sam",
                InitialDirectory = directory is not null && System.IO.Directory.Exists(directory) ? directory : string.Empty,
                FileName = string.Empty,
            };
        }

        /// <summary>Whether <paramref name="path"/> is the open model's own file.</summary>
        internal static bool IsOpenModel(string? path, string? path_Model)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(path_Model))
            {
                return false;
            }

            try
            {
                return string.Equals(System.IO.Path.GetFullPath(path), System.IO.Path.GetFullPath(path_Model), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ApplySuggestions()
        {
            if (session is null)
            {
                return;
            }

            //The selected rows, or every shown row where none is selected - never rows the person cannot see.
            List<PartOMixedDwellingRow> rows = SelectedRows.Count != 0 ? SelectedRows : ShownRows;

            List<PartOMixedSelectionChange> changes = session.SuggestionChanges(rows);
            if (changes.Count == 0)
            {
                MessageBox.Show(this, "No applicable suggestion differs from the selection for these dwellings. Nothing was changed.", "Part O — Apply suggestions");
                return;
            }

            PartOMixedChangesWindow partOMixedChangesWindow = new(changes) { Owner = this };
            if (partOMixedChangesWindow.ShowDialog() == true)
            {
                session.Apply(changes);

                RefreshView();
                RefreshSummary();
                RefreshSelection();
            }
        }

        private void ConstraintsChanged()
        {
            if (writing || session is null)
            {
                return;
            }

            session.Constraints.NaturalVentilationAllowed = checkBox_NaturalAllowed.IsChecked == true;
            session.Constraints.OptimisationAllowed = checkBox_OptimisationAllowed.IsChecked == true;
            session.Refresh();

            RefreshAll();
        }

        private void CatalogueChanged()
        {
            if (writing || session is null)
            {
                return;
            }

            session.CatalogueOffered = checkBox_CatalogueOffered.IsChecked == true;

            RefreshProducts();
            RefreshAll();
        }

        // ---- Closing ----------------------------------------------------------------------------------------------

        private void Screen()
        {
            if (session is null)
            {
                return;
            }

            PartOScreeningWindow partOScreeningWindow = new(session.CatalogueHasProducts, session.State.Strategies_Screening, session.State.ScreeningMode, session.Constraints) { Owner = this };
            if (partOScreeningWindow.ShowDialog() != true)
            {
                return;
            }

            ScreeningStrategies.Clear();
            ScreeningStrategies.AddRange(partOScreeningWindow.Strategies);
            ScreeningMode = partOScreeningWindow.Mode;

            Close(PartOMixedDesignAction.Screen);
        }

        private void Close(PartOMixedDesignAction partOMixedDesignAction)
        {
            Action = partOMixedDesignAction;
            DialogResult = true;
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (DialogResult == true || session is null || !session.IsDirty)
            {
                return;
            }

            MessageBoxResult messageBoxResult = MessageBox.Show(this, "The selected strategies or an accepted design airflow have changed and are not saved onto the model.\n\nSave them now?", "Part O — Mixed Design", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    Action = PartOMixedDesignAction.SaveAndClose;
                    //Setting DialogResult inside Closing closes the window with it.
                    DialogResult = true;
                    break;

                case MessageBoxResult.No:
                    break;

                default:
                    e.Cancel = true;
                    break;
            }
        }

        private static string FilterText(PartOMixedDwellingFilter partOMixedDwellingFilter)
        {
            return partOMixedDwellingFilter switch
            {
                PartOMixedDwellingFilter.NeedsAttention => "Needs attention",
                PartOMixedDwellingFilter.FailingFinal => "Failing in the final run",
                PartOMixedDwellingFilter.NotSelected => "No strategy selected",
                PartOMixedDwellingFilter.SuggestionDiffers => "Suggestion differs from selection",
                _ => "All dwellings",
            };
        }

        private sealed class ProductItem
        {
            public ProductItem(VentilationUnitReference? ventilationUnitReference, string text)
            {
                Reference = ventilationUnitReference;
                Text = text;
            }

            public VentilationUnitReference? Reference { get; }

            public string Text { get; }

            public override string ToString() => Text;
        }
    }
}
