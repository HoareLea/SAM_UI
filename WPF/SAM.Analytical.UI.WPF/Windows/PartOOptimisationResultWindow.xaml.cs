// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The history of an automatic Iteration 2B optimisation - not a progress dialog after the fact, but the
    /// engineering record of what was tried and what it produced.
    /// <para>
    /// <b>Presentation only.</b> Every number shown is one the optimisation recorded: the design airflows
    /// from the round, the Approved Document F requirements from the adjustments that carry them, the
    /// duties and ratings from the unit states, and the TM59 verdicts from the production assessment.
    /// Nothing is recomputed here, and in particular no pass or fail is re-derived from an Actual and a
    /// Limit.
    /// </para>
    /// <para>
    /// <b>Simple by default, detailed on demand.</b> The kept design's TM59 outcome and the stop reason come
    /// first, one wording per <see cref="PartOOptimisationStopReason"/> (<see cref="PartOOptimisationSummary"/>):
    /// an optimisation that ended at the selected unit's capacity with rooms still failing is a real, useful
    /// answer; one that ended because a simulation would not run is not, and a reader must not have to work
    /// out which they are looking at. The histories and every note follow, collapsed; the run's own complete
    /// statement heads the notes, so nothing the summary shortens is lost.
    /// </para>
    /// <para>
    /// <b>The diagnostic capacity envelope is shown apart from the run's answer, always.</b> It says what
    /// the already-selected unit could deliver if taken to its own ceiling - which is a different statement
    /// from what the optimisation accepted, and would be actively misleading read as the run's best result.
    /// So it has its own summary line, its own Stage value in both grids, and its own <c>MAX</c> run label;
    /// and where none was calculated although it was asked for, the line says why rather than going blank.
    /// </para>
    /// </summary>
    public partial class PartOOptimisationResultWindow : System.Windows.Window
    {
        public PartOOptimisationResultWindow()
        {
            InitializeComponent();

            SizeChanged += OnGrown;
        }

        /// <summary>Whether the first placement has run; growth after it is answered by <see cref="OnGrown"/>.</summary>
        private bool placed;

        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);

            //Never taller than the monitor it is on - the one its handle is on, not the primary: the content
            //scrolls and Copy All / Close stay reachable with the detail open. The Hub's placement, shared.
            PartOWindowPlacement.KeepOnScreen(this);

            placed = true;
        }

        /// <summary>
        /// Opening Engineering detail makes the window taller after it was placed, since its height follows its
        /// content; growth that would take Copy All / Close below the working area moves it up again - the Hub's
        /// rule. A size the person dragged (<c>SizeToContent</c> then <c>Manual</c>) is left alone.
        /// </summary>
        private void OnGrown(object sender, SizeChangedEventArgs e)
        {
            if (placed && e.HeightChanged && e.NewSize.Height > e.PreviousSize.Height && SizeToContent != SizeToContent.Manual)
            {
                PartOWindowPlacement.KeepOnScreen(this);
            }
        }

        /// <summary>
        /// The run to show, with no Cancel request recorded. Setting it fills the summary, both histories and
        /// the notes - see <see cref="Show"/>.
        /// </summary>
        public PartOOptimisationRun OptimisationRun
        {
            set => Show(value, false);
        }

        /// <summary>What the window says above the detail. Exposed so what the user is told is assertable.</summary>
        internal PartOOptimisationSummary? Summary { get; private set; }

        /// <summary>
        /// Fills the window from the run: the outcome and summary first (<see cref="PartOOptimisationSummary"/>),
        /// the histories and notes collapsed below.
        /// </summary>
        /// <param name="cancelRequested">Whether Cancel was requested in the progress window - used only to
        /// explain a stop that was not the cancellation.</param>
        /// <param name="canContinue">Whether the session run still holds the kept design, so 2B can be started
        /// again from it - see <see cref="PartOOptimisationSummary.Create"/>.</param>
        internal void Show(PartOOptimisationRun? partOOptimisationRun, bool cancelRequested, bool canContinue = false)
        {
            PartOOptimisationSummary summary = PartOOptimisationSummary.Create(partOOptimisationRun!, cancelRequested, canContinue);

            Summary = summary;

            textBlock_VerdictGlyph.Text = summary.Glyph;
            textBlock_Verdict.Text = summary.VerdictText;

            Brush brush = VerdictBrush(summary.Verdict);
            textBlock_VerdictGlyph.Foreground = brush;
            border_Verdict.BorderBrush = brush;

            textBlock_StopReason.Text = summary.StopHeadline;
            textBlock_StopMeaning.Text = summary.StopMeaning;

            textBlock_CancelNote.Text = summary.CancelNote ?? string.Empty;
            textBlock_CancelNote.Visibility = string.IsNullOrWhiteSpace(summary.CancelNote) ? Visibility.Collapsed : Visibility.Visible;

            itemsControl_Facts.ItemsSource = summary.Facts;

            run_NextStep.Text = summary.NextStep;

            List<PartOOptimisationAirFlowRow> rows_AirFlow = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun);
            List<PartOOptimisationUnitRow> rows_Unit = PartOOptimisationUnitRow.Rows(partOOptimisationRun);

            dataGrid_AirFlow.ItemsSource = rows_AirFlow;
            dataGrid_Unit.ItemsSource = rows_Unit;

            tabItem_AirFlow.Header = string.Format("Design airflow changes ({0})", UI.Query.PartOCount(rows_AirFlow.Count, "row", "rows"));
            tabItem_Unit.Header = string.Format("Ventilation unit duty by round ({0})", UI.Query.PartOCount(rows_Unit.Count, "row", "rows"));

            int count = SetDiagnostics(partOOptimisationRun);

            tabItem_Diagnostics.Header = string.Format("Notes, warnings and refusals ({0})", count);

            run_DetailSummary.Text = string.Format(
                " — every round's design airflow, ventilation unit duty and {0}",
                UI.Query.PartOCount(count, "note", "notes"));
        }

        private Brush VerdictBrush(PartOTM59Verdict partOTM59Verdict)
        {
            string key = partOTM59Verdict switch
            {
                PartOTM59Verdict.Pass => "PartO.Brush.Success",
                PartOTM59Verdict.Fail => "PartO.Brush.Danger",
                _ => "PartO.Brush.Muted",
            };

            return TryFindResource(key) as Brush ?? Brushes.Gray;
        }

        /// <summary>
        /// Every iteration's notes, warnings and refusals, in order and labelled by iteration - including
        /// each iteration's unique TSD, which is what makes the run auditable afterwards.
        /// </summary>
        private int SetDiagnostics(PartOOptimisationRun? partOOptimisationRun)
        {
            StringBuilder stringBuilder = new();

            //The run's own complete statement first, in its own words: the summary above shortens it, and
            //nothing it says is lost.
            if (partOOptimisationRun is not null)
            {
                stringBuilder.AppendLine(partOOptimisationRun.Description);
                stringBuilder.AppendLine();
            }

            int count = 0;

            foreach (PartOOptimisationStep partOOptimisationStep in partOOptimisationRun?.Steps ?? [])
            {
                stringBuilder.AppendLine(string.Format(
                    "RUN {0} ({1}) - weather '{2}', results '{3}'",
                    partOOptimisationStep.IsCapacityEnvelope ? "MAX" : partOOptimisationStep.Iteration.ToString(),
                    Core.Query.Description(partOOptimisationStep.Kind),
                    partOOptimisationStep.WeatherData ?? "-",
                    partOOptimisationStep.Path_TSD ?? "-"));

                foreach (DesignAirFlowTargetRefusal designAirFlowTargetRefusal in partOOptimisationStep.TargetRefusals)
                {
                    stringBuilder.AppendLine(string.Format("  NOT OPTIMISABLE: {0}", designAirFlowTargetRefusal));

                    count++;
                }

                foreach (string note in Distinct(partOOptimisationStep.Notes))
                {
                    stringBuilder.AppendLine(string.Format("  {0}", note));

                    count++;
                }

                foreach (string warning in Distinct(partOOptimisationStep.Warnings))
                {
                    stringBuilder.AppendLine(string.Format("  WARNING: {0}", warning));

                    count++;
                }

                foreach (string refusal in Distinct(partOOptimisationStep.Refusals))
                {
                    stringBuilder.AppendLine(string.Format("  REFUSED: {0}", refusal));

                    count++;
                }

                stringBuilder.AppendLine();
            }

            textBox_Diagnostics.Text = stringBuilder.Length == 0 ? "Nothing was recorded." : stringBuilder.ToString();

            //The warm-start count is a WORKFLOW fact, so it belongs beside the notes rather than anywhere
            //near the engineering summary at the top. Reported whenever any iteration warm started, because
            //an engineer auditing a run's duration needs to know which iterations reused the conversion -
            //and each of those iterations' own notes says so too.
            int warmStarted = partOOptimisationRun?.WarmStarted ?? 0;

            label_Diagnostics.Text = warmStarted == 0
                ? "Every iteration's notes, warnings and refusals, in order, with each iteration's own results file."
                : string.Format("Every iteration's notes, warnings and refusals, in order, with each iteration's own results file. {0} reused the baseline conversion; each still ran its own full-year simulation.", UI.Query.PartOCount(warmStarted, "iteration", "iterations"));

            return count;
        }

        /// <summary>
        /// One line per distinct message. A round reports the same allocation note through both the round
        /// and the dwelling, and repeating it would bury the lines that only appear once.
        /// </summary>
        private static List<string> Distinct(IEnumerable<string> descriptions)
        {
            List<string> result = [];

            HashSet<string> seen = [];

            foreach (string description in descriptions ?? [])
            {
                if (!string.IsNullOrWhiteSpace(description) && seen.Add(description))
                {
                    result.Add(description);
                }
            }

            return result;
        }

        private void button_CopyAll_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder stringBuilder = new();

            stringBuilder.AppendLine(Summary?.Text ?? string.Empty);

            stringBuilder.AppendLine();

            stringBuilder.AppendLine("Run\tStage\tSpace\tType\tDirection\tDesign before\tRequested\tAchieved\tPart F requires\tTM59");

            foreach (PartOOptimisationAirFlowRow partOOptimisationAirFlowRow in dataGrid_AirFlow.ItemsSource as List<PartOOptimisationAirFlowRow> ?? [])
            {
                stringBuilder.AppendLine(string.Format(
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5:0.###}\t{6}\t{7:0.###}\t{8:0.###}\t{9}",
                    partOOptimisationAirFlowRow.Run,
                    partOOptimisationAirFlowRow.Stage,
                    partOOptimisationAirFlowRow.Space,
                    partOOptimisationAirFlowRow.Type,
                    partOOptimisationAirFlowRow.Direction,
                    partOOptimisationAirFlowRow.DesignBefore_Lps,
                    partOOptimisationAirFlowRow.Requested_Lps.HasValue ? partOOptimisationAirFlowRow.Requested_Lps.Value.ToString("0.###") : "-",
                    partOOptimisationAirFlowRow.Achieved_Lps,
                    partOOptimisationAirFlowRow.Requirement_Lps,
                    partOOptimisationAirFlowRow.TM59Status));
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Run\tStage\tAHU\tSystem\tDuty\tMaximum\tHeadroom\tProduct\tEquipment");

            foreach (PartOOptimisationUnitRow partOOptimisationUnitRow in dataGrid_Unit.ItemsSource as List<PartOOptimisationUnitRow> ?? [])
            {
                stringBuilder.AppendLine(string.Format(
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                    partOOptimisationUnitRow.Run,
                    partOOptimisationUnitRow.Stage,
                    partOOptimisationUnitRow.AHU,
                    partOOptimisationUnitRow.System,
                    partOOptimisationUnitRow.Duty,
                    partOOptimisationUnitRow.Maximum,
                    partOOptimisationUnitRow.Headroom,
                    partOOptimisationUnitRow.Product,
                    partOOptimisationUnitRow.Equipment));
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(textBox_Diagnostics.Text);

            try
            {
                Clipboard.SetText(stringBuilder.ToString());
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                //Another process can hold the clipboard open; the text is still on screen.
            }
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
