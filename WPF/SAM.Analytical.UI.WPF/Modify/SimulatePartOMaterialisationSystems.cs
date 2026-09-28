// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// The production simulation of a mixed model SAM put on the TAS Systems route (any dwelling cooled - SAM PR3B
        /// record, owner decision "Route"): the sequence the PR3B licensed gate proved, over the existing Iteration 3
        /// pipeline stages, without the A/B comparison.
        ///
        /// <list type="number">
        /// <item><b>SAM_Systems</b> - ONE graph (<see cref="PartOIteration3Pipeline.MaterialiseMixed"/>): every MVHR
        /// dwelling's unit the ordinary MV ventilation, only the cooled dwellings' units the product's manufacturer-guidance
        /// arrangement (<see cref="Query.PartOMixedSystemsCall"/>). Checked before any TAS time is spent: exactly the cooled
        /// units carry guidance, each at the cooling operating airflow SAM recorded.</item>
        /// <item><b>Thermal source</b> - the no-IZAM TAS run of the WHOLE materialised model (natural dwellings and common
        /// spaces free-run in it).</item>
        /// <item><b>ONE TPD</b> - converted, simulated, and each cooled unit's operation read back; exactly one read-back per
        /// cooled unit, on that unit's own air system.</item>
        /// <item><b>Bridge + TM59</b> - the thermostat bridge, then the production assessment with <b>the materialiser's own
        /// scenarios</b> (a cooled dwelling is <c>ActiveTrimCooling</c>, on the mechanical criterion).</item>
        /// <item><b>Persisted</b> beside the bridge results, provenanced to them, so the ordinary review path reopens it.</item>
        /// </list>
        ///
        /// <para>Nothing here computes an airflow, a range or a capacity, and the materialised model is never altered.</para>
        /// </summary>
        internal static PartOStrategySetSimulation SimulatePartOMaterialisationSystems(PartOMaterialisation partOMaterialisation, IReadOnlyList<VentilationUnitTemplate>? ventilationUnitTemplates, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken)
        {
            PartOStrategySetSimulation result = new() { Route = PartOSimulationRoute.Systems };

            DateTime dateTime = DateTime.Now;
            PartOIteration3Pipeline partOIteration3Pipeline = new();

            // ---- 1. ONE mixed SAM_Systems graph ------------------------------------------------------------------------

            PartOProgressHost.Current?.Detail("TAS Systems ventilation (SAM_Systems)");

            MechanicalVentilationMaterialisation? mechanicalVentilationMaterialisation = PartOMixedSystemsMaterialisation(partOMaterialisation, ventilationUnitTemplates, partOIteration3Pipeline, out string? refusal_Systems);
            if (mechanicalVentilationMaterialisation is null)
            {
                result.Refusal = refusal_Systems;
                return result;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                result.Cancelled = true;
                return result;
            }

            // ---- 2. The no-IZAM thermal source of the whole model ------------------------------------------------------

            PartOProgressHost.Current?.Detail("TAS thermal source (full year, no IZAM)");

            NoIzamThermalSource noIzamThermalSource = partOIteration3Pipeline.ThermalSource(partOMaterialisation.AnalyticalModel, partOSimulationContext, partOSimulationContext.ProjectName, cancellationToken, out AnalyticalModel analyticalModel_Source, out bool cancelled, out List<string> notes_Source, out string refusal_Source);

            result.Notes.AddRange(notes_Source ?? []);

            if (cancelled)
            {
                result.Cancelled = true;
                return result;
            }

            if (noIzamThermalSource is null || !noIzamThermalSource.IsComplete || analyticalModel_Source is null || string.IsNullOrWhiteSpace(noIzamThermalSource.Path_TSD))
            {
                result.Refusal = Join("The TAS thermal source of the mixed model was not produced.", [refusal_Source, .. noIzamThermalSource?.Refusals ?? []]);
                return result;
            }

            string directory = Path.GetDirectoryName(noIzamThermalSource.Path_TSD)!;
            string path_TPD = Path.Combine(directory, partOSimulationContext.ProjectName + ".tpd");
            string path_TBD_Bridge = Query.Path_PartOMixedBridgeTBD(directory, partOSimulationContext.ProjectName);
            string path_TSD_Bridge = Path.ChangeExtension(path_TBD_Bridge, ".tsd");

            // ---- 3. ONE TPD: convert, simulate, read back each cooled unit ---------------------------------------------

            PartOProgressHost.Current?.Detail("TAS Systems simulation (full year)");

            SystemVentilationRoute systemVentilationRoute = partOIteration3Pipeline.Route(noIzamThermalSource, mechanicalVentilationMaterialisation, path_TPD, 0, PartOSimulationContext.HourCount_FullYear - 1);
            if (systemVentilationRoute is null || !systemVentilationRoute.IsComplete)
            {
                result.Refusal = Join("The TAS Systems simulation of the mixed model did not complete.", systemVentilationRoute?.Refusals);
                return result;
            }

            result.Path_TPD = path_TPD;
            result.Notes.AddRange(systemVentilationRoute.Notes ?? []);

            List<string> refusals_Guidance = Query.PartOMixedGuidanceReadBackRefusals(mechanicalVentilationMaterialisation, systemVentilationRoute.GuidanceCoolingResults);
            if (refusals_Guidance.Count != 0)
            {
                result.Refusal = Join("The cooled units' operation could not be read back from TAS.", refusals_Guidance);
                return result;
            }

            GuidanceCoolingResults guidanceCoolingResults = systemVentilationRoute.GuidanceCoolingResults;
            result.GuidanceSummaries.AddRange(guidanceCoolingResults.Results.ConvertAll(x => x.Summary()));

            try
            {
                File.WriteAllText(Path.ChangeExtension(path_TPD, null) + "_GuidanceOperation.csv", guidanceCoolingResults.ToCsv());
            }
            catch (Exception exception)
            {
                result.Notes.Add(string.Format("The cooled units' hourly operation could not be written beside the TPD. ({0})", exception.Message));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                result.Cancelled = true;
                return result;
            }

            // ---- 4. The bridge, then TM59 with the materialiser's own scenarios ----------------------------------------

            PartOProgressHost.Current?.Detail("Resultant temperature (thermostat bridge)");

            FileInfo fileInfo_Before = new(path_TSD_Bridge);
            (bool Exists, long Length, DateTime WriteTime) bridge_Before = (fileInfo_Before.Exists, fileInfo_Before.Exists ? fileInfo_Before.Length : 0, fileInfo_Before.Exists ? fileInfo_Before.LastWriteTimeUtc : default);

            ResultantTemperatureResults resultantTemperatureResults = partOIteration3Pipeline.ResultantTemperatures(systemVentilationRoute, path_TBD_Bridge);
            if (resultantTemperatureResults is null || !resultantTemperatureResults.IsComplete)
            {
                result.Refusal = Join("The resultant temperatures of the mixed model were not produced.", resultantTemperatureResults?.Refusals);
                return result;
            }

            result.Notes.AddRange(resultantTemperatureResults.Notes ?? []);

            FileInfo fileInfo = new(path_TSD_Bridge);
            if (!fileInfo.Exists || (bridge_Before.Exists && fileInfo.Length == bridge_Before.Length && fileInfo.LastWriteTimeUtc == bridge_Before.WriteTime))
            {
                result.Refusal = string.Format("The bridge results at '{0}' were not written by this run, so they cannot be assessed as its results.", path_TSD_Bridge);
                return result;
            }

            PartOProgressHost.Current?.Detail("TM59 assessment");

            result.Assessment = PartOTM59Assessment.Assess(analyticalModel_Source, path_TSD_Bridge, partOMaterialisation.OverheatingScenarios);
            if (!result.Assessment.IsAssessed)
            {
                result.Notes.Add(result.Assessment.Refusal ?? "The production TM59 assessment could not be produced.");
            }

            // ---- 5. Reopenable: the source model, stamped with the scenarios and provenanced to the bridge results -----

            analyticalModel_Source.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new Core.SAMCollection<OverheatingScenario>(partOMaterialisation.OverheatingScenarios));
            analyticalModel_Source.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(analyticalModel_Source, path_TSD_Bridge));

            if (!PersistPartORunModel(analyticalModel_Source, path_TSD_Bridge, noIzamThermalSource.Path_TBD, out string note_Persist))
            {
                result.Notes.Add(note_Persist ?? "The mixed run's model could not be written beside its results, so its space-level result cannot be reopened later.");
            }
            else if (note_Persist is not null)
            {
                result.Notes.Add(note_Persist);
            }

            fileInfo.Refresh();
            result.Path_TSD = path_TSD_Bridge;
            result.Length_TSD = fileInfo.Length;
            result.Timestamp_TSD = fileInfo.LastWriteTimeUtc.Ticks;

            string? path_RunModel = Query.Path_PartORunModel(path_TSD_Bridge);
            result.Path_RunModel = path_RunModel is not null && File.Exists(path_RunModel) ? path_RunModel : null;

            result.Elapsed = DateTime.Now - dateTime;

            return result;
        }

        /// <summary>
        /// The mixed SAM_Systems graph of a Systems-route materialisation, checked against SAM's record: every cooled unit
        /// and only the cooled units carry manufacturer guidance, each at the cooling operating airflow SAM recorded.
        /// Null, with the reason, where it cannot be built or does not agree. No TAS.
        /// </summary>
        internal static MechanicalVentilationMaterialisation? PartOMixedSystemsMaterialisation(PartOMaterialisation partOMaterialisation, IEnumerable<VentilationUnitTemplate>? ventilationUnitTemplates, PartOIteration3Pipeline partOIteration3Pipeline, out string? refusal)
        {
            refusal = null;

            PartOMixedSystemsCall partOMixedSystemsCall = Query.PartOMixedSystemsCall(partOMaterialisation, ventilationUnitTemplates);
            if (!partOMixedSystemsCall.IsValid)
            {
                refusal = Join("The TAS Systems ventilation of the mixed model could not be composed.", partOMixedSystemsCall.Refusals);
                return null;
            }

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = partOIteration3Pipeline.MaterialiseMixed(partOMaterialisation.AnalyticalModel.AdjacencyCluster, partOMixedSystemsCall.Spaces, partOMixedSystemsCall.GuidanceSettings);
            if (mechanicalVentilationMaterialisation is null || !mechanicalVentilationMaterialisation.IsMaterialised)
            {
                refusal = Join("SAM_Systems could not materialise the mixed ventilation.", mechanicalVentilationMaterialisation?.Refusals);
                return null;
            }

            List<string> refusals = [];
            Dictionary<Guid, PartOCooledDwelling> dictionary_Cooled = partOMaterialisation.Record!.CooledDwellings.ToDictionary(x => x.AirHandlingUnitGuid);

            foreach (MechanicalVentilationGuidanceCooling mechanicalVentilationGuidanceCooling in mechanicalVentilationMaterialisation.GuidanceCoolings)
            {
                if (!dictionary_Cooled.TryGetValue(mechanicalVentilationGuidanceCooling.Guid_AirHandlingUnit, out PartOCooledDwelling? partOCooledDwelling))
                {
                    refusals.Add("SAM_Systems gave manufacturer-guidance cooling to a unit of a dwelling that is not cooled.");
                    continue;
                }

                if (!(Math.Abs(mechanicalVentilationGuidanceCooling.ElevatedAirFlow_Lps - partOCooledDwelling.CoolingOperatingAirFlow_Lps) <= 1e-9))
                {
                    refusals.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "The cooled unit of {0} would operate at {1:0.###} l/s, not at the {2:0.###} l/s SAM recorded for it.", partOCooledDwelling.VentilationUnitReference, mechanicalVentilationGuidanceCooling.ElevatedAirFlow_Lps, partOCooledDwelling.CoolingOperatingAirFlow_Lps));
                }
            }

            if (mechanicalVentilationMaterialisation.GuidanceCoolings.Count != dictionary_Cooled.Count)
            {
                refusals.Add(string.Format("SAM_Systems gave manufacturer-guidance cooling to {0} unit(s) for {1} cooled dwelling(s).", mechanicalVentilationMaterialisation.GuidanceCoolings.Count, dictionary_Cooled.Count));
            }

            if (refusals.Count != 0)
            {
                refusal = Join("The mixed TAS Systems ventilation does not match SAM's cooled dwellings.", refusals);
                return null;
            }

            return mechanicalVentilationMaterialisation;
        }

        private static string Join(string text, IEnumerable<string?>? reasons)
        {
            List<string> reasons_Temp = [.. (reasons ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!)];

            return reasons_Temp.Count == 0 ? text : string.Format("{0} {1}", text, string.Join(" ", reasons_Temp));
        }
    }

    public static partial class Query
    {
        /// <summary>The thermostat bridge of a mixed Systems-route run - its results are the run's results, and name its run model.</summary>
        internal static string Path_PartOMixedBridgeTBD(string directory, string projectName)
        {
            return Path.Combine(directory, projectName + "_Bridge.tbd");
        }

        /// <summary>
        /// Why TAS's read-back of the cooled units does not account for them: it must be complete, and hold exactly one
        /// record per guidance unit, each on that unit's own air system (never another dwelling's).
        /// </summary>
        internal static List<string> PartOMixedGuidanceReadBackRefusals(MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation, GuidanceCoolingResults? guidanceCoolingResults)
        {
            List<string> result = [];

            HashSet<Guid> guids_AirSystem = [.. mechanicalVentilationMaterialisation.GuidanceCoolings.Select(x => x.Guid_AirSystem)];

            if (guidanceCoolingResults is null || !guidanceCoolingResults.IsComplete)
            {
                result.Add("TAS returned no complete manufacturer-guidance operation record.");
                result.AddRange(guidanceCoolingResults?.Refusals ?? []);
                return result;
            }

            if (guidanceCoolingResults.Results.Count != guids_AirSystem.Count)
            {
                result.Add(string.Format("TAS returned {0} manufacturer-guidance operation record(s) for {1} cooled unit(s).", guidanceCoolingResults.Results.Count, guids_AirSystem.Count));
            }

            foreach (GuidanceCoolingResult guidanceCoolingResult in guidanceCoolingResults.Results)
            {
                if (!guids_AirSystem.Contains(guidanceCoolingResult.Guid_AirSystem))
                {
                    result.Add(string.Format("TAS returned manufacturer-guidance operation for air system '{0}', which is not a cooled unit's.", guidanceCoolingResult.Name));
                }
            }

            return result;
        }
    }
}
