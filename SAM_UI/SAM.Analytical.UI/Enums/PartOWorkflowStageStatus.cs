// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What the Prepare &amp; Run dialog says about one <see cref="PartOWorkflowStage"/>.
    /// <para>
    /// <b>Only <see cref="Blocked"/> stops Run.</b> Everything else is either already true, about to be made
    /// true by the run, or not part of this scenario. A stage the production authority merely warns about
    /// stays <see cref="Ready"/> with the warning in its detail line: turning a warning into a UI blocker
    /// would refuse models the pipeline itself accepts.
    /// </para>
    /// </summary>
    public enum PartOWorkflowStageStatus
    {
        /// <summary>Exists on the model, valid for this request, and used as it stands.</summary>
        [Description("READY")] Ready,

        /// <summary>Exists from an earlier step of this session and is reused rather than rebuilt.</summary>
        [Description("REUSED")] Reused,

        /// <summary>Missing or incompatible, and this run generates it.</summary>
        [Description("NEEDS PREPARATION")] Prepare,

        /// <summary>Missing, and nothing this run does can supply it. The only status that stops Run.</summary>
        [Description("REQUIRED")] Blocked,

        /// <summary>Not part of the requested scenario or scope.</summary>
        [Description("N/A")] NotApplicable,

        /// <summary>Happens during the run, on the model the run produces.</summary>
        [Description("PENDING")] Pending,

        /// <summary>Nothing has produced it yet.</summary>
        [Description("NOT RUN")] NotRun,
    }
}
