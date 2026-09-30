// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// The high-level Approved Document O command: one dialog that says what the model already provides,
        /// and one button that carries a scenario and a dwelling scope all the way to a reviewed TM59 result.
        ///
        /// <para><b>Orchestration only - there is no second Part O implementation here</b></para>
        /// <para>
        /// Every stage is an existing command, called in the order they have always run in:
        /// </para>
        /// <list type="number">
        /// <item><see cref="PreparePartOIteration(UIAnalyticalModel, PartORun, PartOWorkflowRequest, VentilationUnitCatalogue, IWin32Window)"/>
        /// - which is <c>SAM.Analytical.Modify.PreparePartOIteration</c> plus the summary a person accepts it
        /// on, and which the Prepare Iteration picker also calls.</item>
        /// <item><see cref="SimulatePartO"/> - the Simulate dialog's own core, with its three open inputs taken
        /// from the Hub's Simulation case rather than from a dialog, and beneath it
        /// <see cref="RunPartOSimulation"/>, which is where <c>SAMAnalytical.Check</c> gates the normalized
        /// model before TAS converts it and where the run is completed.</item>
        /// <item><see cref="AssessPartOTM59(PartORun, IWin32Window)"/> - the production TM59 assessment over
        /// the model the workflow returned.</item>
        /// </list>
        /// <para>
        /// Nothing is inlined, nothing is reimplemented, and no engineering decision is taken here. A Part O
        /// behaviour that changes in one of those methods changes here with it.
        /// </para>
        ///
        /// <para><b>Already-valid work is reused, and the model is what says so</b></para>
        /// <para>
        /// Where the session's run is already prepared for exactly this scenario and dwelling scope -
        /// <see cref="PartOWorkflowInspection.ReusePreparation"/>, decided by comparing the request against
        /// the preparation's own <see cref="PartOPreparationContext"/> - the preparation is skipped and the
        /// prepared model is simulated as it stands. Anything else differing prepares again. There is no
        /// UI-side cache: the run and the model are re-read on every showing of the dialog.
        /// </para>
        ///
        /// <para><b>Existing results are reviewed, never re-run</b></para>
        /// <para>
        /// Review Results and Optimise (2B) are the same two commands the Results tab exposes, gated by the
        /// same two authorities - <c>PartORun.IsAssessable</c> and <c>Modify.CanOptimise</c> - so a reopened
        /// <c>.sam</c> whose provenance validates is reviewable here without a TAS run, and one whose
        /// provenance does not is refused here for the reason that authority gives.
        /// </para>
        ///
        /// <para><b>The dialog is a hub, not a wizard</b></para>
        /// <para>
        /// It reopens after each action with its status rebuilt, so the run a person just completed is
        /// visible as READY and the follow-on 2B becomes available in the place they were already looking.
        /// The choices they made are carried across; the state is not - it is re-inspected every time.
        /// </para>
        /// </summary>
        /// <param name="uIAnalyticalModel">
        /// The design model. Every case derives from it and the window stays on it (PR-4): only the Part O inputs
        /// confirmed in a review are ever written onto it. A Part O result opened here can be reviewed, not run from.
        /// </param>
        /// <param name="partORun">The session's Part O run.</param>
        /// <param name="owner">Owner window for the dialogs.</param>
        public static void RunPartOWorkflow(this UIAnalyticalModel? uIAnalyticalModel, PartORun partORun, IWin32Window? owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject is null || partORun is null)
            {
                return;
            }

            //Read once, outside the loop: the read touches a file, and the dialog rebuilds its status on
            //every keystroke. Re-reading between rounds could also change what a selected unit is understood
            //to be rated at halfway through a session.
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

            PartOWorkflowScenario? partOWorkflowScenario = null;
            PartOWorkflowScope partOWorkflowScope = PartOWorkflowScope.AllDwellings;
            List<Guid>? guids_Dwelling = null;
            //The Iteration 2B settings last confirmed in this session - only a pre-fill for the next
            //confirmation; the settings a run uses are the ones confirmed when it starts.
            PartOOptimisationSettings? partOOptimisationSettings = null;
            PartOEquipmentSelection? partOEquipmentSelection = null;
            PartOProjectTestVentilationUnit? partOProjectTestVentilationUnit = null;
            bool stated_ProjectTestVentilationUnit = false;

            //Carried across showings, like the choices above: the Iteration 3 method, the Simulation case
            //(first taken exactly as the Simulate dialog would have opened), and the line saying what the last
            //action did - which is where a successful run's completion is reported, instead of a message box.
            PartOIteration3BehaviourMode partOIteration3BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance;
            bool iteration3InFocus = false;
            PartOSimulationCase? partOSimulationCase = null;
            PartOWorkflowOutcome? partOWorkflowOutcome = null;

            while (true)
            {
                PartOWorkflowCapabilities partOWorkflowCapabilities = Capabilities(partORun, uIAnalyticalModel.JSAMObject, out PartOIteration3Eligibility? partOIteration3Eligibility);

                if (partOSimulationCase is null)
                {
                    ActiveSetting.Setting.TryGetValue(AnalyticalSettingParameter.SimulateOptions_PartO, out SimulateOptions simulateOptions_Remembered);

                    partOSimulationCase = PartOSimulationCase.Create(uIAnalyticalModel.JSAMObject, uIAnalyticalModel.Path, simulateOptions_Remembered);
                }

                PartOWorkflowWindow partOWorkflowWindow = new()
                {
                    //Order matters: the model builds the dwelling list the restored selection is applied to.
                    AnalyticalModel = uIAnalyticalModel.JSAMObject,
                    PartORun = partORun,
                    VentilationUnitCatalogue = ventilationUnitCatalogue,
                    Capabilities = partOWorkflowCapabilities,
                    Iteration3Eligibility = partOIteration3Eligibility,
                    Iteration3Mode = partOIteration3BehaviourMode,
                    SimulationCase = partOSimulationCase,
                    LastOutcome = partOWorkflowOutcome,
                    Iteration3InFocus = iteration3InFocus,
                };

                partOWorkflowWindow.Restore(partOWorkflowScenario, partOWorkflowScope, guids_Dwelling);

                //Carried across the loop, on top of what the model said. A prepare that refused, or a
                //dialog that was closed, leaves the project's stored preselection untouched - so without
                //this the engineer's unsaved change of mode or pool would be silently discarded the moment
                //the window reopened. Only where a previous showing actually stated one.
                //The test product first, for the reason the window's own writer gives: the pool is restored
                //by ticking catalogue rows, and the test product has no row until it has been stated.
                //
                //A separate "was it stated" flag rather than a null check, because null is a MEANINGFUL
                //previous answer here - a previous showing where the engineer switched the test product OFF
                //has to survive a refused prepare exactly as one where they switched it on does.
                if (stated_ProjectTestVentilationUnit)
                {
                    partOWorkflowWindow.ProjectTestVentilationUnit = partOProjectTestVentilationUnit;
                }

                if (partOEquipmentSelection is not null)
                {
                    partOWorkflowWindow.EquipmentSelection = partOEquipmentSelection;
                }

                //Setting up is finished, so the ONE inspection this whole gesture owes is paid here - over
                //the fully restored state, which is the only one anybody will ever see. Every line above
                //moves an inspection input, and each used to be answered with an inspection of its own: nine
                //passes over the dwelling scope to show one window, on a model that may carry five thousand
                //spaces. Nothing is skipped and nothing is remembered; see CompleteInitialisation.
                partOWorkflowWindow.CompleteInitialisation();

                if (owner is not null)
                {
                    new System.Windows.Interop.WindowInteropHelper(partOWorkflowWindow).Owner = owner.Handle;
                }

                bool? showDialog = partOWorkflowWindow.ShowDialog();

                //Carried whether the dialog was accepted or closed, so reopening the command in the same
                //session does not start over.
                partOWorkflowScenario = partOWorkflowWindow.Scenario;
                partOWorkflowScope = partOWorkflowWindow.Scope;
                partOEquipmentSelection = partOWorkflowWindow.EquipmentSelection;
                partOProjectTestVentilationUnit = partOWorkflowWindow.ProjectTestVentilationUnit;
                stated_ProjectTestVentilationUnit = true;
                partOIteration3BehaviourMode = partOWorkflowWindow.Iteration3Mode;
                partOSimulationCase = partOWorkflowWindow.SimulationCase;

                //Which case the person was working in, so the reopened Hub's primary Review action opens THAT
                //case's result: Iteration 3 after an Iteration 3 run or opened result, otherwise the scenario's.
                iteration3InFocus = partOWorkflowWindow.Action == PartOWorkflowAction.Iteration3
                    || partOWorkflowWindow.Action == PartOWorkflowAction.Iteration3Review
                    || (partOWorkflowWindow.Iteration3InFocus && partOWorkflowWindow.Action is not PartOWorkflowAction.PrepareAndRun and not PartOWorkflowAction.Optimise and not PartOWorkflowAction.ReviewResults);

                guids_Dwelling = [];
                foreach (Zone zone in partOWorkflowWindow.Zones_Dwelling)
                {
                    guids_Dwelling.Add(zone.Guid);
                }

                if (showDialog is null || !showDialog.Value)
                {
                    return;
                }

                switch (partOWorkflowWindow.Action)
                {
                    case PartOWorkflowAction.PrepareAndRun:
                        partOWorkflowOutcome = PrepareAndRun(uIAnalyticalModel, partORun, partOWorkflowWindow.Request, partOWorkflowWindow.Inspection, ventilationUnitCatalogue, partOSimulationCase, partOWorkflowWindow.Scenario, owner);
                        break;

                    case PartOWorkflowAction.ReviewResults:
                        //The same summary instance the result window was given - see ReviewOutcome.
                        partOWorkflowOutcome = ReviewOutcome(ReviewPartOTM59(partORun, owner), partORun);
                        break;

                    case PartOWorkflowAction.Optimise:
                        {
                            //Worded from the run's own stop reason. Where nothing started - the confirmation was
                            //cancelled, or the run was refused (that refusal was shown) - the previous line is
                            //kept; HubOutcome still shows it only while the run it describes holds.
                            PartOOptimisationRun? partOOptimisationRun = RunPartOOptimisationResult(uIAnalyticalModel, partORun, owner, partOOptimisationSettings, out PartOOptimisationSettings? partOOptimisationSettings_Confirmed);

                            if (partOOptimisationSettings_Confirmed is not null)
                            {
                                partOOptimisationSettings = partOOptimisationSettings_Confirmed;

                                partOWorkflowOutcome = OptimisationOutcome(partOOptimisationRun) ?? partOWorkflowOutcome;
                            }
                        }
                        break;

                    case PartOWorkflowAction.Iteration3:
                        partOWorkflowOutcome = RunPartOIteration3Case(partORun, partOIteration3BehaviourMode, owner) ?? partOWorkflowOutcome;
                        break;

                    case PartOWorkflowAction.Iteration3Review:
                        partOWorkflowOutcome = ReviewPartOIteration3Case(partORun, partOIteration3BehaviourMode, owner) ?? partOWorkflowOutcome;
                        break;

                    default:
                        return;
                }
            }
        }

        /// <summary>
        /// Prepare - or reuse a preparation - then simulate, then assess. The three existing commands, in the
        /// one order they have always run in.
        /// <para>
        /// <b>Each step is gated by the previous one's own outcome</b>, read off the run rather than assumed.
        /// A preparation the user declined leaves the run unprepared and nothing is simulated; a simulation
        /// that was cancelled, refused by the pre-simulation check or not a full year leaves the run
        /// uncompleted - with its own reason already shown - and nothing is assessed.
        /// </para>
        /// <para>
        /// <b>Every decision is still a person's, and nothing is chosen silently.</b> The review of the prepared
        /// iteration is still its own window. The Simulate dialog is not: its only open inputs - the weather,
        /// the output folder and the solar method - are the Hub's Simulation case, typed and visible before Run
        /// is pressed, and the rest of the case is the locked Part O case it always was
        /// (<see cref="SimulatePartO"/>). The full-year range was never a choice on this route.
        /// </para>
        /// <para>
        /// <b>One progress window, and no success box.</b> <see cref="PartOProgressHost"/> covers the whole
        /// operation and stands aside for the review and for anything modal. A clean completion is returned as
        /// the Hub's inline line; notes, refusals and a run that was not completed are still shown.
        /// </para>
        /// <para>
        /// <b>The reuse path records the current Iteration 2B choice.</b> A reused preparation is the same
        /// ENGINEERING case, which is what <see cref="PartOWorkflowInspection.ReusePreparation"/> matches on;
        /// the 2B step and limit are not engineering and were therefore not matched - but they are read off
        /// the run afterwards by <c>Modify.CanOptimise</c>, so they have to be the ones this invocation asked
        /// for. <see cref="PartORun.AdoptOptimisationSettings"/> is that record, and where the run will not
        /// take it the preparation is rebuilt instead - correctness before saving a preparation.
        /// </para>
        /// </summary>
        /// <returns>The line the Hub shows about what happened (see PartOHubOutcome), or null where nothing was started.</returns>
        private static PartOWorkflowOutcome? PrepareAndRun(UIAnalyticalModel uIAnalyticalModel, PartORun partORun, PartOWorkflowRequest partOWorkflowRequest, PartOWorkflowInspection? partOWorkflowInspection, VentilationUnitCatalogue ventilationUnitCatalogue, PartOSimulationCase? partOSimulationCase, PartOWorkflowScenario? partOWorkflowScenario, IWin32Window? owner)
        {
            string iteration = partOWorkflowScenario?.ToString() ?? "Part O iteration";
            string name = partOWorkflowScenario?.Name ?? iteration;

            bool reuse = ReuseWithCurrentOptimisation(partORun, partOWorkflowRequest, partOWorkflowInspection?.ReusePreparation ?? false);

            PartOSimulationOutcome partOSimulationOutcome;
            PartOTM59ResultSummary? partOTM59ResultSummary = null;
            TimeSpan? elapsed_Simulation;

            //ONE progress window for the whole of Prepare & Run. It stands aside for the review of the
            //prepared iteration - the engineer's decision - and for anything modal, and closes before the TM59
            //result opens.
            using (PartOProgressHost partOProgressHost = new(
                string.Format("Prepare & Run — {0}", iteration),
                reuse ? "The iteration already prepared for this scenario and scope is reused." : null,
                [PartOProgressStages.PrepareAndReview, PartOProgressStages.BuildingSimulationFullYear, PartOProgressStages.Tm59Assessment]))
            {
                //Reused only where the run's own record of what it was prepared with describes this request.
                //Otherwise prepared again - including where an iteration is prepared for something else, which
                //would otherwise be simulated as though it were this one.
                if (!reuse)
                {
                    partOProgressHost.Start(0);

                    PartOPreparationResult partOPreparationResult = PrepareAndReviewPartOIteration(uIAnalyticalModel, partORun, partOWorkflowRequest, ventilationUnitCatalogue, ReviewIntent_PrepareAndRun, owner);

                    if (partOPreparationResult == PartOPreparationResult.Declined)
                    {
                        //The engineer said no at the review. TAS is not reached, and the Hub says so rather
                        //than coming back as though nothing had been asked.
                        return DeclinedOutcome(iteration);
                    }

                    if (!ContinuesToSimulation(ReviewIntent_PrepareAndRun, partOPreparationResult))
                    {
                        //Refused or not adopted. Each of those has already told the user why.
                        return null;
                    }
                }

                //Not an assumption: PreparePartOIteration returns true only for an adopted preparation, and the
                //reuse path was reached only from a Prepared run - but a model modified between the inspection
                //and here drops the run, and simulating an unprepared run would silently produce a result that
                //cannot be assessed.
                if (partORun.State != PartORunState.Prepared)
                {
                    partOProgressHost.Hide();

                    MessageBox.Show(string.Format("The Part O iteration is no longer prepared, so nothing was simulated.\n\n{0}", partORun.InvalidationReason ?? "Prepare the iteration again."), "Part O — Prepare & Run");

                    return null;
                }

                partOProgressHost.Start(1);

                //The SAM Check gate, the TAS workflow, the run completion and the run's own persisted evidence -
                //all of it the same core the Simulate dialog used, with the three inputs it left open taken
                //from the Hub's Simulation case. See Modify.SimulatePartO.
                partOSimulationOutcome = SimulatePartO(uIAnalyticalModel, partORun, partOSimulationCase);

                elapsed_Simulation = partOProgressHost.State.Duration(1);

                if (partOSimulationOutcome.NeedsAttention || !partORun.CanAssess)
                {
                    //Failed only where the run cannot be assessed; a completed run with notes did complete.
                    if (!partORun.CanAssess)
                    {
                        partOProgressHost.State.Fail();
                    }

                    partOProgressHost.Hide();

                    //What the Simulate dialog's closing box said, where there is something to read: a refusal,
                    //a run that was not completed, zone-identity notes. A clean success says nothing here.
                    string text = Attention(partOSimulationOutcome);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        MessageBox.Show(text, "Part O — Prepare & Run");
                    }

                    //Back only where there is still a stage to watch. A cancelled or uncompleted run goes
                    //nowhere after this box, so showing the window again only flashed it up before it
                    //closed - after the "Simulation cancelled" message had already ended the operation.
                    if (partORun.CanAssess)
                    {
                        partOProgressHost.Show();
                    }
                }

                if (partORun.CanAssess)
                {
                    partOProgressHost.Start(2);

                    partOTM59ResultSummary = ReviewPartOTM59(partORun, owner);
                }
            }

            if (partOSimulationOutcome.Cancelled)
            {
                return SimulationCancelledOutcome(name);
            }

            if (!partORun.CanAssess)
            {
                return NotCompletedOutcome(name, partOSimulationOutcome.Refusal ?? partOSimulationOutcome.Note_PartORun);
            }

            //The verdict is the summary the result window was given - see CompletedOutcome.
            return CompletedOutcome(name, elapsed_Simulation ?? partOSimulationOutcome.Elapsed, partOTM59ResultSummary, partOSimulationOutcome.Notes.Count);
        }

        /// <summary>
        /// What Prepare &amp; Run's review promises: acceptance continues into TAS and TM59, which is what
        /// this command does next.
        /// </summary>
        internal const PartOReviewIntent ReviewIntent_PrepareAndRun = PartOReviewIntent.PrepareAndRun;

        /// <summary>
        /// Whether a command goes on into the TAS simulation after its review: only a command whose intent is
        /// to run, and only where the review was accepted and the model adopted. Prepare &amp; Run's gate before
        /// <c>SimulatePartO</c>; the Prepare Iteration command has no simulation step at all, and this says why
        /// it must never grow one to match a label.
        /// </summary>
        internal static bool ContinuesToSimulation(PartOReviewIntent partOReviewIntent, PartOPreparationResult partOPreparationResult)
        {
            return partOReviewIntent == PartOReviewIntent.PrepareAndRun && partOPreparationResult == PartOPreparationResult.Adopted;
        }

        /// <summary>
        /// What a Part O simulation has to tell a person beyond "it completed" - the parts of the Simulate
        /// dialog's closing message that were never merely an acknowledgement.
        /// </summary>
        private static string Attention(PartOSimulationOutcome partOSimulationOutcome)
        {
            List<string> parts = [];

            if (!string.IsNullOrWhiteSpace(partOSimulationOutcome.Refusal))
            {
                parts.Add(partOSimulationOutcome.Refusal);
            }

            if (partOSimulationOutcome.Notes.Count != 0 || partOSimulationOutcome.Note_PartORun is not null || partOSimulationOutcome.Cancelled)
            {
                //The dialog path's own message carries the capped notes and the not-completed reason, worded
                //exactly as before.
                if (!string.IsNullOrWhiteSpace(partOSimulationOutcome.Message))
                {
                    parts.Add(partOSimulationOutcome.Message.Trim());
                }
            }

            return string.Join("\n\n", parts);
        }

        /// <summary>
        /// Settles the reuse decision for one invocation of Prepare &amp; Run - and, where the preparation is
        /// reused, records on the run the Iteration 2B choice this invocation actually asked for.
        ///
        /// <para><b>Why the record has to be written here</b></para>
        /// <para>
        /// <see cref="PartOWorkflowInspection.ReusePreparation"/> matches the ENGINEERING case: the
        /// iteration, the dwelling scope, the stated routes, the catalogue, the isolation - everything that
        /// reached <c>SAM.Analytical.Modify.PreparePartOIteration</c> and therefore decides whether the
        /// prepared model is the same model. The Iteration 2B step and limit are the one thing on the
        /// preparation context that preparation neither reads nor is affected by, so they are deliberately
        /// not matched - but <c>Modify.CanOptimise</c> reads them off the run afterwards. Skipping the
        /// preparation therefore skipped the only thing that used to record them, and a user who ticked,
        /// unticked or retuned 2B and pressed Run got the choice made at the earlier preparation.
        /// </para>
        ///
        /// <para><b>Why not simply match on them and re-prepare</b></para>
        /// <para>
        /// Because it would rebuild an analytical preparation - and on an isolated run re-derive its
        /// geometry and re-cut its adiabatic interfaces - to change two numbers nothing in that preparation
        /// looks at. Correctness does not require it: the run's own record is what is wrong, and
        /// <see cref="PartORun.AdoptOptimisationSettings"/> is the transition that fixes exactly that.
        /// </para>
        ///
        /// <para><b>Correctness first where they disagree</b></para>
        /// <para>
        /// Where the run will not take the record it was not a reuse target after all, and this returns
        /// false so the caller prepares again. A redundant preparation is always right; a run whose recorded
        /// optimisation is not the one that was asked for is not.
        /// </para>
        ///
        /// <para><b>Nothing is written unless the preparation is reused</b>, and nothing is written during
        /// inspection: on the prepare-again path the new preparation records the choice itself, and a status
        /// refresh must not change the run it is describing.</para>
        /// </summary>
        /// <returns>Whether the existing preparation may be reused. False means prepare it again.</returns>
        internal static bool ReuseWithCurrentOptimisation(PartORun partORun, PartOWorkflowRequest partOWorkflowRequest, bool reuse)
        {
            if (!reuse || partORun is null)
            {
                return false;
            }

            //No settings on the request means none were STATED - the Hub no longer asks for 2B settings (Pass 5:
            //they are confirmed when 2B starts). A preset the Prepare Iteration window recorded is then kept, not
            //cleared: it is what pre-fills the Start Iteration 2B confirmation after this run (Codex P2 on #118).
            //The reuse condition is AdoptOptimisationSettings' own, unchanged: a prepared run with a context.
            PartOOptimisationSettings? partOOptimisationSettings = partOWorkflowRequest?.OptimisationSettings;
            if (partOOptimisationSettings is null)
            {
                return partORun.State == PartORunState.Prepared && partORun.PreparationContext is not null;
            }

            return partORun.AdoptOptimisationSettings(partOOptimisationSettings);
        }

        /// <summary>
        /// The session facts the status list needs and no model can answer, each taken from the authority
        /// that owns it.
        /// <para>
        /// <b>Asked once per showing of the dialog, deliberately.</b> <c>PartORun.IsAssessable</c> re-stats
        /// the results file and can drop a run whose results have gone - the real gate, and not something a
        /// status list may do on every keystroke. <c>Modify.CanOptimise</c> reads it in turn.
        /// </para>
        /// <para>
        /// The Iteration 2B answer is <c>CanOptimise</c>'s alone, with no settings: the step and the limit are
        /// confirmed when 2B starts (<see cref="RunPartOOptimisationResult"/>), so a completed Iteration 2 run
        /// is not refused for having been prepared without them.
        /// </para>
        /// </summary>
        /// <summary>
        /// <see cref="Capabilities(PartORun, out PartOIteration3Eligibility)"/> for the Hub over the open model: also
        /// asks, once per showing, whether that model is a Part O result that no new case may start from
        /// (<c>Query.PartODesignModelRefusal</c>). A result is reviewed, never run from.
        /// </summary>
        internal static PartOWorkflowCapabilities Capabilities(PartORun? partORun, AnalyticalModel? analyticalModel, out PartOIteration3Eligibility? partOIteration3Eligibility)
        {
            PartOWorkflowCapabilities result = Capabilities(partORun, out partOIteration3Eligibility);

            result.DesignModelRefusal = UI.Query.PartODesignModelRefusal(analyticalModel);

            return result;
        }

        internal static PartOWorkflowCapabilities Capabilities(PartORun? partORun, out PartOIteration3Eligibility? partOIteration3Eligibility)
        {
            PartOWorkflowCapabilities result = new();

            partOIteration3Eligibility = null;

            if (partORun is null)
            {
                return result;
            }

            result.ResultsAvailable = partORun.IsAssessable(out string refusal_Results);
            result.ResultsRefusal = refusal_Results;
            result.ResultsRestored = partORun.IsRestored;
            result.Path_Results = partORun.Path_TSD;

            //CanOptimise alone: the settings are confirmed when 2B starts, so a run does not have to have been
            //prepared with them (see RunPartOOptimisationResult).
            result.OptimisationAvailable = partORun.CanOptimise(null, out string? refusal_Optimisation);

            result.OptimisationRefusal = refusal_Optimisation;

            //Asked here for the same reason the two above are: the eligibility authority looks for the
            //pairing record on disk, and a status list rebuilt on every keystroke must not touch the
            //filesystem. It reuses the IsAssessable answer already taken rather than asking it twice -
            //that call can DROP a run whose results have gone, and dropping it twice in one gesture would
            //report the second drop against a run that no longer exists.
            partOIteration3Eligibility = Query.PartOIteration3Eligibility(partORun, result.ResultsAvailable, result.ResultsRefusal);

            result.Iteration3Available = partOIteration3Eligibility.Available;
            result.Iteration3Review = partOIteration3Eligibility.Review;
            result.Iteration3Refusal = partOIteration3Eligibility.Refusal;

            return result;
        }
    }
}
