// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The one-line outcome of the scoped check "Set U-value" runs after Apply (built by
    /// <c>Query.UValueCheckSummary</c>); the full <see cref="Log"/> is one click away in the existing LogWindow.
    /// </summary>
    public sealed class UValueCheckSummary
    {
        internal UValueCheckSummary(Log log, int errors, int warnings, int messages, string text)
        {
            Log = log ?? new Log();
            Errors = errors;
            Warnings = warnings;
            Messages = messages;
            Text = text;
        }

        /// <summary>The records of the check (distinct by text), for "Details".</summary>
        public Log Log { get; }

        public int Errors { get; }

        public int Warnings { get; }

        /// <summary>Informational records; they do not change the outcome.</summary>
        public int Messages { get; }

        /// <summary>True when the check found no error and no warning.</summary>
        public bool Passed => Errors == 0 && Warnings == 0;

        /// <summary>A status glyph, so the outcome never depends on colour alone: ✓ passed, ⚠ warnings only, ✕ errors.</summary>
        public string Glyph => Errors > 0 ? "✕" : Warnings > 0 ? "⚠" : "✓";

        /// <summary>E.g. "No errors or warnings for SIM_EXT_SLD U0.30 and its 12 panels."</summary>
        public string Text { get; }
    }
}
