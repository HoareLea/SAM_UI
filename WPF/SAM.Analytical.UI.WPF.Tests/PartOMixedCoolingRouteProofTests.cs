// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR3A investigation only (27 Sep 2026), licensed TAS: one materialised mixed model (Flat 1 Natural, Flat 2 and
    /// Flat 3 generic MVHR, communal corridor) simulated on the IZAM route and on the Iteration 3 Systems route
    /// (no-IZAM source, SAM_Systems MV, TPD, bridge), plus the Systems route with DisplacementVentilation cleared.
    /// Needs <c>SAM_PARTO_PR3_ROUTE_PROOF</c> (output folder) and <c>SAM_PARTO_MIXED_BASELINE</c> (clean baseline);
    /// without them it does nothing.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOMixedCoolingRouteProofTests
    {
        [WpfFact]
        public void Investigation_IzamAndSystemsRoute_OverOneMixedModel()
        {
            string directory = Environment.GetEnvironmentVariable("SAM_PARTO_PR3_ROUTE_PROOF");
            string path_Baseline = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_BASELINE");
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(path_Baseline))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            StringBuilder log = new();
            void Log(string text)
            {
                log.AppendLine(text);
                File.WriteAllText(Path.Combine(directory, "route-proof.log"), log.ToString());
            }

            AnalyticalModel baseline = Core.Convert.ToSAM<AnalyticalModel>(path_Baseline)?.Find(x => x is not null);
            Log("baseline=" + path_Baseline + " clean=" + baseline.IsPartOCleanBaseline(out _));

            List<Zone> zones = baseline.AdjacencyCluster.GetZones();
            Zone Zone(string name) => zones.Find(x => x.Name == name);

            PartODwellingStrategySet set = new();
            set.Set(new PartODwellingStrategy(Zone("Flat 1").Guid, PartOVentilationMode.NaturalVentilation));
            set.Set(new PartODwellingStrategy(Zone("Flat 2").Guid, PartOVentilationMode.MVHR));
            set.Set(new PartODwellingStrategy(Zone("Flat 3").Guid, PartOVentilationMode.MVHR));

            AnalyticalModel withSet = new(baseline);
            withSet.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);

            PartOMaterialisation partOMaterialisation = withSet.MaterialisePartODwellingStrategies();
            Log("materialised=" + partOMaterialisation.IsMaterialised + " " + string.Join(" | ", partOMaterialisation.Refusals.Select(x => x.Message)));
            Log("scenarios=" + string.Join(" | ", partOMaterialisation.OverheatingScenarios.Select(x => x.ToString())));

            AnalyticalModel model = partOMaterialisation.AnalyticalModel;
            AdjacencyCluster cluster = model.AdjacencyCluster;

            foreach (Zone zone in zones.OrderBy(x => x.Name))
            {
                foreach (Space space in cluster.GetRelatedObjects<Space>(cluster.GetObject<Zone>(zone.Guid)) ?? [])
                {
                    int apertures = cluster.GetPanels(space)?.Sum(p => p.Apertures?.Count ?? 0) ?? 0;
                    Log(string.Format("  {0} / {1}: guid={2} apertures={3} terminals={4}", zone.Name, space.Name, space.Guid, apertures, cluster.GetRelatedObjects<VentilationTerminal>(space)?.Count ?? 0));
                }
            }

            PartOSimulationCase partOSimulationCase = PartOSimulationCase.Create(baseline, path_Baseline, null);
            partOSimulationCase.OutputDirectory = directory;

            //---- IZAM route: the PR2 production final run ----
            PartOSimulationContext context_Izam = Create.PartOMixedSimulationContext(baseline, path_Baseline, partOSimulationCase, "Proof_Izam");
            PartOStrategySetSimulation simulation_Izam = Modify.SimulatePartOMaterialisation(model, partOMaterialisation.OverheatingScenarios, context_Izam, CancellationToken.None);
            Log("IZAM tsd=" + simulation_Izam.Path_TSD + " refusal=" + simulation_Izam.Refusal + " elapsed=" + simulation_Izam.Elapsed);
            LogAssessment(Log, "IZAM", simulation_Izam.Assessment);

            //---- Systems route: the Iteration 3 pipeline stages, no A/B comparison ----
            PartOIteration3Pipeline pipeline = new();
            PartOSimulationContext context_Systems = context_Izam.Copy("Proof_Systems");

            DateTime dateTime = DateTime.Now;
            NoIzamThermalSource noIzamThermalSource = pipeline.ThermalSource(model, context_Systems, "Proof_Systems", CancellationToken.None, out AnalyticalModel model_Source, out bool cancelled, out List<string> notes_Source, out string refusal_Source);
            Log("SOURCE complete=" + noIzamThermalSource?.IsComplete + " tsd=" + noIzamThermalSource?.Path_TSD + " refusal=" + refusal_Source + " " + string.Join(" | ", noIzamThermalSource?.Refusals ?? []) + " elapsed=" + (DateTime.Now - dateTime));

            HashSet<Guid> guids_Mvhr = [Zone("Flat 2").Guid, Zone("Flat 3").Guid];
            List<Space> spaces_Mvhr = [];
            foreach (Guid guid in guids_Mvhr)
            {
                spaces_Mvhr.AddRange(cluster.GetRelatedObjects<Space>(cluster.GetObject<Zone>(guid)) ?? []);
            }

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = pipeline.Materialise(cluster, spaces_Mvhr);
            Log("SAM_Systems materialised=" + mechanicalVentilationMaterialisation.IsMaterialised + " bindings=" + mechanicalVentilationMaterialisation.Bindings.Count + " " + string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            Route(Log, pipeline, noIzamThermalSource, mechanicalVentilationMaterialisation, model_Source, partOMaterialisation.OverheatingScenarios, directory, "Proof_Systems");

            //---- The same Systems route with DisplacementVentilation cleared on every system zone (SAM#129) ----
            string json = mechanicalVentilationMaterialisation.SystemEnergyCentre.ToJsonObject().ToJsonString();
            int count_True = json.Split(["\"DisplacementVentilation\":true"], StringSplitOptions.None).Length - 1;
            string json_Off = json.Replace("\"DisplacementVentilation\":true", "\"DisplacementVentilation\":false");
            Core.Systems.SystemEnergyCentre systemEnergyCentre_Off = new(JsonNode.Parse(json_Off).AsObject());
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation_Off = new(systemEnergyCentre_Off, mechanicalVentilationMaterialisation.Refusals, mechanicalVentilationMaterialisation.Notes, mechanicalVentilationMaterialisation.Bindings);
            Log("DV-off: " + count_True + " system zone(s) cleared");

            Route(Log, pipeline, noIzamThermalSource, mechanicalVentilationMaterialisation_Off, model_Source, partOMaterialisation.OverheatingScenarios, directory, "Proof_SystemsDVOff");

            Log("DONE");
        }

        private static void Route(Action<string> log, PartOIteration3Pipeline pipeline, NoIzamThermalSource noIzamThermalSource, MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation, AnalyticalModel model_Source, List<OverheatingScenario> overheatingScenarios, string directory, string name)
        {
            DateTime dateTime = DateTime.Now;
            SystemVentilationRoute systemVentilationRoute = pipeline.Route(noIzamThermalSource, mechanicalVentilationMaterialisation, Path.Combine(directory, name + ".tpd"), 0, PartOSimulationContext.HourCount_FullYear - 1);
            log(name + " ROUTE complete=" + systemVentilationRoute?.IsComplete + " " + string.Join(" | ", systemVentilationRoute?.Refusals ?? []) + " elapsed=" + (DateTime.Now - dateTime));
            if (systemVentilationRoute is null || !systemVentilationRoute.IsComplete)
            {
                return;
            }

            dateTime = DateTime.Now;
            string path_Bridge = Path.Combine(directory, name + "-Bridge.tbd");
            ResultantTemperatureResults resultantTemperatureResults = pipeline.ResultantTemperatures(systemVentilationRoute, path_Bridge);
            log(name + " BRIDGE complete=" + resultantTemperatureResults?.IsComplete + " rooms=" + resultantTemperatureResults?.Results.Count + " " + string.Join(" | ", resultantTemperatureResults?.Refusals ?? []) + " elapsed=" + (DateTime.Now - dateTime));
            if (resultantTemperatureResults is null || !resultantTemperatureResults.IsComplete)
            {
                return;
            }

            string path_TSD = Path.ChangeExtension(path_Bridge, ".tsd");
            LogAssessment(log, name, PartOTM59Assessment.Assess(model_Source, path_TSD, overheatingScenarios));
        }

        private static void LogAssessment(Action<string> log, string name, PartOTM59Assessment partOTM59Assessment)
        {
            if (partOTM59Assessment is null || !partOTM59Assessment.IsAssessed)
            {
                log(name + " TM59 NOT ASSESSED " + partOTM59Assessment?.Refusal);
                return;
            }

            log(name + " TM59 overall=" + partOTM59Assessment.OccupiedSpaceComplianceStatus + " corridor=" + partOTM59Assessment.Report?.CorridorRiskStatus);
            foreach (KeyValuePair<Guid, TM59ComplianceStatus> keyValuePair in partOTM59Assessment.OccupiedSpaceStatuses ?? [])
            {
                log("   " + keyValuePair.Key + " " + keyValuePair.Value);
            }

            log(partOTM59Assessment.Report?.ToString());
        }
    }
}
