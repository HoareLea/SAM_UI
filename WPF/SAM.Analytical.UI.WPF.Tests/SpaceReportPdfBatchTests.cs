// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SAM.Core.Reporting;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR2F-2 batch export of Space report PDFs (<see cref="SpaceReportPdfBatch"/>): one shared model snapshot,
    /// a fresh diagnostics log per document, deterministic file names, one existing-file policy, failures that do not
    /// stop the batch, cancellation between documents, no staging files left, and the log. Report content is SAM's:
    /// these tests prove the batch hands SAM's documents through unchanged.
    /// </summary>
    public class SpaceReportPdfBatchTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_UI_SpaceReportPdfBatchTests_" + Guid.NewGuid().ToString("N"));

        private readonly ITestOutputHelper testOutputHelper;

        public SpaceReportPdfBatchTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
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

        // ------------------------------------------------------------------ the single-document writer seam

        [Theory]
        [InlineData("SpaceAssumptionsPdf")]
        [InlineData("SpaceDesignLoadSummaryPdf")]
        public void TheContextOverload_WritesTheSameDocument_AsTheModelOverload(string id)
        {
            SpaceReportPdf spaceReportPdf = id == SpaceReportPdf.SpaceAssumptions.Id ? SpaceReportPdf.SpaceAssumptions : SpaceReportPdf.SpaceDesignLoadSummary;
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office", "Store");

            CapturingRenderer capturingRenderer_Model = new CapturingRenderer();
            SpaceReportPdfResult result_Model = Modify.WriteSpaceReportPdf(analyticalModel, spaces[0], Path.Combine(directory, "model.pdf"), spaceReportPdf, documentRenderer: capturingRenderer_Model);

            Analytical.Reporting.DocumentContext documentContext = Analytical.Reporting.Create.DocumentContext(analyticalModel, new DocumentOptions());
            CapturingRenderer capturingRenderer_Context = new CapturingRenderer();
            SpaceReportPdfResult result_Context = Modify.WriteSpaceReportPdf(documentContext.WithNewDiagnostics(), spaces[0], Path.Combine(directory, "context.pdf"), spaceReportPdf, capturingRenderer_Context);

            Assert.True(result_Model.Succeeded, result_Model.Message);
            Assert.True(result_Context.Succeeded, result_Context.Message);
            Assert.Equal(Texts(capturingRenderer_Model.Documents.Single()), Texts(capturingRenderer_Context.Documents.Single()));
            Assert.Equal(result_Model.Notes, result_Context.Notes);
        }

        [Fact]
        public void TheContextOverload_ReportsTheContextsOwnDiagnostics()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Bare");
            Analytical.Reporting.DocumentContext documentContext = Analytical.Reporting.Create.DocumentContext(analyticalModel).WithNewDiagnostics();

            SpaceReportPdfResult result = Modify.WriteSpaceReportPdf(documentContext, spaces[0], Path.Combine(directory, "bare.pdf"), SpaceReportPdf.SpaceAssumptions, new CapturingRenderer());

            Assert.True(result.Succeeded, result.Message);
            Assert.NotEmpty(result.Notes);
            Assert.Equal(documentContext.Diagnostics.Select(x => x.Text), result.Notes);
        }

        // ------------------------------------------------------------------ scope and report types

        [Fact]
        public void OneReportType_SelectedSpaces_WritesOnlyThoseSpaces()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office", "Store", "Plant");

            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, [spaces[2].Guid, spaces[0].Guid], [SpaceReportPdf.SpaceAssumptions], directory);
            SpaceReportPdfBatchResult result = batch.Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.False(batch.AllSpaces);
            Assert.Equal(2, result.DocumentCount);
            Assert.Equal(2, result.Created);
            Assert.Equal(0, result.Failed + result.Skipped + result.NotStarted);

            //Model order, not selection order; and the one-Space command's own file names.
            Assert.Equal(["Office", "Plant"], result.Items.Select(x => x.SpaceName));
            Assert.Equal(
                ["Office - Space Assumptions.pdf", "Plant - Space Assumptions.pdf"],
                Pdfs().Select(Path.GetFileName).OrderBy(x => x));
            Assert.Equal(Query.SpaceReportPdfFileName(spaces[0], SpaceReportPdf.SpaceAssumptions), Path.GetFileName(result.Items[0].Path));
        }

        [Fact]
        public void BothReportTypes_AllSpaces_WritesEverySpaceTimesEveryReport_InSpaceThenReportOrder()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office", "Store", "Plant");

            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory);
            SpaceReportPdfBatchResult result = batch.Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.True(batch.AllSpaces);
            Assert.Equal(6, result.DocumentCount);
            Assert.Equal(6, result.Created);
            Assert.Equal(6, Pdfs().Count);
            Assert.Equal(
                ["Office|Space Assumptions", "Office|Space Design Load Summary", "Store|Space Assumptions", "Store|Space Design Load Summary", "Plant|Space Assumptions", "Plant|Space Design Load Summary"],
                result.Items.Select(x => x.SpaceName + "|" + x.SpaceReportPdf.Name));
        }

        [Fact]
        public void TheRealRenderer_WritesPdfsThatOpen()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office", "Store");

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory).Run(SpaceReportPdfExistingFiles.Skip);

            Assert.Equal(4, result.Created);
            foreach (SpaceReportPdfBatchItem item in result.Items)
            {
                using PdfDocument pdfDocument = PdfReader.Open(item.Path, PdfDocumentOpenMode.Import);
                Assert.True(pdfDocument.PageCount >= 1);
            }

            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }

        [Fact]
        public void NoReportType_OrNoSpace_IsRefusedBeforeAnythingIsWritten()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office");

            Assert.Throws<ArgumentException>(() => SpaceReportPdfBatch.Create(analyticalModel, null, [], directory));
            Assert.Throws<ArgumentException>(() => SpaceReportPdfBatch.Create(analyticalModel, [], [SpaceReportPdf.SpaceAssumptions], directory));
            Assert.Throws<ArgumentException>(() => SpaceReportPdfBatch.Create(new AnalyticalModel("Empty", null, null, null, new AdjacencyCluster()), null, [SpaceReportPdf.SpaceAssumptions], directory));
            Assert.Empty(Directory.GetFiles(directory));
        }

        [Fact]
        public void ASelectedSpaceNoLongerInTheModel_IsAFailedDocument_NotDropped()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office");
            Guid removed = Guid.NewGuid();

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, [spaces[0].Guid, removed], [SpaceReportPdf.SpaceAssumptions], directory).Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.Equal(2, result.DocumentCount);
            Assert.Equal(1, result.Created);
            SpaceReportPdfBatchItem failed = Assert.Single(result.Items, x => x.Status == SpaceReportPdfBatchStatus.Failed);
            Assert.Equal(removed, failed.SpaceGuid);
            Assert.Equal(SpaceReportPdfFailure.Selection, failed.Failure);
            Assert.Null(failed.Path);
        }

        // ------------------------------------------------------------------ one shared snapshot, isolated diagnostics

        [Fact]
        public void EveryDocument_SharesTheOneSnapshot_WithItsOwnDiagnosticsLog()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office", "Store", "Plant");
            List<Analytical.Reporting.DocumentContext> documentContexts = [];

            SpaceReportPdf recording(SpaceReportPdf spaceReportPdf) => new SpaceReportPdf(spaceReportPdf.Id, spaceReportPdf.Name, (documentContext, space) =>
            {
                documentContexts.Add(documentContext);
                return spaceReportPdf.CreateDocument(documentContext, space);
            });

            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [recording(SpaceReportPdf.SpaceAssumptions), recording(SpaceReportPdf.SpaceDesignLoadSummary)], directory);
            SpaceReportPdfBatchResult result = batch.Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.Equal(6, result.Created);
            Assert.Equal(6, documentContexts.Count);

            Analytical.Reporting.DocumentContext root = batch.DocumentContext;
            Assert.All(documentContexts, x =>
            {
                //The same snapshot: the model copied once, the cluster read once.
                Assert.Same(root.AnalyticalModel, x.AnalyticalModel);
                Assert.Same(root.AdjacencyCluster, x.AdjacencyCluster);
                Assert.Same(root.ProfileLibrary, x.ProfileLibrary);
                Assert.Same(root.Provenance, x.Provenance);
                Assert.NotSame(root, x);
                Assert.NotSame(root.Diagnostics, x.Diagnostics);
            });

            Assert.Equal(6, documentContexts.Select(x => x.Diagnostics).Distinct().Count());
            Assert.Empty(root.Diagnostics);

            //The batch's Spaces are the snapshot's, not the caller's model's.
            Assert.All(batch.Spaces, x => Assert.Same(x, root.AdjacencyCluster.GetObject<Space>(x.Guid)) );
        }

        [Fact]
        public void EachDocumentsNotes_AreItsOwn_AndMatchTheOneSpaceCommand()
        {
            //A bare Space gathers many notes; the representative one far fewer. Neither may carry the other's.
            AnalyticalModel analyticalModel = Model(out _, x => { if (x.Name == "Office") Rich(x); }, "Bare", "Office");

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory).Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.Equal(4, result.Created);
            foreach (SpaceReportPdfBatchItem item in result.Items)
            {
                Space space = analyticalModel.AdjacencyCluster.GetObject<Space>(item.SpaceGuid);
                SpaceReportPdfResult single = Modify.WriteSpaceReportPdf(analyticalModel, space, Path.Combine(directory, "single.pdf"), item.SpaceReportPdf, documentRenderer: new CountingRenderer());

                Assert.Equal(single.Notes, item.Notes);
            }

            List<string> bare = result.Items[0].Notes.ToList();
            List<string> office = result.Items[2].Notes.ToList();
            Assert.True(bare.Count > office.Count, string.Join(" | ", office));
        }

        // ------------------------------------------------------------------ failures do not stop the batch

        [Fact]
        public void AFailingSpace_IsRecorded_AndTheRestOfTheBatchIsWritten()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office", "Broken", "Plant");
            SpaceReportPdf failing = new SpaceReportPdf("SpaceAssumptionsPdf", "Space Assumptions", (documentContext, space) =>
                space.Name == "Broken" ? throw new InvalidOperationException("collector failed") : SpaceReportPdf.SpaceAssumptions.CreateDocument(documentContext, space));

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, null, [failing], directory).Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.Equal(2, result.Created);
            Assert.Equal(1, result.Failed);
            Assert.False(result.Cancelled);

            SpaceReportPdfBatchItem item = result.Items[1];
            Assert.Equal(SpaceReportPdfBatchStatus.Failed, item.Status);
            Assert.Equal(SpaceReportPdfFailure.Document, item.Failure);
            Assert.Contains("collector failed", item.Message);
            Assert.IsType<InvalidOperationException>(item.Exception);
            Assert.False(File.Exists(item.Path));

            string log = File.ReadAllText(result.LogPath);
            Assert.Contains("[2/3] FAILED | Broken (", log);
            Assert.Contains("    stage: Document", log);
            Assert.Contains("collector failed", log);
        }

        [Fact]
        public void AFailingReportType_DoesNotPreventTheLaterReports()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office", "Store");
            SpaceReportPdf failing = new SpaceReportPdf("FailingPdf", "Failing", (_, _) => throw new InvalidOperationException("always"));

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, null, [failing, SpaceReportPdf.SpaceDesignLoadSummary], directory).Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.Equal(2, result.Failed);
            Assert.Equal(2, result.Created);
            Assert.All(result.Items.Where(x => x.SpaceReportPdf == failing), x => Assert.Equal(SpaceReportPdfBatchStatus.Failed, x.Status));
            Assert.All(result.Items.Where(x => x.SpaceReportPdf != failing), x => Assert.Equal(SpaceReportPdfBatchStatus.Created, x.Status));
        }

        [Fact]
        public void RenderingAndOutputFailures_NameTheirStage_AndLeaveNoStagingFile()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office", "Locked", "Plant");
            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions], directory);

            string locked = batch.Path(spaces[1].Guid, SpaceReportPdf.SpaceAssumptions);
            File.WriteAllText(locked, "previous");

            SpaceReportPdfBatchResult result;
            using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                //Office renders; Locked renders but cannot be saved; Plant's render (the third) throws.
                result = batch.Run(SpaceReportPdfExistingFiles.Overwrite, documentRenderer: new CountingRenderer(failAt: 3));
            }

            Assert.Equal(["Created", "Failed:Output", "Failed:Rendering"], result.Items.Select(x => x.Status == SpaceReportPdfBatchStatus.Failed ? "Failed:" + x.Failure : x.Status.ToString()));
            Assert.Equal("previous", File.ReadAllText(locked));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.False(File.Exists(batch.Path(spaces[2].Guid, SpaceReportPdf.SpaceAssumptions)));
        }

        // ------------------------------------------------------------------ the output folder itself

        [Fact]
        public void TheOutputFolder_CannotBeCreated_FailsFast_BeforeAnyDocument()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office", "Store");

            //A file already sits where the output folder needs to be, so CreateDirectory cannot succeed.
            string blocked = Path.Combine(directory, "blocked");
            File.WriteAllText(blocked, "not a folder");

            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], blocked);
            CountingRenderer countingRenderer = new CountingRenderer();

            IOException exception = Assert.Throws<IOException>(() => batch.Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: countingRenderer));

            Assert.Contains(blocked, exception.Message);
            //Nothing was attempted: no document was rendered, no PDF, no log, and the blocking file is untouched.
            Assert.Equal(0, countingRenderer.Count);
            Assert.Empty(Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories));
            Assert.Empty(Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories));
            Assert.Equal("not a folder", File.ReadAllText(blocked));
        }

        // ------------------------------------------------------------------ existing files

        [Fact]
        public void Overwrite_ReplacesExistingPdfs()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office", "Store");
            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions], directory);
            string existing = batch.Path(spaces[0].Guid, SpaceReportPdf.SpaceAssumptions);
            File.WriteAllText(existing, "previous");

            Assert.Equal(1, batch.ExistingCount());

            SpaceReportPdfBatchResult result = batch.Run(SpaceReportPdfExistingFiles.Overwrite, documentRenderer: new CountingRenderer());

            Assert.Equal(2, result.Created);
            Assert.Equal(0, result.Skipped);
            Assert.Equal("%PDF-stub", File.ReadAllText(existing));
        }

        [Fact]
        public void Skip_KeepsExistingPdfs_AndCountsThem()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office", "Store");
            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory);
            string existing = batch.Path(spaces[1].Guid, SpaceReportPdf.SpaceDesignLoadSummary);
            File.WriteAllText(existing, "previous");

            CountingRenderer countingRenderer = new CountingRenderer();
            SpaceReportPdfBatchResult result = batch.Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: countingRenderer);

            Assert.Equal(3, result.Created);
            Assert.Equal(1, result.Skipped);
            Assert.Equal(3, countingRenderer.Count);
            Assert.Equal("previous", File.ReadAllText(existing));
            Assert.Equal(SpaceReportPdfBatchStatus.Skipped, result.Items[3].Status);
            Assert.Contains("[4/4] Skipped | Store (", File.ReadAllText(result.LogPath));
        }

        // ------------------------------------------------------------------ cancellation

        [Fact]
        public void Cancelling_StopsBetweenDocuments_KeepsTheFinishedPdfs_AndLogsTheCancellation()
        {
            AnalyticalModel analyticalModel = Model(out _, "A", "B", "C", "D", "E");
            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            List<SpaceReportPdfBatchProgress> reports = [];

            //Cancelled while the third document is being written: that one finishes, the fourth never starts.
            SpaceReportPdfBatchResult result = batch.Run(
                SpaceReportPdfExistingFiles.Skip,
                new SynchronousProgress(x =>
                {
                    reports.Add(x);
                    if (x.DocumentIndex == 2)
                    {
                        cancellationTokenSource.Cancel();
                    }
                }),
                cancellationTokenSource.Token,
                new CountingRenderer());

            Assert.True(result.Cancelled);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(3, result.Created);
            Assert.Equal(7, result.NotStarted);
            Assert.Equal(3, Pdfs().Count);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            Assert.Equal(3, reports.Count);
            Assert.Equal([1, 1, 2], reports.Select(x => x.SpaceIndex));
            Assert.All(reports, x => Assert.Equal(5, x.SpaceCount));

            string log = File.ReadAllText(result.LogPath);
            Assert.Contains("Result:          CANCELLED after 3 of 10 documents", log);
            Assert.Contains("Created: 3  Skipped: 0  Failed: 0  Not started: 7", log);
        }

        [Fact]
        public void CancelledBeforeTheFirstDocument_WritesNoPdf_ButStillALog()
        {
            AnalyticalModel analyticalModel = Model(out _, "A", "B");

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions], directory).Run(SpaceReportPdfExistingFiles.Skip, null, new CancellationToken(true), new CountingRenderer());

            Assert.True(result.Cancelled);
            Assert.Empty(result.Items);
            Assert.Equal(2, result.NotStarted);
            Assert.Empty(Pdfs());
            Assert.True(File.Exists(result.LogPath));
        }

        // ------------------------------------------------------------------ the log

        [Fact]
        public void TheLog_HasTheRun_EveryDocument_ItsNotes_AndTheTotals()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office", "Store");

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, [spaces[0].Guid, spaces[1].Guid], [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory).Run(SpaceReportPdfExistingFiles.Overwrite, documentRenderer: new CountingRenderer());

            Assert.NotNull(result.LogPath);
            Assert.Null(result.LogError);
            Assert.Equal(directory, Path.GetDirectoryName(result.LogPath));
            Assert.Matches(@"^Space reports \d{4}-\d{2}-\d{2} \d{6}\.log$", Path.GetFileName(result.LogPath));

            string[] lines = File.ReadAllLines(result.LogPath);
            Assert.Equal("SAM Space report export", lines[0]);
            Assert.Matches(@"^Run started:     \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", lines[1]);
            Assert.Contains("Model:           Batch", lines);
            Assert.Contains("Scope:           Selected Spaces", lines);
            Assert.Contains("Spaces:          2", lines);
            Assert.Contains("Report types:    Space Assumptions, Space Design Load Summary", lines);
            Assert.Contains("Units:           SI", lines);
            Assert.Contains("Output folder:   " + directory, lines);
            Assert.Contains("Existing files:  Overwrite", lines);
            Assert.Contains("Documents:       4", lines);

            Assert.Contains(string.Format("[1/4] Created | Office ({0}) | Space Assumptions | Office - Space Assumptions.pdf", spaces[0].Guid.ToString("D")), lines);
            Assert.Contains(string.Format("[4/4] Created | Store ({0}) | Space Design Load Summary | Store - Space Design Load Summary.pdf", spaces[1].Guid.ToString("D")), lines);

            //Each document's diagnostics, under it.
            int first = Array.FindIndex(lines, x => x.StartsWith("[1/4]"));
            int second = Array.FindIndex(lines, x => x.StartsWith("[2/4]"));
            Assert.Equal(result.Items[0].Notes.Select(x => "    note: " + x), lines.Skip(first + 1).Take(second - first - 1));

            Assert.Contains("Result:          Completed", lines);
            Assert.Contains("Created: 4  Skipped: 0  Failed: 0  Not started: 0", lines);
            Assert.Matches(@"^Run finished:    \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} \(elapsed \d{2}:\d{2}:\d{2}\)$", lines.Single(x => x.StartsWith("Run finished:")));
        }

        [Fact]
        public void TwoRunsInOneSecond_WriteTwoLogs()
        {
            AnalyticalModel analyticalModel = Model(out _, "Office");
            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions], directory);

            string first = batch.Run(SpaceReportPdfExistingFiles.Overwrite, documentRenderer: new CountingRenderer()).LogPath;
            string second = batch.Run(SpaceReportPdfExistingFiles.Overwrite, documentRenderer: new CountingRenderer()).LogPath;

            Assert.NotEqual(first, second);
            Assert.True(File.Exists(first));
            Assert.True(File.Exists(second));
        }

        // ------------------------------------------------------------------ file names

        [Fact]
        public void UniqueNames_AreTheOneSpaceCommandsNames()
        {
            List<Space> spaces = [new Space("Office"), new Space("Plant/Store: A*B?"), new Space("CON")];

            Dictionary<(Guid, string), string> names = Query.SpaceReportPdfFileNames(spaces, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary]);

            Assert.Equal(6, names.Count);
            foreach (Space space in spaces)
            {
                Assert.Equal(Query.SpaceReportPdfFileName(space, SpaceReportPdf.SpaceAssumptions), names[(space.Guid, SpaceReportPdf.SpaceAssumptions.Id)]);
                Assert.Equal(Query.SpaceReportPdfFileName(space, SpaceReportPdf.SpaceDesignLoadSummary), names[(space.Guid, SpaceReportPdf.SpaceDesignLoadSummary.Id)]);
            }
        }

        [Theory]
        [InlineData("Office", "Office")]
        [InlineData("Office", "OFFICE")]
        [InlineData("A/B", "A:B")]
        [InlineData("CON", "_CON")]
        [InlineData("Office.", "Office")]
        [InlineData("  Office  ", "Office")]
        public void CollidingNames_AllGetTheirShortGuid_NotOnlyTheSecond(string name_1, string name_2)
        {
            Space space_1 = new Space(new Guid("a1b2c3d4-0000-0000-0000-000000000001"), name_1, new Point3D(0, 0, 0));
            Space space_2 = new Space(new Guid("e5f60718-0000-0000-0000-000000000002"), name_2, new Point3D(0, 0, 0));
            Space other = new Space("Plant");

            Dictionary<(Guid, string), string> names = Query.SpaceReportPdfFileNames([space_1, space_2, other], [SpaceReportPdf.SpaceDesignLoadSummary]);

            string name(Space space) => names[(space.Guid, SpaceReportPdf.SpaceDesignLoadSummary.Id)];

            Assert.EndsWith(" - Space Design Load Summary [a1b2c3d4].pdf", name(space_1));
            Assert.EndsWith(" - Space Design Load Summary [e5f60718].pdf", name(space_2));
            Assert.NotEqual(name(space_1), name(space_2), StringComparer.OrdinalIgnoreCase);

            //A Space outside the collision keeps its plain name.
            Assert.Equal("Plant - Space Design Load Summary.pdf", name(other));
        }

        [Fact]
        public void NamesEqualAfterTruncation_Collide()
        {
            string prefix = new string('x', 150);
            Space space_1 = new Space(prefix + " first floor");
            Space space_2 = new Space(prefix + " second floor");

            Dictionary<(Guid, string), string> names = Query.SpaceReportPdfFileNames([space_1, space_2], [SpaceReportPdf.SpaceAssumptions]);

            Assert.Equal(prefix + " - Space Assumptions [" + space_1.Guid.ToString("N").Substring(0, 8) + "].pdf", names[(space_1.Guid, SpaceReportPdf.SpaceAssumptions.Id)]);
            Assert.Equal(prefix + " - Space Assumptions [" + space_2.Guid.ToString("N").Substring(0, 8) + "].pdf", names[(space_2.Guid, SpaceReportPdf.SpaceAssumptions.Id)]);
        }

        [Fact]
        public void EmptyNames_FallBackToTheGuid_WhichIsAlreadyUnique()
        {
            Space space_1 = new Space(Guid.NewGuid(), "", new Point3D(0, 0, 0));
            Space space_2 = new Space(Guid.NewGuid(), "???", new Point3D(0, 0, 0));
            Space space_3 = new Space(Guid.NewGuid(), null, new Point3D(0, 0, 0));

            Dictionary<(Guid, string), string> names = Query.SpaceReportPdfFileNames([space_1, space_2, space_3], [SpaceReportPdf.SpaceAssumptions]);

            foreach (Space space in new[] { space_1, space_2, space_3 })
            {
                Assert.Equal("Space " + space.Guid.ToString("D") + " - Space Assumptions.pdf", names[(space.Guid, SpaceReportPdf.SpaceAssumptions.Id)]);
            }
        }

        [Fact]
        public void ShortGuidsThatStillCollide_UseTheFullGuid()
        {
            Space space_1 = new Space(new Guid("a1b2c3d4-0000-0000-0000-000000000001"), "Office", new Point3D(0, 0, 0));
            Space space_2 = new Space(new Guid("a1b2c3d4-0000-0000-0000-000000000002"), "Office", new Point3D(0, 0, 0));
            Space space_3 = new Space(new Guid("0badf00d-0000-0000-0000-000000000003"), "office", new Point3D(0, 0, 0));

            Dictionary<(Guid, string), string> names = Query.SpaceReportPdfFileNames([space_1, space_2, space_3], [SpaceReportPdf.SpaceAssumptions]);

            Assert.Equal("Office - Space Assumptions [a1b2c3d4000000000000000000000001].pdf", names[(space_1.Guid, SpaceReportPdf.SpaceAssumptions.Id)]);
            Assert.Equal("Office - Space Assumptions [a1b2c3d4000000000000000000000002].pdf", names[(space_2.Guid, SpaceReportPdf.SpaceAssumptions.Id)]);
            Assert.Equal("office - Space Assumptions [0badf00d].pdf", names[(space_3.Guid, SpaceReportPdf.SpaceAssumptions.Id)]);
        }

        [Fact]
        public void Names_AreDeterministic_AndIndependentOfOrder()
        {
            List<Space> spaces = Enumerable.Range(0, 200).Select(i => new Space(Guid.NewGuid(), (i % 7 == 0 ? "Office" : i % 5 == 0 ? "OFFICE" : "Room " + (i % 60)), new Point3D(0, 0, 0))).ToList();
            List<SpaceReportPdf> spaceReportPdfs = [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary];

            Dictionary<(Guid, string), string> names = Query.SpaceReportPdfFileNames(spaces, spaceReportPdfs, directory);

            Random random = new Random(1);
            for (int i = 0; i < 5; i++)
            {
                List<Space> shuffled = spaces.OrderBy(_ => random.Next()).ToList();
                Assert.Equal(names.OrderBy(x => x.Key), Query.SpaceReportPdfFileNames(shuffled, spaceReportPdfs.AsEnumerable().Reverse(), directory).OrderBy(x => x.Key));
            }

            Assert.Equal(names.Count, names.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            //Adding an unrelated Space renames nothing.
            Dictionary<(Guid, string), string> names_More = Query.SpaceReportPdfFileNames([.. spaces, new Space("Unrelated")], spaceReportPdfs, directory);
            Assert.All(names, x => Assert.Equal(x.Value, names_More[x.Key]));
        }

        [Fact]
        public void ALongFolder_ShortensTheSpacePart_SoTheStagedPathFitsMaxPath()
        {
            //A folder 150 characters long, whatever the machine's temp path.
            string longDirectory = Path.Combine(directory, new string('d', 150 - Path.GetFullPath(directory).Length - 1));
            Space space_1 = new Space(new string('n', 150));
            Space space_2 = new Space(new string('n', 150) + "x");

            Dictionary<(Guid, string), string> names = Query.SpaceReportPdfFileNames([space_1, space_2, new Space("Office")], [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], longDirectory);

            Assert.All(names.Values, x => Assert.True(Path.Combine(Path.GetFullPath(longDirectory), x).Length + ".tmp".Length <= Query.SpaceReportPdfPathMaxLength, x));
            Assert.Equal(names.Count, names.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal("Office - Space Assumptions.pdf", names.Values.Single(x => x.StartsWith("Office - Space Assumptions")));
        }

        [Fact]
        public void AFolderTooLongForAnyName_IsRefused()
        {
            string tooLong = Path.Combine(directory, new string('d', 240 - Path.GetFullPath(directory).Length - 1));

            Assert.Throws<PathTooLongException>(() => Query.SpaceReportPdfFileNames([new Space("Office")], [SpaceReportPdf.SpaceDesignLoadSummary], tooLong));
        }

        [Fact]
        public void DuplicateSpaceNamesInAModel_ExportWithoutOverwritingEachOther()
        {
            AnalyticalModel analyticalModel = Model(out List<Space> spaces, "Office", "Office", "office");

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions], directory).Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());

            Assert.Equal(3, result.Created);
            Assert.Equal(3, Pdfs().Count);
            Assert.All(spaces, x => Assert.Contains(Pdfs(), y => y.EndsWith("[" + x.Guid.ToString("N").Substring(0, 8) + "].pdf")));
        }

        // ------------------------------------------------------------------ scale

        /// <summary>
        /// Hundreds of Spaces through the real document build (stub renderer): every document shares the one
        /// snapshot, nothing rendered is retained, and the time per document does not grow with position. Timing is
        /// written to the test output, not asserted.
        /// </summary>
        [Fact]
        public void HundredsOfSpaces_OneSnapshot_NothingRetained()
        {
            const int count = 400;
            AnalyticalModel analyticalModel = Model(out _, Enumerable.Range(0, count).Select(i => "Room " + i).ToArray());

            Stopwatch stopwatch = Stopwatch.StartNew();
            SpaceReportPdfBatch batch = SpaceReportPdfBatch.Create(analyticalModel, null, [SpaceReportPdf.SpaceAssumptions, SpaceReportPdf.SpaceDesignLoadSummary], directory);
            TimeSpan setup = stopwatch.Elapsed;

            List<long> ticks = [];
            long previous = 0;

            WeakRenderer weakRenderer = new WeakRenderer();
            SpaceReportPdfBatchResult result = batch.Run(
                SpaceReportPdfExistingFiles.Skip,
                new SynchronousProgress(_ =>
                {
                    long now = stopwatch.ElapsedTicks;
                    if (previous != 0)
                    {
                        ticks.Add(now - previous);
                    }

                    previous = now;
                }),
                default,
                weakRenderer);

            stopwatch.Stop();

            Assert.Equal(2 * count, result.Created);
            Assert.Equal(2 * count, weakRenderer.Count);

            //Every per-document context is WithNewDiagnostics() of the root: the model was copied once.
            List<Analytical.Reporting.DocumentContext> contexts = [];
            SpaceReportPdf recording = new SpaceReportPdf("R", "R", (documentContext, space) => { contexts.Add(documentContext); return SpaceReportPdf.SpaceAssumptions.CreateDocument(documentContext, space); });
            SpaceReportPdfBatch.Create(analyticalModel, batch.Spaces.Take(50).Select(x => x.Guid), [recording], Path.Combine(directory, "r")).Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new CountingRenderer());
            Assert.Single(contexts.Select(x => x.AdjacencyCluster).Distinct());

            //No rendered document is kept once its PDF is on disk.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.True(weakRenderer.Alive() <= 1, weakRenderer.Alive() + " documents still alive");

            double firstQuarter = ticks.Take(ticks.Count / 4).Average();
            double lastQuarter = ticks.Skip(3 * ticks.Count / 4).Average();
            testOutputHelper.WriteLine(
                "{0} Spaces x 2 reports: setup {1:F0} ms, total {2:F0} ms, {3:F2} ms/document; first quarter {4:F2} ms/doc, last quarter {5:F2} ms/doc",
                count,
                setup.TotalMilliseconds,
                stopwatch.Elapsed.TotalMilliseconds,
                stopwatch.Elapsed.TotalMilliseconds / (2 * count),
                firstQuarter * 1000.0 / Stopwatch.Frequency,
                lastQuarter * 1000.0 / Stopwatch.Frequency);
        }

        // ------------------------------------------------------------------ fixtures

        private List<string> Pdfs()
        {
            return Directory.GetFiles(directory, "*.pdf").ToList();
        }

        /// <summary>Bare Spaces (every report prints its not-set/not-simulated states) named as given, in this order.</summary>
        internal static AnalyticalModel Model(out List<Space> spaces, params string[] names)
        {
            return Model(out spaces, null, names);
        }

        private static AnalyticalModel Model(out List<Space> spaces, Action<Space> modify, params string[] names)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            spaces = [];
            for (int i = 0; i < names.Length; i++)
            {
                Space space = new Space(Guid.NewGuid(), names[i], new Point3D(i, 0, 1.5));
                space.SetValue(SpaceParameter.Area, 10.0 + i);
                space.SetValue(SpaceParameter.Volume, 27.0 + i);
                modify?.Invoke(space);
                adjacencyCluster.AddObject(space);
                spaces.Add(space);
            }

            return new AnalyticalModel("Batch", null, null, null, adjacencyCluster);
        }

        private static void Rich(Space space)
        {
            InternalCondition internalCondition = new InternalCondition("S39_Office_OpenPlan_NCM");
            internalCondition.SetValue(InternalConditionParameter.AreaPerPerson, 10.0);
            internalCondition.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, 75.0);
            internalCondition.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, 55.0);
            internalCondition.SetValue(InternalConditionParameter.LightingGainPerArea, 8.0);
            internalCondition.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, 15.0);
            internalCondition.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, 0.25);

            space.InternalCondition = internalCondition;
            space.SetValue(SpaceParameter.SupplyAirFlow, 0.198);
            space.SetValue(SpaceParameter.DesignHeatingLoad, 779.0);
            space.SetValue(SpaceParameter.DesignCoolingLoad, 1429.0);
        }

        internal static List<string> Texts(Document document)
        {
            List<string> result = [];
            foreach (DocumentSection documentSection in document.Sections)
            {
                result.Add(documentSection.Title);
                foreach (DocumentBlock documentBlock in documentSection.Blocks)
                {
                    if (documentBlock is NoticeBlock noticeBlock)
                    {
                        result.Add(noticeBlock.Text);
                    }
                    else if (documentBlock is KeyValueBlock keyValueBlock)
                    {
                        result.AddRange(keyValueBlock.Rows.SelectMany(x => new[] { x.Label, x.Value.Text }));
                    }
                    else if (documentBlock is TableBlock tableBlock)
                    {
                        result.AddRange(tableBlock.Rows.SelectMany(x => x.Cells).Select(x => x.Text));
                    }
                }
            }

            return result;
        }

        /// <summary>IProgress that runs on the calling thread, so a test sees each report before the next document.</summary>
        internal sealed class SynchronousProgress(Action<SpaceReportPdfBatchProgress> action) : IProgress<SpaceReportPdfBatchProgress>
        {
            public void Report(SpaceReportPdfBatchProgress value) => action(value);
        }

        private sealed class CapturingRenderer : IDocumentRenderer
        {
            public List<Document> Documents { get; } = [];

            public string FileExtension => ".pdf";

            public void Render(Document document, Stream stream)
            {
                Documents.Add(document);
                stream.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-stub"));
            }
        }

        /// <summary>Writes a stub PDF and keeps nothing; the <c>failAt</c>-th render (1-based) throws.</summary>
        internal sealed class CountingRenderer(int failAt = 0) : IDocumentRenderer
        {
            public int Count { get; private set; }

            public string FileExtension => ".pdf";

            public void Render(Document document, Stream stream)
            {
                Count++;
                if (Count == failAt)
                {
                    throw new InvalidOperationException("renderer failed");
                }

                stream.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-stub"));
            }
        }

        private sealed class WeakRenderer : IDocumentRenderer
        {
            private readonly List<WeakReference<Document>> documents = [];

            public int Count => documents.Count;

            public string FileExtension => ".pdf";

            public int Alive() => documents.Count(x => x.TryGetTarget(out _));

            public void Render(Document document, Stream stream)
            {
                documents.Add(new WeakReference<Document>(document));
                stream.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-stub"));
            }
        }
    }

    /// <summary>The "Export Space reports..." window and its menu entries (PR2F-2).</summary>
    [Collection(WpfCollection.Name)]
    public class SpaceReportPdfBatchWindowTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_UI_SpaceReportPdfBatchWindowTests_" + Guid.NewGuid().ToString("N"));

        public SpaceReportPdfBatchWindowTests()
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

        [WpfFact]
        public void TheRibbonButton_SitsInTheReportsGroup_AfterTheOneSpaceCommands()
        {
            Windows.AnalyticalWindow analyticalWindow = new Windows.AnalyticalWindow();
            try
            {
                System.Windows.Controls.Ribbon.RibbonGroup ribbonGroup = Assert.IsType<System.Windows.Controls.Ribbon.RibbonGroup>(analyticalWindow.FindName("RibbonGroup_Edit_Reports"));
                List<System.Windows.Controls.Ribbon.RibbonButton> ribbonButtons = ribbonGroup.Items.OfType<System.Windows.Controls.Ribbon.RibbonButton>().ToList();

                Assert.Equal(["RibbonButton_SpaceAssumptionsPdf", "RibbonButton_SpaceDesignLoadSummaryPdf", "RibbonButton_SpaceReportPdfs"], ribbonButtons.Select(x => x.Name));
                Assert.Equal("Export Space Reports", ribbonButtons[2].Label);
                Assert.False(ribbonButtons[2].IsEnabled);
            }
            finally
            {
                analyticalWindow.Close();
            }
        }

        [WpfFact]
        public void TheContextMenuItem_IsEnabledForOneOrManySpaces_AndCarriesTheSelection()
        {
            Space space_1 = new Space("One");
            Space space_2 = new Space("Two");

            MenuItem menuItem = Create.MenuItem_SpaceReportPdfs([space_1, space_2, space_1], null);
            Assert.True(menuItem.IsEnabled);
            Assert.Equal("Export Space reports...", menuItem.Header);
            Assert.Equal("MenuItem_SpaceReportPdfs", menuItem.Name);
            Assert.Equal([space_1.Guid, space_2.Guid], ((IEnumerable<Space>)menuItem.Tag).Select(x => x.Guid));

            Assert.True(Create.MenuItem_SpaceReportPdfs([space_1], null).IsEnabled);
            Assert.False(Create.MenuItem_SpaceReportPdfs([], null).IsEnabled);

            //The one-Space commands are unchanged: several Spaces still disable them.
            Assert.False(Create.MenuItem_SpaceReportPdf([space_1, space_2], SpaceReportPdf.SpaceAssumptions, null).IsEnabled);
        }

        [WpfFact]
        public void TheWindow_DefaultsToTheSelection_OrToAllSpaces_AndNeedsAReport()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out List<Space> spaces, "A", "B", "C");

            SpaceReportPdfBatchWindow selected = new SpaceReportPdfBatchWindow(analyticalModel, [spaces[1]], directory, new SpaceReportPdfPrompts());
            Assert.True(selected.radioButton_Selected.IsChecked);
            Assert.Equal("Selected Spaces (1)", selected.radioButton_Selected.Content);
            Assert.Equal("All Spaces (3)", selected.radioButton_All.Content);
            Assert.False(selected.button_Export.IsEnabled);
            Assert.Equal("Choose at least one report.", selected.textBlock_Hint.Text);

            selected.checkBox_SpaceDesignLoadSummary.IsChecked = true;
            Assert.True(selected.button_Export.IsEnabled);
            selected.Close();

            SpaceReportPdfBatchWindow none = new SpaceReportPdfBatchWindow(analyticalModel, null, null, new SpaceReportPdfPrompts());
            Assert.True(none.radioButton_All.IsChecked);
            Assert.False(none.radioButton_Selected.IsEnabled);
            none.checkBox_SpaceAssumptions.IsChecked = true;
            Assert.False(none.button_Export.IsEnabled);
            Assert.Equal("Choose an output folder.", none.textBlock_Hint.Text);
            none.Close();
        }

        [WpfFact]
        public async System.Threading.Tasks.Task Export_WritesTheBatch_AsksOnceAboutExistingFiles_AndOffersTheFolder()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out List<Space> spaces, "A", "B", "C");
            List<(string Text, MessageBoxButton Button)> messages = [];
            List<string> opened = [];
            MessageBoxResult answer = MessageBoxResult.No;

            SpaceReportPdfPrompts prompts = new SpaceReportPdfPrompts()
            {
                ShowMessage = (text, _, button, _) => { messages.Add((text, button)); return answer; },
                Open = opened.Add,
            };

            SpaceReportPdfBatchWindow window = new SpaceReportPdfBatchWindow(analyticalModel, [spaces[0], spaces[2]], directory, prompts, new SpaceReportPdfBatchTests.CountingRenderer());
            window.checkBox_SpaceAssumptions.IsChecked = true;
            window.checkBox_SpaceDesignLoadSummary.IsChecked = true;

            await window.ExportAsync();

            Assert.Equal(4, window.Result.Created);
            Assert.Empty(messages);
            Assert.Equal(Visibility.Visible, window.stackPanel_Summary.Visibility);
            Assert.StartsWith("Finished 4 documents in ", window.textBlock_Summary.Text);
            Assert.Contains("Created: 4   Skipped: 0   Failed: 0", window.textBlock_Summary.Text);
            Assert.Equal("Close", window.button_Cancel.Content);

            window.button_OpenFolder.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Assert.Equal([directory], opened);

            //All Spaces now: 2 of the 6 PDFs exist. One question; "No" skips them.
            window.radioButton_All.IsChecked = true;
            File.WriteAllText(Path.Combine(directory, "A - Space Assumptions.pdf"), "previous");

            await window.ExportAsync();

            (string text, MessageBoxButton button) = Assert.Single(messages);
            Assert.Equal(MessageBoxButton.YesNoCancel, button);
            Assert.StartsWith("4 of the 6 PDFs are already in the output folder.", text);
            Assert.Equal(2, window.Result.Created);
            Assert.Equal(4, window.Result.Skipped);
            Assert.Equal("previous", File.ReadAllText(Path.Combine(directory, "A - Space Assumptions.pdf")));

            //"Cancel" writes nothing more.
            answer = MessageBoxResult.Cancel;
            int logs = Directory.GetFiles(directory, "*.log").Length;
            SpaceReportPdfBatchResult previousResult = window.Result;

            await window.ExportAsync();

            Assert.Equal(2, messages.Count);
            Assert.Same(previousResult, window.Result);
            Assert.Equal(logs, Directory.GetFiles(directory, "*.log").Length);

            window.Close();
        }

        [WpfFact]
        public async System.Threading.Tasks.Task Export_WhenTheOutputFolderCannotBeCreated_StopsBeforeAnyDocument_WithOneMessage()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out _, "A", "B");

            //A file already sits where the output folder needs to be, so CreateDirectory (inside Run) cannot succeed.
            string blocked = Path.Combine(directory, "blocked");
            File.WriteAllText(blocked, "not a folder");

            List<(string Text, MessageBoxButton Button)> messages = [];
            SpaceReportPdfPrompts prompts = new SpaceReportPdfPrompts()
            {
                ShowMessage = (text, _, button, _) => { messages.Add((text, button)); return MessageBoxResult.OK; },
            };

            SpaceReportPdfBatchWindow window = new SpaceReportPdfBatchWindow(analyticalModel, null, blocked, prompts, new SpaceReportPdfBatchTests.CountingRenderer());
            window.checkBox_SpaceAssumptions.IsChecked = true;

            await window.ExportAsync();

            //Create() and ExistingCount() both succeed (Directory.Exists is false for a file, so existingCount is 0
            //and there is no existing-files prompt); the failure surfaces from Run, through the window's existing
            //"stopped" handling for any Run exception - the same state a mid-run failure would reach.
            Assert.Equal("Space report export stopped", window.textBlock_Heading.Text);
            (string text, MessageBoxButton button) = Assert.Single(messages);
            Assert.Contains(blocked, text);
            Assert.Equal(MessageBoxButton.OK, button);
            Assert.Null(window.Result);
            Assert.Empty(Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories));
            Assert.Empty(Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories));

            window.Close();
        }

        [Fact]
        public void TheSummary_ListsAFewFailures_AndPointsToTheLogForTheRest()
        {
            AnalyticalModel analyticalModel = SpaceReportPdfBatchTests.Model(out _, "A", "B", "C", "D", "E");
            SpaceReportPdf failing = new SpaceReportPdf("FailingPdf", "Failing", (_, _) => throw new InvalidOperationException("boom"));

            SpaceReportPdfBatchResult result = SpaceReportPdfBatch.Create(analyticalModel, null, [failing], directory).Run(SpaceReportPdfExistingFiles.Skip, documentRenderer: new SpaceReportPdfBatchTests.CountingRenderer());

            string summary = SpaceReportPdfBatchWindow.Summary(result);

            Assert.Contains("Created: 0   Skipped: 0   Failed: 5", summary);
            Assert.Contains("A - Failing: The Failing report could not be built for this Space: boom", summary);
            Assert.DoesNotContain("D - Failing", summary);
            Assert.Contains("...and 2 more failures: see the log.", summary);
        }

        [Fact]
        public void TheSuggestedFolder_SitsBesideTheSavedModel()
        {
            string model = Path.Combine(directory, "Project A.sam");

            Assert.Equal(Path.Combine(directory, "Project A Space reports"), Modify.SpaceReportPdfBatchDirectory(model));
            Assert.Null(Modify.SpaceReportPdfBatchDirectory(null));
            Assert.Null(Modify.SpaceReportPdfBatchDirectory(Path.Combine(directory, "missing", "x.sam")));
        }
    }
}
