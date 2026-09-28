// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// "Export Space reports...": report types, scope (selected / all Spaces) and an output folder, then
    /// <see cref="SpaceReportPdfBatch"/> on a background task with progress and Cancel. Existing PDFs are asked about
    /// once per batch. The window is modal, so the model cannot change under the batch's snapshot.
    /// <para>
    /// The progress follows the SAM progress-dialog pattern (documentation/ProgressDialogPattern.md), whose reference
    /// is <see cref="PartOProgressWindow"/>: the stages are a <see cref="PartOProgressState"/>, drawn by
    /// <see cref="Create.ProgressStageRows"/> and <c>Themes/ProgressStyles.xaml</c>. The batch reports a real count,
    /// so the bar is determinate while PDFs are written; the preparation reports none, so it is not.
    /// </para>
    /// </summary>
    public partial class SpaceReportPdfBatchWindow : System.Windows.Window
    {
        private const int FailuresShown = 3;

        /// <summary>The stages of one export, in order.</summary>
        internal static readonly IReadOnlyList<string> StageNames = ["Prepare the model snapshot and plan the PDFs", "Write the PDFs and the log"];

        private readonly AnalyticalModel? analyticalModel;
        private readonly List<Guid> selectedSpaceGuids;
        private readonly SpaceReportPdfPrompts spaceReportPdfPrompts;
        private readonly IDocumentRenderer? documentRenderer;
        private readonly int spaceCount;
        private readonly DispatcherTimer dispatcherTimer;

        private CancellationTokenSource? cancellationTokenSource;
        private bool running;
        private bool closeWhenFinished;

        private PartOProgressState? progressState;
        private double? progressFraction;
        private bool cancelRequested;
        private string? finalNote;

        public SpaceReportPdfBatchWindow()
            : this(null, null, null)
        {
        }

        /// <param name="selectedSpaces">The current selection; the scope defaults to it when it has Spaces.</param>
        /// <param name="directory">The suggested output folder; may not exist yet (the batch creates it).</param>
        /// <param name="spaceReportPdfPrompts">Null means real dialogs owned by this window; tests pass stand-ins.</param>
        internal SpaceReportPdfBatchWindow(AnalyticalModel? analyticalModel, IEnumerable<Space>? selectedSpaces, string? directory, SpaceReportPdfPrompts? spaceReportPdfPrompts = null, IDocumentRenderer? documentRenderer = null)
        {
            InitializeComponent();

            this.analyticalModel = analyticalModel;
            this.documentRenderer = documentRenderer;
            this.spaceReportPdfPrompts = spaceReportPdfPrompts ?? SpaceReportPdfPrompts.Dialogs(this);

            selectedSpaceGuids = selectedSpaces?.Where(x => x != null).Select(x => x.Guid).Distinct().ToList() ?? new List<Guid>();

            //Counted once, never listed: GetSpaces copies the Spaces only, not the adjacency cluster.
            spaceCount = analyticalModel?.GetSpaces()?.Count ?? 0;

            radioButton_Selected.Content = string.Format(CultureInfo.CurrentCulture, "Selected Spaces ({0:N0})", selectedSpaceGuids.Count);
            radioButton_Selected.IsEnabled = selectedSpaceGuids.Count > 0;
            radioButton_Selected.ToolTip = selectedSpaceGuids.Count > 0 ? null : "Select Spaces in the view or the model tree first.";
            radioButton_All.Content = string.Format(CultureInfo.CurrentCulture, "All Spaces ({0:N0})", spaceCount);

            if (selectedSpaceGuids.Count > 0)
            {
                radioButton_Selected.IsChecked = true;
            }
            else
            {
                radioButton_All.IsChecked = true;
            }

            textBox_Folder.Text = directory ?? string.Empty;

            Closing += SpaceReportPdfBatchWindow_Closing;

            //Keeps the elapsed time and the running stage's time moving between documents, as the Part O window does.
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500),
            };

            dispatcherTimer.Tick += (s, e) => RenderProgress();
            Closed += (s, e) => dispatcherTimer.Stop();

            UpdateState();
        }

        /// <summary>The last batch's result; null until one has run.</summary>
        internal SpaceReportPdfBatchResult? Result { get; private set; }

        internal IReadOnlyList<SpaceReportPdf> SelectedSpaceReportPdfs
        {
            get
            {
                List<SpaceReportPdf> result = new List<SpaceReportPdf>();
                if (checkBox_SpaceAssumptions.IsChecked == true)
                {
                    result.Add(SpaceReportPdf.SpaceAssumptions);
                }

                if (checkBox_SpaceDesignLoadSummary.IsChecked == true)
                {
                    result.Add(SpaceReportPdf.SpaceDesignLoadSummary);
                }

                return result;
            }
        }

        /// <summary>
        /// The Export button: plan on a background task, ask once about existing PDFs, then write the batch with
        /// progress. Returns when the batch (or the refusal) is done.
        /// </summary>
        internal async Task ExportAsync()
        {
            if (running)
            {
                return;
            }

            IReadOnlyList<SpaceReportPdf> spaceReportPdfs = SelectedSpaceReportPdfs;
            string directory = textBox_Folder.Text;
            if (analyticalModel == null || spaceReportPdfs.Count == 0 || string.IsNullOrWhiteSpace(directory))
            {
                UpdateState();
                return;
            }

            List<Guid>? spaceGuids = radioButton_All.IsChecked == true ? null : selectedSpaceGuids;

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            SetRunning(true);
            BeginProgress(RunSummary(spaceGuids?.Count ?? spaceCount, spaceReportPdfs, null));

            //True once the window shows how this export ended; otherwise the progress goes and the form is as it was.
            bool final = false;

            try
            {
                SpaceReportPdfBatch spaceReportPdfBatch;
                int existingCount;
                try
                {
                    AnalyticalModel analyticalModel_Snapshot = analyticalModel;
                    (spaceReportPdfBatch, existingCount) = await Task.Run(() =>
                    {
                        SpaceReportPdfBatch result = SpaceReportPdfBatch.Create(analyticalModel_Snapshot, spaceGuids, spaceReportPdfs, directory);
                        return (result, result.ExistingCount());
                    });
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError("Export Space reports could not start: {0}", exception);
                    final = EndProgress("Space reports could not be exported", null, false, exception.Message, "Nothing was written. The message gives the reason.");
                    spaceReportPdfPrompts.ShowMessage(string.Format("The Space reports could not be exported: {0}", exception.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    final = EndProgress("Export cancelled", null, false, null, "Cancelled before the first PDF: nothing was written.");
                    return;
                }

                progressState?.Complete(false);
                textBlock_Subheading.Text = RunSummary(spaceReportPdfBatch.DocumentCount / Math.Max(1, spaceReportPdfs.Count), spaceReportPdfs, spaceReportPdfBatch.DocumentCount);
                RenderProgress();

                SpaceReportPdfExistingFiles? existingFiles = ExistingFiles(existingCount, spaceReportPdfBatch.DocumentCount);
                if (existingFiles == null)
                {
                    return;
                }

                progressState?.Start(1);
                ReportProgress(0, spaceReportPdfBatch.DocumentCount, null);

                Progress<SpaceReportPdfBatchProgress> progress = new Progress<SpaceReportPdfBatchProgress>(x =>
                {
                    if (running)
                    {
                        ReportProgress(
                            x.DocumentIndex,
                            x.DocumentCount,
                            string.Format(CultureInfo.CurrentCulture, "Space {0:N0} / {1:N0} · {2} · {3}", x.SpaceIndex, x.SpaceCount, x.SpaceReportPdf.Name, x.SpaceName));
                    }
                });

                SpaceReportPdfBatchResult spaceReportPdfBatchResult;
                try
                {
                    spaceReportPdfBatchResult = await Task.Run(() => spaceReportPdfBatch.Run(existingFiles.Value, progress, cancellationToken, documentRenderer));
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError("Export Space reports failed: {0}", exception);
                    final = EndProgress("Space report export stopped", null, false, exception.Message, "The export stopped. The message gives the reason.");
                    spaceReportPdfPrompts.ShowMessage(string.Format("The Space report export stopped: {0}", exception.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Result = spaceReportPdfBatchResult;
                final = EndProgress(spaceReportPdfBatchResult);
                ShowSummary(spaceReportPdfBatchResult, spaceReportPdfBatch.Directory);
            }
            finally
            {
                dispatcherTimer.Stop();

                if (!final)
                {
                    //Nothing was started (the existing-PDF question was cancelled): back to the form, as before.
                    progressState = null;
                    stackPanel_Progress.Visibility = Visibility.Collapsed;
                }

                SetRunning(false);

                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;

                if (closeWhenFinished)
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Asked once, only when some planned PDFs already exist; otherwise Skip (so a PDF that appears mid-run is
        /// never silently overwritten). Null: the user cancelled the export.
        /// </summary>
        private SpaceReportPdfExistingFiles? ExistingFiles(int existingCount, int documentCount)
        {
            if (existingCount <= 0)
            {
                return SpaceReportPdfExistingFiles.Skip;
            }

            string text = string.Format(
                CultureInfo.CurrentCulture,
                "{0:N0} of the {1:N0} PDFs are already in the output folder.\n\nYes: overwrite them.\nNo: skip them and keep the existing PDFs.\nCancel: do not export.",
                existingCount,
                documentCount);

            return spaceReportPdfPrompts.ShowMessage(text, Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) switch
            {
                MessageBoxResult.Yes => SpaceReportPdfExistingFiles.Overwrite,
                MessageBoxResult.No => SpaceReportPdfExistingFiles.Skip,
                _ => null,
            };
        }

        private void BeginProgress(string runSummary)
        {
            progressState = new PartOProgressState(StageNames);
            progressState.Start(0);
            progressFraction = null;
            cancelRequested = false;
            finalNote = null;

            textBlock_Heading.Text = "Exporting Space reports";
            textBlock_Subheading.Text = runSummary;
            stackPanel_Progress.Visibility = Visibility.Visible;

            RenderProgress();
            dispatcherTimer.Start();
        }

        /// <summary>A real count: <paramref name="completed"/> documents finished of <paramref name="total"/>.</summary>
        private void ReportProgress(int completed, int total, string? detail)
        {
            if (progressState == null)
            {
                return;
            }

            progressState.Activity(string.Format(CultureInfo.CurrentCulture, "{0:N0} of {1:N0}", completed, total));
            progressState.Report(completed, total);
            progressState.Detail = detail;
            progressFraction = progressState.Fraction;

            RenderProgress();
        }

        /// <summary>How a batch that ran ended: finished (with or without failures) or cancelled between documents.</summary>
        private bool EndProgress(SpaceReportPdfBatchResult result)
        {
            if (result.Cancelled)
            {
                //The bar ends at what the result counts, not at the last progress tick (sent before its document).
                progressFraction = result.DocumentCount == 0 ? 0 : (double)result.Items.Count / result.DocumentCount;

                return EndProgress(
                    "Export cancelled",
                    string.Format(CultureInfo.CurrentCulture, "stopped after {0:N0} of {1:N0}", result.Items.Count, result.DocumentCount),
                    false,
                    null,
                    "Cancelled between documents: the PDFs already written are kept, and the log lists every document.");
            }

            return EndProgress(
                result.Failed > 0 ? "Space reports exported, with failures" : "Space reports exported",
                string.Format(CultureInfo.CurrentCulture, "{0:N0} of {0:N0}", result.DocumentCount),
                true,
                null,
                "The log in the output folder lists every document.");
        }

        /// <summary>The final state: the running stage completed or failed, the clock stopped, the bar where it reached.</summary>
        private bool EndProgress(string heading, string? activity, bool completed, string? detail, string note)
        {
            if (progressState == null)
            {
                return false;
            }

            if (activity != null)
            {
                progressState.Activity(activity);
            }

            if (completed)
            {
                progressState.Complete();
                progressFraction = 1;
            }
            else
            {
                if (detail is null)
                {
                    progressState.Fail();
                }
                else
                {
                    progressState.Fail(detail);
                }

                progressState.SkipUnstarted();
            }

            textBlock_Heading.Text = heading;
            finalNote = note;

            RenderProgress();

            return true;
        }

        /// <summary>Re-reads the stage state, as <see cref="PartOProgressWindow.Render"/> does. Internal for tests.</summary>
        internal void RenderProgress()
        {
            if (progressState == null)
            {
                UpdateFooter();
                return;
            }

            itemsControl_Stages.ItemsSource = Create.ProgressStageRows(progressState);

            string? detail = progressState.Detail;
            textBlock_Detail.Text = detail ?? string.Empty;
            textBlock_Detail.Visibility = string.IsNullOrWhiteSpace(detail) ? Visibility.Collapsed : Visibility.Visible;

            //Determinate only on a real count. Once the export has ended the bar stays where it reached.
            double? fraction = progressState.IsFinished ? progressFraction : progressState.Fraction;
            progressBar.IsIndeterminate = !progressState.IsFinished && !fraction.HasValue;
            progressBar.Value = fraction ?? 0;

            textBlock_Percent.Text = fraction.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0}%", (int)Math.Floor(fraction.Value * 100 + 1e-9)) : string.Empty;
            textBlock_Percent.Visibility = fraction.HasValue ? Visibility.Visible : Visibility.Collapsed;

            textBlock_Elapsed.Text = progressState.ElapsedText;

            textBlock_Note.Text = progressState.IsFinished ? finalNote ?? string.Empty : Note(progressState.Status(0) == PartOProgressStageStatus.Running, cancelRequested);

            UpdateFooter();
        }

        /// <summary>
        /// The line beside Cancel while an export runs: whether there is a percentage, and when Cancel takes effect -
        /// the batch observes it between documents only.
        /// </summary>
        /// <param name="preparing">The model snapshot is being prepared (no count yet).</param>
        /// <param name="cancelRequested">Cancel has been pressed and the export has not yet stopped.</param>
        internal static string Note(bool preparing, bool cancelRequested)
        {
            if (cancelRequested)
            {
                return "Cancel requested. The PDF being written finishes and is kept; the export stops before the next one and still writes the log.";
            }

            return preparing
                ? "No percentage is shown while the model snapshot is prepared: it does not report one. Cancel takes effect before the first PDF is written."
                : "Cancel takes effect at the next safe point, between documents: the PDF being written finishes and is kept, and the log is still written.";
        }

        /// <summary>"3 Spaces × 2 reports (Space Assumptions, Space Design Load Summary) = 6 PDFs": what this export covers.</summary>
        internal static string RunSummary(int spaceCount, IReadOnlyList<SpaceReportPdf> spaceReportPdfs, int? documentCount)
        {
            string result = string.Format(
                CultureInfo.CurrentCulture,
                "{0:N0} {1} × {2} {3} ({4})",
                spaceCount,
                spaceCount == 1 ? "Space" : "Spaces",
                spaceReportPdfs.Count,
                spaceReportPdfs.Count == 1 ? "report" : "reports",
                string.Join(", ", spaceReportPdfs.Select(x => x.Name)));

            return documentCount.HasValue
                ? string.Format(CultureInfo.CurrentCulture, "{0} = {1:N0} {2}", result, documentCount.Value, documentCount.Value == 1 ? "PDF" : "PDFs")
                : result;
        }

        /// <summary>A hint about the form wins; otherwise the progress note, where there is one.</summary>
        private void UpdateFooter()
        {
            bool hint = !string.IsNullOrEmpty(textBlock_Hint.Text);
            bool note = !hint && stackPanel_Progress.Visibility == Visibility.Visible && !string.IsNullOrEmpty(textBlock_Note.Text);

            textBlock_Hint.Visibility = note ? Visibility.Collapsed : Visibility.Visible;
            textBlock_Note.Visibility = note ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowSummary(SpaceReportPdfBatchResult result, string directory)
        {
            stackPanel_Summary.Visibility = Visibility.Visible;

            textBlock_Summary.Text = Summary(result);
            button_OpenFolder.Tag = directory;
            button_OpenLog.Tag = result.LogPath;
            button_OpenLog.IsEnabled = result.LogPath != null;
        }

        /// <summary>Totals, and the first few failures; the log has every document.</summary>
        internal static string Summary(SpaceReportPdfBatchResult result)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (result.Cancelled)
            {
                stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "Cancelled after {0:N0} of {1:N0} documents.", result.Items.Count, result.DocumentCount).AppendLine();
            }
            else
            {
                stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "Finished {0:N0} documents in {1}.", result.DocumentCount, result.Elapsed.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)).AppendLine();
            }

            stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "Created: {0:N0}   Skipped: {1:N0}   Failed: {2:N0}", result.Created, result.Skipped, result.Failed);
            if (result.NotStarted > 0)
            {
                stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "   Not started: {0:N0}", result.NotStarted);
            }

            List<SpaceReportPdfBatchItem> failures = result.Items.Where(x => x.Status == SpaceReportPdfBatchStatus.Failed).ToList();
            foreach (SpaceReportPdfBatchItem item in failures.Take(FailuresShown))
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendFormat("{0} - {1}: {2}", string.IsNullOrWhiteSpace(item.SpaceName) ? item.SpaceGuid.ToString("D") : item.SpaceName, item.SpaceReportPdf.Name, item.Message);
            }

            if (failures.Count > FailuresShown)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "...and {0:N0} more failures: see the log.", failures.Count - FailuresShown);
            }

            if (result.LogPath == null)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendFormat("The log could not be written: {0}", result.LogError);
            }

            return stringBuilder.ToString();
        }

        private void SetRunning(bool value)
        {
            running = value;

            groupBox_Reports.IsEnabled = !value;
            groupBox_Spaces.IsEnabled = !value;
            groupBox_Folder.IsEnabled = !value;

            if (value)
            {
                stackPanel_Summary.Visibility = Visibility.Collapsed;
            }

            button_Cancel.IsEnabled = true;
            button_Cancel.Content = value ? "Cancel" : (Result == null ? "Cancel" : "Close");

            UpdateState();
        }

        private void UpdateState()
        {
            if (running)
            {
                button_Export.IsEnabled = false;
                textBlock_Hint.Text = string.Empty;
                UpdateFooter();
                return;
            }

            string? hint = null;
            if (analyticalModel == null)
            {
                hint = "Open an analytical model first.";
            }
            else if (SelectedSpaceReportPdfs.Count == 0)
            {
                hint = "Choose at least one report.";
            }
            else if (string.IsNullOrWhiteSpace(textBox_Folder.Text))
            {
                hint = "Choose an output folder.";
            }

            button_Export.IsEnabled = hint == null;
            textBlock_Hint.Text = hint ?? string.Empty;
            UpdateFooter();
        }

        private void Options_Changed(object sender, RoutedEventArgs e)
        {
            //Raised by InitializeComponent before every control exists.
            if (button_Export != null && textBox_Folder != null)
            {
                UpdateState();
            }
        }

        private void button_Browse_Click(object sender, RoutedEventArgs e)
        {
            string? directory = spaceReportPdfPrompts.ChooseFolder("Choose the output folder", textBox_Folder.Text);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                textBox_Folder.Text = directory;
            }

            UpdateState();
        }

        private async void button_Export_Click(object sender, RoutedEventArgs e)
        {
            await ExportAsync();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                RequestCancel();
                return;
            }

            Close();
        }

        /// <summary>Stops the batch before its next document; the document in hand finishes and is kept.</summary>
        internal void RequestCancel()
        {
            if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            cancelRequested = true;

            button_Cancel.IsEnabled = false;
            button_Cancel.Content = "Cancelling...";

            RenderProgress();
        }

        private void SpaceReportPdfBatchWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!running)
            {
                return;
            }

            //Closing mid-run cancels between documents, and the window closes once the log is written.
            e.Cancel = true;
            closeWhenFinished = true;
            RequestCancel();
        }

        private void button_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Open(button_OpenFolder.Tag as string);
        }

        private void button_OpenLog_Click(object sender, RoutedEventArgs e)
        {
            Open(button_OpenLog.Tag as string);
        }

        private void Open(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                spaceReportPdfPrompts.Open(path);
            }
            catch (Exception exception)
            {
                spaceReportPdfPrompts.ShowMessage(string.Format("'{0}' could not be opened: {1}", path, exception.Message), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
