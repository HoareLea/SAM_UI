// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Systems;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Mixed Part O PR3B acceptance gate, licensed TAS (SAM PR3B record §4). One clean PR2 baseline: Flat 1 Natural,
    /// Flat 2 MVHR uncooled, Flat 3 MVHR cooled (the product's manufacturer guidance), communal corridor free-running.
    /// SAM materialises the strategies with the catalogue's descriptors and templates; SAM_Systems builds ONE graph
    /// with the MVRE arrangement for Flat 3's unit only; SAM_Tas builds ONE TPD; the bridge and TM59 follow with the
    /// materialiser's own scenarios. Then the cooling intent is removed and the model rebuilt from the same clean
    /// baseline, which must leave no cooling anywhere, and the baseline must be byte-for-byte unchanged.
    /// <para>
    /// The mixed SAM_Systems call is made here exactly as <see cref="PartOIteration3Pipeline.Materialise"/> makes its
    /// call (same topology, schedule, name and flags) plus <c>GuidanceTemplate</c> - wiring it into the product is PR3C.
    /// </para>
    /// Needs <c>SAM_PARTO_PR3B_GATE</c> (output folder) and <c>SAM_PARTO_MIXED_BASELINE</c> (the clean baseline).
    /// Optional: <c>SAM_PARTO_CATALOGUE</c> (catalogue folder, default the installed one), <c>SAM_PARTO_MIXED_ACCEPTED</c>
    /// (the baseline with Flat 3's accepted Optimised design, for the retained-design + cooling case) and
    /// <c>SAM_PARTO_LEGACY_RUN</c> (a saved Iteration 1a run folder, copied, for the B0 / MG re-acceptance with
    /// DisplacementVentilation = false). Without the two required variables it does nothing.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOMixedCoolingGateTests
    {
        [WpfFact]
        public void Gate_MixedCooling_OneModelOneTpd()
        {
            string directory = Environment.GetEnvironmentVariable("SAM_PARTO_PR3B_GATE");
            string path_Baseline = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_BASELINE");
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(path_Baseline))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            StringBuilder log = new();
            List<string> failures = [];
            void Log(string text)
            {
                log.AppendLine(text);
                File.WriteAllText(Path.Combine(directory, "gate.log"), log.ToString());
            }
            void Check(bool condition, string text)
            {
                Log((condition ? "PASS " : "FAIL ") + text);
                if (!condition)
                {
                    failures.Add(text);
                }
            }

            string sha_Baseline_Before = Sha256(path_Baseline);
            AnalyticalModel baseline = Core.Convert.ToSAM<AnalyticalModel>(path_Baseline)?.Find(x => x is not null);
            string json_Baseline_Before = baseline.ToJsonObject().ToJsonString();
            Check(baseline.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> refusals_Clean), "clean baseline " + path_Baseline + " " + string.Join(" | ", refusals_Clean?.Select(x => x.Message) ?? []));

            //---- The catalogue: descriptors select the units, templates carry the manufacturer guidance ----
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(Environment.GetEnvironmentVariable("SAM_PARTO_CATALOGUE"));
            Log("catalogue=" + ventilationUnitCatalogue.Path + " state=" + ventilationUnitCatalogue.State + " sha=" + ventilationUnitCatalogue.Sha256);
            List<VentilationUnitCapacityDescriptor> descriptors = ventilationUnitCatalogue.CapacityDescriptors;
            List<VentilationUnitTemplate> templates = ventilationUnitCatalogue.Templates;
            VentilationUnitTemplate template_Cooled = templates.Find(x => x.OperatingStrategy is not null && descriptors.Exists(d => d.VentilationUnitReference?.ToString() == x.VentilationUnitReference?.ToString()));
            Check(template_Cooled is not null, "a catalogue product with manufacturer cooling guidance: " + template_Cooled?.VentilationUnitReference + " + " + template_Cooled?.CoolingModuleModel);
            VentilationUnitReference reference_Cooled = template_Cooled.VentilationUnitReference;

            List<Zone> zones = baseline.AdjacencyCluster.GetZones();
            Zone Zone(string name) => zones.Find(x => x.Name == name);
            Zone zone_Flat1 = Zone("Flat 1"), zone_Flat2 = Zone("Flat 2"), zone_Flat3 = Zone("Flat 3");
            Zone zone_Corridor = zones.Find(x => x != zone_Flat1 && x != zone_Flat2 && x != zone_Flat3);

            //---- 1. Materialise Natural / uncooled MVHR / cooled MVHR ----
            PartOMaterialisation cooled = Materialise(baseline, descriptors, templates,
                new PartODwellingStrategy(zone_Flat1.Guid, PartOVentilationMode.NaturalVentilation),
                new PartODwellingStrategy(zone_Flat2.Guid, PartOVentilationMode.MVHR),
                new PartODwellingStrategy(zone_Flat3.Guid, PartOVentilationMode.MVHR, reference_Cooled, PartOActiveCooling.SupplyAirCooling));
            Log("materialised=" + cooled.IsMaterialised + " route=" + cooled.Route + " " + string.Join(" | ", cooled.Refusals.Select(x => x.Reason + ": " + x.Message)));
            Check(cooled.IsMaterialised, "one complete analytical model materialised");
            Check(cooled.Route == PartOSimulationRoute.Systems, "route recorded Systems");

            PartOCooledDwelling cooledDwelling = cooled.Record?.CooledDwellings.SingleOrDefault();
            Check(cooled.Record?.CooledDwellings.Count == 1 && cooledDwelling?.ZoneGuid == zone_Flat3.Guid, "exactly one cooled dwelling, Flat 3");
            Log(string.Format("Flat 3: product {0}, design duty {1} / {2} l/s, cooling operating airflow {3} l/s", cooledDwelling?.VentilationUnitReference, cooledDwelling?.DesignSupply_Lps, cooledDwelling?.DesignExtract_Lps, cooledDwelling?.CoolingOperatingAirFlow_Lps));

            Dictionary<Guid, OverheatingScenario> scenario_By_Zone = cooled.OverheatingScenarios.ToDictionary(x => x.ZoneGuid);
            Log("scenarios=" + string.Join(" | ", cooled.OverheatingScenarios.Select(x => zones.Find(z => z.Guid == x.ZoneGuid)?.Name + ": " + x.Iteration + " " + x.Key)));
            Check(Iteration(scenario_By_Zone, zone_Flat1) == PartOIteration.BaseNaturalVentilation, "Flat 1 scenario BaseNaturalVentilation");
            Check(Iteration(scenario_By_Zone, zone_Flat2) == PartOIteration.BasePassive, "Flat 2 scenario BasePassive (uncooled MVHR)");
            Check(Iteration(scenario_By_Zone, zone_Flat3) == PartOIteration.ActiveTrimCooling, "Flat 3 scenario ActiveTrimCooling");
            Check(zone_Corridor is null || Iteration(scenario_By_Zone, zone_Corridor) == PartOIteration.DwellingIndependent, "corridor scenario DwellingIndependent");

            AnalyticalModel model = cooled.AnalyticalModel;
            AdjacencyCluster cluster = model.AdjacencyCluster;
            List<Space> Spaces(AdjacencyCluster adjacencyCluster, Zone zone) => adjacencyCluster.GetRelatedObjects<Space>(adjacencyCluster.GetObject<Zone>(zone.Guid)) ?? [];

            //---- 2. The thermal source, then ONE SAM_Systems graph: MV for Flat 2, MVRE + guidance for Flat 3 ----
            PartOSimulationCase partOSimulationCase = PartOSimulationCase.Create(baseline, path_Baseline, null);
            partOSimulationCase.OutputDirectory = directory;
            PartOSimulationContext context = Create.PartOMixedSimulationContext(baseline, path_Baseline, partOSimulationCase, "Gate_Systems");

            PartOIteration3Pipeline pipeline = new();
            DateTime dateTime = DateTime.Now;
            NoIzamThermalSource noIzamThermalSource = pipeline.ThermalSource(model, context, "Gate_Systems", CancellationToken.None, out AnalyticalModel model_Source, out bool _, out List<string> _, out string refusal_Source);
            Check(noIzamThermalSource?.IsComplete == true, "thermal source complete " + refusal_Source + " " + string.Join(" | ", noIzamThermalSource?.Refusals ?? []) + " (" + (DateTime.Now - dateTime) + ")");

            MechanicalVentilationGuidanceSettings guidanceSettings = template_Cooled.MechanicalVentilationGuidanceSettings(cooledDwelling.DesignSupply_Lps, cooledDwelling.DesignExtract_Lps, out string refusal_Guidance);
            Check(guidanceSettings is not null, "guidance settings at SAM's rule " + refusal_Guidance);

            List<Space> spaces_Mvhr = [.. Spaces(cluster, zone_Flat2), .. Spaces(cluster, zone_Flat3)];
            MechanicalVentilationMaterialisation mixed = Mixed(cluster, spaces_Mvhr, new() { { cooledDwelling.AirHandlingUnitGuid, guidanceSettings } });
            Check(mixed.IsMaterialised, "one SAM_Systems graph materialised " + string.Join(" | ", mixed.Refusals));
            Check(mixed.GuidanceCoolings.Count == 1 && mixed.GuidanceCoolings[0].Guid_AirHandlingUnit == cooledDwelling.AirHandlingUnitGuid, "exactly one guidance-cooled unit, Flat 3's");
            Check(Math.Abs(mixed.GuidanceCoolings.Single().ElevatedAirFlow_Lps - cooledDwelling.CoolingOperatingAirFlow_Lps) < 1e-9, "the unit operates at the recorded cooling operating airflow " + mixed.GuidanceCoolings.Single().ElevatedAirFlow_Lps + " l/s");

            List<SystemSpace> systemSpaces = mixed.SystemEnergyCentre.GetSystemPlantRooms().SelectMany(x => x.GetSystemComponents<SystemSpace>() ?? []).ToList();
            Check(systemSpaces.Count != 0 && systemSpaces.TrueForAll(x => !x.DisplacementVentilation), "DisplacementVentilation = false on all " + systemSpaces.Count + " system zones");
            Check(mixed.SystemEnergyCentre.GetSystemPlantRooms().Count == 1, "one plant room");

            Dictionary<Guid, Guid> airSystem_By_Unit = mixed.Bindings.Where(x => x.BindingType == MechanicalVentilationBindingType.AirSystem).ToDictionary(x => x.Guid_Analytical, x => x.Guid_Systems);
            SystemPlantRoom systemPlantRoom = mixed.SystemEnergyCentre.GetSystemPlantRooms().Single();
            foreach (KeyValuePair<Guid, Guid> keyValuePair in airSystem_By_Unit)
            {
                AirSystem airSystem = systemPlantRoom.GetSystems<AirSystem>().Single(x => x.Guid == keyValuePair.Value);
                int coils = (systemPlantRoom.GetSystemComponents<ISystemComponent>(airSystem) ?? []).Count(x => x is SystemDXCoil);
                bool isFlat3 = keyValuePair.Key == cooledDwelling.AirHandlingUnitGuid;
                Check(coils == (isFlat3 ? 1 : 0), (isFlat3 ? "Flat 3" : "Flat 2") + " air system DX coils = " + coils);
            }

            //---- 3. ONE TPD, the bridge, TM59 with the materialiser's scenarios ----
            dateTime = DateTime.Now;
            SystemVentilationRoute systemVentilationRoute = pipeline.Route(noIzamThermalSource, mixed, Path.Combine(directory, "Gate_Systems.tpd"), 0, PartOSimulationContext.HourCount_FullYear - 1);
            Check(systemVentilationRoute?.IsComplete == true, "one TPD Systems document simulated " + string.Join(" | ", systemVentilationRoute?.Refusals ?? []) + " (" + (DateTime.Now - dateTime) + ")");

            List<GuidanceCoolingResult> guidanceCoolingResults = systemVentilationRoute?.GuidanceCoolingResults?.Results ?? [];
            Check(guidanceCoolingResults.Count == 1 && guidanceCoolingResults[0].Guid_AirSystem == airSystem_By_Unit[cooledDwelling.AirHandlingUnitGuid], "guidance read-back for 1 unit, Flat 3's air system");
            if (guidanceCoolingResults.Count == 1)
            {
                GuidanceCoolingResult guidanceCoolingResult = guidanceCoolingResults[0];
                double sensible_Max = guidanceCoolingResult.DXSensible_W.Count == 0 ? 0 : guidanceCoolingResult.DXSensible_W.Max(Math.Abs);
                int hours_Cooling = guidanceCoolingResult.DXSensible_W.Count(x => Math.Abs(x) > 1e-6);
                Check(hours_Cooling > 0, string.Format("Flat 3 receives cooling: {0} h with DX load, peak sensible {1:0} W, elevated {2} l/s", hours_Cooling, sensible_Max, guidanceCoolingResult.Elevated_Lps));
                File.WriteAllText(Path.Combine(directory, "Gate_Systems-guidance.csv"), systemVentilationRoute.GuidanceCoolingResults.ToCsv());
            }

            PartOTM59Assessment partOTM59Assessment = null;
            if (systemVentilationRoute?.IsComplete == true)
            {
                dateTime = DateTime.Now;
                string path_Bridge = Path.Combine(directory, "Gate_Systems-Bridge.tbd");
                ResultantTemperatureResults resultantTemperatureResults = pipeline.ResultantTemperatures(systemVentilationRoute, path_Bridge);
                Check(resultantTemperatureResults?.IsComplete == true, "bridge complete, rooms=" + resultantTemperatureResults?.Results.Count + " " + string.Join(" | ", resultantTemperatureResults?.Refusals ?? []) + " (" + (DateTime.Now - dateTime) + ")");

                if (resultantTemperatureResults?.IsComplete == true)
                {
                    partOTM59Assessment = PartOTM59Assessment.Assess(model_Source, Path.ChangeExtension(path_Bridge, ".tsd"), cooled.OverheatingScenarios);
                }
            }

            Check(partOTM59Assessment?.IsAssessed == true, "TM59 assessed " + partOTM59Assessment?.Refusal);
            if (partOTM59Assessment?.IsAssessed == true)
            {
                Log("TM59 overall=" + partOTM59Assessment.OccupiedSpaceComplianceStatus + " corridor=" + partOTM59Assessment.Report?.CorridorRiskStatus);

                Dictionary<Guid, string> zone_By_Space = [];
                foreach (Zone zone in new[] { zone_Flat1, zone_Flat2, zone_Flat3 })
                {
                    Spaces(model_Source.AdjacencyCluster, zone).ForEach(x => zone_By_Space[x.Guid] = zone.Name);
                }

                foreach (PartOTM59SpaceResult partOTM59SpaceResult in partOTM59Assessment.SpaceResults ?? [])
                {
                    zone_By_Space.TryGetValue(partOTM59SpaceResult.SpaceGuid_Design, out string name_Zone);
                    Log(string.Format("   {0} / {1}: {2} {3} {4}/{5} {6}", name_Zone ?? "?", partOTM59SpaceResult.SpaceName, partOTM59SpaceResult.Mechanical ? "mechanical" : "natural", partOTM59SpaceResult.Check, partOTM59SpaceResult.Actual, partOTM59SpaceResult.Limit, partOTM59SpaceResult.ComplianceStatus));
                    if (name_Zone is not null)
                    {
                        Check(partOTM59SpaceResult.Mechanical == (name_Zone != "Flat 1"), name_Zone + " / " + partOTM59SpaceResult.SpaceName + " on the " + (name_Zone == "Flat 1" ? "natural" : "mechanical") + " TM59 criterion");
                    }
                }

                Log(partOTM59Assessment.Report?.ToString());
            }

            //---- 4. Cooling intent removed: rebuilt from the same clean baseline, no generated cooling anywhere ----
            PartOMaterialisation uncooled = Materialise(baseline, descriptors, templates,
                new PartODwellingStrategy(zone_Flat1.Guid, PartOVentilationMode.NaturalVentilation),
                new PartODwellingStrategy(zone_Flat2.Guid, PartOVentilationMode.MVHR),
                new PartODwellingStrategy(zone_Flat3.Guid, PartOVentilationMode.MVHR, reference_Cooled));
            Check(uncooled.IsMaterialised && uncooled.Record?.CooledDwellings.Count == 0 && uncooled.Route == PartOSimulationRoute.Izam, "cooling removed: no cooled dwelling, route Izam");
            Check(Iteration(uncooled.OverheatingScenarios.ToDictionary(x => x.ZoneGuid), zone_Flat3) == PartOIteration.BasePassive, "cooling removed: Flat 3 scenario BasePassive");
            //Every materialisation mints fresh guids, so the engineering state is compared guid-free and order-free - with
            //the same comparison of two uncooled rebuilds as the control that it is a fair one.
            PartOMaterialisation uncooled_Again = Materialise(baseline, descriptors, templates,
                new PartODwellingStrategy(zone_Flat1.Guid, PartOVentilationMode.NaturalVentilation),
                new PartODwellingStrategy(zone_Flat2.Guid, PartOVentilationMode.MVHR),
                new PartODwellingStrategy(zone_Flat3.Guid, PartOVentilationMode.MVHR, reference_Cooled));
            Check(EngineeringState(uncooled_Again.AnalyticalModel) == EngineeringState(uncooled.AnalyticalModel), "control: two uncooled rebuilds have the same engineering state (guid- and order-free)");
            Check(EngineeringState(uncooled.AnalyticalModel) == EngineeringState(model), "cooling removed: the rebuilt model's engineering state equals the cooled one's (cooling lives only in the record)");

            List<Space> spaces_Mvhr_Uncooled = [.. Spaces(uncooled.AnalyticalModel.AdjacencyCluster, zone_Flat2), .. Spaces(uncooled.AnalyticalModel.AdjacencyCluster, zone_Flat3)];
            MechanicalVentilationMaterialisation plain = pipeline.Materialise(uncooled.AnalyticalModel.AdjacencyCluster, spaces_Mvhr_Uncooled);
            int coils_Plain = plain.SystemEnergyCentre?.GetSystemPlantRooms().Sum(x => (x.GetSystemComponents<SystemDXCoil>() ?? []).Count) ?? -1;
            Check(plain.IsMaterialised && plain.GuidanceCoolings.Count == 0 && coils_Plain == 0, "cooling removed: SAM_Systems graph has no guidance record and no DX coil");

            //---- 5. Optimised (retained design) + cooling: the accepted design is never altered to fit the range ----
            string path_Accepted = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_ACCEPTED");
            if (!string.IsNullOrWhiteSpace(path_Accepted) && File.Exists(path_Accepted))
            {
                string sha_Accepted_Before = Sha256(path_Accepted);
                AnalyticalModel accepted = Core.Convert.ToSAM<AnalyticalModel>(path_Accepted)?.Find(x => x is not null);
                Zone zone_Flat3_Accepted = accepted.AdjacencyCluster.GetZones().Find(x => x.Name == "Flat 3");
                string fingerprint = accepted.AdjacencyCluster.PartODwellingDesignFingerprint(zone_Flat3_Accepted);
                double design_Lps = accepted.AdjacencyCluster.GetRelatedObjects<Space>(zone_Flat3_Accepted)?.SelectMany(x => accepted.AdjacencyCluster.GetRelatedObjects<VentilationTerminal>(x) ?? []).Where(x => x.FlowClassification == FlowClassification.Supply).Sum(x => x.DesignFlowRate_Lps ?? 0) ?? 0;

                PartOMaterialisation retained = Materialise(accepted, descriptors, templates,
                    new PartODwellingStrategy(accepted.AdjacencyCluster.GetZones().Find(x => x.Name == "Flat 1").Guid, PartOVentilationMode.NaturalVentilation),
                    new PartODwellingStrategy(accepted.AdjacencyCluster.GetZones().Find(x => x.Name == "Flat 2").Guid, PartOVentilationMode.MVHR),
                    new PartODwellingStrategy(zone_Flat3_Accepted.Guid, PartOVentilationMode.MVHR, reference_Cooled, PartOActiveCooling.SupplyAirCooling, PartODesignAirFlowBasis.RetainedDesign, fingerprint));
                Log(string.Format("Optimised + cooled: Flat 3 accepted supply design {0:0.#} l/s -> materialised={1} {2}", design_Lps, retained.IsMaterialised, string.Join(" | ", retained.Refusals.Select(x => x.Reason + ": " + x.Message))));
                bool inRange = design_Lps >= 60 - 1e-9 && design_Lps <= 120 + 1e-9;
                Check(inRange ? retained.IsMaterialised && retained.Record?.CooledDwellings.Count == 1 : !retained.IsMaterialised && retained.Refusals.Exists(x => x.Reason == PartOMaterialisationRefusalReason.CoolingAirFlowOutsideGuidance),
                    inRange ? "Optimised + cooled inside the published range is materialised" : "Optimised + cooled beyond the published range is refused (CoolingAirFlowOutsideGuidance), the design untouched");
                Check(Sha256(path_Accepted) == sha_Accepted_Before, "accepted fixture unchanged");
            }

            //---- 6. The clean source baseline is unchanged ----
            Check(Sha256(path_Baseline) == sha_Baseline_Before, "baseline file unchanged (SHA-256 " + sha_Baseline_Before + ")");
            Check(baseline.ToJsonObject().ToJsonString() == json_Baseline_Before, "baseline object unchanged by every materialisation");

            Log(failures.Count == 0 ? "GATE PASSED" : "GATE FAILED: " + failures.Count);
            Assert.Empty(failures);
        }

        /// <summary>
        /// Legacy Iteration 3 re-acceptance (SAM PR3B record §4): B0 (Parity) and MG (manufacturer guidance) rerun on
        /// licensed TAS from a COPY of a saved Iteration 1a run, now with DisplacementVentilation = false on every
        /// system zone (SAM_Systems PR3B-2). The saved run folder is never written. Reports the outcome for comparison
        /// with the saved reviews; the accepted ventilation design is not changed.
        /// </summary>
        [WpfFact]
        public void Gate_LegacyIteration3_B0AndMG_DisplacementOff()
        {
            string directory = Environment.GetEnvironmentVariable("SAM_PARTO_PR3B_GATE");
            string directory_Run = Environment.GetEnvironmentVariable("SAM_PARTO_LEGACY_RUN");
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(directory_Run) || !Directory.Exists(directory_Run))
            {
                return;
            }

            StringBuilder log = new();
            void Log(string text)
            {
                log.AppendLine(text);
                File.WriteAllText(Path.Combine(directory, "legacy.log"), log.ToString());
            }

            Directory.CreateDirectory(directory);
            List<string> failures = [];

            foreach ((string name, PartOIteration3BehaviourMode mode) in new[] { ("Legacy-B0", PartOIteration3BehaviourMode.Parity), ("Legacy-MG", PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance) })
            {
                string directory_Copy = Path.Combine(directory, name);
                if (Directory.Exists(directory_Copy))
                {
                    Directory.Delete(directory_Copy, true);
                }

                Directory.CreateDirectory(directory_Copy);
                foreach (string path in Directory.GetFiles(directory_Run))
                {
                    if (path.EndsWith(".sam", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tbd", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tsd", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".t3d", StringComparison.OrdinalIgnoreCase) || path.EndsWith("-TM59.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        //Only the Iteration 1a run itself - no Iteration 3 artefacts, so nothing stale is reviewed.
                        if (Path.GetFileName(path).Contains("-It3") || Path.GetFileName(path).Contains("Iteration3"))
                        {
                            continue;
                        }

                        File.Copy(path, Path.Combine(directory_Copy, Path.GetFileName(path)));
                    }
                }

                string path_Model = Directory.GetFiles(directory_Copy, "*.partorun.json").Select(x => x.Substring(0, x.Length - ".partorun.json".Length) + ".sam").Single(File.Exists);
                AnalyticalModel analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path_Model).First();

                PartORun partORun = new();
                bool restored = partORun.Restore(analyticalModel, path_Model, out string refusal_Restore);
                Log(name + " restored=" + restored + " " + refusal_Restore);
                if (!restored)
                {
                    failures.Add(name + " not restored");
                    continue;
                }

                DateTime dateTime = DateTime.Now;
                PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, new PartOIteration3Pipeline(), CancellationToken.None, mode);
                Log(string.Format("{0} ({1}) complete={2} in {3}: {4}", name, mode, partOIteration3Result.IsComplete, DateTime.Now - dateTime, PartOIteration3ReportText.Outcome(partOIteration3Result)));
                Log("  " + (partOIteration3Result.IsComplete
                    ? PartOIteration3ReportText.Comparison(partOIteration3Result, System.Globalization.CultureInfo.InvariantCulture)
                    : PartOIteration3ReportText.Refusal(partOIteration3Result).Replace(Environment.NewLine, " / ")));
                Log("  report=" + partOIteration3Result.Path_Report);

                if (!partOIteration3Result.IsComplete)
                {
                    failures.Add(name + " not complete");
                }
            }

            Log(failures.Count == 0 ? "LEGACY RE-RUN COMPLETE" : "LEGACY RE-RUN FAILED: " + string.Join(", ", failures));
            Assert.Empty(failures);
        }

        private static PartOMaterialisation Materialise(AnalyticalModel baseline, IEnumerable<VentilationUnitCapacityDescriptor> descriptors, IEnumerable<VentilationUnitTemplate> templates, params PartODwellingStrategy[] strategies)
        {
            PartODwellingStrategySet set = new();
            foreach (PartODwellingStrategy strategy in strategies)
            {
                set.Set(strategy);
            }

            AnalyticalModel withSet = new(baseline);
            withSet.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);

            return withSet.MaterialisePartODwellingStrategies(descriptors, null, templates);
        }

        /// <summary>The call <see cref="PartOIteration3Pipeline.Materialise"/> makes, plus the MVRE guidance topology (PR3B-2).</summary>
        private static MechanicalVentilationMaterialisation Mixed(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, Dictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings)
        {
            MechanicalVentilationSettings mechanicalVentilationSettings = new()
            {
                Schedule = Query.PartOIteration3OperatingSchedule(),
                Name = PartOIteration3Pipeline.Name_SystemEnergyCentre,
                MaterialiseSystemSpaceComponents = false,
                GuidanceTemplate = new SystemTemplate(PartOIteration3Pipeline.Ventilation_Template_ManufacturerAware, null, null, null, null, null).SystemEnergyCentre(),
                GuidanceSettings = guidanceSettings,
            };

            SystemEnergyCentre systemEnergyCentre = new SystemTemplate(PartOIteration3Pipeline.Ventilation_Template, null, null, null, null, null).SystemEnergyCentre();

            return adjacencyCluster.MechanicalVentilation(systemEnergyCentre, mechanicalVentilationSettings, spaces);
        }

        private static PartOIteration? Iteration(Dictionary<Guid, OverheatingScenario> scenario_By_Zone, Zone zone)
        {
            return zone is not null && scenario_By_Zone.TryGetValue(zone.Guid, out OverheatingScenario overheatingScenario) ? overheatingScenario.Iteration : null;
        }

        /// <summary>The model's cluster JSON with every guid masked and its lines sorted - guid- and order-free.</summary>
        private static string EngineeringState(AnalyticalModel analyticalModel)
        {
            string json = analyticalModel.AdjacencyCluster.ToJsonObject().ToJsonString().Replace(",\"", ",\n\"");
            json = System.Text.RegularExpressions.Regex.Replace(json, "[0-9a-fA-F]{8}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{12}", "G");
            List<string> lines = [.. json.Split('\n')];
            lines.Sort(StringComparer.Ordinal);
            return string.Join("\n", lines);
        }

        private static string Sha256(string path)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream fileStream = File.OpenRead(path);
            return BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", string.Empty);
        }
    }
}
