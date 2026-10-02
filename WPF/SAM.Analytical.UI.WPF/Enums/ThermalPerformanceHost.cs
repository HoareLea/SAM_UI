// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Where the Thermal Performance panel is shown. The control and its view-model are the same in every host; the host only
    /// decides which window or grid cell holds the control.
    /// </summary>
    public enum ThermalPerformanceHost
    {
        /// <summary>Not shown.</summary>
        Hidden,

        /// <summary>A column of the analytical window, beside the view tabs (the default).</summary>
        Docked,

        /// <summary>A modeless window owned by the analytical window; it can be moved to another monitor.</summary>
        Floating,
    }
}
