// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What the model stores for a row of the Thermal Performance panel: the provenance of the value shown.</summary>
    public enum ThermalStoredState
    {
        /// <summary>Every element of the row stores the same value (written by Tas when the model was last calculated).</summary>
        Stored,

        /// <summary>The elements of the row store different values, or only some of them store one: the number shown is not one number.</summary>
        Varies,

        /// <summary>No element of the row stores a value: nothing was calculated, so none is shown as if it were accurate.</summary>
        NotCalculated,
    }
}
