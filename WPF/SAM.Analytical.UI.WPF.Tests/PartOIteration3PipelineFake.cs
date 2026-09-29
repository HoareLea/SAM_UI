// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Analytical.UI;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// A recording stand-in for the five pieces of work an Iteration 3 run delegates.
    ///
    /// <para><b>What it is for, and what it is not</b></para>
    /// <para>
    /// Four of the five need a licensed TAS installation and an hour of wall clock, so the refusal
    /// boundaries - the part of this pipeline that actually has to be right - would otherwise be
    /// untestable. What this records is <b>which members were called</b>, which is how "a refused
    /// materialisation never reaches the thermal source" is asserted rather than assumed.
    /// </para>
    /// <para>
    /// <b>The objects it hands back are the real ones.</b> Every return value is a genuine
    /// <c>MechanicalVentilationMaterialisation</c>, <c>NoIzamThermalSource</c>,
    /// <c>SystemVentilationRoute</c> or <c>ResultantTemperatureResults</c>, built through its own
    /// production constructor - so the orchestration is tested against those types' real completeness
    /// rules and not against this file's idea of them.
    /// </para>
    /// </summary>
    internal class PartOIteration3PipelineFake : IPartOIteration3Pipeline
    {
        internal List<string> Called { get; } = [];

        /// <summary>
        /// The files each stage WRITES when it is called - because in production the stage writes them,
        /// and the artifact-ownership rule is exactly "did this attempt write it". A test that wants the
        /// stale case simply leaves these empty and puts the file there beforehand.
        /// </summary>
        internal List<string> Paths_ThermalSource { get; } = [];

        internal List<string> Paths_Route { get; } = [];

        /// <summary>Run inside <see cref="Route"/>: what the production route reports as it converts and simulates.</summary>
        internal Action Reporting_Route { get; set; }

        internal List<string> Paths_Bridge { get; } = [];

        internal List<string> Paths_Persist { get; } = [];

        private static void Write(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                File.WriteAllText(path, string.Format("written by this attempt - {0}", Guid.NewGuid()));
            }
        }

        internal MechanicalVentilationMaterialisation Materialisation { get; set; }

        internal NoIzamThermalSource NoIzamThermalSource { get; set; }

        internal AnalyticalModel AnalyticalModel_CandidateB { get; set; }

        internal bool Cancelled { get; set; }

        internal string Refusal_ThermalSource { get; set; }

        internal SystemVentilationRoute SystemVentilationRoute { get; set; }

        internal ResultantTemperatureResults ResultantTemperatureResults { get; set; }

        internal PartOIteration3Assessment Assessment_ReferenceA { get; set; }

        internal PartOIteration3Assessment Assessment_CandidateB { get; set; }

        /// <summary>
        /// Reference A's results file. Where set, <see cref="Assess"/> answers Reference A's assessment for that path
        /// and Candidate B's for any other, whatever the call order - which is what a second attempt on the same run
        /// needs. Null keeps the call-order rule (first call is Reference A).
        /// </summary>
        internal string Path_TSD_ReferenceA { get; set; }

        /// <summary>Thrown from <see cref="Assess"/> for Candidate B, where set - an assessment that does not return at all.</summary>
        internal Exception Exception_CandidateB { get; set; }

        internal bool Persisted { get; set; } = true;

        /// <summary>
        /// Whether <see cref="Persist"/> writes the model for real, through the production writer. The
        /// reopen tests need a genuine <c>.sam</c> on disk, because what they are testing is that a later
        /// session can read one back and validate its provenance.
        /// </summary>
        internal bool Persist_ForReal { get; set; }

        internal string Note_Persist { get; set; }

        /// <summary>
        /// Whether <see cref="Assess"/> writes a TM59 report beside the results it assessed and names it,
        /// as the production member does whenever its save succeeds.
        /// </summary>
        internal bool Write_Reports { get; set; }

        private static int count_Reports;

        /// <summary>
        /// Writes a TM59 report where production writes one - beside the results, at the one naming
        /// authority's path - and answers that path.
        /// <para>
        /// Every write is a different length, so a rewrite is visible in the file's fingerprint however
        /// close together two writes land on the clock.
        /// </para>
        /// </summary>
        internal static string WriteReport(string path_TSD)
        {
            string path = Query.Path_TM59Report(path_TSD);

            File.WriteAllText(path, string.Format("TM59 report {0}", new string('#', Interlocked.Increment(ref count_Reports))));

            return path;
        }

        /// <summary>The same assessment, naming the given report path.</summary>
        internal static PartOIteration3Assessment WithReport(PartOIteration3Assessment partOIteration3Assessment, string path_Report)
        {
            return new PartOIteration3Assessment(
                partOIteration3Assessment.IsAssessed,
                partOIteration3Assessment.Refusal,
                partOIteration3Assessment.OccupiedSpaceComplianceStatus,
                partOIteration3Assessment.SpaceResults,
                partOIteration3Assessment.AssociationRefusals,
                partOIteration3Assessment.VentilationStrategyRefusals,
                partOIteration3Assessment.SpaceGuids_Unassessed,
                partOIteration3Assessment.ResultantTemperatures,
                partOIteration3Assessment.ReportText,
                path_Report,
                partOIteration3Assessment.Count_Processed);
        }

        /// <summary>What <see cref="Assess"/> was asked to capture, in call order - the capture scope.</summary>
        internal List<List<Guid>> Captured { get; } = [];

        /// <summary>The simulation context Candidate B was actually run as.</summary>
        internal PartOSimulationContext SimulationContext_CandidateB { get; private set; }

        /// <summary>The cluster the materialisation was given - the SCOPED working copy.</summary>
        internal AdjacencyCluster AdjacencyCluster_Materialised { get; private set; }

        /// <summary>The rooms the materialisation was scoped to.</summary>
        internal List<Space> Spaces_Materialised { get; private set; }

        /// <summary>PR5A: the unit settings <see cref="Materialise"/> was actually handed, null meaning none was passed at all.</summary>
        internal IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings> UnitSettings_Materialised { get; private set; }

        /// <summary>PR5A: the fan heat gain policy <see cref="Route"/> was actually handed.</summary>
        internal SystemVentilationFanHeatGainPolicy FanHeatGainPolicy_Route { get; private set; }

        /// <summary>PR5B: the cooling settings <see cref="Materialise"/> was actually handed, null meaning none was passed at all.</summary>
        internal IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings> CoolingSettings_Materialised { get; private set; }

        public MechanicalVentilationMaterialisation Materialise(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings> unitSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings = null)
        {
            Called.Add(nameof(Materialise));

            AdjacencyCluster_Materialised = adjacencyCluster;
            Spaces_Materialised = [.. spaces ?? []];
            UnitSettings_Materialised = unitSettings;
            CoolingSettings_Materialised = coolingSettings;

            return Materialisation;
        }

        public NoIzamThermalSource ThermalSource(
            AnalyticalModel analyticalModel_Prepared,
            PartOSimulationContext partOSimulationContext,
            string projectName,
            CancellationToken cancellationToken,
            out AnalyticalModel analyticalModel_Source,
            out bool cancelled,
            out List<string> notes,
            out string refusal)
        {
            Called.Add(nameof(ThermalSource));

            Write(Paths_ThermalSource);

            SimulationContext_CandidateB = partOSimulationContext;

            analyticalModel_Source = AnalyticalModel_CandidateB;
            cancelled = Cancelled;
            notes = [];
            refusal = Refusal_ThermalSource;

            return NoIzamThermalSource;
        }

        public SystemVentilationRoute Route(NoIzamThermalSource noIzamThermalSource, MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation, string path_TPD, int startHour, int endHour, SystemVentilationFanHeatGainPolicy fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.ClearToZero)
        {
            Called.Add(nameof(Route));

            FanHeatGainPolicy_Route = fanHeatGainPolicy;

            //Stands for the coarse events SAM_Tas' route reports while it runs (see PartOProgressStageTests).
            Reporting_Route?.Invoke();

            Write(Paths_Route);

            return SystemVentilationRoute;
        }

        public ResultantTemperatureResults ResultantTemperatures(SystemVentilationRoute systemVentilationRoute, string path_TBD_Bridge)
        {
            Called.Add(nameof(ResultantTemperatures));

            Write(Paths_Bridge);

            return ResultantTemperatureResults;
        }

        public PartOIteration3Assessment Assess(AnalyticalModel analyticalModel_Workflow, string path_TSD, IEnumerable<OverheatingScenario> overheatingScenarios, IEnumerable<Guid> spaceGuids_Capture)
        {
            Called.Add(nameof(Assess));

            List<Guid> guids = [.. spaceGuids_Capture ?? []];
            guids.Sort();

            Captured.Add(guids);

            //By the results path where one is stated - so one fake can serve several attempts on the same run -
            //and otherwise by call order, as every single-attempt test has always relied on.
            bool isReferenceA = Path_TSD_ReferenceA is null ? Captured.Count == 1 : string.Equals(path_TSD, Path_TSD_ReferenceA, StringComparison.OrdinalIgnoreCase);

            if (!isReferenceA && Exception_CandidateB is not null)
            {
                throw Exception_CandidateB;
            }

            PartOIteration3Assessment result = isReferenceA ? Assessment_ReferenceA : Assessment_CandidateB;

            return Write_Reports && result is not null && result.IsAssessed ? WithReport(result, WriteReport(path_TSD)) : result;
        }

        public bool Persist(AnalyticalModel analyticalModel, string path_TSD, string path_TBD, out string note)
        {
            Called.Add(nameof(Persist));

            if (Persist_ForReal)
            {
                Core.Convert.ToFile(analyticalModel, Query.Path_PartORunModel(path_TSD), Core.SAMFileType.SAM);
            }
            else
            {
                Write(Paths_Persist);
            }

            note = Note_Persist;

            return Persisted;
        }

        /// <summary>Asserts that nothing after the named member was called.</summary>
        internal void AssertNothingAfter(string name)
        {
            int index = Called.LastIndexOf(name);

            Assert.True(index >= 0, string.Format("'{0}' was never called, so 'nothing after it' is not the assertion this test meant to make.", name));
            Assert.Equal(index, Called.Count - 1);
        }

        /// <summary>Asserts that the named member was never called at all.</summary>
        internal void AssertNeverCalled(params string[] names)
        {
            foreach (string name in names)
            {
                Assert.DoesNotContain(name, Called);
            }
        }
    }
}
