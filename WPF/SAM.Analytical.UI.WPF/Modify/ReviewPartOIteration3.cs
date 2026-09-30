// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Reopens a persisted Approved Document O Iteration 3 pairing and rebuilds its comparison from
        /// the results that already exist - <b>without running TAS</b>.
        ///
        /// <para><b>The promise, and what enforces it</b></para>
        /// <para>
        /// Nothing here starts <c>TBD.exe</c>, <c>TSD.exe</c>, <c>TAS3D.exe</c> or <c>TPD.exe</c>, and no
        /// TAS file is written or touched. That is not a convention: the only pipeline member this
        /// reaches is <see cref="IPartOIteration3Pipeline.Assess"/>, which reads a results file through
        /// the TSD reader the ordinary Review Results command already uses. The four members that run TAS
        /// are never called, which a test can assert by handing in a pipeline whose other members throw.
        /// </para>
        ///
        /// <para><b>Validated before anything is read</b></para>
        /// <list type="number">
        /// <item>The record parses and is of this build's schema.</item>
        /// <item>It names <b>these</b> results - the run being reviewed is the Reference A it was written
        /// against.</item>
        /// <item>Reference A's design state and overheating scenarios still match the fingerprints the
        /// record copied from its own provenance, so the pairing is not being shown against a design that
        /// has moved.</item>
        /// <item>Every Candidate B file the record names is still exactly the file it recorded - present,
        /// same length, same write time. A changed or missing one refuses <b>by name</b>. The two TM59
        /// reports are the exception: every assessment rewrites its report, this review's included, so they
        /// are lineage rather than comparison authority. They are neither validated nor offered from the
        /// record - a review offers only the reports its own assessments wrote.</item>
        /// <item>Candidate B's persisted model records its provenance to the <b>bridge</b> results, which
        /// is where its resultant temperatures were read from.</item>
        /// </list>
        ///
        /// <para><b>A refused record shows its ledger and nothing else</b></para>
        /// <para>
        /// Where the recorded pairing did not complete, the ledger is the whole answer: the refused stage,
        /// its reasons verbatim, the artifacts that attempt genuinely produced, and the stages that never
        /// ran. No assessment is read and no Candidate B number is produced, because there are none - and
        /// producing some from the files that happen to be on disk is exactly the failure this design is
        /// built against.
        /// </para>
        ///
        /// <para><b>Deterministic</b></para>
        /// <para>
        /// The rebuild uses the record's own room set, its own dwelling grouping and its own bound rooms,
        /// captures exactly those rooms on both sides, and orders everything by guid before walking it.
        /// So re-exporting an unchanged completed pairing produces the same text every time, on any
        /// machine.
        /// </para>
        /// </summary>
        /// <param name="partORun">The Part O run whose results the pairing was written against.</param>
        /// <param name="iPartOIteration3Pipeline">Only <see cref="IPartOIteration3Pipeline.Assess"/> is used.</param>
        /// <param name="partOIteration3BehaviourMode">
        /// Which method's pairing to reopen - resolved by <see cref="Query.PartOIteration3RecordPath"/>, so a
        /// pairing written before per-method records existed is still found by the mode recorded inside it.
        /// Null reopens the mode-independent legacy record where there is one, and otherwise the first method
        /// with a record, in <see cref="Query.PartOIteration3BehaviourModes"/> order.
        /// </param>
        /// <param name="step">Told, in words, which step is starting - for a progress window. Optional.</param>
        public static PartOIteration3Result ReviewPartOIteration3(PartORun partORun, IPartOIteration3Pipeline iPartOIteration3Pipeline, PartOIteration3BehaviourMode? partOIteration3BehaviourMode = null, Action<string> step = null)
        {
            List<string> notes = [];

            PartOIteration3Ledger partOIteration3Ledger = new();

            if (partORun is null || iPartOIteration3Pipeline is null)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "No Part O run or no pipeline was supplied.", ["No Part O run or no pipeline was supplied, so no Iteration 3 pairing could be reopened."]);

                return new PartOIteration3Result(partOIteration3Ledger, null, null, null, null, null, null, null, true, notes);
            }

            step?.Invoke("Reading the saved Iteration 3 result");

            string path_TSD_ReferenceA = partORun.Path_TSD;
            string path_Record = Path_Record_Review(path_TSD_ReferenceA, partOIteration3BehaviourMode);

            PartOIteration3Record partOIteration3Record = Query.PartOIteration3PairingRecord(path_Record);

            //A method's own record that names another method is not that method's result.
            if (partOIteration3Record is not null && partOIteration3BehaviourMode.HasValue && partOIteration3Record.BehaviourMode != partOIteration3BehaviourMode.Value)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.Input,
                    "The saved Iteration 3 record is for another method.",
                    [string.Format("The record at '{0}' was written for '{1}', not for '{2}'.", path_Record, partOIteration3Record.BehaviourMode, partOIteration3BehaviourMode.Value)]);

                return new PartOIteration3Result(partOIteration3Ledger, null, null, null, null, null, null, path_Record, true, notes);
            }

            if (partOIteration3Record is null)
            {
                partOIteration3Ledger.Refuse(
                    PartOIteration3Stage.Input,
                    "There is no Iteration 3 pairing to reopen.",
                    [string.Format("No Approved Document O Iteration 3 pairing record could be read at '{0}'.", path_Record ?? "<no path>")]);

                return new PartOIteration3Result(partOIteration3Ledger, null, null, null, null, null, null, path_Record, true, notes);
            }

            //The ledger of the RECORDED attempt is what a review shows; it is never rebuilt here. What
            //this method adds is either the rebuilt comparison, or its own refusal to trust the record.
            PartOIteration3Ledger partOIteration3Ledger_Recorded = Recorded(partOIteration3Record);

            List<string> refusals = Query.PartOIteration3ReviewRefusals(partORun, partOIteration3Record, out AnalyticalModel analyticalModel_CandidateB, out string path_TSD_CandidateB);

            if (refusals.Count != 0)
            {
                //A record that cannot be trusted is shown as an Input refusal of THIS review, on a fresh
                //ledger - not as the recorded attempt's verdict, which may well have completed. The two
                //are different statements and a reader must not have them merged.
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "This Iteration 3 pairing can no longer be shown.", refusals);

                return new PartOIteration3Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, null, path_Record, true, notes);
            }

            if (!partOIteration3Record.IsComplete)
            {
                //The recorded attempt refused. Its ledger IS the answer, and nothing is read.
                notes.Add("This Approved Document O Iteration 3 attempt refused, so it produced no comparison. Its stage ledger is the record of what happened and how far it got.");

                return new PartOIteration3Result(partOIteration3Ledger_Recorded, partOIteration3Record, null, null, null, null, null, path_Record, true, notes);
            }

            //---------------------------------------------------------------------------------------------
            //Re-read the two EXISTING results files through the same unchanged TM59 authority.
            //---------------------------------------------------------------------------------------------
            List<PartOIteration3BindingRecord> bindings = partOIteration3Record.Bindings;

            List<Guid> guids_Space_Bound = [];
            Dictionary<Guid, PartOIteration3Room> dictionary_Room = [];

            foreach (PartOIteration3BindingRecord partOIteration3BindingRecord in bindings)
            {
                guids_Space_Bound.Add(partOIteration3BindingRecord.Guid_Space);

                dictionary_Room[partOIteration3BindingRecord.Guid_Space] = new PartOIteration3Room(
                    partOIteration3BindingRecord.Guid_Space,
                    partOIteration3BindingRecord.Name_Space,
                    partOIteration3BindingRecord.Guid_Dwelling,
                    partOIteration3BindingRecord.Name_Dwelling);
            }

            guids_Space_Bound.Sort();

            step?.Invoke("Re-assessing the reference case from its existing results");

            PartOIteration3Assessment partOIteration3Assessment_A = iPartOIteration3Pipeline.Assess(partORun.AnalyticalModel_Assessment, path_TSD_ReferenceA, partORun.OverheatingScenarios, guids_Space_Bound);

            List<OverheatingScenario> overheatingScenarios_CandidateB = [];
            if (analyticalModel_CandidateB.TryGetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, out SAMCollection<OverheatingScenario> collection) && collection is not null)
            {
                foreach (OverheatingScenario overheatingScenario in collection)
                {
                    if (overheatingScenario is not null)
                    {
                        overheatingScenarios_CandidateB.Add(overheatingScenario);
                    }
                }
            }

            step?.Invoke("Re-assessing the system case from its existing results");

            PartOIteration3Assessment partOIteration3Assessment_B = iPartOIteration3Pipeline.Assess(analyticalModel_CandidateB, path_TSD_CandidateB, overheatingScenarios_CandidateB, guids_Space_Bound);

            if (partOIteration3Assessment_A is null || !partOIteration3Assessment_A.IsAssessed || partOIteration3Assessment_B is null || !partOIteration3Assessment_B.IsAssessed)
            {
                List<string> reasons = [];

                if (partOIteration3Assessment_A is null || !partOIteration3Assessment_A.IsAssessed)
                {
                    reasons.Add(partOIteration3Assessment_A?.Refusal ?? string.Format("Reference A's results at '{0}' could not be reassessed.", path_TSD_ReferenceA));
                }

                if (partOIteration3Assessment_B is null || !partOIteration3Assessment_B.IsAssessed)
                {
                    reasons.Add(partOIteration3Assessment_B?.Refusal ?? string.Format("Candidate B's results at '{0}' could not be reassessed.", path_TSD_CandidateB));
                }

                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "This Iteration 3 pairing could not be reassessed from its existing results.", reasons);

                return new PartOIteration3Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, null, path_Record, true, notes);
            }

            //---------------------------------------------------------------------------------------------
            //Rebuild the reconciliation and the comparison from the record's own bindings.
            //---------------------------------------------------------------------------------------------
            step?.Invoke("Rebuilding the comparison");

            List<string> refusals_Reconciliation = Query.PartOIteration3ReviewReconciliationRefusals(
                partOIteration3Record,
                partOIteration3Assessment_A,
                partOIteration3Assessment_B,
                dictionary_Room,
                out List<PartOIteration3Room> rooms_Comparable,
                out List<PartOIteration3CriterionComparison> criteria);

            if (refusals_Reconciliation.Count != 0)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "This Iteration 3 pairing no longer reconciles.", refusals_Reconciliation);

                return new PartOIteration3Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, null, path_Record, true, notes);
            }

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                rooms_Comparable,
                partOIteration3Assessment_A.ResultantTemperatures,
                partOIteration3Assessment_B.ResultantTemperatures,
                criteria,
                out List<string> refusals_Comparison);

            if (partOIteration3Comparison is null)
            {
                partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "This Iteration 3 pairing's comparison could not be rebuilt.", refusals_Comparison);

                return new PartOIteration3Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, null, path_Record, true, notes);
            }

            notes.Add(string.Format(
                "Rebuilt from the existing results at '{0}' and '{1}'. No TAS simulation was run and no TAS file was written.",
                path_TSD_ReferenceA,
                path_TSD_CandidateB));

            //Only the reports THIS review's assessments wrote. The record's report entries are lineage the
            //review did not validate, so they are never offered in place of a report this review failed
            //to write.
            NoteUnwrittenReport(notes, PartOIteration3Roles.ReferenceA_TM59Report, partOIteration3Assessment_A);
            NoteUnwrittenReport(notes, PartOIteration3Roles.CandidateB_TM59Report, partOIteration3Assessment_B);

            PartOIteration3Result partOIteration3Result = new(
                partOIteration3Ledger_Recorded,
                partOIteration3Record,
                partOIteration3Comparison,
                partOIteration3Assessment_A,
                partOIteration3Assessment_B,
                partOIteration3Assessment_A.Path_Report,
                partOIteration3Assessment_B.Path_Report,
                path_Record,
                true,
                notes);

            //A successful review IS a successful A/B result, so it persists its report on the same terms
            //a run does. Every refusal above returns before reaching this, which is what keeps a refused
            //review from replacing the report a successful one wrote.
            SavePartOIteration3Report(partOIteration3Result);

            return partOIteration3Result;
        }

        /// <summary>
        /// The record a review reads. With a method: that method's record, legacy-aware. Without one: the
        /// legacy mode-independent record where it exists - what every caller of the one-method era meant -
        /// and otherwise the first method that has a record.
        /// </summary>
        private static string Path_Record_Review(string path_TSD, PartOIteration3BehaviourMode? partOIteration3BehaviourMode)
        {
            if (partOIteration3BehaviourMode.HasValue)
            {
                return Query.PartOIteration3RecordPath(path_TSD, partOIteration3BehaviourMode.Value, out bool _);
            }

            string path_Legacy = PartOIteration3Paths.Path_Record_ForResults(path_TSD);

            if (!string.IsNullOrWhiteSpace(path_Legacy) && System.IO.File.Exists(path_Legacy))
            {
                return path_Legacy;
            }

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode_Candidate in Query.PartOIteration3BehaviourModes)
            {
                //The method's own record where it exists - in Iteration 3's reports folder, or beside the results
                //where a pairing written before that folder existed put it.
                string path = Query.PartOIteration3RecordPath(path_TSD, partOIteration3BehaviourMode_Candidate, out bool legacy);

                if (!legacy && !string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                {
                    return path;
                }
            }

            return path_Legacy;
        }

        private static void NoteUnwrittenReport(List<string> notes, string role, PartOIteration3Assessment partOIteration3Assessment)
        {
            if (string.IsNullOrWhiteSpace(partOIteration3Assessment.Path_Report))
            {
                notes.Add(string.Format("This review wrote no {0}, so none is offered. {1}", role, partOIteration3Assessment.Refusal_Report ?? "The assessment named no report."));
            }
        }

        /// <summary>
        /// The recorded attempt's ledger, rebuilt from the record so the review shows exactly what that
        /// run said - including the stages that never ran.
        /// <para>
        /// Replayed through the live <see cref="PartOIteration3Ledger"/> rather than trusted as a list,
        /// so the ordering rule that governed the run governs what is shown: a record claiming a stage
        /// completed after a refusal is rejected here exactly as it would have been there.
        /// </para>
        /// <para>
        /// <b>A pre-PR5A (v1) record has no <c>EquipmentResolution</c> stage</b>, because the stage did not
        /// exist - and the ordering rule would otherwise drop every stage after it. For v1 only, the stage
        /// is replayed as what it would have said, the Parity no-op, immediately after a completed
        /// <c>SystemScope</c>. Nothing else is inferred, and a v2 record is replayed exactly as written.
        /// </para>
        /// </summary>
        private static PartOIteration3Ledger Recorded(PartOIteration3Record partOIteration3Record)
        {
            PartOIteration3Ledger result = new();

            bool isLegacy_V1 = partOIteration3Record?.IsLegacy_V1 ?? false;

            foreach (PartOIteration3StageState partOIteration3StageState in partOIteration3Record?.Stages ?? [])
            {
                if (isLegacy_V1
                    && partOIteration3StageState.Stage != PartOIteration3Stage.EquipmentResolution
                    && result.State(PartOIteration3Stage.SystemScope).IsCompleted
                    && result.State(PartOIteration3Stage.EquipmentResolution).Status == PartOIteration3StageStatus.NotRun)
                {
                    result.Complete(
                        PartOIteration3Stage.EquipmentResolution,
                        string.Format("Recorded before PR5A ('{0}'): Parity mode, Candidate B0. No product was resolved.", PartOIteration3Record.LegacySchema_V1));
                }

                switch (partOIteration3StageState.Status)
                {
                    case PartOIteration3StageStatus.Completed:
                        result.Complete(partOIteration3StageState.Stage, partOIteration3StageState.Detail, partOIteration3StageState.Artifacts);
                        break;

                    case PartOIteration3StageStatus.Refused:
                        result.Refuse(partOIteration3StageState.Stage, partOIteration3StageState.Detail, partOIteration3StageState.Reasons, partOIteration3StageState.Artifacts);
                        break;
                }
            }

            return result;
        }
    }
}
