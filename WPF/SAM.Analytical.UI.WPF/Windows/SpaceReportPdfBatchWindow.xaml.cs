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

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// "Export Space reports...": report types, scope (selected / all Spaces) and an output folder, then
    /// <see cref="SpaceReportPdfBatch"/> on a background task with progress and Cancel. Existing PDFs are asked about
    /// once per batch. The window is modal, so the model cannot change under the batch's snapshot.
    /// </summary>
    public partial class SpaceReportPdfBatchWindow : System.Windows.Window
    {
        private const int FailuresShown = 3;

        private readonly AnalyticalModel? analyticalModel;
        private readonly List<Guid> selectedSpaceGuids;
        private readonly SpaceReportPdfPrompts spaceReportPdfPrompts;
        private readonly IDocumentRenderer? documentRenderer;

        private CancellationTokenSource? cancellationTokenSource;
        private bool running;
        private bool closeWhenFinished;

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
            int spaceCount = analyticalModel?.GetSpaces()?.Count ?? 0;

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
            ShowProgress("Preparing the model snapshot...", null, null);

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
                    spaceReportPdfPrompts.ShowMessage(string.Format("The Space reports could not be exported: {0}", exception.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                SpaceReportPdfExistingFiles? existingFiles = ExistingFiles(existingCount, spaceReportPdfBatch.DocumentCount);
                if (existingFiles == null)
                {
                    return;
                }

                ShowProgress(string.Format(CultureInfo.CurrentCulture, "Starting {0:N0} documents...", spaceReportPdfBatch.DocumentCount), null, 0);

                Progress<SpaceReportPdfBatchProgress> progress = new Progress<SpaceReportPdfBatchProgress>(x =>
                {
                    if (running)
                    {
                        ShowProgress(
                            string.Format(CultureInfo.CurrentCulture, "Space {0:N0} / {1:N0} - {2}", x.SpaceIndex, x.SpaceCount, x.SpaceReportPdf.Name),
                            x.SpaceName,
                            x.DocumentCount == 0 ? 0 : (double)x.DocumentIndex / x.DocumentCount);
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
                    spaceReportPdfPrompts.ShowMessage(string.Format("The Space report export stopped: {0}", exception.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Result = spaceReportPdfBatchResult;
                ShowSummary(spaceReportPdfBatchResult, spaceReportPdfBatch.Directory);
            }
            finally
            {
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

        private void ShowProgress(string text, string? detail, double? fraction)
        {
            stackPanel_Progress.Visibility = Visibility.Visible;
            textBlock_Progress.Text = text;
            textBlock_ProgressDetail.Text = detail ?? string.Empty;
            progressBar.IsIndeterminate = !fraction.HasValue;
            progressBar.Value = fraction ?? 0;
        }

        private void ShowSummary(SpaceReportPdfBatchResult result, string directory)
        {
            stackPanel_Progress.Visibility = Visibility.Collapsed;
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
            else
            {
                stackPanel_Progress.Visibility = Visibility.Collapsed;
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

            button_Cancel.IsEnabled = false;
            button_Cancel.Content = "Cancelling...";
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
