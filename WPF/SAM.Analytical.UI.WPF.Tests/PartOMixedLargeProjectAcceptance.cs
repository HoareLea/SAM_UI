// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Mixed Part O PR4 licensed acceptance at scale (SAM PR0 §F; SAM_UI PR3A §14.1 "TPD scale"): the real clean baseline
    /// replicated into a large project, one strategy set per block - Flat 1 Natural, Flat 2 MVHR, Flat 3 MVHR + active
    /// cooling (the PR3C acceptance case) - through the PRODUCTION Build &amp; Run (<see cref="Modify.BuildAndRunPartOMixedDesign"/>
    /// with the production simulators) on licensed TAS, every stage timed.
    /// <para>
    /// Every block is the same building far from the others, so the original block's outcome is every block's reference:
    /// each dwelling's TM59 outcome must match it, with the scenario its strategy says. Also checked: one model, one route,
    /// the cooled units' read-back, the run model reopening, and no growth of the saved or materialised model.
    /// </para>
    /// <para>
    /// Needs <c>SAM_PARTO_PR4_DIR</c> (output folder) and <c>SAM_PARTO_MIXED_BASELINE</c> (the clean baseline). Optional:
    /// <c>SAM_PARTO_PR4_COPIES</c> (blocks, default 10), <c>SAM_PARTO_PR4_COOLING</c> (<c>on</c> - the default - puts the
    /// whole building on the Systems route; <c>off</c> is the IZAM route), <c>SAM_PARTO_CATALOGUE</c>. Without the first
    /// two it does nothing.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOMixedLargeProjectAcceptance
    {
        [WpfFact]
        public void Acceptance_LargeMixedProject_OnLicensedTas()
        {
            string directory_Root = Environment.GetEnvironmentVariable("SAM_PARTO_PR4_DIR");
            string path_Source = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_BASELINE");
            if (string.IsNullOrWhiteSpace(directory_Root) || string.IsNullOrWhiteSpace(path_Source))
            {
                return;
            }

            int copies = int.TryParse(Environment.GetEnvironmentVariable("SAM_PARTO_PR4_COPIES"), out int value) && value > 0 ? value : 10;
            bool cooling = !string.Equals(Environment.GetEnvironmentVariable("SAM_PARTO_PR4_COOLING"), "off", StringComparison.OrdinalIgnoreCase);

            string name = string.Format("Large_x{0}_{1}", copies, cooling ? "cooled" : "uncooled");
            string directory = Path.Combine(directory_Root, name);
            Directory.CreateDirectory(directory);

            StringBuilder log = new();
            List<string> failures = [];
            Stopwatch stopwatch_Total = Stopwatch.StartNew();
            void Log(string text)
            {
                log.AppendLine(string.Format("[{0:hh\\:mm\\:ss}] {1}", stopwatch_Total.Elapsed, text));
                File.WriteAllText(Path.Combine(directory_Root, name + ".log"), log.ToString());
            }
            void Check(bool condition, string text)
            {
                Log((condition ? "PASS " : "FAIL ") + text);
                if (!condition)
                {
                    failures.Add(text);
                }
            }

            // ---- The large project ------------------------------------------------------------------------------------
            AnalyticalModel source = Core.Convert.ToSAM<AnalyticalModel>(path_Source).Single();
            string json_Source = source.ToJsonObject().ToJsonString();

            Stopwatch stopwatch = Stopwatch.StartNew();
            AnalyticalModel large = PartOMixedLargeProjectFixture.Replicate(source, copies);
            Log(string.Format("replicated {0} x {1}: {2} spaces, {3} zones, {4} panels in {5:0.0} s", Path.GetFileName(path_Source), copies, large.AdjacencyCluster.GetSpaces().Count, large.AdjacencyCluster.GetZones().Count, large.AdjacencyCluster.GetPanels().Count, stopwatch.Elapsed.TotalSeconds));
            Check(large.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> refusals_Clean), "the large project is a clean baseline " + string.Join(" | ", refusals_Clean?.Select(x => x.Message) ?? []));
            Check(large.AdjacencyCluster.GetSpaces().Select(x => x.Name).Distinct().Count() == large.AdjacencyCluster.GetSpaces().Count, "every space name is unique");

            string path_Model = Path.Combine(directory, name + ".sam");

            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(Environment.GetEnvironmentVariable("SAM_PARTO_CATALOGUE"));
            List<VentilationUnitCapacityDescriptor> descriptors = ventilationUnitCatalogue.CapacityDescriptors;
            List<VentilationUnitTemplate> templates = ventilationUnitCatalogue.Templates;
            VentilationUnitTemplate template_Cooled = templates.Find(x => x.OperatingStrategy is not null && descriptors.Exists(d => d.VentilationUnitReference?.ToString() == x.VentilationUnitReference?.ToString()));
            Log("catalogue=" + ventilationUnitCatalogue.Path + " sha=" + ventilationUnitCatalogue.Sha256 + " cooled product=" + template_Cooled?.VentilationUnitReference);

            // ---- The engineer's selection, by bulk assignment in the session -----------------------------------------
            stopwatch.Restart();
            PartOMixedDesignSession session = new(large, path_Model, descriptors, null, templates) { CatalogueOffered = true };
            TimeSpan open = stopwatch.Elapsed;

            List<PartOMixedDwellingRow> Rows(string flat) => [.. session.Rows.Where(x => PartOMixedLargeProjectFixture.Original(x.Name) == flat)];
            List<PartOMixedDwellingRow> rows_Natural = Rows("Flat 1"), rows_Mvhr = Rows("Flat 2"), rows_Cooled = Rows("Flat 3");
            Check(session.Rows.Count == 3 * copies && rows_Natural.Count == copies && rows_Mvhr.Count == copies && rows_Cooled.Count == copies, string.Format("{0} dwelling rows, never a row per space", session.Rows.Count));

            stopwatch.Restart();
            Check(session.SetNatural(rows_Natural) is null, "Flat 1 of every block: Natural");
            Check(session.SetMvhr(rows_Mvhr, null) is null, "Flat 2 of every block: MVHR (automatic product)");
            Check(session.SetMvhr(rows_Cooled, template_Cooled.VentilationUnitReference) is null, "Flat 3 of every block: MVHR + " + template_Cooled.VentilationUnitReference);
            if (cooling)
            {
                Check(session.SetCooling(rows_Cooled, true) is null, "Flat 3 of every block: active cooling on");
            }
            TimeSpan assign = stopwatch.Elapsed;
            Log(string.Format("session open {0:0} ms, bulk assignment {1:0} ms; readiness: {2}", open.TotalMilliseconds, assign.TotalMilliseconds, session.Readiness().Text));

            AnalyticalModel baseline = session.WithSelection();
            stopwatch.Restart();
            Check(Core.Convert.ToFile(baseline, path_Model, SAMFileType.SAM), "saved " + path_Model);
            long length_Saved = new FileInfo(path_Model).Length;
            Log(string.Format("saved baseline {0:N0} bytes in {1:0.0} s", length_Saved, stopwatch.Elapsed.TotalSeconds));

            // ---- Build & Run: the production route, every stage timed -------------------------------------------------
            PartOSimulationCase partOSimulationCase = PartOSimulationCase.Create(baseline, path_Model, null);
            partOSimulationCase.OutputDirectory = directory;
            string projectName = Create.PartOMixedProjectName(baseline, "Mixed");
            PartOSimulationContext context = Create.PartOMixedSimulationContext(baseline, path_Model, partOSimulationCase, projectName);
            Log("case: " + Query.PartOSimulationCaseKey(context));

            List<(TimeSpan At, string Stage)> stages = [];
            List<(TimeSpan At, long Length)> samples_TPD = [];
            PartOMixedRunEvidence evidence;
            PartOStrategySetRun run;
            using (PartOProgressHost partOProgressHost = new("PR4", null, ["Materialise", "Simulate"], show: false))
            {
                using CancellationTokenSource cancellationTokenSource_Poll = new();
                Stopwatch stopwatch_Run = Stopwatch.StartNew();
                Thread thread = new(() =>
                {
                    string detail = null;
                    long length_TPD = -1;
                    while (!cancellationTokenSource_Poll.IsCancellationRequested)
                    {
                        string detail_Now = partOProgressHost.State.Detail;
                        if (detail_Now != detail)
                        {
                            detail = detail_Now;
                            lock (stages)
                            {
                                stages.Add((stopwatch_Run.Elapsed, detail ?? "(none)"));
                            }
                        }

                        //The TPD's own growth splits its stage: the conversion writes the document, the simulation its results.
                        FileInfo fileInfo_TPD = new(Path.Combine(directory, projectName + ".tpd"));
                        long length_TPD_Now = fileInfo_TPD.Exists ? fileInfo_TPD.Length : -1;
                        if (length_TPD_Now != length_TPD && (samples_TPD.Count == 0 || stopwatch_Run.Elapsed - samples_TPD[^1].At > TimeSpan.FromSeconds(15)))
                        {
                            length_TPD = length_TPD_Now;
                            lock (samples_TPD)
                            {
                                samples_TPD.Add((stopwatch_Run.Elapsed, length_TPD));
                            }
                        }

                        Thread.Sleep(200);
                    }
                })
                { IsBackground = true };
                thread.Start();

                evidence = Modify.BuildAndRunPartOMixedDesign(baseline, true, descriptors, context, CancellationToken.None, out run, null, () => { lock (stages) { stages.Add((stopwatch_Run.Elapsed, "materialised")); } }, templates);

                TimeSpan total = stopwatch_Run.Elapsed;
                cancellationTokenSource_Poll.Cancel();
                thread.Join();
                stages.Add((total, "done"));
            }

            for (int i = 0; i < stages.Count; i++)
            {
                TimeSpan end = i + 1 < stages.Count ? stages[i + 1].At : stages[i].At;
                Log(string.Format("   stage {0,-55} starts {1:hh\\:mm\\:ss}  lasts {2:hh\\:mm\\:ss}", stages[i].Stage, stages[i].At, end - stages[i].At));
            }

            foreach ((TimeSpan At, long Length) sample in samples_TPD)
            {
                Log(string.Format("   tpd {0:hh\\:mm\\:ss} {1,15:N0} bytes", sample.At, sample.Length));
            }

            File.WriteAllLines(Path.Combine(directory_Root, name + ".notes.txt"), run?.Simulation?.Notes ?? []);
            Log(string.Format("{0} run notes written to {1}.notes.txt", run?.Simulation?.Notes?.Count ?? 0, name));

            Check(evidence is not null, "Build & Run completed " + (run?.Simulation?.Refusal ?? run?.Materialisation?.Refusal));
            if (evidence is null)
            {
                run?.Simulation?.Notes?.ForEach(x => Log("   note: " + x));
                Assert.Fail(string.Join(Environment.NewLine, failures));
                return;
            }

            // ---- One model, one route, truthful scenarios, cooling only where intended --------------------------------
            PartOMaterialisation materialisation = run.Materialisation;
            Check(evidence.Route == (cooling ? PartOSimulationRoute.Systems : PartOSimulationRoute.Izam), "route " + evidence.Route);
            Check(materialisation.Record.CooledDwellings.Count == (cooling ? copies : 0), "cooled dwellings recorded: " + materialisation.Record.CooledDwellings.Count);
            Check(!cooling || materialisation.Record.CooledDwellings.All(x => rows_Cooled.Exists(r => r.ZoneGuid == x.ZoneGuid)), "only Flat 3 of each block is cooled");
            Check(!cooling || evidence.GuidanceSummaries.Count == copies, "TAS guidance read-back for " + evidence.GuidanceSummaries.Count + " unit(s)");
            Check(!cooling || (evidence.Path_TPD is not null && File.Exists(evidence.Path_TPD)), "ONE TPD: " + evidence.Path_TPD);
            Check((materialisation.AnalyticalModel.AdjacencyCluster.GetObjects<AirHandlingUnit>() ?? []).TrueForAll(x => double.IsNaN(x.SummerSupplyTemperature)), "no unit supply setpoint on the analytical model");

            Dictionary<Guid, PartOIteration> iterations = materialisation.OverheatingScenarios.ToDictionary(x => x.ZoneGuid, x => x.Iteration);
            Check(rows_Natural.TrueForAll(x => iterations[x.ZoneGuid] == PartOIteration.BaseNaturalVentilation), "every Natural dwelling: BaseNaturalVentilation");
            Check(rows_Mvhr.TrueForAll(x => iterations[x.ZoneGuid] == PartOIteration.BasePassive), "every uncooled MVHR dwelling: BasePassive");
            Check(rows_Cooled.TrueForAll(x => iterations[x.ZoneGuid] == (cooling ? PartOIteration.ActiveTrimCooling : PartOIteration.BasePassive)), "every Flat 3: " + (cooling ? "ActiveTrimCooling" : "BasePassive"));
            List<Zone> zones_Common = [.. baseline.AdjacencyCluster.GetZones().Where(x => !session.Rows.Any(r => r.ZoneGuid == x.Guid))];
            Check(zones_Common.Count == copies && zones_Common.TrueForAll(x => iterations.TryGetValue(x.Guid, out PartOIteration iteration) && iteration == PartOIteration.DwellingIndependent), "every corridor: DwellingIndependent (" + zones_Common.Count + ")");

            // ---- TM59: every dwelling's outcome is its block's reference, every space assessed --------------------------
            Log(string.Format("TM59 overall={0}, corridor={1}, unassessed spaces={2}", evidence.OccupiedSpaceComplianceStatus, evidence.CorridorRiskStatus, evidence.SpaceCount_Unassessed));
            Check(evidence.SpaceCount_Unassessed == 0, "no space left unassessed");

            Dictionary<string, PartODwellingResult> reference = [];
            foreach (PartOMixedDwellingRow row in session.Rows.Where(x => PartOMixedLargeProjectFixture.Block(x.Name) == 0))
            {
                reference[row.Name] = evidence.Result(row.ZoneGuid);
                Log(string.Format("   reference {0}: {1} ({2} pass, {3} fail) - {4}", row.Name, reference[row.Name]?.Outcome, reference[row.Name]?.SpaceCount_Pass, reference[row.Name]?.SpaceCount_Fail, string.Join(", ", reference[row.Name]?.FailingSpaceNames ?? [])));
            }

            List<string> deviations = [];
            foreach (PartOMixedDwellingRow row in session.Rows)
            {
                PartODwellingResult result = evidence.Result(row.ZoneGuid);
                PartODwellingResult result_Reference = reference[PartOMixedLargeProjectFixture.Original(row.Name)];
                if (result is null || result.Outcome == PartODwellingOutcome.NotAssessed || result.Outcome != result_Reference.Outcome || result.SpaceCount_Fail != result_Reference.SpaceCount_Fail || result.SpaceCount_Pass != result_Reference.SpaceCount_Pass)
                {
                    deviations.Add(string.Format("{0}: {1} ({2}/{3}) vs {4} ({5}/{6})", row.Name, result?.Outcome, result?.SpaceCount_Pass, result?.SpaceCount_Fail, result_Reference.Outcome, result_Reference.SpaceCount_Pass, result_Reference.SpaceCount_Fail));
                }
            }
            Check(deviations.Count == 0, string.Format("every one of the {0} dwellings has its block reference's TM59 outcome ({1} deviate{2})", session.Rows.Count, deviations.Count, deviations.Count == 0 ? string.Empty : ": " + string.Join(" | ", deviations.Take(10))));

            // ---- Reopen: the run model restores against its results --------------------------------------------------
            Check(evidence.Path_RunModel is not null && File.Exists(evidence.Path_RunModel), "run model on disk " + evidence.Path_RunModel);
            if (evidence.Path_RunModel is not null && File.Exists(evidence.Path_RunModel))
            {
                stopwatch.Restart();
                AnalyticalModel model_Run = Core.Convert.ToSAM<AnalyticalModel>(evidence.Path_RunModel).Single();
                PartORun partORun = new();
                bool restored = partORun.Restore(model_Run, evidence.Path_RunModel, out string refusal_Restore);
                Check(restored, string.Format("run model reopens and restores against its results in {0:0.0} s {1}", stopwatch.Elapsed.TotalSeconds, refusal_Restore));
            }

            // ---- No growth: the saved baseline, rebuilt again, is what it was -----------------------------------------
            AnalyticalModel baseline_Reopened = Core.Convert.ToSAM<AnalyticalModel>(path_Model).Single();
            stopwatch.Restart();
            PartOMaterialisation materialisation_Again = baseline_Reopened.MaterialisePartODwellingStrategies(descriptors, null, templates);
            TimeSpan materialise_Again = stopwatch.Elapsed;
            Check(materialisation_Again.IsMaterialised && materialisation_Again.AnalyticalModel.ToJsonObject().ToJsonString().Length == materialisation.AnalyticalModel.ToJsonObject().ToJsonString().Length, string.Format("re-materialised from the reopened baseline in {0:0.0} s with the same size (no growth)", materialise_Again.TotalSeconds));
            Check(new FileInfo(path_Model).Length == length_Saved, "the saved baseline was not written by the run");
            Check(evidence.IsCurrent(baseline_Reopened, descriptors, templates, out string reason_Current), "the run is current against the reopened baseline " + reason_Current);
            Check(json_Source == Core.Convert.ToSAM<AnalyticalModel>(path_Source).Single().ToJsonObject().ToJsonString(), "the source fixture is unchanged");

            foreach (string path in Directory.GetFiles(directory).OrderBy(x => x))
            {
                Log(string.Format("   file {0,-60} {1,15:N0} bytes", Path.GetFileName(path), new FileInfo(path).Length));
            }

            Log(failures.Count == 0 ? "ACCEPTANCE PASSED" : "ACCEPTANCE FAILED: " + failures.Count);
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }
    }
}
