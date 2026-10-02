// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What a glazing candidate row is marked for before it is chosen.</summary>
    public enum GlazingWarningKind
    {
        /// <summary>The system's Default Panel Type is of another panel group than the panels carrying the apertures (ModelCheck would warn).</summary>
        PanelGroup,

        /// <summary>The system has no frame layers while the current one has: the apertures would lose their frame.</summary>
        Frameless,

        /// <summary>A material of the system is missing from its source, or differs from the model's material of the same name: it cannot be applied.</summary>
        Material,
    }
}
