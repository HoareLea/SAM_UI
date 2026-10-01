// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The Approved Document O run in progress in this session: what was prepared, what the TAS workflow
    /// returned for it, and whether the two still belong together.
    /// <para>
    /// <b>Not a bag holding both models.</b> The states own different things and the transitions are the only
    /// way between them. <see cref="PartORunState.Prepared"/> owns the preparation and its scenarios;
    /// <see cref="PartORunState.WorkflowCompleted"/> owns, in addition, the model the workflow <i>returned</i>
    /// and the TSD it wrote. <see cref="AnalyticalModel_Assessment"/> - the only model an assessment may use -
    /// exists solely in the completed state, and there is no code path on which it can be the preparation
    /// output.
    /// </para>
    /// <para>
    /// <b>Why the preparation model must never be assessed.</b> A TM59 query resolves a simulated space back
    /// to a design space through <c>SpaceParameter.ZoneGuid</c>, the identity TAS preserves across the round
    /// trip, and only the model <c>WorkflowCalculator.Calculate</c> returns carries the <i>current</i> TAS
    /// zone identities - a preparation output can still hold stale guids from an earlier round trip on the
    /// same source file. Measured both ways on the licensed acceptance run: preparation output gives an
    /// incomplete <c>SimulationSpaceMap</c> and every space refused; workflow output resolves all nine.
    /// </para>
    /// <para>
    /// <b>Staleness is rejected, not detected after the fact.</b> Anything that <i>changes</i> the loaded
    /// model between preparing and completing - an edit, an import, an undo, a redo, a second unrelated
    /// simulation - arrives here as an unexpected <see cref="NotifyModified(bool)"/> and drops the run to
    /// <see cref="PartORunState.None"/> with a reason. A replacement that changed only how the model is
    /// <i>drawn</i> is not one of those and is not an event here at all - see
    /// <see cref="NotifyModified(bool)"/>, and <c>Query.IsModelChange</c> for where the two are told apart. The Part O commands announce their own writes with
    /// <see cref="ExpectModification"/> first, so the only way to reach
    /// <see cref="PartORunState.WorkflowCompleted"/> is a workflow over the model that was prepared and not
    /// touched since. That is what makes pairing one preparation's scenarios with another run's results
    /// unreachable rather than merely unlikely.
    /// </para>
    /// <para>
    /// <b><see cref="PartORunState.WorkflowCompleted"/> means "this prepared run produced the full-year
    /// results being assessed"</b> - not "a TSD exists". Three things are required, and the second and third
    /// are what <see cref="ExpectResults"/> exists for: the workflow must have returned a model; the
    /// simulation must have been the full annual run a TM59 assessment reads, which is enforced by arming
    /// only for that case; and the results file must have been created or rewritten by <i>this</i> workflow,
    /// measured against a fingerprint taken before it ran. An earlier session's <c>&lt;project&gt;.tsd</c>
    /// left in the output directory satisfies none of them.
    /// </para>
    /// <para>
    /// <b>Session state, with one recorded exception.</b> The run itself - how it was prepared and which TAS
    /// case produced it - lives only for the session. What DOES travel with the model are the two statements
    /// stamped onto it when it is produced (see <c>Modify.RunPartOSimulation</c>): the
    /// <c>SimulationResultProvenance</c> naming the results file and its fingerprint, and the overheating
    /// scenarios the run was prepared with. On reopen, <see cref="Restore"/> reconnects those into a run that
    /// can be <b>reviewed</b> - assessed again from the same results - but holds no preparation or simulation
    /// context, so it can never be <b>resumed</b> into Iteration 2B. Preparing again remains the only way to
    /// optimise.
    /// </para>
    /// </summary>
    public class PartORun
    {
        private AnalyticalModel analyticalModel_Prepared;

        private List<OverheatingScenario> overheatingScenarios = [];

        private AnalyticalModel analyticalModel_Workflow;

        //How this run was prepared and how it was simulated - kept so an Iteration 2B optimisation can
        //repeat BOTH without asking again, which is what makes its rounds comparable. See
        //PartOPreparationContext and PartOSimulationContext.
        private PartOPreparationContext partOPreparationContext;

        private PartOSimulationContext partOSimulationContext;

        //The identities of the ventilation systems the preparation BUILT for this run - see
        //Guids_VentilationSystem_Prepared.
        private List<System.Guid> guids_VentilationSystem_Prepared = [];

        private string path_TSD;

        private System.DateTime dateTime_TSD;

        private bool modificationExpected;

        //The pre-run fingerprint of the results file this workflow is expected to write. See ExpectResults.
        private bool resultsExpected;

        private string path_TSD_Expected;

        private bool exists_TSD_Expected;

        private long length_TSD_Expected = -1;

        private System.DateTime dateTime_TSD_Expected;

        //Per Iteration 3 method: an attempt whose TAS work completed but whose pairing did not. See
        //Iteration3Checkpoint.
        private readonly Dictionary<PartOIteration3BehaviourMode, object> iteration3Checkpoints = [];

        /// <summary>How far this run has got.</summary>
        public PartORunState State { get; private set; } = PartORunState.None;

        /// <summary>
        /// Raised whenever a transition has moved what this run allows - prepared, adopted an optimisation
        /// setting, completed, restored, dropped or cleared.
        ///
        /// <para><b>Why the run announces it rather than each caller remembering to ask</b></para>
        /// <para>
        /// Whether the Approved Document O result commands are available is <see cref="CanAssess"/>, which is
        /// a fact about this run and about nothing else - so every command that moved the run had to refresh
        /// the ribbon itself afterwards, and the ordering made that fragile. <c>Modify.Simulate</c> completes
        /// the run <i>after</i> the model replacement it belongs to, so the refresh that replacement triggers
        /// runs while the run is still <see cref="PartORunState.Prepared"/>: the commands stay disabled
        /// unless the caller refreshes a second time, and a caller that did not left a completed run with
        /// unavailable results and nothing on screen saying why.
        /// </para>
        /// <para>
        /// Raised from the transitions themselves, so "the run became assessable" and "the commands say so"
        /// cannot come apart. Handlers must only <b>read</b> this run - <see cref="State"/>,
        /// <see cref="CanAssess"/>, <see cref="IsRestored"/>, <see cref="InvalidationReason"/> and
        /// <see cref="PreparationContext"/> are all pure reads, so no handler can move the state it is
        /// describing. <see cref="IsAssessable"/> is deliberately not one of them: it touches the filesystem
        /// and can drop a run, which is the gate's job and not a status refresh's.
        /// </para>
        /// </summary>
        public event System.EventHandler StateChanged;

        /// <summary>Announces a completed transition. Never called from a place that is mid-transition.</summary>
        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, System.EventArgs.Empty);
        }

        /// <summary>
        /// Why the run was dropped, or null where it never was. Retained through
        /// <see cref="PartORunState.None"/> so the UI can say why the assessment is unavailable instead of
        /// only that it is.
        /// </summary>
        public string InvalidationReason { get; private set; }

        /// <summary>
        /// The prepared model this run is built on, or null in <see cref="PartORunState.None"/>.
        /// <para>
        /// <b>Never assess this.</b> It is exposed so the run can be inspected and so the distinction between
        /// the two models is observable to a test, not because it is an alternative to
        /// <see cref="AnalyticalModel_Assessment"/>. It is the model that was handed TO the workflow; its zone
        /// identities may predate the round trip.
        /// </para>
        /// </summary>
        public AnalyticalModel AnalyticalModel_Prepared => analyticalModel_Prepared;

        /// <summary>
        /// The scenarios the assessment attributes results to. Empty outside a live run.
        /// <para>
        /// These belong to the preparation this run was built on and to no other. Pairing them with a
        /// different run's results is the thing this class prevents.
        /// </para>
        /// </summary>
        public List<OverheatingScenario> OverheatingScenarios => [.. overheatingScenarios];

        /// <summary>
        /// <b>The model a TM59 assessment must be given</b> - the one the completed TAS workflow returned.
        /// Null in every state but <see cref="PartORunState.WorkflowCompleted"/>, so there is nothing to fall
        /// back to and no way to reach the preparation output through this property.
        /// </summary>
        public AnalyticalModel AnalyticalModel_Assessment => State == PartORunState.WorkflowCompleted ? analyticalModel_Workflow : null;

        /// <summary>
        /// What the assessed result was derived from (PR-5): the reference its model carries, for a live run and for a result opened
        /// with File > Open alike, or null where there is no completed run or the result predates the reference. It is information
        /// only - nothing here makes that design, or this result, the open model.
        /// </summary>
        public PartOBaselineReference BaselineReference
        {
            get
            {
                AnalyticalModel analyticalModel = AnalyticalModel_Assessment;

                return analyticalModel is not null && analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.PartOBaselineReference, out PartOBaselineReference partOBaselineReference) && partOBaselineReference is not null && partOBaselineReference.IsValid ? partOBaselineReference : null;
            }
        }

        /// <summary>The TSD the completed workflow wrote. Null outside <see cref="PartORunState.WorkflowCompleted"/>.</summary>
        public string Path_TSD => State == PartORunState.WorkflowCompleted ? path_TSD : null;

        /// <summary>
        /// What this run was prepared with - the base provision, the dwelling scope, the stated routes and
        /// the product catalogue that was offered. Null outside a live run, and null on a run prepared
        /// through the overload that states none.
        /// <para>
        /// <b>Carried so an optimisation can re-prepare identically.</b> Iteration 2B changes design airflow
        /// and rebuilds the Part O state around it; re-preparing with a different route or a different
        /// catalogue would make each round a different engineering case and the TM59 results across the run
        /// incomparable.
        /// </para>
        /// </summary>
        public PartOPreparationContext PreparationContext => State == PartORunState.None ? null : partOPreparationContext;

        /// <summary>
        /// The TAS case the completed workflow ran as - the weather, the solar method and the day range.
        /// Null outside <see cref="PartORunState.WorkflowCompleted"/>, and on a run completed through the
        /// overload that states none.
        /// <para>
        /// <b>Carried so an optimisation reruns the SAME case.</b> A round whose weather or day range moved
        /// would leave the change in TM59 results unattributable to the airflow change that was made.
        /// </para>
        /// </summary>
        public PartOSimulationContext SimulationContext => State == PartORunState.WorkflowCompleted ? partOSimulationContext : null;

        /// <summary>
        /// The identities of the <c>VentilationSystem</c> objects the Part O preparation <b>built</b> for
        /// this run - <c>PartOIterationPreparation.VentilationSystems</c>, captured at the moment the run
        /// adopted the preparation. Empty on a run prepared through an overload that states none, and on a
        /// restored run.
        ///
        /// <para><b>Why the identities are captured rather than looked up later</b></para>
        /// <para>
        /// A real Approved Document O model carries authored ventilation systems this iteration did not
        /// build and deliberately did not change - natural ventilation, uncontrolled ventilation, a legacy
        /// mechanical system - and <c>Modify.PreparePartOIteration</c> preserves all of them, saying so in
        /// its own notes. The only moment at which "which of these is the design under assessment" is known
        /// for certain is the moment the preparation hands its systems back. Afterwards there is no rule
        /// that recovers it: type is not it (a legacy MV system is also mechanical), terminals are not it
        /// (a competing design may carry them), and the display name is <b>never</b> it. See SAM #114.
        /// </para>
        /// <para>
        /// <b>Read-only, and cleared with everything else.</b> A dropped, reset or restored run carries
        /// none: a captured identity that outlived the preparation it came from would scope the next run's
        /// materialisation to the previous run's design.
        /// </para>
        /// <para>
        /// A <b>restored</b> run deliberately has none. The file records what was run, not how this session
        /// prepared it, so a reopened run may review a completed Iteration 3 record and may not start a new
        /// Candidate B - the same rule that keeps it out of Iteration 2B.
        /// </para>
        /// </summary>
        public List<System.Guid> Guids_VentilationSystem_Prepared => State == PartORunState.None ? [] : [.. guids_VentilationSystem_Prepared];

        /// <summary>
        /// Whether this run has results at all - what the ribbon enables on.
        /// <para>
        /// A pure state read, evaluated on every ribbon refresh, so it deliberately does not touch the
        /// filesystem. <see cref="IsAssessable"/> is the real gate and the command reads that one; a completed
        /// run whose results have since gone is dropped there, which turns this false and puts the reason in
        /// <see cref="InvalidationReason"/> for the tooltip.
        /// </para>
        /// </summary>
        public bool CanAssess => State == PartORunState.WorkflowCompleted;

        /// <summary>
        /// Whether this run was reconnected to a model reopened from disk (see <see cref="Restore"/>) rather
        /// than produced in this session. A restored run can be <b>reviewed</b> - its results are re-read and
        /// reassessed - but never <b>resumed</b> into Iteration 2B: it carries no preparation or simulation
        /// context, by construction, so <c>CanOptimise</c> refuses it.
        /// </summary>
        public bool IsRestored { get; private set; }

        /// <summary>
        /// Whether this restored run also carries, beside its results, how it was prepared and the case it ran as
        /// (a <see cref="PartORunResume"/> bound to these results and a matching prepared model) - so Iteration 3
        /// can be started from it without re-running Prepare &amp; Run. Iteration 2B stays unavailable.
        /// </summary>
        public bool CanResumeIteration3 { get; private set; }

        /// <summary>Why a restored run could not be resumed for Iteration 3, or null.</summary>
        public string ResumeRefusal { get; private set; }

        /// <summary>
        /// The TAS work of this run's last Iteration 3 attempt for one method, where that attempt completed its TAS
        /// work (thermal source, TAS Systems, resultant temperature) but not its pairing - so a retry in this session
        /// can reuse it instead of running TAS again. Null where there is none.
        /// <para>
        /// <b>Held by the run because its lifetime is the run's.</b> Every transition that drops, clears, restores
        /// or re-prepares this run clears it with everything else, so it can never outlive the preparation, the
        /// case and the results it was produced from - the same rule that keeps a stale run from being assessed.
        /// Held here, it is also never written anywhere: it is session state, like the run.
        /// </para>
        /// <para>
        /// <b>Opaque here.</b> Its contents are TAS Systems and SAM_Systems results this assembly does not
        /// reference; the Iteration 3 orchestrator owns the type, and proves before any reuse that every file and
        /// every identity it was produced from is unchanged. This class only bounds its lifetime.
        /// </para>
        /// </summary>
        public object Iteration3Checkpoint(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            return State == PartORunState.WorkflowCompleted && iteration3Checkpoints.TryGetValue(partOIteration3BehaviourMode, out object result) ? result : null;
        }

        /// <summary>
        /// Keeps an Iteration 3 attempt's completed TAS work for a retry in this session - see
        /// <see cref="Iteration3Checkpoint"/>. Only on a completed run: there is nothing an attempt could have been
        /// built on otherwise.
        /// </summary>
        /// <returns>Whether it was kept.</returns>
        public bool KeepIteration3Checkpoint(PartOIteration3BehaviourMode partOIteration3BehaviourMode, object checkpoint)
        {
            if (State != PartORunState.WorkflowCompleted || checkpoint is null)
            {
                return false;
            }

            iteration3Checkpoints[partOIteration3BehaviourMode] = checkpoint;

            return true;
        }

        /// <summary>Forgets one method's kept TAS work - its pairing completed, or its files are about to be rewritten.</summary>
        public void DropIteration3Checkpoint(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            iteration3Checkpoints.Remove(partOIteration3BehaviourMode);
        }

        /// <summary>
        /// Announces that the next model replacement is this run's own, so it is not read as an outside edit.
        /// One shot: it is consumed by the next <see cref="NotifyModified"/> and must be re-armed for the next
        /// write.
        /// <para>
        /// Called immediately before a Part O command's own <c>SetJSAMObject</c>. Anything arriving unarmed is
        /// somebody else's change, which is exactly the signal wanted.
        /// </para>
        /// </summary>
        public void ExpectModification()
        {
            modificationExpected = true;
        }

        /// <summary>
        /// Announces, <b>before the workflow runs</b>, which results file this run expects it to write, and
        /// fingerprints whatever is at that path now.
        /// <para>
        /// <b>Why a pre-run fingerprint and not a post-run timestamp.</b> The results path is derived from the
        /// TBD's, so an earlier session's <c>&lt;project&gt;.tsd</c> can already be sitting in the output
        /// directory. <c>Modify.Simulate</c> deletes only the TBD before running, so a sizing-only or otherwise
        /// non-simulating workflow leaves that old file untouched - and <see cref="Complete"/> reading its write
        /// time <i>after</i> the run would record a stale file as this run's result and then let
        /// <see cref="IsAssessable"/> approve it against the newly prepared model and scenarios. Captured here,
        /// the same file being unchanged afterwards is exactly the signal that this workflow wrote nothing.
        /// </para>
        /// <para>
        /// <b>Arming is also where the full-year requirement is enforced.</b> The caller arms this only for the
        /// simulation a TM59 assessment can actually read - a full annual hourly series - so a partial,
        /// one-day or sizing-only workflow leaves the run unarmed and <see cref="Complete"/> refuses it even if
        /// it is reached. <see cref="PartORunState.WorkflowCompleted"/> therefore means "this prepared run
        /// produced the full-year results being assessed", not "a TSD exists".
        /// </para>
        /// <para>
        /// One shot per run, and only from <see cref="PartORunState.Prepared"/>: re-arming replaces the
        /// fingerprint, and <see cref="Invalidate"/> clears it, so a dropped run cannot be completed by a
        /// workflow that was announced to its predecessor.
        /// </para>
        /// </summary>
        /// <param name="path_TSD">The results file the workflow about to run is expected to write.</param>
        /// <returns>Whether a fingerprint was armed.</returns>
        public bool ExpectResults(string path_TSD)
        {
            resultsExpected = false;
            path_TSD_Expected = null;
            exists_TSD_Expected = false;
            length_TSD_Expected = -1;
            dateTime_TSD_Expected = default;

            if (State != PartORunState.Prepared || string.IsNullOrWhiteSpace(path_TSD))
            {
                return false;
            }

            path_TSD_Expected = path_TSD;
            resultsExpected = true;

            //Length as well as write time: two different observations of the same file, and a rewrite that
            //landed inside the filesystem's timestamp granularity still changes one of them. Where they both
            //match, the file is treated as untouched - refusing a genuine rerun is the safe way to be wrong.
            FileInfo fileInfo = new(path_TSD);
            if (fileInfo.Exists)
            {
                exists_TSD_Expected = true;
                length_TSD_Expected = fileInfo.Length;
                dateTime_TSD_Expected = fileInfo.LastWriteTimeUtc;
            }

            return true;
        }

        /// <summary>
        /// The loaded model was replaced <b>by something that changed it</b>. Consumes an armed expectation,
        /// or drops the run.
        /// </summary>
        public void NotifyModified()
        {
            NotifyModified(true);
        }

        /// <summary>
        /// The loaded model was replaced, saying whether the replacement changed the model or only how it is
        /// drawn - <c>Query.IsModelChange</c>, which is where that is decided and why.
        ///
        /// <para><b>Why a view change must not drop a run</b></para>
        /// <para>
        /// SAM stores view settings on the model, so hiding a space, isolating one, activating a saved view,
        /// editing appearances or the legend, moving a section plane or switching the active view all replace
        /// the loaded model object - and every one of them used to arrive here as an outside edit and drop the
        /// run. Nothing about a space, a panel, an aperture, an airflow, a zone or an overheating scenario
        /// moves when one of those happens, so the preparation still describes the model exactly.
        /// </para>
        /// <para>
        /// It made the expert workflow unusable rather than merely awkward. Preparing an iteration and
        /// simulating it are two separate commands, so a person is between them precisely in order to look at
        /// what was prepared; looking at it dropped the run, silently, and the full-year TAS simulation that
        /// followed then had nothing left to complete. The guided <c>Prepare &amp; Run</c> command never met
        /// it - it prepares, simulates and assesses in one gesture, with no point at which a view can be
        /// touched - which is why the two paths disagreed.
        /// </para>
        /// <para>
        /// <b>Nothing else is weakened.</b> A presentation-only replacement is not merely tolerated, it is not
        /// an event at all: it neither drops the run nor consumes an armed
        /// <see cref="ExpectModification"/>, so a Part O command's own write is still recognised however many
        /// view changes happen before it. Every other replacement - an edit, an import, an undo, a redo, a
        /// second unrelated simulation, or anything a future modification type describes - still drops the
        /// run.
        /// </para>
        /// </summary>
        /// <param name="modelChanged">
        /// Whether the replacement may have changed the analytical model. False only where it is <i>proved</i>
        /// to be presentation-only.
        /// </param>
        public void NotifyModified(bool modelChanged)
        {
            if (!modelChanged)
            {
                return;
            }

            if (modificationExpected)
            {
                modificationExpected = false;

                return;
            }

            if (State == PartORunState.None)
            {
                return;
            }

            //Named per state: a prepared run and a completed one are lost for different reasons and the user
            //has different work to redo.
            Invalidate(State == PartORunState.Prepared
                ? "The model changed after the Part O iteration was prepared, so the preparation and its overheating scenarios no longer describe it. Prepare the iteration again before simulating."
                : "The model changed after the Part O results were imported, so the assessment no longer has a model and results that belong together. Prepare the iteration again and re-run the simulation.");
        }

        /// <summary>
        /// Records a successful preparation. Replaces whatever was pending - a new preparation supersedes an
        /// older run rather than sitting beside it.
        /// </summary>
        /// <param name="partOIterationPreparation">
        /// The preparation. A null one, one that refused, or one carrying no model or no scenario leaves the
        /// run in <see cref="PartORunState.None"/>: there is nothing to simulate and nothing to attribute.
        /// </param>
        /// <returns>Whether the run is now <see cref="PartORunState.Prepared"/>.</returns>
        public bool Prepare(PartOIterationPreparation partOIterationPreparation)
        {
            return Prepare(partOIterationPreparation, null);
        }

        /// <summary>
        /// Records the Iteration 2B optimisation this prepared run should allow afterwards, <b>without
        /// re-preparing it</b>.
        /// <para>
        /// <b>Why this is not a re-preparation.</b>
        /// <see cref="PartOPreparationContext.OptimisationSettings"/> is not a preparation input:
        /// <c>SAM.Analytical.Modify.PreparePartOIteration</c> neither reads it nor is affected by it, and it
        /// rides on the context only because it is a choice made at the same moment about the same run.
        /// Everything else on the context - the iteration, the dwelling scope, the stated routes, the
        /// catalogue - DID reach the analytical preparation, and none of those can be changed here.
        /// </para>
        /// <para>
        /// <b>What it exists for.</b> Where an orchestration reuses an already-prepared run rather than
        /// preparing it again, the preparation that would have recorded the user's current 2B choice is
        /// skipped - so without this the run would carry the choice made at the earlier preparation and
        /// <c>Modify.CanOptimise</c> would read it. This is the transition that keeps the run's record equal
        /// to what was actually asked for.
        /// </para>
        /// <para>
        /// <b>Null clears it</b>, which is how "the user unticked Iteration 2B" is recorded. Stale settings
        /// are never left active.
        /// </para>
        /// <para>
        /// <b>Only a <see cref="PartORunState.Prepared"/> run with a preparation context takes it.</b> A
        /// completed run's settings belong to the baseline its results were produced under and are refused
        /// here; so is a restored run, which carries no preparation context at all. Neither is a reuse
        /// target, so refusing costs nothing and stops this becoming a general back door into the record.
        /// </para>
        /// <para>
        /// <b>The settings themselves are not validated here.</b> <c>PartOOptimisationSettings.IsValid</c> is
        /// that authority and <c>Modify.CanOptimise</c> applies it before any optimisation runs; a second
        /// opinion in this method would be a second rule to keep in step.
        /// </para>
        /// </summary>
        /// <param name="partOOptimisationSettings">The optimisation to allow, or null for none.</param>
        /// <returns>True where the run recorded it. False leaves the run exactly as it was.</returns>
        public bool AdoptOptimisationSettings(PartOOptimisationSettings partOOptimisationSettings)
        {
            if (State != PartORunState.Prepared || partOPreparationContext is null)
            {
                return false;
            }

            partOPreparationContext.OptimisationSettings = partOOptimisationSettings;

            //Announced although State has not moved: Modify.CanOptimise reads this off the run, so what the
            //Iteration 2B command allows has moved even though how far the run has got has not.
            OnStateChanged();

            return true;
        }

        /// <summary>
        /// The same transition, recording <b>how</b> the preparation was asked for so it can be repeated
        /// identically by an optimisation - see <see cref="PreparationContext"/>.
        /// </summary>
        /// <param name="partOPreparationContext">
        /// The preparation's own inputs. Null keeps this method's behaviour exactly as it was before an
        /// optimisation existed: the run is live, and simply cannot be re-prepared automatically.
        /// </param>
        public bool Prepare(PartOIterationPreparation partOIterationPreparation, PartOPreparationContext partOPreparationContext)
        {
            return Prepare(partOIterationPreparation?.AnalyticalModel, partOIterationPreparation?.OverheatingScenarios, partOPreparationContext, Guids_VentilationSystem(partOIterationPreparation), partOIterationPreparation?.Refusal);
        }

        /// <summary>
        /// The identities of the ventilation systems a preparation <b>built</b> - what every adoption of a
        /// preparation hands to <see cref="Prepare(AnalyticalModel, IEnumerable{OverheatingScenario}, PartOPreparationContext, IEnumerable{System.Guid}, string)"/>.
        /// <para>
        /// THE capture point for the Iteration 3 system scope, and the only one there is: see
        /// <see cref="Guids_VentilationSystem_Prepared"/> for why it cannot be recovered afterwards. Stated
        /// once, so a caller that adopts a model other than the preparation's own - the Prepare &amp; Run
        /// dialog rebuilds it after an equipment edit - still captures exactly the same identities.
        /// </para>
        /// </summary>
        public static List<System.Guid> Guids_VentilationSystem(PartOIterationPreparation partOIterationPreparation)
        {
            List<System.Guid> result = [];
            foreach (VentilationSystem ventilationSystem in partOIterationPreparation?.VentilationSystems ?? [])
            {
                if (ventilationSystem is not null && ventilationSystem.Guid != System.Guid.Empty)
                {
                    result.Add(ventilationSystem.Guid);
                }
            }

            return result;
        }

        /// <summary>
        /// The same transition from the two things a run actually needs from a preparation - its model and its
        /// scenarios. The overload above is how production reaches it; this one is also what a test can call,
        /// since <c>PartOIterationPreparation</c> is only assembled by <c>SAM.Analytical</c> itself.
        /// </summary>
        /// <param name="analyticalModel_Prepared">The prepared copy to simulate.</param>
        /// <param name="overheatingScenarios">The scenarios stated for it.</param>
        /// <param name="refusal">The preparation's fatal refusal, where it had one.</param>
        public bool Prepare(AnalyticalModel analyticalModel_Prepared, IEnumerable<OverheatingScenario> overheatingScenarios, string refusal = null)
        {
            return Prepare(analyticalModel_Prepared, overheatingScenarios, null, refusal);
        }

        /// <summary>
        /// The same transition from a model, its scenarios and the preparation's own inputs - what production
        /// reaches through <see cref="Prepare(PartOIterationPreparation, PartOPreparationContext)"/>, and what
        /// a test can call, since <c>PartOIterationPreparation</c> is only assembled by <c>SAM.Analytical</c>
        /// itself.
        /// </summary>
        /// <param name="partOPreparationContext">
        /// How the preparation was asked for - see <see cref="PreparationContext"/>. Null leaves the run
        /// live but not automatically repeatable.
        /// </param>
        public bool Prepare(AnalyticalModel analyticalModel_Prepared, IEnumerable<OverheatingScenario> overheatingScenarios, PartOPreparationContext partOPreparationContext, string refusal = null)
        {
            return Prepare(analyticalModel_Prepared, overheatingScenarios, partOPreparationContext, null, refusal);
        }

        /// <summary>
        /// The same transition, additionally capturing the identities of the ventilation systems the
        /// preparation <b>built</b> - see <see cref="Guids_VentilationSystem_Prepared"/>, which is where
        /// the whole reason this parameter exists is written down.
        /// </summary>
        /// <param name="guids_VentilationSystem">
        /// <c>PartOIterationPreparation.VentilationSystems</c>' identities. Null or empty is legitimate -
        /// the natural-ventilation route builds no system - and simply leaves the run with no Iteration 3
        /// system scope, which it then refuses to start one from.
        /// </param>
        public bool Prepare(AnalyticalModel analyticalModel_Prepared, IEnumerable<OverheatingScenario> overheatingScenarios, PartOPreparationContext partOPreparationContext, IEnumerable<System.Guid> guids_VentilationSystem, string refusal = null)
        {
            ResetCore();

            if (analyticalModel_Prepared is null)
            {
                Invalidate(refusal ?? "Nothing was prepared, so there is no Part O run.");

                return false;
            }

            List<OverheatingScenario> overheatingScenarios_Temp = [];
            foreach (OverheatingScenario overheatingScenario in overheatingScenarios ?? [])
            {
                if (overheatingScenario is not null)
                {
                    overheatingScenarios_Temp.Add(overheatingScenario);
                }
            }

            if (overheatingScenarios_Temp.Count == 0)
            {
                //Without a scenario there is no ventilation strategy for any space, so a TM59 assessment would
                //refuse every one of them. Refusing here says so while the user is still looking at the
                //preparation, rather than after a full simulation.
                Invalidate("The preparation stated no overheating scenario, so no space would have a ventilation strategy to be assessed against. Nothing is pending.");

                return false;
            }

            this.analyticalModel_Prepared = analyticalModel_Prepared;
            this.overheatingScenarios = overheatingScenarios_Temp;
            this.partOPreparationContext = partOPreparationContext;

            //Recorded only for a run that is actually going live: a refused preparation returns above, so
            //its systems - if it even reported any - never become a live run's Iteration 3 scope.
            List<System.Guid> guids_VentilationSystem_Temp = [];
            foreach (System.Guid guid in guids_VentilationSystem ?? [])
            {
                if (guid != System.Guid.Empty && !guids_VentilationSystem_Temp.Contains(guid))
                {
                    guids_VentilationSystem_Temp.Add(guid);
                }
            }

            guids_VentilationSystem_Prepared = guids_VentilationSystem_Temp;

            State = PartORunState.Prepared;

            OnStateChanged();

            return true;
        }

        /// <summary>
        /// Whether the results at <paramref name="path_TSD"/> are provably <b>this run's</b> - the results
        /// lineage rule, on its own, without changing anything.
        ///
        /// <para><b>Why this is a separate, askable question</b></para>
        /// <para>
        /// <see cref="Complete"/> is the authority on pairing a run with its results, and it applies this
        /// rule as part of that. But the rule is also needed <i>before</i> Complete is reached: the caller
        /// stamps <c>SimulationResultProvenance</c> onto the workflow's model and writes the run's persisted
        /// <c>.sam</c> immediately after the workflow returns, and a workflow that returned a model while
        /// leaving an existing TSD untouched would otherwise get a fully self-consistent, reopenable artifact
        /// written for it - model, scenarios and file fingerprints all agreeing - which a later session would
        /// restore and offer for review against an <i>earlier</i> run's results. Complete would then refuse
        /// the run, correctly, and the misleading file would already be on disk.
        /// </para>
        /// <para>
        /// So the rule lives here once and is asked twice, rather than being restated at the call site: a
        /// second copy of a staleness rule is exactly how the two answers drift apart.
        /// </para>
        /// <para>
        /// <b>Pure.</b> It reads state and the filesystem and neither writes nor invalidates - the caller
        /// decides what a "no" means. <see cref="Complete"/> invalidates on it; the persistence path simply
        /// declines to write.
        /// </para>
        /// </summary>
        /// <param name="path_TSD">The results file to test.</param>
        /// <param name="refusal">Why the results are not this run's, or null where they are.</param>
        public bool IsResultsOfThisRun(string path_TSD, out string refusal)
        {
            refusal = null;

            if (string.IsNullOrWhiteSpace(path_TSD) || !File.Exists(path_TSD))
            {
                refusal = string.Format("No simulation results were found at '{0}', so the workflow did not complete a run that can be assessed. A sizing-only run writes no TSD.", path_TSD ?? "?");

                return false;
            }

            //Nothing announced this workflow's results, so nothing establishes that they are this run's. That
            //is the state a partial, one-day or sizing-only simulation leaves the run in, because the caller
            //arms ExpectResults only for the full-year run a TM59 assessment can read.
            if (!resultsExpected || !string.Equals(path_TSD_Expected, path_TSD, System.StringComparison.Ordinal))
            {
                refusal = string.Format("The results at '{0}' were not announced as this Part O run's, so it cannot be established that this workflow produced them. Only a full-year simulation of the prepared model completes a Part O run - prepare the iteration again and simulate with Full Year Simulation ticked.", path_TSD);

                return false;
            }

            //The file that was already there, byte-length and write-time unchanged: this workflow did not
            //write it. Accepting it would pair an earlier session's results with the model just prepared.
            FileInfo fileInfo = new(path_TSD);
            if (exists_TSD_Expected && fileInfo.Length == length_TSD_Expected && fileInfo.LastWriteTimeUtc == dateTime_TSD_Expected)
            {
                refusal = string.Format("The simulation results at '{0}' are unchanged from before this workflow ran, so they are an earlier run's and not this one's. Simulate the prepared model with Full Year Simulation ticked to produce results this Part O run can be assessed against.", path_TSD);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Pairs the prepared run with the model a completed TAS workflow returned, and the TSD it wrote.
        /// </summary>
        /// <param name="analyticalModel_Workflow">
        /// The model <c>WorkflowCalculator.Calculate</c> returned - not the model that was handed to it, and
        /// not the loaded model read back afterwards.
        /// </param>
        /// <param name="path_TSD">
        /// The simulation results. Required to exist, to be the path <see cref="ExpectResults"/> was armed
        /// with, and to have been <b>created or rewritten since that arming</b> - a derived file name is a
        /// guess, an old file at that name is somebody else's run, and a workflow that wrote nothing did not
        /// complete this one. Its write time is then captured so <see cref="IsAssessable"/> can tell that the
        /// file being assessed is still the one this run wrote.
        /// </param>
        /// <param name="refusal">Why the run was not completed, or null where it was.</param>
        public bool Complete(AnalyticalModel analyticalModel_Workflow, string path_TSD, out string refusal)
        {
            return Complete(analyticalModel_Workflow, path_TSD, null, out refusal);
        }

        /// <summary>
        /// The same transition, recording <b>which TAS case</b> produced these results so an optimisation
        /// can rerun it over a changed design - see <see cref="SimulationContext"/>.
        /// </summary>
        /// <param name="partOSimulationContext">
        /// The case that just ran. Null keeps the behaviour this method had before an optimisation existed.
        /// </param>
        public bool Complete(AnalyticalModel analyticalModel_Workflow, string path_TSD, PartOSimulationContext partOSimulationContext, out string refusal)
        {
            refusal = null;

            //Only from Prepared. Completing from None would pair results with nothing; completing from
            //WorkflowCompleted would re-point a finished run at a second simulation's results while keeping
            //the first one's model - the precise stale pairing this type exists to prevent.
            if (State != PartORunState.Prepared)
            {
                refusal = State == PartORunState.None
                    ? "No Part O iteration is prepared, so a workflow result has nothing to complete. " + (InvalidationReason ?? "Prepare an iteration first.")
                    : "This Part O run already has results. Prepare the iteration again before simulating, so the model and the results being assessed are from the same run.";

                Invalidate(refusal);

                return false;
            }

            if (analyticalModel_Workflow is null)
            {
                refusal = "The TAS workflow returned no analytical model, so there is no model carrying the current TAS zone identities to assess against. The preparation output is not a substitute for it.";

                Invalidate(refusal);

                return false;
            }

            //THE results lineage rule, asked of the one place that owns it - see IsResultsOfThisRun, which
            //the persistence path asks the same question of before it writes anything reopenable.
            if (!IsResultsOfThisRun(path_TSD, out refusal))
            {
                Invalidate(refusal);

                return false;
            }

            this.analyticalModel_Workflow = analyticalModel_Workflow;
            this.path_TSD = path_TSD;
            dateTime_TSD = File.GetLastWriteTimeUtc(path_TSD);

            this.partOSimulationContext = partOSimulationContext;

            State = PartORunState.WorkflowCompleted;
            InvalidationReason = null;

            OnStateChanged();

            return true;
        }

        /// <summary>
        /// Reconnects this run to a model <b>reopened from disk</b> that records the results it was produced
        /// from - so a completed, saved run's TM59 assessment can be reviewed in a later session without
        /// rerunning the annual simulation.
        /// <para>
        /// <b>The other way into <see cref="PartORunState.WorkflowCompleted"/>, and deliberately narrower
        /// than <see cref="Complete"/>.</b> Complete pairs a run with the workflow this session just watched
        /// write the results. Restore pairs it with the model's own persisted statement of the same fact:
        /// the <c>SimulationResultProvenance</c> and the overheating scenarios stamped onto the model when it
        /// was produced - the run's own <c>&lt;project&gt;.sam</c>, or the user's own save of the same model
        /// (see <c>Modify.RunPartOSimulation</c>). <c>SimulationResultProvenance.TryResolvePath_TSD</c> then
        /// validates the whole rule: the record is complete, the results file matches the recorded path,
        /// length and write time, the design state matches its fingerprint, and the scenarios about to be
        /// read below match theirs. Anything that fails is refused; nothing is adopted on its name alone.
        /// </para>
        /// <para>
        /// <b>The scenarios are read, never inferred.</b> What is loaded below is exactly the collection
        /// persisted with this run, and the fingerprint above is what proves it is the collection the results
        /// were assessed under. They are never rebuilt from room names, zone layout or anything else on the
        /// reopened model: a regenerated scenario set would be a new assessment authority wearing an old
        /// run's results.
        /// </para>
        /// <para>
        /// <b>Review, never resume.</b> A restored run holds no <see cref="PreparationContext"/> and no
        /// <see cref="SimulationContext"/> - those are statements about how this session prepared and ran the
        /// case, and a file cannot reproduce them. Iteration 2B therefore refuses a restored run, exactly as
        /// it refuses any run it cannot repeat.
        /// </para>
        /// <para>
        /// <b>A model with no record is not an error.</b> Where the model carries no provenance at all -
        /// never simulated on this path, or saved before the record existed - the run is simply left cleared
        /// with <paramref name="refusal"/> null, so the caller's default guidance ("prepare and simulate")
        /// still applies. A model that DOES carry a record but fails validation gets its reason, which the
        /// tooltip shows.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">
        /// The model just opened. It becomes <see cref="AnalyticalModel_Assessment"/> only where every check
        /// passes: it is the model the workflow returned, saved with its zone identities, which is what an
        /// assessment resolves against.
        /// </param>
        /// <param name="path_Model">
        /// The file the model was opened from, if known - used to locate the results beside it when the whole
        /// output folder moved since the run. Never trusted without the recorded fingerprint.
        /// </param>
        /// <param name="refusal">Why no run could be restored, or null where the model simply records none.</param>
        public bool Restore(AnalyticalModel analyticalModel, string path_Model, out string refusal)
        {
            //One announcement for the whole attempt, whatever it decides: this clears the run before it
            //knows whether it can restore one, and the refusal paths below return a reason rather than
            //invalidating, so announcing inside would either report a state nobody saw or not report a
            //completed run having just been cleared.
            bool result = RestoreCore(analyticalModel, path_Model, out refusal);

            OnStateChanged();

            return result;
        }

        /// <summary>The restore itself. See <see cref="Restore"/>, which is the only caller.</summary>
        private bool RestoreCore(AnalyticalModel analyticalModel, string path_Model, out string refusal)
        {
            refusal = null;

            ResetCore();

            if (analyticalModel is null)
            {
                return false;
            }

            //Qualified: this namespace declares its own AnalyticalModelParameter (UIGeometrySettings), which
            //would otherwise shadow the SAM.Analytical one being read here.
            if (!analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out SimulationResultProvenance simulationResultProvenance) || simulationResultProvenance is null)
            {
                //Nothing recorded: a model that never went through the full-year simulation path, or one
                //saved before the record existed. Not an invalidation - the default guidance already covers
                //it, and inventing a pairing here is the failure this type exists to prevent.
                return false;
            }

            if (!simulationResultProvenance.TryResolvePath_TSD(analyticalModel, path_Model, out string path_TSD, out refusal))
            {
                InvalidationReason = refusal;

                return false;
            }

            //Exactly the scenarios persisted with this run, which the fingerprint check above has already
            //proved are the ones its results were assessed under. Read, never derived: nothing here looks at
            //a room name, a zone or the design to decide what was assessed.
            List<OverheatingScenario> overheatingScenarios_Temp = [];
            if (analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, out SAM.Core.SAMCollection<OverheatingScenario> collection) && collection is not null)
            {
                foreach (OverheatingScenario overheatingScenario in collection)
                {
                    if (overheatingScenario is not null)
                    {
                        overheatingScenarios_Temp.Add(overheatingScenario);
                    }
                }
            }

            if (overheatingScenarios_Temp.Count == 0)
            {
                //Belt and braces behind the scenario fingerprint: a complete record is never taken over an
                //empty scenario set, so reaching here means the record is stating something the model does
                //not carry. Without the scenarios there is no authoritative ventilation strategy for any
                //space, and the assessment refuses rather than defaults.
                refusal = "This model records the simulation results it was produced from, but not the overheating scenarios they were assessed against, so its TM59 assessment cannot be reviewed. Prepare the iteration again and re-run the simulation.";

                InvalidationReason = refusal;

                return false;
            }

            analyticalModel_Workflow = analyticalModel;
            this.path_TSD = path_TSD;
            dateTime_TSD = new System.DateTime(simulationResultProvenance.Timestamp_TSD, System.DateTimeKind.Utc);
            overheatingScenarios = overheatingScenarios_Temp;

            //Deliberately left null: a restored run can be assessed but not re-prepared or re-simulated as
            //the case it came from, which is what keeps it out of Iteration 2B.
            partOPreparationContext = null;
            partOSimulationContext = null;

            State = PartORunState.WorkflowCompleted;
            IsRestored = true;
            InvalidationReason = null;

            CanResumeIteration3 = TryResume(analyticalModel, simulationResultProvenance, path_TSD, out string refusal_Resume);
            ResumeRefusal = refusal_Resume;

            OnStateChanged();

            return true;
        }

        /// <summary>
        /// Adopts the preparation and case a completed run saved beside its results, where they are provably this
        /// run's: the sidecar names these results' TSD by length and write time, and the prepared model beside it
        /// still has the fingerprint it was saved with. Anything else leaves the run review-only, with the reason.
        /// </summary>
        private bool TryResume(AnalyticalModel analyticalModel, SimulationResultProvenance simulationResultProvenance, string path_TSD, out string refusal)
        {
            refusal = null;

            PartORunResume partORunResume = PartORunResume.Read(PartORunResume.Path_Resume(path_TSD));
            if (partORunResume is null)
            {
                refusal = "No saved preparation was found beside these results (runs completed before this was recorded, or on another build), so Iteration 3 needs Iteration 1a prepared and run in this session.";
                return false;
            }

            if (partORunResume.Length_TSD != simulationResultProvenance.Length_TSD || partORunResume.Timestamp_TSD != simulationResultProvenance.Timestamp_TSD)
            {
                refusal = "The saved preparation beside these results belongs to a different simulation of them, so it is not used.";
                return false;
            }

            string path_Prepared = PartORunResume.Path_PreparedModel(path_TSD);
            AnalyticalModel analyticalModel_Prepared_Temp = null;
            try
            {
                List<AnalyticalModel> analyticalModels = System.IO.File.Exists(path_Prepared) ? Core.Convert.ToSAM<AnalyticalModel>(path_Prepared) : null;
                analyticalModel_Prepared_Temp = analyticalModels is not null && analyticalModels.Count == 1 ? analyticalModels[0] : null;
            }
            catch
            {
                analyticalModel_Prepared_Temp = null;
            }

            if (analyticalModel_Prepared_Temp is null || SimulationResultProvenance.Fingerprint(analyticalModel_Prepared_Temp) != partORunResume.Fingerprint_PreparedModel)
            {
                refusal = string.Format("The prepared model saved beside these results ('{0}') is missing or no longer the one they were prepared from, so it is not used.", path_Prepared);
                return false;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel_Prepared_Temp.AdjacencyCluster;
            List<Zone> zones = [];
            foreach (System.Guid guid in partORunResume.Guids_Zone)
            {
                Zone zone = adjacencyCluster?.GetObject<Zone>(guid);
                if (zone is null)
                {
                    refusal = "A dwelling zone the saved preparation names is not in the saved prepared model, so it is not used.";
                    return false;
                }

                zones.Add(zone);
            }

            foreach (System.Guid guid in partORunResume.Guids_VentilationSystem)
            {
                if (adjacencyCluster?.GetObject<VentilationSystem>(guid) is null)
                {
                    refusal = "A ventilation system the saved preparation built is not in the saved prepared model, so it is not used.";
                    return false;
                }
            }

            if (!System.Enum.TryParse(partORunResume.SolarCalculationMethod, out SolarCalculationMethod solarCalculationMethod))
            {
                refusal = "The saved case states a solar calculation method this build does not know, so it is not used.";
                return false;
            }

            analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out Weather.WeatherData weatherData);

            analyticalModel_Prepared = analyticalModel_Prepared_Temp;
            guids_VentilationSystem_Prepared = [.. partORunResume.Guids_VentilationSystem];
            //Resumed, not re-made: no descriptors (Iteration 2B never starts from a restored run), and whether a
            //catalogue was offered exactly as the sidecar records it - unknown on a v1 sidecar.
            partOPreparationContext = PartOPreparationContext.Resumed(partORunResume.PartOIteration, zones, partORunResume.VentilationUnitCatalogueOffered);
            partOSimulationContext = new PartOSimulationContext(
                System.IO.Path.GetDirectoryName(path_TSD),
                System.IO.Path.GetFileNameWithoutExtension(path_TSD),
                weatherData,
                solarCalculationMethod,
                partORunResume.SimulateFrom,
                partORunResume.SimulateTo)
            {
                UnmetHours = partORunResume.UnmetHours,
                Sizing = partORunResume.Sizing,
                UseWidths = partORunResume.UseWidths,
                UpdateConstructionLayersByPanelType = partORunResume.UpdateConstructionLayersByPanelType,
            };

            return true;
        }

        /// <summary>
        /// Whether an assessment may run right now, and why not where it may not.
        /// <para>
        /// Re-checks the TSD rather than trusting <see cref="State"/> alone: the file is on disk, and anything
        /// - another SAM session, a rerun from outside this window - can have replaced it since. A result read
        /// from a file this run did not write would be attributed to this run's scenarios.
        /// </para>
        /// <para>
        /// <b>A completed run that fails that check is dropped here, not merely refused.</b> Otherwise
        /// <see cref="State"/> stays <see cref="PartORunState.WorkflowCompleted"/>, <see cref="CanAssess"/>
        /// stays true, and the ribbon re-enables the command with its success tooltip the moment the refusal
        /// dialog is dismissed - offering a click that is known to fail, over and over. It is also what the
        /// invariant requires: the state means "this run produced the full-year results being assessed", and a
        /// deleted or rewritten file is no longer those results. Same rule as everywhere else in this type -
        /// staleness is rejected, not carried.
        /// </para>
        /// <para>
        /// Deliberately <b>not</b> read by <see cref="CanAssess"/>, which stays a pure state read: a property
        /// the ribbon evaluates on every refresh must not touch the filesystem, and must not drop a run as a
        /// side effect of being looked at.
        /// </para>
        /// </summary>
        public bool IsAssessable(out string refusal)
        {
            refusal = null;

            if (State != PartORunState.WorkflowCompleted)
            {
                //NOT invalidated: Prepared is a live run waiting for its simulation, and None has already been
                //explained. Only the two results checks below drop anything.
                refusal = State == PartORunState.Prepared
                    ? "The Part O iteration is prepared but has not been simulated, so there are no results to assess. Run the TAS simulation first."
                    : "No Part O run is available to assess. " + (InvalidationReason ?? "Prepare an iteration and run the TAS simulation.");

                return false;
            }

            if (!File.Exists(path_TSD))
            {
                refusal = string.Format("The simulation results this run produced are no longer at '{0}'. Prepare the iteration again and re-run the simulation.", path_TSD);

                Invalidate(refusal);

                return false;
            }

            if (File.GetLastWriteTimeUtc(path_TSD) != dateTime_TSD)
            {
                refusal = string.Format("The simulation results at '{0}' have been rewritten since this Part O run produced them, so they are no longer the results this run's overheating scenarios describe. Prepare the iteration again and re-run the simulation.", path_TSD);

                Invalidate(refusal);

                return false;
            }

            return true;
        }

        /// <summary>Drops the run and records why. Idempotent; the first reason is not overwritten by a later one.</summary>
        public void Invalidate(string reason)
        {
            InvalidateCore(reason);

            OnStateChanged();
        }

        /// <summary>
        /// The drop itself, without announcing it - so <see cref="Reset"/>, which finishes by clearing the
        /// reason this records, raises <see cref="StateChanged"/> once for the whole clearing rather than
        /// once for a state nothing ever saw.
        /// </summary>
        private void InvalidateCore(string reason)
        {
            analyticalModel_Prepared = null;
            overheatingScenarios = [];
            analyticalModel_Workflow = null;
            path_TSD = null;
            dateTime_TSD = default;
            modificationExpected = false;
            IsRestored = false;
            CanResumeIteration3 = false;
            ResumeRefusal = null;

            //Cleared with everything else: a dropped run's preparation inputs and TAS case must not be
            //picked up by its successor, which was prepared and simulated differently.
            partOPreparationContext = null;
            partOSimulationContext = null;

            //Cleared with everything else, and for the sharpest version of the same reason: a captured
            //system identity that outlived its preparation would scope the NEXT run's Iteration 3
            //materialisation to the PREVIOUS run's design, and every guid in it would resolve, so nothing
            //downstream could tell.
            guids_VentilationSystem_Prepared = [];

            //Cleared with everything else: an Iteration 3 attempt's TAS work belongs to the run it was built on,
            //and reusing it for a successor would pair that successor with another design's TAS results.
            iteration3Checkpoints.Clear();

            //Cleared with everything else: a workflow announced to the run that has just been dropped must not
            //be able to complete its successor.
            resultsExpected = false;
            path_TSD_Expected = null;
            exists_TSD_Expected = false;
            length_TSD_Expected = -1;
            dateTime_TSD_Expected = default;

            State = PartORunState.None;

            InvalidationReason ??= reason;
        }

        /// <summary>
        /// Clears the run outright - no pending state and no reason. For closing or opening a model, where
        /// there is nothing to explain: the run did not go stale, it stopped applying.
        /// </summary>
        public void Reset()
        {
            ResetCore();

            OnStateChanged();
        }

        /// <summary>
        /// The clearing itself, without announcing it - for the two transitions that <b>start</b> by
        /// clearing, <see cref="Prepare(AnalyticalModel, IEnumerable{OverheatingScenario}, PartOPreparationContext, string)"/>
        /// and <see cref="Restore"/>. Each of those announces its own outcome once, and a run cleared on the
        /// way to being prepared is a state nothing ever saw.
        /// </summary>
        private void ResetCore()
        {
            InvalidateCore(null);

            InvalidationReason = null;
        }
    }
}
