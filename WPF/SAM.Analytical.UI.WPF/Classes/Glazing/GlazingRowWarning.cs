// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One thing to know about a glazing candidate <i>before</i> it is chosen: shown as a short marker on its row and, once
    /// the candidate is chosen, as a full sentence among the warnings above Apply.
    /// </summary>
    public sealed class GlazingRowWarning
    {
        internal GlazingRowWarning(GlazingWarningKind kind, string shortText, string text, bool blocks)
        {
            Kind = kind;
            ShortText = shortText;
            Text = text;
            Blocks = blocks;
        }

        public GlazingWarningKind Kind { get; }

        /// <summary>The marker on the row, e.g. "made for roofs".</summary>
        public string ShortText { get; }

        /// <summary>The full sentence.</summary>
        public string Text { get; }

        /// <summary>True when the system cannot be applied because of it (shown as the block reason, not as a warning).</summary>
        public bool Blocks { get; }
    }
}
