// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.WPF;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void Simulate(this UIAnalyticalModel uIAnalyticalModel)
        {
            Simulate(uIAnalyticalModel, null);
        }

        /// <summary>
        /// Runs the TAS workflow, optionally completing the session's Approved Document O run with the model
        /// the workflow returned and the results it wrote.
        /// </summary>
        /// <param name="partORun">
        /// The session's Part O run, or null where the caller has none.
        /// <para>
        /// <b>Only the guided Part O case completes a run</b> (<paramref name="partOWorkflow"/> over a
        /// <see cref="PartORunState.Prepared"/> run). It simulates the run's own prepared model - the open model
        /// is the design model and is never it (PR-4) - and the result stays with the run: the window is not
        /// replaced. Every other call is the ordinary simulation of the open model, which adopts its result as it
        /// always has; that replacement reaches the run as an unexpected modification and drops it, which is the
        /// intended outcome: a simulation of the design is not the prepared case, and must not be paired with the
        /// preparation's overheating scenarios.
        /// </para>
        /// </summary>
        /// <param name="partOWorkflow">
        /// Whether this call comes from the guided Approved Document O command, <c>RunPartOWorkflow</c>.
        ///
        /// <para><b>Why the intent is passed rather than inferred from <paramref name="partORun"/></b></para>
        /// <para>
        /// The ribbon's own Simulate button also hands over the session's run - it has to, so that a workflow
        /// over a prepared model can complete it - and that button is the ordinary expert command. Inferring
        /// "this is a Part O run" from a prepared run alone would therefore have retuned and locked the
        /// expert dialog whenever a Part O iteration happened to be prepared, taking away the sizing-only and
        /// export runs an engineer may legitimately want over that same model.
        /// </para>
        /// <para>
        /// So the guided command says so and gets the Part O case chosen for it; every other caller reaches
        /// exactly the dialog it reached before, whatever state the run is in.
        /// </para>
        /// </param>
        public static void Simulate(this UIAnalyticalModel uIAnalyticalModel, PartORun partORun, bool partOWorkflow = false)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            // Whether this invocation is the guided command simulating a PREPARED Approved Document O
            // iteration - the one case with a TAS case of its own. See Create.SimulateOptions_PartO.
            //
            // The run state is the same condition RunPartOSimulation's pre-simulation gate is scoped to: a
            // run in any other state is one this method's own model replacement is about to drop, so it is
            // not a Part O run and does not get the Part O preset or the locked dialog.
            bool partO = partOWorkflow && partORun is not null && partORun.State == PartORunState.Prepared;

            //The Part O case is the run's prepared model, never the open design model (PR-4).
            AnalyticalModel analyticalModel_Source = partO ? partORun.AnalyticalModel_Prepared : analyticalModel;
            if (analyticalModel_Source is null)
            {
                return;
            }

            analyticalModel = analyticalModel_Source;

            SimulateWindow simulateWindow = new SimulateWindow();

            SimulateOptions simulateOptions;

            if (partO)
            {
                // A key of its own, so the two directions of contamination are both impossible: the manual
                // command's remembered options cannot decide a Part O run's TAS case - which is how a run
                // came to be started with Full Year Simulation unticked - and a Part O run cannot retune the
                // manual command behind an expert's back.
                ActiveSetting.Setting.TryGetValue(AnalyticalSettingParameter.SimulateOptions_PartO, out SimulateOptions simulateOptions_PartO);

                simulateOptions = Create.SimulateOptions_PartO(analyticalModel, uIAnalyticalModel.Path, simulateOptions_PartO);
            }
            else
            {
                ActiveSetting.Setting.TryGetValue(AnalyticalSettingParameter.SimulateOptions, out simulateOptions);
                if (simulateOptions == null)
                {
                    simulateOptions = UI.Create.SimulateOptions(uIAnalyticalModel);
                }
            }

            simulateWindow.ProjectName = analyticalModel.Name;

            if (!string.IsNullOrWhiteSpace(uIAnalyticalModel.Path))
            {
                simulateWindow.OutputDirectory = System.IO.Path.GetDirectoryName(uIAnalyticalModel.Path);
            }

            simulateWindow.SimulateOptions = simulateOptions;

            if (analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData))
            {
                simulateWindow.WeatherData = weatherData;
            }

            // AFTER the options are set, and after the weather: the Simulate setter re-enables the sizing
            // and full-year boxes as a group, so locking before this would be undone by it.
            if (partO)
            {
                simulateWindow.LockPartOSettings();
            }

            bool? showdialog = simulateWindow.ShowDialog();
            if(showdialog == null || !showdialog.HasValue || !showdialog.Value)
            {
                return;
            }

            ActiveSetting.Setting.SetValue(partO ? AnalyticalSettingParameter.SimulateOptions_PartO : AnalyticalSettingParameter.SimulateOptions, simulateWindow.SimulateOptions);

            SimulateInputs simulateInputs = new()
            {
                ProjectName = simulateWindow.ProjectName,
                OutputDirectory = simulateWindow.OutputDirectory,
                UnmetHours = simulateWindow.UnmetHours,
                PrintRoomDataSheets = simulateWindow.RoomDataSheets,
                FullYearSimulation = simulateWindow.FullYearSimulation,
                FullYearSimulation_From = simulateWindow.FullYearSimulation_From,
                FullYearSimulation_To = simulateWindow.FullYearSimulation_To,
                CreateSAP = simulateWindow.CreateSAP,
                CreateTM59 = simulateWindow.CreateTM59,
                CreateTPD = simulateWindow.CreateTPD,
                CreatePartL = simulateWindow.CreatePartL,
                Sizing = simulateWindow.Sizing,
                UseWidths = simulateWindow.UseWidths,
                SolarCalculationMethod = simulateWindow.SolarCalculationMethod,
                UpdateConstructionLayersByPanelType = simulateWindow.UpdateConstructionLayersByPanelType,
                TextMap = simulateWindow.SelectedTextMap,
                WeatherData = simulateWindow.SelectedWeatherData,
                ZoneCategory = simulateWindow.SelectedZoneCategory,
                Simulate = simulateWindow.Simulate,

                //Only the guided Part O route writes into a case folder; the expert command writes where it is told.
                PartOOutputCase = partO ? PartOOutputPaths.CaseOf(partORun.PreparationContext) : null,
            };

            //Only the Part O case hands the run in. The ordinary command simulates the open model and never
            //completes, stamps or persists a Part O run.
            Simulate(uIAnalyticalModel, partO ? partORun : null, analyticalModel_Source, simulateInputs, false, null);
        }

        /// <summary>
        /// The guided Approved Document O simulation, with no dialog: the Part O Hub's Simulation case supplies
        /// the three inputs a person owns - weather, output folder and solar method - and everything else is the
        /// locked Part O case <see cref="Create.SimulateOptions_PartO"/> has always put in the Simulate dialog.
        ///
        /// <para><b>The same run as the dialog path, by construction</b></para>
        /// <para>
        /// The inputs are composed from <c>SimulateOptions_PartO</c> exactly as the locked dialog would hand
        /// them back - Simulate and Full Year on, days 1 to 365 (the locked range the dialog's disabled boxes
        /// hold), sizing, unmet hours, widths and every export off, the project name derived from the prepared
        /// model - and then go through the one core below that the dialog path also uses. No engineering input
        /// differs; only where three of them were typed.
        /// </para>
        /// <para>
        /// <b>No success dialog.</b> A clean completion is returned as <see cref="PartOSimulationOutcome"/> and
        /// shown inline by the Hub. A refusal, a run that could not be completed and any zone-identity notes are
        /// returned too, and the caller shows those - they are things a person has to read.
        /// </para>
        /// </summary>
        /// <param name="uIAnalyticalModel">
        /// The open design model. It is never simulated and never replaced: the case simulated is the run's own
        /// prepared model, and its result stays with the run (PR-4). Only its path is read, for the defaults.
        /// </param>
        /// <param name="partORun">The session's run; must be <see cref="PartORunState.Prepared"/>.</param>
        /// <param name="partOSimulationCase">The Hub's Simulation case.</param>
        public static PartOSimulationOutcome SimulatePartO(this UIAnalyticalModel uIAnalyticalModel, PartORun partORun, PartOSimulationCase partOSimulationCase)
        {
            return SimulatePartO(uIAnalyticalModel, partORun, partOSimulationCase, null);
        }

        /// <summary>
        /// <see cref="SimulatePartO(UIAnalyticalModel, PartORun, PartOSimulationCase)"/> with the TAS workflow step
        /// supplied - the one seam <see cref="RunPartOSimulation"/> already has, for a test. Null is TAS.
        /// </summary>
        internal static PartOSimulationOutcome SimulatePartO(UIAnalyticalModel uIAnalyticalModel, PartORun partORun, PartOSimulationCase partOSimulationCase, PartOWorkflowRunner partOWorkflowRunner)
        {
            AnalyticalModel analyticalModel = partORun?.State == PartORunState.Prepared ? partORun.AnalyticalModel_Prepared : null;

            if (uIAnalyticalModel is null || analyticalModel is null || partORun is null || partORun.State != PartORunState.Prepared)
            {
                return new PartOSimulationOutcome { Refusal = "The Part O iteration is not prepared, so nothing was simulated. Prepare the iteration again." };
            }

            string refusal_Case = partOSimulationCase?.Refusal();
            if (refusal_Case is not null)
            {
                return new PartOSimulationOutcome { Refusal = refusal_Case };
            }

            ActiveSetting.Setting.TryGetValue(AnalyticalSettingParameter.SimulateOptions_PartO, out SimulateOptions simulateOptions_Remembered);

            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(analyticalModel, uIAnalyticalModel.Path, simulateOptions_Remembered);

            //The three inputs the dialog left open, now typed in the Hub instead.
            simulateOptions.WeatherData = new WeatherData(partOSimulationCase.WeatherData);
            simulateOptions.OutputDirectory = partOSimulationCase.OutputDirectory;
            simulateOptions.SolarCalculationMethod = partOSimulationCase.SolarCalculationMethod;

            //Remembered under the Part O key, exactly as the dialog's OK remembered it.
            ActiveSetting.Setting.SetValue(AnalyticalSettingParameter.SimulateOptions_PartO, simulateOptions);

            SimulateInputs simulateInputs = new()
            {
                ProjectName = simulateOptions.ProjectName,
                OutputDirectory = simulateOptions.OutputDirectory,
                UnmetHours = simulateOptions.UnmetHours,
                PrintRoomDataSheets = simulateOptions.RoomDataSheets,
                FullYearSimulation = simulateOptions.FullYearSimulation,

                //The locked range: the dialog's From/To boxes are disabled on the Part O route and hold 1 and
                //365, which is what it handed back.
                FullYearSimulation_From = 1,
                FullYearSimulation_To = 365,

                CreateSAP = simulateOptions.CreateSAP,
                CreateTM59 = simulateOptions.CreateTM59,
                CreateTPD = simulateOptions.CreateTPD,
                CreatePartL = simulateOptions.CreatePartL,
                Sizing = simulateOptions.Sizing,
                UseWidths = simulateOptions.UseWidths,
                SolarCalculationMethod = simulateOptions.SolarCalculationMethod,
                UpdateConstructionLayersByPanelType = simulateOptions.UpdateConstructionLayersByPanelType,
                TextMap = null,
                WeatherData = simulateOptions.WeatherData,
                ZoneCategory = null,
                Simulate = simulateOptions.Simulate,
                PartOOutputCase = PartOOutputPaths.CaseOf(partORun.PreparationContext),
            };

            return Simulate(uIAnalyticalModel, partORun, analyticalModel, simulateInputs, true, partOWorkflowRunner);
        }

        /// <summary>What the Simulate dialog hands back, as values - so the dialog path and the Hub path share one core.</summary>
        private sealed class SimulateInputs
        {
            public string ProjectName;
            public string OutputDirectory;
            public bool UnmetHours;
            public bool PrintRoomDataSheets;
            public bool FullYearSimulation;
            public int FullYearSimulation_From;
            public int FullYearSimulation_To;
            public bool CreateSAP;
            public bool CreateTM59;
            public bool CreateTPD;
            public bool CreatePartL;
            public bool Sizing;
            public bool UseWidths;
            public SolarCalculationMethod SolarCalculationMethod;
            public bool UpdateConstructionLayersByPanelType;
            public TextMap TextMap;
            public WeatherData WeatherData;
            public string ZoneCategory;
            public bool Simulate;

            /// <summary>
            /// The Part O case this run is, where it is the guided Part O route - and then
            /// <see cref="OutputDirectory"/> is the Part O ROOT and the run writes into the case's own folder beneath
            /// it (<see cref="PartOOutputPaths"/>). Null for the expert command, or where the case cannot be said,
            /// which writes into <see cref="OutputDirectory"/> itself as before.
            /// </summary>
            public PartOOutputCase? PartOOutputCase;
        }

        /// <summary>
        /// The one simulation core. <paramref name="quiet"/> is the Hub path: nothing is shown here, and what
        /// the dialog path shows as message boxes is returned instead.
        /// <para>
        /// <b>Two cases, told apart by <paramref name="partORun"/>.</b> With a run, this is the Part O case: it
        /// simulates the run's prepared model, completes the run, and <b>never replaces the open model</b> - the
        /// window stays on the design model and the result is reached through the run (PR-4, the approved
        /// model-state architecture). Without one, it is the ordinary simulation of the open model, whose result
        /// is adopted into the window exactly as before.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel_Source">What is simulated: the run's prepared model, or the open model.</param>
        /// <param name="partOWorkflowRunner">The TAS workflow step; null is TAS. See <see cref="RunPartOSimulation"/>.</param>
        private static PartOSimulationOutcome Simulate(UIAnalyticalModel uIAnalyticalModel, PartORun partORun, AnalyticalModel analyticalModel_Source, SimulateInputs simulateInputs, bool quiet, PartOWorkflowRunner partOWorkflowRunner)
        {
            PartOSimulationOutcome partOSimulationOutcome = new();

            AnalyticalModel analyticalModel = analyticalModel_Source;
            if (analyticalModel == null || uIAnalyticalModel == null)
            {
                return partOSimulationOutcome;
            }

            string projectName = simulateInputs.ProjectName;
            string outputDirectory = simulateInputs.OutputDirectory;

            //Approved Document O: the folder a person chose is the Part O root, exactly as chosen, and this case's
            //TAS work goes into its own tas folder beneath it, so 1a, 1b and 2 - which share file names - cannot
            //overwrite each other. Resolved here, not in the options, so the remembered folder stays the root.
            if (simulateInputs.Simulate && simulateInputs.PartOOutputCase is PartOOutputCase partOOutputCase)
            {
                PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(outputDirectory, partOOutputCase);
                if (partOOutputPaths is not null)
                {
                    string refusal_Directories = partOOutputPaths.TryCreateDirectories();
                    if (refusal_Directories is not null)
                    {
                        partOSimulationOutcome.Ran = true;
                        partOSimulationOutcome.Refusal = refusal_Directories;

                        if (!quiet)
                        {
                            MessageBox.Show(refusal_Directories);
                        }

                        return partOSimulationOutcome;
                    }

                    outputDirectory = partOOutputPaths.Directory_Tas;
                }
            }

            bool unmetHours = simulateInputs.UnmetHours;
            bool printRoomDataSheets = simulateInputs.PrintRoomDataSheets;

            bool fullYearSimulation = simulateInputs.FullYearSimulation;
            int fullYearSimulation_From = simulateInputs.FullYearSimulation_From;
            int fullYearSimulation_To = simulateInputs.FullYearSimulation_To;

            bool createSAP = simulateInputs.CreateSAP;
            bool createTM59 = simulateInputs.CreateTM59;
            bool createTPD = simulateInputs.CreateTPD;
            bool createPartL = simulateInputs.CreatePartL;

            bool sizing = simulateInputs.Sizing;

            bool useWidths = simulateInputs.UseWidths;

            SolarCalculationMethod solarCalculationMethod = simulateInputs.SolarCalculationMethod;
            bool updateConstructionLayersByPanelType = simulateInputs.UpdateConstructionLayersByPanelType;

            TextMap textMap = simulateInputs.TextMap;
            WeatherData weatherData = simulateInputs.WeatherData;
            string zoneCategory = simulateInputs.ZoneCategory;

            if (!simulateInputs.Simulate && !createSAP && !createTM59)
            {
                return partOSimulationOutcome;
            }

            partOSimulationOutcome.Ran = true;

            // The model is renamed here, and a cancelled or failed run must not leave the instance the user
            // still has open renamed behind its back. SHALLOW is enough for that: Name is a field of
            // AnalyticalModel itself, so a new wrapper isolates it, and the copy constructor carries the
            // Guid over so identity does not change.
            //
            // Deliberately NOT deep here, and that is the point of the seam. The in-place writes that need
            // real ownership - the materials, the construction layers, and every TAS identity stamp - all
            // happen inside RunPartOSimulation, which takes the deep working copy itself and hands the
            // result back for this method to adopt. Cloning here as well meant the normal Part O run cloned
            // the same model three times over (here, there, and again inside WorkflowCalculator) to
            // establish one guarantee, and then threw this one away on adoption.
            //
            // The one path where THIS method mutates a model of its own is the export block below, which
            // converts in place when the workflow did not run or did not produce a model. It takes its own
            // deep copy at that point - see analyticalModel_Owned - so the guarantee is unchanged and is
            // paid for exactly once, on whichever path actually needs it.
            analyticalModel = new AnalyticalModel(analyticalModel)
            {
                Name = projectName,
            };

            //PR-5: a Part O case says what it was derived from. The open model is the DESIGN (PR-4 keeps the window on it, and a
            //result opened by hand is refused before a case starts), and by now any input the engineer confirmed has been
            //written to it, so its state here is the state this case is derived from. Stamped on the working copy
            //only - never on the design, which is what marks a model as a result - and before RunPartOSimulation
            //takes the provenance record, which fingerprints it.
            if (simulateInputs.PartOOutputCase is PartOOutputCase partOOutputCase_Derived
                && UI.Query.PartODerivedCaseOf(partOOutputCase_Derived) is PartODerivedCase partODerivedCase
                && uIAnalyticalModel.JSAMObject is AnalyticalModel analyticalModel_Design
                && !analyticalModel_Design.HasValue(Analytical.AnalyticalModelParameter.PartOBaselineReference)
                && !analyticalModel_Design.HasValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance))
            {
                analyticalModel.StampPartOBaselineReference(Analytical.Create.PartOBaselineReferenceFromDesign(partODerivedCase, analyticalModel_Design, uIAnalyticalModel.Path, outputDirectory));
            }

            // Whether `analyticalModel` is a working copy this method may mutate freely. False while it is
            // the shallow copy taken above, which still shares its spaces, panels and apertures with the
            // model the user has open; true once a deep copy has been adopted or taken. See
            // AnalyticalModel(AnalyticalModel, bool) for the ownership rule this tracks.
            bool analyticalModel_Owned = false;

            // Everything the TAS case needs, in one object - so this run and any Iteration 2B optimisation
            // that later repeats it are provably the same case. See PartOSimulationContext.
            PartOSimulationContext partOSimulationContext = new(outputDirectory, projectName, weatherData, solarCalculationMethod, fullYearSimulation ? fullYearSimulation_From : -1, fullYearSimulation ? fullYearSimulation_To : -1)
            {
                UnmetHours = unmetHours,
                Sizing = sizing,
                UseWidths = useWidths,
                UpdateConstructionLayersByPanelType = updateConstructionLayersByPanelType,
            };

            DateTime dateTime = DateTime.Now;

            string path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");

            bool converted = false;

            bool cancelled = false;

            // Whether WorkflowCalculator actually ran and returned a model. Necessary for a Part O run to be
            // completed, and on its own NOT sufficient - see workflowSimulatedFullYear.
            bool workflowCompleted = false;

            // Whether the workflow that ran was the FULL-YEAR simulation a TM59 assessment reads: days 1 to
            // 365, taken from the settings actually handed to WorkflowCalculator rather than from the tick box.
            // A workflow can return a model without producing an annual series at all - Full Year Simulation
            // unticked, a partial date range, a one-day run forced because shading changed, or sizing only -
            // and a Part O assessment of any of those reports criteria and verdicts computed over an
            // incomplete series. Only this promotes a prepared run to WorkflowCompleted. Nothing else in this
            // method reads it, so a normal non-Part-O simulation is unaffected.
            bool workflowSimulatedFullYear = false;

            // Reported at the end rather than raised as they happen: a space left without a TAS zone identity
            // is a gap in whatever is exported next - and the DomOv XML does NOT refuse such a space, it
            // writes the SAM space guid in place of the TAS zone guid, so nothing downstream announces the
            // gap. A run that says "converted" while quietly having produced that is the failure mode being
            // closed. A dialog per space would be unusable on a block of flats.
            List<string> notes_Simulate = [];

            if(simulateInputs.Simulate)
            {
                // The whole path to a TSD - materials, construction layers, the TBD, the solar calculation,
                // the zones, the shading, the workflow - and the Part O arming that goes before it. Extracted
                // so an Iteration 2B optimisation can repeat this exact case over a changed design without a
                // second copy of it existing to drift. See Modify.RunPartOSimulation.
                AnalyticalModel analyticalModel_Workflow = Modify.RunPartOSimulation(analyticalModel, partOSimulationContext, projectName, partORun, CancellationToken.None, out path_TBD, out string _, out cancelled, out workflowSimulatedFullYear, out List<string> notes_Simulation, out string refusal_Simulation, null, partOWorkflowRunner);

                notes_Simulate.AddRange(notes_Simulation);

                if (refusal_Simulation != null)
                {
                    // The run never started, so there is no TBD for the exports below to convert or read
                    // back. Aborted outright, exactly as this method has always aborted on these two
                    // conditions - a gbXML that could not be written, and a TBD that could not be replaced.
                    partOSimulationOutcome.Refusal = refusal_Simulation;

                    if (!quiet)
                    {
                        MessageBox.Show(refusal_Simulation);
                    }

                    return partOSimulationOutcome;
                }

                if (!cancelled && analyticalModel_Workflow != null)
                {
                    // The one place a Part O run may be completed from. Set here rather than inferred later
                    // from `converted`, which is also true when the .tbd was written without the workflow
                    // ever running - a model that never went through WorkflowCalculator does not carry the
                    // current TAS zone identities, so it is not a workflow output and must not be assessed
                    // as one.
                    workflowCompleted = true;

                    // Adopted - and with it, its ownership. RunPartOSimulation took the deep working copy
                    // and nothing else holds what it hands back, so the export block below may mutate this
                    // model in place without taking another.
                    analyticalModel = analyticalModel_Workflow;
                    analyticalModel_Owned = true;

                    if (printRoomDataSheets)
                    {
                        if (!System.IO.Directory.Exists(outputDirectory))
                        {
                            System.IO.Directory.CreateDirectory(outputDirectory);
                        }

                        UI.Modify.PrintRoomDataSheets(analyticalModel, outputDirectory);
                    }

                    converted = true;
                }

                // A run that produced no model leaves `analyticalModel` as the copy taken above - not as a
                // half-processed one, because RunPartOSimulation worked on a copy of its own. `converted`
                // stays false, so the export path below still converts it itself, exactly as before.
            }


            // Skipped outright once cancelled: these read back the .tbd the cancelled run was part-way through
            // writing, and a partial file converts into a model that looks complete but is not.
            if(!cancelled && (createSAP || createTM59 || createPartL))
            {
                // Ownership, taken HERE and only where it is needed. This block mutates the model in place:
                // Tas.Convert.ToTBD is called with updateGuids, and Modify.RestampSimulationZoneIdentity
                // writes SpaceParameter.ZoneGuid straight onto the live spaces (its own summary says the
                // caller must put the returned spaces back). Against the shallow copy taken at the top of
                // this method those writes would reach the model the user still has open.
                //
                // On the ordinary path this costs nothing: the workflow ran, its model was adopted above,
                // and it is already owned. The copy is taken only where the workflow was not run at all -
                // an export-only run - or ran and produced nothing, which are exactly the cases where no
                // deep copy has been made for this model yet.
                if (!analyticalModel_Owned && analyticalModel != null)
                {
                    analyticalModel = new AnalyticalModel(analyticalModel, true);
                    analyticalModel_Owned = true;
                }

                using (ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager("Convert to TBD", "Converting..."))
                {
                    if (!converted)
                    {
                        converted = Tas.Convert.ToTBD(analyticalModel, path_TBD, null, null, null, true);
                    }

                    if (converted)
                    {
                        AnalyticalModel analyticalModel_TBD = Tas.Convert.ToSAM(path_TBD, false, false);
                        if(analyticalModel != null && analyticalModel_TBD != null)
                        {
                            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

                            // Makes every space's TAS zone identity describe the .tbd about to be exported.
                            // This used to match by NAME for every space and overwrite unconditionally, which
                            // discarded the strong identity Modify.UpdateIds had just written and collapsed
                            // same-named rooms in different dwellings onto one zone - see
                            // Modify.RestampSimulationZoneIdentity for both defects, and for why this seam is
                            // needed by the Simulate-unticked DomOv path (only that one: SAP is handed
                            // analyticalModel_TBD, which Tas.Convert.ToSAM stamps itself, and Part L reads no
                            // ZoneGuid).
                            //
                            // workflowCompleted is the whole discriminator, and it is the authoritative one.
                            // TRUE: WorkflowCalculator wrote path_TBD and stamped this model against it, so the
                            // stamps already here name zones in the file being re-read - authoritative, current,
                            // left alone. FALSE: Tas.Convert.ToTBD wrote path_TBD above, deleting whatever was
                            // there and minting new zone guids, so any stamp this model carries belongs to an
                            // earlier .tbd and names nothing in this one - it is replaced from an unambiguous
                            // name match and discarded where there is none. Passing the wrong value here
                            // exports either a stale identity or a weak one.
                            List<Space> spaces_Restamped = Modify.RestampSimulationZoneIdentity(adjacencyCluster.GetSpaces(), analyticalModel_TBD.AdjacencyCluster?.GetSpaces(), workflowCompleted, out List<string> notes_ZoneIdentity);

                            foreach (Space space_Restamped in spaces_Restamped)
                            {
                                adjacencyCluster.AddObject(space_Restamped);
                            }

                            notes_Simulate.AddRange(notes_ZoneIdentity);

                            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
                        }

                        if (createTM59)
                        {
                            if (Tas.TM59.Modify.TryCreatePath(path_TBD, out string path_TM59))
                            {
                                Tas.TM59.Convert.ToXml(analyticalModel, path_TM59, new TM59Manager(textMap));
                            }
                        }

                        if (createSAP)
                        {
                            if (string.IsNullOrWhiteSpace(zoneCategory))
                            {
                                converted = false;
                            }
                            else
                            {
                                if (!Tas.SAP.Modify.TryCreatePath(path_TBD, out string path_SAP))
                                {
                                    converted = false;
                                }
                                else
                                {
                                    converted = Tas.SAP.Convert.ToFile(analyticalModel_TBD, path_SAP, zoneCategory, textMap);
                                }

                            }
                        }

                        if(createPartL)
                        {
                            converted = Tas.Create.TBD_ByPartL(analyticalModel, path_TBD, out string path_TBD_Destination);
                        }
                    }
                }
            }

            if(!cancelled && createTPD)
            {
                string directory = System.IO.Path.GetDirectoryName(path_TBD);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD);

                string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));
                if(System.IO.File.Exists(path_TSD))
                {
                    string path_TPD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tpd"));

                    Tas.Create.TPD(path_TPD, path_TSD, analyticalModel);
                }
            }

            TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - dateTime.Ticks);

            // Whether this run may complete a pending Part O run.
            bool completePartORun = partORun != null
                && partORun.State == PartORunState.Prepared
                && workflowCompleted
                && workflowSimulatedFullYear
                && !cancelled
                && analyticalModel != null;

            // A prepared run this simulation cannot complete is dropped HERE, with the reason it was actually
            // refused for: what the user needs to be told is that the simulation was not the full year a TM59
            // assessment reads. Nothing is dropped where the run was cancelled or produced no model, since the
            // preparation then still describes the design and can be simulated again.
            string? note_PartORun = null;
            if (partORun != null && partORun.State == PartORunState.Prepared && !completePartORun && !cancelled && analyticalModel != null)
            {
                note_PartORun = workflowCompleted
                    ? "The Part O run was not completed: the simulation that ran was not a full-year simulation, and a TM59 assessment reads a full annual hourly series. Prepare the iteration again and simulate with Full Year Simulation ticked over days 1 to 365."
                    : "The Part O run was not completed: the TAS workflow did not run over the prepared model, so there are no results to assess. Prepare the iteration again and run the energy simulation.";

                partORun.Invalidate(note_PartORun);
            }

            string message;
            if (cancelled)
            {
                message = "Simulation cancelled.";
            }
            else
            {
                message = converted ? "Model successfuly converted." : "Model could not be converted.";
            }

            message += string.Format("\n Time elapsed: {0}min{1}sec", timeSpan.Minutes, timeSpan.Seconds);

            if (notes_Simulate.Count != 0)
            {
                // Capped, with the remainder counted rather than dropped silently. A model with many unzoned
                // or unexported spaces can produce one note each, and a dialog listing all of them is one
                // nobody reads - but a cap that hid how much it was hiding would be worse than either.
                const int count_NotesShown = 5;

                message += string.Format("\n\n{0}", string.Join("\n", notes_Simulate.GetRange(0, Math.Min(count_NotesShown, notes_Simulate.Count))));

                if (notes_Simulate.Count > count_NotesShown)
                {
                    message += string.Format("\n... and {0} more space(s) with no TAS zone identity.", notes_Simulate.Count - count_NotesShown);
                }
            }

            // Appended separately rather than added to notes_Simulate, whose cap counts the remainder as
            // "space(s) with no TAS zone identity" - which this is not.
            if (note_PartORun != null)
            {
                message += string.Format("\n\n{0}", note_PartORun);
            }

            partOSimulationOutcome.Cancelled = cancelled;
            partOSimulationOutcome.Elapsed = timeSpan;
            partOSimulationOutcome.Message = message;
            partOSimulationOutcome.Notes.AddRange(notes_Simulate);
            partOSimulationOutcome.Note_PartORun = note_PartORun;

            //The dialog path's closing message, unchanged. The Hub path shows a clean completion inline and
            //reads the notes and the not-completed reason off the outcome, so a person is still told
            //everything this box said - without an OK to click on success.
            if (!quiet)
            {
                MessageBox.Show(message);
            }

            // The Part O case NEVER replaces the open model: the window stays on the design model, and the result is
            // the run's (PR-4). A normal Save therefore cannot write Part O output over the design file.
            //
            // The ordinary simulation adopts its result as it always has. A cancelled run leaves analyticalModel
            // null, and pushing that back would replace the model the user still has open with nothing. Not
            // adopting it really does leave the loaded model untouched, because everything above this point
            // worked on the copy taken before the first mutation.
            if (partORun is null && !cancelled && analyticalModel != null)
            {
                uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
            }

            if (completePartORun)
            {
                // The model handed over is the one this workflow produced - not the preparation output, and
                // not the open design model.
                // The TSD is required to exist: the file name is derived, and a derived name is a guess until
                // the file behind it is there. A sizing-only run writes none, and is correctly not completable.
                string path_TSD = System.IO.Path.ChangeExtension(path_TBD, "tsd");

                // WITH the case that just ran. Completing without it leaves PartORun.SimulationContext
                // null, which Modify.CanOptimise refuses - so every baseline produced through this window
                // would be rejected by the Optimise command with no way for a user to tell why.
                if (!partORun.Complete(analyticalModel, path_TSD, partOSimulationContext, out string refusal_PartORun))
                {
                    string refusal_Complete = string.Format("The Part O run was not completed, so its TM59 assessment is not available.\n\n{0}", refusal_PartORun);

                    partOSimulationOutcome.Refusal = refusal_Complete;

                    if (!quiet)
                    {
                        MessageBox.Show(refusal_Complete);
                    }
                }
            }

            partOSimulationOutcome.Completed = partORun is not null && partORun.State == PartORunState.WorkflowCompleted && completePartORun;

            return partOSimulationOutcome;
        }

        public static AnalyticalModel Simulate(this AnalyticalModel analyticalModel, string path, IWin32Window owner = null)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (!analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData))
            {
                weatherData = null;
            }

            string projectName = null;
            string outputDirectory = null;
            bool unmetHours = false;
            bool printRoomDataSheets = false;

            bool fullYearSimulation = false;
            int fullYearSimulation_From = -1;
            int fullYearSimulation_To = -1;
            bool sizing = true;

            SolarCalculationMethod solarCalculationMethod = SolarCalculationMethod.None;
            bool updateConstructionLayersByPanelType = false;

            SimulateWindow simulateWindow_Path = new SimulateWindow();
            simulateWindow_Path.ProjectName = System.IO.Path.GetFileNameWithoutExtension(path);
            simulateWindow_Path.OutputDirectory = System.IO.Path.GetDirectoryName(path);
            simulateWindow_Path.WeatherData = weatherData;
            // Mirror the WinForms SimulateForm default that this overload replaced:
            // sizing is on by default for the path-based simulation flow.
            simulateWindow_Path.Sizing = true;
            simulateWindow_Path.UpdateConstructionLayersByPanelType = true;

            if (owner != null)
            {
                new System.Windows.Interop.WindowInteropHelper(simulateWindow_Path).Owner = owner.Handle;
            }

            if (simulateWindow_Path.ShowDialog() != true)
            {
                return null;
            }

            projectName = simulateWindow_Path.ProjectName;
            outputDirectory = simulateWindow_Path.OutputDirectory;
            unmetHours = simulateWindow_Path.UnmetHours;
            sizing = simulateWindow_Path.Sizing;
            weatherData = simulateWindow_Path.SelectedWeatherData;
            solarCalculationMethod = simulateWindow_Path.SolarCalculationMethod;
            updateConstructionLayersByPanelType = simulateWindow_Path.UpdateConstructionLayersByPanelType;
            printRoomDataSheets = simulateWindow_Path.RoomDataSheets;
            fullYearSimulation = simulateWindow_Path.FullYearSimulation;
            if (fullYearSimulation)
            {
                fullYearSimulation_From = simulateWindow_Path.FullYearSimulation_From;
                fullYearSimulation_To = simulateWindow_Path.FullYearSimulation_To;
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(outputDirectory) || !System.IO.Directory.Exists(outputDirectory))
            {
                return null;
            }

            if (weatherData == null)
            {
                return null;
            }

            string path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");

            // THE ownership boundary of this overload, and its only deep copy. "Update Materials" writes
            // into the model, Tas.Convert.ToTBD below converts it in place with updateGuids, and this method
            // signals cancellation by returning null - so mutating the caller's instance and then handing
            // back null would change it while telling the caller nothing came of the run. The copy keeps the
            // Guid.
            //
            // Deep because those writes are in-place mutations of the shared spaces and panels rather than
            // same-guid replacements. Unlike the UIAnalyticalModel overload, this method converts the model
            // ITSELF before running the workflow, so the copy cannot be deferred to RunPartOSimulation -
            // there is no call to it on this path. The workflow is told the model is already owned, so the
            // copy is still taken exactly once.
            analyticalModel = new AnalyticalModel(analyticalModel, true);

            bool shadingUpdated = false;

            bool cancelled = false;

            // One token spans the preparation steps below and the workflow that follows them, so a single
            // Cancel click aborts whichever of the two is running.
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                // Hosted off this thread rather than shown on it: "Converting to TBD" and "Updating Shading"
                // below are single COM calls that run for minutes, and Windows ghosts a window whose thread
                // has stopped pumping and then discards clicks on the ghost - so a Cancel button on this
                // thread would silently lose the click. Not a using either: see Modify.RunWorkflow for why the
                // host must be disposed before the final token check.
                ProgressWindowHost progressWindowHost = new ProgressWindowHost("Preparing Model", 8, true, Analytical.Tas.Query.CancelNote(null));

                // Announce the stage, then observe - the order WorkflowCalculator.Step uses, so a click that
                // lands while the note is being updated is still seen before the stage starts work.
                Action<string> step = description =>
                {
                    progressWindowHost.Note = Analytical.Tas.Query.CancelNote(description);
                    progressWindowHost.Update(description);
                    cancellationToken.ThrowIfCancellationRequested();
                };

                try
                {
                    progressWindowHost.CancelRequested += (s, e) => cancellationTokenSource.Cancel();

                    step("Update Materials");

                    IEnumerable<Core.IMaterial> materials = Analytical.Query.Materials(analyticalModel.AdjacencyCluster, Analytical.Query.DefaultMaterialLibrary());
                    if (materials != null)
                    {
                        foreach (Core.IMaterial material in materials)
                        {
                            if (analyticalModel.HasMaterial(material))
                            {
                                continue;
                            }

                            analyticalModel.AddMaterial(material);
                        }
                    }

                    step("Update ConstructionLayers By PanelTypes");

                    analyticalModel = updateConstructionLayersByPanelType ? analyticalModel.UpdateConstructionLayersByPanelType() : analyticalModel;

                    if (System.IO.File.Exists(path_TBD))
                    {
                        System.IO.File.Delete(path_TBD);
                    }

                    List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                    step("Solar Calculations");
                    if (solarCalculationMethod != SolarCalculationMethod.None)
                    {
                        SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), false, Core.Tolerance.MacroDistance, Core.Tolerance.MacroDistance, 0.012, Core.Tolerance.Distance);
                    }

                    using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                    {
                        TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                        step("Updating WeatherData");
                        Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, analyticalModel == null ? 0 : analyticalModel.AdjacencyCluster.BuildingHeight());

                        TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                        List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
                        if (dayTypes.Find(x => x.name == "HDD") == null)
                        {
                            TBD.dayType dayType = calendar.AddDayType();
                            dayType.name = "HDD";
                        }

                        if (dayTypes.Find(x => x.name == "CDD") == null)
                        {
                            TBD.dayType dayType = calendar.AddDayType();
                            dayType.name = "CDD";
                        }

                        step("Converting to TBD");
                        Tas.Convert.ToTBD(analyticalModel, tBDDocument, true);

                        step("Updating Zones");
                        Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                        step("Updating Shading");
                        shadingUpdated = Tas.Modify.UpdateShading(tBDDocument, analyticalModel);

                        sAMTBDDocument.Save();
                    }

                    step("Printing Room Data Sheets");
                    if (printRoomDataSheets && analyticalModel != null)
                    {
                        if (!System.IO.Directory.Exists(outputDirectory))
                        {
                            System.IO.Directory.CreateDirectory(outputDirectory);
                        }

                        UI.Modify.PrintRoomDataSheets(analyticalModel, outputDirectory);
                    }
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                finally
                {
                    progressWindowHost.Dispose();
                }

                // The host is down, so no further click can arrive and any in-flight one has already run:
                // this observation is final - but only once the host confirms it actually shut down. If it
                // could not, its thread is still live and may be sitting on a click nothing observed, so
                // preparation cannot be reported as having completed uninterrupted.
                if (!cancelled && (cancellationTokenSource.IsCancellationRequested || !progressWindowHost.ShutdownCompleted))
                {
                    cancelled = true;
                }

                if (cancelled)
                {
                    return null;
                }

                // Still inside the token source's scope: the workflow shares the token with the preparation
                // above, so one Cancel click covers both halves of the run.
                List<DesignDay> heatingDesignDays = new List<DesignDay>() { Analytical.Query.HeatingDesignDay(weatherData) };
                List<DesignDay> coolingDesignDays = new List<DesignDay>() { Analytical.Query.CoolingDesignDay(weatherData) };

                SurfaceOutputSpec surfaceOutputSpec = new SurfaceOutputSpec("Tas.Simulate")
                {
                    SolarGain = true,
                    Conduction = true,
                    ApertureData = false,
                    Condensation = false,
                    Convection = false,
                    LongWave = false,
                    Temperature = false
                };

                List<SurfaceOutputSpec> surfaceOutputSpecs = new List<SurfaceOutputSpec>() { surfaceOutputSpec };

                int simulate_From = -1;
                int simulate_To = -1;

                bool simulate = fullYearSimulation;

                if (simulate)
                {
                    simulate_From = fullYearSimulation_From;
                    simulate_To = fullYearSimulation_To;
                }

                if (shadingUpdated)
                {
                    if (!simulate)
                    {
                        simulate_From = 1;
                        simulate_To = 1;
                        simulate = true;
                    }
                }

                WorkflowSettings workflowSettings = new WorkflowSettings()
                {
                    Path_TBD = path_TBD,
                    Path_gbXML = null,
                    WeatherData = null,
                    DesignDays_Heating = heatingDesignDays,
                    DesignDays_Cooling = coolingDesignDays,
                    SurfaceOutputSpecs = surfaceOutputSpecs,
                    UnmetHours = unmetHours,
                    Simulate = simulate,
                    Sizing = sizing,
                    UpdateZones = false,
                    UseWidths = false,
                    SimulateFrom = simulate_From,
                    SimulateTo = simulate_To
                };

                // OWNED: the deep copy taken at the top of this method, which everything since has worked
                // on. Without saying so the workflow took a second one for the same guarantee.
                analyticalModel = Modify.RunWorkflow(analyticalModel, workflowSettings, cancellationToken, out cancelled, true);

                // A cancelled workflow returns null; returning null rather than a half-populated model is what
                // tells the caller nothing usable came back.
                if (cancelled || analyticalModel == null)
                {
                    return null;
                }

                analyticalModel.SetValue(Analytical.AnalyticalModelParameter.WeatherData, weatherData);

                return analyticalModel;
            }
        }
    }
}
