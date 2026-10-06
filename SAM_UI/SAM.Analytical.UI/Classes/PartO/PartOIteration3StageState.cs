// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One row of the Iteration 3 stage ledger: which stage, what became of it, what it said, and which
    /// files <b>this attempt</b> created or updated at it.
    /// <para>
    /// <b>The reasons are verbatim.</b> A refusal raised by SAM_Systems, SAM_Tas or the TM59 authority is
    /// carried across word for word. Rewording another authority's refusal is how an orchestration quietly
    /// becomes a second opinion about what that authority decided.
    /// </para>
    /// <para>
    /// <b>The artifacts are this attempt's, not the directory's.</b> Candidate B writes to deterministic
    /// paths, so an earlier attempt's <c>.tbd</c>, <c>.tsd</c>, <c>.tpd</c> or TM59 report may already be
    /// sitting at exactly the path this attempt would use. A file is recorded here only where this attempt
    /// demonstrably created or updated it - see <c>PartOIteration3Artifacts</c>, which is what decides that.
    /// A stale file reported as evidence of a stage that never ran is worse than no evidence at all.
    /// </para>
    /// </summary>
    public class PartOIteration3StageState
    {
        private readonly List<string> reasons = [];

        private readonly List<string> artifacts = [];

        public PartOIteration3StageState(PartOIteration3Stage partOIteration3Stage, PartOIteration3StageStatus partOIteration3StageStatus, string detail, IEnumerable<string> reasons = null, IEnumerable<string> artifacts = null)
        {
            Stage = partOIteration3Stage;
            Status = partOIteration3StageStatus;
            Detail = detail;

            foreach (string reason in reasons ?? [])
            {
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    this.reasons.Add(reason);
                }
            }

            foreach (string artifact in artifacts ?? [])
            {
                if (!string.IsNullOrWhiteSpace(artifact))
                {
                    this.artifacts.Add(artifact);
                }
            }
        }

        public PartOIteration3Stage Stage { get; }

        public PartOIteration3StageStatus Status { get; }

        /// <summary>One sentence: what this stage did, or what it was waiting for.</summary>
        public string Detail { get; }

        /// <summary>Every refusal reason, verbatim and in order. Empty unless <see cref="Status"/> is refused.</summary>
        public List<string> Reasons => [.. reasons];

        /// <summary>Files THIS attempt created or updated at this stage. Never a pre-existing one.</summary>
        public List<string> Artifacts => [.. artifacts];

        /// <summary>The stage's name, from the enum's own description. No second spelling.</summary>
        public string Name => Core.Query.Description(Stage);

        /// <summary>The status word, from the enum's own description. No second spelling.</summary>
        public string StatusText => Core.Query.Description(Status);

        public bool IsRefused => Status == PartOIteration3StageStatus.Refused;

        public bool IsCompleted => Status == PartOIteration3StageStatus.Completed;

        public override string ToString()
        {
            return string.Format("{0}: {1} - {2}", Name, StatusText, Detail);
        }
    }
}
