// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What the Thermal Performance panel lists.</summary>
    public enum ThermalPerformanceMode
    {
        /// <summary>The selected panels and apertures, by construction.</summary>
        Selection,

        /// <summary>Every construction of the external envelope: walls, roofs, floors, windows and doors.</summary>
        WholeEnvelope,
    }
}
