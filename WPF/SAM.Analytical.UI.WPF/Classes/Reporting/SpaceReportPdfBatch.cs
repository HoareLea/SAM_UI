// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using SAM.Core.Reporting.Pdf;
using SAM.Units;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// A batch export of Space report PDFs ("Export Space reports..."): many Spaces × one or more
    /// <see cref="SpaceReportPdf"/> reports into one folder, with one log. UI-free; the window
    /// (<see cref="SpaceReportPdfBatchWindow"/>) drives it from a background task.
    /// <para>
    /// <see cref="Create"/> copies the model ONCE into a shared <see cref="Analytical.Reporting.DocumentContext"/>,
    /// resolves the Spaces from that snapshot and plans every file name. <see cref="Run"/> then writes the documents
    /// one at a time, each through <see cref="Modify.WriteSpaceReportPdf(Analytical.Reporting.DocumentContext, Space, string?, SpaceReportPdf, IDocumentRenderer?)"/>
    /// over <c>WithNewDiagnostics()</c> of the shared context, so each document's diagnostics are its own and no
    /// document copies the model again. Every PDF is on disk before the next is built; nothing is kept but a small
    /// result per document. Sequential by design: the PDF font stack is process-global and not proven thread-safe.
    /// </para>
    /// <para>
    /// Orchestration only: the documents are exactly SAM.Analytical.Reporting's, in its default units (SI).
    /// </para>
    /// </summary>
    public sealed class SpaceReportPdfBatch
    {
        private readonly Analytical.Reporting.DocumentContext documentContext;
        private readonly List<Space> spaces;
        private readonly Dictionary<(Guid SpaceGuid, string ReportId), string> fileNames;

        private SpaceReportPdfBatch(Analytical.Reporting.DocumentContext documentContext, List<Space> spaces, List<Guid> missingSpaceGuids, List<SpaceReportPdf> spaceReportPdfs, string directory, bool allSpaces, Dictionary<(Guid SpaceGuid, string ReportId), string> fileNames)
        {
            this.documentContext = documentContext;
            this.spaces = spaces;
            this.fileNames = fileNames;
            MissingSpaceGuids = missingSpaceGuids;
            SpaceReportPdfs = spaceReportPdfs;
            Directory = directory;
            AllSpaces = allSpaces;
        }

        /// <summary>
        /// Takes the snapshot and plans the batch. <paramref name="spaceGuids"/> null means every Space in the model;
        /// otherwise the Spaces with these Guids, in model order. A Guid no longer in the model is kept as a failed
        /// document per report rather than dropped.
        /// </summary>
        /// <exception cref="ArgumentException">No report type, or no Space to report.</exception>
        /// <exception cref="PathTooLongException">The folder leaves no room for the file names.</exception>
        public static SpaceReportPdfBatch Create(AnalyticalModel analyticalModel, IEnumerable<Guid>? spaceGuids, IEnumerable<SpaceReportPdf> spaceReportPdfs, string directory, UnitStyle unitStyle = UnitStyle.SI)
        {
            if (analyticalModel == null)
            {
                throw new ArgumentNullException(nameof(analyticalModel));
            }

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Choose an output folder.", nameof(directory));
            }

            List<SpaceReportPdf> spaceReportPdfs_Unique = spaceReportPdfs?.Where(x => x != null).GroupBy(x => x.Id).Select(x => x.First()).ToList() ?? new List<SpaceReportPdf>();
            if (spaceReportPdfs_Unique.Count == 0)
            {
                throw new ArgumentException("Choose at least one report.", nameof(spaceReportPdfs));
            }

            directory = System.IO.Path.GetFullPath(directory);

            //The one model copy of the batch (AnalyticalModel.AdjacencyCluster copies the cluster).
            Analytical.Reporting.DocumentContext documentContext = Analytical.Reporting.Create.DocumentContext(analyticalModel, new DocumentOptions() { UnitSystem = unitStyle });

            List<Space> spaces_Model = documentContext.AdjacencyCluster?.GetSpaces() ?? new List<Space>();

            List<Space> spaces;
            List<Guid> missingSpaceGuids = new List<Guid>();
            if (spaceGuids == null)
            {
                spaces = spaces_Model.Where(x => x != null).GroupBy(x => x.Guid).Select(x => x.First()).ToList();
            }
            else
            {
                List<Guid> spaceGuids_Unique = spaceGuids.Distinct().ToList();
                HashSet<Guid> spaceGuids_Set = new HashSet<Guid>(spaceGuids_Unique);

                spaces = spaces_Model.Where(x => x != null && spaceGuids_Set.Contains(x.Guid)).GroupBy(x => x.Guid).Select(x => x.First()).ToList();

                HashSet<Guid> spaceGuids_Found = new HashSet<Guid>(spaces.Select(x => x.Guid));
                missingSpaceGuids = spaceGuids_Unique.Where(x => !spaceGuids_Found.Contains(x)).ToList();
            }

            if (spaces.Count == 0 && missingSpaceGuids.Count == 0)
            {
                throw new ArgumentException(spaceGuids == null ? "The model has no Spaces." : "Select at least one Space.", nameof(spaceGuids));
            }

            Dictionary<(Guid SpaceGuid, string ReportId), string> fileNames = Query.SpaceReportPdfFileNames(spaces, spaceReportPdfs_Unique, directory);

            return new SpaceReportPdfBatch(documentContext, spaces, missingSpaceGuids, spaceReportPdfs_Unique, directory, spaceGuids == null, fileNames);
        }

        /// <summary>True for "All Spaces", false for "Selected Spaces".</summary>
        public bool AllSpaces { get; }

        public string Directory { get; }

        public IReadOnlyList<SpaceReportPdf> SpaceReportPdfs { get; }

        /// <summary>The Spaces to report, from the batch's model snapshot.</summary>
        public IReadOnlyList<Space> Spaces => spaces;

        /// <summary>Selected Spaces that are no longer in the model.</summary>
        public IReadOnlyList<Guid> MissingSpaceGuids { get; }

        public int DocumentCount => (spaces.Count + MissingSpaceGuids.Count) * SpaceReportPdfs.Count;

        /// <summary>The shared context every document derives its own from. Exposed for tests.</summary>
        internal Analytical.Reporting.DocumentContext DocumentContext => documentContext;

        /// <summary>The planned PDF path of one Space's report.</summary>
        public string Path(Guid spaceGuid, SpaceReportPdf spaceReportPdf)
        {
            return System.IO.Path.Combine(Directory, fileNames[(spaceGuid, spaceReportPdf.Id)]);
        }

        /// <summary>How many planned PDFs are already in the folder: the one question to ask before <see cref="Run"/>.</summary>
        public int ExistingCount()
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                return 0;
            }

            return fileNames.Values.Count(x => File.Exists(System.IO.Path.Combine(Directory, x)));
        }

        /// <summary>
        /// Writes every document, in Space then report order, and the log. A failed document is recorded and the
        /// batch goes on. Cancellation is honoured between documents: the document in hand is finished (or, on
        /// failure, its staging file removed), completed PDFs are kept and the log says the run was cancelled.
        /// </summary>
        /// <param name="documentRenderer">Null means <see cref="PdfRenderer"/>; tests pass a stand-in.</param>
        public SpaceReportPdfBatchResult Run(SpaceReportPdfExistingFiles existingFiles, IProgress<SpaceReportPdfBatchProgress>? progress = null, CancellationToken cancellationToken = default, IDocumentRenderer? documentRenderer = null)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            DateTime started = DateTime.Now;

            documentRenderer ??= new PdfRenderer();

            List<SpaceReportPdfBatchItem> items = new List<SpaceReportPdfBatchItem>();
            bool cancelled = false;

            string? logPath = null;
            string? logError = null;
            StreamWriter? streamWriter = null;
            try
            {
                System.IO.Directory.CreateDirectory(Directory);

                logPath = LogPath(Directory, started);
                streamWriter = new StreamWriter(logPath, false, new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                logError = exception.Message;
                logPath = null;
                streamWriter = null;
            }

            try
            {
                WriteLog(streamWriter, ref logError, x => WriteHeader(x, started, existingFiles));

                foreach (Guid spaceGuid in MissingSpaceGuids)
                {
                    foreach (SpaceReportPdf spaceReportPdf in SpaceReportPdfs)
                    {
                        SpaceReportPdfBatchItem item = new SpaceReportPdfBatchItem(spaceGuid, null, spaceReportPdf, null, SpaceReportPdfBatchStatus.Failed, SpaceReportPdfFailure.Selection, "The selected Space is no longer in the model.", null, null);
                        items.Add(item);
                        WriteLog(streamWriter, ref logError, x => WriteItem(x, item, items.Count));
                    }
                }

                for (int i = 0; i < spaces.Count && !cancelled; i++)
                {
                    Space space = spaces[i];

                    foreach (SpaceReportPdf spaceReportPdf in SpaceReportPdfs)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            cancelled = true;
                            break;
                        }

                        progress?.Report(new SpaceReportPdfBatchProgress(i + 1, spaces.Count, space.Name, spaceReportPdf, items.Count, DocumentCount));

                        SpaceReportPdfBatchItem item = WriteDocument(space, spaceReportPdf, existingFiles, documentRenderer);
                        items.Add(item);

                        if (item.Status == SpaceReportPdfBatchStatus.Failed)
                        {
                            Trace.TraceError("Space report export: {0} ({1}) failed for '{2}': {3}", spaceReportPdf.Title, item.Failure, item.Path, item.Exception);
                        }

                        WriteLog(streamWriter, ref logError, x => WriteItem(x, item, items.Count));
                    }
                }

                stopwatch.Stop();

                SpaceReportPdfBatchResult result = new SpaceReportPdfBatchResult(items, DocumentCount, cancelled, logPath, logError, stopwatch.Elapsed);

                WriteLog(streamWriter, ref logError, x => WriteFooter(x, result));

                return logError == null ? result : new SpaceReportPdfBatchResult(items, DocumentCount, cancelled, null, logError, stopwatch.Elapsed);
            }
            finally
            {
                try
                {
                    streamWriter?.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        private SpaceReportPdfBatchItem WriteDocument(Space space, SpaceReportPdf spaceReportPdf, SpaceReportPdfExistingFiles existingFiles, IDocumentRenderer documentRenderer)
        {
            string path = Path(space.Guid, spaceReportPdf);

            if (existingFiles == SpaceReportPdfExistingFiles.Skip && File.Exists(path))
            {
                return new SpaceReportPdfBatchItem(space.Guid, space.Name, spaceReportPdf, path, SpaceReportPdfBatchStatus.Skipped, SpaceReportPdfFailure.None, "A PDF is already at this path.", null, null);
            }

            SpaceReportPdfResult spaceReportPdfResult;
            try
            {
                //A fresh diagnostics log over the shared snapshot: this document's warnings, and only its.
                spaceReportPdfResult = Modify.WriteSpaceReportPdf(documentContext.WithNewDiagnostics(), space, path, spaceReportPdf, documentRenderer);
            }
            catch (Exception exception)
            {
                return new SpaceReportPdfBatchItem(space.Guid, space.Name, spaceReportPdf, path, SpaceReportPdfBatchStatus.Failed, SpaceReportPdfFailure.Document, exception.Message, exception, null);
            }

            if (!spaceReportPdfResult.Succeeded)
            {
                return new SpaceReportPdfBatchItem(space.Guid, space.Name, spaceReportPdf, path, SpaceReportPdfBatchStatus.Failed, spaceReportPdfResult.Failure, spaceReportPdfResult.Message, spaceReportPdfResult.Exception, null);
            }

            return new SpaceReportPdfBatchItem(space.Guid, space.Name, spaceReportPdf, spaceReportPdfResult.Path ?? path, SpaceReportPdfBatchStatus.Created, SpaceReportPdfFailure.None, null, null, spaceReportPdfResult.Notes);
        }

        // ------------------------------------------------------------------ log

        /// <summary>"Space reports yyyy-MM-dd HHmmss.log", with " (2)", " (3)"... when a run in the same second left one.</summary>
        private static string LogPath(string directory, DateTime started)
        {
            string name = "Space reports " + started.ToString("yyyy-MM-dd HHmmss", CultureInfo.InvariantCulture);

            string path = System.IO.Path.Combine(directory, name + ".log");
            for (int i = 2; File.Exists(path); i++)
            {
                path = System.IO.Path.Combine(directory, string.Format(CultureInfo.InvariantCulture, "{0} ({1}).log", name, i));
            }

            return path;
        }

        /// <summary>A log that cannot be written never stops the PDFs: the first error is kept and logging stops.</summary>
        private static void WriteLog(StreamWriter? streamWriter, ref string? logError, Action<StreamWriter> write)
        {
            if (streamWriter == null || logError != null)
            {
                return;
            }

            try
            {
                write(streamWriter);
                streamWriter.Flush();
            }
            catch (Exception exception)
            {
                logError = exception.Message;
            }
        }

        private void WriteHeader(StreamWriter streamWriter, DateTime started, SpaceReportPdfExistingFiles existingFiles)
        {
            streamWriter.WriteLine("SAM Space report export");
            streamWriter.WriteLine("Run started:     {0}", Timestamp(started));
            streamWriter.WriteLine("Model:           {0}", string.IsNullOrWhiteSpace(documentContext.AnalyticalModel.Name) ? "-" : documentContext.AnalyticalModel.Name);
            streamWriter.WriteLine("Scope:           {0}", AllSpaces ? "All Spaces" : "Selected Spaces");
            streamWriter.WriteLine("Spaces:          {0}{1}", spaces.Count, MissingSpaceGuids.Count == 0 ? string.Empty : string.Format(" (+ {0} selected but no longer in the model)", MissingSpaceGuids.Count));
            streamWriter.WriteLine("Report types:    {0}", string.Join(", ", SpaceReportPdfs.Select(x => x.Name)));
            streamWriter.WriteLine("Units:           {0}", documentContext.Options.UnitSystem);
            streamWriter.WriteLine("Output folder:   {0}", Directory);
            streamWriter.WriteLine("Existing files:  {0}", existingFiles == SpaceReportPdfExistingFiles.Overwrite ? "Overwrite" : "Skip");
            streamWriter.WriteLine("Documents:       {0}", DocumentCount);
            streamWriter.WriteLine();
        }

        private void WriteItem(StreamWriter streamWriter, SpaceReportPdfBatchItem item, int number)
        {
            string status = item.Status switch
            {
                SpaceReportPdfBatchStatus.Created => "Created",
                SpaceReportPdfBatchStatus.Skipped => "Skipped",
                _ => "FAILED",
            };

            string space = string.Format("{0} ({1})", string.IsNullOrWhiteSpace(item.SpaceName) ? "-" : item.SpaceName, item.SpaceGuid.ToString("D"));

            streamWriter.WriteLine("[{0}/{1}] {2} | {3} | {4} | {5}", number, DocumentCount, status, space, item.SpaceReportPdf.Name, item.Path == null ? "-" : System.IO.Path.GetFileName(item.Path));

            if (item.Status == SpaceReportPdfBatchStatus.Failed)
            {
                streamWriter.WriteLine("    stage: {0}", item.Failure);
                streamWriter.WriteLine("    error: {0}", OneLine(item.Message));
                if (item.Exception != null)
                {
                    streamWriter.WriteLine("    exception: {0}: {1}", item.Exception.GetType().FullName, OneLine(item.Exception.Message));
                    for (Exception? exception = item.Exception.InnerException; exception != null; exception = exception.InnerException)
                    {
                        streamWriter.WriteLine("    inner: {0}: {1}", exception.GetType().FullName, OneLine(exception.Message));
                    }
                }
            }
            else if (item.Status == SpaceReportPdfBatchStatus.Skipped)
            {
                streamWriter.WriteLine("    {0}", item.Message);
            }

            foreach (string note in item.Notes)
            {
                streamWriter.WriteLine("    note: {0}", OneLine(note));
            }
        }

        private static void WriteFooter(StreamWriter streamWriter, SpaceReportPdfBatchResult result)
        {
            streamWriter.WriteLine();
            streamWriter.WriteLine("Run finished:    {0} (elapsed {1})", Timestamp(DateTime.Now), result.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
            streamWriter.WriteLine("Result:          {0}", result.Cancelled ? string.Format("CANCELLED after {0} of {1} documents", result.Items.Count, result.DocumentCount) : "Completed");
            streamWriter.WriteLine("Created: {0}  Skipped: {1}  Failed: {2}  Not started: {3}", result.Created, result.Skipped, result.Failed, result.NotStarted);
        }

        private static string Timestamp(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string OneLine(string? text)
        {
            return string.IsNullOrEmpty(text) ? "-" : text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
