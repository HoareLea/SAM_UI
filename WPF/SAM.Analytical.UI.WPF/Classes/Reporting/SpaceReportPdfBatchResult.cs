// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What a batch export does with a PDF that is already in the output folder. Chosen once per batch.</summary>
    public enum SpaceReportPdfExistingFiles
    {
        /// <summary>Leave the existing PDF and count the document as skipped.</summary>
        Skip,

        /// <summary>Replace it (staged, so a failure keeps the old PDF).</summary>
        Overwrite,
    }

    public enum SpaceReportPdfBatchStatus
    {
        Created,

        /// <summary>A PDF was already at the path and the batch skips existing files.</summary>
        Skipped,

        Failed,
    }

    /// <summary>One document of a batch export: one Space, one report.</summary>
    public sealed class SpaceReportPdfBatchItem
    {
        internal SpaceReportPdfBatchItem(Guid spaceGuid, string? spaceName, SpaceReportPdf spaceReportPdf, string? path, SpaceReportPdfBatchStatus status, SpaceReportPdfFailure failure, string? message, Exception? exception, IReadOnlyList<string>? notes)
        {
            SpaceGuid = spaceGuid;
            SpaceName = spaceName;
            SpaceReportPdf = spaceReportPdf;
            Path = path;
            Status = status;
            Failure = failure;
            Message = message;
            Exception = exception;
            Notes = notes ?? Array.Empty<string>();
        }

        public Guid SpaceGuid { get; }

        public string? SpaceName { get; }

        public SpaceReportPdf SpaceReportPdf { get; }

        /// <summary>The planned PDF path; null when the Space could not be resolved.</summary>
        public string? Path { get; }

        public SpaceReportPdfBatchStatus Status { get; }

        /// <summary>The stage that failed; <see cref="SpaceReportPdfFailure.None"/> unless <see cref="Status"/> is Failed.</summary>
        public SpaceReportPdfFailure Failure { get; }

        public string? Message { get; }

        public Exception? Exception { get; }

        /// <summary>This document's own reporting diagnostics (its <c>WithNewDiagnostics()</c> log), already printed in the PDF.</summary>
        public IReadOnlyList<string> Notes { get; }
    }

    /// <summary>
    /// The outcome of <see cref="SpaceReportPdfBatch.Run"/>. Documents not reached before a cancellation are counted
    /// in <see cref="NotStarted"/> and have no item.
    /// </summary>
    public sealed class SpaceReportPdfBatchResult
    {
        internal SpaceReportPdfBatchResult(IReadOnlyList<SpaceReportPdfBatchItem> items, int documentCount, bool cancelled, string? logPath, string? logError, TimeSpan elapsed)
        {
            Items = items;
            DocumentCount = documentCount;
            Cancelled = cancelled;
            LogPath = logPath;
            LogError = logError;
            Elapsed = elapsed;

            foreach (SpaceReportPdfBatchItem item in items)
            {
                switch (item.Status)
                {
                    case SpaceReportPdfBatchStatus.Created:
                        Created++;
                        break;

                    case SpaceReportPdfBatchStatus.Skipped:
                        Skipped++;
                        break;

                    default:
                        Failed++;
                        break;
                }
            }
        }

        public IReadOnlyList<SpaceReportPdfBatchItem> Items { get; }

        /// <summary>Documents planned: Spaces × report types.</summary>
        public int DocumentCount { get; }

        public int Created { get; }

        public int Skipped { get; }

        public int Failed { get; }

        /// <summary>Documents never started because the batch was cancelled.</summary>
        public int NotStarted => DocumentCount - Items.Count;

        public bool Cancelled { get; }

        /// <summary>The batch log in the output folder; null when it could not be written (see <see cref="LogError"/>).</summary>
        public string? LogPath { get; }

        public string? LogError { get; }

        public TimeSpan Elapsed { get; }
    }

    /// <summary>Progress of a batch export, reported before each document.</summary>
    public sealed class SpaceReportPdfBatchProgress
    {
        internal SpaceReportPdfBatchProgress(int spaceIndex, int spaceCount, string? spaceName, SpaceReportPdf spaceReportPdf, int documentIndex, int documentCount)
        {
            SpaceIndex = spaceIndex;
            SpaceCount = spaceCount;
            SpaceName = spaceName;
            SpaceReportPdf = spaceReportPdf;
            DocumentIndex = documentIndex;
            DocumentCount = documentCount;
        }

        /// <summary>1-based.</summary>
        public int SpaceIndex { get; }

        public int SpaceCount { get; }

        public string? SpaceName { get; }

        public SpaceReportPdf SpaceReportPdf { get; }

        /// <summary>0-based count of documents finished before this one.</summary>
        public int DocumentIndex { get; }

        public int DocumentCount { get; }
    }
}
