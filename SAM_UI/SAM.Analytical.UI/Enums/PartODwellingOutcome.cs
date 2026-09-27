// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What one simulation says about one dwelling - a screening cell, or a dwelling's final mixed-run result.
    /// <para>
    /// <b>Only a simulation produces <see cref="Pass"/> or <see cref="Fail"/>.</b> A strategy that was not
    /// simulated for a dwelling is <see cref="NotRun"/>; a simulation that ran but reached no verdict for it is
    /// <see cref="NotAssessed"/>; a strategy that cannot be screened in this build is <see cref="Unavailable"/>.
    /// None of those is ever shown, stored or read as a pass.
    /// </para>
    /// <para>Members are appended, never reordered: they are persisted by name.</para>
    /// </summary>
    public enum PartODwellingOutcome
    {
        [Description("—")] Undefined,

        /// <summary>Every occupied space of the dwelling passed, and none of its spaces went unassessed.</summary>
        [Description("PASS")] Pass,

        /// <summary>At least one occupied space of the dwelling failed.</summary>
        [Description("FAIL")] Fail,

        /// <summary>The strategy was simulated, but no pass or fail was reached for this dwelling.</summary>
        [Description("NOT ASSESSED")] NotAssessed,

        /// <summary>The strategy was not simulated for this dwelling.</summary>
        [Description("NOT RUN")] NotRun,

        /// <summary>The strategy cannot be screened in this build, or for this project.</summary>
        [Description("UNAVAILABLE")] Unavailable,
    }
}
