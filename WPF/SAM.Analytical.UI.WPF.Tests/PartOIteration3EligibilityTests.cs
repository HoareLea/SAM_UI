// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>What the Iteration 3 action can do with the run in front of it.</b>
    /// <para>
    /// Producing a Candidate B runs TAS twice; reopening one reads two files that already exist. Which of
    /// the two is offered is a property of the run's state, and the two have different reasons for being
    /// unavailable - so they are answered separately and a person who can only review is not told why
    /// they cannot run.
    /// </para>
    /// <para>
    /// <b>A reopened run may review and may not run</b>, the same rule Iteration 2B follows and for the
    /// same reason: a file records what was run, not how this session prepared it.
    /// </para>
    /// </summary>
    public class PartOIteration3EligibilityTests : IDisposable
    {
        private readonly string directory = PartOIteration3Fixture.Directory_Temp();

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        private PartORun Run(PartOIteration partOIteration = PartOIteration.BasePassive, bool fullYear = true, bool captureSystems = true)
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones);

            PartORun result = new();

            Assert.True(result.Prepare(
                PartOIteration3Fixture.Model(adjacencyCluster),
                PartOIteration3Fixture.Scenarios(),
                new PartOPreparationContext(partOIteration, zones, null, null),
                captureSystems ? guids : null));

            string path_TSD = Path.Combine(directory, "Flat.tsd");

            Assert.True(result.ExpectResults(path_TSD));

            File.WriteAllText(path_TSD, "reference A results");

            PartOSimulationContext partOSimulationContext = fullYear
                ? PartOIteration3Fixture.SimulationContext(directory)
                : new PartOSimulationContext(directory, "Flat", null, SolarCalculationMethod.TAS, 1, 1);

            Assert.True(result.Complete(PartOIteration3Fixture.Model(new AdjacencyCluster(adjacencyCluster), "Flat"), path_TSD, partOSimulationContext, out string _));

            return result;
        }

        private static PartOIteration3Eligibility Eligibility(PartORun partORun)
        {
            return Query.PartOIteration3Eligibility(partORun, partORun.IsAssessable(out string refusal), refusal);
        }

        [Fact]
        public void A_completed_in_session_iteration_1a_full_year_run_with_captured_systems_can_run()
        {
            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(Run());

            Assert.True(partOIteration3Eligibility.CanRun);
            Assert.True(partOIteration3Eligibility.Available);
            Assert.False(partOIteration3Eligibility.Review);
            Assert.Null(partOIteration3Eligibility.Refusal_Run);
        }

        [Fact]
        public void An_unprepared_run_can_do_nothing_and_says_which()
        {
            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(new PartORun());

            Assert.False(partOIteration3Eligibility.Available);
            Assert.NotNull(partOIteration3Eligibility.Refusal);
        }

        [Fact]
        public void A_prepared_but_unsimulated_run_says_to_simulate_first()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones);

            PartORun partORun = new();

            partORun.Prepare(PartOIteration3Fixture.Model(adjacencyCluster), PartOIteration3Fixture.Scenarios(), new PartOPreparationContext(PartOIteration.BasePassive, zones, null, null), guids);

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            Assert.False(partOIteration3Eligibility.CanRun);
            Assert.Contains("has not been simulated", partOIteration3Eligibility.Refusal_Run);
        }

        [Fact]
        public void A_run_that_was_not_the_full_year_cannot_be_a_reference()
        {
            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(Run(PartOIteration.BasePassive, false));

            Assert.False(partOIteration3Eligibility.CanRun);
            Assert.Contains("not the full annual case", partOIteration3Eligibility.Refusal_Run);
        }

        /// <summary>
        /// The frozen architecture pairs Candidate B against Iteration 1a and nothing else. Extending the
        /// foundation to 1b or to 2 is a separate decision, not a side effect of this button.
        /// </summary>
        [Theory]
        [InlineData(PartOIteration.BaseNaturalVentilation)]
        [InlineData(PartOIteration.AcousticRestricted)]
        [InlineData(PartOIteration.Undefined)]
        public void Only_an_iteration_1a_run_can_produce_a_candidate_B(PartOIteration partOIteration)
        {
            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(Run(partOIteration));

            Assert.False(partOIteration3Eligibility.CanRun);
            Assert.Contains("Iteration 1a", partOIteration3Eligibility.Refusal_Run);
        }

        [Fact]
        public void A_run_whose_preparation_built_no_ventilation_system_cannot_produce_a_candidate_B()
        {
            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(Run(PartOIteration.BasePassive, true, false));

            Assert.False(partOIteration3Eligibility.CanRun);
            Assert.Contains("built no ventilation system", partOIteration3Eligibility.Refusal_Run);
        }

        /// <summary>
        /// A reopened run has no preparation and no simulation context by construction, so there is no
        /// thermal case to reproduce and no record of which systems the iteration built.
        /// </summary>
        [Fact]
        public void A_restored_run_may_review_and_may_not_run()
        {
            PartORun partORun = Run();

            string path_TSD = partORun.Path_TSD;

            //A reopened model that records the results it was produced from - the restore path's input.
            AnalyticalModel analyticalModel = PartOIteration3Fixture.Model(new AdjacencyCluster(), "Flat");

            analyticalModel.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new Core.SAMCollection<OverheatingScenario>(PartOIteration3Fixture.Scenarios()));
            analyticalModel.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(analyticalModel, path_TSD));

            PartORun partORun_Restored = new();

            Assert.True(partORun_Restored.Restore(analyticalModel, path_TSD, out string _));
            Assert.True(partORun_Restored.IsRestored);
            Assert.Empty(partORun_Restored.Guids_VentilationSystem_Prepared);

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun_Restored);

            Assert.False(partOIteration3Eligibility.CanRun);
            Assert.Contains("reopened from a saved model", partOIteration3Eligibility.Refusal_Run);
            Assert.Contains("can review an existing Iteration 3 pairing but cannot start a new one", partOIteration3Eligibility.Refusal_Run);
        }

        [Fact]
        public void With_no_record_beside_the_results_there_is_nothing_to_review()
        {
            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(Run());

            Assert.False(partOIteration3Eligibility.CanReview);
            Assert.Contains("No Approved Document O Iteration 3 pairing has been recorded", partOIteration3Eligibility.Refusal_Review);
        }

        /// <summary>A ledger in which every stage completed - what a completed pairing records.</summary>
        private static PartOIteration3Ledger Ledger_Complete()
        {
            PartOIteration3Ledger result = new();

            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                result.Complete(partOIteration3Stage, "done");
            }

            return result;
        }

        /// <summary>
        /// Once a COMPLETED pairing exists the action reviews it rather than rerunning TAS to reproduce a
        /// comparison that is already on disk.
        /// </summary>
        [Fact]
        public void A_recorded_pairing_makes_the_action_a_review()
        {
            PartORun partORun = Run();

            PartOIteration3Record partOIteration3Record = new();

            partOIteration3Record.Adopt(Ledger_Complete());

            File.WriteAllText(Path.Combine(directory, "Flat-Iteration3.json"), partOIteration3Record.ToString());

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            Assert.True(partOIteration3Eligibility.CanReview);
            Assert.True(partOIteration3Eligibility.Review);
            Assert.True(partOIteration3Eligibility.Available);
            Assert.True(partOIteration3Eligibility.PairingStatus(PartOIteration3BehaviourMode.Parity).IsReviewable);
        }

        /// <summary>
        /// A refused attempt is kept - its ledger is the diagnosis - but it never locks its method into Review:
        /// the engineer who fixed the reason must be able to run the method again without deleting a file.
        /// </summary>
        [Fact]
        public void A_refused_attempt_does_not_make_its_method_reviewable_and_leaves_run_available()
        {
            PartORun partORun = Run();

            PartOIteration3Ledger partOIteration3Ledger = new();
            partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "ready");
            partOIteration3Ledger.Refuse(PartOIteration3Stage.ReferenceA, "no", ["no"]);

            PartOIteration3Record partOIteration3Record = new() { BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance };
            partOIteration3Record.Adopt(partOIteration3Ledger);

            File.WriteAllText(PartOIteration3Paths.Path_Record_ForResults(partORun.Path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance), partOIteration3Record.ToString());

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            PartOIteration3PairingStatus partOIteration3PairingStatus = partOIteration3Eligibility.PairingStatus(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.True(partOIteration3PairingStatus.IsRefused);
            Assert.False(partOIteration3PairingStatus.IsReviewable);
            Assert.False(partOIteration3Eligibility.Review);
            Assert.True(partOIteration3Eligibility.CanRun);
            Assert.Contains("can be run again", partOIteration3Eligibility.Refusal_Review);
        }

        /// <summary>
        /// Each method keeps its own result: a completed route check and a completed manufacturer-guidance
        /// result against the same reference case are both reviewable, and neither hides the other.
        /// </summary>
        [Fact]
        public void Completed_results_of_two_methods_coexist_and_are_each_reviewable()
        {
            PartORun partORun = Run();

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in new[] { PartOIteration3BehaviourMode.Parity, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance })
            {
                PartOIteration3Record partOIteration3Record = new() { BehaviourMode = partOIteration3BehaviourMode };
                partOIteration3Record.Adopt(Ledger_Complete());

                File.WriteAllText(PartOIteration3Paths.Path_Record_ForResults(partORun.Path_TSD, partOIteration3BehaviourMode), partOIteration3Record.ToString());
            }

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            Assert.True(partOIteration3Eligibility.PairingStatus(PartOIteration3BehaviourMode.Parity).IsReviewable);
            Assert.True(partOIteration3Eligibility.PairingStatus(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance).IsReviewable);
            Assert.False(partOIteration3Eligibility.PairingStatus(PartOIteration3BehaviourMode.SelectedProduct).Exists);

            //And a completed result never stops a method being run.
            Assert.True(partOIteration3Eligibility.CanRun);
        }

        /// <summary>
        /// The mode-independent record written before per-method records existed is read as the method recorded
        /// INSIDE it - and only as that method.
        /// </summary>
        [Fact]
        public void A_legacy_record_is_read_as_the_method_it_records_and_no_other()
        {
            PartORun partORun = Run();

            PartOIteration3Record partOIteration3Record = new() { BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance };
            partOIteration3Record.Adopt(Ledger_Complete());

            File.WriteAllText(Path.Combine(directory, "Flat-Iteration3.json"), partOIteration3Record.ToString());

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            PartOIteration3PairingStatus partOIteration3PairingStatus = partOIteration3Eligibility.PairingStatus(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.True(partOIteration3PairingStatus.IsReviewable);
            Assert.True(partOIteration3PairingStatus.IsLegacy);
            Assert.Equal(Path.Combine(directory, "Flat-Iteration3.json"), partOIteration3PairingStatus.Path_Record);

            Assert.False(partOIteration3Eligibility.PairingStatus(PartOIteration3BehaviourMode.Parity).Exists);

            //A method's own record supersedes the legacy one once it exists.
            PartOIteration3Record partOIteration3Record_New = new() { BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance };
            partOIteration3Record_New.Adopt(new PartOIteration3Ledger());

            string path_New = PartOIteration3Paths.Path_Record_ForResults(partORun.Path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);
            File.WriteAllText(path_New, partOIteration3Record_New.ToString());

            PartOIteration3PairingStatus partOIteration3PairingStatus_New = Eligibility(partORun).PairingStatus(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.Equal(path_New, partOIteration3PairingStatus_New.Path_Record);
            Assert.False(partOIteration3PairingStatus_New.IsLegacy);
        }

        [Fact]
        public void A_record_of_another_schema_is_not_reviewable()
        {
            PartORun partORun = Run();

            PartOIteration3Record partOIteration3Record = new()
            {
                Schema = "PartOIteration3Record:v0",
            };

            partOIteration3Record.Adopt(new PartOIteration3Ledger());

            File.WriteAllText(Path.Combine(directory, "Flat-Iteration3.json"), partOIteration3Record.ToString());

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            Assert.False(partOIteration3Eligibility.CanReview);
            Assert.Contains("schema", partOIteration3Eligibility.Refusal_Review);
        }

        /// <summary>
        /// A pairing written before PR5A - every existing acceptance pairing - is still offered for review.
        /// Whether its content is still true is the review's own question, asked again there.
        /// </summary>
        [Fact]
        public void A_pre_PR5A_v1_record_is_still_reviewable()
        {
            PartORun partORun = Run();

            PartOIteration3Record partOIteration3Record = new()
            {
                Schema = PartOIteration3Record.LegacySchema_V1,
            };

            partOIteration3Record.Adopt(Ledger_Complete());

            File.WriteAllText(Path.Combine(directory, "Flat-Iteration3.json"), partOIteration3Record.ToString());

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            Assert.True(partOIteration3Eligibility.CanReview, partOIteration3Eligibility.Refusal_Review);
            Assert.True(partOIteration3Eligibility.Review);
        }

        [Fact]
        public void A_file_that_is_not_a_record_is_not_reviewable()
        {
            PartORun partORun = Run();

            File.WriteAllText(Path.Combine(directory, "Flat-Iteration3.json"), "not json at all {");

            PartOIteration3Eligibility partOIteration3Eligibility = Eligibility(partORun);

            Assert.False(partOIteration3Eligibility.CanReview);
            Assert.Contains("could not be read", partOIteration3Eligibility.Refusal_Review);
        }
    }
}
