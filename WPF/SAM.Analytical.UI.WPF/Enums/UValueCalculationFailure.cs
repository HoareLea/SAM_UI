// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Why a layer-thickness U-value calculation cannot start or did not reach its target. Classified by
    /// <c>Query.UValueCalculationFailure</c>; <c>Query.UValueCalculationMessage</c> words it.
    /// </summary>
    public enum UValueCalculationFailure
    {
        /// <summary>No failure: the calculation can start, or it reached the target within tolerance.</summary>
        None,

        /// <summary>The heat-flow direction is undefined, so Tas cannot calculate a U-value (checked before the call).</summary>
        HeatFlowUndefined,

        /// <summary>No layer may be adjusted: all are gas, glass, or thinner than 10 mm.</summary>
        NoAdjustableLayer,

        /// <summary>Tas / TCD could not run: no result, or an undefined initial U-value.</summary>
        Unavailable,

        /// <summary>The target is not reachable by varying the layer within the thickness range.</summary>
        Unreachable,
    }
}
