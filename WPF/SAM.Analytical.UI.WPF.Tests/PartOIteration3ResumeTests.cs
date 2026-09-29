// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Retrying an Iteration 3 attempt that failed after its TAS work had already succeeded.</b>
    ///
    /// <para>
    /// The same end-to-end harness as the rest of <see cref="PartOIteration3RunTests"/>: the real
    /// <c>Modify.RunPartOIteration3</c> over a real <c>PartORun</c>, with the recording pipeline counting which pieces
    /// of work each attempt actually asked for. The rule under test is two-sided: TAS work this session proves still
    /// belongs to the run is never repeated, and TAS work it cannot prove that for is never reused.
    /// </para>
    /// </summary>
    public partial class PartOIteration3RunTests
    {
        private static readonly string[] Members_Tas =
        [
            nameof(IPartOIteration3Pipeline.Materialise),
            nameof(IPartOIteration3Pipeline.ThermalSource),
            nameof(IPartOIteration3Pipeline.Route),
            nameof(IPartOIteration3Pipeline.ResultantTemperatures),
        ];

        private static readonly string[] Roles_Tas =
        [
            PartOIteration3Roles.ThermalSource_TBD,
            PartOIteration3Roles.ThermalSource_TSD,
            PartOIteration3Roles.Systems_TPD,
            PartOIteration3Roles.Bridge_TBD,
            PartOIteration3Roles.Bridge_TSD,
        ];

        private static PartOIteration3Assessment Assessment_Refused(string refusal)
        {
            return new PartOIteration3Assessment(false, refusal, TM59ComplianceStatus.Undefined, null, null, null, null, null, null, null, 0);
        }

        /// <summary>
        /// A pipeline for a run whose Candidate B TM59 assessment fails once everything TAS does has succeeded, and
        /// the good Candidate B assessment to put back before the retry.
        /// </summary>
        private PartOIteration3PipelineFake Pipeline_FailingAtCandidateBTM59(out PartOIteration3Assessment partOIteration3Assessment_CandidateB)
        {
            PartOIteration3PipelineFake result = Pipeline_Complete(out List<Guid> _);

            result.Path_TSD_ReferenceA = path_TSD_ReferenceA;

            partOIteration3Assessment_CandidateB = result.Assessment_CandidateB;

            result.Assessment_CandidateB = Assessment_Refused("The Candidate B results could not be read (controlled failure).");

            return result;
        }

        /// <summary>A completed run whose Parity attempt got through its TAS work and then failed at Candidate B's TM59.</summary>
        private PartORun Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result partOIteration3Result_Failed)
        {
            PartORun result = Run();

            partOIteration3PipelineFake = Pipeline_FailingAtCandidateBTM59(out PartOIteration3Assessment partOIteration3Assessment_CandidateB);

            partOIteration3Result_Failed = Modify.RunPartOIteration3(result, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.CandidateBTM59, partOIteration3Result_Failed.Ledger.Stage_Refused);
            Assert.NotNull(result.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));

            partOIteration3PipelineFake.Called.Clear();
            partOIteration3PipelineFake.Assessment_CandidateB = partOIteration3Assessment_CandidateB;

            return result;
        }

        private static void AssertTasRan(PartOIteration3PipelineFake partOIteration3PipelineFake)
        {
            foreach (string member in Members_Tas)
            {
                Assert.Single(partOIteration3PipelineFake.Called, member);
            }
        }

        //-------------------------------------------------------------------------------------------------
        //1. A valid retry skips the TAS work
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// THE regression: TAS succeeded, TM59 then failed, and the retry in the same session must not ask for any of
        /// the TAS work again - the materialisation, the thermal source, the TAS Systems route or the resultant
        /// temperature. Only the assessments and the persistence run.
        /// </summary>
        [Fact]
        public void A_retry_after_candidate_B_TM59_failed_does_not_run_TAS_again()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_FailingAtCandidateBTM59(out PartOIteration3Assessment partOIteration3Assessment_CandidateB);

            PartOIteration3Result partOIteration3Result_Failed = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.CandidateBTM59, partOIteration3Result_Failed.Ledger.Stage_Refused);
            Assert.True(partOIteration3Result_Failed.Ledger.State(PartOIteration3Stage.ResultantTemperature).IsCompleted);

            AssertTasRan(partOIteration3PipelineFake);

            partOIteration3PipelineFake.Called.Clear();
            partOIteration3PipelineFake.Assessment_CandidateB = partOIteration3Assessment_CandidateB;

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete, string.Join(" | ", partOIteration3Result.Ledger.Reasons));

            partOIteration3PipelineFake.AssertNeverCalled(Members_Tas);

            Assert.Equal(
                [nameof(IPartOIteration3Pipeline.Assess), nameof(IPartOIteration3Pipeline.Assess), nameof(IPartOIteration3Pipeline.Persist)],
                partOIteration3PipelineFake.Called);

            Assert.True(File.Exists(partOIteration3Result.Path_Record));
        }

        /// <summary>The same, where the last stage - making the pairing reopenable - is the one that failed.</summary>
        [Fact]
        public void A_retry_after_persistence_failed_does_not_run_TAS_again()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);
            partOIteration3PipelineFake.Path_TSD_ReferenceA = path_TSD_ReferenceA;
            partOIteration3PipelineFake.Persisted = false;
            partOIteration3PipelineFake.Note_Persist = "The disk was full (controlled failure).";

            PartOIteration3Result partOIteration3Result_Failed = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.Persistence, partOIteration3Result_Failed.Ledger.Stage_Refused);
            Assert.False(partOIteration3Result_Failed.IsComplete);

            partOIteration3PipelineFake.Called.Clear();
            partOIteration3PipelineFake.Persisted = true;
            partOIteration3PipelineFake.Note_Persist = null;

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete, string.Join(" | ", partOIteration3Result.Ledger.Reasons));
            partOIteration3PipelineFake.AssertNeverCalled(Members_Tas);
        }

        /// <summary>
        /// An assessment that throws rather than refusing: the attempt never reaches its own exit, so nothing it
        /// computed after the TAS work is kept - and the TAS work still is.
        /// </summary>
        [Fact]
        public void A_retry_after_candidate_B_TM59_threw_does_not_run_TAS_again()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);
            partOIteration3PipelineFake.Path_TSD_ReferenceA = path_TSD_ReferenceA;
            partOIteration3PipelineFake.Exception_CandidateB = new IOException("TSD.exe stopped responding (controlled failure).");

            Assert.Throws<IOException>(() => Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake));

            partOIteration3PipelineFake.Called.Clear();
            partOIteration3PipelineFake.Exception_CandidateB = null;

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete, string.Join(" | ", partOIteration3Result.Ledger.Reasons));
            partOIteration3PipelineFake.AssertNeverCalled(Members_Tas);
        }

        /// <summary>
        /// A cancel after the TAS work - the one cancel point after it, before Candidate B is assessed - is a stop like
        /// any other: the TAS work is kept.
        /// </summary>
        [Fact]
        public void A_retry_after_a_cancel_before_candidate_B_TM59_does_not_run_TAS_again()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);
            partOIteration3PipelineFake.Path_TSD_ReferenceA = path_TSD_ReferenceA;

            using (System.Threading.CancellationTokenSource cancellationTokenSource = new())
            {
                PartOIteration3Result partOIteration3Result_Cancelled = Modify.RunPartOIteration3(
                    partORun,
                    partOIteration3PipelineFake,
                    cancellationTokenSource.Token,
                    PartOIteration3BehaviourMode.Parity,
                    stage =>
                    {
                        if (stage == PartOIteration3Stage.CandidateBTM59)
                        {
                            cancellationTokenSource.Cancel();
                        }
                    });

                Assert.Equal(PartOIteration3Stage.CandidateBTM59, partOIteration3Result_Cancelled.Ledger.Stage_Refused);
            }

            partOIteration3PipelineFake.Called.Clear();

            Assert.True(Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake).IsComplete);
            partOIteration3PipelineFake.AssertNeverCalled(Members_Tas);
        }

        /// <summary>
        /// The resumed attempt says, stage by stage, that its TAS work was reused - so neither the result window nor the
        /// saved record ever reads as though TAS ran again - and it names the attempt it reused.
        /// </summary>
        [Fact]
        public void A_resumed_attempt_records_its_TAS_stages_as_reused_from_the_earlier_attempt()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete);

            Assert.Contains("Resumed: the TAS work of the attempt of", partOIteration3Result.Ledger.State(PartOIteration3Stage.Input).Detail);

            foreach (PartOIteration3Stage partOIteration3Stage in new[] { PartOIteration3Stage.Materialisation, PartOIteration3Stage.ThermalSource, PartOIteration3Stage.SystemsConversion, PartOIteration3Stage.SystemsSimulation, PartOIteration3Stage.ZoneTemperature, PartOIteration3Stage.ResultantTemperature })
            {
                Assert.StartsWith("Reused from the attempt of", partOIteration3Result.Ledger.State(partOIteration3Stage).Detail);
            }

            //The stages this attempt actually ran say nothing of the kind.
            foreach (PartOIteration3Stage partOIteration3Stage in new[] { PartOIteration3Stage.ReferenceATM59, PartOIteration3Stage.CandidateBTM59, PartOIteration3Stage.Reconciliation, PartOIteration3Stage.Comparison, PartOIteration3Stage.Persistence })
            {
                Assert.DoesNotContain("Reused", partOIteration3Result.Ledger.State(partOIteration3Stage).Detail);
            }

            //And the TAS files are listed as reused, never as written by this attempt.
            Assert.All(partOIteration3Result.Ledger.State(PartOIteration3Stage.ResultantTemperature).Artifacts, x => Assert.Contains("reused from the attempt of", x));

            //Saved exactly so: the record a later session reopens carries the same words.
            PartOIteration3Record partOIteration3Record = Query.PartOIteration3PairingRecord(partOIteration3Result.Path_Record);

            Assert.True(partOIteration3Record.IsComplete);
            Assert.StartsWith("Reused from the attempt of", partOIteration3Record.Stages.Find(x => x.Stage == PartOIteration3Stage.ThermalSource).Detail);
        }

        //-------------------------------------------------------------------------------------------------
        //2. A changed model or run state rejects reuse
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// The prepared design changed in place - here a room's floor area, on the room object the prepared model
        /// shares. Nothing announced it, so the run is still live; the design fingerprint is what refuses the kept work,
        /// and TAS runs again.
        /// <para>
        /// (Replacing an object on <c>AnalyticalModel.AdjacencyCluster</c> would not do: that getter hands back a
        /// copy, so the prepared model would not have changed at all - and the kept work would rightly be reused.)
        /// </para>
        /// </summary>
        [Fact]
        public void A_changed_prepared_design_is_not_resumed_and_TAS_runs_again()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);

            string fingerprint = SimulationResultProvenance.Fingerprint(partORun.AnalyticalModel_Prepared);

            partORun.AnalyticalModel_Prepared.AdjacencyCluster.GetObjects<Space>().First().SetValue(SpaceParameter.Area, PartOIteration3Fixture.Area + 1.0);

            Assert.NotEqual(fingerprint, SimulationResultProvenance.Fingerprint(partORun.AnalyticalModel_Prepared));

            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity);

            Assert.False(partOIteration3ResumePlan.Reuse);
            Assert.Contains("the prepared design has changed", partOIteration3ResumePlan.Reason);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            AssertTasRan(partOIteration3PipelineFake);
            Assert.Contains(partOIteration3Result.Notes, x => x.Contains("were not reused") && x.Contains("the prepared design has changed"));
            Assert.DoesNotContain("Reused", partOIteration3Result.Ledger.State(PartOIteration3Stage.ThermalSource).Detail);
        }

        /// <summary>
        /// Every transition that drops or replaces the run - an outside model change, a reset, a new preparation, a
        /// rewritten reference results file - takes the kept TAS work with it.
        /// </summary>
        [Fact]
        public void Dropping_the_run_drops_the_kept_TAS_work()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake _, out PartOIteration3Result _);

            partORun.NotifyModified();

            Assert.False(partORun.CanAssess);
            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));

            partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake _, out PartOIteration3Result _);
            partORun.Reset();
            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));

            partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake _, out PartOIteration3Result _);
            Assert.True(partORun.Prepare(partORun.AnalyticalModel_Prepared, partORun.OverheatingScenarios, partORun.PreparationContext, partORun.Guids_VentilationSystem_Prepared));
            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));

            //Reference A's results rewritten: the run's own gate drops it at the next attempt, and nothing is reused.
            partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);
            File.WriteAllText(path_TSD_ReferenceA, "reference A results, rewritten by another simulation");
            File.SetLastWriteTimeUtc(path_TSD_ReferenceA, DateTime.UtcNow.AddMinutes(5));

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.Input, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));
            partOIteration3PipelineFake.AssertNeverCalled(Members_Tas);
        }

        //-------------------------------------------------------------------------------------------------
        //3. A changed method or TAS case rejects reuse
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// Kept work belongs to the method that produced it: another method finds none, and the method that produced
        /// it keeps it.
        /// </summary>
        [Fact]
        public void Another_method_never_resumes_from_a_different_methods_TAS_work()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake _, out PartOIteration3Result _);

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in new[] { PartOIteration3BehaviourMode.SelectedProduct, PartOIteration3BehaviourMode.SelectedProductCooling, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance })
            {
                Assert.Null(partORun.Iteration3Checkpoint(partOIteration3BehaviourMode));
                Assert.False(Query.PartOIteration3ResumePlan(partORun, partOIteration3BehaviourMode).Reuse);
            }

            Assert.True(Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity).Reuse);

            //And the identity itself names a method difference, should one ever be compared.
            PartOIteration3ResumeIdentity partOIteration3ResumeIdentity = Query.PartOIteration3ResumeIdentity(partORun, PartOIteration3BehaviourMode.Parity, null);
            PartOIteration3ResumeIdentity partOIteration3ResumeIdentity_Other = Query.PartOIteration3ResumeIdentity(partORun, PartOIteration3BehaviourMode.SelectedProductCooling, "sha");

            Assert.Empty(partOIteration3ResumeIdentity.Differences(Query.PartOIteration3ResumeIdentity(partORun, PartOIteration3BehaviourMode.Parity, null)));
            Assert.Contains(partOIteration3ResumeIdentity.Differences(partOIteration3ResumeIdentity_Other), x => x.StartsWith("the method is"));
        }

        /// <summary>The TAS case the thermal source ran as changed - one workflow option is enough - so TAS runs again.</summary>
        [Fact]
        public void A_changed_TAS_case_is_not_resumed_and_TAS_runs_again()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);

            partORun.SimulationContext.UnmetHours = !partORun.SimulationContext.UnmetHours;

            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity);

            Assert.False(partOIteration3ResumePlan.Reuse);
            Assert.Contains("the TAS case", partOIteration3ResumePlan.Reason);

            Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            AssertTasRan(partOIteration3PipelineFake);
        }

        //-------------------------------------------------------------------------------------------------
        //4. A missing or changed artifact never fakes completeness
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// Any one TAS file missing or rewritten: the kept work is not reused - never the remaining files with a fresh
        /// one beside them - the file is named, and every TAS stage runs again from the thermal source.
        /// </summary>
        [Theory]
        [InlineData("Flat-It3B.tsd", false)]
        [InlineData("Flat-It3B.tpd", false)]
        [InlineData("Flat-It3B-Bridge.tsd", false)]
        [InlineData("Flat-It3B-Bridge.tbd", true)]
        [InlineData("Flat-It3B.tpd", true)]
        public void A_missing_or_rewritten_TAS_file_is_not_resumed_and_every_TAS_stage_runs_again(string fileName, bool rewrite)
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);

            string path = Path.Combine(directory, fileName);

            if (rewrite)
            {
                File.WriteAllText(path, "rewritten by something else, and longer than it was");
            }
            else
            {
                File.Delete(path);
            }

            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity);

            Assert.False(partOIteration3ResumePlan.Reuse);
            Assert.Contains(path, partOIteration3ResumePlan.Reason);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            AssertTasRan(partOIteration3PipelineFake);
            Assert.True(partOIteration3Result.IsComplete);
        }

        /// <summary>
        /// A file that changes after the decision to reuse and before it is used: the attempt refuses at that stage,
        /// naming the file, runs no TAS - and discards the kept work, so the next attempt runs TAS instead of refusing
        /// the same way again.
        /// </summary>
        [Fact]
        public void A_TAS_file_that_changes_after_the_resume_decision_refuses_and_discards_the_kept_work()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);

            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity);

            Assert.True(partOIteration3ResumePlan.Reuse);

            string path_TPD = Path.Combine(directory, "Flat-It3B.tpd");

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(
                partORun,
                partOIteration3PipelineFake,
                default,
                PartOIteration3BehaviourMode.Parity,
                stage =>
                {
                    if (stage == PartOIteration3Stage.SystemsConversion)
                    {
                        File.WriteAllText(path_TPD, "rewritten while the attempt was running");
                    }
                },
                partOIteration3ResumePlan);

            Assert.Equal(PartOIteration3Stage.SystemsConversion, partOIteration3Result.Ledger.Stage_Refused);
            Assert.Contains(partOIteration3Result.Ledger.Reasons, x => x.Contains(path_TPD));
            partOIteration3PipelineFake.AssertNeverCalled(Members_Tas);
            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));

            partOIteration3PipelineFake.Called.Clear();

            Assert.True(Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake).IsComplete);
            AssertTasRan(partOIteration3PipelineFake);
        }

        //-------------------------------------------------------------------------------------------------
        //5. Missing provenance fails closed - a record on disk is never enough
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>The fresh-process case.</b> A later session - a new run over the same results - finds the saved record of
        /// an attempt that completed its TAS work and stopped, with every TAS file still present. It is not resumed: the
        /// record holds neither the model the no-IZAM workflow returned nor the TAS Systems bindings, so nothing proves
        /// the files belong to this run. TAS runs, and the attempt says why.
        /// </summary>
        [Fact]
        public void A_refused_record_from_another_session_is_explained_and_never_resumed()
        {
            PartORun partORun_Earlier = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result partOIteration3Result_Failed);

            Assert.True(File.Exists(partOIteration3Result_Failed.Path_Record));

            //The same results reopened by another session: a new run, restored from the persisted reference model - no
            //kept work, whatever is on disk.
            string path_Model_ReferenceA = Query.Path_PartORunModel(path_TSD_ReferenceA);
            Assert.True(Core.Convert.ToFile(partORun_Earlier.AnalyticalModel_Assessment, path_Model_ReferenceA, Core.SAMFileType.SAM));

            PartORun partORun = new();
            Assert.True(partORun.Restore(Core.Convert.ToSAM<AnalyticalModel>(path_Model_ReferenceA).Single(), path_Model_ReferenceA, out string refusal_Restore), refusal_Restore);

            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));

            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity);

            Assert.False(partOIteration3ResumePlan.Reuse);

            //Review-only without a saved preparation, so there is nothing to explain - and nothing is resumed either.
            Assert.False(partORun.CanResumeIteration3);
            Assert.Null(partOIteration3ResumePlan.Reason);

            //The same record - still on disk beside the same results names - seen by a run that holds no kept work, as
            //a new preparation and simulation of the project in a later session would: explained, by stage, and not
            //resumed, even though every TAS file it names is still there.
            PartORun partORun_Live = Run();

            Assert.True(File.Exists(partOIteration3Result_Failed.Path_Record));
            Assert.All(Roles_Tas, x => Assert.True(File.Exists(partOIteration3Result_Failed.Record.File(x).Path)));

            partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun_Live, PartOIteration3BehaviourMode.Parity);

            Assert.False(partOIteration3ResumePlan.Reuse);
            Assert.Contains("stopped at candidate b tm59", partOIteration3ResumePlan.Reason);
            Assert.Contains("can be proven only in the SAM session that produced them", partOIteration3ResumePlan.Reason);

            partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);
            partOIteration3PipelineFake.Path_TSD_ReferenceA = path_TSD_ReferenceA;

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun_Live, partOIteration3PipelineFake);

            AssertTasRan(partOIteration3PipelineFake);
            Assert.True(partOIteration3Result.IsComplete);
            Assert.Contains(partOIteration3Result.Notes, x => x.Contains("can be proven only in the SAM session that produced them"));
        }

        /// <summary>An identity with any value missing never matches - not even an identity with the same hole.</summary>
        [Fact]
        public void An_identity_with_missing_provenance_never_matches_anything()
        {
            PartOIteration3ResumeIdentity partOIteration3ResumeIdentity = new()
            {
                BehaviourMode = PartOIteration3BehaviourMode.Parity,
                Path_TSD_ReferenceA = "a.tsd",
                Length_TSD_ReferenceA = 1,
                Timestamp_TSD_ReferenceA = 1,
                Fingerprint_Model_ReferenceA = "model",
                Fingerprint_Scenarios_ReferenceA = "scenarios",
                Fingerprint_Scenario = "case",
                Fingerprint_PreparedModel = null,
            };

            Assert.False(partOIteration3ResumeIdentity.IsComplete(out string refusal));
            Assert.Contains("the prepared design", refusal);
            Assert.NotEmpty(partOIteration3ResumeIdentity.Differences(partOIteration3ResumeIdentity));

            //A product method without the catalogue it resolved against is incomplete in the same way.
            PartOIteration3ResumeIdentity partOIteration3ResumeIdentity_Product = new()
            {
                BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                Path_TSD_ReferenceA = "a.tsd",
                Length_TSD_ReferenceA = 1,
                Timestamp_TSD_ReferenceA = 1,
                Fingerprint_Model_ReferenceA = "model",
                Fingerprint_Scenarios_ReferenceA = "scenarios",
                Fingerprint_Scenario = "case",
                Fingerprint_PreparedModel = "prepared",
            };

            Assert.False(partOIteration3ResumeIdentity_Product.IsComplete(out refusal));
            Assert.Contains("catalogue", refusal);
        }

        //-------------------------------------------------------------------------------------------------
        //6. A downstream failure preserves the TAS work
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A TM59 failure deletes and rewrites none of the TAS evidence: every TAS file the failed attempt recorded is
        /// still exactly that file afterwards, and the resumed pairing records the very same files.
        /// </summary>
        [Fact]
        public void A_downstream_failure_leaves_every_TAS_file_exactly_as_recorded_and_the_resumed_pairing_records_the_same_files()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result partOIteration3Result_Failed);

            List<PartOIteration3FileRecord> files_Failed = [.. Roles_Tas.Select(x => partOIteration3Result_Failed.Record.File(x))];

            Assert.All(files_Failed, x => Assert.True(x.Current(out string refusal), refusal));

            //The failed attempt is recorded as failed, and is not reviewable - a partial run is never a completed one.
            Assert.False(partOIteration3Result_Failed.Record.IsComplete);
            Assert.True(Query.PartOIteration3PairingStatus(partORun.Path_TSD, PartOIteration3BehaviourMode.Parity).IsRefused);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete);

            foreach (PartOIteration3FileRecord partOIteration3FileRecord in files_Failed)
            {
                PartOIteration3FileRecord partOIteration3FileRecord_Resumed = partOIteration3Result.Record.File(partOIteration3FileRecord.Role);

                Assert.Equal(partOIteration3FileRecord.Path, partOIteration3FileRecord_Resumed.Path);
                Assert.Equal(partOIteration3FileRecord.Length, partOIteration3FileRecord_Resumed.Length);
                Assert.Equal(partOIteration3FileRecord.Ticks_Utc, partOIteration3FileRecord_Resumed.Ticks_Utc);
                Assert.True(partOIteration3FileRecord_Resumed.Current(out string refusal), refusal);
            }
        }

        //-------------------------------------------------------------------------------------------------
        //7. An ordinary run is unchanged
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A run with no failure calls every piece of work exactly once, as it always did, and keeps nothing once its
        /// pairing completes - so running the method again afterwards is a deliberate new run and runs TAS.
        /// </summary>
        [Fact]
        public void A_complete_run_keeps_nothing_and_running_again_runs_TAS()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);
            partOIteration3PipelineFake.Path_TSD_ReferenceA = path_TSD_ReferenceA;

            Assert.True(Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake).IsComplete);

            Assert.Equal(
                [
                    nameof(IPartOIteration3Pipeline.Assess),
                    nameof(IPartOIteration3Pipeline.Materialise),
                    nameof(IPartOIteration3Pipeline.ThermalSource),
                    nameof(IPartOIteration3Pipeline.Route),
                    nameof(IPartOIteration3Pipeline.ResultantTemperatures),
                    nameof(IPartOIteration3Pipeline.Assess),
                    nameof(IPartOIteration3Pipeline.Persist),
                ],
                partOIteration3PipelineFake.Called);

            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));
            Assert.False(Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity).Reuse);

            partOIteration3PipelineFake.Called.Clear();

            Assert.True(Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake).IsComplete);
            AssertTasRan(partOIteration3PipelineFake);
        }

        /// <summary>
        /// A failure BEFORE the TAS work completed keeps nothing: there is no partial reuse, so the retry runs every TAS
        /// stage, the thermal source included.
        /// </summary>
        [Fact]
        public void A_failure_inside_the_TAS_work_keeps_nothing_and_the_retry_runs_all_of_it()
        {
            PartORun partORun = Run();

            PartOIteration3PipelineFake partOIteration3PipelineFake = Pipeline_Complete(out List<Guid> _);
            partOIteration3PipelineFake.Path_TSD_ReferenceA = path_TSD_ReferenceA;

            Analytical.Tas.TPD.ResultantTemperatureResults resultantTemperatureResults = partOIteration3PipelineFake.ResultantTemperatureResults;
            partOIteration3PipelineFake.ResultantTemperatureResults = null;

            PartOIteration3Result partOIteration3Result_Failed = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.Equal(PartOIteration3Stage.ResultantTemperature, partOIteration3Result_Failed.Ledger.Stage_Refused);
            Assert.Null(partORun.Iteration3Checkpoint(PartOIteration3BehaviourMode.Parity));

            partOIteration3PipelineFake.Called.Clear();
            partOIteration3PipelineFake.ResultantTemperatureResults = resultantTemperatureResults;

            Assert.True(Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake).IsComplete);
            AssertTasRan(partOIteration3PipelineFake);
        }

        //-------------------------------------------------------------------------------------------------
        //The completed result, and what the window and the Hub say
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// The resumed pairing is the same authoritative result a normal run produces: the same comparison, the same
        /// files recorded, and it reopens through the ordinary review - which runs no TAS - to the same comparison.
        /// </summary>
        [Fact]
        public void A_resumed_pairing_has_the_normal_result_shape_and_reopens_through_the_ordinary_review()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);
            partOIteration3PipelineFake.Persist_ForReal = true;

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(partORun, partOIteration3PipelineFake);

            Assert.True(partOIteration3Result.IsComplete);
            Assert.All(partOIteration3Result.Ledger.Stages, x => Assert.Equal(PartOIteration3StageStatus.Completed, x.Status));

            //The fixture's comparison: six rooms, Candidate B one degree warmer.
            Assert.Equal(6, partOIteration3Result.Comparison.Statistics.Count_Rooms);
            Assert.Equal(1.0, partOIteration3Result.Comparison.Statistics.MeanBias, 12);
            Assert.Equal(6, partOIteration3Result.Record.Bindings.Count);

            foreach (string role in Roles_Tas.Append(PartOIteration3Roles.CandidateB_Model))
            {
                Assert.NotNull(partOIteration3Result.Record.File(role));
            }

            partOIteration3PipelineFake.Called.Clear();

            PartOIteration3Result partOIteration3Result_Review = Modify.ReviewPartOIteration3(partORun, partOIteration3PipelineFake, PartOIteration3BehaviourMode.Parity);

            Assert.True(partOIteration3Result_Review.IsComplete, string.Join(" | ", partOIteration3Result_Review.Ledger.Reasons));
            Assert.Equal(1.0, partOIteration3Result_Review.Comparison.Statistics.MeanBias, 12);
            Assert.Equal([nameof(IPartOIteration3Pipeline.Assess), nameof(IPartOIteration3Pipeline.Assess)], partOIteration3PipelineFake.Called);
        }

        /// <summary>
        /// The progress window of a resumed run lists and walks only what it does - preparing, reusing the TAS results,
        /// assessing, comparing - and never shows a TAS stage as running.
        /// </summary>
        [Fact]
        public void The_progress_window_of_a_resumed_run_shows_the_TAS_work_as_reused_and_never_as_running()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake partOIteration3PipelineFake, out PartOIteration3Result _);

            PartOIteration3ResumePlan partOIteration3ResumePlan = Query.PartOIteration3ResumePlan(partORun, PartOIteration3BehaviourMode.Parity);
            Assert.True(partOIteration3ResumePlan.Reuse);

            IReadOnlyList<string> phases = Modify.PartOIteration3Phases(PartOIteration3BehaviourMode.Parity, true);

            Assert.Equal([PartOProgressStages.PreparingSystemCase, PartOProgressStages.ReusingTasResults, PartOProgressStages.AssessingTm59, PartOProgressStages.ComparingAndSaving], phases);

            using PartOProgressHost partOProgressHost = new("Iteration 3", null, phases, true, false);

            List<string> running = [];
            List<string> details = [];

            Action<PartOIteration3Stage> announcer = Modify.PartOIteration3StageAnnouncer(partOProgressHost, PartOIteration3BehaviourMode.Parity, true);

            PartOIteration3Result partOIteration3Result = Modify.RunPartOIteration3(
                partORun,
                partOIteration3PipelineFake,
                partOProgressHost.Token,
                PartOIteration3BehaviourMode.Parity,
                stage =>
                {
                    announcer(stage);

                    for (int i = 0; i < partOProgressHost.State.Count; i++)
                    {
                        if (partOProgressHost.State.Status(i) == PartOProgressStageStatus.Running && (running.Count == 0 || running[^1] != partOProgressHost.State.Name(i)))
                        {
                            running.Add(partOProgressHost.State.Name(i));
                        }
                    }

                    if (partOProgressHost.State.Detail is string detail)
                    {
                        details.Add(detail);
                    }
                },
                partOIteration3ResumePlan);

            Assert.True(partOIteration3Result.IsComplete);

            partOProgressHost.State.Complete();

            Assert.Equal(phases, running);
            Assert.Contains(details, x => x.Contains("reused, not run again"));
            Assert.DoesNotContain(details, x => x.Contains("Materialising the ventilation systems") || x.Contains("Full-year resultant-temperature run in TAS") || x.Contains("Preparing the TAS Systems document"));

            for (int i = 0; i < partOProgressHost.State.Count; i++)
            {
                Assert.Equal(PartOProgressStageStatus.Completed, partOProgressHost.State.Status(i));
            }
        }

        /// <summary>The Hub's line after a stop says what running again will do, read off the run.</summary>
        [Fact]
        public void The_Hub_says_a_stopped_method_will_reuse_its_TAS_results_only_while_they_are_kept()
        {
            PartORun partORun = Run_FailedAfterTas(out PartOIteration3PipelineFake _, out PartOIteration3Result _);

            Assert.Contains("reuses them instead of running TAS", Modify.PartOIteration3RetryText(partORun, PartOIteration3BehaviourMode.Parity));
            Assert.Equal("It can be run again.", Modify.PartOIteration3RetryText(partORun, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));

            partORun.NotifyModified();

            Assert.Equal("It can be run again.", Modify.PartOIteration3RetryText(partORun, PartOIteration3BehaviourMode.Parity));
        }
    }
}
