// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Which elements "Set U-value" (panels) and "Set glazing" (apertures) change: one scope for both, so the choice means
    /// the same wherever a thermal change is made.
    /// </summary>
    public enum ThermalApplyScope
    {
        /// <summary>Default: every element using the source construction (matched by construction Guid).</summary>
        AllUsing,

        /// <summary>Only the selected elements that use the source construction.</summary>
        SelectedOnly,

        /// <summary>Create the new construction / add the chosen system without assigning it to any element.</summary>
        DontAssign,
    }
}
