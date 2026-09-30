// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The simulated half of one strategy-set run: what the TAS workflow and the production TM59 assessment
    /// produced for a materialised model.
    /// </summary>
    internal sealed class PartOStrategySetSimulation
    {
        public bool Cancelled { get; set; }

        /// <summary>Why the run did not produce assessable results, or null.</summary>
        public string? Refusal { get; set; }

        public List<string> Notes { get; } = [];

        public string? Path_TSD { get; set; }

        public long Length_TSD { get; set; }

        public long Timestamp_TSD { get; set; }

        /// <summary>The run's own persisted model beside its results, where one was written.</summary>
        public string? Path_RunModel { get; set; }

        public PartOTM59Assessment? Assessment { get; set; }

        public TimeSpan Elapsed { get; set; }

        /// <summary>The route the model was simulated on - SAM's record's, never chosen here.</summary>
        public PartOSimulationRoute Route { get; set; } = PartOSimulationRoute.Izam;

        /// <summary>The TAS Systems document of a Systems-route run; null on the IZAM route.</summary>
        public string? Path_TPD { get; set; }

        /// <summary>What each cooled unit did, read back from TAS - one line per cooled unit. Systems route only.</summary>
        public List<string> GuidanceSummaries { get; } = [];
    }

    /// <summary>
    /// Runs TAS and the TM59 assessment over a materialised model. The production one is
    /// <see cref="Modify.SimulatePartOMaterialisation"/>; a test supplies its own, so the orchestration is
    /// exercised without a licensed TAS.
    /// </summary>
    internal delegate PartOStrategySetSimulation PartOStrategySetSimulator(AnalyticalModel analyticalModel_Materialised, List<OverheatingScenario> overheatingScenarios, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a materialisation SAM put on the TAS Systems route (a cooled dwelling): thermal source, ONE mixed SAM_Systems
    /// graph, ONE TPD, the bridge and TM59. The production one is <see cref="Modify.SimulatePartOMaterialisationSystems"/>;
    /// a test supplies its own.
    /// </summary>
    internal delegate PartOStrategySetSimulation PartOStrategySetSystemsSimulator(PartOMaterialisation partOMaterialisation, IReadOnlyList<VentilationUnitTemplate>? ventilationUnitTemplates, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken);

    /// <summary>
    /// One strategy set taken all the way from the clean baseline to a per-dwelling result: SAM materialises it,
    /// TAS simulates the materialised model, the production assessment assesses it.
    /// </summary>
    internal sealed class PartOStrategySetRun
    {
        public PartOMaterialisation? Materialisation { get; set; }

        public PartOStrategySetSimulation? Simulation { get; set; }

        public List<PartODwellingResult> Results { get; } = [];

        /// <summary>SAM's structured refusals, where the materialisation refused. Never converted into a generic message.</summary>
        public List<PartOMaterialisationRefusal> Refusals => Materialisation?.Refusals ?? [];

        public bool IsMaterialised => Materialisation?.IsMaterialised ?? false;

        public bool Cancelled => Simulation?.Cancelled ?? false;

        /// <summary>Whether TAS ran the full year and the results were assessed - the only case with per-dwelling results.</summary>
        public bool Completed => IsMaterialised && Simulation is not null && !Simulation.Cancelled && Simulation.Refusal is null && Simulation.Assessment is not null;
    }

    public static partial class Modify
    {
        /// <summary>
        /// One strategy set, from the clean baseline to a per-dwelling TM59 result - the path shared by screening and
        /// by the final mixed run.
        ///
        /// <para><b>No second Part O implementation</b></para>
        /// <list type="number">
        /// <item><b>Materialise</b>: <c>SAM.Analytical.Modify.MaterialisePartODwellingStrategies</c>, the PR1 authority,
        /// over <paramref name="analyticalModel_Baseline"/> and the strategy set it carries. A refusal is returned as
        /// SAM's structured refusals and nothing is simulated.</item>
        /// <item><b>Simulate</b>: the Part O full-year case (<see cref="RunPartOSimulation"/>, via
        /// <see cref="SimulatePartOMaterialisation"/>) over the materialised model, in a PRIVATE run - as the Iteration
        /// 2B capacity envelope does - so the session's own Part O run is never touched.</item>
        /// <item><b>Assess</b>: the production <see cref="PartOTM59Assessment.Assess"/> over the model the workflow
        /// returned, tallied per dwelling by <see cref="Query.PartODwellingResults(PartOTM59Assessment, AdjacencyCluster, IEnumerable{Guid})"/>.</item>
        /// </list>
        ///
        /// <para><b>The baseline is never written</b></para>
        /// <para>
        /// The materialisation is pure and returns a new model; nothing here calls <c>SetJSAMObject</c>, so the open
        /// model stays the baseline plus the selection whatever this run does - completes, refuses or is cancelled.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel_Baseline">A clean baseline carrying the strategy set to build. Not modified.</param>
        /// <param name="ventilationUnitCapacityDescriptors">The catalogue offered, or null to offer none.</param>
        /// <param name="guids_Zone_Assessed">The dwellings to assess; null for every dwelling.</param>
        /// <param name="ventilationUnitTemplates">
        /// The catalogue's product templates - each product's manufacturer guidance, which is a cooled dwelling's cooling.
        /// Null offers none, so SAM refuses any cooled dwelling (<c>CoolingWithoutProductGuidance</c>).
        /// </param>
        /// <param name="partOStrategySetSystemsSimulator">The Systems-route simulator, where SAM puts the model on that route; null for production.</param>
        internal static PartOStrategySetRun RunPartOStrategySet(AnalyticalModel analyticalModel_Baseline, IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors, IEnumerable<Guid>? guids_Zone_Assessed, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken, PartOStrategySetSimulator? partOStrategySetSimulator = null, Action? onMaterialised = null, IEnumerable<VentilationUnitTemplate>? ventilationUnitTemplates = null, PartOStrategySetSystemsSimulator? partOStrategySetSystemsSimulator = null)
        {
            List<VentilationUnitTemplate>? ventilationUnitTemplates_Temp = ventilationUnitTemplates is null ? null : [.. ventilationUnitTemplates];

            PartOStrategySetRun result = new()
            {
                Materialisation = Analytical.Modify.MaterialisePartODwellingStrategies(analyticalModel_Baseline, ventilationUnitCapacityDescriptors, guids_Zone_Assessed, ventilationUnitTemplates_Temp),
            };

            if (!result.IsMaterialised)
            {
                return result;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                result.Simulation = new PartOStrategySetSimulation { Cancelled = true };
                return result;
            }

            PartOMaterialisation partOMaterialisation = result.Materialisation!;

            onMaterialised?.Invoke();

            //SAM's record decides the route, for the WHOLE model: never a hybrid of IZAM dwellings and TPD dwellings.
            result.Simulation = partOMaterialisation.Route == PartOSimulationRoute.Systems
                ? (partOStrategySetSystemsSimulator ?? SimulatePartOMaterialisationSystems)(partOMaterialisation, ventilationUnitTemplates_Temp, partOSimulationContext, cancellationToken)
                : (partOStrategySetSimulator ?? SimulatePartOMaterialisation)(partOMaterialisation.AnalyticalModel, [.. partOMaterialisation.OverheatingScenarios], partOSimulationContext, cancellationToken);

            //The TPD, route and bridge timing CSVs SAM_Tas wrote beside the run's TAS files go to diagnostics.
            PartOOutputPaths.FileDiagnostics(partOSimulationContext.OutputDirectory);

            if (result.Completed)
            {
                //Membership from the MATERIALISED model: the design side the assessment keys its spaces by.
                result.Results.AddRange(Query.PartODwellingResults(result.Simulation.Assessment, partOMaterialisation.AnalyticalModel.AdjacencyCluster, partOMaterialisation.Record?.ZoneGuids_Assessed));
            }

            return result;
        }

        /// <summary>
        /// The production simulation of a materialised model: prepared into a private <see cref="PartORun"/>, simulated
        /// by <see cref="RunPartOSimulation"/> exactly as every Part O full-year run is (the pre-simulation check, the
        /// TAS workflow, the results lineage, the persisted run model beside the results), completed, and assessed.
        /// </summary>
        internal static PartOStrategySetSimulation SimulatePartOMaterialisation(AnalyticalModel analyticalModel_Materialised, List<OverheatingScenario> overheatingScenarios, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken)
        {
            PartOStrategySetSimulation result = new();

            DateTime dateTime = DateTime.Now;

            PartORun partORun = new();
            if (!partORun.Prepare(analyticalModel_Materialised, overheatingScenarios))
            {
                result.Refusal = partORun.InvalidationReason ?? "The materialised model could not be prepared for simulation.";
                return result;
            }

            AnalyticalModel analyticalModel_Workflow = RunPartOSimulation(analyticalModel_Materialised, partOSimulationContext, partOSimulationContext.ProjectName, partORun, cancellationToken, out string _, out string path_TSD, out bool cancelled, out bool fullYear, out List<string> notes, out string refusal);

            result.Notes.AddRange(notes);
            result.Path_TSD = path_TSD;
            result.Elapsed = DateTime.Now - dateTime;

            if (cancelled)
            {
                result.Cancelled = true;
                return result;
            }

            if (refusal is not null || analyticalModel_Workflow is null || !fullYear)
            {
                result.Refusal = refusal ?? (analyticalModel_Workflow is null
                    ? "The TAS workflow did not run over the materialised model, so there are no results to assess."
                    : "The simulation that ran was not the full year a TM59 assessment reads.");

                return result;
            }

            if (!partORun.Complete(analyticalModel_Workflow, path_TSD, partOSimulationContext, out string refusal_Complete))
            {
                result.Refusal = refusal_Complete;
                return result;
            }

            FileInfo fileInfo = new(path_TSD);
            if (fileInfo.Exists)
            {
                result.Length_TSD = fileInfo.Length;
                result.Timestamp_TSD = fileInfo.LastWriteTimeUtc.Ticks;
            }

            string? path_RunModel = Query.Path_PartORunModel(path_TSD);
            result.Path_RunModel = path_RunModel is not null && File.Exists(path_RunModel) ? path_RunModel : null;

            PartOProgressHost.Current?.Detail(PartOProgressStages.AssessingTm59);

            result.Assessment = PartOTM59Assessment.Assess(partORun.AnalyticalModel_Assessment, partORun.Path_TSD, partORun.OverheatingScenarios);

            if (!result.Assessment.IsAssessed)
            {
                result.Notes.Add(result.Assessment.Refusal ?? "The production TM59 assessment could not be produced.");
            }

            return result;
        }
    }

    public static partial class Create
    {
        /// <summary>
        /// The locked Part O full-year TAS case for a mixed-design run, with the three inputs a person owns - weather,
        /// output folder, solar method - taken from the Simulation case, and the project name given by the run.
        /// Composed from <see cref="SimulateOptions_PartO"/> exactly as <c>Modify.SimulatePartO</c> composes it, so a
        /// mixed run and a Prepare &amp; Run are the same TAS case.
        /// </summary>
        internal static PartOSimulationContext PartOMixedSimulationContext(AnalyticalModel analyticalModel, string? path_Model, PartOSimulationCase partOSimulationCase, string projectName)
        {
            SimulateOptions simulateOptions = SimulateOptions_PartO(analyticalModel, path_Model, null);

            return new PartOSimulationContext(PartOMixedOutputDirectory(partOSimulationCase), projectName, partOSimulationCase.WeatherData is null ? null : new Weather.WeatherData(partOSimulationCase.WeatherData), partOSimulationCase.SolarCalculationMethod, 1, 365)
            {
                UnmetHours = simulateOptions?.UnmetHours ?? false,
                Sizing = simulateOptions?.Sizing ?? false,
                UseWidths = simulateOptions?.UseWidths ?? false,
                UpdateConstructionLayersByPanelType = simulateOptions?.UpdateConstructionLayersByPanelType ?? true,
            };
        }

        /// <summary>
        /// Where a mixed-design run's TAS files go: the MixedDesign case's <c>tas</c> folder beneath the Part O root the
        /// Simulation case names (<see cref="PartOOutputPaths"/>) - every screening and the final run, together.
        /// </summary>
        internal static string? PartOMixedOutputDirectory(PartOSimulationCase? partOSimulationCase)
        {
            string? directory_Root = partOSimulationCase?.OutputDirectory;

            return PartOOutputPaths.Create(directory_Root, PartOOutputCase.MixedDesign)?.Directory_Tas ?? directory_Root;
        }

        /// <summary>
        /// A run's project name - the TBD, TSD and run model are named from it - made from the model name and a suffix,
        /// with anything a file name cannot carry replaced. Distinct per run so a screening never overwrites the final
        /// mixed run's results, or one strategy's screening another's.
        /// </summary>
        internal static string PartOMixedProjectName(AnalyticalModel? analyticalModel, string suffix)
        {
            string name = string.IsNullOrWhiteSpace(analyticalModel?.Name) ? "Model" : analyticalModel!.Name.Trim();

            string result = string.Format("{0}_{1}", name, suffix);
            foreach (char @char in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(@char, '_');
            }

            return result;
        }
    }
}
