// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>How "Set U-value" changes the construction.</summary>
    public enum UValueApplyMode
    {
        /// <summary>Default: a new construction (e.g. "SIM_EXT_SLD U0.50") with the adjusted layer; the source stays unchanged.</summary>
        NewConstruction,

        /// <summary>The source construction itself is changed, so every panel using it changes.</summary>
        ModifyInPlace,
    }
}
