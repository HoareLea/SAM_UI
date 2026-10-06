// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What is on disk for ONE Iteration 3 method against one Reference A: nothing, a completed pairing, or
    /// the last attempt's refusal.
    ///
    /// <para><b>Only a completed pairing is reviewable as the answer</b></para>
    /// <para>
    /// A refused attempt is kept because its ledger is the diagnosis, but it never makes Review the primary
    /// action: the engineer who has just fixed the reason must be able to run the method again without
    /// deleting anything by hand. <see cref="IsReviewable"/> is that rule, and it is the one every caller
    /// reads.
    /// </para>
    /// </summary>
    public class PartOIteration3PairingStatus
    {
        internal PartOIteration3PairingStatus(PartOIteration3BehaviourMode partOIteration3BehaviourMode, string path_Record, bool legacy, PartOIteration3Record partOIteration3Record, string refusal_Read)
        {
            BehaviourMode = partOIteration3BehaviourMode;
            Path_Record = path_Record;
            IsLegacy = legacy;
            Record = partOIteration3Record;
            Refusal_Read = refusal_Read;
        }

        public PartOIteration3BehaviourMode BehaviourMode { get; }

        /// <summary>The record this method resolves to, whether or not a file is there.</summary>
        public string Path_Record { get; }

        /// <summary>Whether the record is the mode-independent one written before per-method records existed.</summary>
        public bool IsLegacy { get; }

        /// <summary>The parsed record, or null where there is none or it could not be read.</summary>
        public PartOIteration3Record Record { get; }

        /// <summary>Why a record file that exists could not be read, or null.</summary>
        public string Refusal_Read { get; }

        /// <summary>Whether any attempt of this method has been recorded and could be read.</summary>
        public bool Exists => Record is not null;

        /// <summary>Whether the recorded attempt completed - the only state that offers Review as the answer.</summary>
        public bool IsReviewable => Record is not null && Record.IsComplete;

        /// <summary>Whether the last recorded attempt refused.</summary>
        public bool IsRefused => Record is not null && !Record.IsComplete;

        /// <summary>When the recorded attempt was made, in local time, or null.</summary>
        public DateTime? When => Record is null || Record.Ticks_Utc <= 0 ? null : new DateTime(Record.Ticks_Utc, DateTimeKind.Utc).ToLocalTime();

        public override string ToString()
        {
            return string.Format("{0}: {1}", BehaviourMode, IsReviewable ? "completed" : IsRefused ? "refused" : Refusal_Read ?? "none");
        }
    }
}
