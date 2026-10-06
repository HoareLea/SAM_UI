// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The ordered record of one Iteration 3 attempt: every stage of
    /// <see cref="PartOIteration3Stage"/>, in declaration order, with what became of it.
    ///
    /// <para><b>Fail closed, structurally</b></para>
    /// <para>
    /// The ledger - not the caller - enforces the pipeline rule. <see cref="Refuse"/> fixes the refused
    /// stage and <b>no later stage can ever be completed</b>: a completion attempted after a refusal is
    /// rejected outright rather than recorded, so a caller that forgets to return early cannot produce a
    /// ledger claiming a stage ran after the run had already stopped. Every stage after the refusal stays
    /// <see cref="PartOIteration3StageStatus.NotRun"/>, which is exactly what a reader of a refused run
    /// needs to see.
    /// </para>
    /// <para>
    /// It also refuses to go backwards or to record a stage twice, because both would mean the caller's
    /// order and the declared order had come apart - and the declared order is what the reader is being
    /// shown.
    /// </para>
    ///
    /// <para><b>Comparison cannot exist unless the chain completed</b></para>
    /// <para>
    /// <see cref="IsComplete"/> is true only where every stage completed. <c>PartOIteration3Result</c>
    /// carries a comparison only where that holds, so a refused run has no comparison object to present at
    /// all rather than an empty one a UI might render.
    /// </para>
    /// </summary>
    public class PartOIteration3Ledger
    {
        private readonly Dictionary<PartOIteration3Stage, PartOIteration3StageState> states = [];

        private static readonly PartOIteration3Stage[] order = (PartOIteration3Stage[])Enum.GetValues(typeof(PartOIteration3Stage));

        /// <summary>Every stage of the pipeline, in the order they are attempted.</summary>
        public static IReadOnlyList<PartOIteration3Stage> Order => order;

        /// <summary>The stage that refused, or null where none has.</summary>
        public PartOIteration3Stage? Stage_Refused { get; private set; }

        /// <summary>Every stage, in pipeline order. Always the complete list, whatever happened.</summary>
        public List<PartOIteration3StageState> Stages
        {
            get
            {
                List<PartOIteration3StageState> result = [];

                foreach (PartOIteration3Stage partOIteration3Stage in order)
                {
                    result.Add(State(partOIteration3Stage));
                }

                return result;
            }
        }

        /// <summary>One stage's row, never null - an unreached stage answers its NOT RUN row.</summary>
        public PartOIteration3StageState State(PartOIteration3Stage partOIteration3Stage)
        {
            return states.TryGetValue(partOIteration3Stage, out PartOIteration3StageState result)
                ? result
                : new PartOIteration3StageState(partOIteration3Stage, PartOIteration3StageStatus.NotRun, NotRunDetail(partOIteration3Stage));
        }

        /// <summary>Whether any stage refused.</summary>
        public bool IsRefused => Stage_Refused.HasValue;

        /// <summary>Whether every stage completed. The only state in which a comparison may exist.</summary>
        public bool IsComplete
        {
            get
            {
                if (IsRefused)
                {
                    return false;
                }

                foreach (PartOIteration3Stage partOIteration3Stage in order)
                {
                    if (!states.TryGetValue(partOIteration3Stage, out PartOIteration3StageState partOIteration3StageState) || !partOIteration3StageState.IsCompleted)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>The refused stage's reasons, verbatim. Empty where nothing refused.</summary>
        public List<string> Reasons => Stage_Refused.HasValue ? State(Stage_Refused.Value).Reasons : [];

        /// <summary>
        /// Every file this attempt created or updated, across every stage, in stage order. Never a
        /// pre-existing file from an earlier attempt.
        /// </summary>
        public List<string> Artifacts
        {
            get
            {
                List<string> result = [];

                foreach (PartOIteration3StageState partOIteration3StageState in Stages)
                {
                    foreach (string artifact in partOIteration3StageState.Artifacts)
                    {
                        result.Add(artifact);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Records a stage as done. Rejected - and nothing is recorded - where the run has already refused,
        /// where the stage is already recorded, or where an earlier stage has not been recorded yet.
        /// </summary>
        /// <returns>Whether the completion was accepted. False means the caller's order is wrong.</returns>
        public bool Complete(PartOIteration3Stage partOIteration3Stage, string detail, IEnumerable<string> artifacts = null)
        {
            if (!CanRecord(partOIteration3Stage))
            {
                return false;
            }

            states[partOIteration3Stage] = new PartOIteration3StageState(partOIteration3Stage, PartOIteration3StageStatus.Completed, detail, null, artifacts);

            return true;
        }

        /// <summary>
        /// Records a stage as refused, verbatim, and stops the run. Subsequent completions are rejected and
        /// every later stage stays NOT RUN.
        /// <para>
        /// A refusal with nothing to say is itself recorded as a defect rather than silently accepted: a
        /// REFUSED with no reason is the one thing a reader cannot act on.
        /// </para>
        /// </summary>
        /// <returns>Whether the refusal was recorded. False means one was already recorded.</returns>
        public bool Refuse(PartOIteration3Stage partOIteration3Stage, string detail, IEnumerable<string> reasons, IEnumerable<string> artifacts = null)
        {
            if (IsRefused || states.ContainsKey(partOIteration3Stage))
            {
                return false;
            }

            List<string> reasons_Temp = [];
            foreach (string reason in reasons ?? [])
            {
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    reasons_Temp.Add(reason);
                }
            }

            if (reasons_Temp.Count == 0)
            {
                reasons_Temp.Add(string.Format("The {0} stage refused and said nothing about why. This is itself a defect.", Core.Query.Description(partOIteration3Stage)));
            }

            states[partOIteration3Stage] = new PartOIteration3StageState(partOIteration3Stage, PartOIteration3StageStatus.Refused, detail, reasons_Temp, artifacts);

            Stage_Refused = partOIteration3Stage;

            return true;
        }

        private bool CanRecord(PartOIteration3Stage partOIteration3Stage)
        {
            if (IsRefused || states.ContainsKey(partOIteration3Stage))
            {
                return false;
            }

            //Every earlier stage must already be recorded, or the declared order and the executed order have
            //come apart - and the declared order is what a reader is shown.
            foreach (PartOIteration3Stage partOIteration3Stage_Earlier in order)
            {
                if (partOIteration3Stage_Earlier == partOIteration3Stage)
                {
                    break;
                }

                if (!states.ContainsKey(partOIteration3Stage_Earlier))
                {
                    return false;
                }
            }

            return true;
        }

        private static string NotRunDetail(PartOIteration3Stage partOIteration3Stage)
        {
            return string.Format("{0} was not reached.", Core.Query.Description(partOIteration3Stage));
        }
    }
}
