// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>How serious a Glazing System Builder authoring issue is. Only <see cref="Error"/> blocks calculation and Save.</summary>
    public enum GlazingDraftIssueSeverity
    {
        /// <summary>Worth knowing (e.g. the system has no frame).</summary>
        Info,

        /// <summary>Allowed but probably not intended (e.g. two panes in contact).</summary>
        Warning,

        /// <summary>The system cannot be calculated or saved as it is.</summary>
        Error,
    }
}
