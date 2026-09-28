// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR3C licensed acceptance, read-only inspection of what the REAL product UI produced (the run itself is driven through
    /// SAM Analytical.exe by UI Automation - PR3C record). Reads the model the UI saved, its Mixed Design sidecar and the
    /// final run's reopenable model, with SAM's own APIs, and writes what it found. Changes nothing.
    /// <para>
    /// Needs <c>SAM_PARTO_PR3C_MODEL</c> (the model the UI saved) and <c>SAM_PARTO_PR3C_EXPECT</c> (<c>cooled</c> or
    /// <c>uncooled</c>); optional <c>SAM_PARTO_PR3C_LOG</c> (the log file). Without them it does nothing.
    /// </para>
    /// </summary>
    public class PartOMixedCoolingAcceptanceInspection
    {
        [Fact]
        public void Inspect_TheRealUiMixedCoolingRun()
        {
            string path_Model = Environment.GetEnvironmentVariable("SAM_PARTO_PR3C_MODEL");
            string expect = Environment.GetEnvironmentVariable("SAM_PARTO_PR3C_EXPECT");
            if (string.IsNullOrWhiteSpace(path_Model) || string.IsNullOrWhiteSpace(expect))
            {
                return;
            }

            bool cooled = expect == "cooled";
            string path_Log = Environment.GetEnvironmentVariable("SAM_PARTO_PR3C_LOG") ?? Path.ChangeExtension(path_Model, "." + expect + ".inspect.txt");

            StringBuilder log = new();
            List<string> failures = [];
            void Check(bool condition, string text)
            {
                log.AppendLine((condition ? "PASS " : "FAIL ") + text);
                File.WriteAllText(path_Log, log.ToString());
                if (!condition)
                {
                    failures.Add(text);
                }
            }

            // ---- The saved model: still a clean baseline, carrying the selection ------------------------------------

            AnalyticalModel model = Core.Convert.ToSAM<AnalyticalModel>(path_Model).Single();
            Check(model.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> findings), "the saved model is still a clean Part O baseline " + string.Join(" | ", findings?.Select(x => x.Message) ?? []));

            List<Zone> zones = model.AdjacencyCluster.GetZones();
            Zone Z(string name) => zones.Single(x => x.Name == name);
            Zone flat1 = Z("Flat 1"), flat2 = Z("Flat 2"), flat3 = Z("Flat 3");
            Zone corridor = zones.Single(x => x != flat1 && x != flat2 && x != flat3);

            PartODwellingStrategySet set = model.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            log.AppendLine("saved selection: " + string.Join(" | ", new[] { flat1, flat2, flat3 }.Select(x => x.Name + " " + set?.Strategy(x.Guid)?.CanonicalText())));
            Check(set?.Strategy(flat1.Guid)?.VentilationMode == PartOVentilationMode.NaturalVentilation, "saved: Flat 1 Natural");
            Check(set?.Strategy(flat2.Guid)?.VentilationMode == PartOVentilationMode.MVHR && set.Strategy(flat2.Guid).ActiveCooling == PartOActiveCooling.None, "saved: Flat 2 MVHR, cooling off");
            Check(set?.Strategy(flat3.Guid)?.VentilationMode == PartOVentilationMode.MVHR && (set.Strategy(flat3.Guid).ActiveCooling == PartOActiveCooling.SupplyAirCooling) == cooled, "saved: Flat 3 MVHR, cooling " + (cooled ? "on" : "off"));
            Check(set?.Strategy(corridor.Guid) is null, "saved: no strategy for the corridor");

            // ---- The Mixed Design sidecar: the final run's evidence ----------------------------------------------------

            PartOMixedDesignState state = PartOMixedDesignState.Read(PartOMixedDesignState.Path_State(path_Model));
            PartOMixedRunEvidence evidence = state?.FinalRun;
            Check(evidence is not null && evidence.ReadRefusal is null, "the sidecar holds a readable final run " + evidence?.ReadRefusal);
            if (evidence is null)
            {
                Assert.Empty(failures);
                return;
            }

            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();
            Check(evidence.IsCurrent(model, ventilationUnitCatalogue.CapacityDescriptors, ventilationUnitCatalogue.Templates, out string reason), "the final run is current for the saved design, catalogue and templates " + reason);

            log.AppendLine(string.Format("route={0} tpd={1} tsd={2} runModel={3}", evidence.Route, evidence.Path_TPD, evidence.Path_TSD, evidence.Path_RunModel));
            Check(evidence.Route == (cooled ? PartOSimulationRoute.Systems : PartOSimulationRoute.Izam), "route " + (cooled ? "Systems" : "Izam"));
            Check(cooled ? evidence.Record.CooledDwellings.Count == 1 && evidence.Record.CooledDwellings[0].ZoneGuid == flat3.Guid : evidence.Record.CooledDwellings.Count == 0, cooled ? "exactly one cooled dwelling, Flat 3" : "no cooled dwelling");
            if (cooled && evidence.Record.CooledDwellings.Count == 1)
            {
                PartOCooledDwelling cooledDwelling = evidence.Record.CooledDwellings[0];
                log.AppendLine(string.Format("Flat 3: {0}, design duty {1:0.#} / {2:0.#} l/s, cooling operating airflow {3:0.#} l/s", cooledDwelling.VentilationUnitReference, cooledDwelling.DesignSupply_Lps, cooledDwelling.DesignExtract_Lps, cooledDwelling.CoolingOperatingAirFlow_Lps));
            }

            Check(cooled ? evidence.Path_TPD is not null && File.Exists(evidence.Path_TPD) : evidence.Path_TPD is null, cooled ? "one TPD document on disk: " + evidence.Path_TPD : "no TPD for the IZAM route");
            if (cooled && evidence.Path_TPD is not null)
            {
                Check(Directory.GetFiles(Path.GetDirectoryName(evidence.Path_TPD), "*.tpd").Length == 1, "exactly one .tpd in the run folder");
            }

            Check(cooled ? evidence.GuidanceSummaries.Count == 1 : evidence.GuidanceSummaries.Count == 0, "TAS guidance read-back for " + evidence.GuidanceSummaries.Count + " unit(s)");
            evidence.GuidanceSummaries.ForEach(x => log.AppendLine("   read-back: " + x));

            log.AppendLine(string.Format("TM59: overall {0} ({1}); corridor {2}", evidence.Overall, evidence.OccupiedSpaceComplianceStatus, evidence.CorridorText));
            foreach (Zone zone in new[] { flat1, flat2, flat3 })
            {
                PartODwellingResult result = evidence.Result(zone.Guid);
                log.AppendLine(string.Format("   {0}: {1} ({2} pass, {3} fail, {4} not assessed) ran as {5}", zone.Name, result?.Outcome, result?.SpaceCount_Pass, result?.SpaceCount_Fail, result?.SpaceCount_NotAssessed, UI.Query.PartODwellingStrategyText(evidence.Strategies?.Strategy(zone.Guid))));
                Check(result is not null && result.Outcome != PartODwellingOutcome.NotAssessed, zone.Name + " has an assessed TM59 result");
            }

            Check(evidence.SpaceCount_Unassessed == 0, "no space left unassessed (" + evidence.SpaceCount_Unassessed + ")");

            // ---- The run model: reopens through the ordinary review path, with the materialiser's scenarios -----------

            Check(evidence.Path_RunModel is not null && File.Exists(evidence.Path_RunModel), "the run model is on disk: " + evidence.Path_RunModel);
            if (evidence.Path_RunModel is not null && File.Exists(evidence.Path_RunModel))
            {
                AnalyticalModel model_Run = Core.Convert.ToSAM<AnalyticalModel>(evidence.Path_RunModel).Single();
                PartORun partORun = new();
                Check(partORun.Restore(model_Run, evidence.Path_RunModel, out string refusal_Restore), "the run model restores against its results " + refusal_Restore);
                Check(string.Equals(Path.GetFullPath(partORun.Path_TSD ?? "-"), Path.GetFullPath(evidence.Path_TSD), StringComparison.OrdinalIgnoreCase), "restored results are the evidence's: " + partORun.Path_TSD);

                Dictionary<Guid, PartOIteration> iterations = partORun.OverheatingScenarios.ToDictionary(x => x.ZoneGuid, x => x.Iteration);
                log.AppendLine("scenarios: " + string.Join(" | ", partORun.OverheatingScenarios.Select(x => (zones.Find(z => z.Guid == x.ZoneGuid)?.Name ?? x.ZoneGuid.ToString()) + " " + x.Iteration + " " + x.Key)));
                Check(iterations.TryGetValue(flat1.Guid, out PartOIteration i1) && i1 == PartOIteration.BaseNaturalVentilation, "Flat 1 BaseNaturalVentilation");
                Check(iterations.TryGetValue(flat2.Guid, out PartOIteration i2) && i2 == PartOIteration.BasePassive, "Flat 2 BasePassive");
                Check(iterations.TryGetValue(flat3.Guid, out PartOIteration i3) && i3 == (cooled ? PartOIteration.ActiveTrimCooling : PartOIteration.BasePassive), "Flat 3 " + (cooled ? "ActiveTrimCooling" : "BasePassive"));
                Check(iterations.TryGetValue(corridor.Guid, out PartOIteration i4) && i4 == PartOIteration.DwellingIndependent, "corridor DwellingIndependent");

                //No generated cooling on the model itself: cooling exists only on the Systems route, configured from SAM's record.
                Check((model_Run.AdjacencyCluster.GetObjects<AirHandlingUnit>() ?? []).TrueForAll(x => double.IsNaN(x.SummerSupplyTemperature)), "no unit carries a supply setpoint (no cooling on the analytical model)");
            }

            log.AppendLine(failures.Count == 0 ? "INSPECTION PASSED" : "INSPECTION FAILED: " + failures.Count);
            File.WriteAllText(path_Log, log.ToString());
            Assert.Empty(failures);
        }
    }
}
