// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the Part O TM59 result window heads itself with. <b>Presentation only</b> - it is chosen by
    /// <see cref="PartOTM59ResultSummary"/> from the production assessment's own combined status and the
    /// existing Part O guards, and nothing persists it.
    /// </summary>
    public enum PartOTM59Verdict
    {
        /// <summary>No assessment could be produced: the run's results are missing, stale or unreadable.</summary>
        Unavailable,

        /// <summary>
        /// An assessment ran but reached no pass or fail that may be reported - no occupied space produced a
        /// verdict, or the spaces that did pass are only part of the Part O dwelling scope.
        /// </summary>
        NotAssessed,

        /// <summary>The production assessment's occupied-space status is Pass over the whole dwelling scope.</summary>
        Pass,

        /// <summary>The production assessment's occupied-space status is Fail.</summary>
        Fail,
    }
}
