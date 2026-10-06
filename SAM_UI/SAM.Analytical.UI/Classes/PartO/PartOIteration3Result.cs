// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The whole answer of one Iteration 3 attempt or review: the ordered ledger, the durable pairing
    /// record, and - only where every stage completed - the A/B comparison.
    ///
    /// <para><b>A refused run carries no comparison, structurally</b></para>
    /// <para>
    /// The constructor drops the comparison unless the ledger is complete, so a presentation layer cannot
    /// render a grid of Candidate B numbers for a run that refused before producing them. That is the same
    /// rule SAM_Systems' materialisation and SAM_Tas' route already follow, for the same reason: an object
    /// that can hold both a refusal and an answer is an object two readers will disagree about.
    /// </para>
    ///
    /// <para><b>No annual series is retained</b></para>
    /// <para>
    /// The two resultant-temperature captures are consumed by the comparison and released. At five
    /// thousand rooms, holding both would be eighty million doubles kept alive for as long as a window is
    /// open, to show statistics that were computed before it opened.
    /// </para>
    /// </summary>
    public class PartOIteration3Result
    {
        private readonly List<string> notes = [];

        public PartOIteration3Result(
            PartOIteration3Ledger partOIteration3Ledger,
            PartOIteration3Record partOIteration3Record,
            PartOIteration3Comparison partOIteration3Comparison,
            PartOIteration3Assessment partOIteration3Assessment_ReferenceA,
            PartOIteration3Assessment partOIteration3Assessment_CandidateB,
            string path_TM59Report_ReferenceA,
            string path_TM59Report_CandidateB,
            string path_Record,
            bool restored,
            IEnumerable<string> notes)
        {
            Ledger = partOIteration3Ledger ?? new PartOIteration3Ledger();
            Record = partOIteration3Record;

            //Fail closed: an incomplete chain has no comparison, whatever the caller passed.
            Comparison = Ledger.IsComplete ? partOIteration3Comparison : null;

            //Kept on a refusal as well, because the reports an assessment DID produce are evidence and
            //the window offers them - but note that no Candidate B NUMBER is presented on a refused run;
            //that is the presentation layer's rule and Comparison being null is what enforces it.
            Assessment_ReferenceA = partOIteration3Assessment_ReferenceA;
            Assessment_CandidateB = partOIteration3Assessment_CandidateB;

            Path_TM59Report_ReferenceA = path_TM59Report_ReferenceA;
            Path_TM59Report_CandidateB = path_TM59Report_CandidateB;
            Path_Record = path_Record;
            IsRestored = restored;

            foreach (string note in notes ?? [])
            {
                if (!string.IsNullOrWhiteSpace(note))
                {
                    this.notes.Add(note);
                }
            }
        }

        /// <summary>Every stage, in pipeline order. Never null.</summary>
        public PartOIteration3Ledger Ledger { get; }

        /// <summary>The pairing record. Null only where the run refused before one could be assembled.</summary>
        public PartOIteration3Record Record { get; }

        /// <summary>The A/B diagnostics. Null on any refusal.</summary>
        public PartOIteration3Comparison Comparison { get; }

        /// <summary>
        /// Reference A's assessment as the existing TM59 authority reported it, or null where it was never
        /// reached. Carried so the window can open the production report an engineer reads.
        /// </summary>
        public PartOIteration3Assessment Assessment_ReferenceA { get; }

        /// <summary>Candidate B's, on the same terms.</summary>
        public PartOIteration3Assessment Assessment_CandidateB { get; }

        /// <summary>Reference A's TM59 report, where one was written.</summary>
        public string Path_TM59Report_ReferenceA { get; }

        /// <summary>Candidate B's TM59 report, where one was written.</summary>
        public string Path_TM59Report_CandidateB { get; }

        /// <summary>Where the pairing record lives.</summary>
        public string Path_Record { get; }

        /// <summary>Whether this was reopened from a record rather than produced by a run this session.</summary>
        public bool IsRestored { get; }

        /// <summary>
        /// Where this review's own human-readable report was written, or null where none was.
        /// <para>
        /// Only a <b>completed</b> pairing writes one. A refusal deliberately writes nothing, so this
        /// pairing's last successful report survives a later refusal instead of being replaced by it -
        /// see <c>Modify.SavePartOIteration3Report</c>.
        /// </para>
        /// </summary>
        public string Path_Report { get; private set; }

        /// <summary>Its structured sibling, on the same terms.</summary>
        public string Path_Report_Json { get; private set; }

        /// <summary>
        /// Why no report was written, where one was expected. Never fatal: the review in front of the
        /// engineer is already correct, and what a failure loses is a copy of it.
        /// </summary>
        public string Refusal_Report { get; private set; }

        /// <summary>
        /// Records what persisting this review's report did.
        /// <para>
        /// Called once, by the attempt that produced this result, immediately after the report is written
        /// or refused - the same shape as <c>PartOIteration3Record.Adopt</c>, and for the same reason: the
        /// outcome is only knowable once the thing it describes exists.
        /// </para>
        /// </summary>
        public void RecordReport(string path_Report, string path_Report_Json, string refusal)
        {
            Path_Report = path_Report;
            Path_Report_Json = path_Report_Json;
            Refusal_Report = refusal;
        }

        /// <summary>What was worth saying that is not a refusal.</summary>
        public List<string> Notes => [.. notes];

        /// <summary>Whether the whole chain completed.</summary>
        public bool IsComplete => Ledger.IsComplete && Comparison is not null;

        /// <summary>Whether a stage refused.</summary>
        public bool IsRefused => Ledger.IsRefused;

        /// <summary>The refused stage's reasons, verbatim.</summary>
        public List<string> Reasons => Ledger.Reasons;

        public override string ToString()
        {
            return IsComplete
                ? string.Format("Iteration 3 {0}: {1}", IsRestored ? "review" : "run", Comparison)
                : string.Format(
                    "Iteration 3 {0} REFUSED at {1}.",
                    IsRestored ? "review" : "run",
                    Ledger.Stage_Refused.HasValue ? Core.Query.Description(Ledger.Stage_Refused.Value) : "an unrecorded stage");
        }
    }
}
