// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.TPD;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Part O progress stages: every stage the window lists names one real operation the run performs, a run
    /// lists only the stages it performs, and a count ("Air system 2 of 3") is shown only where SAM_Tas
    /// counted real air systems. COM-free: SAM_Tas' route events are fed in by hand, in the order and with
    /// the counts the production route reports them.
    /// </summary>
    public class PartOProgressStageTests
    {
        private static readonly string[] Umbrellas =
        [
            "TAS Systems simulation",
            "TAS resultant temperature",
            "TM59 comparison and reports",
            "ventilation legs",
        ];

        private static string Running(PartOProgressState partOProgressState)
        {
            for (int i = 0; i < partOProgressState.Count; i++)
            {
                if (partOProgressState.Status(i) == PartOProgressStageStatus.Running)
                {
                    return partOProgressState.Name(i);
                }
            }

            return null;
        }

        private static string Snapshot(PartOProgressHost partOProgressHost)
        {
            string running = Running(partOProgressHost.State);

            return partOProgressHost.State.Detail is string detail ? string.Format("{0} | {1}", running, detail) : running;
        }

        //Exactly what Create.SystemVentilationRoute reports for a three-air-system route, in order.
        private static IEnumerable<SystemVentilationRouteProgress> RouteEvents(bool guidance, bool cooling = false)
        {
            for (int i = 1; i <= 3; i++)
            {
                yield return new SystemVentilationRouteProgress(SystemVentilationRouteStage.ConvertingAirSystems, i, 3);
            }

            yield return new SystemVentilationRouteProgress(SystemVentilationRouteStage.ReconcilingConversion, 0, 0);

            for (int i = 1; i <= 3; i++)
            {
                yield return new SystemVentilationRouteProgress(SystemVentilationRouteStage.SimulatingAirSystems, i, 3);
            }

            if (cooling)
            {
                yield return new SystemVentilationRouteProgress(SystemVentilationRouteStage.ReadingRecirculationCooling, 0, 0);
            }

            if (guidance)
            {
                yield return new SystemVentilationRouteProgress(SystemVentilationRouteStage.ReadingManufacturerGuidance, 0, 0);
            }
        }

        //The production announcer for every ledger stage the run announces (SystemsSimulation and
        //ZoneTemperature are never announced - the route's events drive them), in order, with the route's
        //events at the point the route runs. Returns what the window showed after each step.
        private static List<string> Walk(PartOIteration3BehaviourMode partOIteration3BehaviourMode, IEnumerable<SystemVentilationRouteProgress> events)
        {
            using PartOProgressHost partOProgressHost = new("Iteration 3", null, Modify.PartOIteration3Phases(partOIteration3BehaviourMode), true, false);

            Action<PartOIteration3Stage> announcer = Modify.PartOIteration3StageAnnouncer(partOProgressHost, partOIteration3BehaviourMode);

            List<string> result = [];

            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                if (partOIteration3Stage == PartOIteration3Stage.SystemsSimulation || partOIteration3Stage == PartOIteration3Stage.ZoneTemperature)
                {
                    continue;
                }

                announcer(partOIteration3Stage);
                result.Add(Snapshot(partOProgressHost));

                if (partOIteration3Stage == PartOIteration3Stage.SystemsConversion)
                {
                    foreach (SystemVentilationRouteProgress systemVentilationRouteProgress in events)
                    {
                        Modify.ReportPartOSystemVentilationProgress(partOProgressHost, systemVentilationRouteProgress);
                        result.Add(Snapshot(partOProgressHost));
                    }
                }
            }

            return result;
        }

        private static List<string> Stages(List<string> snapshots)
        {
            List<string> result = [];

            foreach (string snapshot in snapshots)
            {
                string stage = snapshot.Split(" | ")[0];

                if (result.Count == 0 || result[^1] != stage)
                {
                    result.Add(stage);
                }
            }

            return result;
        }

        //-------------------------------------------------------------------------------------------------
        //Iteration 3: the stages a method performs
        //-------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(PartOIteration3BehaviourMode.Parity)]
        [InlineData(PartOIteration3BehaviourMode.SelectedProduct)]
        public void A_method_without_a_guidance_or_cooling_read_back_lists_no_such_stage(PartOIteration3BehaviourMode partOIteration3BehaviourMode)
        {
            Assert.Equal(
                [
                    PartOProgressStages.PreparingSystemCase,
                    PartOProgressStages.BuildingSimulationThermalSource,
                    PartOProgressStages.CreatingVentilationSystems,
                    PartOProgressStages.RunningTasSystems,
                    PartOProgressStages.CalculatingResultantTemperatures,
                    PartOProgressStages.AssessingTm59,
                    PartOProgressStages.ComparingAndSaving,
                ],
                Modify.PartOIteration3Phases(partOIteration3BehaviourMode));
        }

        [Fact]
        public void The_manufacturer_guidance_stage_is_listed_for_the_guidance_method_only_and_between_the_systems_run_and_the_resultant_temperatures()
        {
            IReadOnlyList<string> phases = Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.Equal(8, phases.Count);
            Assert.Equal(PartOProgressStages.EvaluatingManufacturerGuidance, phases[4]);
            Assert.Equal(PartOProgressStages.RunningTasSystems, phases[3]);
            Assert.Equal(PartOProgressStages.CalculatingResultantTemperatures, phases[5]);

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues<PartOIteration3BehaviourMode>())
            {
                Assert.Equal(
                    partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                    Modify.PartOIteration3Phases(partOIteration3BehaviourMode).Contains(PartOProgressStages.EvaluatingManufacturerGuidance));
            }
        }

        [Fact]
        public void The_cooling_module_stage_is_listed_for_the_cooling_method_only()
        {
            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues<PartOIteration3BehaviourMode>())
            {
                Assert.Equal(
                    partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProductCooling,
                    Modify.PartOIteration3Phases(partOIteration3BehaviourMode).Contains(PartOProgressStages.EvaluatingCoolingModules));
            }
        }

        [Fact]
        public void The_resultant_temperature_and_TM59_stages_are_their_own_and_come_after_the_TAS_systems_run()
        {
            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues<PartOIteration3BehaviourMode>())
            {
                IReadOnlyList<string> phases = Modify.PartOIteration3Phases(partOIteration3BehaviourMode);

                int running = phases.ToList().IndexOf(PartOProgressStages.RunningTasSystems);
                int resultant = phases.ToList().IndexOf(PartOProgressStages.CalculatingResultantTemperatures);
                int assess = phases.ToList().IndexOf(PartOProgressStages.AssessingTm59);
                int comparing = phases.ToList().IndexOf(PartOProgressStages.ComparingAndSaving);

                Assert.True(running >= 0 && running < resultant && resultant < assess && assess < comparing);

                //One ledger stage each, so neither can be folded into the systems run.
                Assert.Equal(resultant, Modify.PartOIteration3Phase(PartOIteration3Stage.ResultantTemperature, partOIteration3BehaviourMode));
                Assert.Equal(assess, Modify.PartOIteration3Phase(PartOIteration3Stage.CandidateBTM59, partOIteration3BehaviourMode));
                Assert.NotEqual(running, resultant);
            }
        }

        [Fact]
        public void No_iteration_3_stage_or_step_carries_the_old_umbrella_wording()
        {
            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues<PartOIteration3BehaviourMode>())
            {
                foreach (string phase in Modify.PartOIteration3Phases(partOIteration3BehaviourMode))
                {
                    foreach (string umbrella in Umbrellas)
                    {
                        Assert.DoesNotContain(umbrella, phase, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                string detail = Modify.PartOIteration3StageDetail(partOIteration3Stage);

                foreach (string umbrella in Umbrellas)
                {
                    Assert.DoesNotContain(umbrella, detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        //-------------------------------------------------------------------------------------------------
        //Stage sequences
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_parity_run_shows_its_seven_stages_in_order_and_never_a_guidance_stage()
        {
            List<string> snapshots = Walk(PartOIteration3BehaviourMode.Parity, RouteEvents(false));

            Assert.Equal(
                [
                    PartOProgressStages.PreparingSystemCase,
                    PartOProgressStages.BuildingSimulationThermalSource,
                    PartOProgressStages.CreatingVentilationSystems,
                    PartOProgressStages.RunningTasSystems,
                    PartOProgressStages.CalculatingResultantTemperatures,
                    PartOProgressStages.AssessingTm59,
                    PartOProgressStages.ComparingAndSaving,
                ],
                Stages(snapshots));

            Assert.DoesNotContain(snapshots, x => x.Contains("manufacturer guidance", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void A_guidance_run_evaluates_the_guidance_after_the_systems_run_and_before_the_resultant_temperatures()
        {
            List<string> snapshots = Walk(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, RouteEvents(true));

            Assert.Equal(
                [
                    PartOProgressStages.PreparingSystemCase,
                    PartOProgressStages.BuildingSimulationThermalSource,
                    PartOProgressStages.CreatingVentilationSystems,
                    PartOProgressStages.RunningTasSystems,
                    PartOProgressStages.EvaluatingManufacturerGuidance,
                    PartOProgressStages.CalculatingResultantTemperatures,
                    PartOProgressStages.AssessingTm59,
                    PartOProgressStages.ComparingAndSaving,
                ],
                Stages(snapshots));
        }

        [Fact]
        public void A_cooling_run_evaluates_the_cooling_modules_and_shows_no_guidance_stage()
        {
            List<string> snapshots = Walk(PartOIteration3BehaviourMode.SelectedProductCooling, RouteEvents(false, true));

            Assert.Contains(PartOProgressStages.EvaluatingCoolingModules, Stages(snapshots));
            Assert.DoesNotContain(PartOProgressStages.EvaluatingManufacturerGuidance, Stages(snapshots));
        }

        [Fact]
        public void The_items_of_the_air_system_stages_are_real_counts_in_order()
        {
            List<string> snapshots = Walk(PartOIteration3BehaviourMode.Parity, RouteEvents(false));

            List<string> converting = snapshots.Where(x => x.StartsWith(PartOProgressStages.CreatingVentilationSystems)).ToList();
            List<string> running = snapshots.Where(x => x.StartsWith(PartOProgressStages.RunningTasSystems)).ToList();

            Assert.Contains($"{PartOProgressStages.CreatingVentilationSystems} | Air system 1 of 3", converting);
            Assert.Contains($"{PartOProgressStages.CreatingVentilationSystems} | Air system 2 of 3", converting);
            Assert.Contains($"{PartOProgressStages.CreatingVentilationSystems} | Air system 3 of 3", converting);

            //Reconciling the finished document is named as that, not as a fourth air system.
            Assert.Contains(converting, x => x.Contains("Checking the systems against the ventilation design"));

            Assert.Equal(
                [
                    $"{PartOProgressStages.RunningTasSystems} | Air system 1 of 3",
                    $"{PartOProgressStages.RunningTasSystems} | Air system 2 of 3",
                    $"{PartOProgressStages.RunningTasSystems} | Air system 3 of 3",
                ],
                running);
        }

        [Fact]
        public void A_stage_that_shares_a_phase_does_not_keep_the_previous_stages_step()
        {
            List<string> snapshots = Walk(PartOIteration3BehaviourMode.Parity, RouteEvents(false));

            Assert.Equal($"{PartOProgressStages.PreparingSystemCase} | Checking the reference run", snapshots[0]);
            Assert.Contains($"{PartOProgressStages.PreparingSystemCase} | Materialising the ventilation systems", snapshots);

            //The building simulation names its own steps (the TBD / workflow steps report into the window), so
            //entering it clears the previous step rather than leaving "Materialising..." on screen.
            Assert.Contains(PartOProgressStages.BuildingSimulationThermalSource, snapshots);
        }

        //-------------------------------------------------------------------------------------------------
        //Counts are real or absent
        //-------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 0)]
        [InlineData(0, 3)]
        [InlineData(4, 3)]
        public void No_count_is_shown_where_the_route_did_not_count_real_items(int current, int total)
        {
            SystemVentilationRouteProgress systemVentilationRouteProgress = new(SystemVentilationRouteStage.SimulatingAirSystems, current, total);

            Assert.False(systemVentilationRouteProgress.HasCount);

            using PartOProgressHost partOProgressHost = new("Iteration 3", null, Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity), true, false);

            Modify.ReportPartOSystemVentilationProgress(partOProgressHost, systemVentilationRouteProgress);

            Assert.Equal(PartOProgressStages.RunningTasSystems, Running(partOProgressHost.State));
            Assert.Null(partOProgressHost.State.Detail);
            Assert.False(partOProgressHost.State.IsDeterminate);
        }

        [Fact]
        public void A_count_never_becomes_a_percentage()
        {
            using PartOProgressHost partOProgressHost = new("Iteration 3", null, Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity), true, false);

            foreach (SystemVentilationRouteProgress systemVentilationRouteProgress in RouteEvents(false))
            {
                Modify.ReportPartOSystemVentilationProgress(partOProgressHost, systemVentilationRouteProgress);

                Assert.False(partOProgressHost.State.IsDeterminate);
                Assert.Null(partOProgressHost.State.Percent);
            }
        }

        //-------------------------------------------------------------------------------------------------
        //One-stage windows (the mixed design run) and no window at all
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_window_with_one_TAS_stage_carries_the_whole_line_in_its_step_and_keeps_its_stages()
        {
            string[] stages = ["Materialise the mixed model from the baseline", "TAS simulation (full year) and TM59 assessment"];

            using PartOProgressHost partOProgressHost = new("Build & Run Mixed Design", null, stages, true, false);

            partOProgressHost.Start(1);

            Modify.ReportPartOSystemVentilationProgress(partOProgressHost, new SystemVentilationRouteProgress(SystemVentilationRouteStage.SimulatingAirSystems, 2, 3));

            Assert.Equal("TAS simulation (full year) and TM59 assessment", Running(partOProgressHost.State));
            Assert.Equal($"{PartOProgressStages.RunningTasSystems} — Air system 2 of 3", partOProgressHost.State.Detail);

            Modify.ReportPartOSystemVentilationProgress(partOProgressHost, new SystemVentilationRouteProgress(SystemVentilationRouteStage.ReadingManufacturerGuidance, 0, 0));

            Assert.Equal(PartOProgressStages.EvaluatingManufacturerGuidance, partOProgressHost.State.Detail);
            Assert.Equal(2, partOProgressHost.State.Count);
        }

        [Fact]
        public void Progress_is_optional_no_host_and_no_event_are_harmless()
        {
            Modify.ReportPartOSystemVentilationProgress(null, new SystemVentilationRouteProgress(SystemVentilationRouteStage.SimulatingAirSystems, 1, 3));

            using PartOProgressHost partOProgressHost = new("Iteration 3", null, Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity), true, false);

            Modify.ReportPartOSystemVentilationProgress(partOProgressHost, null);

            Assert.Null(Running(partOProgressHost.State));
        }

        //-------------------------------------------------------------------------------------------------
        //Prepare & Run (Iteration 1a and 2): the TBD / IZAM route, no TAS Systems work
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void The_prepare_and_run_stages_name_a_building_simulation_and_no_systems_or_guidance_work()
        {
            string[] stages = [PartOProgressStages.PrepareAndReview, PartOProgressStages.BuildingSimulationFullYear, PartOProgressStages.Tm59Assessment];

            foreach (string stage in stages)
            {
                Assert.DoesNotContain("Systems", stage);
                Assert.DoesNotContain("guidance", stage, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
