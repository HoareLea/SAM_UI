// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Which panels "Set U-value" assigns the new construction to. Only meaningful for
    /// <see cref="UValueApplyMode.NewConstruction"/>: modifying in place always affects every panel using the construction.
    /// </summary>
    public enum UValueApplyScope
    {
        /// <summary>Default: every panel using the source construction (matched by construction Guid).</summary>
        AllPanels,

        /// <summary>Only the selected panels that use the source construction.</summary>
        SelectedPanels,

        /// <summary>Create the new construction without assigning it to any panel.</summary>
        DontAssign,
    }
}
