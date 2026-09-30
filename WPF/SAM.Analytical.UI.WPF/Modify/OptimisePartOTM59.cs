// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        //The stages of the Iteration 2B progress window, in order. The rounds are ONE stage, labelled with
        //the round being run, because how many there will be is not known until the optimisation stops.
        internal const int PartOOptimisationPhase_Starting = 0;

        internal const int PartOOptimisationPhase_Rounds = 1;

        internal const int PartOOptimisationPhase_CapacityEnvelope = 2;

        /// <summary>
        /// The stage names of the Iteration 2B progress window. The capacity envelope appears only where it
        /// was asked for - a stage that can never run is not listed.
        /// </summary>
        internal static List<string> PartOOptimisationPhases(PartOOptimisationSettings partOOptimisationSettings)
        {
            List<string> result = ["Assess the Iteration 2 results (TM59)", "Optimisation rounds"];

            if (partOOptimisationSettings?.CapacityEnvelope == true)
            {
                result.Add("Capacity envelope (diagnostic)");
            }

            return result;
        }

        /// <summary>
        /// Ends the Iteration 2B progress window's stage list from the run's own record - never simply as
        /// completed. The ordinary optimisation completed where it reached a terminal condition (passed, at
        /// capacity, at its limit, nothing left to target) - the same four the Hub calls completed; any other
        /// stop, a cancellation included, did not complete. A capacity envelope that was running completed
        /// only where its own step says so. Null - refused before anything ran - did not complete. A completed
        /// run's stages that never started are marked not needed, never left reading as upcoming.
        /// </summary>
        internal static void PartOOptimisationProgressEnd(PartOProgressState partOProgressState, PartOOptimisationRun? partOOptimisationRun)
        {
            if (partOProgressState is null)
            {
                return;
            }

            bool completed;

            if (partOOptimisationRun is null)
            {
                completed = false;
            }
            else if (partOProgressState.Status(PartOOptimisationPhase_CapacityEnvelope) == PartOProgressStageStatus.Running)
            {
                completed = partOOptimisationRun.Step_CapacityEnvelope?.IsCompleted == true;
            }
            else
            {
                completed = partOOptimisationRun.StopReason is PartOOptimisationStopReason.Passed
                    or PartOOptimisationStopReason.CapacityReached
                    or PartOOptimisationStopReason.IterationLimitReached
                    or PartOOptimisationStopReason.NoEligibleTargets;
            }

            if (completed)
            {
                partOProgressState.Complete();

                //Ended at its own terminal condition, so what never started - the rounds, where the baseline
                //needed none; an envelope declined before it simulated - was not needed. Only here: after a
                //cancellation or a failure a stage that never ran was not "not needed", so it is left alone.
                partOProgressState.SkipUnstarted();
            }
            else
            {
                partOProgressState.Fail();
            }
        }

        /// <summary>
        /// The Iteration 2B progress window's second line. The round limit is the run's own setting - a limit,
        /// not a total: how many rounds run depends on the results, and it says so.
        /// </summary>
        internal static string PartOOptimisationProgressSubheading(PartOOptimisationSettings partOOptimisationSettings)
        {
            int maximumIterations = partOOptimisationSettings.MaximumIterations;

            return string.Format(
                "Each round is a full-year TAS simulation. The limit is {0} {1}; how many run depends on the results.",
                maximumIterations,
                maximumIterations == 1 ? "round" : "rounds");
        }

        /// <summary>
        /// Runs the automatic Approved Document O <b>Iteration 2B</b> optimisation over a completed
        /// Iteration 2 run: raise the design airflow of every eligible failing mechanical space by a fixed
        /// step, rebalance, rebuild the Part O state, re-simulate the same full year, reassess with
        /// production TM59, and repeat until something explicit stops it.
        ///
        /// <para><b>What each round is</b></para>
        /// <code>
        /// last valid design
        ///   -> deliberate targets            production TM59 FAIL, mechanical, in dwelling scope, +step
        ///   -> ONE design airflow round      Modify.EvaluateTargetedDesignAirFlows - all targets at once
        ///   -> re-prepare Part O             transfer air, network and unit duties rebuilt by SAM
        ///   -> full-year TAS, unique TSD     the SAME weather case, under its own project name
        ///   -> production TM59               on the model the workflow RETURNED
        ///   -> next targets, or stop
        /// </code>
        ///
        /// <para><b>This is design optimisation, and only that</b></para>
        /// <para>
        /// Design airflow moves. The Approved Document F requirement is a floor and is never written. The
        /// selected product is a ceiling and is never changed. No operating or profile airflow is written at
        /// all, so nothing here is the Iteration 3 behaviour of running a room at one airflow normally and
        /// another during hot hours.
        /// </para>
        ///
        /// <para><b>Why the re-preparation is offered NO catalogue</b></para>
        /// <para>
        /// <c>Modify.PreparePartOIteration</c> given a catalogue runs its smallest-capable-unit rule against
        /// the realized duty - which is right for Iteration 2 and would be a disaster here: every round the
        /// design grew, it would quietly buy the next product up, and the optimisation would never reach
        /// capacity because capacity would keep moving. It is therefore called with <b>null</b>, which
        /// leaves <c>AirHandlingUnitParameter.VentilationUnitReference</c> untouched; the preparation reuses
        /// the existing system and unit through their design terminals, so the product selected at
        /// Iteration 2 survives every round unchanged. The catalogue is still used - by the design airflow
        /// round, to read what that selected product is rated at and refuse a round beyond it.
        /// </para>
        ///
        /// <para><b>A full step, or no step</b></para>
        /// <para>
        /// <c>EvaluateTargetedDesignAirFlows</c> never clamps: a round is adopted at exactly the airflows it
        /// asked for, or refused. Where the refusal is the selected unit's rating, the dwellings that
        /// refused are marked at capacity, their targets dropped, and the round retried for the dwellings
        /// that can still take a full step - so one flat reaching its unit does not stop the others. When
        /// nothing is left that can take a full step, the run stops at
        /// <see cref="PartOOptimisationStopReason.CapacityReached"/> with the last valid design intact.
        /// </para>
        ///
        /// <para><b>Every PR #76 guarantee is kept, not worked around</b></para>
        /// <para>
        /// Each round is a complete <see cref="PartORun"/> lifecycle - prepared, armed before the workflow,
        /// completed only from a full-year run that wrote its own TSD, and assessed only from the model that
        /// workflow returned. Nothing here reads a model back off disk, assesses a preparation output, or
        /// completes a run from a results file it did not watch being written. A cancelled or failed round
        /// leaves the run dropped and contributes a recorded step with no model, so it can never become a
        /// false successful result.
        /// </para>
        /// </summary>
        /// <param name="partORun">
        /// The session's Part O run, which must be a completed Iteration 2 run. It is driven through a fresh
        /// lifecycle for every round and left holding the last valid one.
        /// </param>
        /// <param name="partOOptimisationSettings">The step, the iteration guard, and whether the final
        /// diagnostic capacity envelope is wanted.</param>
        /// <param name="refusal">Why the optimisation could not be started at all.</param>
        /// <returns>The whole run - the baseline, every round, why it stopped, the last valid design, and -
        /// separately from all of it - the diagnostic capacity envelope where one was calculated.</returns>
        public static PartOOptimisationRun? OptimisePartOTM59(this PartORun? partORun, PartOOptimisationSettings? partOOptimisationSettings, out string? refusal)
        {
            refusal = null;

            partOOptimisationSettings ??= new PartOOptimisationSettings();

            if (!CanOptimise(partORun, partOOptimisationSettings, out refusal))
            {
                return null;
            }

            //Captured HERE, while CanOptimise still guarantees both are there. The ordinary optimisation
            //below can leave partORun dropped - a cancelled or unassessable round invalidates it - and both
            //contexts read null in that state, so the envelope stage could not ask for them afterwards.
            PartOPreparationContext partOPreparationContext = partORun!.PreparationContext!;
            //The baseline's own TAS case, writing into Iteration 2B's folder beneath the baseline's Part O root: every
            //round and the envelope go there, and the baseline's results are read where they are. See PartOOutputPaths.
            PartOSimulationContext partOSimulationContext = partORun.SimulationContext!;

            PartOOutputPaths? partOOutputPaths = PartOOutputPaths.Create(PartOOutputPaths.Root(partOSimulationContext.OutputDirectory), PartOOutputCase.Iteration2B);
            if (partOOutputPaths is not null)
            {
                refusal = partOOutputPaths.TryCreateDirectories();
                if (refusal is not null)
                {
                    return null;
                }

                partOSimulationContext = partOOutputPaths.SimulationContext(partOSimulationContext);
            }

            PartOOptimisationRun result = Optimise(partORun, partOOptimisationSettings, partOPreparationContext, partOSimulationContext);

            //AFTER the ordinary optimisation has finished and its answer is settled, never during it. The
            //envelope reads the design the optimisation accepted and writes nothing back into the run - see
            //CapacityEnvelope.
            CapacityEnvelope(result, partOOptimisationSettings, partOPreparationContext, partOSimulationContext);

            return result;
        }

        /// <summary>
        /// The ordinary optimisation, unchanged: rounds at the whole configured step until something
        /// explicit stops it, and the last design that was actually valid.
        /// <para>
        /// Separated from the public entry point only so the diagnostic capacity envelope can run strictly
        /// <b>after</b> this has reached its terminal condition. Nothing about the loop's behaviour depends
        /// on whether an envelope follows it, and nothing here knows one might.
        /// </para>
        /// </summary>
        private static PartOOptimisationRun Optimise(PartORun partORun, PartOOptimisationSettings partOOptimisationSettings, PartOPreparationContext partOPreparationContext, PartOSimulationContext partOSimulationContext)
        {
            PartOOptimisationRun result = new(partOOptimisationSettings);

            //Run 0. The Iteration 2 design exactly as it stands - not re-simulated, because it already has
            //its full-year results and its own assessment, and re-running it would only prove TAS is
            //deterministic while costing an engineer several minutes.
            AnalyticalModel? analyticalModel_LastValid = partORun.AnalyticalModel_Assessment;
            string? path_TSD_LastValid = partORun.Path_TSD;
            List<OverheatingScenario> overheatingScenarios_LastValid = partORun.OverheatingScenarios;

            //Reported, where the command shows the Part O progress window; nothing below depends on it.
            PartOProgressHost.Current?.Start(PartOOptimisationPhase_Starting);

            PartOTM59Assessment partOTM59Assessment = PartOTM59Assessment.Assess(analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);

            //The run this optimisation actually starts from, named by the results file that produced it
            //rather than by the context's base name. The two differ exactly when this design is itself the
            //output of a previous optimisation, and getting it wrong would label the baseline with a
            //project that refers to different results.
            string projectName_Baseline = string.IsNullOrWhiteSpace(path_TSD_LastValid)
                ? partOSimulationContext.ProjectName
                : Path.GetFileNameWithoutExtension(path_TSD_LastValid);

            //And the numbering continues from it. An optimisation run a second time over the design the
            //first one left behind would otherwise start again at -Opt01 and overwrite the first run's
            //TBD and TSD - destroying the evidence the whole per-iteration naming exists to keep. See
            //PartOSimulationContext.Iteration_ProjectName.
            int iteration_Baseline = PartOSimulationContext.Iteration_ProjectName(projectName_Baseline);

            PartOOptimisationStep partOOptimisationStep = Step(0, projectName_Baseline, path_TSD_LastValid, partOSimulationContext, analyticalModel_LastValid, partOPreparationContext, partOTM59Assessment);

            result.Steps.Add(partOOptimisationStep);

            if (!partOTM59Assessment.IsAssessed)
            {
                return Stop(result, PartOOptimisationStopReason.AssessmentFailed, partOTM59Assessment.Refusal, analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
            }

            partOOptimisationStep.IsCompleted = true;

            if (partOTM59Assessment.OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Pass)
            {
                string? refusal_Partial = PartialAssessment(analyticalModel_LastValid, partOPreparationContext, partOTM59Assessment);

                return refusal_Partial is null
                    ? Stop(result, PartOOptimisationStopReason.Passed, "The Iteration 2 baseline already passes, so no optimisation round was run.", analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid)
                    : Stop(result, PartOOptimisationStopReason.AssessmentFailed, refusal_Partial, analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
            }

            if (partOTM59Assessment.OccupiedSpaceComplianceStatus != TM59ComplianceStatus.Fail)
            {
                //Neither a pass nor a failure: the assessment produced no occupied-space verdict at all.
                //There is nothing to optimise towards, and - far more importantly - nothing here may be
                //reported as passing. "Not Fail" is not "Pass", and treating it as one would have this run
                //claim every eligible space meets its criteria on the strength of an assessment that
                //reached no conclusion about any of them.
                return Stop(result, PartOOptimisationStopReason.AssessmentFailed, NoVerdict(partOTM59Assessment), analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
            }

            //The run's own canonical TBD - the conversion the baseline's own simulation already produced,
            //beside the results file it wrote. Adopted ONCE, here, and only ever read from: every later
            //round is given a COPY of it.
            //
            //Never chained. Round 3 is copied from run 0, not from round 2, so no round can inherit
            //another's leftover ventilation state - which is the whole reason a canonical baseline exists
            //rather than a running TBD.
            //
            //Adopted from the BASELINE's results path rather than from the context's project name, because
            //an optimisation run over a previous optimisation's output starts from that run's files, and
            //the conversion to reuse is the one beside the design actually being optimised.
            PartOCanonicalTBD partOCanonicalTBD = null;

            if (!partOOptimisationSettings.WarmStart)
            {
                result.CanonicalTBDRefusal = "Warm starting was not asked for, so every iteration converted the model in full.";
            }
            else
            {
                partOCanonicalTBD = PartOCanonicalTBD.Adopt(
                    string.IsNullOrWhiteSpace(path_TSD_LastValid) ? null : Path.ChangeExtension(path_TSD_LastValid, "tbd"),
                    analyticalModel_LastValid,
                    partOSimulationContext,
                    out string refusal_Canonical);

                result.CanonicalTBD = partOCanonicalTBD;
                result.CanonicalTBDRefusal = refusal_Canonical;

                partOOptimisationStep.Notes.Add(partOCanonicalTBD is null
                    ? refusal_Canonical
                    : string.Format("This run's later iterations start from the converted TBD this baseline produced - '{0}' - rather than converting the same geometry again. Each one is given its own copy of it, always from this baseline and never from the previous iteration, and each still performs a real full-year simulation of its own design. The baseline itself is only ever read.", partOCanonicalTBD.Path_TBD));
            }

            //Dwellings whose selected unit has already refused a full step. Their rooms stay out of every
            //later round: a dwelling at its unit's ceiling cannot take another step, and asking again every
            //round would rerun the same refusal.
            HashSet<Guid> guids_AtCapacity = [];

            using CancellationTokenSource cancellationTokenSource = new();

            for (int iteration = 1; iteration <= partOOptimisationSettings.MaximumIterations; iteration++)
            {
                // ---- The targets, and the one round they make ------------------------------------------

                PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(analyticalModel_LastValid, partOTM59Assessment.SpaceResults, partOPreparationContext.Zones, partOOptimisationSettings.AirFlowStep_Lps);

                List<DesignAirFlowTarget> designAirFlowTargets = partOOptimisationTargetSelection.Targets.FindAll(x => !guids_AtCapacity.Contains(x.SpaceGuid));

                if (designAirFlowTargets.Count == 0)
                {
                    //NO step is recorded here. Nothing was attempted - no round, no preparation, no
                    //simulation - and appending one anyway would put a run in the history, and in the round
                    //count, that never happened. What WAS learned belongs to the design it was learned
                    //about, so the reasons go on the step that produced that design.
                    result.Step_LastValid?.Notes.AddRange(partOOptimisationTargetSelection.NotOptimisable);

                    //Told apart deliberately: nothing left to try because the equipment is full is a
                    //different answer from nothing left to try at all, and an engineer does different things
                    //about them.
                    return Stop(
                        result,
                        guids_AtCapacity.Count == 0 ? PartOOptimisationStopReason.NoEligibleTargets : PartOOptimisationStopReason.CapacityReached,
                        guids_AtCapacity.Count == 0
                            ? "No failing space remained that this optimisation could target."
                            : "Every remaining failing space is in a dwelling whose selected ventilation unit cannot carry another full step.",
                        analyticalModel_LastValid,
                        path_TSD_LastValid,
                        overheatingScenarios_LastValid);
                }

                //Recorded only now that there is a round to attempt.
                partOOptimisationStep = new PartOOptimisationStep(iteration)
                {
                    //Offset by where this optimisation started, so a second run over a previous one's
                    //output continues its numbering rather than overwriting its files.
                    ProjectName = partOSimulationContext.ProjectName_Iteration(iteration_Baseline + iteration),
                    WeatherData = partOSimulationContext.WeatherData?.Name,
                };

                result.Steps.Add(partOOptimisationStep);

                //The round's own number - no total, because there is none until the run stops.
                PartOProgressHost? partOProgressHost = PartOProgressHost.Current;
                partOProgressHost?.Start(PartOOptimisationPhase_Rounds);
                partOProgressHost?.State.Activity(string.Format("round {0}", iteration));
                partOProgressHost?.Detail("Raising the design airflow of the failing spaces and preparing the round");

                partOOptimisationStep.Notes.AddRange(partOOptimisationTargetSelection.NotOptimisable);

                DesignAirFlowRoundCandidate? designAirFlowRoundCandidate = Round(analyticalModel_LastValid, designAirFlowTargets, partOPreparationContext, partOOptimisationSettings, guids_AtCapacity, partOOptimisationStep);

                if (designAirFlowRoundCandidate is null)
                {
                    return Stop(result, PartOOptimisationStopReason.CapacityReached, "No dwelling could take another full step within its selected ventilation unit.", analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                if (!designAirFlowRoundCandidate.IsAccepted)
                {
                    partOOptimisationStep.Refusals.AddRange(designAirFlowRoundCandidate.Refusals);

                    return Stop(result, PartOOptimisationStopReason.RebalanceRefused, string.Join(" ", designAirFlowRoundCandidate.Refusals), analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                partOOptimisationStep.TargetedAdjustments.AddRange(designAirFlowRoundCandidate.TargetedAdjustments);
                partOOptimisationStep.DerivedAdjustments.AddRange(designAirFlowRoundCandidate.DerivedAdjustments);
                partOOptimisationStep.TargetRefusals.AddRange(designAirFlowRoundCandidate.TargetRefusals);
                partOOptimisationStep.Notes.AddRange(designAirFlowRoundCandidate.Notes);

                AnalyticalModel analyticalModel_Round = new(analyticalModel_LastValid, designAirFlowRoundCandidate.AdjacencyCluster);

                // ---- Rebuild the real Part O state around the new design -------------------------------

                //NULL catalogue - see the method documentation. The unit selected at Iteration 2 is reused
                //through its design terminals and is not re-chosen against the grown duty.
                PartOIterationPreparation partOIterationPreparation = Analytical.Modify.PreparePartOIteration(analyticalModel_Round, partOPreparationContext.PartOIteration, partOPreparationContext.Zones, partOPreparationContext.VentilationStrategies, null);

                partOOptimisationStep.Notes.AddRange(partOIterationPreparation.Notes);
                partOOptimisationStep.Warnings.AddRange(partOIterationPreparation.Warnings);

                if (!partORun.Prepare(partOIterationPreparation, partOPreparationContext))
                {
                    partOOptimisationStep.Refusals.Add(partOIterationPreparation.Refusal ?? partORun.InvalidationReason ?? "The Part O iteration could not be re-prepared over the optimised design.");

                    return Stop(result, PartOOptimisationStopReason.PreparationFailed, string.Join(" ", partOOptimisationStep.Refusals), analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                // ---- The same full-year case, under this iteration's own name --------------------------

                //Checked EVERY round, not once: the canonical file can be replaced underneath a running
                //optimisation, and a round that has to convert in full must say so on its own record.
                //
                //Compared against the LAST VALID design - a workflow output, like the one the canonical was
                //fingerprinted from - so like is compared with like across the TAS round trip. The design
                //airflow this round changed is deliberately not part of the comparison: it is what the
                //warm-started run re-applies, and including it would turn the warm start off every round.
                PartOCanonicalTBD partOCanonicalTBD_Round = WarmStart(partOCanonicalTBD, analyticalModel_LastValid, partOSimulationContext, partOOptimisationStep);

                AnalyticalModel analyticalModel_Workflow = RunPartOSimulation(partOIterationPreparation.AnalyticalModel, partOSimulationContext, partOOptimisationStep.ProjectName, partORun, cancellationTokenSource.Token, out string _, out string path_TSD, out bool cancelled, out bool fullYear, out List<string> notes_Simulation, out string refusal_Simulation, partOCanonicalTBD_Round);

                partOOptimisationStep.Notes.AddRange(notes_Simulation);
                partOOptimisationStep.Path_TSD = path_TSD;

                if (cancelled)
                {
                    partORun.Invalidate("The Part O optimisation was cancelled during this round's simulation, so this round has no results.");

                    return Stop(result, PartOOptimisationStopReason.Cancelled, "Cancelled during the simulation of this round.", analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                if (analyticalModel_Workflow is null || !fullYear)
                {
                    partOOptimisationStep.Refusals.Add(refusal_Simulation ?? (analyticalModel_Workflow is null
                        ? "The TAS workflow did not run over this round's design, so there are no results to assess."
                        : "The simulation that ran was not the full year a TM59 assessment reads."));

                    partORun.Invalidate(string.Join(" ", partOOptimisationStep.Refusals));

                    return Stop(result, PartOOptimisationStopReason.SimulationFailed, string.Join(" ", partOOptimisationStep.Refusals), analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                if (!partORun.Complete(analyticalModel_Workflow, path_TSD, partOSimulationContext, out string refusal_Complete))
                {
                    partOOptimisationStep.Refusals.Add(refusal_Complete);

                    return Stop(result, PartOOptimisationStopReason.SimulationFailed, refusal_Complete, analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                // ---- Production TM59, on the model the workflow returned -------------------------------

                partOProgressHost?.Detail("Reading this round's results and assessing them against CIBSE TM59");

                partOTM59Assessment = PartOTM59Assessment.Assess(partORun.AnalyticalModel_Assessment, partORun.Path_TSD, partORun.OverheatingScenarios);

                partOProgressHost?.Detail(null);

                Record(partOOptimisationStep, partORun.AnalyticalModel_Assessment, partOPreparationContext, partOTM59Assessment);

                if (!partOTM59Assessment.IsAssessed)
                {
                    partOOptimisationStep.Refusals.Add(partOTM59Assessment.Refusal ?? "The production TM59 assessment could not be produced for this round.");

                    //DROPPED, like every other failed round. The workflow completed, so the run is sitting
                    //in WorkflowCompleted holding THIS round's model and TSD - but the optimisation is about
                    //to hand back the previous design, and leaving the run assessable would pair a model the
                    //user is not looking at with results from a round that was never assessed. That is the
                    //stale pairing PartORun exists to make unreachable, and an unassessable round has no
                    //business surviving as one.
                    partORun.Invalidate(partOTM59Assessment.Refusal ?? "The production TM59 assessment could not be produced for this optimisation round, so it is not a run that can be assessed.");

                    return Stop(result, PartOOptimisationStopReason.AssessmentFailed, partOTM59Assessment.Refusal, analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                //This round is now the last valid design: it was prepared, simulated over the full year and
                //assessed, and every one of those was checked rather than assumed.
                partOOptimisationStep.IsCompleted = true;

                analyticalModel_LastValid = partORun.AnalyticalModel_Assessment;
                path_TSD_LastValid = partORun.Path_TSD;
                overheatingScenarios_LastValid = partORun.OverheatingScenarios;

                if (partOTM59Assessment.OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Pass)
                {
                    //As at the baseline: a pass over a subset is not a pass.
                    string? refusal_Partial = PartialAssessment(analyticalModel_LastValid, partOPreparationContext, partOTM59Assessment);

                    return refusal_Partial is null
                        ? Stop(result, PartOOptimisationStopReason.Passed, null, analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid)
                        : Stop(result, PartOOptimisationStopReason.AssessmentFailed, refusal_Partial, analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }

                //As at the baseline: only an explicit Pass ends this run as a pass.
                if (partOTM59Assessment.OccupiedSpaceComplianceStatus != TM59ComplianceStatus.Fail)
                {
                    return Stop(result, PartOOptimisationStopReason.AssessmentFailed, NoVerdict(partOTM59Assessment), analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
                }
            }

            return Stop(result, PartOOptimisationStopReason.IterationLimitReached, null, analyticalModel_LastValid, path_TSD_LastValid, overheatingScenarios_LastValid);
        }

        /// <summary>
        /// Whether a run is an Iteration 2B starting point at all.
        /// <para>
        /// <b>Every condition is checked before anything runs</b>, because the first thing an optimisation
        /// does otherwise is spend minutes of TAS time discovering it should not have started.
        /// </para>
        /// </summary>
        public static bool CanOptimise(this PartORun? partORun, PartOOptimisationSettings? partOOptimisationSettings, out string? refusal)
        {
            refusal = null;

            if (partORun is null)
            {
                refusal = "There is no Part O run to optimise.";

                return false;
            }

            if (partOOptimisationSettings is not null && !partOOptimisationSettings.IsValid(out refusal))
            {
                return false;
            }

            //The same gate the assessment command uses, and it re-checks the results file rather than
            //trusting the state alone.
            if (!partORun.IsAssessable(out refusal))
            {
                return false;
            }

            //A run reopened from disk, ahead of the two null checks it would otherwise fall into - because
            //those describe a run that was never recorded, and this one was. Saying WHY a valid saved result
            //cannot be continued is the difference between a rule and a glitch.
            //
            //-- Why a restored run cannot resume into Iteration 2B --
            //
            //SETTLED, and settled as intentional. This is the record of that decision, so it does not come
            //back as an open question.
            //
            //It is not the results and it is not the design: PartORun.Restore validates both against
            //SimulationResultProvenance, and 2B does not re-simulate the baseline anyway (see Optimise, run
            //0). It is the PREPARATION, in whole - not the catalogue alone.
            //
            //Every 2B round re-prepares the design it just changed, and Analytical.Modify.PreparePartOIteration
            //is given the run's route, its assessed zones and its per-zone ventilation strategies. A restored
            //run holds NONE of them: PartOPreparationContext is a statement about how this session prepared
            //the case, and Restore clears it. Rebuilding it from the reopened model would be a regenerated
            //preparation authority wearing an old run's results - the same failure Restore refuses for the
            //overheating scenarios, and for the same reason.
            //
            //The catalogue is the part where drift is SILENT, which is why it is the part the message names.
            //Note precisely what it is and is not used for here: the re-preparation is offered NO catalogue
            //(see this method's own documentation - null, so the product selected at Iteration 2 survives
            //every round). PartOPreparationContext.VentilationUnitCapacityDescriptors is read only by the
            //design airflow round and the capacity envelope, through
            //Analytical.Query.SelectedVentilationUnitCapacityDescriptor, to find what the ALREADY SELECTED
            //product is rated at. The model stores that product's identity and never its capability -
            //VentilationUnitReference says why - so the rating has to come from a catalogue handed in, and
            //the dwellings 2B stops at (guids_AtCapacity) are exactly the ones sitting on that rating.
            //A catalogue corrected between the baseline and a resumed run therefore moves where the
            //optimisation stops while nothing in the model has changed, and it does so without failing:
            //SAM.Tests.PartOCatalogueDriftTests measures exactly that, one rating adopting a round the next
            //refuses.
            //
            //-- Why this is not fixed by persisting the catalogue --
            //
            //Because the catalogue is not the binding constraint. Persisting the capacities of the selected
            //products would be small and stable, and would unblock nothing on its own: the route, the scope
            //and the strategies are equally absent, and 2B needs all of them every round. Persisting the
            //whole preparation context is a new serialisable contract with its own versioning, validation
            //and restore path - a new supported capability rather than a correction - and it would put an
            //equipment capability number into a saved file beside numbers meaning design duty and
            //regulatory requirement, which is the confusion VentilationUnitReference exists to prevent.
            //
            //Re-preparing is therefore the honest route back in, and it is one dialog: the reopened model is
            //the prepared design, and Prepare & Run over the same scenario and scope reaches a run that
            //carries both contexts again.
            if (partORun.IsRestored)
            {
                refusal = "This Part O run was reopened from a saved model, so its results can be reviewed but not continued. Iteration 2B re-prepares the design at every round against the ventilation unit catalogue the baseline selected from, and a saved model does not carry that catalogue - repeating the optimisation from today's catalogue could check a round against a capacity the baseline was never selected under. Run Prepare & Run again over the same scenario and dwellings to optimise from a live run.";

                return false;
            }

            PartOPreparationContext? partOPreparationContext = partORun.PreparationContext;
            if (partOPreparationContext is null)
            {
                refusal = "This Part O run does not record how it was prepared, so the same preparation cannot be repeated over an optimised design. Prepare the iteration again and re-run the simulation.";

                return false;
            }

            //The route, asked of SAM rather than decided here. Iteration 2B raises MECHANICAL design
            //airflow; a naturally ventilated dwelling has no design airflow for it to raise, and pretending
            //otherwise would be an optimisation of a quantity that does not govern the result.
            PartOVentilationMode partOVentilationMode = Analytical.Query.PartOIterationVentilationMode(partOPreparationContext.PartOIteration, out string _);

            if (partOVentilationMode != PartOVentilationMode.MVHR)
            {
                refusal = string.Format("Iteration 2B optimises mechanical design airflow, and this run was prepared over the {0} route. Natural ventilation is not a mechanical airflow optimisation target.", Core.Query.Description(partOVentilationMode));

                return false;
            }

            if (!partOPreparationContext.HasVentilationUnitCatalogue)
            {
                refusal = "This Part O run was prepared without equipment selection, so it is an Iteration 1a run and there is no selected ventilation unit for an optimisation to work within. Prepare the iteration again with a manufacturer ventilation unit selected.";

                return false;
            }

            PartOSimulationContext? partOSimulationContext = partORun.SimulationContext;
            if (partOSimulationContext is null || !partOSimulationContext.IsFullYear)
            {
                refusal = "This Part O run does not record a full-year TAS case that can be repeated, so an optimisation could not rerun the same weather case over each round. Re-run the simulation with Full Year Simulation ticked over days 1 to 365.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// One design airflow round, retried without the dwellings whose <b>selected</b> unit refused it.
        /// <para>
        /// <b>Only an equipment refusal is retried, and only by removing whole dwellings.</b> A dwelling at
        /// its unit's ceiling cannot take another full step and is out of the optimisation from here; every
        /// other dwelling can still take one, and stopping them because of it would waste capacity the
        /// design has. Any other refusal - an Approved Document F floor, an unbalanced dwelling, terminals
        /// that cannot be attributed - is <b>not</b> retried: it is a design that needs a person.
        /// </para>
        /// <para>
        /// The round itself is never partially adopted. Each attempt is a whole transaction over the
        /// targets it was given, at exactly the airflows they asked for.
        /// </para>
        /// </summary>
        /// <returns>The accepted round, the refused one where the refusal was not about equipment, or null
        /// where no dwelling can take another full step.</returns>
        private static DesignAirFlowRoundCandidate? Round(AnalyticalModel? analyticalModel, List<DesignAirFlowTarget> designAirFlowTargets, PartOPreparationContext partOPreparationContext, PartOOptimisationSettings partOOptimisationSettings, HashSet<Guid> guids_AtCapacity, PartOOptimisationStep partOOptimisationStep)
        {
            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                return null;
            }

            while (designAirFlowTargets.Count != 0)
            {
                DesignAirFlowRoundCandidate designAirFlowRoundCandidate = adjacencyCluster.EvaluateTargetedDesignAirFlows(designAirFlowTargets, PartFExtractAllocationStrategy.MinimumFirstCookingPriority, partOOptimisationSettings.Tolerance_Lps, partOPreparationContext.VentilationUnitCapacityDescriptors);

                if (designAirFlowRoundCandidate.IsAccepted)
                {
                    return designAirFlowRoundCandidate;
                }

                List<DwellingDesignAirFlowRound> dwellingDesignAirFlowRounds = designAirFlowRoundCandidate.VentilationUnitRefusals;
                if (dwellingDesignAirFlowRounds.Count == 0)
                {
                    //Not an equipment refusal. Handed back refused, so the caller stops rather than
                    //quietly retrying a design problem with fewer rooms.
                    return designAirFlowRoundCandidate;
                }

                foreach (DwellingDesignAirFlowRound dwellingDesignAirFlowRound in dwellingDesignAirFlowRounds)
                {
                    foreach (DesignAirFlowAdjustment designAirFlowAdjustment in dwellingDesignAirFlowRound.TargetedAdjustments)
                    {
                        guids_AtCapacity.Add(designAirFlowAdjustment.SpaceGuid);
                    }

                    partOOptimisationStep.Notes.Add(string.Format(
                        "Ventilation system '{0}' cannot take another {1:0.###} l/s step within its selected ventilation unit '{2}': the round would have designed {3:0.###}/{4:0.###} l/s against a maximum of {5:0.###}/{6:0.###} l/s. Its rooms are out of the optimisation from here; the selected product is unchanged.",
                        dwellingDesignAirFlowRound.VentilationSystem?.FullName ?? "?",
                        partOOptimisationSettings.AirFlowStep_Lps,
                        dwellingDesignAirFlowRound.VentilationUnitReference,
                        dwellingDesignAirFlowRound.SupplyDuty_After_Lps,
                        dwellingDesignAirFlowRound.ExtractDuty_After_Lps,
                        dwellingDesignAirFlowRound.VentilationUnitCapacityDescriptor?.MaximumSupplyFlowRate_Lps ?? double.NaN,
                        dwellingDesignAirFlowRound.VentilationUnitCapacityDescriptor?.MaximumExtractFlowRate_Lps ?? double.NaN));
                }

                designAirFlowTargets = designAirFlowTargets.FindAll(x => !guids_AtCapacity.Contains(x.SpaceGuid));
            }

            return null;
        }

        /// <summary>
        /// Why a <b>pass</b> may not be believed: an occupied space inside the Part O dwelling scope
        /// produced no result at all, so the verdict is over a subset of the rooms this run is about.
        ///
        /// <para><b>The failure this closes</b></para>
        /// <para>
        /// <c>PartOTM59Assessment</c> excludes a space whose simulated counterpart does not resolve to
        /// exactly one design space - correctly, because attributing it would be a guess - and records the
        /// exclusion as a warning. The remaining rooms can then all pass. Reading the combined status alone
        /// would have this run announce that <i>every eligible occupied space passes</i> on the strength of
        /// an assessment that never looked at one of them, stop, and hand back that design as the answer.
        /// Requiring an explicit <c>Pass</c> does not catch it: the status genuinely is <c>Pass</c>.
        /// </para>
        /// <para>
        /// Only the <b>dwelling scope</b> is asked about. The air handling units' simulation zones and
        /// anything else outside it were never part of this run's claim, so their absence is not a hole in
        /// it.
        /// </para>
        /// <para>
        /// Applied to a pass and not to a failure, deliberately: a failing round carries on optimising and
        /// reports the unresolved room in its warnings either way, whereas a pass is the claim that ends
        /// the run and so has to be airtight.
        /// </para>
        /// </summary>
        /// <returns>Null where every in-scope space was assessed, and the reason otherwise.</returns>
        /// <remarks>Internal rather than private so the subset-pass guard is pinned by tests.</remarks>
        internal static string? PartialAssessment(AnalyticalModel? analyticalModel, PartOPreparationContext partOPreparationContext, PartOTM59Assessment partOTM59Assessment)
        {
            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster is null || partOTM59Assessment.SpaceGuids_Unassessed.Count == 0)
            {
                return null;
            }

            HashSet<Guid> guids_Scope = Query.PartODwellingSpaceGuids(adjacencyCluster, partOPreparationContext.Zones);

            List<string> names = [];

            foreach (Guid guid in partOTM59Assessment.SpaceGuids_Unassessed)
            {
                if (!guids_Scope.Contains(guid))
                {
                    continue;
                }

                //The cluster's own O(1) authority, in place of a linear walk of the model's space list per
                //unassessed room. Same answer - that list is this object dictionary flattened - and the
                //fallback to the guid where the room is not in the model at all is unchanged.
                names.Add(adjacencyCluster.GetObject<Space>(guid)?.Name ?? guid.ToString());
            }

            if (names.Count == 0)
            {
                return null;
            }

            names.Sort(StringComparer.Ordinal);

            return string.Format(
                "The production TM59 assessment returned a pass, but {0} space(s) inside the Part O dwelling scope produced no result at all and were excluded from it: {1}. A pass over a subset of the rooms this run is about is not a pass, so it is not reported as one. Resolve those spaces - see the notes - and assess again.",
                names.Count,
                string.Join(", ", names.ConvertAll(x => string.Format("'{0}'", x))));
        }

        /// <summary>
        /// Why a run that produced an assessment stops anyway: it reached no occupied-space verdict.
        /// <para>
        /// Said in terms of what the assessment did rather than what the optimisation wanted, because the
        /// thing an engineer has to fix is upstream - spaces that did not resolve, or a scenario stating a
        /// strategy no criterion is known for.
        /// </para>
        /// </summary>
        private static string NoVerdict(PartOTM59Assessment partOTM59Assessment)
        {
            return string.Format(
                "The production TM59 assessment reached no occupied-space verdict for this design - its combined status is '{0}', which is neither a pass nor a failure - so there is nothing to optimise towards and nothing that may be reported as passing.{1}",
                Core.Query.Description(partOTM59Assessment.OccupiedSpaceComplianceStatus),
                partOTM59Assessment.AssociationRefusals.Count == 0 ? string.Empty : string.Format(" {0} space(s) produced no result; see the notes.", partOTM59Assessment.AssociationRefusals.Count));
        }

        /// <summary>The baseline step - iteration 0, no targets, its own results and its own assessment.</summary>
        private static PartOOptimisationStep Step(int iteration, string? projectName, string? path_TSD, PartOSimulationContext partOSimulationContext, AnalyticalModel? analyticalModel, PartOPreparationContext partOPreparationContext, PartOTM59Assessment partOTM59Assessment)
        {
            PartOOptimisationStep result = new(iteration)
            {
                ProjectName = projectName,
                Path_TSD = path_TSD,
                WeatherData = partOSimulationContext.WeatherData?.Name,
            };

            Record(result, analyticalModel, partOPreparationContext, partOTM59Assessment);

            return result;
        }

        /// <summary>
        /// Writes one iteration's unit states and production TM59 results onto its step - the audit trail
        /// that makes the run reproducible.
        /// <para>
        /// <b>And persists the report.</b> An assessed step's production report is written beside the
        /// results it was assessed from - the baseline's own TSD, each round's <c>-OptNN</c> one, the
        /// envelope's <c>-OptMax</c> one - so the evidence for every simulated case survives the session
        /// (see <see cref="SavePartOTM59Report"/>). A failure to write lands on the step as a warning and
        /// fails nothing.
        /// </para>
        /// </summary>
        private static void Record(PartOOptimisationStep partOOptimisationStep, AnalyticalModel? analyticalModel, PartOPreparationContext partOPreparationContext, PartOTM59Assessment partOTM59Assessment)
        {
            partOOptimisationStep.UnitStates.AddRange(UnitStates(analyticalModel, partOPreparationContext.VentilationUnitCapacityDescriptors, partOPreparationContext.Zones));

            //The COMPLETE design vector of this iteration, beside the adjustments that say what moved. Every
            //step records it - baseline, round and envelope alike - because a room-direction no round touched
            //has no adjustment anywhere, and without this the history could not print it at all. Read off the
            //model this step was assessed on, so it states that step's design and not a later one's.
            partOOptimisationStep.DesignAirFlowStates.AddRange(DesignAirFlowStates(analyticalModel, partOPreparationContext.Zones));

            if (!partOTM59Assessment.IsAssessed)
            {
                return;
            }

            partOOptimisationStep.TM59Results.AddRange(partOTM59Assessment.SpaceResults);
            partOOptimisationStep.OccupiedSpaceComplianceStatus = partOTM59Assessment.OccupiedSpaceComplianceStatus;
            partOOptimisationStep.Warnings.AddRange(partOTM59Assessment.AssociationRefusals);

            if (!SavePartOTM59Report(partOOptimisationStep.Path_TSD, partOTM59Assessment.Report, out string? path_TM59Report, out string? refusal_Report))
            {
                partOOptimisationStep.Warnings.Add(refusal_Report);
            }
        }

        /// <summary>
        /// <b>The complete design ventilation vector of one iteration</b> - every space and direction this
        /// run's equipment serves, at the design that iteration carried, whether or not anything moved it.
        /// <para>
        /// Recorded beside the adjustments because the two answer different questions. An adjustment exists
        /// only where something CHANGED, so a room-direction a round left alone was previously absent from
        /// the run altogether and the airflow history could not print it - which read as though the
        /// direction had been removed, when the ventilation network still carries it. See
        /// <see cref="PartODesignAirFlowState"/>.
        /// </para>
        /// <para>
        /// <b>Scoped exactly as the unit table is</b>, through the same
        /// <see cref="Query.PartOIterationAirHandlingUnits"/>: this iteration's own units and the systems
        /// they supply. A legacy mechanical ventilation system the building was drawn with is not this run's
        /// equipment and contributes nothing here either.
        /// </para>
        /// <para>
        /// A room-direction with no terminal has no design airflow to state and is not part of the vector -
        /// printing a zero would invent one. A room reached through two of a unit's systems in the same
        /// direction appears once: the history is at room grain, and both contributions are that room's air.
        /// </para>
        /// <para>
        /// <b>Read only.</b> Every figure comes from the authority that owns it - the design from the room's
        /// terminals, the requirement from Approved Document F - and nothing is written back to the model.
        /// </para>
        /// </summary>
        private static List<PartODesignAirFlowState> DesignAirFlowStates(AnalyticalModel? analyticalModel, IEnumerable<Zone>? zones_Dwelling)
        {
            List<PartODesignAirFlowState> result = [];

            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                return result;
            }

            List<AirHandlingUnit> airHandlingUnits = Query.PartOIterationAirHandlingUnits(adjacencyCluster, zones_Dwelling);

            airHandlingUnits.Sort((x, y) => string.CompareOrdinal(x?.Name, y?.Name));

            //ONE snapshot for this read-only pass, built by SAM. Every room of the run used to re-resolve
            //itself against the whole model to read its Approved Document F requirement, which made building
            //the design vector quadratic in the model. Nothing is cached here: the snapshot lives for this
            //one call, holds no rate, and answers exactly what Query.PartFRequiredFlowRate_Lps answers.
            PartFIndex partFIndex = new(adjacencyCluster);

            HashSet<string> keys = [];

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                if (airHandlingUnit is null)
                {
                    continue;
                }

                foreach (VentilationSystem ventilationSystem in Analytical.Query.VentilationSystems(adjacencyCluster, airHandlingUnit) ?? [])
                {
                    List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [];

                    spaces.RemoveAll(x => x is null);
                    spaces.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));

                    foreach (Space space in spaces)
                    {
                        foreach (FlowClassification flowClassification in new[] { FlowClassification.Supply, FlowClassification.Extract })
                        {
                            List<VentilationTerminal> ventilationTerminals = Analytical.Query.VentilationTerminals(
                                adjacencyCluster.VentilationTerminals(space) ?? [],
                                flowClassification);

                            if (ventilationTerminals is null || ventilationTerminals.Count == 0)
                            {
                                continue;
                            }

                            if (!keys.Add(string.Format("{0}|{1}", space.Guid, flowClassification)))
                            {
                                continue;
                            }

                            result.Add(new PartODesignAirFlowState(
                                space.Guid,
                                space.Name,
                                flowClassification,
                                ventilationTerminals.VentilationTerminalDesignDuty_Lps(flowClassification) ?? 0,
                                partFIndex.PartFRequiredFlowRate_Lps(space, flowClassification) ?? 0));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// What every air handling unit <b>of this run's Part O scope</b> is carrying, beside what its
        /// selected product is rated to move.
        /// <para>
        /// Every value from its own authority: the duty from <c>Query.AirHandlingUnitDesignDuty</c>, which
        /// sums every system the unit supplies rather than assuming it serves one; the product from
        /// <c>Query.SelectedVentilationUnitReference</c>; the rating from the descriptor that product
        /// resolves to in the run's own catalogue. Nothing here is stored on the unit, so nothing here can
        /// go stale against the design.
        /// </para>
        /// <para>
        /// <b>Only the iteration's own equipment.</b> The model's authored systems - a legacy mechanical
        /// ventilation system the building was drawn with, say - are not this run's equipment: the
        /// preparation never connects them to a design terminal, so
        /// <see cref="Query.PartOIterationAirHandlingUnits"/> excludes them by relation. The model is not
        /// edited to achieve that; the evidence is simply scoped to what the run is actually about.
        /// </para>
        /// </summary>
        private static List<PartOOptimisationUnitState> UnitStates(AnalyticalModel? analyticalModel, IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors, IEnumerable<Zone>? zones_Dwelling)
        {
            List<PartOOptimisationUnitState> result = [];

            AdjacencyCluster? adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                return result;
            }

            List<AirHandlingUnit> airHandlingUnits = Query.PartOIterationAirHandlingUnits(adjacencyCluster, zones_Dwelling);

            airHandlingUnits.Sort((x, y) => string.CompareOrdinal(x?.Name, y?.Name));

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                if (airHandlingUnit is null)
                {
                    continue;
                }

                if (!Analytical.Query.AirHandlingUnitDesignDuty(adjacencyCluster, airHandlingUnit, out double supplyDuty_Lps, out double extractDuty_Lps))
                {
                    supplyDuty_Lps = double.NaN;
                    extractDuty_Lps = double.NaN;
                }

                List<VentilationSystem> ventilationSystems = Analytical.Query.VentilationSystems(adjacencyCluster, airHandlingUnit) ?? [];

                VentilationUnitReference? ventilationUnitReference = airHandlingUnit.SelectedVentilationUnitReference();

                VentilationUnitCapacityDescriptor? ventilationUnitCapacityDescriptor = ventilationUnitCapacityDescriptors is null
                    ? null
                    : Analytical.Query.SelectedVentilationUnitCapacityDescriptor(airHandlingUnit, ventilationUnitCapacityDescriptors);

                VentilationUnitSelectionOutcome ventilationUnitSelectionOutcome = VentilationUnitSelectionOutcome.NotApplicable;
                string? reason = null;

                if (ventilationUnitCapacityDescriptors is not null && ventilationUnitReference is not null)
                {
                    ventilationUnitSelectionOutcome = Analytical.Query.IsVentilationUnitSufficient(adjacencyCluster, airHandlingUnit, ventilationUnitCapacityDescriptors, out reason)
                        ? VentilationUnitSelectionOutcome.Kept
                        : VentilationUnitSelectionOutcome.Refused;
                }

                result.Add(new PartOOptimisationUnitState(
                    airHandlingUnit.Name,
                    string.Join(", ", ventilationSystems.ConvertAll(x => x?.FullName)),
                    supplyDuty_Lps,
                    extractDuty_Lps,
                    ventilationUnitReference,
                    ventilationUnitCapacityDescriptor,
                    ventilationUnitSelectionOutcome,
                    ventilationUnitSelectionOutcome == VentilationUnitSelectionOutcome.Refused ? reason : null));
            }

            return result;
        }

        /// <summary>
        /// The <b>optional final diagnostic</b>: what the already-selected ventilation units could deliver
        /// if each were taken to its own design-capacity ceiling - calculated only once the ordinary
        /// optimisation has stopped, and kept entirely apart from what it accepted.
        ///
        /// <para><b>The question, and why it needs its own stage</b></para>
        /// <para>
        /// The optimisation above is all-or-nothing at a fixed step, and stays that way. So a run stops on
        /// the selected unit's capacity, or on the iteration guard, with eligible rooms still failing - and
        /// an engineer is left holding "this design still fails" with no statement of how close the
        /// equipment already bought can get. That statement is what this produces:
        /// </para>
        /// <para>
        /// <i>"If the last valid design were increased coherently while preserving its terminal airflow
        /// proportions, what design could the already-selected unit support at its capacity ceiling?"</i>
        /// </para>
        /// <para>
        /// It is <b>not</b> another round. It is a design the ordinary policy deliberately refuses, evaluated
        /// to say what the equipment could support - so for the real Flat 1, a dwelling designed 40 supply /
        /// 22 + 18 extract on a 150/150 unit becomes 150 supply / 82.5 + 67.5 extract: the same dwelling,
        /// larger.
        /// </para>
        ///
        /// <para><b>What it runs, and on what</b></para>
        /// <code>
        /// last ACCEPTED ordinary design
        ///   -> the +5 policy's target vector, read for SCOPE ONLY - which units are being diagnosed
        ///   -> Modify.EvaluateDesignAirFlowCapacityEnvelope   one proportional factor per equipment group
        ///   -> re-prepare Part O                              transfer air, network and duties rebuilt
        ///   -> full-year TAS, its OWN -OptMax TSD              the SAME weather case
        ///   -> production TM59, on the model the workflow RETURNED
        /// </code>
        /// <para>
        /// The vector's <b>figures</b> are not read. An earlier revision scaled those increments, which spent
        /// the remaining headroom only on the rooms the optimiser happened to be pushing - the same Flat 1
        /// came out at 150 supply / <b>22</b> + 128 extract, the bathroom carrying the whole increase. See
        /// <c>Analytical.Modify.EvaluateDesignAirFlowCapacityEnvelope</c>.
        /// </para>
        /// <para>
        /// Every guarantee the rounds keep is kept here too: the preparation is offered <b>no</b> catalogue,
        /// so the product selected at Iteration 2 is not re-chosen against the grown duty; the TAS case is
        /// the one the baseline was produced by, verbatim; the assessment reads the model the workflow
        /// returned and never the preparation output; and the results file is the envelope's own, so it
        /// overwrites no round's evidence.
        /// </para>
        ///
        /// <para><b>Why it gets its own <c>PartORun</c></b></para>
        /// <para>
        /// The session's run is left holding the <b>last accepted ordinary design</b> and the results that
        /// go with it, because that is the design the command adopts and the one an engineer will assess
        /// again. Driving the envelope's lifecycle through that same run would leave it paired with the
        /// diagnostic - so <c>RunPartOOptimisation</c>'s reference check would correctly refuse to arm, and
        /// worse, whatever the user then assessed would be the diagnostic's results against the accepted
        /// design's model. A private run has no model coupling at all and costs nothing, so the envelope
        /// gets one and the session's run is never touched.
        /// </para>
        ///
        /// <para><b>Every "no" is recorded</b></para>
        /// <para>
        /// Not asked for; the run passed; it did not reach a terminal condition an envelope answers;
        /// nothing eligible left to target; no useful headroom; an unresolvable capacity; a vector that
        /// cannot be formed - each is written to
        /// <see cref="PartOOptimisationRun.CapacityEnvelopeDescription"/> in its own words. An optional
        /// diagnostic that silently produces nothing leaves a reader unable to tell it was considered.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Internal rather than private so the no-run decisions are pinned by tests. Every one of them -
        /// not asked for, the run passed, the wrong terminal condition, no failing verdict, nothing left to
        /// target, and an envelope that reached no ceiling - is settled <b>before</b> any TAS work, so they
        /// can be, and are, asserted without a simulation standing behind them.
        /// </remarks>
        internal static void CapacityEnvelope(PartOOptimisationRun partOOptimisationRun, PartOOptimisationSettings partOOptimisationSettings, PartOPreparationContext partOPreparationContext, PartOSimulationContext partOSimulationContext)
        {
            if (!partOOptimisationSettings.CapacityEnvelope)
            {
                partOOptimisationRun.CapacityEnvelopeDescription = "The diagnostic selected-equipment capacity envelope was not asked for, so none was calculated.";

                return;
            }

            //Passing is the whole point of the optimisation, and there is nothing to diagnose about a design
            //that met its criteria. Said explicitly rather than left silent, so a reader knows the
            //diagnostic was considered and correctly declined.
            if (partOOptimisationRun.StopReason == PartOOptimisationStopReason.Passed)
            {
                partOOptimisationRun.CapacityEnvelopeDescription = "The optimisation reached a design in which every eligible occupied space passes its production TM59 criteria, so there is nothing for a capacity envelope to diagnose and none was calculated.";

                return;
            }

            //The two terminal conditions an envelope answers. Every other stop is a run that did not finish
            //rather than a design limited by its equipment - a refused round, a preparation or simulation
            //that failed, an assessment that produced nothing, a cancellation - and enveloping from one
            //would be diagnosing a design whose own optimisation never established what it could do.
            if (partOOptimisationRun.StopReason != PartOOptimisationStopReason.CapacityReached && partOOptimisationRun.StopReason != PartOOptimisationStopReason.IterationLimitReached)
            {
                partOOptimisationRun.CapacityEnvelopeDescription = string.Format(
                    "The optimisation stopped at '{0}', which is a run that did not finish rather than a design limited by the equipment selected for it, so no capacity envelope was calculated. An envelope answers a stop on the selected unit's capacity or on the iteration guard; from any other it would diagnose a design whose own optimisation never established what it could do.",
                    Core.Query.Description(partOOptimisationRun.StopReason));

                return;
            }

            PartOOptimisationStep? partOOptimisationStep_LastValid = partOOptimisationRun.Step_LastValid;

            AnalyticalModel? analyticalModel_LastValid = partOOptimisationRun.AnalyticalModel_LastValid;

            AdjacencyCluster? adjacencyCluster = analyticalModel_LastValid?.AdjacencyCluster;

            if (partOOptimisationStep_LastValid is null || adjacencyCluster is null)
            {
                partOOptimisationRun.CapacityEnvelopeDescription = "The optimisation left no valid design to calculate a capacity envelope from, so none was calculated.";

                return;
            }

            //An explicit failure, exactly as the optimisation itself requires. "Not Pass" is not "Fail", and
            //enveloping from an assessment that reached no verdict would scale a design towards its
            //equipment's ceiling on the strength of results that said nothing about any of its rooms.
            if (partOOptimisationStep_LastValid.OccupiedSpaceComplianceStatus != TM59ComplianceStatus.Fail)
            {
                partOOptimisationRun.CapacityEnvelopeDescription = string.Format(
                    "The last valid design's production TM59 status is '{0}', which is not a failure, so there is no failing room to say which equipment a capacity envelope would be about, and none was calculated.",
                    Core.Query.Description(partOOptimisationStep_LastValid.OccupiedSpaceComplianceStatus));

                return;
            }

            // ---- The SCOPE: which equipment the failing rooms sit on --------------------------------------

            //The SAME policy, at the SAME step, over the SAME production results - so the envelope diagnoses
            //the equipment serving the rooms the optimisation would next have pushed, and not some other set
            //of dwellings. Notably NOT filtered by the dwellings the optimisation marked at capacity: those
            //are precisely the ones whose equipment ceiling is the thing being diagnosed.
            //
            //Only the SCOPE. The envelope reads no FIGURE from this vector - it grows the whole last valid
            //design of each unit these rooms resolve to, proportionally, to that unit's own ceiling. See
            //Analytical.Modify.EvaluateDesignAirFlowCapacityEnvelope for why the +5 l/s increments are the
            //wrong thing to spend the remaining headroom on.
            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(analyticalModel_LastValid, partOOptimisationStep_LastValid.TM59Results, partOPreparationContext.Zones, partOOptimisationSettings.AirFlowStep_Lps);

            if (partOOptimisationTargetSelection.Targets.Count == 0)
            {
                //What was learned belongs to the design it was learned about, so the reasons go on the step
                //that produced that design - the same rule the optimisation follows.
                partOOptimisationStep_LastValid.Notes.AddRange(partOOptimisationTargetSelection.NotOptimisable);

                partOOptimisationRun.CapacityEnvelopeDescription = "No failing space remained that could be targeted, so there is no deliberate target vector to say which equipment a capacity envelope would be about, and none was calculated.";

                return;
            }

            // ---- The envelope itself, calculated by SAM and adopted by nobody -----------------------------

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = adjacencyCluster.EvaluateDesignAirFlowCapacityEnvelope(
                partOOptimisationTargetSelection.Targets,
                PartFExtractAllocationStrategy.MinimumFirstCookingPriority,
                partOOptimisationSettings.Tolerance_Lps,
                partOPreparationContext.VentilationUnitCapacityDescriptors);

            partOOptimisationRun.CapacityEnvelope = designAirFlowCapacityEnvelope;
            partOOptimisationRun.CapacityEnvelopeDescription = designAirFlowCapacityEnvelope.Reason;

            if (!designAirFlowCapacityEnvelope.IsScaled)
            {
                //No design to simulate. Nothing is run: a diagnostic with nothing to say must not cost a
                //full-year TAS run to say it.
                partOOptimisationStep_LastValid.Notes.AddRange(designAirFlowCapacityEnvelope.Notes);
                partOOptimisationStep_LastValid.Notes.AddRange(designAirFlowCapacityEnvelope.Refusals);

                //Each equipment group's OWN reason is carried up onto the one line a reader sees. The
                //envelope's overall sentence only says that no group reached a ceiling; which of "this unit
                //is already at its rating", "nothing is selected on it" and "its capacity is not in the
                //catalogue offered" it was is the whole diagnostic, and leaving it further down the notes
                //would bury it.
                List<string> reasons = designAirFlowCapacityEnvelope.Groups.ConvertAll(x => x.Reason);

                reasons.RemoveAll(string.IsNullOrWhiteSpace);

                partOOptimisationRun.CapacityEnvelopeDescription = string.Join(" ", [designAirFlowCapacityEnvelope.Reason, .. reasons, .. designAirFlowCapacityEnvelope.Refusals]);

                return;
            }

            //Its own step, its own kind and its own project name. The iteration number continues the run's
            //sequence so the step reads in order; the FILE name carries Max, which is what keeps the
            //diagnostic out of the rounds' numbering - see PartOSimulationContext.ProjectName_CapacityEnvelope.
            //
            //The file name is also qualified by THIS SESSION'S baseline, read off the run's own step 0 -
            //the same iteration baseline its rounds are numbered from. Without it the envelope was the one
            //piece of an optimisation's evidence a second optimisation destroyed: the rounds of a run
            //started from -Opt05 number from -Opt06 and collide with nothing, while its envelope was
            //-OptMax both times and overwrote the first session's TBD, TSD, run model and TM59 report.
            //Step_Baseline is the run's own record of what it started from, so this cannot disagree with
            //the numbering the rounds actually used.
            int iteration_Baseline = PartOSimulationContext.Iteration_ProjectName(partOOptimisationRun.Step_Baseline?.ProjectName);

            PartOOptimisationStep partOOptimisationStep = new(PartOSimulationContext.Iteration_ProjectName(partOOptimisationStep_LastValid.ProjectName) + 1, PartOOptimisationStepKind.CapacityEnvelope)
            {
                ProjectName = partOSimulationContext.ProjectName_CapacityEnvelope(iteration_Baseline),
                WeatherData = partOSimulationContext.WeatherData?.Name,
            };

            partOOptimisationRun.Steps.Add(partOOptimisationStep);

            partOOptimisationStep.TargetedAdjustments.AddRange(designAirFlowCapacityEnvelope.TargetedAdjustments);
            partOOptimisationStep.DerivedAdjustments.AddRange(designAirFlowCapacityEnvelope.DerivedAdjustments);
            partOOptimisationStep.Notes.AddRange(partOOptimisationTargetSelection.NotOptimisable);
            partOOptimisationStep.Notes.AddRange(designAirFlowCapacityEnvelope.Notes);
            partOOptimisationStep.Warnings.AddRange(designAirFlowCapacityEnvelope.Warnings);

            //The ENVELOPE's own dropped targets, not its round's. Its round is given the design that already
            //exists, which every room can carry by definition, so RoundCandidate.TargetRefusals is empty -
            //the failing rooms the ordinary policy has no lever for are dropped when the envelope resolves
            //its scope, and that is where they are recorded.
            partOOptimisationStep.TargetRefusals.AddRange(designAirFlowCapacityEnvelope.TargetRefusals);

            AnalyticalModel analyticalModel_Envelope = new(analyticalModel_LastValid, designAirFlowCapacityEnvelope.AdjacencyCluster);

            // ---- Rebuild the real Part O state around the envelope design ---------------------------------

            //NULL catalogue, for exactly the reason every round passes null: given one, the preparation runs
            //its smallest-capable-unit rule against the realized duty and would quietly buy the next product
            //up - which for an envelope would be absurd, since the envelope's whole subject is what the
            //CURRENT product can deliver.
            PartOIterationPreparation partOIterationPreparation = Analytical.Modify.PreparePartOIteration(analyticalModel_Envelope, partOPreparationContext.PartOIteration, partOPreparationContext.Zones, partOPreparationContext.VentilationStrategies, null);

            partOOptimisationStep.Notes.AddRange(partOIterationPreparation.Notes);
            partOOptimisationStep.Warnings.AddRange(partOIterationPreparation.Warnings);

            //A PRIVATE run, so the session's own - which holds the last accepted ordinary design and its
            //results - is not touched. See the method documentation.
            PartORun partORun_Envelope = new();

            if (!partORun_Envelope.Prepare(partOIterationPreparation, partOPreparationContext))
            {
                partOOptimisationStep.Refusals.Add(partOIterationPreparation.Refusal ?? partORun_Envelope.InvalidationReason ?? "The Part O iteration could not be re-prepared over the capacity envelope design.");

                partOOptimisationRun.CapacityEnvelopeDescription = Describe(designAirFlowCapacityEnvelope, partOOptimisationStep);

                return;
            }

            // ---- The same full-year case, under the envelope's own name -----------------------------------

            using CancellationTokenSource cancellationTokenSource = new();

            //The envelope is one more full-year run of the same building, so it warm starts on exactly the
            //same terms as a round - and is checked on exactly the same terms too.
            PartOCanonicalTBD partOCanonicalTBD_Envelope = WarmStart(partOOptimisationRun.CanonicalTBD, analyticalModel_LastValid, partOSimulationContext, partOOptimisationStep);

            //Started only here, where the envelope really simulates; one declined above leaves its stage
            //unstarted rather than shown as run.
            PartOProgressHost.Current?.Start(PartOOptimisationPhase_CapacityEnvelope);

            AnalyticalModel analyticalModel_Workflow = RunPartOSimulation(partOIterationPreparation.AnalyticalModel, partOSimulationContext, partOOptimisationStep.ProjectName, partORun_Envelope, cancellationTokenSource.Token, out string _, out string path_TSD, out bool cancelled, out bool fullYear, out List<string> notes_Simulation, out string refusal_Simulation, partOCanonicalTBD_Envelope);

            partOOptimisationStep.Notes.AddRange(notes_Simulation);
            partOOptimisationStep.Path_TSD = path_TSD;

            if (cancelled)
            {
                partOOptimisationStep.Refusals.Add("The capacity envelope was cancelled during its simulation, so it has no results. The optimisation's own answer above is unaffected.");

                partOOptimisationRun.CapacityEnvelopeDescription = Describe(designAirFlowCapacityEnvelope, partOOptimisationStep);

                return;
            }

            if (analyticalModel_Workflow is null || !fullYear)
            {
                partOOptimisationStep.Refusals.Add(refusal_Simulation ?? (analyticalModel_Workflow is null
                    ? "The TAS workflow did not run over the capacity envelope design, so there are no results to assess."
                    : "The simulation that ran over the capacity envelope design was not the full year a TM59 assessment reads."));

                partOOptimisationRun.CapacityEnvelopeDescription = Describe(designAirFlowCapacityEnvelope, partOOptimisationStep);

                return;
            }

            if (!partORun_Envelope.Complete(analyticalModel_Workflow, path_TSD, partOSimulationContext, out string refusal_Complete))
            {
                partOOptimisationStep.Refusals.Add(refusal_Complete);

                partOOptimisationRun.CapacityEnvelopeDescription = Describe(designAirFlowCapacityEnvelope, partOOptimisationStep);

                return;
            }

            // ---- Production TM59, on the model the workflow returned, stored as the ENVELOPE's ------------

            PartOTM59Assessment partOTM59Assessment = PartOTM59Assessment.Assess(partORun_Envelope.AnalyticalModel_Assessment, partORun_Envelope.Path_TSD, partORun_Envelope.OverheatingScenarios);

            Record(partOOptimisationStep, partORun_Envelope.AnalyticalModel_Assessment, partOPreparationContext, partOTM59Assessment);

            if (!partOTM59Assessment.IsAssessed)
            {
                partOOptimisationStep.Refusals.Add(partOTM59Assessment.Refusal ?? "The production TM59 assessment could not be produced for the capacity envelope design.");

                partOOptimisationRun.CapacityEnvelopeDescription = Describe(designAirFlowCapacityEnvelope, partOOptimisationStep);

                return;
            }

            //Completed - and completed as a CAPACITY ENVELOPE, which is why PartOOptimisationRun.Rounds and
            //Step_LastValid both exclude this kind. It is a finished diagnostic, not a finished round.
            partOOptimisationStep.IsCompleted = true;

            partOOptimisationRun.AnalyticalModel_CapacityEnvelope = partORun_Envelope.AnalyticalModel_Assessment;
            partOOptimisationRun.Path_TSD_CapacityEnvelope = partORun_Envelope.Path_TSD;

            foreach (OverheatingScenario overheatingScenario in partORun_Envelope.OverheatingScenarios)
            {
                if (overheatingScenario is not null)
                {
                    partOOptimisationRun.OverheatingScenarios_CapacityEnvelope.Add(overheatingScenario);
                }
            }

            partOOptimisationRun.CapacityEnvelopeDescription = Describe(designAirFlowCapacityEnvelope, partOOptimisationStep);
        }

        /// <summary>
        /// The envelope in one paragraph, worded so it cannot be mistaken for an optimisation result.
        /// <para>
        /// It states what the equipment could deliver and what TM59 then made of that - and says, in those
        /// words, that this is a diagnostic and that the design the optimisation accepted is unchanged.
        /// </para>
        /// </summary>
        private static string Describe(DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope, PartOOptimisationStep partOOptimisationStep)
        {
            List<string> scales = designAirFlowCapacityEnvelope.Groups_Scaled.ConvertAll(x => string.Format(
                "'{0}' x{1:0.###} to {2:0.###}/{3:0.###} l/s of {4:0.###}/{5:0.###} l/s",
                x.Name,
                x.Scale,
                x.SupplyDuty_After_Lps,
                x.ExtractDuty_After_Lps,
                x.VentilationUnitCapacityDescriptor?.MaximumSupplyFlowRate_Lps ?? double.NaN,
                x.VentilationUnitCapacityDescriptor?.MaximumExtractFlowRate_Lps ?? double.NaN));

            return string.Format(
                "DIAGNOSTIC ONLY - this is not an optimisation round and the design the optimisation accepted is unchanged. Taking the already-selected ventilation unit(s) to their own design-capacity ceiling: {0}. {1} No product was reselected, no Approved Document F requirement was altered and no operating airflow was written. Production TM59 for that design: {2}.{3}",
                scales.Count == 0 ? "no equipment group reached a ceiling" : string.Join("; ", scales),
                partOOptimisationStep.IsCompleted
                    ? string.Format("Simulated over the same full-year weather case as its own run '{0}', results '{1}'.", partOOptimisationStep.ProjectName ?? "-", partOOptimisationStep.Path_TSD ?? "-")
                    : "It was NOT successfully simulated and assessed - see its refusals.",
                partOOptimisationStep.IsCompleted ? Core.Query.Description(partOOptimisationStep.OccupiedSpaceComplianceStatus) : "not established",
                partOOptimisationStep.Refusals.Count == 0 ? string.Empty : " " + string.Join(" ", partOOptimisationStep.Refusals));
        }

        /// <summary>
        /// Whether this iteration may start from the run's canonical TBD - and, where it may not, the reason
        /// recorded on the iteration that had to convert in full.
        ///
        /// <para><b>Why the check is per iteration</b></para>
        /// <para>
        /// A canonical TBD is a file on disk for the several minutes an optimisation runs, and anything with
        /// access to that directory can replace it. Checking once at adoption would leave every later round
        /// reusing a file on the strength of its path alone - which is precisely the stale-conversion
        /// failure this whole design exists to avoid. So the check is repeated, and a single round can fall
        /// back while the others do not.
        /// </para>
        /// <para>
        /// <b>A fallback is a note, not a refusal.</b> The full conversion is always available and always
        /// authoritative, so a round that cannot warm start simply runs the workflow every round ran before
        /// this existed. Nothing about its result is different or weaker; only its duration is.
        /// </para>
        /// </summary>
        /// <returns>The canonical TBD to start from, or null for the full conversion.</returns>
        private static PartOCanonicalTBD? WarmStart(PartOCanonicalTBD? partOCanonicalTBD, AnalyticalModel? analyticalModel, PartOSimulationContext partOSimulationContext, PartOOptimisationStep partOOptimisationStep)
        {
            if (partOCanonicalTBD is null)
            {
                return null;
            }

            if (!partOCanonicalTBD.IsValidFor(analyticalModel, partOSimulationContext, out string? refusal))
            {
                partOOptimisationStep.Notes.Add(refusal ?? "The canonical TBD could not be shown to be valid for this iteration, so it converted the model in full.");

                return null;
            }

            partOOptimisationStep.WarmStarted = true;

            partOOptimisationStep.Notes.Add(string.Format(
                "This iteration started from the canonical TBD '{0}', copied to its own '{1}.tbd'. The geometry, constructions, apertures and shading calculation were not recomputed - they are unchanged by a design airflow round. The zone identities, the zones, the ventilation network and the full-year simulation all were.",
                partOCanonicalTBD.Path_TBD,
                partOOptimisationStep.ProjectName ?? "?"));

            return partOCanonicalTBD;
        }

        /// <summary>
        /// Ends the run with an explicit reason and the last design that was actually valid.
        /// <para>
        /// <b>The last valid model is always the one from a step that completed</b> - prepared, simulated
        /// over the full year and assessed - never a round that was refused, cancelled or unsimulated. That
        /// is what makes a <see cref="PartOOptimisationStopReason.CapacityReached"/> stop usable: the design
        /// it leaves behind is a real, simulated, assessed one step below the unit's ceiling.
        /// </para>
        /// </summary>
        private static PartOOptimisationRun Stop(PartOOptimisationRun partOOptimisationRun, PartOOptimisationStopReason partOOptimisationStopReason, string? description, AnalyticalModel? analyticalModel, string? path_TSD, IEnumerable<OverheatingScenario>? overheatingScenarios)
        {
            partOOptimisationRun.StopReason = partOOptimisationStopReason;
            partOOptimisationRun.StopDescription = description;

            partOOptimisationRun.AnalyticalModel_LastValid = analyticalModel;
            partOOptimisationRun.Path_TSD_LastValid = path_TSD;

            foreach (OverheatingScenario overheatingScenario in overheatingScenarios ?? [])
            {
                if (overheatingScenario is not null)
                {
                    partOOptimisationRun.OverheatingScenarios_LastValid.Add(overheatingScenario);
                }
            }

            return partOOptimisationRun;
        }
    }
}
