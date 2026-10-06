// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// The mixed Part O design command: a strategy per dwelling, optional screening, and ONE mixed model built from
        /// the clean baseline and run.
        ///
        /// <para><b>An additional route - the frozen Part O workflow is untouched</b></para>
        /// <para>
        /// Prepare &amp; Run (1a / 1b / 2 / 2B / 3) and this command share no state: this one owns no
        /// <see cref="PartORun"/> of the session's, and never replaces the open model with a prepared or simulated one.
        /// Its only write to the open model is the selected strategy set, when a person saves it
        /// (<see cref="SavePartOMixedSelection"/>) - a genuine model change, reported to the session as one.
        /// </para>
        ///
        /// <para><b>The open model stays the clean baseline</b></para>
        /// <para>
        /// Screening and the final run materialise COPIES (<c>SAM.Analytical.Modify.MaterialisePartODwellingStrategies</c>
        /// is pure), simulate them in private runs, and keep their results beside their own TSDs. So the old failure -
        /// preparation overwriting the only usable model - cannot happen here, and every rebuild starts from the same
        /// baseline with the whole current selection.
        /// </para>
        /// </summary>
        public static void RunPartOMixedDesign(this UIAnalyticalModel? uIAnalyticalModel, IWin32Window? owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject is null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            string? path_Model = uIAnalyticalModel.Path;

            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();
            List<VentilationUnitCapacityDescriptor>? descriptors = ventilationUnitCatalogue.HasSelectableProducts ? ventilationUnitCatalogue.CapacityDescriptors : null;

            //The same catalogue's templates: a cooled dwelling's cooling is its product's manufacturer guidance, read by SAM.
            List<VentilationUnitTemplate>? templates = ventilationUnitCatalogue.HasSelectableProducts ? ventilationUnitCatalogue.Templates : null;

            string? path_State = PartOMixedDesignState.Path_State(path_Model);

            PartOMixedDesignSession partOMixedDesignSession = new(analyticalModel, path_Model, descriptors, PartOMixedDesignState.Read(path_State), templates);

            ActiveSetting.Setting.TryGetValue(AnalyticalSettingParameter.SimulateOptions_PartO, out SimulateOptions simulateOptions_Remembered);
            PartOSimulationCase partOSimulationCase = PartOSimulationCase.Create(analyticalModel, path_Model, simulateOptions_Remembered);
            partOMixedDesignSession.SimulationCaseKey = Query.PartOSimulationCaseKey(partOSimulationCase);

            string? outcome = null;
            PartOMixedDwellingFilter partOMixedDwellingFilter = PartOMixedDwellingFilter.All;
            string searchText = string.Empty;
            bool grouped = false;

            while (true)
            {
                PartOMixedDesignWindow partOMixedDesignWindow = new()
                {
                    Session = partOMixedDesignSession,
                    SimulationCase = partOSimulationCase,
                    LastOutcome = outcome,
                    Filter = partOMixedDwellingFilter,
                    SearchText = searchText,
                    Grouped = grouped,
                };

                if (owner is not null)
                {
                    new System.Windows.Interop.WindowInteropHelper(partOMixedDesignWindow).Owner = owner.Handle;
                }

                bool? showDialog = partOMixedDesignWindow.ShowDialog();

                partOSimulationCase = partOMixedDesignWindow.SimulationCase;
                partOMixedDesignSession.SimulationCaseKey = Query.PartOSimulationCaseKey(partOSimulationCase);
                partOMixedDwellingFilter = partOMixedDesignWindow.Filter;
                searchText = partOMixedDesignWindow.SearchText;
                grouped = partOMixedDesignWindow.Grouped;

                //The constraints and the screening choice are the project's, whatever was done - kept beside the model,
                //unless the model was refused as a baseline (see WritePartOMixedDesignState).
                WriteState(partOMixedDesignSession, path_State);

                if (showDialog != true)
                {
                    return;
                }

                switch (partOMixedDesignWindow.Action)
                {
                    case PartOMixedDesignAction.Save:
                        outcome = SavePartOMixedSelection(uIAnalyticalModel, partOMixedDesignSession);
                        break;

                    case PartOMixedDesignAction.SaveAndClose:
                        SavePartOMixedSelection(uIAnalyticalModel, partOMixedDesignSession);
                        return;

                    case PartOMixedDesignAction.Check:
                        outcome = CheckPartOMixedDesign(partOMixedDesignSession, owner);
                        if (partOMixedDesignSession.Rows.Any(x => x.NeedsAttention))
                        {
                            partOMixedDwellingFilter = PartOMixedDwellingFilter.NeedsAttention;
                        }
                        break;

                    case PartOMixedDesignAction.Screen:
                        partOMixedDesignSession.State.Strategies_Screening.Clear();
                        partOMixedDesignSession.State.Strategies_Screening.AddRange(partOMixedDesignWindow.ScreeningStrategies);
                        partOMixedDesignSession.State.ScreeningMode = partOMixedDesignWindow.ScreeningMode;

                        outcome = ScreenPartOMixedDesign(uIAnalyticalModel, partOMixedDesignSession, partOSimulationCase, partOMixedDesignWindow.ScreeningStrategies, partOMixedDesignWindow.ScreeningMode, owner);
                        WriteState(partOMixedDesignSession, path_State);
                        break;

                    case PartOMixedDesignAction.BuildAndRun:
                        outcome = BuildAndRunPartOMixedDesign(uIAnalyticalModel, partOMixedDesignSession, partOSimulationCase, owner);
                        WriteState(partOMixedDesignSession, path_State);
                        if (partOMixedDesignSession.Rows.Any(x => x.NeedsAttention))
                        {
                            partOMixedDwellingFilter = PartOMixedDwellingFilter.NeedsAttention;
                        }
                        else if (partOMixedDesignSession.Rows.Any(x => x.FinalFail))
                        {
                            partOMixedDwellingFilter = PartOMixedDwellingFilter.FailingFinal;
                        }
                        break;

                    case PartOMixedDesignAction.ReviewFinal:
                        outcome = ReviewPartOMixedDesign(partOMixedDesignSession, owner) ?? outcome;
                        break;

                    default:
                        return;
                }
            }
        }

        /// <summary>
        /// The final mixed run's space-level TM59 result, through the existing review path: the run's own persisted
        /// model is reopened, restored against its results by <see cref="PartORun.Restore"/> (the provenance check every
        /// reopened Part O run passes), and shown in the TM59 result window. A private run - the session's is untouched -
        /// and no TAS.
        /// </summary>
        private static string? ReviewPartOMixedDesign(PartOMixedDesignSession partOMixedDesignSession, IWin32Window? owner)
        {
            PartOMixedRunEvidence? partOMixedRunEvidence = partOMixedDesignSession.State.FinalRun;
            string? path_RunModel = partOMixedRunEvidence?.Path_RunModel;

            if (!partOMixedDesignSession.FinalCurrent || string.IsNullOrWhiteSpace(path_RunModel) || !File.Exists(path_RunModel))
            {
                return "There is no current final mixed run to open.";
            }

            AnalyticalModel? analyticalModel_Run = Core.Convert.ToSAM<AnalyticalModel>(path_RunModel)?.FirstOrDefault();

            PartORun partORun = new();
            string? refusal = null;
            if (analyticalModel_Run is null || !partORun.Restore(analyticalModel_Run, path_RunModel, out refusal))
            {
                string text = string.Format("The final mixed run's result could not be reopened from '{0}'. {1}", path_RunModel, analyticalModel_Run is null ? "The run model could not be read." : refusal ?? "It records no results.");
                MessageBox.Show(owner, text, "Part O — Mixed Design");
                return text;
            }

            ReviewPartOTM59(partORun, owner);

            return null;
        }

        /// <summary>
        /// Writes the draft selection onto the open model - the one write this workflow makes to it - and rebases the
        /// session on the model now open.
        /// </summary>
        internal static string SavePartOMixedSelection(UIAnalyticalModel uIAnalyticalModel, PartOMixedDesignSession partOMixedDesignSession)
        {
            if (!partOMixedDesignSession.IsDirty)
            {
                return "The selection on the model is already up to date.";
            }

            AnalyticalModel analyticalModel = partOMixedDesignSession.WithSelection();

            //A genuine change of the model, announced as one: a legacy Part O run prepared on this model no longer
            //describes it, and PartORun drops it for exactly that reason.
            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());

            partOMixedDesignSession.Rebase(analyticalModel);

            return string.Format("Selection saved onto the model ({0}). Save the model to keep it.", partOMixedDesignSession.Readiness().Text);
        }

        /// <summary>
        /// Asks SAM to materialise the selected design from the baseline and, on the TAS Systems route, runs the same
        /// Systems preflight Build &amp; Run runs before TAS (<see cref="CheckPartOMixedDesign(AnalyticalModel, IEnumerable{VentilationUnitCapacityDescriptor}, IEnumerable{VentilationUnitTemplate})"/>)
        /// - no simulation - and attaches every structured refusal to the dwelling it names.
        /// </summary>
        private static string CheckPartOMixedDesign(PartOMixedDesignSession partOMixedDesignSession, IWin32Window? owner)
        {
            PartOMixedDesignCheck partOMixedDesignCheck;

            using (new SAM.Core.UI.WPF.ProgressBarWindowManager("Part O — Check design", "Materialising the selected design from the baseline..."))
            {
                partOMixedDesignCheck = CheckPartOMixedDesign(partOMixedDesignSession.WithSelection(), partOMixedDesignSession.DescriptorsOffered, partOMixedDesignSession.TemplatesOffered);
            }

            PartOMaterialisation partOMaterialisation = partOMixedDesignCheck.Materialisation!;

            partOMixedDesignSession.SetRefusals(partOMaterialisation.Refusals);

            //Which systems the design's assessment includes - the same SAM scope Build & Run reports (PR-6).
            RecordSystemsInAssessment(partOMixedDesignSession, partOMixedDesignCheck);

            if (partOMaterialisation.IsMaterialised && partOMixedDesignCheck.Refusal_Systems is not null)
            {
                MessageBox.Show(owner, partOMixedDesignCheck.Refusal_Systems, "Part O — Check design");

                return string.Format("Check: SAM can build this mixed design ({0}), but its TAS Systems ventilation cannot be prepared, so Build & Run would stop before TAS. Nothing was simulated.", partOMixedDesignSession.Readiness().Text);
            }

            if (partOMaterialisation.IsMaterialised)
            {
                string systems = partOMixedDesignCheck.SystemsChecked ? " The TAS Systems ventilation can be prepared." : string.Empty;

                return string.Format("Check: SAM can build this mixed design ({0}).{1}{2} Nothing was simulated.", partOMixedDesignSession.Readiness().Text, CoolingText(partOMaterialisation), systems);
            }

            MessageBox.Show(owner, RefusalText(partOMaterialisation.Refusals), "Part O — Check design");

            return string.Format("Check: SAM refused the design ({0}). Nothing was built.", UI.Query.PartOCount(partOMaterialisation.Refusals.Count, "refusal", "refusals"));
        }

        /// <summary>Hands the session SAM's systems answer from a Check design - null where SAM refused to materialise the design.</summary>
        internal static void RecordSystemsInAssessment(PartOMixedDesignSession partOMixedDesignSession, PartOMixedDesignCheck partOMixedDesignCheck)
        {
            partOMixedDesignSession.SetSystemsInAssessment(partOMixedDesignCheck.SystemsInAssessment);
        }

        /// <summary>
        /// Hands the session the systems a Build &amp; Run took, from the SAM scope its preflight used. SAM refusing to
        /// materialise the design clears the answer; a run cancelled before the preflight has none to give, so an earlier
        /// answer for this same design stays and one for another design is already stale.
        /// </summary>
        internal static void RecordSystemsInAssessment(PartOMixedDesignSession partOMixedDesignSession, PartOStrategySetRun partOStrategySetRun)
        {
            if (!partOStrategySetRun.IsMaterialised)
            {
                partOMixedDesignSession.SetSystemsInAssessment(null);
            }
            else if (partOStrategySetRun.Simulation?.SystemsInAssessment is PartOSystemsInAssessment partOSystemsInAssessment)
            {
                partOMixedDesignSession.SetSystemsInAssessment(partOSystemsInAssessment);
            }
        }

        private static string ScreenPartOMixedDesign(UIAnalyticalModel uIAnalyticalModel, PartOMixedDesignSession partOMixedDesignSession, PartOSimulationCase partOSimulationCase, List<PartOScreeningStrategy> strategies, PartOScreeningMode partOScreeningMode, IWin32Window? owner)
        {
            string? refusal_Case = partOSimulationCase.Refusal();
            if (refusal_Case is not null)
            {
                return string.Format("Screening not started: {0}", refusal_Case);
            }

            AnalyticalModel analyticalModel_Baseline = partOMixedDesignSession.Baseline;

            List<PartOScreeningStrategy> strategies_Runnable = [.. UI.Query.PartOScreeningStrategies().Where(x => strategies.Contains(x) && UI.Query.PartOScreeningStrategyUnavailable(x, partOMixedDesignSession.CatalogueHasProducts) is null)];
            if (strategies_Runnable.Count == 0)
            {
                return "Screening not started: none of the chosen strategies can be screened.";
            }

            if (!RunModelsSafe(uIAnalyticalModel.Path, partOSimulationCase, strategies_Runnable.Select(x => Create.PartOMixedProjectName(analyticalModel_Baseline, ScreeningSuffix(x))), out string? refusal_Path))
            {
                return string.Format("Screening not started: {0}", refusal_Path);
            }

            PartOScreeningOutcome partOScreeningOutcome;

            using (PartOProgressHost partOProgressHost = new(
                "Screen dwelling strategies",
                "Evidence only - your selected design is not changed.",
                strategies_Runnable.Select(x => string.Format("{0} (full year)", UI.Query.PartOScreeningStrategyLabel(x)))))
            {
                partOScreeningOutcome = ScreenPartODwellingStrategies(
                    analyticalModel_Baseline,
                    partOMixedDesignSession.Rows.Select(x => x.ZoneGuid),
                    strategies_Runnable,
                    partOScreeningMode,
                    partOMixedDesignSession.Constraints,
                    partOMixedDesignSession.Descriptors,
                    x => Create.PartOMixedSimulationContext(analyticalModel_Baseline, uIAnalyticalModel.Path, partOSimulationCase, Create.PartOMixedProjectName(analyticalModel_Baseline, ScreeningSuffix(x))),
                    partOProgressHost.Token,
                    null,
                    (index, strategy) => partOProgressHost.Start(strategies_Runnable.IndexOf(strategy)));

                if (partOScreeningOutcome.Cancelled)
                {
                    partOProgressHost.State.Fail("Cancelled");
                }
                else
                {
                    partOProgressHost.State.SkipUnstarted();
                    partOProgressHost.State.Complete();
                }
            }

            partOMixedDesignSession.ApplyScreening(partOScreeningOutcome.Evidence);

            //What a person has to read: refusals, runs that did not complete, and what was not run and why.
            List<string> lines = [];
            foreach (PartOScreeningStep partOScreeningStep in partOScreeningOutcome.Steps)
            {
                string label = UI.Query.PartOScreeningStrategyLabel(partOScreeningStep.Strategy);

                if (partOScreeningStep.Evidence is not null)
                {
                    lines.Add(string.Format("{0}: {1} pass of {2} screened.", label, partOScreeningStep.Evidence.Results.Count(x => x.Outcome == PartODwellingOutcome.Pass), partOScreeningStep.DwellingCount));

                    //What the run warned about is part of the evidence it produced.
                    if (partOScreeningStep.Notes.Count != 0)
                    {
                        lines.Add(string.Format("   {0} run notes ({1}): {2}", label, partOScreeningStep.Notes.Count, string.Join(" | ", partOScreeningStep.Notes.Take(3))));
                    }
                }
                else if (partOScreeningStep.Cancelled)
                {
                    lines.Add(string.Format("{0}: cancelled - no result.", label));
                }
                else if (partOScreeningStep.Refusals.Count != 0)
                {
                    lines.Add(string.Format("{0}: SAM refused to build it.\n{1}", label, RefusalText(partOScreeningStep.Refusals)));
                }
                else if (partOScreeningStep.Refusal_Simulation is not null)
                {
                    lines.Add(string.Format("{0}: not completed - {1}", label, partOScreeningStep.Refusal_Simulation));
                }
                else if (partOScreeningStep.Skipped is not null)
                {
                    lines.Add(string.Format("{0}: {1}", label, partOScreeningStep.Skipped));
                }
            }

            if (partOScreeningOutcome.Steps.Any(x => x.Refusals.Count != 0 || x.Refusal_Simulation is not null || x.Cancelled))
            {
                MessageBox.Show(owner, string.Join("\n\n", lines), "Part O — Screen dwelling strategies");
            }

            string prefix = partOScreeningOutcome.Cancelled ? "Screening cancelled; completed strategies kept, the selection is unchanged." : "Screening complete; the selection is unchanged.";

            return string.Format("{0} {1}", prefix, string.Join(" ", lines.Select(x => x.Split('\n')[0])));
        }

        private static string BuildAndRunPartOMixedDesign(UIAnalyticalModel uIAnalyticalModel, PartOMixedDesignSession partOMixedDesignSession, PartOSimulationCase partOSimulationCase, IWin32Window? owner)
        {
            PartOMixedReadiness partOMixedReadiness = partOMixedDesignSession.Readiness();
            if (!partOMixedReadiness.CanBuild)
            {
                return string.Format("Not built: {0}", string.Join(" ", partOMixedReadiness.Blockers.DefaultIfEmpty(partOMixedReadiness.Text)));
            }

            string? refusal_Case = partOSimulationCase.Refusal();
            if (refusal_Case is not null)
            {
                return string.Format("Not built: {0}", refusal_Case);
            }

            //The selection is SAM authority on the baseline: the materialisation reads it from there, so it is saved first.
            string? saved = partOMixedDesignSession.IsDirty ? SavePartOMixedSelection(uIAnalyticalModel, partOMixedDesignSession) : null;

            AnalyticalModel analyticalModel_Baseline = partOMixedDesignSession.Baseline;
            string projectName = Create.PartOMixedProjectName(analyticalModel_Baseline, "Mixed");

            //A Systems-route run's model is named from its bridge results; neither may land on the open model.
            string projectName_Bridge = System.IO.Path.GetFileNameWithoutExtension(Query.Path_PartOMixedBridgeTBD(string.Empty, projectName));

            if (!RunModelsSafe(uIAnalyticalModel.Path, partOSimulationCase, [projectName, projectName_Bridge], out string? refusal_Path))
            {
                return string.Format("Not built: {0}", refusal_Path);
            }

            PartOMixedRunEvidence? partOMixedRunEvidence;
            PartOStrategySetRun partOStrategySetRun;

            using (PartOProgressHost partOProgressHost = new(
                "Build & Run Mixed Design",
                string.Format("{0} - one mixed model, materialised from the clean baseline as a copy.", partOMixedReadiness.Text),
                ["Materialise the mixed model from the baseline", "TAS simulation (full year) and TM59 assessment"]))
            {
                partOProgressHost.Start(0);

                partOMixedRunEvidence = BuildAndRunPartOMixedDesign(
                    analyticalModel_Baseline,
                    partOMixedDesignSession.CatalogueOffered,
                    partOMixedDesignSession.Descriptors,
                    Create.PartOMixedSimulationContext(analyticalModel_Baseline, uIAnalyticalModel.Path, partOSimulationCase, projectName),
                    partOProgressHost.Token,
                    out partOStrategySetRun,
                    null,
                    () => partOProgressHost.Start(1),
                    partOMixedDesignSession.Templates);

                if (partOMixedRunEvidence is null)
                {
                    partOProgressHost.State.Fail(partOStrategySetRun.Cancelled ? "Cancelled" : null);
                }
                else
                {
                    partOProgressHost.State.Complete();
                }
            }

            string prefix = saved is null ? string.Empty : "Selection saved onto the model. ";

            if (!partOStrategySetRun.IsMaterialised)
            {
                partOMixedDesignSession.SetRefusals(partOStrategySetRun.Refusals);
                RecordSystemsInAssessment(partOMixedDesignSession, partOStrategySetRun);

                MessageBox.Show(owner, RefusalText(partOStrategySetRun.Refusals), "Part O — Build & Run Mixed Design");

                return string.Format("{0}Not built: SAM refused the design ({1}). Nothing was simulated; the affected dwellings are shown.", prefix, UI.Query.PartOCount(partOStrategySetRun.Refusals.Count, "refusal", "refusals"));
            }

            partOMixedDesignSession.SetRefusals(null);

            RecordSystemsInAssessment(partOMixedDesignSession, partOStrategySetRun);

            if (partOStrategySetRun.Cancelled || partOMixedRunEvidence is null)
            {
                //A run that did not complete may already have rewritten the results file the previous result was assessed
                //from (the run writes to the same path), so that result is asked again rather than kept as current.
                bool current_Before = partOMixedDesignSession.FinalCurrent;
                partOMixedDesignSession.RevalidateFinal();
                string previous = !current_Before
                    ? string.Empty
                    : partOMixedDesignSession.FinalCurrent ? " The previous result is still current." : string.Format(" The previous result is now STALE: {0}", partOMixedDesignSession.FinalStale);

                if (partOStrategySetRun.Cancelled)
                {
                    return string.Format("{0}Mixed run cancelled. The selection is unchanged.{1}", prefix, previous);
                }

                string refusal = partOStrategySetRun.Simulation?.Refusal ?? "The mixed run produced no assessable results.";

                MessageBox.Show(owner, refusal, "Part O — Build & Run Mixed Design");

                return string.Format("{0}Mixed run not completed: {1}{2}", prefix, refusal, previous);
            }

            partOMixedDesignSession.ApplyFinal(partOMixedRunEvidence);

            //What the run itself warned about (a pre-simulation check warning, a run model that could not be kept) is part
            //of its outcome - never dropped behind a clean-looking success.
            List<string> notes_Run = partOStrategySetRun.Simulation?.Notes ?? [];
            string notes = notes_Run.Count == 0 ? string.Empty : string.Format(" Run notes ({0}): {1}", notes_Run.Count, string.Join(" | ", notes_Run.Take(3)));

            return string.Format("{0}Mixed design built and run: {1}{2}{3}", prefix, partOMixedDesignSession.FinalText, CoolingText(partOStrategySetRun.Materialisation), notes);
        }

        /// <summary>
        /// What SAM's record says about cooling, in a sentence (or empty where no dwelling is cooled): which dwellings are
        /// cooled, by which product, at the cooling operating airflow SAM resolved - reported, never computed here.
        /// </summary>
        internal static string CoolingText(PartOMaterialisation? partOMaterialisation)
        {
            List<PartOCooledDwelling> partOCooledDwellings = partOMaterialisation?.Record?.CooledDwellings ?? [];
            if (partOCooledDwellings.Count == 0)
            {
                return string.Empty;
            }

            AdjacencyCluster? adjacencyCluster = partOMaterialisation!.AnalyticalModel?.AdjacencyCluster;
            IEnumerable<string> dwellings = partOCooledDwellings.Take(5).Select(x => string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} ({1} at {2:0.#} l/s)", adjacencyCluster?.GetObject<Zone>(x.ZoneGuid)?.Name ?? x.ZoneGuid.ToString(), x.VentilationUnitReference, x.CoolingOperatingAirFlow_Lps));

            return string.Format(" Active cooling from the product's manufacturer guidance: {0}{1}; the whole building runs on the TAS Systems route.", string.Join(", ", dwellings), partOCooledDwellings.Count > 5 ? string.Format(" and {0} more", partOCooledDwellings.Count - 5) : string.Empty);
        }

        /// <summary>SAM's structured refusals, grouped by reason, each with its subject - never collapsed into one generic message.</summary>
        internal static string RefusalText(IEnumerable<PartOMaterialisationRefusal>? partOMaterialisationRefusals)
        {
            List<string> lines = [];

            foreach (IGrouping<Enums.PartOMaterialisationRefusalReason, PartOMaterialisationRefusal> grouping in (partOMaterialisationRefusals ?? []).GroupBy(x => x.Reason))
            {
                List<PartOMaterialisationRefusal> refusals = [.. grouping];

                lines.Add(string.Format("{0} ({1}):", Core.Query.Description(grouping.Key), refusals.Count));

                foreach (PartOMaterialisationRefusal partOMaterialisationRefusal in refusals.Take(5))
                {
                    lines.Add(string.Format("  • {0}{1}", string.IsNullOrWhiteSpace(partOMaterialisationRefusal.Subject) ? string.Empty : partOMaterialisationRefusal.Subject + ": ", partOMaterialisationRefusal.Message));
                }

                if (refusals.Count > 5)
                {
                    lines.Add(string.Format("  • …and {0} more.", refusals.Count - 5));
                }
            }

            return lines.Count == 0 ? "SAM refused the design." : string.Join("\n", lines);
        }

        private static string ScreeningSuffix(PartOScreeningStrategy partOScreeningStrategy)
        {
            return string.Format("Screen_{0}", partOScreeningStrategy);
        }

        /// <summary>
        /// Refuses a run whose persisted run model (<c>&lt;project&gt;.sam</c> beside its results) would land on the open
        /// model's own file - the baseline must never be overwritten by a run artefact.
        /// </summary>
        private static bool RunModelsSafe(string? path_Model, PartOSimulationCase partOSimulationCase, IEnumerable<string> projectNames, out string? refusal)
        {
            refusal = null;

            //Where the runs actually write: the MixedDesign case folder beneath the chosen root.
            string? directory = Create.PartOMixedOutputDirectory(partOSimulationCase);

            if (string.IsNullOrWhiteSpace(path_Model) || string.IsNullOrWhiteSpace(directory))
            {
                return true;
            }

            string path_Full = Path.GetFullPath(path_Model);
            foreach (string projectName in projectNames)
            {
                string path_Run = Path.GetFullPath(Path.Combine(directory, projectName + ".sam"));
                if (string.Equals(path_Run, path_Full, StringComparison.OrdinalIgnoreCase))
                {
                    refusal = string.Format("the run model '{0}' would overwrite the open model. Choose another output folder.", path_Run);
                    return false;
                }
            }

            return true;
        }

        private static void WriteState(PartOMixedDesignSession partOMixedDesignSession, string? path_State)
        {
            WritePartOMixedDesignState(partOMixedDesignSession, path_State);
        }

        /// <summary>
        /// Keeps the session's state beside the model - unless the model was refused as a baseline when the window
        /// opened. A refused model is never built from, so its session has nothing to keep: no sidecar is created
        /// beside it, and one already there is left exactly as it was.
        /// </summary>
        /// <returns>Whether the state was written.</returns>
        internal static bool WritePartOMixedDesignState(PartOMixedDesignSession partOMixedDesignSession, string? path_State)
        {
            if (path_State is null || !partOMixedDesignSession.OpenedOnCleanBaseline)
            {
                return false;
            }

            return partOMixedDesignSession.State.Write(path_State, out _);
        }
    }
}
