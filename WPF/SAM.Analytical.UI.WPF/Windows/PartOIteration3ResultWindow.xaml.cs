// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The Iteration 3 comparison: the reference case (the earlier iteration) against the system case (the
    /// same design with its ventilation run as an explicit TAS system, by one method) - or, where the system
    /// case did not complete, why.
    ///
    /// <para><b>Summary first, detail on demand</b></para>
    /// <para>
    /// The verdicts and the headline statistics are tiles; what the system case ran - the products and, for
    /// manufacturer operating guidance, the guidance as a table - is a card beneath them; the room comparison
    /// is the main area, one collapsed group per dwelling; and the full record (the stage ledger, the raw notes,
    /// the pairing summary) is behind Technical details. Copy All still writes every word of it.
    /// </para>
    ///
    /// <para><b>An incomplete system case shows no system-case number, anywhere</b></para>
    /// <para>
    /// On a refusal the tiles, the card, the filters and the grid are <b>collapsed</b>, and what is shown
    /// instead is the stage that stopped it, its reasons verbatim, the files that attempt genuinely produced and
    /// the stages that never ran. A grid of temperatures beside a refusal would be the most convincing wrong
    /// answer this window could give. It is enforced upstream too - <c>PartOIteration3Result</c> holds no
    /// comparison for a refused ledger.
    /// </para>
    ///
    /// <para><b>The last successful report stays reachable, and stays labelled historical</b></para>
    /// <para>
    /// When a design later moves and a review correctly refuses the result, the report the earlier success
    /// wrote is <b>not</b> overwritten and <b>not</b> presented as the current answer - the window offers it
    /// under its own name and says in the refusal what it is and is not.
    /// </para>
    ///
    /// <para><b>It renders text, it does not compose the record</b></para>
    /// <para>
    /// The pairing's own statements - the outcome, the summary, the refusal, the notes, Copy All - come from
    /// <see cref="PartOIteration3ReportText"/>, which the persisted report is assembled from. What this window
    /// adds is presentation: the tiles and the engineer-facing wording, all read straight off the result.
    /// </para>
    ///
    /// <para><b>Built for five thousand dwellings</b></para>
    /// <para>
    /// The grid virtualises and recycles <b>with grouping on</b> (<c>IsVirtualizingWhenGrouping</c>, without
    /// which WPF realises every row of a grouped view), the rows are flat values computed once, the groups
    /// open collapsed unless a filter has narrowed the list, and filtering rebuilds one list and reassigns one
    /// <c>ItemsSource</c>. Nothing walks the model, and no row is realised until it scrolls into view.
    /// </para>
    /// </summary>
    public partial class PartOIteration3ResultWindow : System.Windows.Window
    {
        /// <summary>At or below this many visible rows, a filtered or searched view opens its groups.</summary>
        internal const int Count_ExpandFiltered = 300;

        private PartOIteration3Result partOIteration3Result;

        private List<PartOIteration3Row> rows = [];

        /// <summary>The persisted report this window can open, or null where there is none on disk.</summary>
        private string path_Report_Available;

        /// <summary>Expand all / Collapse all: null follows the filter rule, true or false overrides it.</summary>
        private bool? expand_Override;

        public PartOIteration3ResultWindow()
        {
            InitializeComponent();

            Fit();
        }

        /// <summary>The pairing to present. Setting it rebuilds the whole window; set the other inputs first.</summary>
        public PartOIteration3Result Result
        {
            get
            {
                return partOIteration3Result;
            }

            set
            {
                partOIteration3Result = value;

                Refresh();
            }
        }

        /// <summary>Which earlier iteration the reference case is, in the engineer's terms - "Iteration 1a — baseline".</summary>
        public string ReferenceText { get; set; }

        /// <summary>The progress window's stage lines with their durations, for a run in this session. Null for a review.</summary>
        public List<string> StageTimings { get; set; }

        /// <summary>The manufacturer operating guidance the system case ran, as fields - see <see cref="Query.PartOIteration3GuidanceEvidence"/>.</summary>
        public List<PartOIteration3GuidanceEvidence> Guidance { get; set; }

        /// <summary>What <see cref="Guidance"/> is, or why there is none.</summary>
        public string GuidanceSource { get; set; }

        /// <summary>
        /// Keeps the window inside the desktop it opens on.
        /// <para>
        /// The XAML asks for 1320x860, which is what this window wants on a 1920x1080 desktop at 100% scaling.
        /// At 125% that desktop is 825 device-independent pixels tall and at 150% it is 688, so the requested
        /// height alone would put the action bar below the taskbar. So the request is clamped to the work area
        /// and the window is re-centred inside it. <c>SystemParameters.WorkArea</c> is already in
        /// device-independent pixels, so no DPI arithmetic belongs here.
        /// </para>
        /// </summary>
        private void Fit()
        {
            Rect rect;

            try
            {
                rect = SystemParameters.WorkArea;
            }
            catch (Exception)
            {
                //No desktop to measure - a test host, or a session with no interactive window station.
                return;
            }

            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            //Allowance for the window chrome, which is outside the client size these properties set.
            double width = Math.Min(Width, Math.Max(MinWidth, rect.Width - 20));
            double height = Math.Min(Height, Math.Max(MinHeight, rect.Height - 40));

            bool clamped = width < Width || height < Height;

            Width = width;
            Height = height;

            if (!clamped)
            {
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = rect.Left + ((rect.Width - width) / 2);
            Top = rect.Top + ((rect.Height - height) / 2);
        }

        private void Refresh()
        {
            if (partOIteration3Result is null)
            {
                return;
            }

            bool complete = partOIteration3Result.IsComplete;

            PartOIteration3Record partOIteration3Record = partOIteration3Result.Record;

            //------------------------------------------------------------------------------------------
            //Header, in the engineer's terms
            //------------------------------------------------------------------------------------------
            string label = partOIteration3Record is null ? null : Query.PartOIteration3MethodLabel(partOIteration3Record.BehaviourMode);

            textBlock_Heading.Text = string.IsNullOrWhiteSpace(label) ? "Iteration 3 comparison" : string.Format("Iteration 3 comparison — {0}", label);

            textBlock_Cases.Text = string.Format(
                "Reference case: {0}{1}   ·   System case: explicit TAS system{2}   ·   {3}",
                ReferenceText ?? "earlier Part O iteration",
                string.IsNullOrWhiteSpace(partOIteration3Record?.ProjectName_ReferenceA) ? string.Empty : string.Format(" ({0})", partOIteration3Record.ProjectName_ReferenceA),
                string.IsNullOrWhiteSpace(partOIteration3Record?.ProjectName_CandidateB) ? string.Empty : string.Format(" ({0})", partOIteration3Record.ProjectName_CandidateB),
                partOIteration3Result.IsRestored ? "reopened from the saved result — no TAS simulation was run" : "produced in this session");

            textBlock_Stages.Text = StagesText();

            //------------------------------------------------------------------------------------------
            //Technical details - the pairing's own statements, verbatim
            //------------------------------------------------------------------------------------------
            textBlock_Outcome.Text = PartOIteration3ReportText.Outcome(partOIteration3Result);
            textBlock_Summary.Text = PartOIteration3ReportText.Summary(partOIteration3Result);

            dataGrid_Stage.ItemsSource = partOIteration3Result.Ledger.Stages;

            textBox_Diagnostics.Text = PartOIteration3ReportText.Diagnostics(partOIteration3Result);

            //Resolved before the refusal text is composed, because a refusal that has a historical report
            //to point at says so, and one that has not must not.
            path_Report_Available = Path_Report();

            //------------------------------------------------------------------------------------------
            //Exactly one of the refusal and the comparison is shown, and it takes the slack
            //------------------------------------------------------------------------------------------
            textBlock_Refusal.Text = complete ? string.Empty : Refusal();
            textBlock_RefusalHeading.Text = complete ? string.Empty : RefusalHeading();
            border_Refusal.Visibility = complete ? Visibility.Collapsed : Visibility.Visible;

            rowDefinition_Refusal.Height = complete ? new GridLength(0) : new GridLength(3, GridUnitType.Star);
            rowDefinition_Refusal.MinHeight = complete ? 0 : 80;

            rowDefinition_Comparison.Height = complete ? new GridLength(3, GridUnitType.Star) : new GridLength(0);
            rowDefinition_Comparison.MinHeight = complete ? 120 : 0;

            Visibility visibility = complete ? Visibility.Visible : Visibility.Collapsed;

            stackPanel_Comparison.Visibility = visibility;
            stackPanel_Filters.Visibility = visibility;
            dataGrid_Comparison.Visibility = visibility;

            if (complete)
            {
                RefreshTiles();
                RefreshSystem();

                rows = PartOIteration3Row.Rows(partOIteration3Result.Comparison);
            }
            else
            {
                rows = [];
            }

            checkBox_Failures.Content = string.Format(CultureInfo.CurrentCulture, "Failures ({0})", rows.Count(x => x.IsFailure));
            checkBox_Changed.Content = string.Format(CultureInfo.CurrentCulture, "Changed ({0})", rows.Count(x => x.Changed));
            checkBox_Largest.Content = "Largest differences";

            ApplyFilter();

            //------------------------------------------------------------------------------------------
            //Actions
            //------------------------------------------------------------------------------------------
            button_ReportA.IsEnabled = partOIteration3Result.Assessment_ReferenceA?.ReportText is not null;
            button_ReportA.ToolTip = button_ReportA.IsEnabled
                ? "Show the CIBSE TM59 report for the reference case."
                : "The reference case produced no TM59 report on this attempt.";

            button_ReportB.IsEnabled = complete && partOIteration3Result.Assessment_CandidateB?.ReportText is not null;
            button_ReportB.ToolTip = button_ReportB.IsEnabled
                ? "Show the CIBSE TM59 report for the system case."
                : "The system case produced no TM59 report on this attempt.";

            button_OpenFolder.IsEnabled = !string.IsNullOrWhiteSpace(Folder());
            button_OpenFolder.ToolTip = button_OpenFolder.IsEnabled ? "Open the folder holding this comparison's files." : "This comparison recorded no folder.";

            RefreshReportButton(complete);
        }

        /// <summary>
        /// The stages as one line - with their durations for a run in this session, and as the ledger records
        /// them (no durations, which a saved record does not hold) for a review.
        /// </summary>
        private string StagesText()
        {
            if (StageTimings is not null && StageTimings.Count != 0)
            {
                return string.Join("     ", StageTimings);
            }

            PartOIteration3BehaviourMode partOIteration3BehaviourMode = partOIteration3Result.Record?.BehaviourMode ?? PartOIteration3BehaviourMode.Parity;

            IReadOnlyList<string> phases = Modify.PartOIteration3Phases(partOIteration3BehaviourMode);

            List<string> result = [];

            for (int i = 0; i < phases.Count; i++)
            {
                List<PartOIteration3StageStatus> statuses = partOIteration3Result.Ledger.Stages
                    .Where(x => Modify.PartOIteration3Phase(x.Stage, partOIteration3BehaviourMode) == i)
                    .Select(x => x.Status)
                    .ToList();

                //A phase refused where any of its stages did, completed where all of them did, not run otherwise.
                PartOProgressStageStatus partOProgressStageStatus = statuses.Contains(PartOIteration3StageStatus.Refused)
                    ? PartOProgressStageStatus.Failed
                    : statuses.Count != 0 && statuses.TrueForAll(x => x == PartOIteration3StageStatus.Completed)
                        ? PartOProgressStageStatus.Completed
                        : PartOProgressStageStatus.Pending;

                result.Add(string.Format("{0} {1}", PartOProgressState.Glyph(partOProgressStageStatus), phases[i]));
            }

            return string.Join("     ", result);
        }

        private void RefreshTiles()
        {
            PartOIteration3Statistics partOIteration3Statistics = partOIteration3Result.Comparison.Statistics;

            SetVerdict(textBlock_TileReference, partOIteration3Result.Assessment_ReferenceA);
            SetVerdict(textBlock_TileSystem, partOIteration3Result.Assessment_CandidateB);

            textBlock_TileBias.Text = string.Format(CultureInfo.CurrentCulture, "{0:+0.00;-0.00;0.00} K", partOIteration3Statistics.MeanBias);
            textBlock_TileRmse.Text = string.Format(CultureInfo.CurrentCulture, "{0:0.00} K", partOIteration3Statistics.RootMeanSquareError);
            textBlock_TileMax.Text = string.Format(CultureInfo.CurrentCulture, "{0:0.00} K", partOIteration3Statistics.MaximumAbsoluteDifference);

            border_TileMax.ToolTip = string.Format(
                CultureInfo.CurrentCulture,
                "In '{0}' at hour {1} of the year. Over {2} room(s) and {3} hourly value(s) each side.",
                partOIteration3Statistics.Name_Space_MaximumAbsoluteDifference ?? "-",
                partOIteration3Statistics.Hour_MaximumAbsoluteDifference,
                partOIteration3Statistics.Count_Rooms,
                partOIteration3Statistics.Count_Values);

            textBlock_TileChanged.Text = string.Format(CultureInfo.CurrentCulture, "{0} of {1}", partOIteration3Result.Comparison.Count_Changed, partOIteration3Result.Comparison.Criteria.Count);
        }

        private static void SetVerdict(TextBlock textBlock, PartOIteration3Assessment partOIteration3Assessment)
        {
            if (partOIteration3Assessment is null || !partOIteration3Assessment.IsAssessed)
            {
                textBlock.Text = "—";
                textBlock.Foreground = Brushes.Gray;

                return;
            }

            TM59ComplianceStatus tM59ComplianceStatus = partOIteration3Assessment.OccupiedSpaceComplianceStatus;

            textBlock.Text = Query.PartOVerdictText(tM59ComplianceStatus);
            textBlock.Foreground = tM59ComplianceStatus switch
            {
                TM59ComplianceStatus.Pass => Brushes.SeaGreen,
                TM59ComplianceStatus.Fail => Brushes.Firebrick,
                _ => Brushes.DimGray,
            };
        }

        /// <summary>What the system case ran: the method, the products and - for guidance - the guidance as fields.</summary>
        private void RefreshSystem()
        {
            PartOIteration3Record partOIteration3Record = partOIteration3Result.Record;

            List<KeyValuePair<string, string>> facts = [];

            dataGrid_SystemUnits.ItemsSource = null;
            expander_SystemUnits.Visibility = Visibility.Collapsed;
            textBlock_SystemSource.Text = string.Empty;

            if (partOIteration3Record is null)
            {
                expander_System.Visibility = Visibility.Collapsed;

                return;
            }

            expander_System.Visibility = Visibility.Visible;

            PartOIteration3BehaviourMode partOIteration3BehaviourMode = partOIteration3Record.BehaviourMode;

            switch (partOIteration3BehaviourMode)
            {
                case PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance:
                    textBlock_SystemHeader.Text = "Manufacturer operating guidance";
                    GuidanceFacts(facts);
                    break;

                case PartOIteration3BehaviourMode.SelectedProduct:
                    textBlock_SystemHeader.Text = "Selected products — certified efficiency and SFP";
                    ProductFacts(facts, partOIteration3Record.Equipment.ConvertAll(x => x.Reference ?? x.ToString()));
                    facts.Add(new KeyValuePair<string, string>("Applied", "Certified sensible heat-recovery efficiency and specific fan power; fan heat from the system."));
                    break;

                case PartOIteration3BehaviourMode.SelectedProductCooling:
                    textBlock_SystemHeader.Text = "Selected products — published cooling table (validation)";
                    ProductFacts(facts, partOIteration3Record.Cooling.ConvertAll(x => Query.PartOIteration3ProductText(x.Reference, x.CoolingModuleModel)));
                    facts.Add(new KeyValuePair<string, string>("Applied", "The route check plus each product's published cooling table, run as recirculation."));
                    break;

                default:
                    textBlock_SystemHeader.Text = "Route check (validation)";
                    facts.Add(new KeyValuePair<string, string>("Applied", "The reference case's own design airflows as an explicit system - no product and no fan heat."));
                    facts.Add(new KeyValuePair<string, string>("Use", "Shows how closely the explicit system model reproduces the reference case."));
                    break;
            }

            itemsControl_SystemFacts.ItemsSource = facts;
        }

        private static void ProductFacts(List<KeyValuePair<string, string>> facts, List<string> products)
        {
            List<string> lines = products
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => string.Format(CultureInfo.CurrentCulture, "{0} × {1}", x.Count(), x.Key))
                .ToList();

            facts.Add(new KeyValuePair<string, string>("Products", lines.Count == 0 ? "None recorded" : string.Join(Environment.NewLine, lines.Take(6)) + (lines.Count > 6 ? string.Format(CultureInfo.CurrentCulture, "{0}… and {1} more", Environment.NewLine, lines.Count - 6) : string.Empty)));
        }

        /// <summary>
        /// The guidance as a card. Where every unit runs the same product the same way - the usual case - it is
        /// one set of values; otherwise the products are listed and the per-unit table carries the rest.
        /// </summary>
        private void GuidanceFacts(List<KeyValuePair<string, string>> facts)
        {
            List<PartOIteration3GuidanceEvidence> guidance = Guidance ?? [];

            textBlock_SystemSource.Text = string.Join(" ", new[] { GuidanceSource, "Manufacturer guidance — provisional, not certified performance." }.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (guidance.Count == 0)
            {
                facts.Add(new KeyValuePair<string, string>("Applied", Query.PartOIteration3MethodExplanation(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)));

                return;
            }

            CultureInfo cultureInfo = CultureInfo.CurrentCulture;

            List<IGrouping<string, PartOIteration3GuidanceEvidence>> groupings = guidance
                .GroupBy(x => string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3:0.###}|{4:0.###}|{5:0.###}|{6:0.###}|{7:0.###}", x.Reference, x.CoolingModuleModel, x.CoolingActivation, x.CoolingActivationTemperature_C, x.ElevatedAirFlow_Lps, x.ExchangerExtractFraction, x.CoilNetTemperatureDrop_K, x.MinimumSupplyTemperature_C))
                .ToList();

            if (groupings.Count == 1)
            {
                PartOIteration3GuidanceEvidence first = guidance[0];

                facts.Add(new KeyValuePair<string, string>("Product", Query.PartOIteration3ProductText(first.Reference, first.CoolingModuleModel)));
                facts.Add(new KeyValuePair<string, string>("Applies to", string.Format(cultureInfo, "{0} ventilation unit(s)", guidance.Count)));
                facts.Add(new KeyValuePair<string, string>("Cooling control", string.IsNullOrWhiteSpace(first.CoolingActivation) ? "—" : first.CoolingActivation));
                facts.Add(new KeyValuePair<string, string>("Setpoint", Number(first.CoolingActivationTemperature_C, "{0:0.#} °C", cultureInfo)));
                facts.Add(new KeyValuePair<string, string>("Background (design) flow", Range(guidance.ConvertAll(x => x.DesignSupplyFlowRate_Lps), "l/s", cultureInfo)));
                facts.Add(new KeyValuePair<string, string>("Cooling flow", string.Format(cultureInfo, "{0} (stated range {1}–{2} l/s)", Number(first.ElevatedAirFlow_Lps, "{0:0.#} l/s", cultureInfo), Number(first.MinimumElevatedAirFlow_Lps, "{0:0.#}", cultureInfo), Number(first.MaximumElevatedAirFlow_Lps, "{0:0.#}", cultureInfo))));
                facts.Add(new KeyValuePair<string, string>("Supply minimum", Number(first.MinimumSupplyTemperature_C, "{0:0.#} °C", cultureInfo)));
                facts.Add(new KeyValuePair<string, string>("Strategy", string.Format(cultureInfo, "Heat exchanger (bypass, or recovery at {0:0.00}) → DX coil, net drop {1}", first.ExchangerExtractFraction, Number(first.CoilNetTemperatureDrop_K, "{0:0.0} K", cultureInfo))));
            }
            else
            {
                ProductFacts(facts, guidance.ConvertAll(x => Query.PartOIteration3ProductText(x.Reference, x.CoolingModuleModel)));
                facts.Add(new KeyValuePair<string, string>("Strategy", "Heat exchanger (bypass or recovery) → DX coil. The units run different guidance values - see Per unit."));
            }

            dataGrid_SystemUnits.ItemsSource = guidance;
            expander_SystemUnits.Visibility = Visibility.Visible;
            expander_SystemUnits.Header = string.Format(cultureInfo, "Per unit ({0})", guidance.Count);
        }

        private static string Number(double value, string format, CultureInfo cultureInfo)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? "—" : string.Format(cultureInfo, format, value);
        }

        private static string Range(List<double> values, string unit, CultureInfo cultureInfo)
        {
            List<double> finite = values.FindAll(x => !double.IsNaN(x) && !double.IsInfinity(x));

            if (finite.Count == 0)
            {
                return "—";
            }

            double minimum = finite.Min();
            double maximum = finite.Max();

            return Math.Abs(maximum - minimum) < 0.05
                ? string.Format(cultureInfo, "{0:0.#} {1}", minimum, unit)
                : string.Format(cultureInfo, "{0:0.#}–{1:0.#} {2}", minimum, maximum, unit);
        }

        /// <summary>
        /// The report button, which says two different things depending on what it would open.
        /// <para>
        /// After a completed comparison it opens <b>this</b> one's saved report. After a refusal it opens the
        /// last successful one, under a name that says so - because the refusal means the result no longer
        /// describes the model in front of the user.
        /// </para>
        /// </summary>
        private void RefreshReportButton(bool complete)
        {
            bool exists = !string.IsNullOrWhiteSpace(path_Report_Available);

            button_OpenReport.IsEnabled = exists;

            if (complete)
            {
                button_OpenReport.Content = "Comparison report";

                button_OpenReport.ToolTip = exists
                    ? string.Format("Open the full comparison report saved for this result:{0}{1}", Environment.NewLine, path_Report_Available)
                    : partOIteration3Result?.Refusal_Report ?? "This comparison saved no report.";

                return;
            }

            button_OpenReport.Content = "Last successful report";

            button_OpenReport.ToolTip = exists
                ? string.Format(
                    "Open the last comparison report saved for this method:{0}{1}{0}{0}It is HISTORICAL. It describes the comparison as it was when it succeeded, NOT the model in front of you - which is why this one did not complete.",
                    Environment.NewLine,
                    path_Report_Available)
                : "No successful comparison report has been saved for this method.";
        }

        /// <summary>The refusal's one-line heading, in the engineer's terms.</summary>
        private string RefusalHeading()
        {
            PartOIteration3Stage? partOIteration3Stage = partOIteration3Result.Ledger.Stage_Refused;

            string stage = partOIteration3Stage.HasValue ? Core.Query.Description(partOIteration3Stage.Value).ToLowerInvariant() : "an unrecorded stage";

            return partOIteration3Result.IsRestored && partOIteration3Stage == PartOIteration3Stage.Input && (partOIteration3Result.Record?.IsComplete ?? false)
                ? "This saved result can no longer be shown for the model in front of you. No system case results are presented."
                : string.Format("The system case did not complete — it stopped at {0}. No system case results are presented. It can be run again from the Part O Hub.", stage);
        }

        /// <summary>
        /// The refusal as the report authority states it, plus - only where one is actually on disk - a
        /// statement that a historical report exists, and what it is and is not.
        /// </summary>
        private string Refusal()
        {
            string refusal = PartOIteration3ReportText.Refusal(partOIteration3Result);

            if (string.IsNullOrWhiteSpace(path_Report_Available))
            {
                return refusal;
            }

            return string.Concat(
                refusal,
                Environment.NewLine,
                Environment.NewLine,
                string.Format(
                    "A previously saved comparison report for this method is still on disk at '{0}'. It is HISTORICAL: it describes the comparison as it was when it succeeded, and it does NOT describe the analytical model in front of you. This refusal has not overwritten it, and it is not offered as this attempt's result.",
                    path_Report_Available));
        }

        /// <summary>
        /// The persisted report to offer, or null where there is none.
        /// <para>
        /// A completed review names the report it has just written. A refused one names nothing, so the
        /// path is derived from the pairing record exactly as the writer derives it, and is offered only
        /// where the file is genuinely there.
        /// </para>
        /// </summary>
        private string Path_Report()
        {
            string path = partOIteration3Result?.Path_Report;

            if (string.IsNullOrWhiteSpace(path))
            {
                path = PartOIteration3Paths.Path_Report_ForRecord(partOIteration3Result?.Path_Record);
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                return System.IO.File.Exists(path) ? path : null;
            }
            catch (Exception)
            {
                //A path that cannot be stat'ed offers nothing, which is the safe direction.
                return null;
            }
        }

        /// <summary>
        /// Rebuilds the visible rows from the filters, then groups - or, for Largest differences, lists them
        /// flat, largest first. One pass over the rows, one assignment; the grid virtualises whatever comes out.
        /// </summary>
        private void ApplyFilter()
        {
            if (dataGrid_Comparison is null)
            {
                return;
            }

            string text = textBox_Search?.Text;
            string text_Lower = string.IsNullOrWhiteSpace(text) ? null : text.Trim().ToLowerInvariant();

            bool changedOnly = checkBox_Changed?.IsChecked ?? false;
            bool failuresOnly = checkBox_Failures?.IsChecked ?? false;
            bool largest = checkBox_Largest?.IsChecked ?? false;

            List<PartOIteration3Row> result = [];

            foreach (PartOIteration3Row partOIteration3Row in rows)
            {
                if (changedOnly && !partOIteration3Row.Changed)
                {
                    continue;
                }

                if (failuresOnly && !partOIteration3Row.IsFailure)
                {
                    continue;
                }

                if (!partOIteration3Row.Matches(text_Lower))
                {
                    continue;
                }

                result.Add(partOIteration3Row);
            }

            if (largest)
            {
                //Sorted once, here, rather than through a SortDescription the view would re-evaluate.
                result.Sort((x, y) => y.MaximumAbsoluteDifference.CompareTo(x.MaximumAbsoluteDifference));
            }

            ListCollectionView listCollectionView = new(result);

            if (!largest)
            {
                listCollectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PartOIteration3Row.DwellingGroup)));
            }

            //Groups open collapsed - a list of dwellings - unless the filters have narrowed the list to
            //something a person can read, or they asked for everything opened.
            bool narrowed = changedOnly || failuresOnly || text_Lower is not null;

            dataGrid_Comparison.Tag = expand_Override ?? (narrowed && result.Count <= Count_ExpandFiltered);

            dataGrid_Comparison.ItemsSource = listCollectionView;

            button_ExpandAll.Visibility = largest ? Visibility.Collapsed : Visibility.Visible;
            button_CollapseAll.Visibility = largest ? Visibility.Collapsed : Visibility.Visible;

            int count_Dwellings = largest ? 0 : listCollectionView.Groups?.Count ?? 0;

            textBlock_RowCount.Text = (result.Count == rows.Count
                ? string.Format(CultureInfo.CurrentCulture, "{0} row(s)", rows.Count)
                : string.Format(CultureInfo.CurrentCulture, "{0} of {1} row(s)", result.Count, rows.Count))
                + (count_Dwellings == 0 ? string.Empty : string.Format(CultureInfo.CurrentCulture, " · {0} dwelling(s)", count_Dwellings));
        }

        /// <summary>Whether the dwelling groups are currently set to open. Exposed for tests.</summary>
        internal bool GroupsExpanded => dataGrid_Comparison.Tag is bool expanded && expanded;

        /// <summary>The visible rows, in their current order - what Copy All writes. Exposed for tests.</summary>
        internal List<PartOIteration3Row> Rows_Visible
        {
            get
            {
                List<PartOIteration3Row> result = [];

                foreach (object item in dataGrid_Comparison.ItemsSource as ListCollectionView ?? (System.Collections.IEnumerable)new List<PartOIteration3Row>())
                {
                    if (item is PartOIteration3Row partOIteration3Row)
                    {
                        result.Add(partOIteration3Row);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The whole window as tab-separated text, over the rows currently shown.
        /// <para>
        /// The same composition the persisted report uses, so a copy and the saved file say the same
        /// things in the same order. They differ in exactly two stated ways: a copy follows the filters a
        /// person is holding and is formatted for their culture, while the saved report takes every row,
        /// the invariant culture and the pairing's full provenance.
        /// </para>
        /// </summary>
        internal string CopyAllText()
        {
            return PartOIteration3ReportText.Text(partOIteration3Result, Rows_Visible, CultureInfo.CurrentCulture);
        }

        private string Folder()
        {
            string path = partOIteration3Result?.Path_Record;

            if (string.IsNullOrWhiteSpace(path))
            {
                path = partOIteration3Result?.Record?.File(PartOIteration3Roles.Bridge_TSD)?.Path;
            }

            return string.IsNullOrWhiteSpace(path) ? null : System.IO.Path.GetDirectoryName(path);
        }

        private void textBox_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            expand_Override = null;

            ApplyFilter();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            expand_Override = null;

            ApplyFilter();
        }

        private void button_All_Click(object sender, RoutedEventArgs e)
        {
            expand_Override = null;

            checkBox_Failures.IsChecked = false;
            checkBox_Changed.IsChecked = false;
            checkBox_Largest.IsChecked = false;
            textBox_Search.Text = string.Empty;

            ApplyFilter();
        }

        private void button_ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            expand_Override = true;

            ApplyFilter();
        }

        private void button_CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            expand_Override = false;

            ApplyFilter();
        }

        private void button_ReportA_Click(object sender, RoutedEventArgs e)
        {
            ShowReport(partOIteration3Result?.Assessment_ReferenceA, partOIteration3Result?.Path_TM59Report_ReferenceA, "Reference case");
        }

        private void button_ReportB_Click(object sender, RoutedEventArgs e)
        {
            ShowReport(partOIteration3Result?.Assessment_CandidateB, partOIteration3Result?.Path_TM59Report_CandidateB, "System case");
        }

        /// <summary>
        /// The existing TM59 result window, over the production report text this assessment produced -
        /// the same window the ordinary Review Results command shows, so there is one place a TM59 report
        /// is read.
        /// </summary>
        private void ShowReport(PartOIteration3Assessment partOIteration3Assessment, string path_Report, string description)
        {
            if (partOIteration3Assessment?.ReportText is null)
            {
                return;
            }

            PartOTM59ResultWindow partOTM59ResultWindow = new()
            {
                Report = partOIteration3Assessment.ReportText,
                Title = string.Format("Part O — Iteration 3 — {0} — CIBSE TM59", description),
            };

            partOTM59ResultWindow.SetDiagnostics(partOIteration3Assessment.AssociationRefusals, partOIteration3Assessment.VentilationStrategyRefusals);

            partOTM59ResultWindow.ResultSummary = PartOTM59ResultSummary.ForStatus(partOIteration3Assessment.OccupiedSpaceComplianceStatus);

            partOTM59ResultWindow.Summary = string.Format(
                "{0}: TM59 {1} over {2}. {3}",
                description,
                Query.PartOVerdictText(partOIteration3Assessment.OccupiedSpaceComplianceStatus),
                UI.Query.PartOCount(partOIteration3Assessment.Count_Processed, "processed space", "processed spaces"),
                string.IsNullOrWhiteSpace(path_Report) ? "No report file was written." : string.Format("Report: {0}", path_Report));

            //Owned, so the child cannot be lost behind the main application window.
            new System.Windows.Interop.WindowInteropHelper(partOTM59ResultWindow).Owner = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            partOTM59ResultWindow.ShowDialog();
        }

        private void button_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Open(Folder());
        }

        private void button_OpenReport_Click(object sender, RoutedEventArgs e)
        {
            //Re-resolved on the click: the file may have been moved or deleted since the window opened,
            //and handing the shell a path that has gone is an error dialog nobody asked for.
            string path = Path_Report();

            if (string.IsNullOrWhiteSpace(path))
            {
                path_Report_Available = null;

                RefreshReportButton(partOIteration3Result?.IsComplete ?? false);

                return;
            }

            Open(path);
        }

        /// <summary>
        /// Hands a path to the shell, or does nothing where there is nothing to hand it. Every failure is a
        /// refusal to act, never an exception.
        /// </summary>
        private static void Open(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
                {
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception)
            {
                //No shell association, a refused launch, or a path the file system would not answer for.
            }
        }

        private void button_CopyAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(CopyAllText());
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                //Another process can hold the clipboard open; the text is still on screen.
            }
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
            }
            catch (InvalidOperationException)
            {
                //Shown with Show() rather than ShowDialog() - a host that embeds this window. Closing is the
                //same intent.
                Close();
            }
        }
    }
}
