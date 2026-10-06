// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Opt-in, and only on a machine holding saved Part O results and a TAS installation.</b> Reopens a real
    /// saved Iteration 1a run and its saved manufacturer-guidance Iteration 3 result exactly as the application
    /// does - File › Open's <c>PartORun.Restore</c>, then the Hub's eligibility and pre-flight, then the review
    /// path, which reads the existing results and runs no TAS simulation - and renders the Hub, the progress
    /// window and the comparison to PNG.
    /// <para>
    /// Set <c>SAM_PARTO_EVIDENCE</c> to the folder holding the saved run (the one with
    /// <c>&lt;run&gt;.sam</c>, <c>.tsd</c>, <c>.partorun.json</c> and the Iteration 3 record) and
    /// <c>SAM_PARTO_SCREENSHOTS</c> to where the images and a text summary go. Without both, this passes
    /// having done nothing, which is the point: nobody's ordinary test run depends on TAS or on saved results.
    /// </para>
    /// <para>
    /// A review rewrites the TM59 and comparison reports beside the results, as it does in the application.
    /// Those files are backed up first and restored afterwards, so the evidence folder is left as it was.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowEvidenceHarness
    {
        [WpfFact]
        public void Reopen_the_saved_run_and_its_guidance_result_and_render_the_workflow()
        {
            string directory_Evidence = Environment.GetEnvironmentVariable("SAM_PARTO_EVIDENCE");
            string directory_Screenshots = Environment.GetEnvironmentVariable("SAM_PARTO_SCREENSHOTS");

            if (string.IsNullOrWhiteSpace(directory_Evidence) || string.IsNullOrWhiteSpace(directory_Screenshots) || !Directory.Exists(directory_Evidence))
            {
                return;
            }

            Directory.CreateDirectory(directory_Screenshots);

            StringBuilder summary = new();

            string path_Model = Directory.GetFiles(directory_Evidence, "*.partorun.json").Select(x => x.Substring(0, x.Length - ".partorun.json".Length) + ".sam").Single(File.Exists);

            //Every report a review may rewrite, backed up so the evidence folder is restored afterwards.
            Dictionary<string, byte[]> backups = [];
            foreach (string path in Directory.GetFiles(directory_Evidence).Where(x => x.EndsWith("-TM59.txt", StringComparison.OrdinalIgnoreCase) || x.Contains("-Review.")))
            {
                backups[path] = File.ReadAllBytes(path);
            }

            List<string> files_Before = [.. Directory.GetFiles(directory_Evidence)];

            try
            {
                //---------------------------------------------------------------------------------------------
                //File › Open
                //---------------------------------------------------------------------------------------------
                DateTime dateTime = DateTime.Now;

                AnalyticalModel analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path_Model).First();

                PartORun partORun = new();
                bool restored = partORun.Restore(analyticalModel, path_Model, out string refusal_Restore);

                summary.AppendLine(string.Format("Opened '{0}' in {1:0.0} s. Restored: {2}. Resume Iteration 3: {3}. {4}", path_Model, (DateTime.Now - dateTime).TotalSeconds, restored, partORun.CanResumeIteration3, refusal_Restore ?? string.Empty));

                Assert.True(restored, refusal_Restore);

                //---------------------------------------------------------------------------------------------
                //The Hub, as the command builds it
                //---------------------------------------------------------------------------------------------
                dateTime = DateTime.Now;

                VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

                PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(partORun, out PartOIteration3Eligibility partOIteration3Eligibility);

                summary.AppendLine(string.Format("Eligibility: can run {0}; {1}", partOIteration3Eligibility.CanRun, partOIteration3Eligibility.Refusal_Run ?? "no run refusal"));

                foreach (PartOIteration3PairingStatus partOIteration3PairingStatus in partOIteration3Eligibility.PairingStatuses)
                {
                    summary.AppendLine(string.Format("  {0}: {1}{2}", Query.PartOIteration3MethodLabel(partOIteration3PairingStatus.BehaviourMode), partOIteration3PairingStatus, partOIteration3PairingStatus.IsLegacy ? " (legacy record)" : string.Empty));
                }

                PartOWorkflowWindow partOWorkflowWindow = new()
                {
                    AnalyticalModel = analyticalModel,
                    PartORun = partORun,
                    VentilationUnitCatalogue = ventilationUnitCatalogue,
                    Capabilities = partOWorkflowCapabilities,
                    Iteration3Eligibility = partOIteration3Eligibility,
                    Iteration3Mode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                    SimulationCase = PartOSimulationCase.Create(analyticalModel, path_Model, null),
                    LastOutcome = new PartOWorkflowOutcome(PartOWorkflowOutcomeKind.Information, "Opened the saved Iteration 1a run · results are reviewable without a new simulation"),
                };

                partOWorkflowWindow.CompleteInitialisation();

                summary.AppendLine(string.Format("Hub built in {0:0.0} s. Reference: {1}", (DateTime.Now - dateTime).TotalSeconds, partOWorkflowWindow.Iteration3ReferenceText));

                PartOIteration3Preflight partOIteration3Preflight = partOWorkflowWindow.Iteration3Preflight;
                summary.AppendLine(string.Format("Pre-flight (guidance): can run {0}; {1}", partOIteration3Preflight?.CanRun, partOIteration3Preflight is null ? "none" : string.Join(" | ", partOIteration3Preflight.Summary())));
                foreach (string refusal in partOIteration3Preflight?.Refusals ?? [])
                {
                    summary.AppendLine("    refusal: " + refusal);
                }

                summary.AppendLine("Open result: " + partOWorkflowWindow.CanOpenIteration3Result + " (" + partOWorkflowWindow.Iteration3ReviewText + "); run: " + partOWorkflowWindow.CanRunIteration3);

                Render(partOWorkflowWindow, Path.Combine(directory_Screenshots, "01-hub-reopened-1a.png"), 800, 1560);

                //Every method's pre-flight, and the Advanced disclosure open for the validation methods.
                foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Query.PartOIteration3BehaviourModes)
                {
                    partOWorkflowWindow.Iteration3Mode = partOIteration3BehaviourMode;

                    PartOIteration3Preflight partOIteration3Preflight_Mode = partOWorkflowWindow.Iteration3Preflight;

                    summary.AppendLine(string.Format(
                        "Pre-flight ({0}): can run {1}; {2}{3}",
                        Query.PartOIteration3MethodLabel(partOIteration3BehaviourMode),
                        partOIteration3Preflight_Mode?.CanRun,
                        partOIteration3Preflight_Mode is null ? "none" : string.Join(" | ", partOIteration3Preflight_Mode.Summary()),
                        partOIteration3Preflight_Mode is null || partOIteration3Preflight_Mode.CanRun ? string.Empty : " - " + partOIteration3Preflight_Mode.Refusals.FirstOrDefault()));
                }

                partOWorkflowWindow.Iteration3Mode = PartOIteration3BehaviourMode.SelectedProduct;
                Render(partOWorkflowWindow, Path.Combine(directory_Screenshots, "02-hub-certified-product.png"), 800, 1560);

                partOWorkflowWindow.Iteration3Mode = PartOIteration3BehaviourMode.Parity;
                Render(partOWorkflowWindow, Path.Combine(directory_Screenshots, "03-hub-advanced-route-check.png"), 800, 1560);

                partOWorkflowWindow.Close();

                //---------------------------------------------------------------------------------------------
                //The progress window, as it looks part-way through a run (a synthetic state - no TAS is run)
                //---------------------------------------------------------------------------------------------
                DateTime now = DateTime.UtcNow.AddMinutes(-4).AddSeconds(-32);
                PartOProgressState partOProgressState = new(Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance), () => now);
                partOProgressState.Start(0);
                now = now.AddSeconds(9);
                partOProgressState.Start(1);
                now = now.AddSeconds(3);
                partOProgressState.Start(2);
                partOProgressState.Detail = "Simulating Model";
                now = DateTime.UtcNow;

                PartOProgressWindow partOProgressWindow = new()
                {
                    Heading = "Iteration 3 — " + Query.PartOIteration3MethodLabel(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance),
                    Subheading = "Reference case: " + Query.PartOIterationText(partORun),
                    Cancellable = true,
                    State = partOProgressState,
                };

                Render(partOProgressWindow, Path.Combine(directory_Screenshots, "04-progress-iteration3.png"), 520, 0);
                partOProgressWindow.Close();

                //---------------------------------------------------------------------------------------------
                //Open result - the saved guidance result, reviewed from its existing results, no TAS
                //---------------------------------------------------------------------------------------------
                dateTime = DateTime.Now;

                PartOIteration3Result partOIteration3Result = Modify.ReviewPartOIteration3(partORun, new PartOIteration3Pipeline(), PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

                summary.AppendLine(string.Format("Review (no TAS) in {0:0.0} s: complete {1}. {2}", (DateTime.Now - dateTime).TotalSeconds, partOIteration3Result.IsComplete, PartOIteration3ReportText.Outcome(partOIteration3Result)));

                if (partOIteration3Result.IsComplete)
                {
                    summary.AppendLine("  " + PartOIteration3ReportText.Comparison(partOIteration3Result, System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    summary.AppendLine("  " + PartOIteration3ReportText.Refusal(partOIteration3Result).Replace(Environment.NewLine, " / "));
                }

                List<PartOIteration3GuidanceEvidence> guidance = Query.PartOIteration3GuidanceEvidence(partORun, partOIteration3Result.Record, ventilationUnitCatalogue, out string source_Guidance);

                summary.AppendLine(string.Format("Guidance fields: {0} unit(s). {1}", guidance.Count, source_Guidance));

                foreach (var (width, height, name) in new[] { (1320, 860, "05-comparison-1080p.png"), (1800, 1150, "06-comparison-1440p.png") })
                {
                    PartOIteration3ResultWindow partOIteration3ResultWindow = new()
                    {
                        ReferenceText = Query.PartOIterationText(partORun),
                        Guidance = guidance,
                        GuidanceSource = source_Guidance,
                        Result = partOIteration3Result,
                    };

                    Render(partOIteration3ResultWindow, Path.Combine(directory_Screenshots, name), width, height);

                    partOIteration3ResultWindow.Close();
                }

                //Filtered to the changed outcomes, groups opened.
                PartOIteration3ResultWindow partOIteration3ResultWindow_Changed = new()
                {
                    ReferenceText = Query.PartOIterationText(partORun),
                    Guidance = guidance,
                    GuidanceSource = source_Guidance,
                    Result = partOIteration3Result,
                };

                ((System.Windows.Controls.CheckBox)partOIteration3ResultWindow_Changed.FindName("checkBox_Changed")).IsChecked = true;

                Render(partOIteration3ResultWindow_Changed, Path.Combine(directory_Screenshots, "07-comparison-changed-filter.png"), 1320, 860);

                summary.AppendLine(string.Format("Changed filter: {0} row(s) visible, groups open {1}.", partOIteration3ResultWindow_Changed.Rows_Visible.Count, partOIteration3ResultWindow_Changed.GroupsExpanded));

                partOIteration3ResultWindow_Changed.Close();
            }
            finally
            {
                foreach (KeyValuePair<string, byte[]> keyValuePair in backups)
                {
                    File.WriteAllBytes(keyValuePair.Key, keyValuePair.Value);
                }

                //A report the review wrote where there was none before is removed again.
                foreach (string path in Directory.GetFiles(directory_Evidence).Except(files_Before, StringComparer.OrdinalIgnoreCase))
                {
                    File.Delete(path);
                    summary.AppendLine("Removed file the review created: " + path);
                }

                File.WriteAllText(Path.Combine(directory_Screenshots, "summary.txt"), summary.ToString());
            }
        }

        /// <summary>Shows a window at a size, lets it lay out, and writes it to a PNG.</summary>
        internal static void Render(System.Windows.Window window, string path, double width, double height)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = 0;
            window.Top = 0;
            window.Width = width;

            if (height > 0)
            {
                window.SizeToContent = SizeToContent.Manual;
                window.MaxHeight = double.PositiveInfinity;
                window.Height = height;
            }
            else
            {
                window.SizeToContent = SizeToContent.Height;
            }

            window.ShowActivated = false;

            //A window may cap its own height to its monitor as it first renders (the Hub does); a render
            //asks for the full content, so the requested ceiling is put back once it has shown.
            double maxHeight = window.MaxHeight;

            if (!window.IsVisible)
            {
                window.Show();
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }

            window.MaxHeight = maxHeight;

            window.UpdateLayout();

            //Let bindings and deferred layout settle.
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();

            FrameworkElement content = (FrameworkElement)window.Content;

            int pixelWidth = (int)Math.Ceiling(content.ActualWidth);
            int pixelHeight = (int)Math.Ceiling(content.ActualHeight);

            RenderTargetBitmap renderTargetBitmap = new(pixelWidth + 2, pixelHeight + 2, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual drawingVisual = new();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, pixelWidth + 2, pixelHeight + 2));
                drawingContext.DrawRectangle(new VisualBrush(content), null, new Rect(1, 1, pixelWidth, pixelHeight));
            }

            renderTargetBitmap.Render(drawingVisual);

            PngBitmapEncoder pngBitmapEncoder = new();
            pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            using FileStream fileStream = File.Create(path);
            pngBitmapEncoder.Save(fileStream);
        }
    }
}
