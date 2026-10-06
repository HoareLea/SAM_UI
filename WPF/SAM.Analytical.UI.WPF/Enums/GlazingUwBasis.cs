// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>How the overall U-value (Uw) of a candidate was obtained.</summary>
    public enum GlazingUwBasis
    {
        /// <summary>Not available (no usable Ug / Uf).</summary>
        None,

        /// <summary>Area-weighted over the affected apertures' actual pane and frame areas with the candidate applied.</summary>
        Area,

        /// <summary>Approximate: 80 % pane, 20 % frame, because the apertures' areas are not available.</summary>
        Approximate,
    }
}
