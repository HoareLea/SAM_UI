// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

#nullable enable

using SAM.Core.Reporting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Evidence for the SAM progress-dialog pattern (documentation/ProgressDialogPattern.md): renders the Part O
    /// reference window, every state of the Space report batch window, and the Print Room Data Sheets window, to PNG.
    /// Env-gated: set <c>SAM_PROGRESS_EVIDENCE_OUT</c> to a folder (optional <c>SAM_PROGRESS_EVIDENCE_MODEL</c>, a .sam
    /// file; otherwise a small synthetic model). The batch states are produced by the real window running the real
    /// batch; only the PDF renderer is a stand-in, gated so a mid-run state can be captured.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ProgressDialogPatternEvidenceHarness
    {
        [WpfFact]
        public async Task RenderEveryState()
        {
            string? directory = Environment.GetEnvironmentVariable("SAM_PROGRESS_EVIDENCE_OUT");
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            string directory_Pdf = Path.Combine(directory, "pdf");
            if (Directory.Exists(directory_Pdf))
            {
                Directory.Delete(directory_Pdf, true);
            }

            string? path_Model = Environment.GetEnvironmentVariable("SAM_PROGRESS_EVIDENCE_MODEL");
            AnalyticalModel analyticalModel = string.IsNullOrWhiteSpace(path_Model)
                ? SpaceReportPdfBatchTests.Model(out _, "Studio 1.01", "Bedroom 1.02", "Bathroom 1.03", "Kitchen 1.04", "Living 1.05", "Hall 1.06")
                : Core.Convert.ToSAM<AnalyticalModel>(path_Model).Find(x => x is not null)!;

            //Part O: the reference implementation, as it looks mid-run.
            DateTime now = DateTime.UtcNow;
            PartOProgressState partO = new(["Prepare and review the iteration", "TAS simulation (full year)", "TM59 assessment"], () => now);
            partO.Start(0);
            now = now.AddSeconds(6);
            partO.Start(1);
            now = now.AddSeconds(20);
            partO.Detail = "T3D to TBD -> Shading";
            Snap(new PartOProgressWindow { Title = "Part O", Heading = "Prepare & Run — Iteration 2 — MVHR with manufacturer unit", Cancellable = true, State = partO }, directory, "01-parto-reference-running");

            //Print Room Data Sheets: the window the ribbon command shows - no percentage, no Cancel.
            now = DateTime.UtcNow;
            PartOProgressState rds = new(Modify.PrintRoomDataSheetsStageNames, () => now);
            rds.Start(0);
            now = now.AddSeconds(2);
            rds.Start(1);
            now = now.AddSeconds(38);
            rds.Start(2);
            now = now.AddSeconds(11);
            Snap(Modify.PrintRoomDataSheetsWindow(rds, 4995, @"C:\Projects\Bridge"), directory, "02-rds-printing");

            //The batch window: form, preparing, writing, cancel requested, cancelled.
            GateRenderer gate = new(3);
            SpaceReportPdfBatchWindow window = new(analyticalModel, null, directory_Pdf, new SpaceReportPdfPrompts(), gate);
            try
            {
                Show(window);
                Snap(window, directory, "10-batch-form", false);

                window.checkBox_SpaceAssumptions.IsChecked = true;
                window.checkBox_SpaceDesignLoadSummary.IsChecked = true;

                Task export = window.ExportAsync();
                Snap(window, directory, "11-batch-preparing", false);

                await WaitAsync(() => gate.Reached);
                await Task.Delay(800);
                Snap(window, directory, "12-batch-writing", false);

                window.RequestCancel();
                Snap(window, directory, "13-batch-cancel-requested", false);

                gate.Release();
                await export;
                Snap(window, directory, "14-batch-cancelled", false);
            }
            finally
            {
                window.Close();
            }

            //Completed: all of it again, overwriting (the question is answered Yes).
            SpaceReportPdfPrompts yes = new SpaceReportPdfPrompts() { ShowMessage = (_, _, _, _) => MessageBoxResult.Yes };

            window = new(analyticalModel, null, directory_Pdf, yes, new SpaceReportPdfBatchTests.CountingRenderer());
            try
            {
                Show(window);
                window.checkBox_SpaceAssumptions.IsChecked = true;
                window.checkBox_SpaceDesignLoadSummary.IsChecked = true;
                await window.ExportAsync();
                Snap(window, directory, "15-batch-completed", false);
            }
            finally
            {
                window.Close();
            }

            //Completed with failures: the batch carries on past a failed document; the summary lists it.
            window = new(analyticalModel, null, directory_Pdf, yes, new SpaceReportPdfBatchTests.CountingRenderer(2));
            try
            {
                Show(window);
                window.checkBox_SpaceAssumptions.IsChecked = true;
                await window.ExportAsync();
                Snap(window, directory, "16-batch-completed-with-failures", false);
            }
            finally
            {
                window.Close();
            }
        }

        private static async Task WaitAsync(Func<bool> condition)
        {
            for (int i = 0; i < 200 && !condition(); i++)
            {
                await Task.Delay(50);
            }

            Assert.True(condition());
        }

        private static void Show(System.Windows.Window window)
        {
            window.ShowActivated = false;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = -32000;
            window.Top = -32000;
            window.Show();
        }

        private static void Snap(System.Windows.Window window, string directory, string name, bool show = true)
        {
            if (show)
            {
                Show(window);
            }

            window.UpdateLayout();

            FrameworkElement content = (FrameworkElement)window.Content;
            int width = (int)Math.Ceiling(content.ActualWidth + content.Margin.Left + content.Margin.Right);
            int height = (int)Math.Ceiling(content.ActualHeight + content.Margin.Top + content.Margin.Bottom);

            DrawingVisual drawingVisual = new();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
                drawingContext.DrawRectangle(new VisualBrush(content), null, new Rect(content.Margin.Left, content.Margin.Top, content.ActualWidth, content.ActualHeight));
            }

            RenderTargetBitmap renderTargetBitmap = new(width * 2, height * 2, 192, 192, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);

            PngBitmapEncoder pngBitmapEncoder = new();
            pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            using (FileStream fileStream = File.Create(Path.Combine(directory, name + ".png")))
            {
                pngBitmapEncoder.Save(fileStream);
            }

            if (show)
            {
                window.Close();
            }
        }

        /// <summary>A stand-in renderer that holds document <c>n</c> until released, so a mid-run state can be captured.</summary>
        private sealed class GateRenderer(int n) : IDocumentRenderer
        {
            private readonly ManualResetEventSlim release = new(false);
            private int count;

            public bool Reached { get; private set; }

            public string FileExtension => ".pdf";

            public void Release() => release.Set();

            public void Render(Document document, Stream stream)
            {
                if (++count == n)
                {
                    Reached = true;
                    release.Wait(TimeSpan.FromSeconds(30));
                }

                stream.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-stub"));
            }
        }
    }
}
