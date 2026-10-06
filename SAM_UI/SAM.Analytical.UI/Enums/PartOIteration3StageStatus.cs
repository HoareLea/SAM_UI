// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What the Iteration 3 ledger says about one <see cref="PartOIteration3Stage"/>.
    /// <para>
    /// <b>Three states, and no fourth.</b> There is deliberately no "warning", "partial" or "skipped":
    /// a stage either did its work, refused, or was never reached. A partial stage is the state this whole
    /// pipeline exists to make unreachable - see <see cref="PartOIteration3Ledger"/>.
    /// </para>
    /// </summary>
    public enum PartOIteration3StageStatus
    {
        /// <summary>Never reached. Every stage after a refusal stays here.</summary>
        [Description("NOT RUN")] NotRun,

        /// <summary>Did its work, and said what it produced.</summary>
        [Description("COMPLETED")] Completed,

        /// <summary>Refused, with at least one verbatim reason. Nothing after it runs.</summary>
        [Description("REFUSED")] Refused,
    }
}
