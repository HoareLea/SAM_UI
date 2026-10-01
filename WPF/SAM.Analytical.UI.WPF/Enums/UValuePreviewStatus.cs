// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>State of the "Set U-value" preview. Only <see cref="Reached"/> can be applied.</summary>
    public enum UValuePreviewStatus
    {
        /// <summary>Nothing to show yet (no valid target).</summary>
        None,

        /// <summary>An evaluation is running.</summary>
        Calculating,

        /// <summary>The target is reached within tolerance; the preview can be applied.</summary>
        Reached,

        /// <summary>The target equals the current U-value: nothing to change.</summary>
        NoChange,

        /// <summary>The target cannot be reached within the thickness range; the best achievable U is stated.</summary>
        Unreachable,

        /// <summary>The calculation cannot run (heat-flow direction, no adjustable layer, Tas unavailable, invalid input).</summary>
        Failed,
    }
}
