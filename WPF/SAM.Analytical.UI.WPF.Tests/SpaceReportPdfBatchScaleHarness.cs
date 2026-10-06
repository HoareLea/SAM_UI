// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR2F-2 large-model acceptance, run by hand: replicates a real model's Spaces (with their panels and
    /// simulation results) to about 5,000 and exports both Space reports for all of them with the real renderer.
    /// Set <c>SAM_PR2F2_SCALE_MODEL</c> to the .sam file and <c>SAM_PR2F2_SCALE_OUT</c> to an output folder
    /// (optionally <c>SAM_PR2F2_SCALE_COPIES</c>, default 555: 9 Spaces x 555 = 4,995, and
    /// <c>SAM_PR2F2_SCALE_SAVE</c> to keep the replicated model as a .sam file). Without both it passes
    /// without doing anything. Every 50th copy keeps the original names, so the batch also resolves real name
    /// collisions. Writes <c>scale-summary.txt</c> (setup, per-500-document timing, managed memory and working set).
    /// </summary>
    public class SpaceReportPdfBatchScaleHarness
    {
        [Fact]
        public void ExportAllSpaces_OfAReplicatedModel()
        {
            string path_Model = Environment.GetEnvironmentVariable("SAM_PR2F2_SCALE_MODEL");
            string directory = Environment.GetEnvironmentVariable("SAM_PR2F2_SCALE_OUT");
            if (string.IsNullOrWhiteSpace(path_Model) || string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            int copies = int.TryParse(Environment.GetEnvironmentVariable("SAM_PR2F2_SCALE_COPIES"), out int value) ? value : 555;

            AnalyticalModel analyticalModel = Replicate(Core.Convert.ToSAM<AnalyticalModel>(path_Model).Find(x => x is not null), copies);

            //Optionally keep the replicated model, to open it in the application for a UI run (progress, Cancel).
            string path_Save = Environment.GetEnvironmentVariable("SAM_PR2F2_SCALE_SAVE");
            if (!string.IsNullOrWhiteSpace(path_Save))
            {
                Assert.True(Core.Convert.ToFile(analyticalModel, path_Save));
            }

            Directory.CreateDirectory(directory);

            StringBuilder summary = new StringBuilder();
            Process process = Process.GetCurrentProcess();

            GC.Collect();
            long managed_Before = GC.GetTotalMemory(true);
            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "Model: {0} x {1} copies = {2} Spaces; managed before batch {3:F0} MB", Path.GetFileName(path_Model), copies, analyticalModel.GetSpaces().Count, managed_Before / 1048576.0));

            Stopwatch stopwatch = Stopwatch.StartNew();
            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory);
            TimeSpan setup = stopwatch.Elapsed;
            int existing = batch.ExistingCount();
            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "Setup (snapshot + plan {0} names): {1:F0} ms; existing {2}", batch.DocumentCount, setup.TotalMilliseconds, existing));
            summary.AppendLine("documents | elapsed s | s per 500 docs | managed MB | working set MB");

            TimeSpan previous = stopwatch.Elapsed;
            long managed_Peak = 0;
            long workingSet_Peak = 0;

            SpaceReportPdfBatchResult result = batch.Run(
                SpaceReportPdfExistingFiles.Overwrite,
                new SpaceReportPdfBatchTests.SynchronousProgress(x =>
                {
                    if (x.DocumentIndex % 500 != 0 || x.DocumentIndex == 0)
                    {
                        return;
                    }

                    long managed = GC.GetTotalMemory(false);
                    process.Refresh();
                    managed_Peak = Math.Max(managed_Peak, managed);
                    workingSet_Peak = Math.Max(workingSet_Peak, process.WorkingSet64);

                    TimeSpan now = stopwatch.Elapsed;
                    summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,9} | {1,9:F1} | {2,14:F2} | {3,10:F0} | {4,14:F0}", x.DocumentIndex, now.TotalSeconds, (now - previous).TotalSeconds, managed / 1048576.0, process.WorkingSet64 / 1048576.0));
                    previous = now;
                }));

            stopwatch.Stop();

            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "Total: {0:F1} s ({1:F1} s documents); {2:F2} ms per document", stopwatch.Elapsed.TotalSeconds, result.Elapsed.TotalSeconds, result.Elapsed.TotalMilliseconds / Math.Max(1, result.Items.Count)));
            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "Created {0}  Skipped {1}  Failed {2}  Not started {3}  Cancelled {4}", result.Created, result.Skipped, result.Failed, result.NotStarted, result.Cancelled));
            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "Peak sampled managed {0:F0} MB, working set {1:F0} MB", managed_Peak / 1048576.0, workingSet_Peak / 1048576.0));
            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "PDFs in folder {0}; .tmp files {1}; suffixed names {2}; log {3}", Directory.GetFiles(directory, "*.pdf").Length, Directory.GetFiles(directory, "*.tmp").Length, result.Items.Count(x => x.Path != null && x.Path.EndsWith("].pdf")), result.LogPath));

            File.WriteAllText(Path.Combine(directory, "scale-summary.txt"), summary.ToString());

            Assert.Equal(0, result.Failed);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }

        /// <summary>Adds <paramref name="copies"/> - 1 copies of every Space, each with its own copies of its panels and results.</summary>
        private static AnalyticalModel Replicate(AnalyticalModel analyticalModel, int copies)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            List<Space> spaces = adjacencyCluster.GetSpaces();

            for (int copy = 1; copy < copies; copy++)
            {
                foreach (Space space in spaces)
                {
                    Space space_Copy = new Space(Guid.NewGuid(), space, copy % 50 == 0 ? space.Name : space.Name + " #" + copy, space.Location);

                    //This constructor keeps the parameters and location, not the internal condition.
                    space_Copy.InternalCondition = space.InternalCondition;
                    adjacencyCluster.AddObject(space_Copy);

                    foreach (SpaceSimulationResult spaceSimulationResult in adjacencyCluster.GetRelatedObjects<SpaceSimulationResult>(space) ?? [])
                    {
                        SpaceSimulationResult spaceSimulationResult_Copy = new SpaceSimulationResult(Guid.NewGuid(), spaceSimulationResult);
                        adjacencyCluster.AddObject(spaceSimulationResult_Copy);
                        adjacencyCluster.AddRelation(space_Copy, spaceSimulationResult_Copy);
                    }

                    foreach (Panel panel in adjacencyCluster.GetRelatedObjects<Panel>(space) ?? [])
                    {
                        Panel panel_Copy = Analytical.Create.Panel(Guid.NewGuid(), panel);
                        adjacencyCluster.AddObject(panel_Copy);
                        adjacencyCluster.AddRelation(space_Copy, panel_Copy);
                    }
                }
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }
    }
}
