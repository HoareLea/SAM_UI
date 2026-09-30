// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// The phases an Iteration 3 run is shown as - the ledger's fifteen stages, in the engineer's terms,
        /// each one real work the run performs. The manufacturer-guidance (or cooling-module) evaluation is a
        /// phase only for the method that performs it, so a run never lists work it does not do.
        /// </summary>
        /// <param name="resumed">
        /// Whether the run reuses this session's earlier attempt's TAS work (<see cref="PartOIteration3ResumePlan.Reuse"/>):
        /// its TAS phases are then one "Reusing the completed TAS results" phase, because none of that work is done.
        /// </param>
        internal static IReadOnlyList<string> PartOIteration3Phases(PartOIteration3BehaviourMode partOIteration3BehaviourMode, bool resumed = false)
        {
            return PartOIteration3PhaseList(partOIteration3BehaviourMode, resumed);
        }

        private static List<string> PartOIteration3PhaseList(PartOIteration3BehaviourMode partOIteration3BehaviourMode, bool resumed = false)
        {
            if (resumed)
            {
                return
                [
                    PartOProgressStages.PreparingSystemCase,
                    PartOProgressStages.ReusingTasResults,
                    PartOProgressStages.AssessingTm59,
                    PartOProgressStages.ComparingAndSaving,
                ];
            }

            List<string> result =
            [
                PartOProgressStages.PreparingSystemCase,
                PartOProgressStages.BuildingSimulationThermalSource,
                PartOProgressStages.CreatingVentilationSystems,
                PartOProgressStages.RunningTasSystems,
            ];

            switch (partOIteration3BehaviourMode)
            {
                case PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance:
                    result.Add(PartOProgressStages.EvaluatingManufacturerGuidance);
                    break;

                case PartOIteration3BehaviourMode.SelectedProductCooling:
                    result.Add(PartOProgressStages.EvaluatingCoolingModules);
                    break;
            }

            result.Add(PartOProgressStages.CalculatingResultantTemperatures);
            result.Add(PartOProgressStages.AssessingTm59);
            result.Add(PartOProgressStages.ComparingAndSaving);

            return result;
        }

        /// <summary>Which of <see cref="PartOIteration3Phases"/> a ledger stage belongs to, for this method.</summary>
        internal static int PartOIteration3Phase(PartOIteration3Stage partOIteration3Stage, PartOIteration3BehaviourMode partOIteration3BehaviourMode, bool resumed = false)
        {
            List<string> phases = PartOIteration3PhaseList(partOIteration3BehaviourMode, resumed);

            if (resumed)
            {
                //The materialisation and every TAS stage are the kept work being checked, not done.
                return phases.IndexOf(partOIteration3Stage switch
                {
                    PartOIteration3Stage.Input or PartOIteration3Stage.ReferenceA or PartOIteration3Stage.ReferenceATM59 or PartOIteration3Stage.SystemScope or PartOIteration3Stage.EquipmentResolution => PartOProgressStages.PreparingSystemCase,
                    PartOIteration3Stage.Materialisation or PartOIteration3Stage.ThermalSource or PartOIteration3Stage.SystemsConversion or PartOIteration3Stage.SystemsSimulation or PartOIteration3Stage.ZoneTemperature or PartOIteration3Stage.ResultantTemperature => PartOProgressStages.ReusingTasResults,
                    PartOIteration3Stage.CandidateBTM59 => PartOProgressStages.AssessingTm59,
                    _ => PartOProgressStages.ComparingAndSaving,
                });
            }

            //ZoneTemperature is where the route's read-back evidence is checked and kept, so it belongs to the
            //evaluation phase where the method has one, and to the systems run where it does not.
            string name = partOIteration3Stage switch
            {
                PartOIteration3Stage.Input or PartOIteration3Stage.ReferenceA or PartOIteration3Stage.ReferenceATM59 or PartOIteration3Stage.SystemScope or PartOIteration3Stage.EquipmentResolution or PartOIteration3Stage.Materialisation => PartOProgressStages.PreparingSystemCase,
                PartOIteration3Stage.ThermalSource => PartOProgressStages.BuildingSimulationThermalSource,
                PartOIteration3Stage.SystemsConversion => PartOProgressStages.CreatingVentilationSystems,
                PartOIteration3Stage.SystemsSimulation => PartOProgressStages.RunningTasSystems,
                PartOIteration3Stage.ZoneTemperature => phases.Contains(PartOProgressStages.EvaluatingManufacturerGuidance)
                    ? PartOProgressStages.EvaluatingManufacturerGuidance
                    : phases.Contains(PartOProgressStages.EvaluatingCoolingModules)
                        ? PartOProgressStages.EvaluatingCoolingModules
                        : PartOProgressStages.RunningTasSystems,
                PartOIteration3Stage.ResultantTemperature => PartOProgressStages.CalculatingResultantTemperatures,
                PartOIteration3Stage.CandidateBTM59 => PartOProgressStages.AssessingTm59,
                _ => PartOProgressStages.ComparingAndSaving,
            };

            return phases.IndexOf(name);
        }

        /// <summary>
        /// The step shown beside a phase when one of its stages starts, for the stages that are a distinct
        /// piece of work inside a phase. Null where the phase's own name says it, or where the work reports
        /// finer steps itself (the building simulation, the TAS Systems route).
        /// </summary>
        internal static string? PartOIteration3StageDetail(PartOIteration3Stage partOIteration3Stage, bool resumed = false)
        {
            if (resumed)
            {
                //Said as what happens: the kept work is checked and used, and none of it is run again.
                switch (partOIteration3Stage)
                {
                    case PartOIteration3Stage.Materialisation:
                        return "Ventilation systems from the earlier attempt - not created again";

                    case PartOIteration3Stage.ThermalSource:
                        return "TAS building simulation (thermal source) - reused, not run again";

                    case PartOIteration3Stage.SystemsConversion:
                    case PartOIteration3Stage.SystemsSimulation:
                    case PartOIteration3Stage.ZoneTemperature:
                        return "TAS Systems results - reused, not run again";

                    case PartOIteration3Stage.ResultantTemperature:
                        return "Resultant temperatures - reused, not run again";
                }
            }

            return partOIteration3Stage switch
            {
                PartOIteration3Stage.Input => "Checking the reference run",
                PartOIteration3Stage.ReferenceA => "Reading the Iteration 1a results",
                PartOIteration3Stage.ReferenceATM59 => "Assessing the reference case against TM59",
                PartOIteration3Stage.SystemScope => "Scoping the ventilation design",
                PartOIteration3Stage.EquipmentResolution => "Resolving the ventilation equipment",
                PartOIteration3Stage.Materialisation => "Materialising the ventilation systems",
                PartOIteration3Stage.SystemsConversion => "Preparing the TAS Systems document",
                PartOIteration3Stage.ResultantTemperature => "Full-year resultant-temperature run in TAS",
                PartOIteration3Stage.Reconciliation => "Reconciling the system case with the reference case",
                PartOIteration3Stage.Comparison => "Comparing the two cases",
                PartOIteration3Stage.Persistence => "Saving the system case model",
                _ => null,
            };
        }

        /// <summary>
        /// What the progress window is told when a ledger stage starts: the phase, then the step inside it (a
        /// stage that shares a phase with the one before would otherwise keep the earlier stage's step on screen).
        /// </summary>
        internal static Action<PartOIteration3Stage> PartOIteration3StageAnnouncer(PartOProgressHost partOProgressHost, PartOIteration3BehaviourMode partOIteration3BehaviourMode, bool resumed = false)
        {
            return partOIteration3Stage =>
            {
                partOProgressHost.Start(PartOIteration3Phase(partOIteration3Stage, partOIteration3BehaviourMode, resumed));
                partOProgressHost.Detail(PartOIteration3StageDetail(partOIteration3Stage, resumed)!);
            };
        }

        /// <summary>
        /// Runs the Approved Document O Iteration 3 system case for one method against the session's completed
        /// run, then shows the comparison.
        ///
        /// <para><b>What it asks, and when</b></para>
        /// <list type="bullet">
        /// <item>Nothing where the method can simply run. Which method, and what it will change, was chosen and
        /// shown in the Hub before this was called.</item>
        /// <item>A confirmation where the method already has a completed result, because running it again
        /// replaces that result - a destructive decision, and the one kind of question this keeps.</item>
        /// </list>
        /// <para>
        /// <b>Resume.</b> Where this session's last attempt for the method completed its TAS work and then stopped,
        /// and <see cref="Query.PartOIteration3ResumePlan"/> proves that work still belongs to this run, it is reused
        /// without asking - there is nothing to decide, because it was produced from exactly the inputs this attempt has. The
        /// window says so in its subtitle and lists the TAS work as one reused stage; the outcome line says so too.
        /// </para>
        /// <para>
        /// <b>Progress</b> is one window for the whole run (<see cref="PartOProgressHost"/>): the phases of
        /// <see cref="PartOIteration3Phases"/>, the elapsed time, and Cancel between stages. It closes before the comparison opens. There is no
        /// completion message box: the comparison IS the completion, and the Hub states it inline afterwards.
        /// </para>
        /// </summary>
        /// <returns>The line the Hub shows about what happened, or null where nothing was run.</returns>
        public static PartOWorkflowOutcome? RunPartOIteration3Case(this PartORun? partORun, PartOIteration3BehaviourMode partOIteration3BehaviourMode, IWin32Window? owner = null)
        {
            if (partORun is null)
            {
                return null;
            }

            PartOIteration3Eligibility partOIteration3Eligibility = Query.PartOIteration3Eligibility(partORun, partORun.IsAssessable(out string? refusal_Assessable), refusal_Assessable);

            if (!partOIteration3Eligibility.CanRun)
            {
                MessageBox.Show(string.Format("Iteration 3 cannot run for this reference case.\n\n{0}", partOIteration3Eligibility.Refusal_Run), "Part O — Iteration 3");

                return null;
            }

            string label = Query.PartOIteration3MethodLabel(partOIteration3BehaviourMode);

            PartOIteration3PairingStatus partOIteration3PairingStatus = partOIteration3Eligibility.PairingStatus(partOIteration3BehaviourMode);

            if (partOIteration3PairingStatus.IsReviewable)
            {
                DialogResult dialogResult = MessageBox.Show(
                    string.Format(
                        "A completed Iteration 3 result for '{0}' already exists{1}.\n\nRunning it again replaces that result. It takes several minutes of TAS.\n\nRun it again?",
                        label,
                        partOIteration3PairingStatus.When.HasValue ? string.Format(CultureInfo.CurrentCulture, " ({0:d MMM yyyy HH:mm})", partOIteration3PairingStatus.When.Value) : string.Empty),
                    "Part O — Iteration 3",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (dialogResult != DialogResult.Yes)
                {
                    return null;
                }
            }

            IPartOIteration3Pipeline iPartOIteration3Pipeline = new PartOIteration3Pipeline { ReportProvenance = PartOIteration3ReportProvenance(partORun, partOIteration3BehaviourMode) };

            string reference = Query.PartOIterationText(partORun);

            //Decided before the window opens, so it lists only the stages this run performs: a resumed run shows its
            //TAS work as reused, never as running. The same plan is what the run acts on.
            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, partOIteration3BehaviourMode);
            bool resumed = partOIteration3ResumePlan.Reuse;

            string subheading = string.Format(
                "Reference case: {0}{1}",
                reference,
                resumed
                    ? string.Format(" · resuming: the TAS results of the attempt of {0} are reused, TAS is not run again", partOIteration3ResumePlan.Checkpoint!.When)
                    : partOIteration3ResumePlan.Reason is null ? string.Empty : " · earlier TAS results cannot be reused, so TAS is run");

            PartOIteration3Result partOIteration3Result;
            List<string> lines_Stages;
            TimeSpan elapsed;

            using (PartOProgressHost partOProgressHost = new(string.Format("Iteration 3 — {0}", label), subheading, PartOIteration3Phases(partOIteration3BehaviourMode, resumed)))
            {
                partOIteration3Result = RunPartOIteration3(
                    partORun,
                    iPartOIteration3Pipeline,
                    partOProgressHost.Token,
                    partOIteration3BehaviourMode,
                    PartOIteration3StageAnnouncer(partOProgressHost, partOIteration3BehaviourMode, resumed),
                    partOIteration3ResumePlan);

                if (partOIteration3Result.IsComplete)
                {
                    partOProgressHost.State.Complete();
                }
                else
                {
                    partOProgressHost.State.Fail();
                }

                lines_Stages = partOProgressHost.State.Lines();
                elapsed = partOProgressHost.State.Elapsed;
            }

            ShowPartOIteration3Result(partORun, partOIteration3Result, lines_Stages, owner);

            if (partOIteration3Result.IsComplete)
            {
                return new PartOWorkflowOutcome(
                    PartOWorkflowOutcomeKind.Success,
                    string.Format(
                        "✓ Iteration 3 complete · {0} · {1}{4} · reference {2} / system {3}",
                        label,
                        PartOProgressState.Format(elapsed),
                        Verdict(partOIteration3Result.Assessment_ReferenceA),
                        Verdict(partOIteration3Result.Assessment_CandidateB),
                        resumed ? string.Format(" · TAS results reused from the attempt of {0}", partOIteration3ResumePlan.Checkpoint!.When) : string.Empty));
            }

            PartOIteration3Stage? partOIteration3Stage_Refused = partOIteration3Result.Ledger.Stage_Refused;

            return new PartOWorkflowOutcome(
                PartOWorkflowOutcomeKind.Warning,
                string.Format(
                    "! Iteration 3 did not complete · {0} · stopped at {1} after {2}. {3}",
                    label,
                    partOIteration3Stage_Refused.HasValue ? Core.Query.Description(partOIteration3Stage_Refused.Value).ToLowerInvariant() : "an unrecorded stage",
                    PartOProgressState.Format(elapsed),
                    PartOIteration3RetryText(partORun, partOIteration3BehaviourMode)));
        }

        /// <summary>
        /// The provenance an Iteration 3 TM59 report is saved with. The reference case's results are the
        /// reference iteration's own, so its report is headed by that iteration's scenario, with Iteration 3
        /// as the assessment context it was reassessed for; every other results file is the system case's,
        /// whose scenario is Iteration 3 itself.
        /// </summary>
        internal static Func<string, TM59AssessmentReport, IEnumerable<string>> PartOIteration3ReportProvenance(PartORun partORun, PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            string label = Query.PartOIteration3MethodLabel(partOIteration3BehaviourMode);

            return (path_TSD, tM59AssessmentReport) =>
            {
                bool reference = !string.IsNullOrWhiteSpace(path_TSD) && !string.IsNullOrWhiteSpace(partORun.Path_TSD)
                    && string.Equals(System.IO.Path.GetFullPath(path_TSD), System.IO.Path.GetFullPath(partORun.Path_TSD), StringComparison.OrdinalIgnoreCase);

                return reference
                    ? PartOTM59ResultSummary.ReportProvenance(partORun, tM59AssessmentReport, [("Assessment context", "Iteration 3 — Reference case")])
                    : PartOTM59ResultSummary.ReportProvenance(
                        partORun,
                        tM59AssessmentReport,
                        [("Reference case", Query.PartOIterationText(partORun))],
                        scenario: string.Format("Iteration 3 — Explicit system and cooling assessment · system case: explicit TAS/TPD system ({0})", label),
                        path_TSD: path_TSD);
            };
        }

        /// <summary>
        /// Opens one method's saved Iteration 3 result - rebuilt from the existing results, with no TAS - or,
        /// where the method's last attempt did not complete, that attempt's stage record.
        /// </summary>
        public static PartOWorkflowOutcome? ReviewPartOIteration3Case(this PartORun? partORun, PartOIteration3BehaviourMode partOIteration3BehaviourMode, IWin32Window? owner = null)
        {
            if (partORun is null)
            {
                return null;
            }

            IPartOIteration3Pipeline iPartOIteration3Pipeline = new PartOIteration3Pipeline { ReportProvenance = PartOIteration3ReportProvenance(partORun, partOIteration3BehaviourMode) };

            string label = Query.PartOIteration3MethodLabel(partOIteration3BehaviourMode);

            PartOIteration3Result partOIteration3Result;
            TimeSpan elapsed;

            //No Cancel: this reads two existing results files and starts no TAS process.
            using (PartOProgressHost partOProgressHost = new(
                string.Format("Iteration 3 — {0}", label),
                "Opening the saved result. No TAS simulation is run.",
                ["Read the saved result", "Re-assess the reference case", "Re-assess the system case", "Rebuild the comparison"],
                false,
                showDelay: PartOProgressHost.ShowDelay_Review))
            {
                int index = 0;

                partOIteration3Result = ReviewPartOIteration3(partORun, iPartOIteration3Pipeline, partOIteration3BehaviourMode, text => partOProgressHost.Start(index++));

                partOProgressHost.State.Complete();

                elapsed = partOProgressHost.State.Elapsed;
            }

            ShowPartOIteration3Result(partORun, partOIteration3Result, null, owner);

            return partOIteration3Result.IsComplete
                ? new PartOWorkflowOutcome(
                    PartOWorkflowOutcomeKind.Information,
                    string.Format(
                        "○ Opened the saved Iteration 3 result · {0} · reference {1} / system {2} · no TAS run ({3})",
                        label,
                        Verdict(partOIteration3Result.Assessment_ReferenceA),
                        Verdict(partOIteration3Result.Assessment_CandidateB),
                        PartOProgressState.Format(elapsed)))
                : null;
        }

        /// <summary>
        /// The command the Hub offered before per-method results existed, kept for callers of that era: it
        /// opens the first method with a completed result, and otherwise runs the route check - the method that
        /// needs no product, and what the old picker defaulted to.
        /// </summary>
        public static void PartOIteration3(this PartORun? partORun, IWin32Window? owner = null)
        {
            if (partORun is null)
            {
                return;
            }

            PartOIteration3Eligibility partOIteration3Eligibility = Query.PartOIteration3Eligibility(partORun, partORun.IsAssessable(out string? refusal_Assessable), refusal_Assessable);

            PartOIteration3PairingStatus? partOIteration3PairingStatus_Reviewable = partOIteration3Eligibility.PairingStatuses.Find(x => x.IsReviewable);

            if (partOIteration3PairingStatus_Reviewable is not null)
            {
                ReviewPartOIteration3Case(partORun, partOIteration3PairingStatus_Reviewable.BehaviourMode, owner);

                return;
            }

            RunPartOIteration3Case(partORun, PartOIteration3BehaviourMode.Parity, owner);
        }

        private static void ShowPartOIteration3Result(PartORun partORun, PartOIteration3Result partOIteration3Result, List<string>? lines_Stages, IWin32Window? owner)
        {
            List<PartOIteration3GuidanceEvidence> guidance = Query.PartOIteration3GuidanceEvidence(partORun, partOIteration3Result.Record, VentilationUnitCatalogue.Read(), out string? source_Guidance);

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                ReferenceText = Query.PartOIterationText(partORun),
                StageTimings = lines_Stages,
                Guidance = guidance,
                GuidanceSource = source_Guidance,
                Result = partOIteration3Result,
            };

            if (owner is not null)
            {
                new System.Windows.Interop.WindowInteropHelper(partOIteration3ResultWindow).Owner = owner.Handle;
            }

            partOIteration3ResultWindow.ShowDialog();
        }

        /// <summary>
        /// What running a method again after it stopped will do: reuse the TAS work this session kept for it, or - where
        /// none is kept - simply run again. Read off the run, so it says what the next attempt will actually find.
        /// </summary>
        internal static string PartOIteration3RetryText(PartORun partORun, PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            return partORun?.Iteration3Checkpoint(partOIteration3BehaviourMode) is null
                ? "It can be run again."
                : "Its TAS results are kept for this session, so running it again reuses them instead of running TAS.";
        }

        private static string Verdict(PartOIteration3Assessment? partOIteration3Assessment)
        {
            return partOIteration3Assessment is null || !partOIteration3Assessment.IsAssessed
                ? "—"
                : Query.PartOVerdictText(partOIteration3Assessment.OccupiedSpaceComplianceStatus);
        }
    }
}
