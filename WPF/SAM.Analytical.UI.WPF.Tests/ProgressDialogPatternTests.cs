// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

#nullable enable

using SAM.Core.Reporting;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using TextBlock = System.Windows.Controls.TextBlock;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The SAM progress-dialog pattern (documentation/ProgressDialogPattern.md): the look extracted from
    /// <see cref="PartOProgressWindow"/> into SAM.Core.UI.WPF Themes/ProgressStyles.xaml and
    /// <see cref="ProgressStageRow"/>, and the Space report batch window drawn with it. The batch's own behaviour is
    /// pinned by <see cref="SpaceReportPdfBatchWindowTests"/>; these tests pin only how its progress is shown.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ProgressDialogPatternTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_UI_ProgressDialogPatternTests_" + Guid.NewGuid().ToString("N"));

        public ProgressDialogPatternTests()
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

        [Fact]
        public void StageRows_CarryTheGlyph_TheStatusInWords_AndATimeOnlyForAStageThatRan()
        {
            DateTime now = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
            PartOProgressState partOProgressState = new(["One", "Two", "Three", "Four"], () => now);

            partOProgressState.Start(0);
            now = now.AddSeconds(42);
            partOProgressState.Start(2);
            now = now.AddSeconds(5);

            List<ProgressStageRow> rows = Create.ProgressStageRows(partOProgressState);

            Assert.Equal(["✓", "–", "●", "○"], rows.Select(x => x.Glyph));
            Assert.Equal(["42s", "", "5s", ""], rows.Select(x => x.Duration));
            Assert.Equal(["Completed: One", "Not needed: Two", "Running now: Three", "Upcoming: Four"], rows.Select(x => x.AccessibleName));
            Assert.Equal("Running now: Three", rows[2].ToString());

            Assert.Same(Brushes.SeaGreen, rows[0].Foreground);
            Assert.Same(Brushes.RoyalBlue, rows[2].Foreground);
            Assert.Same(Brushes.Gray, rows[3].Foreground);
            Assert.Equal(FontWeights.SemiBold, rows[2].FontWeight);
            Assert.Equal(FontWeights.Normal, rows[0].FontWeight);

            partOProgressState.Fail();
            ProgressStageRow failed = Create.ProgressStageRows(partOProgressState)[2];
            Assert.Equal("✕", failed.Glyph);
            Assert.Same(Brushes.Firebrick, failed.Foreground);
            Assert.StartsWith("Did not complete: Three", failed.AccessibleName);

            Assert.Empty(Create.ProgressStageRows(null!));
        }

        [WpfFact]
        public void ThePartOWindow_IsDrawnWithTheSharedStyles()
        {
            PartOProgressState partOProgressState = new(["Prepare", "Simulate"]);
            partOProgressState.Start(0);

            PartOProgressWindow partOProgressWindow = new() { Heading = "Heading", State = partOProgressState };

            try
            {
                AssertSharedStyles(partOProgressWindow);

                ItemsControl itemsControl = (ItemsControl)partOProgressWindow.FindName("itemsControl_Stages");
                Assert.All(itemsControl.Items.Cast<object>(), x => Assert.IsType<ProgressStageRow>(x));
                Assert.Equal("Running now: Prepare", itemsControl.Items[0].ToString());
            }
            finally
            {
                partOProgressWindow.Close();
            }
        }

        [WpfFact]
        public async System.Threading.Tasks.Task TheBatchWindow_EndsOnTheCompletedState_WithTheStagesTimesAndAFullBar()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out List<Space> spaces, "A", "B", "C");

            SpaceReportPdfBatchWindow window = new SpaceReportPdfBatchWindow(analyticalModel, [spaces[0], spaces[2]], directory, new SpaceReportPdfPrompts(), new SpaceReportPdfBatchTests.CountingRenderer());
            try
            {
                AssertSharedStyles(window);
                Assert.Equal(Visibility.Collapsed, window.stackPanel_Progress.Visibility);

                window.checkBox_SpaceAssumptions.IsChecked = true;
                window.checkBox_SpaceDesignLoadSummary.IsChecked = true;

                await window.ExportAsync();

                Assert.Equal(4, window.Result!.Created);
                Assert.Equal(Visibility.Visible, window.stackPanel_Progress.Visibility);
                Assert.Equal(Visibility.Visible, window.stackPanel_Summary.Visibility);
                Assert.Equal("Space reports exported", window.textBlock_Heading.Text);
                Assert.Equal("2 Spaces × 2 reports (Space Assumptions, Space Design Load Summary) = 4 PDFs", window.textBlock_Subheading.Text);

                List<ProgressStageRow> rows = window.itemsControl_Stages.Items.Cast<ProgressStageRow>().ToList();
                Assert.Equal(["✓", "✓"], rows.Select(x => x.Glyph));
                Assert.Equal("Completed: Prepare the model snapshot and plan the PDFs", rows[0].AccessibleName);
                Assert.Equal("Completed: Write the PDFs and the log · 4 of 4", rows[1].AccessibleName);
                Assert.All(rows, x => Assert.False(string.IsNullOrEmpty(x.Duration)));

                Assert.False(window.progressBar.IsIndeterminate);
                Assert.Equal(1, window.progressBar.Value);
                Assert.Equal("100%", window.textBlock_Percent.Text);
                Assert.StartsWith("Elapsed ", window.textBlock_Elapsed.Text);

                Assert.Equal(Visibility.Visible, window.textBlock_Note.Visibility);
                Assert.Equal(Visibility.Collapsed, window.textBlock_Hint.Visibility);
                Assert.Equal("The log in the output folder lists every document.", window.textBlock_Note.Text);

                //A hint about the form takes the line back.
                window.checkBox_SpaceAssumptions.IsChecked = false;
                window.checkBox_SpaceDesignLoadSummary.IsChecked = false;
                Assert.Equal(Visibility.Visible, window.textBlock_Hint.Visibility);
                Assert.Equal(Visibility.Collapsed, window.textBlock_Note.Visibility);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public async System.Threading.Tasks.Task TheBatchWindow_Cancelled_ShowsTheStageThatDidNotComplete()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out _, "A", "B", "C", "D");

            SpaceReportPdfBatchWindow? window = null;
            List<string> notes = [];
            CancellingRenderer renderer = new CancellingRenderer(() => window!.Dispatcher.Invoke(() =>
            {
                window!.RequestCancel();
                notes.Add(window.textBlock_Note.Text);
            }));

            window = new SpaceReportPdfBatchWindow(analyticalModel, null, directory, new SpaceReportPdfPrompts(), renderer);
            try
            {
                window.checkBox_SpaceAssumptions.IsChecked = true;

                await window.ExportAsync();

                Assert.True(window.Result!.Cancelled);
                Assert.StartsWith("Cancel requested.", Assert.Single(notes));

                Assert.Equal("Export cancelled", window.textBlock_Heading.Text);
                List<ProgressStageRow> rows = window.itemsControl_Stages.Items.Cast<ProgressStageRow>().ToList();
                Assert.Equal(["✓", "✕"], rows.Select(x => x.Glyph));
                Assert.Equal(string.Format("Did not complete: Write the PDFs and the log · stopped after {0} of 4", window.Result.Items.Count), rows[1].AccessibleName);

                Assert.False(window.progressBar.IsIndeterminate);
                Assert.Equal((double)window.Result.Items.Count / 4, window.progressBar.Value, 6);
                Assert.StartsWith("Cancelled between documents", window.textBlock_Note.Text);
                Assert.StartsWith("Cancelled after", window.textBlock_Summary.Text);
                Assert.Equal("Close", window.button_Cancel.Content);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public async System.Threading.Tasks.Task TheBatchWindow_ExistingPdfQuestionCancelled_GoesBackToTheForm()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out _, "A");
            File.WriteAllText(Path.Combine(directory, "A - Space Assumptions.pdf"), "previous");

            SpaceReportPdfPrompts prompts = new SpaceReportPdfPrompts() { ShowMessage = (_, _, _, _) => MessageBoxResult.Cancel };
            SpaceReportPdfBatchWindow window = new SpaceReportPdfBatchWindow(analyticalModel, null, directory, prompts, new SpaceReportPdfBatchTests.CountingRenderer());
            try
            {
                window.checkBox_SpaceAssumptions.IsChecked = true;

                await window.ExportAsync();

                Assert.Null(window.Result);
                Assert.Equal(Visibility.Collapsed, window.stackPanel_Progress.Visibility);
                Assert.Equal(Visibility.Collapsed, window.textBlock_Note.Visibility);
                Assert.Equal("Cancel", window.button_Cancel.Content);
                Assert.True(window.button_Export.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        }

        [Fact]
        public void TheBatchNote_SaysWhenCancelTakesEffect_AndWhyThereIsNoPercentageYet()
        {
            string preparing = SpaceReportPdfBatchWindow.Note(true, false);
            Assert.StartsWith("No percentage is shown while the model snapshot is prepared", preparing);
            Assert.Contains("before the first PDF", preparing);

            string writing = SpaceReportPdfBatchWindow.Note(false, false);
            Assert.DoesNotContain("No percentage", writing);
            Assert.StartsWith("Cancel takes effect at the next safe point, between documents", writing);

            Assert.StartsWith("Cancel requested.", SpaceReportPdfBatchWindow.Note(false, true));

            Assert.Equal("1 Space × 1 report (Space Assumptions)", SpaceReportPdfBatchWindow.RunSummary(1, [SpaceReportPdf.SpaceAssumptions], null));
            Assert.Equal("1 Space × 1 report (Space Assumptions) = 1 PDF", SpaceReportPdfBatchWindow.RunSummary(1, [SpaceReportPdf.SpaceAssumptions], 1));
        }

        [Fact]
        public void PrintRoomDataSheets_NamesOneStageInItsWindowPerStageItAnnounces()
        {
            Assert.Equal(UI.Modify.PrintRoomDataSheetsStages.Length, Modify.PrintRoomDataSheetsStageNames.Count);

            //No model: nothing runs and no window opens.
            Modify.PrintRoomDataSheetsWithProgress(null);
        }

        [WpfFact]
        public void ThePrintRdsWindow_KeepsTheReplacedWindowsBehaviour_InTheSharedStyle()
        {
            PartOProgressState partOProgressState = new(Modify.PrintRoomDataSheetsStageNames);
            partOProgressState.Start(0);
            partOProgressState.Start(1);

            PartOProgressWindow window = Modify.PrintRoomDataSheetsWindow(partOProgressState, 3, @"C:\Projects\Bridge");
            try
            {
                //As the replaced SAM "Print RDS" ProgressWindow: no owner, not topmost, not in the taskbar, centred.
                Assert.Equal("Print RDS", window.Title);
                Assert.False(window.Topmost);
                Assert.False(window.ShowInTaskbar);
                Assert.Null(window.Owner);
                Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);

                //The shared style, with no percentage and no Cancel - the work reports none and cannot stop.
                AssertSharedStyles(window);
                Assert.Equal("Print Room Data Sheets", window.Heading);
                Assert.Equal(@"3 Spaces → C:\Projects\Bridge", window.Subheading);
                Assert.Equal(Visibility.Collapsed, ((Button)window.FindName("button_Cancel")).Visibility);
                Assert.True(((ProgressBar)window.FindName("progressBar")).IsIndeterminate);
                Assert.Equal("No percentage is shown: this step does not report one. It cannot be cancelled.", ((TextBlock)window.FindName("textBlock_Note")).Text);
                Assert.Equal(["Completed: Collect the room data", "Running now: Write the data to the RDS workbook", "Upcoming: Print the room data sheets (Excel)", "Upcoming: Finish"], ((ItemsControl)window.FindName("itemsControl_Stages")).Items.Cast<ProgressStageRow>().Select(x => x.AccessibleName));
            }
            finally
            {
                window.Close();
            }
        }

        [Fact]
        public void PrintRoomDataSheets_WithAStageCallback_OpensNoWindowOfItsOwn_AndAnnouncesNothingBeforeItStarts()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out _, "A");
            List<int> stages = [];

            //A folder that does not exist ends the command before any stage, as it always has.
            UI.Modify.PrintRoomDataSheets(analyticalModel, Path.Combine(directory, "missing"), null, stages.Add);

            Assert.Empty(stages);
        }

        private static void AssertSharedStyles(System.Windows.Window window)
        {
            Assert.Same(window.FindResource("SAM.Progress.Heading"), ((TextBlock)window.FindName("textBlock_Heading")).Style);
            Assert.Same(window.FindResource("SAM.Progress.Subheading"), ((TextBlock)window.FindName("textBlock_Subheading")).Style);
            Assert.Same(window.FindResource("SAM.Progress.Stages"), ((ItemsControl)window.FindName("itemsControl_Stages")).Style);
            Assert.Same(window.FindResource("SAM.Progress.StageRowTemplate"), ((ItemsControl)window.FindName("itemsControl_Stages")).ItemTemplate);
            Assert.Same(window.FindResource("SAM.Progress.Detail"), ((TextBlock)window.FindName("textBlock_Detail")).Style);
            Assert.Same(window.FindResource("SAM.Progress.Bar"), ((ProgressBar)window.FindName("progressBar")).Style);
            Assert.Same(window.FindResource("SAM.Progress.Status"), ((TextBlock)window.FindName("textBlock_Elapsed")).Style);
            Assert.Same(window.FindResource("SAM.Progress.Status"), ((TextBlock)window.FindName("textBlock_Percent")).Style);
            Assert.Same(window.FindResource("SAM.Progress.Note"), ((TextBlock)window.FindName("textBlock_Note")).Style);
            Assert.Same(window.FindResource("SAM.Progress.CancelButton"), ((Button)window.FindName("button_Cancel")).Style);
        }

        /// <summary>Cancels (through the window, as the button does) while the first document is being written.</summary>
        private sealed class CancellingRenderer(Action cancel) : IDocumentRenderer
        {
            private int count;

            public string FileExtension => ".pdf";

            public void Render(Document document, Stream stream)
            {
                if (++count == 1)
                {
                    cancel();
                }

                stream.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-stub"));
            }
        }
    }
}
