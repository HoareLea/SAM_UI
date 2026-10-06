// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The TM59 assessment of a completed Part O run.
    /// <para>
    /// <b>Simple by default, detailed on demand.</b> It opens on the verdict - glyph and word - and the space
    /// counts, then what the result is of, then the detailed report. Every word above the report is
    /// <see cref="PartOTM59ResultSummary"/>'s, read off the same production objects the report is written from.
    /// </para>
    /// <para>
    /// <b>Presentation only, and deliberately thin.</b> The detailed report is the text
    /// <c>TM59AssessmentReport</c> produced, shown as it was given. The criteria, their limits, the hours
    /// counted and the pass/fail verdicts are the assessment's - none of them is restated, reformatted into a
    /// grid of this window's own making, or recomputed. The natural-ventilation criteria use their own summer
    /// and night subsets and the mechanical criterion its annual one; a window that laid out "exceedable
    /// hours" itself would be the place those got confused.
    /// </para>
    /// <para>
    /// <b>Spaces that were not assessed are shown, not hidden.</b> They are counted in the summary, and the
    /// reason for each is one switch away. An assessment that could not be produced at all is shown here too,
    /// as UNAVAILABLE with its reason - never as a pass or a fail.
    /// </para>
    /// </summary>
    public partial class PartOTM59ResultWindow : System.Windows.Window
    {
        private PartOTM59ResultSummary? partOTM59ResultSummary;

        private string countsDetail = string.Empty;

        public PartOTM59ResultWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            //The Hub's placement, shared: the Close row is at the bottom.
            PartOWindowPlacement.KeepOnScreen(this);
        }

        /// <summary>The summary the window is headed with.</summary>
        public PartOTM59ResultSummary? ResultSummary
        {
            get
            {
                return partOTM59ResultSummary;
            }
            set
            {
                partOTM59ResultSummary = value;

                PartOTM59ResultSummary summary = value ?? PartOTM59ResultSummary.Unavailable(null, null);

                textBlock_VerdictGlyph.Text = summary.Glyph;
                textBlock_Verdict.Text = summary.Heading;

                Brush brush = VerdictBrush(summary.Verdict);
                textBlock_VerdictGlyph.Foreground = brush;
                border_Verdict.BorderBrush = brush;

                textBlock_Counts.Text = summary.Counts ?? string.Empty;
                textBlock_Counts.Visibility = string.IsNullOrWhiteSpace(summary.Counts) ? Visibility.Collapsed : Visibility.Visible;
                textBlock_Counts.ToolTip = summary.CountsDetail;

                textBlock_Reason.Text = summary.Reason ?? string.Empty;
                textBlock_Reason.Visibility = summary.Reason is null ? Visibility.Collapsed : Visibility.Visible;

                textBlock_Caveat.Text = PartOTM59ResultSummary.Caveat;

                itemsControl_Facts.ItemsSource = summary.Facts;

                border_Verdict.Visibility = Visibility.Visible;
                border_Facts.Visibility = summary.Facts.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

                //No assessment, no report: the section is not drawn rather than shown empty.
                Visibility visibility_Report = summary.Verdict == PartOTM59Verdict.Unavailable ? Visibility.Collapsed : Visibility.Visible;
                stackPanel_ReportHeading.Visibility = visibility_Report;
                textBox_Report.Visibility = visibility_Report;

                OpenDiagnosticsWhereTheyExplain();
            }
        }

        /// <summary>The production report text - the Detailed report.</summary>
        public string Report
        {
            get
            {
                return textBox_Report.Text;
            }
            set
            {
                textBox_Report.Text = value;
            }
        }

        /// <summary>
        /// What was processed and assessed per criterion list, and where the report was saved - the counts
        /// line's tooltip and a line of Copy All.
        /// </summary>
        public string Summary
        {
            get
            {
                return countsDetail;
            }
            set
            {
                countsDetail = value ?? string.Empty;
            }
        }

        /// <summary>How many distinct not-assessed reasons the box holds.</summary>
        internal int DiagnosticsCount { get; private set; }

        /// <summary>Whether the not-assessed reasons are on screen. For a test to read.</summary>
        internal bool DiagnosticsShown => textBox_Diagnostics.Visibility == Visibility.Visible;

        /// <summary>The not-assessed reasons, as the box shows them. For a test to read.</summary>
        internal string DiagnosticsText => textBox_Diagnostics.Text;

        /// <summary>The not-assessed bar's count. For a test to read.</summary>
        internal string DiagnosticsLabel => label_Diagnostics.Text;

        /// <summary>The verdict heading as drawn. For a test to read.</summary>
        internal string VerdictHeading => textBlock_Verdict.Text;

        /// <summary>Whether the Detailed report section is drawn. For a test to read.</summary>
        internal bool ReportShown => textBox_Report.Visibility == Visibility.Visible;

        /// <summary>
        /// Why individual spaces produced no result: identities that did not resolve, and scenarios stating a
        /// strategy no criterion is known for.
        /// </summary>
        public void SetDiagnostics(IEnumerable<string> associationRefusals, IEnumerable<string> ventilationStrategyRefusals)
        {
            StringBuilder stringBuilder = new();

            //Distinct: RestoreDesignInternalConditions and Spaces are both asked for AssociationRefusals, and
            //the second call re-reports what the first already said.
            HashSet<string> descriptions = [];

            foreach (string description in associationRefusals ?? [])
            {
                if (!string.IsNullOrWhiteSpace(description) && descriptions.Add(description))
                {
                    stringBuilder.AppendLine(string.Format("NOT ASSESSED: {0}", description));
                }
            }

            foreach (string description in ventilationStrategyRefusals ?? [])
            {
                if (!string.IsNullOrWhiteSpace(description) && descriptions.Add(description))
                {
                    stringBuilder.AppendLine(string.Format("NO CRITERION: {0}", description));
                }
            }

            DiagnosticsCount = descriptions.Count;

            textBox_Diagnostics.Text = descriptions.Count == 0 ? "Every space resolved and was assessed." : stringBuilder.ToString();

            //Reasons, not spaces: one space can carry two sentences. The space count is in the summary.
            label_Diagnostics.Text = descriptions.Count == 0 ? "No reasons recorded" : UI.Query.PartOCount(descriptions.Count, "reason", "reasons");

            //Nothing to show, nothing to switch.
            checkBox_ShowDiagnostics.Visibility = descriptions.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            OpenDiagnosticsWhereTheyExplain();
        }

        /// <summary>What Copy All puts on the clipboard: the summary, the report and every reason.</summary>
        internal string CopyAllText
        {
            get
            {
                StringBuilder stringBuilder = new();

                stringBuilder.AppendLine((partOTM59ResultSummary ?? PartOTM59ResultSummary.Unavailable(null, null)).Text);

                if (!string.IsNullOrWhiteSpace(countsDetail))
                {
                    stringBuilder.AppendLine(countsDetail);
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine(textBox_Report.Text);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(textBox_Diagnostics.Text);

                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Where there is no verdict, the reasons are the explanation, so they open - whichever of the summary
        /// and the reasons was set first. Otherwise they stay one switch away.
        /// </summary>
        private void OpenDiagnosticsWhereTheyExplain()
        {
            if (partOTM59ResultSummary is not null && !partOTM59ResultSummary.HasVerdict && DiagnosticsCount != 0)
            {
                checkBox_ShowDiagnostics.IsChecked = true;
            }

            UpdateDiagnosticsVisibility();
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

        private void UpdateDiagnosticsVisibility()
        {
            textBox_Diagnostics.Visibility = checkBox_ShowDiagnostics.Visibility == Visibility.Visible && checkBox_ShowDiagnostics.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void checkBox_ShowDiagnostics_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDiagnosticsVisibility();
        }

        private void button_CopyAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(CopyAllText);
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
