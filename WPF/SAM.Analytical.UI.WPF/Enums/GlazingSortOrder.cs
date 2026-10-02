// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// How the list of glazing candidates is ordered (<see cref="GlazingViewModel.SortOrder"/>). Only the order shown changes: the filters, the
    /// automatic choice against a target Uw (always the best Uw that meets it) and the values are the same in every order. A value not calculated
    /// is listed last.
    /// </summary>
    public enum GlazingSortOrder
    {
        /// <summary>Best (lowest) overall U-value first; the default.</summary>
        OverallU,

        /// <summary>Lowest g-value first (solar control).</summary>
        GLowest,

        /// <summary>Highest g-value first (solar gain).</summary>
        GHighest,

        /// <summary>Highest light transmittance first.</summary>
        LightHighest,

        /// <summary>By name.</summary>
        Name,
    }
}
