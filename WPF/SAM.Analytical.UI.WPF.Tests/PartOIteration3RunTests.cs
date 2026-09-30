// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Analytical.UI;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The whole Iteration 3 pipeline, end to end, with no TAS.</b>
    ///
    /// <para><b>What is actually being tested</b></para>
    /// <para>
    /// Not the four authorities PR4 calls - those are tested in their own repositories - but the
    /// sequencing, the refusal boundaries and the identity checks between them. The properties that
    /// matter are: a refusal at any stage stops the run <b>before the next delegate is called</b>, every
    /// later stage stays NOT RUN, no comparison is produced, and the record is still written so the
    /// refusal can be reopened.
    /// </para>
    /// <para>
    /// Each case drives the real <c>Modify.RunPartOIteration3</c> over a real
    /// <c>PartORun</c> and real SAM_Systems and SAM_Tas result objects.
    /// </para>
    /// </summary>
    public partial class PartOIteration3RunTests : IDisposable
    {
        private readonly string directory = PartOIteration3Fixture.Directory_Temp();

        //Iteration 3's own folders beneath the reference's folder - see PartOOutputPaths.
        private string directory_It3 => Path.Combine(directory, "Iteration3", "tas");

        private string directory_It3Reports => Path.Combine(directory, "Iteration3", "reports");

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
                //A temporary directory that will not delete is not a test failure.
            }
        }

        //-------------------------------------------------------------------------------------------------
        //The run under test
        //-------------------------------------------------------------------------------------------------

        private AdjacencyCluster adjacencyCluster;

        private List<Guid> guids_VentilationSystem;

        private List<Zone> zones;

        private List<Guid> guids_Space_Dwelling;

        private string path_TSD_ReferenceA;

        /// <summary>
        /// A completed, eligible Iteration 1a run over the fixture design - built through the production
        /// transitions, in the order production performs them.
        /// </summary>
        private PartORun Run()
        {
            adjacencyCluster = PartOIteration3Fixture.Design(out guids_VentilationSystem, out zones);

            guids_Space_Dwelling = [];
            foreach (Zone zone in zones)
            {
                foreach (Space space in PartOIteration3Fixture.Spaces(adjacencyCluster, zone))
                {
                    guids_Space_Dwelling.Add(space.Guid);
                }
            }

            AnalyticalModel analyticalModel_Prepared = PartOIteration3Fixture.Model(adjacencyCluster);

            PartORun result = new();

            Assert.True(result.Prepare(
                analyticalModel_Prepared,
                PartOIteration3Fixture.Scenarios(),
                new PartOPreparationContext(PartOIteration.BasePassive, zones, null, null),
                guids_VentilationSystem));

            path_TSD_ReferenceA = Path.Combine(directory, "Flat.tsd");

            Assert.True(result.ExpectResults(path_TSD_ReferenceA));

            File.WriteAllText(path_TSD_ReferenceA, "reference A results");

            //The design side of the assessment is the model the workflow RETURNED, and it carries the
            //run's own provenance - which the pairing copies its fingerprints from.
            AnalyticalModel analyticalModel_Workflow = PartOIteration3Fixture.Model(new AdjacencyCluster(adjacencyCluster), "Flat");

            analyticalModel_Workflow.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new Core.SAMCollection<OverheatingScenario>(result.OverheatingScenarios));
            analyticalModel_Workflow.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(analyticalModel_Workflow, path_TSD_ReferenceA));

            Assert.True(result.Complete(analyticalModel_Workflow, path_TSD_ReferenceA, PartOIteration3Fixture.SimulationContext(directory), out string refusal));
            Assert.Null(refusal);

            return result;
        }

        private static MechanicalVentilationMaterialisation Materialisation()
        {
            return new MechanicalVentilationMaterialisation(new Core.Systems.SystemEnergyCentre("Part O"), null, ["materialised"], null);
        }

        private static MechanicalVentilationMaterialisation Materialisation_Refused(string refusal)
        {
            return new MechanicalVentilationMaterialisation(null, [refusal], null, null);
        }

        /// <summary>A complete route over the dwelling rooms that carry a design duty.</summary>
        private SystemVentilationRoute Route(NoIzamThermalSource noIzamThermalSource, out List<SystemVentilationBinding> systemVentilationBindings)
        {
            systemVentilationBindings = [];

            List<SystemVentilationConnectionBinding> connectionBindings = [];

            foreach (Zone zone in zones)
            {
                Guid guid_AirSystem = Guid.NewGuid();

                List<Space> spaces = PartOIteration3Fixture.Spaces(adjacencyCluster, zone);

                Dictionary<Guid, Guid> dictionary_SystemSpace = [];

                foreach (Space space in spaces)
                {
                    List<VentilationTerminal> ventilationTerminals = adjacencyCluster.VentilationTerminals(space) ?? [];

                    double? supply = Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Supply);
                    double? extract = Analytical.Query.VentilationTerminalDesignDuty_Lps(ventilationTerminals, FlowClassification.Extract);

                    systemVentilationBindings.Add(PartOIteration3Fixture.Binding(space.Guid, guid_AirSystem, supply, extract, out Guid guid_SystemSpace));

                    dictionary_SystemSpace[space.Guid] = guid_SystemSpace;
                }

                foreach (KeyValuePair<(Guid, Guid), Analytical.Query.DesignTransferAirMovement> keyValuePair in adjacencyCluster.DesignTransferSpaceAirMovements())
                {
                    Guid guid_From = keyValuePair.Value.FromGuid;
                    Guid guid_To = keyValuePair.Value.ToGuid;

                    if (!dictionary_SystemSpace.TryGetValue(guid_From, out Guid guid_SystemSpace_From) || !dictionary_SystemSpace.TryGetValue(guid_To, out Guid guid_SystemSpace_To))
                    {
                        continue;
                    }

                    connectionBindings.Add(new SystemVentilationConnectionBinding(
                        SystemVentilationConnectionType.Transfer,
                        keyValuePair.Value.SpaceAirMovement.Guid,
                        Guid.NewGuid(),
                        guid_AirSystem,
                        guid_SystemSpace_From,
                        guid_SystemSpace_To,
                        adjacencyCluster.DesignTransferFlowRate_Lps(guid_From, guid_To, out Guid _, out Guid _).Value,
                        "damper"));
                }
            }

            return new SystemVentilationRoute(
                noIzamThermalSource,
                Path.Combine(directory_It3, "Flat-It3B.tpd"),
                PartOIteration3Fixture.Evidence(Path.Combine(directory_It3, "Flat-It3B.tpd"), Path.Combine(directory_It3, "Flat-It3B.tpd")),
                systemVentilationBindings,
                connectionBindings,
                PartOIteration3Fixture.ZoneTemperatures(systemVentilationBindings, 0, 23),
                null,
                null);
        }

        private static PartOIteration3Assessment Assessment(IEnumerable<Guid> guids, Func<Guid, double[]> func_Series, TM59ComplianceStatus status = TM59ComplianceStatus.Pass)
        {
            List<PartOTM59SpaceResult> spaceResults = [];
            Dictionary<Guid, double[]> resultantTemperatures = [];

            foreach (Guid guid in guids)
            {
                spaceResults.Add(new PartOTM59SpaceResult(guid, "room", "TM59 Criterion A", 10, 32, status, true));

                resultantTemperatures[guid] = func_Series(guid);
            }

            return new PartOIteration3Assessment(true, null, status, spaceResults, null, null, null, resultantTemperatures, "report", null, spaceResults.Count);
        }

        /// <summary>The pipeline for a run that completes, wired to the fixture design.</summary>
        private PartOIteration3PipelineFake Pipeline_Complete(out List<Guid> guids_Bound)
        {
            NoIzamThermalSource noIzamThermalSource = PartOIteration3Fixture.ThermalSource(directory_It3, guids_Space_Dwelling);

            SystemVentilationRoute systemVentilationRoute = Route(noIzamThermalSource, out List<SystemVentilationBinding> systemVentilationBindings);

            Assert.True(systemVentilationRoute.IsComplete);

            guids_Bound = [];
            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationBindings)
            {
                guids_Bound.Add(systemVentilationBinding.Guid_Space);
            }

            guids_Bound.Sort();

            string path_TSD_Bridge = Path.Combine(directory_It3, "Flat-It3B-Bridge.tsd");

            //Candidate B one degree above Reference A everywhere, so the expected statistics are exact.
            ResultantTemperatureResults resultantTemperatureResults = PartOIteration3Fixture.ResultantTemperatures(path_TSD_Bridge, guids_Bound, 0, 23, (guid, hour) => 21.0);

            List<Guid> guids = guids_Bound;

            PartOIteration3PipelineFake result = new()
            {
                Materialisation = Materialisation(),
                NoIzamThermalSource = noIzamThermalSource,
                AnalyticalModel_CandidateB = PartOIteration3Fixture.Model(new AdjacencyCluster(adjacencyCluster), "Flat-It3B"),
                SystemVentilationRoute = systemVentilationRoute,
                ResultantTemperatureResults = resultantTemperatureResults,
                Assessment_ReferenceA = Assessment(guids_Space_Dwelling, _ => Series(20.0)),
                Assessment_CandidateB = Assessment(guids, _ => Series(21.0)),
            };

            //Each stage writes what the real one writes, so the artifact-ownership rule sees this
            //attempt's files rather than the fixture's - which is what a stale attempt looks like, and is
            //tested on its own below.
            result.Paths_ThermalSource.Add(Path.Combine(directory_It3, "Flat-It3B.tbd"));
            result.Paths_ThermalSource.Add(Path.Combine(directory_It3, "Flat-It3B.tsd"));
            result.Paths_Route.Add(Path.Combine(directory_It3, "Flat-It3B.tpd"));
            result.Paths_Bridge.Add(Path.Combine(directory_It3, "Flat-It3B-Bridge.tbd"));
            result.Paths_Bridge.Add(Path.Combine(directory_It3, "Flat-It3B-Bridge.tsd"));
            result.Paths_Persist.Add(Path.Combine(directory_It3, "Flat-It3B-Bridge.sam"));

            return result;
        }

        private static double[] Series(double value)
        {
            double[] result = new double[24];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = value;
            }

            return result;
        }

        //-------------------------------------------------------------------------------------------------
        //The complete run
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_complete_pairing_records_every_stage_and_produces_the_comparison()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> guids_Bound);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.False(partOIteration3Result.IsRefused);
            Assert.True(partOIteration3Result.IsComplete);
            Assert.NotNull(partOIteration3Result.Comparison);

            Assert.All(partOIteration3Result.Ledger.Stages, x => Assert.Equal(PartOIteration3StageStatus.Completed, x.Status));

            //Six rooms, twenty-four hours each, Candidate B exactly one degree warmer.
            Assert.Equal(guids_Bound.Count, partOIteration3Result.Comparison.Statistics.Count_Rooms);
            Assert.Equal(guids_Bound.Count * 24L, partOIteration3Result.Comparison.Statistics.Count_Values);
            Assert.Equal(1.0, partOIteration3Result.Comparison.Statistics.MeanBias, 12);
            Assert.Equal(1.0, partOIteration3Result.Comparison.Statistics.RootMeanSquareError, 12);

            //One per dwelling.
            Assert.Equal(2, partOIteration3Result.Comparison.Dwellings.Count);

            //And the pairing is reopenable.
            Assert.True(File.Exists(partOIteration3Result.Path_Record));
        }

        /// <summary>
        /// The record names the reference iteration the run itself records (2026-09-29): it used to call every
        /// reference "Iteration 1a", including the Iteration 2 run manufacturer guidance is paired with.
        /// </summary>
        [Fact]
        public void The_input_stage_names_the_reference_iteration_the_run_records()
        {
            PartORun partORun = Run();

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, Pipeline_Complete(out List<Guid> _));

            string detail = partOIteration3Result.Ledger.State(PartOIteration3Stage.Input).Detail;

            Assert.StartsWith("Reference run '", detail);
            Assert.Contains("(" + Query.PartOIterationText(partORun) + ")", detail);
            Assert.DoesNotContain("Iteration 1a run", detail);
        }

        /// <summary>
        /// Candidate B must be the SAME thermal case as Reference A, writing somewhere else - which is
        /// the whole basis on which the two are comparable.
        /// </summary>
        [Fact]
        public void Candidate_B_runs_the_same_case_as_reference_A_under_its_own_project_name()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            PartOSimulationContext partOSimulationContext = partOIteration3PipelineFake.SimulationContext_CandidateB;

            Assert.NotNull(partOSimulationContext);
            Assert.Equal("Flat-It3B", partOSimulationContext.ProjectName);
            //Iteration 3's own tas folder beneath the reference's root - never Reference A's folder.
            Assert.Equal(System.IO.Path.Combine(directory, "Iteration3", "tas"), partOSimulationContext.OutputDirectory);
            Assert.Same(partORun.SimulationContext.WeatherData, partOSimulationContext.WeatherData);
            Assert.Equal(partORun.SimulationContext.SolarCalculationMethod, partOSimulationContext.SolarCalculationMethod);
            Assert.Equal(partORun.SimulationContext.SimulateFrom, partOSimulationContext.SimulateFrom);
            Assert.Equal(partORun.SimulationContext.SimulateTo, partOSimulationContext.SimulateTo);
            Assert.True(partOSimulationContext.IsFullYear);
        }

        /// <summary>
        /// Reference A is captured over the dwelling scope and Candidate B over the rooms the route
        /// bound - and nothing wider. On a real project a full annual series per room is what makes this
        /// expensive.
        /// </summary>
        [Fact]
        public void Each_assessment_captures_only_the_rooms_it_needs()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> guids_Bound);

            Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(2, partOIteration3PipelineFake.Captured.Count);

            List<Guid> guids_Dwelling = [.. guids_Space_Dwelling];
            guids_Dwelling.Sort();

            Assert.Equal(guids_Dwelling, partOIteration3PipelineFake.Captured[0]);
            Assert.Equal(guids_Bound, partOIteration3PipelineFake.Captured[1]);
        }

        /// <summary>The materialisation is given the SCOPED working copy, not the design.</summary>
        [Fact]
        public void The_materialisation_is_given_the_scoped_working_copy_and_the_dwelling_rooms()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.NotNull(partOIteration3PipelineFake.AdjacencyCluster_Materialised);
            Assert.NotSame(adjacencyCluster, partOIteration3PipelineFake.AdjacencyCluster_Materialised);
            Assert.Equal(guids_Space_Dwelling.Count, partOIteration3PipelineFake.Spaces_Materialised.Count);
        }

        //-------------------------------------------------------------------------------------------------
        //The refusal boundaries
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_refused_materialisation_never_reaches_the_thermal_source()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.Materialisation = Materialisation_Refused("Space 'Bedroom 2' is served by air handling units 'MVHR-02' and 'AHU1'.");

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsRefused);
            Assert.Equal(PartOIteration3Stage.Materialisation, partOIteration3Result.Ledger.Stage_Refused);

            //Verbatim.
            Assert.Equal(["Space 'Bedroom 2' is served by air handling units 'MVHR-02' and 'AHU1'."], partOIteration3Result.Reasons);

            partOIteration3PipelineFake.AssertNeverCalled(nameof(IPartOIteration3Pipeline.ThermalSource), nameof(IPartOIteration3Pipeline.Route), nameof(IPartOIteration3Pipeline.ResultantTemperatures), nameof(IPartOIteration3Pipeline.Persist));

            Assert.Null(partOIteration3Result.Comparison);
        }

        [Fact]
        public void A_refused_thermal_source_never_reaches_the_conversion()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.NoIzamThermalSource = new NoIzamThermalSource(null, null, false, false, null, null, ["The thermal source workflow threw."], null);
            partOIteration3PipelineFake.AnalyticalModel_CandidateB = null;

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.ThermalSource, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains("The thermal source workflow threw.", partOIteration3Result.Reasons);

            partOIteration3PipelineFake.AssertNothingAfter(nameof(IPartOIteration3Pipeline.ThermalSource));
        }

        [Fact]
        public void A_cancelled_thermal_source_refuses_and_says_so()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.Cancelled = true;

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.ThermalSource, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains(partOIteration3Result.Reasons, x => x.Contains("cancelled"));
        }

        /// <summary>
        /// A route that produced no simulation evidence never got past the conversion, and the ledger
        /// says so rather than blaming the simulation.
        /// </summary>
        [Fact]
        public void A_route_that_never_simulated_refuses_at_the_conversion()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.SystemVentilationRoute = new SystemVentilationRoute(
                partOIteration3PipelineFake.NoIzamThermalSource,
                null,
                null,
                null,
                null,
                null,
                ["The conversion to TAS Systems did not reconcile against the source graph."],
                null);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.SystemsConversion, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains("The conversion to TAS Systems did not reconcile against the source graph.", partOIteration3Result.Reasons);

            partOIteration3PipelineFake.AssertNothingAfter(nameof(IPartOIteration3Pipeline.Route));
            Assert.Equal(PartOIteration3StageStatus.NotRun, partOIteration3Result.Ledger.State(PartOIteration3Stage.SystemsSimulation).Status);
            Assert.Equal(PartOIteration3StageStatus.NotRun, partOIteration3Result.Ledger.State(PartOIteration3Stage.ZoneTemperature).Status);
        }

        /// <summary>
        /// A route whose simulation is evidenced but whose results did not reconcile refuses at the zone
        /// temperature, with the conversion and the simulation both recorded as done.
        /// </summary>
        [Fact]
        public void A_route_whose_zone_temperature_is_incomplete_refuses_there_and_not_earlier()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            string path_TPD = Path.Combine(directory_It3, "Flat-It3B.tpd");

            partOIteration3PipelineFake.SystemVentilationRoute = new SystemVentilationRoute(
                partOIteration3PipelineFake.NoIzamThermalSource,
                path_TPD,
                PartOIteration3Fixture.Evidence(path_TPD, path_TPD),
                null,
                null,
                null,
                ["Room 0c8f: 12 of 24 zone temperature value(s)."],
                null);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.ZoneTemperature, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Equal(PartOIteration3StageStatus.Completed, partOIteration3Result.Ledger.State(PartOIteration3Stage.SystemsConversion).Status);
            Assert.Equal(PartOIteration3StageStatus.Completed, partOIteration3Result.Ledger.State(PartOIteration3Stage.SystemsSimulation).Status);

            partOIteration3PipelineFake.AssertNothingAfter(nameof(IPartOIteration3Pipeline.Route));
        }

        [Fact]
        public void A_refused_resultant_temperature_never_reaches_candidate_Bs_assessment()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.ResultantTemperatureResults = new ResultantTemperatureResults(
                "the Approved Document O thermostat bridge",
                0,
                23,
                [Guid.NewGuid()],
                null,
                null,
                null,
                ["The bridge could not write its copy of the no-IZAM building."],
                null);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.ResultantTemperature, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains("The bridge could not write its copy of the no-IZAM building.", partOIteration3Result.Reasons);

            //Reference A WAS assessed - that happened before this stage. Candidate B was not.
            Assert.Single(partOIteration3PipelineFake.Captured);
            partOIteration3PipelineFake.AssertNothingAfter(nameof(IPartOIteration3Pipeline.ResultantTemperatures));
        }

        /// <summary>
        /// The two independent readers of Candidate B's own result file must agree. If they ever do not,
        /// one of them is resolving a room to the wrong zone - and both answers are complete, finite and
        /// plausible, so nothing else could see it.
        /// </summary>
        [Fact]
        public void A_provider_and_a_TM59_reading_that_disagree_refuse_before_reconciliation()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> guids_Bound);

            //One hour of one room read back differently - the smallest possible disagreement.
            Guid guid = guids_Bound[0];

            Dictionary<Guid, double[]> resultantTemperatures = [];
            foreach (Guid guid_Space in guids_Bound)
            {
                double[] values = Series(21.0);

                if (guid_Space == guid)
                {
                    values[7] = 21.5;
                }

                resultantTemperatures[guid_Space] = values;
            }

            List<PartOTM59SpaceResult> spaceResults = [];
            foreach (Guid guid_Space in guids_Bound)
            {
                spaceResults.Add(new PartOTM59SpaceResult(guid_Space, "room", "TM59 Criterion A", 10, 32, TM59ComplianceStatus.Pass, true));
            }

            partOIteration3PipelineFake.Assessment_CandidateB = new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Pass, spaceResults, null, null, null, resultantTemperatures, "report", null, spaceResults.Count);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.CandidateBTM59, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains(partOIteration3Result.Reasons, x => x.Contains("hour 7") && x.Contains("resolving this room to different results"));
            Assert.Null(partOIteration3Result.Comparison);
        }

        [Fact]
        public void An_ineligible_run_refuses_at_the_input_and_calls_nothing()
        {
            PartORun partORun = new();

            PartOIteration3PipelineFake partOIteration3PipelineFake = new();

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.Input, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Empty(partOIteration3PipelineFake.Called);
            Assert.All(partOIteration3Result.Ledger.Stages.GetRange(1, 13), x => Assert.Equal(PartOIteration3StageStatus.NotRun, x.Status));
        }

        /// <summary>
        /// A refused pairing is still written down: its ledger is the diagnosis, and a person reopening
        /// the model tomorrow needs it more than they need a completed one.
        /// </summary>
        [Fact]
        public void A_refused_pairing_still_writes_its_record()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.Materialisation = Materialisation_Refused("no");

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(File.Exists(partOIteration3Result.Path_Record));

            PartOIteration3Record partOIteration3Record = Query.PartOIteration3PairingRecord(partOIteration3Result.Path_Record);

            Assert.False(partOIteration3Record.IsComplete);
            Assert.Equal(PartOIteration3Stage.Materialisation, partOIteration3Record.Stage_Refused);
        }

        /// <summary>
        /// An earlier attempt's reopenable Candidate B must not survive this one. Everything else is
        /// proven by ownership; these two are deleted, because they are what a later session acts on.
        /// </summary>
        [Fact]
        public void The_previous_candidate_B_model_and_record_are_deleted_at_the_start_of_an_attempt()
        {
            PartORun partORun = Run();

            string path_Model = Path.Combine(directory_It3, "Flat-It3B-Bridge.sam");
            string path_Record = Path.Combine(directory_It3Reports, "Flat-Iteration3-B0.json");

            File.WriteAllText(path_Model, "an earlier attempt's reopenable Candidate B");
            File.WriteAllText(path_Record, "an earlier attempt's pairing");

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.Materialisation = Materialisation_Refused("no");

            Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.False(File.Exists(path_Model));

            //The record IS rewritten - by this attempt, with this attempt's refusal.
            Assert.True(File.Exists(path_Record));
            Assert.DoesNotContain("an earlier attempt's pairing", File.ReadAllText(path_Record));
        }

        /// <summary>
        /// A file left at a fixed path by an earlier attempt is never reported as this attempt's - the
        /// ownership rule, exercised through the real orchestration rather than through the artifact
        /// type alone.
        /// </summary>
        [Fact]
        public void A_stale_thermal_source_file_refuses_rather_than_being_reported_as_this_attempts()
        {
            PartORun partORun = Run();

            //Created BEFORE the attempt starts and never touched by it, which is exactly the state a
            //failed previous attempt leaves behind.
            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            //The stage writes nothing this time: the files are exactly where the fixture left them, which
            //is the state a failed previous attempt leaves behind.
            partOIteration3PipelineFake.Paths_ThermalSource.Clear();

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.ThermalSource, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains(partOIteration3Result.Reasons, x => x.Contains("unchanged since this attempt started"));

            //And nothing stale is reported as evidence of this attempt.
            Assert.Empty(partOIteration3Result.Ledger.Artifacts);
        }

        //-------------------------------------------------------------------------------------------------
        //The TM59 reports
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A report is recorded, and offered, only where THIS attempt wrote it. On <c>bdc48ef</c> a failed
        /// <c>SavePartOTM59Report</c> still handed back the path it would have written, and the run then
        /// recorded whatever file an earlier assessment had left there as this pairing's report (Codex P2).
        /// The assessment below names exactly that: a path, and an old file at it, that nobody wrote now.
        /// </summary>
        [Fact]
        public void A_TM59_report_this_attempt_did_not_write_is_never_recorded_or_offered()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            string path_Report_A = Query.Path_TM59Report(path_TSD_ReferenceA);
            string path_Report_B = Query.Path_TM59Report(Path.Combine(directory_It3, "Flat-It3B-Bridge.tsd"));

            File.WriteAllText(path_Report_A, "an earlier assessment's Reference A report");
            File.WriteAllText(path_Report_B, "an earlier attempt's Candidate B report");

            partOIteration3PipelineFake.Assessment_ReferenceA = PartOIteration3PipelineFake.WithReport(partOIteration3PipelineFake.Assessment_ReferenceA, path_Report_A);
            partOIteration3PipelineFake.Assessment_CandidateB = PartOIteration3PipelineFake.WithReport(partOIteration3PipelineFake.Assessment_CandidateB, path_Report_B);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            //A report is evidence of an assessment, not an input to the pairing, so the pairing stands.
            Assert.True(partOIteration3Result.IsComplete);

            //Neither old file is recorded, offered or listed as this attempt's.
            Assert.Null(partOIteration3Result.Record.File(PartOIteration3Roles.ReferenceA_TM59Report));
            Assert.Null(partOIteration3Result.Record.File(PartOIteration3Roles.CandidateB_TM59Report));
            Assert.Null(partOIteration3Result.Path_TM59Report_ReferenceA);
            Assert.Null(partOIteration3Result.Path_TM59Report_CandidateB);
            Assert.DoesNotContain(partOIteration3Result.Ledger.Artifacts, x => x.Contains(path_Report_A, StringComparison.OrdinalIgnoreCase) || x.Contains(path_Report_B, StringComparison.OrdinalIgnoreCase));

            //Nor by the record a later session reopens.
            PartOIteration3Record partOIteration3Record = Query.PartOIteration3PairingRecord(partOIteration3Result.Path_Record);

            Assert.Null(partOIteration3Record.File(PartOIteration3Roles.ReferenceA_TM59Report));
            Assert.Null(partOIteration3Record.File(PartOIteration3Roles.CandidateB_TM59Report));

            //Said, not silent - by role and by path.
            Assert.Contains(partOIteration3Result.Notes, x => x.Contains(PartOIteration3Roles.ReferenceA_TM59Report) && x.Contains(path_Report_A));
            Assert.Contains(partOIteration3Result.Notes, x => x.Contains(PartOIteration3Roles.CandidateB_TM59Report) && x.Contains(path_Report_B));

            //And the old files were left exactly as they were.
            Assert.Equal("an earlier assessment's Reference A report", File.ReadAllText(path_Report_A));
            Assert.Equal("an earlier attempt's Candidate B report", File.ReadAllText(path_Report_B));
        }

        /// <summary>
        /// Ownership is decided by change, not by absence: an old report at the same path that this
        /// attempt genuinely overwrote IS this attempt's, and is recorded and offered.
        /// </summary>
        [Fact]
        public void The_TM59_reports_this_attempt_wrote_are_recorded_and_offered_even_over_old_files()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            partOIteration3PipelineFake.Write_Reports = true;

            string path_Report_A = Query.Path_TM59Report(path_TSD_ReferenceA);
            string path_Report_B = Query.Path_TM59Report(Path.Combine(directory_It3, "Flat-It3B-Bridge.tsd"));

            File.WriteAllText(path_Report_A, "an earlier assessment's Reference A report");
            File.WriteAllText(path_Report_B, "an earlier attempt's Candidate B report");

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete);

            Assert.Equal(path_Report_A, partOIteration3Result.Path_TM59Report_ReferenceA);
            Assert.Equal(path_Report_B, partOIteration3Result.Path_TM59Report_CandidateB);

            Assert.Equal(path_Report_A, partOIteration3Result.Record.File(PartOIteration3Roles.ReferenceA_TM59Report)?.Path);
            Assert.Equal(path_Report_B, partOIteration3Result.Record.File(PartOIteration3Roles.CandidateB_TM59Report)?.Path);

            //Candidate B's is claimed as an artifact of this attempt; Reference A's is recorded as a report.
            Assert.Contains(partOIteration3Result.Ledger.Artifacts, x => x.StartsWith(path_Report_B, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(partOIteration3Result.Ledger.Artifacts, x => x.StartsWith(path_Report_A, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The production pipeline's report save, against the lock technique the persistence tests use: a
        /// report that could not be written is handed back as NO path, with the reason - never as the path
        /// of the old file still sitting there.
        /// </summary>
        [Fact]
        public void The_pipeline_hands_back_no_report_path_when_the_report_could_not_be_written()
        {
            string path_TSD = Path.Combine(directory, "Locked-It3B-Bridge.tsd");
            string path_Report = Query.Path_TM59Report(path_TSD);

            File.WriteAllText(path_Report, "an earlier attempt's report");

            TM59AssessmentReport tM59AssessmentReport = new((IEnumerable<Space>)null, null, null, null, null, path_TSD);

            using (FileStream fileStream = new(path_Report, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Null(PartOIteration3Pipeline.Report(path_TSD, tM59AssessmentReport, out string refusal));
                Assert.Contains(path_Report, refusal);
            }

            Assert.Equal("an earlier attempt's report", File.ReadAllText(path_Report));

            //Once it can be written, it is - and only then is its path handed back.
            Assert.Equal(path_Report, PartOIteration3Pipeline.Report(path_TSD, tM59AssessmentReport, out string refusal_None));
            Assert.Null(refusal_None);
            Assert.Equal(tM59AssessmentReport.ToString(), File.ReadAllText(path_Report));
        }

        //-------------------------------------------------------------------------------------------------
        //The Prepare & Run adoption
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// Prepare &amp; Run adopts a preparation through the dialog's own adoption step - over a model of its
        /// own rather than the preparation's, because it rebuilds one after an equipment edit. On
        /// <c>bdc48ef</c> that step dropped the systems the preparation built, so a run prepared from the
        /// dialog could never start Iteration 3 ("preparation built no ventilation system") however
        /// completely it simulated. Found by the licensed UI acceptance; a headless run never reaches it.
        /// </summary>
        [Fact]
        public void The_Prepare_and_Run_adoption_captures_the_systems_the_preparation_built()
        {
            adjacencyCluster = PartOIteration3Fixture.Design(out guids_VentilationSystem, out zones);

            PartOIterationPreparation partOIterationPreparation = new();
            partOIterationPreparation.OverheatingScenarios.AddRange(PartOIteration3Fixture.Scenarios());

            foreach (Guid guid in guids_VentilationSystem)
            {
                partOIterationPreparation.VentilationSystems.Add(adjacencyCluster.GetObject<VentilationSystem>(guid));
            }

            PartORun partORun = new();

            Assert.True(Modify.AdoptPartOPreparation(
                partORun,
                PartOIteration3Fixture.Model(new AdjacencyCluster(adjacencyCluster)),
                partOIterationPreparation,
                new PartOPreparationContext(PartOIteration.BasePassive, zones, null, null)));

            List<Guid> guids_Expected = [.. guids_VentilationSystem];
            guids_Expected.Sort();

            List<Guid> guids_Captured = partORun.Guids_VentilationSystem_Prepared;
            guids_Captured.Sort();

            Assert.NotEmpty(guids_Captured);
            Assert.Equal(guids_Expected, guids_Captured);

            //And the run that follows is one Iteration 3 can start from.
            path_TSD_ReferenceA = Path.Combine(directory, "Flat.tsd");

            Assert.True(partORun.ExpectResults(path_TSD_ReferenceA));

            File.WriteAllText(path_TSD_ReferenceA, "reference A results");

            AnalyticalModel analyticalModel_Workflow = PartOIteration3Fixture.Model(new AdjacencyCluster(adjacencyCluster), "Flat");

            analyticalModel_Workflow.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new Core.SAMCollection<OverheatingScenario>(partORun.OverheatingScenarios));
            analyticalModel_Workflow.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(analyticalModel_Workflow, path_TSD_ReferenceA));

            Assert.True(partORun.Complete(analyticalModel_Workflow, path_TSD_ReferenceA, PartOIteration3Fixture.SimulationContext(directory), out string _));

            PartOIteration3Eligibility partOIteration3Eligibility = Query.PartOIteration3Eligibility(partORun, partORun.IsAssessable(out string refusal_Assessable), refusal_Assessable);

            Assert.True(partOIteration3Eligibility.CanRun, partOIteration3Eligibility.Refusal_Run);
        }

        //-------------------------------------------------------------------------------------------------
        //PR5A (SAM#111 plan §J) - Parity stays the default and the B0 control; Selected-product resolves
        //every scoped air handling unit's already-selected product before materialising, and refuses the
        //whole attempt rather than materialising some units at parity if any one of them cannot resolve.
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// The default - no mode named at all - is Parity, and it is byte-for-byte what this pipeline did
        /// before PR5A existed: no unit settings reach <c>Materialise</c>, and <c>Route</c> gets
        /// <c>ClearToZero</c>.
        /// </summary>
        [Fact]
        public void Parity_is_the_default_and_reaches_materialise_with_no_unit_settings_and_clear_to_zero()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.False(partOIteration3Result.IsRefused);
            Assert.True(partOIteration3Result.IsComplete);

            Assert.Equal(PartOIteration3BehaviourMode.Parity, partOIteration3Result.Record.BehaviourMode);
            Assert.Empty(partOIteration3Result.Record.Equipment);

            Assert.True(
                partOIteration3PipelineFake.UnitSettings_Materialised is null || partOIteration3PipelineFake.UnitSettings_Materialised.Count == 0,
                "Parity mode must not hand Materialise any unit settings.");

            Assert.Equal(SystemVentilationFanHeatGainPolicy.ClearToZero, partOIteration3PipelineFake.FanHeatGainPolicy_Route);
        }

        /// <summary>
        /// Naming Parity explicitly is the same run as naming nothing - the parameter is additive, and no
        /// existing caller's behaviour moves by a single instruction.
        /// </summary>
        [Fact]
        public void Naming_parity_explicitly_produces_the_same_ledger_as_naming_nothing()
        {
            PartORun partORun_Default = Run();
            PartOIteration3Result result_Default = Modify.RunPartOIteration3(partORun_Default, Pipeline_Complete(out List<Guid> _));

            PartORun partORun_Explicit = Run();
            PartOIteration3Result result_Explicit = Modify.RunPartOIteration3(partORun_Explicit, Pipeline_Complete(out List<Guid> _), default, PartOIteration3BehaviourMode.Parity);

            Assert.Equal(result_Default.IsComplete, result_Explicit.IsComplete);
            Assert.Equal(result_Default.Ledger.Stages.Count, result_Explicit.Ledger.Stages.Count);
            Assert.All(result_Explicit.Ledger.Stages, x => Assert.Equal(PartOIteration3StageStatus.Completed, x.Status));
        }

        /// <summary>
        /// Selected-product mode refuses the WHOLE attempt when even one scoped air handling unit has no
        /// selected product - the fixture design's units carry none - and nothing after the equipment
        /// resolution stage is ever reached: not materialisation, not the thermal source, nothing.
        /// <para>
        /// This is the all-or-nothing rule PR5A's plan states explicitly: a partially configured
        /// Candidate B (some units resolved, some silently left at parity) is never produced.
        /// </para>
        /// </summary>
        [Fact]
        public void SelectedProduct_mode_refuses_the_whole_attempt_when_a_unit_has_no_selected_product()
        {
            PartORun partORun = Run();

            //A pipeline that is otherwise fully wired to succeed - so that a defect letting Materialise or
            //anything after it run would prove itself immediately, rather than being masked by a fake that
            //would have refused there anyway.
            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake, default, PartOIteration3BehaviourMode.SelectedProduct);

            Assert.True(partOIteration3Result.IsRefused);
            Assert.False(partOIteration3Result.IsComplete);
            Assert.Equal(PartOIteration3Stage.EquipmentResolution, partOIteration3Result.Ledger.Stage_Refused);

            Assert.Equal(PartOIteration3BehaviourMode.SelectedProduct, partOIteration3Result.Record.BehaviourMode);
            Assert.Empty(partOIteration3Result.Record.Equipment);

            //Reference A's own TM59 assessment runs before equipment resolution even reaches the ledger,
            //so "Assess" is called exactly once - for Reference A - and is deliberately not asserted here.
            partOIteration3PipelineFake.AssertNeverCalled(nameof(IPartOIteration3Pipeline.Materialise), nameof(IPartOIteration3Pipeline.ThermalSource), nameof(IPartOIteration3Pipeline.Route), nameof(IPartOIteration3Pipeline.ResultantTemperatures), nameof(IPartOIteration3Pipeline.Persist));

            Assert.Equal(["Assess"], partOIteration3PipelineFake.Called);

            //Every reason names the unit by identity, never a substituted product.
            Assert.All(partOIteration3Result.Ledger.Reasons, x => Assert.Contains("no selected ventilation unit product", x));
        }

        /// <summary>
        /// PR5B: the selected-product cooling mode (B4) is all-or-nothing in exactly the same way - a unit with
        /// no selected product refuses the whole attempt at equipment resolution, nothing is materialised or
        /// simulated, no cooling row is recorded, and no fan or heat-recovery behaviour is resolved either.
        /// </summary>
        [Fact]
        public void SelectedProductCooling_mode_refuses_the_whole_attempt_when_a_unit_has_no_selected_product()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake, default, PartOIteration3BehaviourMode.SelectedProductCooling);

            Assert.True(partOIteration3Result.IsRefused);
            Assert.Equal(PartOIteration3Stage.EquipmentResolution, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Equal(PartOIteration3BehaviourMode.SelectedProductCooling, partOIteration3Result.Record.BehaviourMode);
            Assert.Empty(partOIteration3Result.Record.Cooling);
            Assert.Empty(partOIteration3Result.Record.Equipment);

            partOIteration3PipelineFake.AssertNeverCalled(nameof(IPartOIteration3Pipeline.Materialise), nameof(IPartOIteration3Pipeline.ThermalSource), nameof(IPartOIteration3Pipeline.Route), nameof(IPartOIteration3Pipeline.ResultantTemperatures), nameof(IPartOIteration3Pipeline.Persist));

            Assert.All(partOIteration3Result.Ledger.Reasons, x => Assert.Contains("no selected ventilation unit product", x));

            //The B4 attempt writes under its own project name, so it can never overwrite the B0 control's files.
            Assert.EndsWith(PartOIteration3Paths.Suffix_CandidateB_Cooling, partOIteration3Result.Record.ProjectName_CandidateB);
        }

        /// <summary>PR5B: Parity and Selected-product runs hand the materialisation no cooling at all - B0 is untouched by the cooling mode existing.</summary>
        [Fact]
        public void Parity_mode_hands_the_materialisation_no_cooling_settings()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete, string.Join("; ", partOIteration3Result.Ledger.Reasons));
            Assert.True(partOIteration3PipelineFake.CoolingSettings_Materialised is null || partOIteration3PipelineFake.CoolingSettings_Materialised.Count == 0);
            Assert.Empty(partOIteration3Result.Record.Cooling);
            Assert.EndsWith(PartOIteration3Paths.Suffix_CandidateB, partOIteration3Result.Record.ProjectName_CandidateB);
        }

        //-------------------------------------------------------------------------------------------------
        //Workflow simplification: per-method record, progress stages, cancellation between stages
        //-------------------------------------------------------------------------------------------------

        /// <summary>A run writes its own method's record, never the mode-independent legacy one.</summary>
        [Fact]
        public void A_run_writes_its_own_methods_record()
        {
            PartORun partORun = Run();

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, Pipeline_Complete(out List<Guid> _));

            Assert.True(partOIteration3Result.IsComplete, string.Join("; ", partOIteration3Result.Ledger.Reasons));
            Assert.Equal(Path.Combine(directory_It3Reports, "Flat-Iteration3-B0.json"), partOIteration3Result.Path_Record);
            Assert.True(File.Exists(partOIteration3Result.Path_Record));
            Assert.False(File.Exists(Path.Combine(directory, "Flat-Iteration3.json")));
        }

        /// <summary>
        /// The progress window is told every stage as it starts, in the ledger's order, and the six phases it
        /// shows only ever move forward.
        /// </summary>
        [Fact]
        public void Every_stage_is_announced_in_order_and_the_phases_only_move_forward()
        {
            PartORun partORun = Run();

            List<PartOIteration3Stage> announced = [];

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, Pipeline_Complete(out List<Guid> _), default, PartOIteration3BehaviourMode.Parity, announced.Add);

            Assert.True(partOIteration3Result.IsComplete);

            Assert.Equal(PartOIteration3Stage.Input, announced[0]);
            Assert.Equal(PartOIteration3Stage.Persistence, announced[^1]);

            for (int i = 1; i < announced.Count; i++)
            {
                Assert.True(announced[i] > announced[i - 1], string.Format("{0} was announced after {1}", announced[i], announced[i - 1]));
                Assert.True(Modify.PartOIteration3Phase(announced[i], PartOIteration3BehaviourMode.Parity) >= Modify.PartOIteration3Phase(announced[i - 1], PartOIteration3BehaviourMode.Parity));
            }

            //Every phase a Parity run lists is reached by a complete run. The ledger's SystemsSimulation stage is
            //never announced - the route announces it - so that phase is reached through the route (see the
            //PartOProgressStageTests); every other phase is reached by an announced stage.
            List<int> phases = announced.ConvertAll(x => Modify.PartOIteration3Phase(x, PartOIteration3BehaviourMode.Parity));
            Assert.Equal(Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity).Count - 1, phases.Distinct().Count());
        }

        /// <summary>
        /// Cancel is honoured between stages: a cancel requested before the building simulation starts stops
        /// the run there, calls no TAS step at all, and is recorded as a refusal - so the method is not
        /// reviewable afterwards and can simply be run again.
        /// </summary>
        [Fact]
        public void A_cancel_before_the_tas_stages_stops_the_run_there_and_calls_no_tas_step()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            using System.Threading.CancellationTokenSource cancellationTokenSource = new();

            //Cancelled as soon as the system case design starts - as a person would click Cancel while the
            //reference case is still being read.
            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(
                partORun,
                partOIteration3PipelineFake,
                cancellationTokenSource.Token,
                PartOIteration3BehaviourMode.Parity,
                stage =>
                {
                    if (stage == PartOIteration3Stage.Materialisation)
                    {
                        cancellationTokenSource.Cancel();
                    }
                });

            Assert.True(partOIteration3Result.IsRefused);
            Assert.Equal(PartOIteration3Stage.ThermalSource, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains("cancelled", string.Join(" ", partOIteration3Result.Ledger.Reasons));

            Assert.DoesNotContain(nameof(IPartOIteration3Pipeline.ThermalSource), partOIteration3PipelineFake.Called);
            Assert.DoesNotContain(nameof(IPartOIteration3Pipeline.Route), partOIteration3PipelineFake.Called);
            Assert.DoesNotContain(nameof(IPartOIteration3Pipeline.ResultantTemperatures), partOIteration3PipelineFake.Called);

            //Recorded, and not reviewable - so it does not lock the method into Review.
            PartOIteration3PairingStatus partOIteration3PairingStatus = Query.PartOIteration3PairingStatus(partORun.Path_TSD, PartOIteration3BehaviourMode.Parity);

            Assert.True(partOIteration3PairingStatus.IsRefused);
            Assert.False(partOIteration3PairingStatus.IsReviewable);
        }

        /// <summary>
        /// The progress window over a real complete run: the production announcer drives it from the ledger's
        /// stages and, inside the route, from the events SAM_Tas' route reports. The window shows the run's
        /// seven stages once each, in order, with the air-system counts, and ends with every stage completed.
        /// </summary>
        [Fact]
        public void The_progress_window_walks_a_complete_run_through_its_stages_once_each_and_completes_them()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            using PartOProgressHost partOProgressHost = new("Iteration 3", null, Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity), true, false);

            List<string> running = [];
            List<string> details = [];

            void Record()
            {
                for (int i = 0; i < partOProgressHost.State.Count; i++)
                {
                    if (partOProgressHost.State.Status(i) == PartOProgressStageStatus.Running)
                    {
                        if (running.Count == 0 || running[^1] != partOProgressHost.State.Name(i))
                        {
                            running.Add(partOProgressHost.State.Name(i));
                        }

                        if (partOProgressHost.State.Detail is string detail)
                        {
                            details.Add(detail);
                        }
                    }
                }
            }

            partOIteration3PipelineFake.Reporting_Route = () =>
            {
                for (int i = 1; i <= 3; i++)
                {
                    Modify.ReportPartOSystemVentilationProgress(PartOProgressHost.Current, new Analytical.Tas.TPD.SystemVentilationRouteProgress(Analytical.Tas.TPD.SystemVentilationRouteStage.ConvertingAirSystems, i, 3));
                    Record();
                }

                for (int i = 1; i <= 3; i++)
                {
                    Modify.ReportPartOSystemVentilationProgress(PartOProgressHost.Current, new Analytical.Tas.TPD.SystemVentilationRouteProgress(Analytical.Tas.TPD.SystemVentilationRouteStage.SimulatingAirSystems, i, 3));
                    Record();
                }
            };

            Action<PartOIteration3Stage> announcer = Modify.PartOIteration3StageAnnouncer(partOProgressHost, PartOIteration3BehaviourMode.Parity);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(
                partORun,
                partOIteration3PipelineFake,
                partOProgressHost.Token,
                PartOIteration3BehaviourMode.Parity,
                stage =>
                {
                    announcer(stage);
                    Record();
                });

            Assert.True(partOIteration3Result.IsComplete);

            partOProgressHost.State.Complete();

            Assert.Equal(Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity), running);

            Assert.Contains("Air system 2 of 3", details);
            Assert.Contains("Air system 3 of 3", details);

            for (int i = 0; i < partOProgressHost.State.Count; i++)
            {
                Assert.Equal(PartOProgressStageStatus.Completed, partOProgressHost.State.Status(i));
            }
        }

        /// <summary>
        /// Cancel and refusal are unchanged by progress reporting: a cancel before the building simulation
        /// still refuses at that stage, calls no TAS step, and the window ends with that stage failed and the
        /// later ones never started.
        /// </summary>
        [Fact]
        public void A_cancelled_run_still_refuses_at_the_stage_it_did_not_start_and_the_window_fails_that_stage()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);

            using PartOProgressHost partOProgressHost = new("Iteration 3", null, Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity), true, false);

            Action<PartOIteration3Stage> announcer = Modify.PartOIteration3StageAnnouncer(partOProgressHost, PartOIteration3BehaviourMode.Parity);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(
                partORun,
                partOIteration3PipelineFake,
                partOProgressHost.Token,
                PartOIteration3BehaviourMode.Parity,
                stage =>
                {
                    if (stage == PartOIteration3Stage.Materialisation)
                    {
                        partOProgressHost.Cancel();
                    }

                    announcer(stage);
                });

            Assert.True(partOIteration3Result.IsRefused);
            Assert.Equal(PartOIteration3Stage.ThermalSource, partOIteration3Result.Ledger.Stage_Refused);
            Assert.DoesNotContain(nameof(IPartOIteration3Pipeline.ThermalSource), partOIteration3PipelineFake.Called);

            partOProgressHost.State.Fail();

            int index_Building = partOProgressHost.State.IndexOf(PartOProgressStages.BuildingSimulationThermalSource);

            Assert.Equal(PartOProgressStageStatus.Failed, partOProgressHost.State.Status(index_Building));
            Assert.Equal(PartOProgressStageStatus.Completed, partOProgressHost.State.Status(0));

            for (int i = index_Building + 1; i < partOProgressHost.State.Count; i++)
            {
                Assert.Equal(PartOProgressStageStatus.Pending, partOProgressHost.State.Status(i));
            }
        }
    }
}
