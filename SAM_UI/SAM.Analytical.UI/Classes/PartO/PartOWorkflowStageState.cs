// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One line of the Prepare &amp; Run status list: which stage, what state it is in, and one sentence
    /// saying why.
    /// <para>
    /// <b>The detail is the point.</b> A status word on its own tells a person that Run is blocked without
    /// telling them what to do about it, so every state carries a sentence naming what was looked for and -
    /// where it is missing - the existing command that produces it.
    /// </para>
    /// </summary>
    public class PartOWorkflowStageState
    {
        public PartOWorkflowStageState(PartOWorkflowStage partOWorkflowStage, PartOWorkflowStageStatus partOWorkflowStageStatus, string detail)
        {
            Stage = partOWorkflowStage;
            Status = partOWorkflowStageStatus;
            Detail = detail;
        }

        public PartOWorkflowStageState(PartOWorkflowStage partOWorkflowStage, PartOWorkflowStageStatus partOWorkflowStageStatus, string detail, string summary)
            : this(partOWorkflowStage, partOWorkflowStageStatus, detail)
        {
            Summary = summary;
        }

        public PartOWorkflowStage Stage { get; }

        public PartOWorkflowStageStatus Status { get; }

        /// <summary>One sentence: what exists, what will be built, or what is missing and where it comes from.</summary>
        public string Detail { get; }

        /// <summary>
        /// A compact first-level line - "3 dwellings · 8 spaces", "8/8 spaces mapped" - stated from the same
        /// counts as <see cref="Detail"/>, or null where the detail's own first sentence is short enough.
        /// Presentation only: it carries nothing the detail does not.
        /// </summary>
        public string Summary { get; }

        /// <summary>The stage's name, from the enum's own description. No second spelling.</summary>
        public string Name => Core.Query.Description(Stage);

        /// <summary>The status word, from the enum's own description. No second spelling.</summary>
        public string StatusText => Core.Query.Description(Status);

        /// <summary>Whether this stage stops Run. Only <see cref="PartOWorkflowStageStatus.Blocked"/> does.</summary>
        public bool IsBlocking => Status == PartOWorkflowStageStatus.Blocked;

        public override string ToString()
        {
            return string.Format("{0}: {1} - {2}", Name, StatusText, Detail);
        }
    }
}
