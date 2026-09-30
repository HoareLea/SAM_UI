// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Tas;
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
        /// <summary>
        /// Runs <b>one</b> stated TAS case over one model, start to finish, and hands back the model the
        /// workflow returned and the results it wrote.
        ///
        /// <para><b>Why this is a method and not a second copy of <see cref="Simulate(UIAnalyticalModel, PartORun, bool)"/></b></para>
        /// <para>
        /// An Iteration 2B optimisation runs the same thermal case ten times over ten designs. It cannot ask
        /// a person for the settings ten times, and it must not run a case that differs in any respect from
        /// the one the baseline was produced by - a change in weather, day range or solar method would leave
        /// the movement in TM59 results unattributable to the airflow change that was made. Writing the
        /// pipeline out again in the optimiser would be exactly the way those two drift apart, so
        /// <c>Simulate</c> collects the settings into a <see cref="PartOSimulationContext"/> and then calls
        /// this, and the optimiser calls this with the context the completed run recorded.
        /// </para>
        ///
        /// <para><b>What it does, and what it deliberately does not</b></para>
        /// <para>
        /// Materials, construction layers, the TBD, the solar calculation, the zones, the shading and the
        /// workflow - the whole path to a TSD. It does <b>not</b> print room data sheets or write the SAP,
        /// Part L, domestic-overheating or TPD exports: those are deliverables of a run somebody asked for,
        /// not part of the thermal case, and producing a dozen copies of each during an optimisation would
        /// be noise. <see cref="Simulate(UIAnalyticalModel, PartORun, bool)"/> still does all of them, after this
        /// returns.
        /// </para>
        ///
        /// <para><b>Each call is its own project name, and therefore its own TSD</b></para>
        /// <para>
        /// <paramref name="projectName"/> decides the TBD and the TSD beside it. An optimisation passes
        /// <c>&lt;project&gt;-Opt01</c>, <c>-Opt02</c> and so on, so no round can overwrite the results that
        /// are the evidence for another round.
        /// </para>
        ///
        /// <para><b>Warm starting from a canonical TBD</b></para>
        /// <para>
        /// Where <paramref name="partOCanonicalTBD"/> is supplied, this run does <b>not</b> convert the
        /// geometry: the canonical TBD is copied to this run's own TBD by
        /// <c>WorkflowSettings.Path_TBD_Canonical</c> and everything after the conversion still runs on the
        /// copy - the zone identity stamps, the zones, the ventilation network, and a real full-year
        /// simulation. Between Iteration 2B rounds only the ventilation state changes, and on the licensed
        /// acceptance model the conversion is 41.6 s of a 64.2 s round against 3.6 s of simulation.
        /// </para>
        /// <para>
        /// <b>No gbXML is written on that path</b>, which is the point: the export, the T3D import and the
        /// shading calculation are the work being skipped. The solar calculation the canonical TBD already
        /// carries is the one this round uses, which is correct precisely because the geometry and the
        /// shading inputs are what did not change.
        /// </para>
        /// <para>
        /// <b>Whether the canonical is still valid is decided before this is called</b> - see
        /// <see cref="PartOCanonicalTBD.IsValidFor"/>. A caller that cannot prove it passes null and gets
        /// the full conversion, which is always available and always authoritative.
        /// </para>
        ///
        /// <para><b>The Part O arming happens here, before the workflow, or not at all</b></para>
        /// <para>
        /// <paramref name="partORun"/> is armed with <c>ExpectResults</c> only where the case about to run
        /// is the full annual series a TM59 assessment can read - read off the settings that will actually
        /// be handed to the workflow, never off an intention. A partial, one-day or sizing-only case
        /// therefore leaves the run unarmed and unable to be completed, which is the guarantee PR #76
        /// established and this does not weaken.
        /// </para>
        ///
        /// <para><b>And the returned model carries the run's provenance</b></para>
        /// <para>
        /// On the full-year path, the model handed back is stamped with the overheating scenarios the run was
        /// prepared with and with the results file it was produced from
        /// (<c>AnalyticalModelParameter.SimulationResultProvenance</c>, which fingerprints both the design
        /// state and those scenarios), and that model is written beside the TBD as this run's own
        /// <c>&lt;project&gt;.sam</c> - the native SAM model form, at the one path
        /// <see cref="Query.Path_PartORunModel(string)"/> states. That is what lets a later session reopen
        /// the saved model and review its TM59 assessment from the existing results, without simulating
        /// again; see <c>PartORun.Restore</c>.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The design to simulate. <b>Copied first</b>, so a cancelled run leaves it untouched.</param>
        /// <param name="partOSimulationContext">The case to run it as.</param>
        /// <param name="projectName">The project name for this run - what makes its TBD and TSD its own.</param>
        /// <param name="partORun">The run to arm, or null where the caller has none.</param>
        /// <param name="partOCanonicalTBD">
        /// An already-converted TBD to start this run from instead of converting the geometry again, or null
        /// for the full conversion. <b>Only ever read</b>; this run works on its own copy of it.
        /// </param>
        /// <param name="externalCancellationToken">Lets one Cancel click abort a whole optimisation, not just this run.</param>
        /// <param name="path_TBD">The TBD this run wrote or would have written.</param>
        /// <param name="path_TSD">The results file beside it. Existence is not guaranteed - a run that did not simulate writes none.</param>
        /// <param name="cancelled">Whether the run was cancelled. A cancelled run returns null.</param>
        /// <param name="fullYear">Whether the case that actually ran was the full annual series.</param>
        /// <param name="notes">What was worth saying about the run - unzoned spaces and the like.</param>
        /// <param name="refusal">
        /// Why the run could not <b>start</b> - the gbXML the TAS solar calculation reads could not be
        /// written, or an existing TBD could not be overwritten. Null where it started, whatever it then did.
        /// <para>
        /// Told apart from a workflow that ran and produced nothing, because the two need different answers:
        /// a run that never started leaves no TBD for anything downstream to convert or export from, and
        /// carrying on past it is how a later step ends up reading a file that is not there.
        /// </para>
        /// </param>
        /// <param name="partOWorkflowRunner">
        /// Who runs the workflow itself, once everything above it has been prepared. <b>Null - the default,
        /// and every existing caller - executes <see cref="RunWorkflow(AnalyticalModel, WorkflowSettings, CancellationToken, out bool, bool)"/>
        /// exactly as before.</b> Approved Document O Iteration 3 supplies
        /// <c>SAM_Tas Create.NoIzamThermalSource</c> here so Candidate B's thermal source is produced by
        /// the identical pipeline with only its last step changed; see <see cref="PartOWorkflowRunner"/>
        /// for why the seam is exactly this narrow.
        /// </param>
        /// <returns>The model the workflow returned, or null where it did not run, failed or was cancelled.</returns>
        public static AnalyticalModel RunPartOSimulation(AnalyticalModel analyticalModel, PartOSimulationContext partOSimulationContext, string projectName, PartORun partORun, CancellationToken externalCancellationToken, out string path_TBD, out string path_TSD, out bool cancelled, out bool fullYear, out List<string> notes, out string refusal, PartOCanonicalTBD partOCanonicalTBD = null, PartOWorkflowRunner partOWorkflowRunner = null)
        {
            path_TBD = null;
            path_TSD = null;
            cancelled = false;
            fullYear = false;
            notes = [];
            refusal = null;

            if (analyticalModel is null || partOSimulationContext is null || string.IsNullOrWhiteSpace(projectName))
            {
                refusal = "No model, no TAS case or no project name was supplied, so nothing could be simulated.";

                return null;
            }

            string outputDirectory = partOSimulationContext.OutputDirectory;
            WeatherData weatherData = partOSimulationContext.WeatherData;
            SolarCalculationMethod solarCalculationMethod = partOSimulationContext.SolarCalculationMethod;

            //THE ownership boundary of an Approved Document O TAS run, and the only deep copy on it.
            //
            //Everything below mutates in place - the name here, the materials at "Update Materials" - and a
            //cancelled run must leave the caller's model exactly as it was rather than renamed and
            //re-materialled behind its back.
            //
            //This copy is what every step from here to WorkflowCalculator then works on, which is why the
            //workflow is TOLD the model is already owned rather than copying it again. The two callers of
            //this method are Simulate, which hands over a model it has not yet mutated, and the Iteration
            //2B optimiser, whose model is derived from the retained last-valid design through SHALLOW
            //copies and so shares its objects - the case that makes this copy load-bearing rather than
            //merely tidy.
            //
            //DEEP, which is what makes that true. The ordinary copy constructor rebuilds the cluster's
            //dictionaries but SHARES the objects inside it, and the writes below are in-place mutations
            //rather than same-guid replacements - UpdateConstructionLayersByPanelType re-materials the
            //shared panels, and the TAS conversion stamps zone and building-element identity onto the
            //shared spaces, panels and apertures - so a shallow copy isolated the model's name and its
            //libraries and nothing else.
            //
            //On the optimisation path the caller IS the retained last-valid design of the previous round.
            //A shallow copy let a later round's conversion stamp new TAS identities onto it, so a round
            //that then failed or was cancelled handed back a last-valid model whose
            //SimulationResultProvenance.Fingerprint_Model no longer matched the results it was produced
            //from - a false "the model has changed since the simulation results were produced from it" on
            //reopening, and a forced re-simulation. See AnalyticalModel(AnalyticalModel, bool) for the
            //ownership rule this establishes.
            analyticalModel = new AnalyticalModel(analyticalModel, true)
            {
                Name = projectName,
            };

            //The model TAS will actually be given, normalized before anything looks at it or writes a file
            //from it.
            //
            //Both of these WERE inside the progress dialog below, after the gbXML had been written. They are
            //here because the pre-simulation gate has to judge the model that is handed to TAS, not the one
            //handed to this method: the material repair fixes exactly the "Material Library does not contain
            //Material X" state Create.Log reports as an Error, so gating before it would refuse models this
            //pipeline was about to make valid - and a defect these two introduced would be invisible.
            //
            //Neither is a long COM call, which is what the dialog exists to keep responsive.

            IEnumerable<IMaterial> materials = Analytical.Query.Materials(analyticalModel.AdjacencyCluster, Analytical.Query.DefaultMaterialLibrary());
            if (materials is not null)
            {
                foreach (IMaterial material in materials)
                {
                    if (analyticalModel.HasMaterial(material))
                    {
                        continue;
                    }

                    analyticalModel.AddMaterial(material);
                }
            }

            analyticalModel = partOSimulationContext.UpdateConstructionLayersByPanelType ? analyticalModel.UpdateConstructionLayersByPanelType() : analyticalModel;

            //The Part O pre-flight: after the normalization above, and before the first byte of any file is
            //written.
            //
            //Scoped to a PREPARED Part O run, which is exactly "the first TAS simulation of a prepared Part
            //O run" and every re-prepared one after it: Simulate arms this with the session's run, and both
            //optimisation call sites - an Iteration 2B round and the capacity envelope - call
            //PartORun.Prepare immediately before calling this. So a full run, an isolated run, Iteration
            //1a, 1b and 2, every 2B round and the envelope are all gated by this one place, and no Part O
            //path can be added later that quietly skips it.
            //
            //NOT the ordinary Simulate command, which reaches this method with no run or an unprepared one.
            //Making SAM Check a hard gate on every TAS simulation in SAM is a bigger change than the Part O
            //contract - Create.Log reports Errors on states a long-standing model may well carry, and a
            //model that simulates today must not stop simulating because the Part O path grew a gate.
            //
            //It runs over this run's own copy, which is by definition the model about to be converted: on an
            //isolated run that is the DERIVED isolated model, because
            //Analytical.Modify.PreparePartOIteration applied the isolation and the run adopted its output.
            //Checking the full-building model it was extracted from would be validating something else.
            //
            //Errors stop it, warnings do not - see PartOPreSimulationCheck, and in particular why the
            //intentional "MVHR-01 is missing internal conditions on some daytypes" state must stay a
            //warning. Passing is not a promise that TAS will run: licensing, file I/O, the solver and the
            //weather data are all still ahead of it.
            PartOPreSimulationCheck partOPreSimulationCheck = PartOPreSimulationCheck.Gate(partORun, analyticalModel);
            if (partOPreSimulationCheck is not null)
            {
                if (!partOPreSimulationCheck.IsValid)
                {
                    //Shown here rather than left to the caller's message box, because only the log carries
                    //the whole list - the type, name and Guid of every record. The refusal the caller then
                    //shows is the summary of it.
                    Core.Log log = partOPreSimulationCheck.Log;
                    if (log is not null)
                    {
                        log.Sort();

                        //A modal the engineer has to read: the shared progress window, topmost on its own
                        //thread, must not sit over it.
                        PartOProgressHost.Current?.Hide();

                        new SAM.Core.UI.WPF.LogWindow(log.Filter([Core.LogRecordType.Error, Core.LogRecordType.Warning, Core.LogRecordType.Undefined])).ShowDialog();
                    }

                    refusal = partOPreSimulationCheck.Refusal();

                    return null;
                }

                //Warnings on a model that IS going to be simulated. Carried as notes so they reach the run's
                //diagnostics and the optimisation step that produced them, and deliberately not shown as a
                //dialog: a Part O model has intentional warnings on every run, and a dialog per run would
                //train people to dismiss the one that mattered.
                foreach (Core.LogRecord logRecord in partOPreSimulationCheck.Warnings)
                {
                    notes.Add(string.Format("Pre-simulation check (warning): {0}", logRecord.Text));
                }
            }

            //Skipped entirely on the warm-start path: the gbXML exists to be imported into a T3D and
            //converted, and a canonical TBD is the product of having done exactly that. Writing one and then
            //not converting it would cost the export for nothing.
            //A Part O case folder (<root>/<case>/tas, see PartOOutputPaths) is created by whoever resolved it; this
            //only restores a subfolder removed since, before the first file. SAM_Tas creates no folders. A legacy
            //flat folder is not created: it was always one that existed.
            PartOOutputPaths partOOutputPaths = PartOOutputPaths.Find(outputDirectory);
            if (partOOutputPaths is not null)
            {
                try
                {
                    partOOutputPaths.CreateDirectories();
                }
                catch (Exception exception)
                {
                    refusal = string.Format("The Part O output folder '{0}' could not be created, so nothing was simulated. ({1})", partOOutputPaths.Directory_Case, exception.Message);

                    return null;
                }
            }

            string path_Xml = null;
            if (solarCalculationMethod == SolarCalculationMethod.TAS && partOCanonicalTBD is null)
            {
                path_Xml = System.IO.Path.Combine(outputDirectory, projectName + ".xml");
                if (!gbXML.Convert.ToFile(analyticalModel, path_Xml))
                {
                    refusal = string.Format("The gbXML file '{0}' could not be created, so the TAS solar calculation has nothing to read.", path_Xml);

                    return null;
                }
            }

            path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");
            path_TSD = System.IO.Path.ChangeExtension(path_TBD, "tsd");

            bool shadingUpdated = false;

            AnalyticalModel result = null;

            // One token spans the COM preparation steps below and the workflow that follows them, so a
            // single Cancel click aborts whichever of the two is running - and, through
            // externalCancellationToken, a whole optimisation rather than one of its rounds.
            //Inside a Part O operation that already shows its own progress window, these steps report there
            //and take its Cancel instead of opening a "Preparing Model" dialog of their own.
            PartOProgressHost partOProgressHost = PartOProgressHost.Current;

            using (CancellationTokenSource cancellationTokenSource = partOProgressHost is null
                ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, partOProgressHost.Token))
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                // Hosted off this thread: the steps below are single COM calls that run for minutes, and
                // Windows ghosts a window whose thread has stopped pumping and then discards clicks on the
                // ghost. Not a using - see below for why the host must be disposed before the final check.
                ProgressWindowHost progressWindowHost = partOProgressHost is null
                    ? new(string.Format("Preparing Model ({0})", projectName), 8, true, Analytical.Tas.Query.CancelNote(null))
                    : null;

                Action<string> step = description =>
                {
                    if (progressWindowHost is not null)
                    {
                        progressWindowHost.Note = Analytical.Tas.Query.CancelNote(description);
                        progressWindowHost.Update(description);
                    }
                    else
                    {
                        partOProgressHost.Detail(description);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                };

                //Cancel is offered while these steps run - each observes the token, and so does the check
                //after them - and withdrawn in the finally, BEFORE that check, so no click can land after the
                //last look. Where the host always offers Cancel (Prepare & Run) this changes nothing.
                IDisposable cancelScope = partOProgressHost?.AllowCancel();

                try
                {
                    if (progressWindowHost is not null)
                    {
                        progressWindowHost.CancelRequested += (s, e) => cancellationTokenSource.Cancel();
                    }

                    //NOT on the warm-start path: the copy from the canonical overwrites this run's TBD
                    //anyway, and deleting first would only widen the window in which the run has no TBD.
                    if (partOCanonicalTBD is null && System.IO.File.Exists(path_TBD))
                    {
                        try
                        {
                            System.IO.File.Delete(path_TBD);
                        }
                        catch
                        {
                            // Take the dialog down before saying anything: it is topmost and lives on another
                            // thread, so a message shown under it can end up hidden behind it.
                            progressWindowHost?.Dispose();

                            refusal = string.Format("The existing TBD file '{0}' could not be overwritten.", path_TBD);

                            return null;
                        }
                    }

                    //The SAM solar path builds the TBD here, from scratch, which is the very work a warm
                    //start exists to avoid - so a canonical TBD supersedes it and the workflow does the
                    //rest on the copy.
                    if (solarCalculationMethod == SolarCalculationMethod.SAM && partOCanonicalTBD is null)
                    {
                        List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                        SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), false, Tolerance.MacroDistance, Tolerance.MacroDistance, 0.012, Tolerance.Distance);

                        using (SAMTBDDocument sAMTBDDocument = new(path_TBD))
                        {
                            TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                            step("Updating WeatherData");
                            Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, analyticalModel is null ? 0 : analyticalModel.AdjacencyCluster.BuildingHeight());

                            TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                            List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
                            if (dayTypes.Find(x => x.name == "HDD") is null)
                            {
                                TBD.dayType dayType = calendar.AddDayType();
                                dayType.name = "HDD";
                            }

                            if (dayTypes.Find(x => x.name == "CDD") is null)
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
                    }
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                finally
                {
                    cancelScope?.Dispose();

                    progressWindowHost?.Dispose();
                }

                // The host is down, so no further click can arrive and any in-flight one has already run:
                // this observation is final - but only once the host confirms it actually shut down. If it
                // could not, its thread is still live and may be sitting on a click nothing observed.
                if (!cancelled && (cancellationTokenSource.IsCancellationRequested || (progressWindowHost is not null && !progressWindowHost.ShutdownCompleted)))
                {
                    cancelled = true;
                }

                if (cancelled)
                {
                    return null;
                }

                List<DesignDay> heatingDesignDays = [Analytical.Query.HeatingDesignDay(weatherData)];
                List<DesignDay> coolingDesignDays = [Analytical.Query.CoolingDesignDay(weatherData)];

                SurfaceOutputSpec surfaceOutputSpec = new("Tas.Simulate")
                {
                    SolarGain = true,
                    Conduction = true,
                    ApertureData = true,
                    Condensation = false,
                    Convection = false,
                    LongWave = false,
                    Temperature = true
                };

                int simulate_From = partOSimulationContext.SimulateFrom;
                int simulate_To = partOSimulationContext.SimulateTo;

                bool simulate = simulate_From > 0 && simulate_To > 0;

                if (!simulate && shadingUpdated)
                {
                    //Unchanged from Simulate: a shading update forces a one-day run even where none was
                    //asked for, and that run is then correctly NOT a full year.
                    simulate_From = 1;
                    simulate_To = 1;
                    simulate = true;
                }

                WorkflowSettings workflowSettings = new()
                {
                    Path_TBD = path_TBD,

                    //The seam. WorkflowCalculator copies this to Path_TBD, skips the conversion a canonical
                    //TBD already carries, and runs everything after it - see
                    //WorkflowSettings.Path_TBD_Canonical.
                    Path_TBD_Canonical = partOCanonicalTBD?.Path_TBD,

                    Path_gbXML = path_Xml,
                    WeatherData = solarCalculationMethod == SolarCalculationMethod.TAS ? weatherData : null,
                    DesignDays_Heating = heatingDesignDays,
                    DesignDays_Cooling = coolingDesignDays,
                    SurfaceOutputSpecs = [surfaceOutputSpec],
                    UnmetHours = partOSimulationContext.UnmetHours,
                    Simulate = simulate,
                    Sizing = partOSimulationContext.Sizing,
                    //TRUE on the warm-start path whatever the solar method. The zones carry the internal
                    //conditions, and re-deriving them from the current model is half of what makes a
                    //warm-started round the current design rather than the baseline's.
                    UpdateZones = partOCanonicalTBD is not null || solarCalculationMethod == SolarCalculationMethod.TAS,
                    UseWidths = partOSimulationContext.UseWidths,
                    SimulateFrom = simulate_From,
                    SimulateTo = simulate_To
                };

                // Read off the settings that are about to run, never off an intention - see
                // Query.IsPartOFullYearSimulation for why nothing less may complete a Part O run.
                fullYear = workflowSettings.IsPartOFullYearSimulation();

                // Announced BEFORE the workflow, and only for the full-year case. Two guarantees in one
                // arming: a partial/one-day/sizing-only workflow leaves the run unarmed and so cannot
                // complete it, and the results file is fingerprinted now so an older TSD at this path
                // cannot be accepted as this run's.
                if (fullYear && partORun is not null && partORun.State == PartORunState.Prepared)
                {
                    partORun.ExpectResults(path_TSD);
                }

                // OWNED: the copy taken at the top of this method is this run's alone, and everything
                // between there and here has worked on it. Without saying so the workflow took a SECOND
                // deep copy of the same model for the same guarantee - and Simulate, which calls this
                // method, had taken a third. See WorkflowCalculator.Calculate(AnalyticalModel, bool).
                //
                // The seam, and the only substitutable step on this path - see PartOWorkflowRunner. The
                // null default IS Modify.RunWorkflow with exactly the arguments it always had, so no
                // existing caller's behaviour depends on this line having been written.
                result = partOWorkflowRunner is null
                    ? Modify.RunWorkflow(analyticalModel, workflowSettings, cancellationToken, out cancelled, true)
                    : partOWorkflowRunner(analyticalModel, workflowSettings, cancellationToken, out cancelled);
            }

            //SAM_Tas writes its timing CSV beside the TBD; in a Part O case folder it belongs in diagnostics.
            PartOOutputPaths.FileDiagnostics(outputDirectory);

            if (cancelled || result is null)
            {
                return null;
            }

            result.SetValue(Analytical.AnalyticalModelParameter.WeatherData, weatherData);

            //The run's self-description, persisted onto the model the workflow returned, so a SAVED copy of
            //it - the per-run <project>.sam this writes beside the TBD, or the user's own .sam saved later -
            //can be reopened in a later session and its results reviewed WITHOUT rerunning the simulation.
            //The scenarios are the assessment's authority over which TM59 criterion applies to which space;
            //the provenance is the proof of which results file the model belongs to, and it fingerprints the
            //scenarios along with the design so neither can move underneath the results. See
            //PartORun.Restore.
            //
            //A run with no scenarios - a plain, non-Part-O simulation - is left entirely unstamped: there is
            //nothing to review it against, and no run model is written for it.
            List<OverheatingScenario> overheatingScenarios = partORun?.OverheatingScenarios;
            if (overheatingScenarios is not null && overheatingScenarios.Count != 0)
            {
                result.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>(overheatingScenarios));

                //Only the full annual series a TM59 assessment can read is recorded - a partial, one-day or
                //sizing-only run writes no provenance, exactly as it cannot complete a Part O run.
                //
                //AND only where the results are provably THIS run's, asked of PartORun.IsResultsOfThisRun -
                //the same lineage rule PartORun.Complete refuses on, a moment later, in the caller. Asked
                //HERE because everything below this line is reopenable: a workflow that returned a model
                //while leaving an existing TSD untouched would otherwise be stamped and persisted into a
                //fully self-consistent .sam - model, scenarios and file fingerprints all agreeing - which a
                //later session would restore and offer for review against an EARLIER run's results. Complete
                //would then refuse the run, correctly, and the misleading artifact would already be written.
                //Nothing is stamped and nothing is written for a run that cannot be completed.
                string refusal_Lineage = "there is no prepared Part O run to have produced them.";

                bool ofThisRun = partORun is not null && partORun.IsResultsOfThisRun(path_TSD, out refusal_Lineage);

                if (fullYear && !ofThisRun)
                {
                    //Noted rather than silent: "no reviewable model was written, and why" is the diagnostic.
                    //The run itself is refused by Complete, which is where that verdict belongs.
                    notes.Add(string.Format("No persisted run model was written for these results, because they are not provably this run's: {0}", refusal_Lineage));
                }

                if (fullYear && ofThisRun)
                {
                    //Constructed AFTER the scenarios are stamped above, deliberately: the record fingerprints
                    //both the design state and the scenarios it finds on the model, and a record taken before
                    //them would bind an empty assessment context.
                    result.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(result, path_TSD));

                    //This run's own persisted model, beside its results and named from them by the single
                    //naming authority - Query.Path_PartORunModel, which is where the extension is stated.
                    //Written through Core.Convert.ToFile under SAMFileType.SAM: SAM's native model writer,
                    //the one Save As uses, so this file reopens through the ordinary Open path with no
                    //special case anywhere.
                    //
                    //And then, ONLY once that has succeeded, the workflow's own "Saving Model" export for
                    //this run - the plain-text <run>.json beside the TBD - is removed, so a Part O run
                    //leaves one reviewable model artifact rather than the same model twice. The ordering is
                    //the safety property and lives in Modify.PersistPartORunModel: a failed .sam write
                    //deletes nothing and leaves the JSON as the fallback copy, and a JSON that could not be
                    //removed is a note, never a failed run. WorkflowCalculator itself is untouched - every
                    //ordinary non-Part-O TAS run in SAM still writes and keeps its <project>.json.
                    Modify.PersistPartORunModel(result, path_TSD, path_TBD, out string note_Persistence);

                    if (!string.IsNullOrWhiteSpace(note_Persistence))
                    {
                        notes.Add(note_Persistence);
                    }

                    //And what a later session needs to start Iteration 3 from these results without re-running
                    //Prepare & Run - the prepared model and a sidecar bound to these results (PartORunResume).
                    //
                    //Not for a mixed-design run (a model SAM materialised from dwelling strategies, which carries
                    //its PartOMaterialisationRecord): it has no legacy preparation to resume, its provenance is
                    //that record, and the note this would add - "no saved preparation was written" - would only
                    //mislead. Every legacy run is untouched: a legacy preparation never carries the record.
                    if (!result.HasValue(Analytical.AnalyticalModelParameter.PartOMaterialisationRecord))
                    {
                        result.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out SimulationResultProvenance simulationResultProvenance_Resume);
                        Modify.PersistPartORunResume(partORun, partOSimulationContext, simulationResultProvenance_Resume, path_TSD, out string note_Resume);

                        if (!string.IsNullOrWhiteSpace(note_Resume))
                        {
                            notes.Add(note_Resume);
                        }
                    }
                }
            }

            return result;
        }
    }
}
