// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>The outcome of one Glazing System Builder performance request.</summary>
    public enum DraftGlazingEvaluationState
    {
        /// <summary>Tas calculated Ug / g / LT (and Uf for a framed system), or the same content was calculated before.</summary>
        Calculated,

        /// <summary>No values: the draft has errors (Tas is not asked), Tas is unavailable, or Tas returned nothing usable.</summary>
        NotCalculated,

        /// <summary>A newer request replaced this one; its result (if any) must not be shown as current.</summary>
        Superseded,
    }
}
