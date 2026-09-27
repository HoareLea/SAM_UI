// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Which stage of <see cref="Modify.WriteSpaceReportPdf"/> failed. Missing engineering data is not a
    /// failure: the reporting framework prints it in the document (—, n/a, not set, notices).
    /// </summary>
    public enum SpaceReportPdfFailure
    {
        None,

        /// <summary>SAM.Analytical.Reporting threw while collecting the Space or building the document.</summary>
        Document,

        /// <summary>The PDF renderer threw (fonts, resources, font resolver, layout).</summary>
        Rendering,

        /// <summary>The rendered PDF could not be written to the chosen path (permissions, locked file, bad path).</summary>
        Output,
    }

    /// <summary>
    /// The outcome of one Space report PDF run (<see cref="SpaceReportPdf"/>). A failure keeps its exception for
    /// diagnostics; nothing is swallowed.
    /// </summary>
    public sealed class SpaceReportPdfResult
    {
        private SpaceReportPdfResult(string? path, SpaceReportPdfFailure failure, string? message, Exception? exception, long length, IReadOnlyList<string>? notes)
        {
            Path = path;
            Failure = failure;
            Message = message;
            Exception = exception;
            Length = length;
            Notes = notes ?? Array.Empty<string>();
        }

        /// <summary>The PDF path chosen by the user.</summary>
        public string? Path { get; }

        public SpaceReportPdfFailure Failure { get; }

        public bool Succeeded => Failure == SpaceReportPdfFailure.None;

        /// <summary>A concise message for the user; null on success.</summary>
        public string? Message { get; }

        /// <summary>The exception behind a failure; null on success.</summary>
        public Exception? Exception { get; }

        /// <summary>Bytes written; 0 on failure.</summary>
        public long Length { get; }

        /// <summary>
        /// The reporting framework's expected-missing-data warnings (DocumentContext.Diagnostics). They are
        /// already printed in the PDF; they are carried here only for diagnostics and tests.
        /// </summary>
        public IReadOnlyList<string> Notes { get; }

        internal static SpaceReportPdfResult Created(string path, long length, IReadOnlyList<string> notes)
        {
            return new SpaceReportPdfResult(path, SpaceReportPdfFailure.None, null, null, length, notes);
        }

        internal static SpaceReportPdfResult Failed(string? path, SpaceReportPdfFailure failure, string message, Exception? exception)
        {
            return new SpaceReportPdfResult(path, failure, message, exception, 0, null);
        }
    }
}
