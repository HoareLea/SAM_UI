// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The three pieces of state an Iteration 3 run stands on: the captured ventilation-system
    /// identities, the complete copy of the thermal case, and the frozen parity operating schedule.
    /// <para>
    /// Each is a place where a quiet omission produces a comparison that is wrong rather than refused -
    /// a stale system scope that still resolves, a copied case missing one option, a schedule that is
    /// constant for one day and zero for the rest of the year.
    /// </para>
    /// </summary>
    public class PartOIteration3StateTests
    {
        private static PartOPreparationContext PreparationContext(IEnumerable<Zone> zones)
        {
            return new PartOPreparationContext(PartOIteration.BasePassive, zones, null, null);
        }

        //-------------------------------------------------------------------------------------------------
        //The captured ventilation-system identities - SAM #114's production answer
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void Preparing_captures_the_ventilation_system_identities_and_exposes_them_read_only()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones);

            PartORun partORun = new();

            Assert.True(partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones), guids));

            Assert.Equal(guids, partORun.Guids_VentilationSystem_Prepared);

            //Read-only: the list handed out is a copy, so a caller cannot reach into the run's scope.
            partORun.Guids_VentilationSystem_Prepared.Clear();

            Assert.Equal(guids.Count, partORun.Guids_VentilationSystem_Prepared.Count);
        }

        [Fact]
        public void Resetting_clears_the_captured_identities()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones);

            PartORun partORun = new();

            partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones), guids);
            partORun.Reset();

            Assert.Empty(partORun.Guids_VentilationSystem_Prepared);
        }

        /// <summary>
        /// The sharpest case: a dropped run's captured identities must not scope the NEXT run's
        /// materialisation. Every one of them would still resolve on the model, so nothing downstream
        /// could tell.
        /// </summary>
        [Fact]
        public void Invalidating_clears_the_captured_identities()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones);

            PartORun partORun = new();

            partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones), guids);
            partORun.Invalidate("the model changed");

            Assert.Empty(partORun.Guids_VentilationSystem_Prepared);
        }

        [Fact]
        public void Preparing_again_replaces_the_captured_identities_rather_than_adding_to_them()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones);

            PartORun partORun = new();

            partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones), guids);

            Guid guid = Guid.NewGuid();

            partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones), [guid]);

            Assert.Equal([guid], partORun.Guids_VentilationSystem_Prepared);
        }

        [Fact]
        public void A_preparation_that_states_none_leaves_the_run_with_no_scope_rather_than_a_stale_one()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones);

            PartORun partORun = new();

            partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones), guids);
            partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones), null);

            Assert.Empty(partORun.Guids_VentilationSystem_Prepared);
        }

        [Fact]
        public void The_overload_that_states_no_identities_keeps_every_existing_callers_behaviour()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> _, out List<Zone> zones);

            PartORun partORun = new();

            Assert.True(partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), PreparationContext(zones)));
            Assert.Empty(partORun.Guids_VentilationSystem_Prepared);
        }

        [Fact]
        public void A_run_with_no_prepared_state_reports_no_identities()
        {
            Assert.Empty(new PartORun().Guids_VentilationSystem_Prepared);
        }

        //-------------------------------------------------------------------------------------------------
        //The thermal case
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>Every public property of the case is carried.</b> Asserted by reflection rather than by
        /// listing them, so a property added later cannot quietly fail to be copied - which would make
        /// Candidate B a different simulation of a different building and put that difference into the
        /// A/B statistics.
        /// </summary>
        [Fact]
        public void Copying_the_simulation_context_carries_every_property_except_the_two_that_must_move()
        {
            PartOSimulationContext partOSimulationContext = PartOIteration3Fixture.SimulationContext("C:\\out", "Flat");

            //Every settable option moved OFF its default, so a copy that missed one shows up as the
            //default rather than as the value.
            partOSimulationContext.UnmetHours = false;
            partOSimulationContext.Sizing = false;
            partOSimulationContext.UseWidths = true;
            partOSimulationContext.UpdateConstructionLayersByPanelType = false;

            PartOSimulationContext result = partOSimulationContext.Copy("Flat-It3B");

            HashSet<string> moved = ["ProjectName"];

            //Derived from the properties above, so it follows them rather than being carried.
            HashSet<string> derived = ["IsFullYear"];

            int count = 0;

            foreach (PropertyInfo propertyInfo in typeof(PartOSimulationContext).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.GetIndexParameters().Length != 0 || derived.Contains(propertyInfo.Name))
                {
                    continue;
                }

                count++;

                object value = propertyInfo.GetValue(partOSimulationContext);
                object value_Copy = propertyInfo.GetValue(result);

                if (moved.Contains(propertyInfo.Name))
                {
                    Assert.NotEqual(value, value_Copy);

                    continue;
                }

                Assert.Equal(value, value_Copy);
            }

            //A guard on the guard: if the type ever loses its properties, the loop above would pass
            //vacuously.
            Assert.True(count >= 9, string.Format("Only {0} properties were compared; the reflection guard is not covering the type.", count));

            Assert.Equal("Flat-It3B", result.ProjectName);
            Assert.Equal("C:\\out", result.OutputDirectory);
            Assert.True(result.IsFullYear);

            //The weather is shared rather than cloned: it is an input both cases read and neither writes.
            Assert.Same(partOSimulationContext.WeatherData, result.WeatherData);
        }

        [Fact]
        public void Copying_the_simulation_context_can_redirect_the_output_directory_too()
        {
            PartOSimulationContext result = PartOIteration3Fixture.SimulationContext("C:\\out", "Flat").Copy("Flat-It3B", "C:\\elsewhere");

            Assert.Equal("C:\\elsewhere", result.OutputDirectory);
            Assert.Equal("Flat-It3B", result.ProjectName);
        }

        //-------------------------------------------------------------------------------------------------
        //The frozen parity operating schedule
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// The name was accepted by PR1's and PR2's licensed acceptances and components reference
        /// schedules by name, so it is pinned letter for letter. SAM #113 has already shown that TAS
        /// behaviour can depend on naming.
        /// </summary>
        [Fact]
        public void The_operating_schedule_name_is_exactly_the_accepted_one()
        {
            Assert.Equal("Part O continuous operation", Query.Name_PartOIteration3OperatingSchedule);
            Assert.Equal(Query.Name_PartOIteration3OperatingSchedule, Query.PartOIteration3OperatingSchedule().Name);
        }

        /// <summary>
        /// 8760 values of exactly 1.0 - not 24, which is the defect PR1 had to fix in
        /// <c>YearlySchedule</c>'s copy constructor and which made an always-on system run for one day a
        /// year while every stage downstream reported success.
        /// </summary>
        [Fact]
        public void The_operating_schedule_is_8760_values_of_exactly_one()
        {
            double[] values = Query.PartOIteration3OperatingSchedule().Values;

            Assert.Equal(8760, values.Length);
            Assert.All(values, x => Assert.Equal(1.0, x));
        }

        /// <summary>
        /// And it survives the copy SAM_Systems takes of it on the way in - which is the step that used
        /// to lose 8736 of the hours.
        /// </summary>
        [Fact]
        public void The_operating_schedule_survives_the_materialisation_settings_copy()
        {
            MechanicalVentilationSettings mechanicalVentilationSettings = new()
            {
                Schedule = Query.PartOIteration3OperatingSchedule(),
            };

            MechanicalVentilationSettings result = new(mechanicalVentilationSettings);

            YearlySchedule yearlySchedule = Assert.IsType<YearlySchedule>(result.Schedule);

            Assert.Equal(Query.Name_PartOIteration3OperatingSchedule, yearlySchedule.Name);
            Assert.Equal(8760, yearlySchedule.Values.Length);
            Assert.All(yearlySchedule.Values, x => Assert.Equal(1.0, x));
        }

        [Fact]
        public void Each_call_answers_its_own_schedule_rather_than_a_shared_one()
        {
            Assert.NotSame(Query.PartOIteration3OperatingSchedule(), Query.PartOIteration3OperatingSchedule());
        }

        //-------------------------------------------------------------------------------------------------
        //The deterministic paths
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void Candidate_B_is_named_from_reference_A_and_never_as_an_optimisation_round()
        {
            PartOIteration3Paths partOIteration3Paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd");

            Assert.Equal("Flat1-It3B", partOIteration3Paths.ProjectName_CandidateB);
            Assert.Equal("Flat1-It3B-Bridge", partOIteration3Paths.ProjectName_Bridge);

            //Every TAS file in Iteration 3's own tas folder beneath the reference's root; the record in its reports.
            Assert.Equal(Path.Combine("C:\\out", "Iteration3", "tas", "Flat1-It3B.tbd"), partOIteration3Paths.Path_TBD_ThermalSource);
            Assert.Equal(Path.Combine("C:\\out", "Iteration3", "tas", "Flat1-It3B.tsd"), partOIteration3Paths.Path_TSD_ThermalSource);
            Assert.Equal(Path.Combine("C:\\out", "Iteration3", "tas", "Flat1-It3B.tpd"), partOIteration3Paths.Path_TPD);
            Assert.Equal(Path.Combine("C:\\out", "Iteration3", "tas", "Flat1-It3B-Bridge.tbd"), partOIteration3Paths.Path_TBD_Bridge);
            Assert.Equal(Path.Combine("C:\\out", "Iteration3", "tas", "Flat1-It3B-Bridge.tsd"), partOIteration3Paths.Path_TSD_Bridge);
            Assert.Equal(Path.Combine("C:\\out", "Iteration3", "tas", "Flat1-It3B-Bridge.sam"), partOIteration3Paths.Path_Model_CandidateB);
            Assert.Equal(Path.Combine("C:\\out", "Iteration3", "reports", "Flat1-Iteration3-B0.json"), partOIteration3Paths.Path_Record);

            //Not an optimisation round: the iteration reader must not see a number in it, or a later
            //optimisation would number its rounds from here and overwrite this pairing's evidence.
            Assert.Equal(0, PartOSimulationContext.Iteration_ProjectName(partOIteration3Paths.ProjectName_CandidateB));
        }

        [Fact]
        public void The_review_derives_the_same_record_path_from_the_results_alone()
        {
            PartOIteration3Paths partOIteration3Paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd");

            Assert.Equal(partOIteration3Paths.Path_Record, PartOIteration3Paths.Path_Record_ForResults("C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.Parity));

            //The mode-independent record every pairing written before per-method records used is still where
            //a review looks for it.
            Assert.Equal(Path.Combine("C:\\out", "Flat1-Iteration3.json"), PartOIteration3Paths.Path_Record_ForResults("C:\\out\\Flat1.tsd"));
        }

        [Fact]
        public void The_snapshot_covers_every_candidate_B_path_and_never_reference_As_own()
        {
            PartOIteration3Paths partOIteration3Paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd");

            List<string> paths = [.. partOIteration3Paths.Paths_CandidateB];

            Assert.Contains(partOIteration3Paths.Path_TBD_ThermalSource, paths);
            Assert.Contains(partOIteration3Paths.Path_TSD_ThermalSource, paths);
            Assert.Contains(partOIteration3Paths.Path_TPD, paths);
            Assert.Contains(partOIteration3Paths.Path_TBD_Bridge, paths);
            Assert.Contains(partOIteration3Paths.Path_TSD_Bridge, paths);
            Assert.Contains(partOIteration3Paths.Path_Model_CandidateB, paths);
            Assert.Contains(partOIteration3Paths.Path_TM59Report_CandidateB, paths);
            Assert.Contains(partOIteration3Paths.Path_Record, paths);

            //Reference A is an INPUT. Nothing this run does writes it, so it is not a path an artifact
            //could be claimed at.
            Assert.DoesNotContain(partOIteration3Paths.Path_TSD_ReferenceA, paths);
            Assert.DoesNotContain(partOIteration3Paths.Path_TM59Report_ReferenceA, paths);
        }
    }
}
