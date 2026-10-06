// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Weather;
using System;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        /// <summary>
        /// The Simulate dialog's state for an <b>Approved Document O</b> run: the TAS case Part O actually
        /// requires, already chosen, with only the inputs a person genuinely owns left to fill in.
        ///
        /// <para><b>The defect this closes</b></para>
        /// <para>
        /// Part O reached the Simulate dialog through the ordinary <c>Modify.Simulate</c>, which seeds itself
        /// from the manual command's remembered <see cref="SimulateOptions"/> and, failing that, from
        /// <c>UI.Create.SimulateOptions</c> - whose <see cref="SimulateOptions.FullYearSimulation"/> is
        /// <b>false</b>. So the normal path was: prepare the iteration, press Run, accept the dialog, wait
        /// for TAS, and be told the run could not be completed because it was not a full year. The one
        /// setting a Part O run cannot do without was the one nobody was asked to turn on. Days 1 to 365 is
        /// not a preference here - <c>Query.IsPartOFullYearSimulation</c> refuses every other range outright,
        /// so there is no completable Part O run with any other value.
        /// </para>
        ///
        /// <para><b>What is fixed, and why each one</b></para>
        /// <list type="bullet">
        /// <item><see cref="SimulateOptions.Simulate"/> and <see cref="SimulateOptions.FullYearSimulation"/>:
        /// without both, no TSD carrying an annual hourly series exists, and a TM59 assessment has nothing to
        /// read.</item>
        /// <item><see cref="SimulateOptions.Sizing"/>: <b>off</b>. Approved Document O asks for a full year
        /// of the building as designed, not a plant sizing exercise, and <b>nothing in the assessment or in
        /// Iteration 2B reads a design load</b> - the only consumer of <c>DesignHeatingLoad</c>,
        /// <c>DesignCoolingLoad</c> or <c>SpaceSimulationResultParameter.DesignLoad</c> anywhere is
        /// <c>Modify.PrintRoomDataSheets</c>, and room data sheets are fixed off below.
        /// <para>
        /// It was on until this, because every Part O run to date had been produced that way. That is not a
        /// reason: <c>Tas.Query.Sizing</c> is not a read-only step. It opens the very TBD that is about to be
        /// simulated and re-saves it several times - zeroing and recomputing every zone's
        /// <c>maxHeatingLoad</c> and <c>maxCoolingLoad</c>, changing every zone's <c>sizeHeating</c> and
        /// <c>sizeCooling</c>, adding an <c>Air_Glass</c> construction, and applying the model's oversizing
        /// factors. The annual run then simulates that plant-sized file. It also writes
        /// <c>&lt;project&gt;_Uncapped.tbd</c> and <c>&lt;project&gt;_HDDCDD.tbd</c> beside the run's own
        /// outputs, because <c>SizingSettings.GenerateUncappedFile</c> and <c>GenerateHDDCDDFile</c> both
        /// default to true and the workflow's call site overrides neither - two extra files for the baseline
        /// and for every Iteration 2B round.
        /// </para>
        /// <para>
        /// So the choice is not "sized or unsized results", it is <b>which file the year is run against</b>.
        /// Part O wants the prepared model as converted. Turning it off is deliberate and it invalidates
        /// canonical baselines taken before it - <c>PartOCanonicalTBD</c> fingerprints <c>sizing=</c>, so a
        /// warm start correctly rebuilds rather than reusing a TBD produced under the old case.
        /// </para>
        /// </item>
        /// <item>The export tick boxes - room data sheets, SAP, Part L, TPD and the TAS domestic-overheating
        /// XML: all <b>off</b>. Each is a deliverable of a run somebody asked for. Part O has its own
        /// overheating deliverable in <c>Modify.AssessPartOTM59</c>, which reads the TSD directly; the
        /// dialog's "Domestic Overheating" box is a second, unrelated XML export that would re-open the TBD
        /// after every run to produce a file nothing in this workflow reads.</item>
        /// <item><see cref="SimulateOptions.UnmetHours"/>, <see cref="SimulateOptions.UseWidths"/> and
        /// <see cref="SimulateOptions.UpdateConstructionLayersByPanelType"/>: part of the TAS case, and not
        /// Part O decisions. Fixed for the same reason as sizing - an optimisation must repeat the case it
        /// started from.</item>
        /// </list>
        ///
        /// <para><b>The project name: derived, and not a user decision</b></para>
        /// <para>
        /// <b>Always</b> taken from the prepared model, never from the remembered options - and locked in the
        /// dialog by <c>SimulateControl.LockPartOSettings</c>. It is the run's <i>identity</i> rather than a
        /// label on it:
        /// </para>
        /// <list type="bullet">
        /// <item>Every artifact a Part O run is judged by derives from it - <c>&lt;project&gt;.tbd</c>,
        /// <c>.tsd</c>, the per-run <c>.sam</c> and <c>&lt;project&gt;-TM59.txt</c>.</item>
        /// <item>On an isolated run it already carries the scope token <c>Query.ProjectName_Isolated</c> put
        /// there so that run's evidence cannot land on a full run's or on another selection's. A name
        /// remembered from an earlier run, or typed over by hand, is exactly the collision that token exists
        /// to prevent.</item>
        /// <item><see cref="PartOSimulationContext.Iteration_ProjectName"/> reads the optimisation round back
        /// out of it, so an edited name can restart Iteration 2B's numbering at <c>-Opt01</c> and overwrite a
        /// previous optimisation's evidence.</item>
        /// </list>
        /// <para>
        /// Nothing downstream refuses a hand-edited name - it is simply believed - so a person could destroy
        /// the provenance of a run by retyping one field. Deriving it and locking it is what removes that,
        /// and it costs nobody anything: <b>where</b> the evidence is written is still theirs to redirect.
        /// </para>
        ///
        /// <para><b>What a person still owns</b></para>
        /// <para>
        /// The weather, the output directory and the solar calculation method. These are carried between runs
        /// in one session because re-picking a weather file for every iteration is the click cost this whole
        /// pass exists to remove - but each is re-validated here rather than trusted:
        /// </para>
        /// <list type="bullet">
        /// <item><b>Weather</b>: the model's own <c>AnalyticalModelParameter.WeatherData</c> wins wherever the
        /// model has one. It is the project's weather, it is what the run will be stamped with, and a
        /// remembered file from another project must not quietly displace it. The remembered one is used only
        /// where the model states nothing.</item>
        /// <item><b>Output directory</b>: the remembered one only where it still exists on this machine;
        /// otherwise the directory the model was opened from. A path from a previous session that has since
        /// gone would otherwise be handed to the run and fail at the first write.</item>
        /// <item><b>Solar calculation method</b>: carried as-is. Both values are always valid, and neither can
        /// be stale in the way a path or a file can.</item>
        /// </list>
        /// <para>
        /// Nothing else is read out of <paramref name="simulateOptions_Remembered"/>. That is the property
        /// that makes "stale settings do not survive a change of scenario, scope or model" true by
        /// construction rather than by a rule somebody has to remember to apply.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The prepared model about to be simulated. Its name is the project name.</param>
        /// <param name="path_Model">Where the model was opened from, if known - the fallback output directory.</param>
        /// <param name="simulateOptions_Remembered">
        /// This session's previous Part O dialog state, or null on the first run. Only the three fields above
        /// are read from it.
        /// </param>
        /// <param name="func_DirectoryExists">
        /// How to tell whether a remembered output directory is still there. Defaults to the filesystem;
        /// present so the rule can be tested without one.
        /// </param>
        public static SimulateOptions SimulateOptions_PartO(AnalyticalModel analyticalModel, string path_Model, SimulateOptions simulateOptions_Remembered, Func<string, bool> func_DirectoryExists = null)
        {
            if (analyticalModel is null)
            {
                return null;
            }

            func_DirectoryExists ??= System.IO.Directory.Exists;

            SimulateOptions result = new()
            {
                // ---- A: what an Approved Document O run IS. Not preferences, and not preselected defaults
                // a person is meant to confirm - there is no other value these can take and still produce a
                // run that can be assessed.
                Simulate = true,
                FullYearSimulation = true,
                Sizing = false,
                UnmetHours = false,
                UseWidths = false,
                UpdateConstructionLayersByPanelType = true,

                // ---- D: other workflows' deliverables.
                RoomDataSheets = false,
                CreateSAP = false,
                CreatePartL = false,
                CreateTPD = false,
                CreateTM59 = false,

                // ---- A: the run's identity, derived from the prepared model every time and locked in the
                // dialog. Not a user decision - see the summary for what an edited name destroys.
                ProjectName = analyticalModel.Name,

                // Deliberately NOT populated, and this is the only line where that is a decision rather than
                // an omission. The zone category list exists to feed one combo box, and that combo is
                // enabled only while the SAP or domestic-overheating export is ticked - both of which are
                // fixed off above and locked by SimulateControl.LockPartOSettings. Reading it would walk the
                // model's zones to fill a control nobody can reach.
                //
                // The ordinary Simulate command still reads it, through UI.Create.SimulateOptions, because
                // there those two boxes are a person's to tick.
                ZoneCategories = null,
            };

            // ---- C: the person's own inputs, carried but re-validated.

            SolarCalculationMethod solarCalculationMethod = simulateOptions_Remembered?.SolarCalculationMethod ?? SolarCalculationMethod.Undefined;

            result.SolarCalculationMethod = solarCalculationMethod == SolarCalculationMethod.Undefined
                ? SolarCalculationMethod.TAS
                : solarCalculationMethod;

            //Qualified: this namespace declares its own AnalyticalModelParameter, which would otherwise
            //shadow the SAM.Analytical one being read here.
            result.WeatherData = analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData) && weatherData is not null
                ? weatherData
                : simulateOptions_Remembered?.WeatherData;

            string outputDirectory_Model = string.IsNullOrWhiteSpace(path_Model) ? null : System.IO.Path.GetDirectoryName(path_Model);

            string outputDirectory_Remembered = simulateOptions_Remembered?.OutputDirectory;

            result.OutputDirectory = !string.IsNullOrWhiteSpace(outputDirectory_Remembered) && func_DirectoryExists(outputDirectory_Remembered)
                ? outputDirectory_Remembered
                : outputDirectory_Model;

            return result;
        }
    }
}
