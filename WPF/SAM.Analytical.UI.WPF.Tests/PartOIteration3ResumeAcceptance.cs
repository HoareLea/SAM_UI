// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Analytical.UI;
using SAM.Core.Tas;
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
    /// <b>Licensed acceptance of the Iteration 3 resume</b>, over the real production pipeline and real TAS.
    ///
    /// <para>
    /// Restores a completed Iteration 1a run from a disposable copy of its output folder (its <c>.sam</c>, the saved
    /// preparation <c>.prepared.sam</c> + <c>.partorun.json</c>, and its <c>.tsd</c>) - so Reference A is never
    /// simulated again - then:
    /// </para>
    /// <list type="number">
    /// <item>runs Iteration 3 once through the production <see cref="PartOIteration3Pipeline"/>, with Candidate B's TM59
    /// assessment replaced by a controlled refusal: real TAS, then a downstream failure;</item>
    /// <item>shows, without running anything, that a changed TAS case and a touched TAS file each refuse the kept work,
    /// and that undoing the change makes it reusable again;</item>
    /// <item>retries: no TAS pipeline member may be called and no TBD / TAS3D / TPD process may start, and the pairing
    /// must complete;</item>
    /// <item>reopens the resumed pairing through the ordinary review, and compares its shape with a normal completed
    /// record of the same method where one is given;</item>
    /// <item>runs the method again after completion - nothing is kept, so it must head for TAS (cancelled at the
    /// thermal source, so no second TAS run is spent proving it).</item>
    /// </list>
    /// <para>
    /// Needs <c>SAM_PARTO_RESUME_DIR</c> (the disposable folder) and <c>SAM_PARTO_RESUME_MODEL</c> (the run's
    /// <c>.sam</c> file name in it). Optional: <c>SAM_PARTO_RESUME_MODE</c> (a <see cref="PartOIteration3BehaviourMode"/>
    /// name; default <c>SelectedProductManufacturerGuidance</c>) and <c>SAM_PARTO_RESUME_BASELINE_RECORD</c> (a completed
    /// record of the same method from a normal run, for the shape comparison). Without the first two it does nothing.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOIteration3ResumeAcceptance
    {
        /// <summary>The production pipeline, counted, with Candidate B's assessment optionally refused.</summary>
        private sealed class CountingPipeline : IPartOIteration3Pipeline
        {
            private readonly PartOIteration3Pipeline production = new();

            internal string Path_TSD_ReferenceA { get; set; }

            internal bool Fail_CandidateB { get; set; }

            internal List<string> Called { get; } = [];

            internal Action<string> Log { get; set; }

            private T Timed<T>(string name, Func<T> func)
            {
                Called.Add(name);
                Stopwatch stopwatch = Stopwatch.StartNew();
                T result = func();
                Log?.Invoke(string.Format("  pipeline.{0} {1:0.0} s", name, stopwatch.Elapsed.TotalSeconds));
                return result;
            }

            public MechanicalVentilationMaterialisation Materialise(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings> unitSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings = null)
            {
                return Materialise(adjacencyCluster, spaces, unitSettings, coolingSettings, guidanceSettings, null);
            }

            public MechanicalVentilationMaterialisation Materialise(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings, IEnumerable<Guid> guids_VentilationSystem)
            {
                return Timed(nameof(Materialise), () => production.Materialise(adjacencyCluster, spaces, unitSettings, coolingSettings, guidanceSettings, guids_VentilationSystem));
            }

            public NoIzamThermalSource ThermalSource(AnalyticalModel analyticalModel_Prepared, PartOSimulationContext partOSimulationContext, string projectName, CancellationToken cancellationToken, out AnalyticalModel analyticalModel_Source, out bool cancelled, out List<string> notes, out string refusal)
            {
                AnalyticalModel analyticalModel = null;
                bool cancelled_Temp = false;
                List<string> notes_Temp = null;
                string refusal_Temp = null;

                NoIzamThermalSource result = Timed(nameof(ThermalSource), () => production.ThermalSource(analyticalModel_Prepared, partOSimulationContext, projectName, cancellationToken, out analyticalModel, out cancelled_Temp, out notes_Temp, out refusal_Temp));

                analyticalModel_Source = analyticalModel;
                cancelled = cancelled_Temp;
                notes = notes_Temp;
                refusal = refusal_Temp;

                return result;
            }

            public SystemVentilationRoute Route(NoIzamThermalSource noIzamThermalSource, MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation, string path_TPD, int startHour, int endHour, SystemVentilationFanHeatGainPolicy fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.ClearToZero)
            {
                return Timed(nameof(Route), () => production.Route(noIzamThermalSource, mechanicalVentilationMaterialisation, path_TPD, startHour, endHour, fanHeatGainPolicy));
            }

            public ResultantTemperatureResults ResultantTemperatures(SystemVentilationRoute systemVentilationRoute, string path_TBD_Bridge)
            {
                return Timed(nameof(ResultantTemperatures), () => production.ResultantTemperatures(systemVentilationRoute, path_TBD_Bridge));
            }

            public PartOIteration3Assessment Assess(AnalyticalModel analyticalModel_Workflow, string path_TSD, IEnumerable<OverheatingScenario> overheatingScenarios, IEnumerable<Guid> spaceGuids_Capture)
            {
                bool isReferenceA = string.Equals(path_TSD, Path_TSD_ReferenceA, StringComparison.OrdinalIgnoreCase);

                if (!isReferenceA && Fail_CandidateB)
                {
                    Called.Add(nameof(Assess) + "(controlled failure)");

                    return new PartOIteration3Assessment(false, "Controlled acceptance failure: Candidate B's TM59 assessment was not run.", TM59ComplianceStatus.Undefined, null, null, null, null, null, null, null, 0);
                }

                return Timed(nameof(Assess), () => production.Assess(analyticalModel_Workflow, path_TSD, overheatingScenarios, spaceGuids_Capture));
            }

            public bool Persist(AnalyticalModel analyticalModel, string path_TSD, string path_TBD, out string note)
            {
                string note_Temp = null;
                bool result = Timed(nameof(Persist), () => production.Persist(analyticalModel, path_TSD, path_TBD, out note_Temp));
                note = note_Temp;
                return result;
            }
        }

        /// <summary>Every TBD / TSD / TAS3D / TPD process that starts while it is running, by name.</summary>
        private sealed class TasProcessWatch : IDisposable
        {
            private static readonly string[] Names = ["TBD", "TSD", "TAS3D", "TPD", "TWD", "TCD"];

            private readonly HashSet<int> seen = [];

            private readonly Timer timer;

            internal TasProcessWatch()
            {
                foreach (Process process in Names.SelectMany(Process.GetProcessesByName))
                {
                    seen.Add(process.Id);
                }

                timer = new Timer(_ => Poll(), null, 0, 250);
            }

            internal List<string> Started { get; } = [];

            private void Poll()
            {
                lock (seen)
                {
                    foreach (Process process in Names.SelectMany(Process.GetProcessesByName))
                    {
                        if (seen.Add(process.Id))
                        {
                            Started.Add(process.ProcessName);
                        }
                    }
                }
            }

            public void Dispose()
            {
                timer.Dispose();
                Poll();
            }
        }

        [WpfFact]
        public void Acceptance_Iteration3Resume_OnLicensedTas()
        {
            string directory = Environment.GetEnvironmentVariable("SAM_PARTO_RESUME_DIR");
            string name_Model = Environment.GetEnvironmentVariable("SAM_PARTO_RESUME_MODEL");
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(name_Model))
            {
                return;
            }

            PartOIteration3BehaviourMode partOIteration3BehaviourMode = Enum.TryParse(Environment.GetEnvironmentVariable("SAM_PARTO_RESUME_MODE"), out PartOIteration3BehaviourMode mode)
                ? mode
                : PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance;

            string path_Log = Path.Combine(directory, "resume-acceptance.log");
            StringBuilder log = new();
            List<string> failures = [];
            Stopwatch stopwatch_Total = Stopwatch.StartNew();
            void Log(string text)
            {
                log.AppendLine(string.Format("[{0:hh\\:mm\\:ss}] {1}", stopwatch_Total.Elapsed, text));
                File.WriteAllText(path_Log, log.ToString());
            }
            void Check(bool condition, string text)
            {
                Log((condition ? "PASS " : "FAIL ") + text);
                if (!condition)
                {
                    failures.Add(text);
                }
            }
            void Ledger(PartOIteration3Result partOIteration3Result)
            {
                foreach (PartOIteration3StageState partOIteration3StageState in partOIteration3Result.Ledger.Stages)
                {
                    Log(string.Format("    {0,-22} {1,-9} {2}", partOIteration3StageState.Stage, partOIteration3StageState.Status, partOIteration3StageState.Detail));
                }
            }
            string Stat(string path)
            {
                return PartOIteration3Artifacts.TryRead(path, out long length, out long ticks) ? string.Format("{0} bytes, {1:O}", length, new DateTime(ticks, DateTimeKind.Utc)) : "MISSING";
            }

            // ---- Reference A, restored - never simulated here -----------------------------------------------------
            string path_Model = Path.Combine(directory, name_Model);
            AnalyticalModel analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path_Model).Single();

            PartORun partORun = new();
            Check(partORun.Restore(analyticalModel, path_Model, out string refusal_Restore), "Reference A restored from its persisted model " + refusal_Restore);
            Check(partORun.CanResumeIteration3, "the saved preparation lets Iteration 3 start without re-running Reference A " + partORun.ResumeRefusal);
            Log(string.Format("Reference A: {0} ({1}); method {2}", partORun.Path_TSD, Stat(partORun.Path_TSD), partOIteration3BehaviourMode));

            //Everything Iteration 3 writes lands beside Reference A's results, and the provenance resolves to the
            //RECORDED results path whenever that file still exists - so a copy of a run whose original folder is still
            //there resolves back to the original. Refuse rather than write into it.
            Check(string.Equals(Path.GetFullPath(Path.GetDirectoryName(partORun.Path_TSD)!).TrimEnd('\\'), Path.GetFullPath(directory).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase), "Reference A's results resolved inside the disposable folder (move or rename the original run's folder if they resolve to it)");

            if (failures.Count != 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, failures));
            }

            PartOIteration3Paths partOIteration3Paths = PartOIteration3Paths.Create(partORun.SimulationContext, partORun.Path_TSD, partOIteration3BehaviourMode);
            string[] paths_Tas = [partOIteration3Paths.Path_TBD_ThermalSource, partOIteration3Paths.Path_TSD_ThermalSource, partOIteration3Paths.Path_TPD, partOIteration3Paths.Path_TBD_Bridge, partOIteration3Paths.Path_TSD_Bridge, partOIteration3Paths.Path_OperatingAirFlow];

            CountingPipeline countingPipeline = new() { Path_TSD_ReferenceA = partORun.Path_TSD, Log = Log };

            // ---- 1. Real TAS, then a controlled failure at Candidate B's TM59 -------------------------------------
            Log("ATTEMPT 1 - real TAS, controlled failure at Candidate B TM59");
            countingPipeline.Fail_CandidateB = true;

            PartOIteration3Result partOIteration3Result_Failed;
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (TasProcessWatch tasProcessWatch = new())
            {
                partOIteration3Result_Failed = Modify.RunPartOIteration3(partORun, countingPipeline, default, partOIteration3BehaviourMode);
                tasProcessWatch.Dispose();
                Log(string.Format("  {0:0.0} s; TAS processes started: {1}", stopwatch.Elapsed.TotalSeconds, string.Join(", ", tasProcessWatch.Started.GroupBy(x => x).Select(x => string.Format("{0} x{1}", x.Key, x.Count())))));
            }

            Ledger(partOIteration3Result_Failed);
            Log("  calls: " + string.Join(", ", countingPipeline.Called));
            Check(partOIteration3Result_Failed.Ledger.Stage_Refused == PartOIteration3Stage.CandidateBTM59, "attempt 1 stopped at Candidate B TM59, after its TAS work");
            Check(partOIteration3Result_Failed.Ledger.State(PartOIteration3Stage.ResultantTemperature).IsCompleted, "attempt 1 completed every TAS stage");
            Check(new[] { nameof(IPartOIteration3Pipeline.Materialise), nameof(IPartOIteration3Pipeline.ThermalSource), nameof(IPartOIteration3Pipeline.Route), nameof(IPartOIteration3Pipeline.ResultantTemperatures) }.All(x => countingPipeline.Called.Count(y => y == x) == 1), "attempt 1 called each TAS member exactly once");
            Check(partORun.Iteration3Checkpoint(partOIteration3BehaviourMode) is not null, "attempt 1's TAS work is kept on the run");
            Check(!partOIteration3Result_Failed.Record.IsComplete && Query.PartOIteration3PairingStatus(partORun.Path_TSD, partOIteration3BehaviourMode).IsRefused, "attempt 1 is recorded as refused, not as a completed pairing");
            Log("  after-failure retry line: " + Modify.PartOIteration3RetryText(partORun, partOIteration3BehaviourMode));

            Dictionary<string, string> stats_Failed = paths_Tas.Where(File.Exists).ToDictionary(x => x, Stat);
            foreach (KeyValuePair<string, string> keyValuePair in stats_Failed)
            {
                Log(string.Format("  {0}: {1}", Path.GetFileName(keyValuePair.Key), keyValuePair.Value));
            }

            // ---- 2. Stale conditions refuse the kept work; undone, it is reusable again (no run) -----------------
            Log("STALE CHECKS - resume decisions only, nothing run");
            stopwatch.Restart();
            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, partOIteration3BehaviourMode);
            Log(string.Format("  resume decision {0:0} ms", stopwatch.Elapsed.TotalMilliseconds));
            Check(partOIteration3ResumePlan.Reuse, "the kept work is reusable as it stands");

            partORun.SimulationContext.UnmetHours = !partORun.SimulationContext.UnmetHours;
            partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, partOIteration3BehaviourMode);
            Check(!partOIteration3ResumePlan.Reuse && partOIteration3ResumePlan.Reason.Contains("the TAS case"), "a changed TAS case refuses it: " + partOIteration3ResumePlan.Reason);
            partORun.SimulationContext.UnmetHours = !partORun.SimulationContext.UnmetHours;

            DateTime dateTime_Bridge = File.GetLastWriteTimeUtc(partOIteration3Paths.Path_TSD_Bridge);
            File.SetLastWriteTimeUtc(partOIteration3Paths.Path_TSD_Bridge, dateTime_Bridge.AddMinutes(1));
            partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, partOIteration3BehaviourMode);
            Check(!partOIteration3ResumePlan.Reuse && partOIteration3ResumePlan.Reason.Contains(partOIteration3Paths.Path_TSD_Bridge), "a touched bridge TSD refuses it, by name: " + partOIteration3ResumePlan.Reason);
            File.SetLastWriteTimeUtc(partOIteration3Paths.Path_TSD_Bridge, dateTime_Bridge);

            Check(Query.PartOIteration3ResumePlan(partORun, partOIteration3BehaviourMode).Reuse, "with both undone, the kept work is reusable again");

            // ---- 3. The retry: no TAS ------------------------------------------------------------------------------
            Log("ATTEMPT 2 - retry");
            countingPipeline.Fail_CandidateB = false;
            countingPipeline.Called.Clear();

            List<PartOIteration3Stage> announced = [];
            PartOIteration3Result partOIteration3Result;
            stopwatch.Restart();
            using (TasProcessWatch tasProcessWatch = new())
            {
                partOIteration3Result = Modify.RunPartOIteration3(partORun, countingPipeline, default, partOIteration3BehaviourMode, announced.Add);
                tasProcessWatch.Dispose();
                Log(string.Format("  {0:0.0} s; TAS processes started: {1}", stopwatch.Elapsed.TotalSeconds, tasProcessWatch.Started.Count == 0 ? "none" : string.Join(", ", tasProcessWatch.Started.GroupBy(x => x).Select(x => string.Format("{0} x{1}", x.Key, x.Count())))));
                Check(!tasProcessWatch.Started.Any(x => x is "TBD" or "TAS3D" or "TPD"), "no TBD, TAS3D or TPD process started (TSD only serves the TM59 reads)");
            }

            Ledger(partOIteration3Result);
            Log("  calls: " + string.Join(", ", countingPipeline.Called));
            Log("  notes: " + string.Join(" | ", partOIteration3Result.Notes));
            Check(partOIteration3Result.IsComplete, "the retry completed the pairing " + string.Join(" | ", partOIteration3Result.Ledger.Reasons));
            Check(countingPipeline.Called.SequenceEqual([nameof(IPartOIteration3Pipeline.Assess), nameof(IPartOIteration3Pipeline.Assess), nameof(IPartOIteration3Pipeline.Persist)]), "the retry called only Assess (A), Assess (B) and Persist - no TAS member");
            Check(partOIteration3Result.Ledger.State(PartOIteration3Stage.ThermalSource).Detail.StartsWith("Reused from the attempt of"), "the ledger records the TAS stages as reused");
            Check(announced.All(x => Modify.PartOIteration3Phase(x, partOIteration3BehaviourMode, true) >= 0), "every announced stage has a phase in the resumed layout");
            Check(paths_Tas.Where(File.Exists).All(x => Stat(x) == stats_Failed[x]), "every TAS file is byte-length and write-time unchanged by the retry");
            Check(partORun.Iteration3Checkpoint(partOIteration3BehaviourMode) is null, "the completed pairing keeps nothing");
            Log(string.Format("  comparison: {0}; {1} TM59 outcome(s) differ", partOIteration3Result.Comparison?.Statistics, partOIteration3Result.Comparison?.Count_Changed));
            Log(string.Format("  reference {0} / system {1}", partOIteration3Result.Assessment_ReferenceA?.OccupiedSpaceComplianceStatus, partOIteration3Result.Assessment_CandidateB?.OccupiedSpaceComplianceStatus));

            // ---- 4. The result shape, and the ordinary reopen ------------------------------------------------------
            Log("RESULT SHAPE");
            PartOIteration3Record partOIteration3Record = Query.PartOIteration3PairingRecord(partOIteration3Result.Path_Record);
            Check(partOIteration3Record is not null && partOIteration3Record.IsComplete, "the saved record is a completed pairing");
            Check(partOIteration3Record.Stages.All(x => x.IsCompleted), "every recorded stage completed");
            Check(File.Exists(partOIteration3Paths.Path_Model_CandidateB) && partOIteration3Record.File(PartOIteration3Roles.CandidateB_Model) is not null, "Candidate B's model is persisted and recorded");
            Check(File.Exists(PartOIteration3Paths.Path_Report_ForRecord(partOIteration3Result.Path_Record)), "the A/B review report was written");

            string path_Baseline = Environment.GetEnvironmentVariable("SAM_PARTO_RESUME_BASELINE_RECORD");
            if (!string.IsNullOrWhiteSpace(path_Baseline) && File.Exists(path_Baseline))
            {
                PartOIteration3Record partOIteration3Record_Baseline = Query.PartOIteration3PairingRecord(path_Baseline);
                Log(string.Format("  baseline (normal run) {0}: complete {1}, {2} stages, {3} bindings, {4} air systems, A {5} / B {6}", Path.GetFileName(path_Baseline), partOIteration3Record_Baseline.IsComplete, partOIteration3Record_Baseline.Stages.Count, partOIteration3Record_Baseline.Bindings.Count, partOIteration3Record_Baseline.Count_AirSystem, partOIteration3Record_Baseline.Status_ReferenceA, partOIteration3Record_Baseline.Status_CandidateB));
                Log(string.Format("  resumed                 : complete {0}, {1} stages, {2} bindings, {3} air systems, A {4} / B {5}", partOIteration3Record.IsComplete, partOIteration3Record.Stages.Count, partOIteration3Record.Bindings.Count, partOIteration3Record.Count_AirSystem, partOIteration3Record.Status_ReferenceA, partOIteration3Record.Status_CandidateB));
                Check(partOIteration3Record_Baseline.IsComplete == partOIteration3Record.IsComplete
                    && partOIteration3Record_Baseline.Stages.Count == partOIteration3Record.Stages.Count
                    && partOIteration3Record_Baseline.Bindings.Count == partOIteration3Record.Bindings.Count
                    && partOIteration3Record_Baseline.Count_AirSystem == partOIteration3Record.Count_AirSystem
                    && partOIteration3Record_Baseline.Status_ReferenceA == partOIteration3Record.Status_ReferenceA
                    && partOIteration3Record_Baseline.Status_CandidateB == partOIteration3Record.Status_CandidateB
                    && new HashSet<string>(partOIteration3Record_Baseline.Files.Select(x => x.Role)).SetEquals(partOIteration3Record.Files.Select(x => x.Role)),
                    "the resumed record has the normal run's shape: stages, bindings, air systems, verdicts and file roles");
            }

            countingPipeline.Called.Clear();
            stopwatch.Restart();
            PartOIteration3Result partOIteration3Result_Review = Modify.ReviewPartOIteration3(partORun, countingPipeline, partOIteration3BehaviourMode);
            Check(partOIteration3Result_Review.IsComplete && partOIteration3Result_Review.Comparison is not null, string.Format("the resumed pairing reopens through the ordinary review ({0:0.0} s) {1}", stopwatch.Elapsed.TotalSeconds, string.Join(" | ", partOIteration3Result_Review.Ledger.Reasons)));
            Check(countingPipeline.Called.All(x => x == nameof(IPartOIteration3Pipeline.Assess)), "the review called only Assess");
            Check(partOIteration3Result_Review.Comparison?.Statistics?.ToString() == partOIteration3Result.Comparison?.Statistics?.ToString(), "the reopened comparison equals the resumed one: " + partOIteration3Result_Review.Comparison?.Statistics);

            //Kept as evidence: a new run starts by deleting the method's record and Candidate B model, as it always has.
            string directory_Evidence = Path.Combine(directory, "resumed-result");
            Directory.CreateDirectory(directory_Evidence);
            foreach (string path in new[] { partOIteration3Result.Path_Record, PartOIteration3Paths.Path_Report_ForRecord(partOIteration3Result.Path_Record), PartOIteration3Paths.Path_Report_ForRecord(partOIteration3Result.Path_Record, "json"), partOIteration3Paths.Path_Model_CandidateB, partOIteration3Paths.Path_TM59Report_CandidateB })
            {
                if (File.Exists(path))
                {
                    File.Copy(path, Path.Combine(directory_Evidence, Path.GetFileName(path)), true);
                }
            }

            // ---- 5. Nothing kept after completion: running again heads for TAS --------------------------------------
            Log("ATTEMPT 3 - run again after completion (cancelled as the thermal source starts)");
            countingPipeline.Called.Clear();
            Check(!Query.PartOIteration3ResumePlan(partORun, partOIteration3BehaviourMode).Reuse, "after completion nothing is reusable");
            using (CancellationTokenSource cancellationTokenSource = new())
            {
                PartOIteration3Result partOIteration3Result_Again = Modify.RunPartOIteration3(partORun, countingPipeline, cancellationTokenSource.Token, partOIteration3BehaviourMode, x =>
                {
                    if (x == PartOIteration3Stage.ThermalSource)
                    {
                        cancellationTokenSource.Cancel();
                    }
                });

                Check(partOIteration3Result_Again.Ledger.Stage_Refused == PartOIteration3Stage.ThermalSource && countingPipeline.Called.Contains(nameof(IPartOIteration3Pipeline.Materialise)), "a new run materialised again and reached the TAS thermal source (cancelled there): calls " + string.Join(", ", countingPipeline.Called));
            }

            Log(string.Format("DONE in {0:0.0} s; {1} failure(s)", stopwatch_Total.Elapsed.TotalSeconds, failures.Count));

            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }
    }
}
