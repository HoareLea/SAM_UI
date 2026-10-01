// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Which apertures "Set glazing" assigns the chosen glazing system to.</summary>
    public enum GlazingApplyScope
    {
        /// <summary>Default: every aperture using the current aperture construction (matched by Guid).</summary>
        AllApertures,

        /// <summary>Only the selected apertures that use the current aperture construction.</summary>
        SelectedApertures,

        /// <summary>Add the chosen system to the model without assigning it to any aperture.</summary>
        DontAssign,
    }
}
