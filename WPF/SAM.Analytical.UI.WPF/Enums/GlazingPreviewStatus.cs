// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>State of the "Set glazing" comparison. Only <see cref="Ready"/> with a chosen candidate can be applied.</summary>
    public enum GlazingPreviewStatus
    {
        /// <summary>The candidates' values are being calculated (Tas TCD).</summary>
        Calculating,

        /// <summary>The comparison is shown.</summary>
        Ready,

        /// <summary>The values could not be calculated (Tas unavailable).</summary>
        Failed,
    }
}
