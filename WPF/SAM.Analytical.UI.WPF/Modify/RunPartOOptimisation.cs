// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// The ribbon command behind the automatic Approved Document O Iteration 2B optimisation: confirm it,
        /// run it, and show its outcome and history. The open design model is never replaced by it.
        /// <para>
        /// <b>Orchestration only.</b> Every engineering decision belongs to
        /// <see cref="OptimisePartOTM59(PartORun, PartOOptimisationSettings, out string)"/> and, beneath it,
        /// to <c>SAM.Analytical</c>. This method chooses no target, no airflow and no stopping point.
        /// </para>
        /// <para>
        /// <b>Eligibility is <see cref="CanOptimise"/>'s alone; the settings are confirmed at the start.</b>
        /// The step, the round limit, the capacity envelope and the warm start are not preparation inputs
        /// (<c>PartORun.AdoptOptimisationSettings</c> says why), so a completed Iteration 2 run does not have to
        /// have been prepared with them. They are stated in the "Start Iteration 2B" confirmation
        /// (<see cref="PartOOptimisationStartWindow"/>), pre-filled from the run's record, this session or the
        /// defaults, passed straight to the optimiser, and recorded on the optimisation's own
        /// <see cref="PartOOptimisationRun.Settings"/> - so an optimisation still cannot run at a step nobody
        /// agreed to.
        /// </para>
        /// <para>
        /// <b>What is kept is the last valid design</b> - the last iteration that was prepared, simulated
        /// over the full year and assessed. On a capacity stop that is the design one full step below the
        /// selected unit's ceiling; it is never a refused round, and never a round that was not simulated. It
        /// stays with the run and under <c>PartO/Iteration2B</c>, and is never written into the design model
        /// (PR-4, owner decision 3).
        /// </para>
        /// </summary>
        /// <param name="uIAnalyticalModel">The open design model. Never replaced or changed by an optimisation.</param>
        /// <param name="partORun">The session's completed Iteration 2 run.</param>
        /// <param name="owner">Owner window for the dialogs.</param>
        public static void RunPartOOptimisation(this UIAnalyticalModel? uIAnalyticalModel, PartORun? partORun, IWin32Window? owner = null)
        {
            RunPartOOptimisationResult(uIAnalyticalModel, partORun, owner, null, out PartOOptimisationSettings? _);
        }

        /// <summary>
        /// <see cref="RunPartOOptimisation"/> itself, returning the optimisation it ran so the Prepare &amp; Run Hub
        /// can word its outcome line from the run's own stop reason. The public command keeps its signature.
        /// </summary>
        /// <param name="partOOptimisationSettings_Session">
        /// The settings last confirmed in this session, used only to pre-fill the confirmation where the run
        /// records none.
        /// </param>
        /// <param name="partOOptimisationSettings_Confirmed">
        /// The settings Start was pressed with, or null where the confirmation was cancelled or the run was
        /// refused before it.
        /// </param>
        /// <param name="confirm">
        /// Test seam: answers the confirmation instead of showing it. Null shows
        /// <see cref="PartOOptimisationStartWindow"/>.
        /// </param>
        /// <param name="optimise">
        /// Test seam: runs the optimisation instead of <see cref="OptimisePartOTM59(PartORun, PartOOptimisationSettings, out string)"/>,
        /// which needs TAS. Null is the production optimiser.
        /// </param>
        /// <param name="showResult">
        /// Test seam: receives the outcome instead of <see cref="PartOOptimisationResultWindow"/> - the run, whether a
        /// Cancel was requested, and whether 2B can continue. Null shows the window.
        /// </param>
        /// <returns>
        /// The optimisation that ran, or null where it was refused before starting (that refusal has already
        /// been shown) or the confirmation was cancelled (<paramref name="partOOptimisationSettings_Confirmed"/>
        /// is then null too).
        /// </returns>
        internal static PartOOptimisationRun? RunPartOOptimisationResult(UIAnalyticalModel? uIAnalyticalModel, PartORun? partORun, IWin32Window? owner, PartOOptimisationSettings? partOOptimisationSettings_Session, out PartOOptimisationSettings? partOOptimisationSettings_Confirmed, Func<PartOOptimisationStart, PartOOptimisationSettings?>? confirm = null, Func<PartORun, PartOOptimisationSettings, PartOOptimisationRun?>? optimise = null, Action<PartOOptimisationRun, bool, bool>? showResult = null)
        {
            partOOptimisationSettings_Confirmed = null;

            //"Last used in this session" for every route - the Hub and the Results ribbon alike (Codex P2 on
            //#118): the ribbon has no session state of its own to carry it in.
            partOOptimisationSettings_Session ??= partOOptimisationSettings_LastConfirmed;

            if (uIAnalyticalModel is null || partORun is null)
            {
                return null;
            }

            //Refused BEFORE anything is asked or run, because the alternative is spending minutes of TAS time
            //to discover the run was never an Iteration 2B starting point. No settings yet: they are confirmed
            //below, and the optimiser validates them with the same PartOOptimisationSettings.IsValid.
            if (!partORun.CanOptimise(null, out string? refusal_CanOptimise))
            {
                MessageBox.Show(string.Format("The Part O Iteration 2B optimisation did not run.\n\n{0}", refusal_CanOptimise), "Part O — Iteration 2B");

                return null;
            }

            PartOOptimisationStart partOOptimisationStart = PartOOptimisationStart.Create(partORun, partOOptimisationSettings_Session);

            PartOOptimisationSettings? partOOptimisationSettings = confirm is null
                ? ConfirmPartOOptimisationStart(partOOptimisationStart, owner)
                : confirm(partOOptimisationStart);

            if (partOOptimisationSettings is null)
            {
                //Cancelled at the confirmation: nothing ran and nothing changed.
                return null;
            }

            partOOptimisationSettings_Confirmed = partOOptimisationSettings;
            partOOptimisationSettings_LastConfirmed = partOOptimisationSettings;

            PartOOptimisationRun? partOOptimisationRun;
            string? refusal;
            bool cancelRequested;

            //ONE Part O progress window for the whole optimisation, as Prepare & Run and Iteration 3 have.
            //While it is the ambient host, each round's preparation and TAS workflow report into it and take
            //its Cancel (see RunPartOSimulation and RunWorkflow) instead of opening a "Preparing Model" and a
            //"Tas Workflow" dialog of their own per round. The rounds, their stop rules and what a Cancel
            //does to a round are unchanged: Cancel is observed where it always was, between the steps of a
            //round's simulation.
            //
            //Because that is the ONLY place it is observed, Cancel is offered only there
            //(cancelOnlyWhileObserved): during a round's simulation preparation and its TAS workflow, which
            //open PartOProgressHost.AllowCancel scopes. During the assessments and rebalancing between
            //simulations it is withdrawn, since a click there could be accepted and never acted on - the
            //final round's assessment is followed by no further simulation where the run then stops at its
            //limit with no envelope.
            using (PartOProgressHost partOProgressHost = new(
                "Iteration 2B — TM59 optimisation",
                PartOOptimisationProgressSubheading(partOOptimisationSettings),
                PartOOptimisationPhases(partOOptimisationSettings),
                cancelOnlyWhileObserved: true))
            {
                if (optimise is null)
                {
                    partOOptimisationRun = partORun.OptimisePartOTM59(partOOptimisationSettings, out refusal);
                }
                else
                {
                    partOOptimisationRun = optimise(partORun, partOOptimisationSettings);
                    refusal = partOOptimisationRun is null ? "The optimisation did not run." : null;
                }

                //From the run's own stop reason, as Iteration 3 ends its list from its result: a cancelled or
                //failed round must not read as completed while the window closes.
                PartOOptimisationProgressEnd(partOProgressHost.State, partOOptimisationRun);

                //Read for the result window only, to explain a stop that followed a Cancel request without
                //being the cancellation - a round that reached a refusal before a point it could stop at. The
                //stop reason is the optimiser's, and its precedence is not changed here.
                cancelRequested = partOProgressHost.IsCancellationRequested;
            }

            if (partOOptimisationRun is null)
            {
                MessageBox.Show(string.Format("The Part O Iteration 2B optimisation did not run.\n\n{0}", refusal), "Part O — Iteration 2B");

                return null;
            }

            AnalyticalModel? analyticalModel_LastValid = partOOptimisationRun.AnalyticalModel_LastValid;

            //Whether the run survives adopting the kept design - the same condition the arming below uses. The
            //optimiser drops the run on a cancelled or failed round, and then 2B cannot be started again from
            //the kept design; the result window's next step must say so rather than offer an unavailable action
            //(Codex P2 on #118).
            bool canContinue = analyticalModel_LastValid is not null
                && partORun.State == PartORunState.WorkflowCompleted
                && ReferenceEquals(partORun.AnalyticalModel_Assessment, analyticalModel_LastValid);

            if (showResult is not null)
            {
                showResult(partOOptimisationRun, cancelRequested, canContinue);
            }
            else
            {
                PartOOptimisationResultWindow partOOptimisationResultWindow = new();
                partOOptimisationResultWindow.Show(partOOptimisationRun, cancelRequested, canContinue);

                if (owner is not null)
                {
                    new System.Windows.Interop.WindowInteropHelper(partOOptimisationResultWindow).Owner = owner.Handle;
                }

                partOOptimisationResultWindow.ShowDialog();
            }

            //The last valid design is NOT adopted into the window (PR-4; owner decision 3). It is the run's
            //completed model where the run survived, and its files are under PartO/Iteration2B; the open design
            //model is never replaced by an optimisation output. Promoting it to design intent would be an explicit
            //"Adopt accepted 2B design" action, which does not exist.
            return partOOptimisationRun;
        }

        /// <summary>
        /// The Iteration 2B settings last confirmed in this application session, whichever route confirmed them -
        /// only ever a pre-fill for the next confirmation, never a setting a run uses unconfirmed.
        /// </summary>
        internal static PartOOptimisationSettings? partOOptimisationSettings_LastConfirmed;

        /// <summary>
        /// Shows the "Start Iteration 2B" confirmation and returns the settings Start was pressed with, or null
        /// where it was cancelled.
        /// </summary>
        private static PartOOptimisationSettings? ConfirmPartOOptimisationStart(PartOOptimisationStart partOOptimisationStart, IWin32Window? owner)
        {
            PartOOptimisationStartWindow partOOptimisationStartWindow = new()
            {
                Start = partOOptimisationStart,
            };

            if (owner is not null)
            {
                new System.Windows.Interop.WindowInteropHelper(partOOptimisationStartWindow).Owner = owner.Handle;
            }

            return partOOptimisationStartWindow.ShowDialog() == true ? partOOptimisationStartWindow.ConfirmedSettings : null;
        }
    }
}
