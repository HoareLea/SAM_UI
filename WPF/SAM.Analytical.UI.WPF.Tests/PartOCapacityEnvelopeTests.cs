// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Analytical.UI.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The Iteration 2B diagnostic capacity envelope, as the UI orchestrates and reports it.</b>
    /// <para>
    /// The ordinary optimisation stops with rooms still failing. The envelope then answers a different
    /// question - what the <i>already-selected</i> unit could deliver at its own ceiling - and the whole
    /// danger of it is that its lifecycle is indistinguishable from a round's: it is prepared, simulated
    /// over the same full year, and assessed with production TM59, and it completes. Read as a round it
    /// would be the run's best result and its newest accepted design, and it is neither.
    /// </para>
    /// <para>
    /// So these tests are almost entirely about <b>separation</b>: the round count, the last valid design,
    /// the results file identity, the stored assessment and every row of the presentation. Plus the no-run
    /// decisions, each of which is settled before any TAS work and so can be asserted here directly.
    /// </para>
    /// <para>
    /// <b>What the envelope arithmetic comes to is not tested here.</b> That belongs to
    /// <c>SAM.Analytical.Modify.EvaluateDesignAirFlowCapacityEnvelope</c> and is pinned by
    /// <c>SAM.Tests.PartOCapacityEnvelopeTests</c>; restating it in the UI would be a second copy of an
    /// engineering rule the UI does not own.
    /// </para>
    /// </summary>
    public class PartOCapacityEnvelopeTests
    {
        private const string name_Bedroom = "Bedroom";

        private const string name_Kitchen = "Kitchen";

        private static readonly Guid guid_Kitchen = new("11111111-1111-1111-1111-111111111111");

        private static readonly Guid guid_Bedroom = new("22222222-2222-2222-2222-222222222222");

        private const string name_Studio = "Studio 1_0";

        private const string name_Bathroom_Studio = "Bathroom_2";

        /// <summary>The shipped Nuaire hybrid unit's model, 150/150 l/s.</summary>
        private const string model_MRXBOX = "MRXBOXAB-ECO5-AECV";

        /// <summary>The shipped Nuaire XBOXER commercial unit's model, 190/190 l/s.</summary>
        private const string model_XBC15 = "XBC15";

        private static readonly Guid guid_Studio = new("33333333-3333-3333-3333-333333333333");

        private static readonly Guid guid_Bathroom_Studio = new("55555555-5555-5555-5555-555555555555");

        // ---- 11. The ordinary accepted design is not replaced ---------------------------------------------

        /// <summary>
        /// <b>11.</b> A <b>completed</b> capacity envelope step does not become the run's last valid design.
        /// <para>
        /// This is the single most important separation here. An envelope is prepared, simulated and
        /// assessed exactly as a round is, and it sets <c>IsCompleted</c> - so a "last step that completed"
        /// which did not ask <i>what kind</i> of step it was would answer with the diagnostic, and the
        /// command would then replace the user's model with a design the optimiser's own all-or-nothing
        /// policy refuses.
        /// </para>
        /// </summary>
        [Fact]
        public void ACompletedCapacityEnvelope_IsNotTheRunsLastValidDesign()
        {
            PartOOptimisationRun partOOptimisationRun = History(out AnalyticalModel analyticalModel_LastValid);

            PartOOptimisationStep partOOptimisationStep = Envelope(partOOptimisationRun);

            Assert.True(partOOptimisationStep.IsCompleted);

            //Still the ordinary round, by identity - not merely by iteration number.
            Assert.Same(partOOptimisationRun.Steps[1], partOOptimisationRun.Step_LastValid);
            Assert.Equal(1, partOOptimisationRun.Step_LastValid.Iteration);
            Assert.True(partOOptimisationRun.Step_LastValid.IsOptimisationRound);

            Assert.Same(analyticalModel_LastValid, partOOptimisationRun.AnalyticalModel_LastValid);
            Assert.NotSame(partOOptimisationRun.AnalyticalModel_LastValid, partOOptimisationRun.AnalyticalModel_CapacityEnvelope);
        }

        /// <summary>
        /// <b>11, and the round count.</b> The envelope is not a round and is not counted as one. Counting
        /// it would have the run report one more successful step at the configured airflow than it took -
        /// and the envelope's step is precisely not that.
        /// </summary>
        [Fact]
        public void ACapacityEnvelope_IsNotCountedAsAnOptimisationRound()
        {
            PartOOptimisationRun partOOptimisationRun = History(out AnalyticalModel _);

            Assert.Equal(1, partOOptimisationRun.Rounds);

            Envelope(partOOptimisationRun);

            //Three steps, still one round.
            Assert.Equal(3, partOOptimisationRun.Steps.Count);
            Assert.Equal(1, partOOptimisationRun.Rounds);

            Assert.Contains("1 optimisation round(s)", partOOptimisationRun.Description);
        }

        /// <summary>
        /// <b>11, in the run's own words.</b> The envelope is described separately and labelled a
        /// diagnostic, and the optimisation's own stop reason and answer are unchanged by its presence.
        /// </summary>
        [Fact]
        public void TheDescription_KeepsTheEnvelopeApartFromTheOptimisationsAnswer()
        {
            PartOOptimisationRun partOOptimisationRun = History(out AnalyticalModel _);

            partOOptimisationRun.StopReason = PartOOptimisationStopReason.CapacityReached;

            string description_Before = partOOptimisationRun.Description;

            Envelope(partOOptimisationRun);

            //The optimisation's own account is still there, word for word, with the envelope appended as its
            //own labelled clause rather than folded into it.
            Assert.Contains("cannot carry another full", partOOptimisationRun.Description);
            Assert.Contains("CAPACITY ENVELOPE:", partOOptimisationRun.Description);
            Assert.DoesNotContain("CAPACITY ENVELOPE:", description_Before);

            Assert.Equal(PartOOptimisationStopReason.CapacityReached, partOOptimisationRun.StopReason);
            Assert.False(partOOptimisationRun.IsPassed);
        }

        // ---- 15. The envelope's production TM59 is stored as the envelope's ------------------------------

        /// <summary>
        /// <b>15.</b> The envelope's production TM59 result is kept on the envelope's own step, beside its
        /// own results file - never merged into the last valid design's. The two designs are different, so
        /// their verdicts are different facts.
        /// </summary>
        [Fact]
        public void TheEnvelopesProductionTM59Result_IsStoredSeparately()
        {
            PartOOptimisationRun partOOptimisationRun = History(out AnalyticalModel _);

            PartOOptimisationStep partOOptimisationStep_LastValid = partOOptimisationRun.Step_LastValid;

            PartOOptimisationStep partOOptimisationStep = Envelope(partOOptimisationRun);

            Assert.Same(partOOptimisationStep, partOOptimisationRun.Step_CapacityEnvelope);
            Assert.True(partOOptimisationRun.HasCapacityEnvelope);

            //The envelope passed where the last accepted design failed - which is the whole point of
            //calculating it, and would be unreadable if the two verdicts shared a slot.
            Assert.Equal(TM59ComplianceStatus.Fail, partOOptimisationStep_LastValid.OccupiedSpaceComplianceStatus);
            Assert.Equal(TM59ComplianceStatus.Pass, partOOptimisationStep.OccupiedSpaceComplianceStatus);

            Assert.NotEmpty(partOOptimisationStep.TM59Results);
            Assert.NotEqual(partOOptimisationStep_LastValid.Path_TSD, partOOptimisationStep.Path_TSD);

            Assert.Equal(partOOptimisationStep.Path_TSD, partOOptimisationRun.Path_TSD_CapacityEnvelope);

            //And the optimisation's answer is still a failure. An envelope that passes does not make the
            //run a pass: nobody accepted that design.
            Assert.False(partOOptimisationRun.IsPassed);
        }

        /// <summary>
        /// The envelope's results file is its own <c>-OptMax</c> identity, so it overwrites no round's
        /// evidence - and, read back, that name is <b>not</b> an iteration number, so a later optimisation
        /// could not renumber from it and land on a round's files.
        /// </summary>
        [Fact]
        public void TheEnvelope_HasItsOwnOptMaxIdentityThatIsNotAnIterationNumber()
        {
            PartOSimulationContext partOSimulationContext = new(Path.GetTempPath(), "Fixture", null, SolarCalculationMethod.SAM, 1, 365);

            Assert.Equal("Fixture-OptMax", partOSimulationContext.ProjectName_CapacityEnvelope());

            //Distinct from every round's name, whatever the round.
            for (int i = 0; i <= 20; i++)
            {
                Assert.NotEqual(partOSimulationContext.ProjectName_Iteration(i), partOSimulationContext.ProjectName_CapacityEnvelope());
            }

            //And it reads back as no iteration at all, rather than as some number a round also uses.
            Assert.Equal(0, PartOSimulationContext.Iteration_ProjectName(partOSimulationContext.ProjectName_CapacityEnvelope()));
        }

        /// <summary>
        /// <b>A second optimisation's envelope does not overwrite the first's evidence.</b>
        /// <para>
        /// The envelope used to be <c>&lt;project&gt;-OptMax</c> unconditionally, which made it the one
        /// piece of an optimisation's output that running the optimisation again destroyed. The rounds never
        /// had this problem: they are numbered from the iteration baseline the run started from, so a second
        /// run continues at <c>-Opt06</c> instead of overwriting <c>-Opt01</c>. The envelope simply ignored
        /// that baseline.
        /// </para>
        /// <para>
        /// Optimise, then optimise again from the result, and the two envelopes are now distinct - while a
        /// first optimisation keeps the spelling every already-saved run uses.
        /// </para>
        /// </summary>
        [Fact]
        public void TheEnvelopeOfARepeatedOptimisation_DoesNotOverwriteTheFirstOnes()
        {
            PartOSimulationContext partOSimulationContext = new(Path.GetTempPath(), "Flat1", null, SolarCalculationMethod.SAM, 1, 365);

            //First optimisation: started from the Iteration 2 design, whose results file carries no -Opt
            //suffix, so the baseline reads as 0.
            string projectName_Envelope_1 = partOSimulationContext.ProjectName_CapacityEnvelope(PartOSimulationContext.Iteration_ProjectName("Flat1"));

            //Second optimisation: started from the design the first left behind, five rounds in.
            string projectName_Envelope_2 = partOSimulationContext.ProjectName_CapacityEnvelope(PartOSimulationContext.Iteration_ProjectName("Flat1-Opt05"));

            Assert.NotEqual(projectName_Envelope_1, projectName_Envelope_2);

            //Backward compatible: the ordinary first optimisation still writes and reopens the name every
            //historic run was saved under.
            Assert.Equal("Flat1-OptMax", projectName_Envelope_1);

            //And the second names the lineage it was measured from.
            Assert.Equal("Flat1-Opt05-Max", projectName_Envelope_2);
        }

        /// <summary>
        /// The qualified envelope name still is not a round, and still reads back as no iteration - the two
        /// properties the unqualified <c>-OptMax</c> had and had to keep. Reading back as 0 is what stops an
        /// optimisation started from an envelope design renumbering from <c>-Opt01</c> and landing on a
        /// round's files.
        /// </summary>
        [Fact]
        public void TheQualifiedEnvelopeName_IsStillNotARoundAndStillReadsBackAsNoIteration()
        {
            PartOSimulationContext partOSimulationContext = new(Path.GetTempPath(), "Flat1", null, SolarCalculationMethod.SAM, 1, 365);

            for (int iteration_Baseline = 0; iteration_Baseline <= 20; iteration_Baseline++)
            {
                string projectName_Envelope = partOSimulationContext.ProjectName_CapacityEnvelope(iteration_Baseline);

                for (int i = 0; i <= 40; i++)
                {
                    Assert.NotEqual(partOSimulationContext.ProjectName_Iteration(i), projectName_Envelope);
                }

                Assert.Equal(0, PartOSimulationContext.Iteration_ProjectName(projectName_Envelope));
            }
        }

        /// <summary>
        /// Two envelopes from different baselines are distinct from each other and from every round of
        /// either session - the actual no-collision claim, over the whole space of baselines rather than
        /// one example.
        /// </summary>
        [Fact]
        public void EveryBaselinesEnvelopeName_IsUnique()
        {
            PartOSimulationContext partOSimulationContext = new(Path.GetTempPath(), "Flat1", null, SolarCalculationMethod.SAM, 1, 365);

            HashSet<string> projectNames = [];

            for (int iteration_Baseline = 0; iteration_Baseline <= 30; iteration_Baseline++)
            {
                Assert.True(projectNames.Add(partOSimulationContext.ProjectName_CapacityEnvelope(iteration_Baseline)),
                    string.Format("Two baselines produced the same envelope name at iteration {0}.", iteration_Baseline));
            }
        }

        /// <summary>
        /// <b>Every envelope artefact shares the one case identity.</b>
        /// <para>
        /// The TBD and TSD are named from the step's project name by <c>RunPartOSimulation</c>, and the run
        /// model and the TM59 report are then derived from the TSD by <c>Query.Path_PartORunModel</c> and
        /// <c>Query.Path_TM59Report</c> - the single naming authority each. So qualifying the project name
        /// moves the whole set together, and this asserts it does rather than leaving one file behind under
        /// the old name where it would be read as belonging to this run.
        /// </para>
        /// </summary>
        [Fact]
        public void EveryEnvelopeArtefact_SharesTheSameUniqueCaseIdentity()
        {
            string directory = Path.GetTempPath();

            PartOSimulationContext partOSimulationContext = new(directory, "Flat1", null, SolarCalculationMethod.SAM, 1, 365);

            string projectName = partOSimulationContext.ProjectName_CapacityEnvelope(PartOSimulationContext.Iteration_ProjectName("Flat1-Opt05"));

            Assert.Equal("Flat1-Opt05-Max", projectName);

            //The pair RunPartOSimulation writes, from the project name.
            string path_TBD = Path.Combine(directory, projectName + ".tbd");
            string path_TSD = Path.ChangeExtension(path_TBD, "tsd");

            //And the pair every reader and writer of a run's own evidence derives from the results file.
            Assert.Equal(Path.Combine(directory, "Flat1-Opt05-Max.sam"), Query.Path_PartORunModel(path_TSD));
            Assert.Equal(Path.Combine(directory, "Flat1-Opt05-Max-TM59.txt"), Query.Path_TM59Report(path_TSD));

            //None of them is the previous session's, which is the collision this closes.
            string projectName_Previous = partOSimulationContext.ProjectName_CapacityEnvelope();
            string path_TSD_Previous = Path.Combine(directory, projectName_Previous + ".tsd");

            Assert.NotEqual(path_TSD_Previous, path_TSD);
            Assert.NotEqual(Query.Path_PartORunModel(path_TSD_Previous), Query.Path_PartORunModel(path_TSD));
            Assert.NotEqual(Query.Path_TM59Report(path_TSD_Previous), Query.Path_TM59Report(path_TSD));
        }

        // ---- Presentation: the three stages are told apart ------------------------------------------------

        /// <summary>
        /// Baseline, optimisation round and capacity envelope are three different things, and every row of
        /// both histories says which it is. The envelope's run label is <c>MAX</c> rather than a number,
        /// because a number would place it in the rounds' sequence, where the last one is the answer.
        /// </summary>
        [Fact]
        public void TheHistory_DistinguishesBaselineFromOptimisationFromCapacityEnvelope()
        {
            PartOOptimisationRun partOOptimisationRun = History(out AnalyticalModel _);

            Envelope(partOOptimisationRun);

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun);

            Assert.Contains(rows, x => x.Stage == "BASELINE" && x.Run == "0");
            Assert.Contains(rows, x => x.Stage == "OPTIMISATION" && x.Run == "1");
            Assert.Contains(rows, x => x.Stage == "CAPACITY ENVELOPE" && x.Run == "MAX");

            //An envelope's deliberate rows are SCALED, and an ordinary round's are TARGETED. Both were
            //chosen, but by different authorities answering different questions - see
            //TheEnvelopesRows_AreScaledRatherThanTargeted.
            Assert.Contains(rows, x => x.Stage == "CAPACITY ENVELOPE" && x.Type == "SCALED" && x.Space == name_Kitchen);
            Assert.Contains(rows, x => x.Stage == "CAPACITY ENVELOPE" && x.Type == "DERIVED" && x.Space == name_Bedroom);
            Assert.Contains(rows, x => x.Stage == "OPTIMISATION" && x.Type == "TARGETED" && x.Space == name_Kitchen);

            List<PartOOptimisationUnitRow> rows_Unit = PartOOptimisationUnitRow.Rows(partOOptimisationRun);

            Assert.Contains(rows_Unit, x => x.Stage == "CAPACITY ENVELOPE" && x.Run == "MAX");

            //Never reselected, on the envelope's row as much as on a round's: the envelope's whole subject
            //is what the CURRENT product can deliver.
            Assert.All(rows_Unit, x => Assert.DoesNotContain("Reselect", x.Equipment));
            Assert.All(rows_Unit, x => Assert.Contains("MVHR-150", x.Product));
        }

        /// <summary>
        /// <b>An envelope's deliberate rows read SCALED, and an ordinary round's read TARGETED.</b>
        /// <para>
        /// Both were chosen, but by different authorities answering different questions. An optimisation
        /// round asked one failing room for one +5 l/s step; the envelope grew the <i>complete last valid
        /// design vector</i> proportionally towards the selected unit's ceiling, so a room moved because its
        /// dwelling grew and not because anybody requested that figure of it. Printing the second as
        /// TARGETED would say the optimisation had asked for figures it never did - the same misreading the
        /// targeted/derived split exists to prevent.
        /// </para>
        /// </summary>
        [Fact]
        public void TheEnvelopesRows_AreScaledRatherThanTargeted()
        {
            PartOOptimisationRun partOOptimisationRun = History(out AnalyticalModel _);

            Envelope(partOOptimisationRun);

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun);

            //Not one MAX row claims to be an optimisation target, and not one OPTIMISATION row claims to be
            //a proportional growth.
            Assert.DoesNotContain(rows, x => x.Run == "MAX" && x.Type == "TARGETED");
            Assert.DoesNotContain(rows, x => x.Run != "MAX" && x.Type == "SCALED");

            //And a SCALED row still carries both the request and what was achieved, so the two can be
            //compared exactly as they are on a round.
            PartOOptimisationAirFlowRow row = rows.Find(x => x.Run == "MAX" && x.Type == "SCALED");

            Assert.NotNull(row);
            Assert.Equal(27, row.DesignBefore_Lps, 6);
            Assert.Equal(63, row.Requested_Lps);
            Assert.Equal(63, row.Achieved_Lps, 6);
            Assert.Equal(13, row.Requirement_Lps, 6);
        }

        // ---- The MAX table is complete, and reconciles ----------------------------------------------------

        /// <summary>
        /// <b>The reporting defect this change fixes.</b>
        /// <para>
        /// For the real Flat 1 the MAX table used to show the studio's <b>supply</b> and the bathroom's
        /// <b>extract</b>, and nothing else - so a reader saw <c>Studio supply 150</c> and
        /// <c>Bathroom extract 128</c> against a unit duty of 150/150, and could not tell where the
        /// remaining 22 l/s of extract came from. The studio's own extract contribution was nowhere in the
        /// table, and had to be dug out of the narrative notes.
        /// </para>
        /// <para>
        /// Now the proportional growth targets every space and direction the unit serves, so every one of
        /// them has its own row:
        /// </para>
        /// <code>
        /// Studio 1_0  Supply    40 -> 150
        /// Studio 1_0  Extract   22 -> 82.5
        /// Bathroom_2  Extract   18 -> 67.5
        /// MVHR-01               150/150
        /// </code>
        /// <para>
        /// Both assertions matter: that the studio appears <b>twice</b>, once per direction, and that summing
        /// the visible rows of one direction reproduces the unit's duty exactly.
        /// </para>
        /// </summary>
        [Fact]
        public void TheMaxTable_ShowsEveryContributionNeededToReconcileTheUnitDuty()
        {
            PartOOptimisationRun partOOptimisationRun = FlatOneHistory();

            PartOOptimisationStep partOOptimisationStep = FlatOneEnvelope(partOOptimisationRun);

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun).FindAll(x => x.Run == "MAX");

            //The studio is there on BOTH sides - which is the row the old table left out.
            PartOOptimisationAirFlowRow row_Studio_Supply = rows.Find(x => x.Space == name_Studio && x.Direction == "Supply");
            PartOOptimisationAirFlowRow row_Studio_Extract = rows.Find(x => x.Space == name_Studio && x.Direction == "Extract");
            PartOOptimisationAirFlowRow row_Bathroom_Extract = rows.Find(x => x.Space == name_Bathroom_Studio && x.Direction == "Extract");

            Assert.NotNull(row_Studio_Supply);
            Assert.NotNull(row_Studio_Extract);
            Assert.NotNull(row_Bathroom_Extract);

            Assert.Equal(40, row_Studio_Supply.DesignBefore_Lps, 6);
            Assert.Equal(150, row_Studio_Supply.Achieved_Lps, 6);

            Assert.Equal(22, row_Studio_Extract.DesignBefore_Lps, 6);
            Assert.Equal(82.5, row_Studio_Extract.Achieved_Lps, 6);

            Assert.Equal(18, row_Bathroom_Extract.DesignBefore_Lps, 6);
            Assert.Equal(67.5, row_Bathroom_Extract.Achieved_Lps, 6);

            //Summing the VISIBLE rows of one direction reproduces the unit's duty on that side, with no
            //hidden terminal contribution and no room counted twice.
            PartOOptimisationUnitState partOOptimisationUnitState = Assert.Single(partOOptimisationStep.UnitStates);

            Assert.Equal(partOOptimisationUnitState.SupplyDuty_Lps, Sum(rows, "Supply"), 6);
            Assert.Equal(partOOptimisationUnitState.ExtractDuty_Lps, Sum(rows, "Extract"), 6);

            //One row per space and direction, aggregated at the room grain the rest of the history uses -
            //not one per terminal object.
            Assert.Equal(3, rows.Count);
            Assert.Equal(3, new HashSet<string>(rows.ConvertAll(x => string.Format("{0}|{1}", x.Space, x.Direction))).Count);

            //And the unit row a reader reconciles against says 150/150, on the rating.
            PartOOptimisationUnitRow partOOptimisationUnitRow = Assert.Single(PartOOptimisationUnitRow.Rows(partOOptimisationRun).FindAll(x => x.Run == "MAX"));

            Assert.Equal("150/150", partOOptimisationUnitRow.Duty);
            Assert.Equal("150/150", partOOptimisationUnitRow.Maximum);
        }

        /// <summary>
        /// The Approved Document F column of the MAX table is the <b>unchanged</b> requirement, sitting
        /// beside a design several times larger - which is the whole point of printing it. The studio still
        /// requires 30 l/s of supply against a diagnostic design of 150, and the bathroom 8 l/s against 67.5.
        /// </summary>
        [Fact]
        public void TheMaxTable_ShowsTheUnchangedApprovedDocumentFRequirementBesideTheGrownDesign()
        {
            PartOOptimisationRun partOOptimisationRun = FlatOneHistory();

            FlatOneEnvelope(partOOptimisationRun);

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun).FindAll(x => x.Run == "MAX");

            Assert.Equal(30, rows.Find(x => x.Space == name_Studio && x.Direction == "Supply").Requirement_Lps, 6);
            Assert.Equal(13, rows.Find(x => x.Space == name_Studio && x.Direction == "Extract").Requirement_Lps, 6);
            Assert.Equal(8, rows.Find(x => x.Space == name_Bathroom_Studio && x.Direction == "Extract").Requirement_Lps, 6);

            //Every row of the diagnostic is above its own floor, and every one of them says so.
            Assert.All(rows, x => Assert.True(x.Achieved_Lps >= x.Requirement_Lps));
        }

        /// <summary>
        /// A room whose first ever movement is in the capacity envelope must not contribute a synthesised
        /// <c>BASELINE</c> row.
        /// <para>
        /// The baseline block works by reading the <c>Before_Lps</c> of each room's first adjustment, which
        /// is a recorded fact for an ordinary round - the round read it off the design it was given. The
        /// envelope's <c>Before_Lps</c> is the <b>last accepted</b> design's airflow, several rounds along,
        /// so treating it the same way would print a run-0 row stating a figure the baseline never carried.
        /// </para>
        /// </summary>
        [Fact]
        public void ARoomThatOnlyTheEnvelopeMoves_ContributesNoSynthesisedBaselineRow()
        {
            PartOOptimisationRun partOOptimisationRun = History(out AnalyticalModel _);

            PartOOptimisationStep partOOptimisationStep = Envelope(partOOptimisationRun);

            //A room nothing before the envelope ever touched, whose "before" is the last accepted design's
            //63 l/s and certainly not the baseline's.
            partOOptimisationStep.TargetedAdjustments.Add(new DesignAirFlowAdjustment(new Guid("44444444-4444-4444-4444-444444444444"), "Ensuite", FlowClassification.Extract, 63, 70, 8, false));

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun);

            Assert.Contains(rows, x => x.Space == "Ensuite" && x.Stage == "CAPACITY ENVELOPE");
            Assert.DoesNotContain(rows, x => x.Space == "Ensuite" && x.Stage == "BASELINE");
        }

        // ---- The no-run cases, each stated -----------------------------------------------------------------

        /// <summary>
        /// Not asked for means not calculated - and said so, rather than left blank. An optional diagnostic
        /// that silently produces nothing leaves a reader unable to tell it was considered at all.
        /// </summary>
        [Fact]
        public void AnEnvelopeThatWasNotAskedFor_IsNotCalculatedAndSaysSo()
        {
            PartOOptimisationRun partOOptimisationRun = Failing(PartOOptimisationStopReason.CapacityReached, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings { CapacityEnvelope = false }, partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.Null(partOOptimisationRun.CapacityEnvelope);
            Assert.False(partOOptimisationRun.HasCapacityEnvelope);

            Assert.Contains("was not asked for", partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// A run that <b>passed</b> has nothing to diagnose, so no envelope is calculated and no TAS time is
        /// spent discovering that.
        /// </summary>
        [Fact]
        public void ARunThatPassed_HasNoEnvelopeToCalculate()
        {
            PartOOptimisationRun partOOptimisationRun = Failing(PartOOptimisationStopReason.Passed, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.Contains("nothing for a capacity envelope to diagnose", partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// The envelope answers a stop on the selected unit's capacity or on the iteration guard. Every
        /// other stop is a run that did not finish rather than a design limited by its equipment, and
        /// enveloping from one would diagnose a design whose own optimisation never established what it
        /// could do.
        /// </summary>
        [Theory]
        [InlineData(PartOOptimisationStopReason.CapacityReached, true)]
        [InlineData(PartOOptimisationStopReason.IterationLimitReached, true)]
        [InlineData(PartOOptimisationStopReason.NoEligibleTargets, false)]
        [InlineData(PartOOptimisationStopReason.RebalanceRefused, false)]
        [InlineData(PartOOptimisationStopReason.PreparationFailed, false)]
        [InlineData(PartOOptimisationStopReason.SimulationFailed, false)]
        [InlineData(PartOOptimisationStopReason.AssessmentFailed, false)]
        [InlineData(PartOOptimisationStopReason.Cancelled, false)]
        public void OnlyACapacityOrIterationLimitStop_IsEnvelopped(PartOOptimisationStopReason partOOptimisationStopReason, bool considered)
        {
            //Deliberately a design already sitting on its unit's rating, so no stop reason can reach a
            //simulation and the only thing that varies is whether the stop reason was considered at all.
            PartOOptimisationRun partOOptimisationRun = Failing(partOOptimisationStopReason, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext, 30);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.NotNull(partOOptimisationRun.CapacityEnvelopeDescription);

            Assert.Equal(considered, !partOOptimisationRun.CapacityEnvelopeDescription.Contains("a run that did not finish"));

            //A stop the envelope does not answer is refused BEFORE the equipment is even looked at, so no
            //envelope was calculated at all - not merely one that found nothing.
            Assert.Equal(considered, partOOptimisationRun.CapacityEnvelope is not null);
        }

        /// <summary>
        /// "Not Pass" is not "Fail". An envelope calculated from an assessment that reached no verdict would
        /// scale a design towards its equipment's ceiling on the strength of results that said nothing about
        /// any of its rooms - the same rule the optimisation itself applies to its own stopping.
        /// </summary>
        [Fact]
        public void ALastValidDesignWithNoFailingVerdict_IsNotEnvelopped()
        {
            PartOOptimisationRun partOOptimisationRun = Failing(PartOOptimisationStopReason.IterationLimitReached, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            partOOptimisationRun.Steps[0].OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Undefined;

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.Contains("which is not a failure", partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// <b>13.</b> Nothing eligible left to target means no envelope <b>and no simulation</b> - a
        /// diagnostic with nothing to say must not cost a full-year TAS run to say it. The reason each
        /// failing room could not be targeted goes on the design it was learned about.
        /// </summary>
        [Fact]
        public void NoEligibleTarget_ProducesNoEnvelopeAndNoSimulation()
        {
            PartOOptimisationRun partOOptimisationRun = Failing(PartOOptimisationStopReason.CapacityReached, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            //Every failing room is outside the Part O dwelling scope, so the +5 policy can target none of
            //them - and the envelope scales exactly what that policy would have asked for.
            partOOptimisationRun.Steps[0].TM59Results.Clear();
            partOOptimisationRun.Steps[0].TM59Results.Add(new PartOTM59SpaceResult(Guid.NewGuid(), "Somewhere else", ">26 C hours", 300, 142, TM59ComplianceStatus.Fail, true));

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.Null(partOOptimisationRun.CapacityEnvelope);

            Assert.Contains("no deliberate target vector", partOOptimisationRun.CapacityEnvelopeDescription);

            //No step was appended at all, so nothing appears in the history or the round count that never
            //happened.
            Assert.Single(partOOptimisationRun.Steps);
        }

        /// <summary>
        /// <b>12.</b> A passing room is never a deliberate target of the envelope either. The envelope
        /// scales the vector the <i>ordinary</i> policy would have asked for next, and that policy targets
        /// only rooms the production assessment failed - so a design in which every room passes yields no
        /// vector, however much headroom the equipment has.
        /// </summary>
        [Fact]
        public void PassingRooms_AreNotTurnedIntoDeliberateEnvelopeTargets()
        {
            PartOOptimisationRun partOOptimisationRun = Failing(PartOOptimisationStopReason.IterationLimitReached, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            //The run's own status still says Fail - so the guard above it is not what refuses this - while
            //every recorded room result is a pass.
            partOOptimisationRun.Steps[0].TM59Results.Clear();

            foreach (Space space in partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster.GetSpaces() ?? [])
            {
                partOOptimisationRun.Steps[0].TM59Results.Add(new PartOTM59SpaceResult(space.Guid, space.Name, ">26 C hours", 100, 142, TM59ComplianceStatus.Pass, true));
            }

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.Contains("no deliberate target vector", partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// <b>14.</b> A design whose equipment has nothing left to give produces no envelope model, and the
        /// reason is recorded rather than merely implied - that reason <i>is</i> the diagnostic. Still no
        /// simulation: there is no design to simulate.
        /// </summary>
        [Fact]
        public void NoUsefulHeadroom_ProducesAnExplicitReasonAndNoSimulation()
        {
            PartOOptimisationRun partOOptimisationRun = Failing(PartOOptimisationStopReason.CapacityReached, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext, 30);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.False(partOOptimisationRun.HasCapacityEnvelope);

            Assert.NotNull(partOOptimisationRun.CapacityEnvelope);
            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, partOOptimisationRun.CapacityEnvelope.Outcome);

            Assert.Contains("no useful headroom", partOOptimisationRun.CapacityEnvelopeDescription);

            //Nothing was appended to the history, so nothing that did not happen is in the round count.
            Assert.Single(partOOptimisationRun.Steps);
        }

        /// <summary>
        /// The settings default the diagnostic on, because the case it answers - a run stopping short of a
        /// pass - is exactly the case in which the run on its own does not tell an engineer what to do next.
        /// </summary>
        [Fact]
        public void TheCapacityEnvelope_IsOnByDefault()
        {
            Assert.True(new PartOOptimisationSettings().CapacityEnvelope);

            //And it is not one of the things that can make the settings unusable: it is a diagnostic
            //switch, not an airflow.
            Assert.True(new PartOOptimisationSettings { CapacityEnvelope = false }.IsValid(out string _));
        }

        // ---- The real catalogue's own products, through the production orchestration ---------------------
        //
        // Everything above drives Modify.CapacityEnvelope over fixture products, because the orchestration
        // is what those tests are about. These four are the Approved Document O closeout case, on the two
        // products SAM_Systems ships - the Nuaire MRXBOX at 150/150 l/s and the Nuaire XBOXER XBC15 at
        // 190/190 l/s - with the XBC15 DELIBERATELY selected on a ~150 l/s design rather than the smaller
        // MRXBOX the automatic rule would pick.
        //
        // THE PRODUCTION PATH, not the library primitive. Modify.CapacityEnvelope is what
        // OptimisePartOTM59 calls, and it is called here exactly as it calls it. Every fact these tests
        // assert - the ceiling, the retained identity, the absence of a reselection, the untouched
        // Approved Document F requirements and operating airflows - is settled by the orchestrator BEFORE
        // it reaches RunPartOSimulation, which is the only step a licensed TAS is needed for. The fixture
        // states no ventilation route, so the run stops with its own refusal at the re-preparation stage
        // and no simulation is attempted - the same seam every test above uses.
        //
        // SAM_UI does not choose products and must not start: the identities and capacities below are
        // stated, and that they are what the shipped catalogue says is asserted in SAM_Systems' own
        // VentilationUnitCatalogueTests.

        /// <summary>
        /// <b>The closeout case, through the production orchestrator.</b> A 150/150 l/s design whose unit is
        /// selected as the XBC15 grows proportionally onto 190/190 and stops there - the ~40 l/s of
        /// equipment-capacity headroom spent as design airflow, and not a thousandth past the rating.
        /// </summary>
        [Fact]
        public void XBC15sRating_IsTheCeilingTheProductionEnvelopeGrowsWithin()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = partOOptimisationRun.CapacityEnvelope;

            Assert.NotNull(designAirFlowCapacityEnvelope);
            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);
            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.Scaled, designAirFlowCapacityEnvelope.Outcome);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(190.0 / 150.0, designAirFlowCapacityEnvelopeGroup.Scale, 9);

            Assert.Equal(150, designAirFlowCapacityEnvelopeGroup.SupplyDuty_Before_Lps, 6);
            Assert.Equal(150, designAirFlowCapacityEnvelopeGroup.ExtractDuty_Before_Lps, 6);

            //THE assertion: 190 l/s acted as a ceiling. On the rating, never past it.
            Assert.Equal(190, designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps, 6);
            Assert.Equal(190, designAirFlowCapacityEnvelopeGroup.ExtractDuty_After_Lps, 6);

            Assert.Equal(0, designAirFlowCapacityEnvelopeGroup.SupplyHeadroom_Lps, 6);
            Assert.Equal(0, designAirFlowCapacityEnvelopeGroup.ExtractHeadroom_Lps, 6);

            //The 40 l/s the closeout is about, spent as design airflow on both sides.
            Assert.Equal(40, designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps - designAirFlowCapacityEnvelopeGroup.SupplyDuty_Before_Lps, 6);

            //And the grown design itself, room by room, in the model the orchestrator built.
            Assert.Equal(190, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bedroom, FlowClassification.Supply), 6);
            Assert.Equal(190, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Kitchen, FlowClassification.Extract), 6);

            //The run reports it as a diagnostic, and says the ceiling it reached, in the sentence a user
            //reads - so the number on screen is the selected product's rating and not a design decision.
            Assert.Contains("190/190 l/s of 190/190 l/s", partOOptimisationRun.CapacityEnvelopeDescription);
            Assert.Contains("DIAGNOSTIC ONLY", partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// <b>XBC15 stays selected, through the orchestration.</b> The MRXBOX is offered, is capable of the
        /// 150/150 design the envelope started from, and is <b>smaller on both sides</b> - so anything that
        /// re-ran the selection rule would swap the plant for the cheaper unit and then report no headroom.
        /// <para>
        /// The production path has two separate reasons this cannot happen, and both are asserted: the
        /// envelope reads the model's own selection rather than choosing, and the re-preparation over the
        /// envelope design is handed a <b>null catalogue</b> on purpose, so preparation has nothing to
        /// select from either.
        /// </para>
        /// </summary>
        [Fact]
        public void TheProductionEnvelope_KeepsXBC15Selected_AndNeverReselects()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            //The premise: the automatic rule would prefer the other product at this duty.
            Assert.Equal(model_MRXBOX, Analytical.Query.SelectSmallestCapableVentilationUnit(partOPreparationContext.VentilationUnitCapacityDescriptors, 150, 150).VentilationUnitReference.Model);

            Assert.Equal(model_XBC15, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = partOOptimisationRun.CapacityEnvelope;

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            //The group the ceiling came from is the XBC15, by identity.
            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(model_XBC15, designAirFlowCapacityEnvelopeGroup.VentilationUnitReference.Model);
            Assert.Equal(190, designAirFlowCapacityEnvelopeGroup.VentilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps, 6);

            //Still selected on the design the optimisation accepted, and on the envelope's own model.
            Assert.Equal(model_XBC15, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));
            Assert.Equal(model_XBC15, SelectedModel(designAirFlowCapacityEnvelope.AdjacencyCluster));

            //No dwelling of the grown round reports a reselection.
            Assert.All(designAirFlowCapacityEnvelope.RoundCandidate.DwellingRounds, x => Assert.Equal(VentilationUnitSelectionOutcome.Kept, x.VentilationUnitSelectionOutcome));

            //And the sentence the user reads says so, rather than leaving it to be trusted.
            Assert.Contains("No product was reselected", partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// <b>The authority separation, on the real case, through the orchestration.</b>
        /// <code>
        /// PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow
        /// </code>
        /// Design airflow moves. The Approved Document F requirements do not. The equipment capacity is read
        /// as a ceiling and written nowhere. No operating airflow is produced. And the design the
        /// optimisation accepted is left exactly as it was - the whole safety of the operation, because an
        /// envelope is a design the ordinary policy refused and a later round must never start from it.
        /// </summary>
        [Fact]
        public void TheProductionEnvelopeOverXBC15_MovesOnlyDesignAirflow()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            AdjacencyCluster adjacencyCluster_LastValid = partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster;

            Dictionary<string, double> requirements_Before = Requirements(adjacencyCluster_LastValid);
            Dictionary<string, double> designs_Before = Designs(adjacencyCluster_LastValid);
            List<string> airMovements_Before = AirMovements(adjacencyCluster_LastValid);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = partOOptimisationRun.CapacityEnvelope;

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            //1. Approved Document F is untouched in the grown model. The requirement was read as a floor and
            //   never written - 13 l/s stays 13 l/s beside a 190 l/s design.
            Assert.Equal(requirements_Before, Requirements(designAirFlowCapacityEnvelope.AdjacencyCluster));
            Assert.Equal(13, Requirement(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bedroom, FlowClassification.Supply), 6);

            //2. No operating airflow. The envelope creates, removes and re-rates no air movement - that is
            //   the preparation's job, and doing it here would collapse design airflow into runtime airflow.
            Assert.Equal(airMovements_Before, AirMovements(designAirFlowCapacityEnvelope.AdjacencyCluster));

            //3. The design the optimisation accepted is untouched, by value and by reference, and remains
            //   the run's last valid design.
            Assert.Equal(designs_Before, Designs(adjacencyCluster_LastValid));
            Assert.Equal(150, Design(adjacencyCluster_LastValid, name_Bedroom, FlowClassification.Supply), 6);
            Assert.NotSame(adjacencyCluster_LastValid, designAirFlowCapacityEnvelope.AdjacencyCluster);

            //4. The envelope is NOT the run's answer and is not counted as one of its rounds - the
            //   baseline is not a round either, so nothing here is.
            Assert.Equal(0, partOOptimisationRun.Rounds);
            Assert.Empty(partOOptimisationRun.Steps.FindAll(x => x.IsOptimisationRound));
            Assert.NotSame(partOOptimisationRun.AnalyticalModel_LastValid, partOOptimisationRun.AnalyticalModel_CapacityEnvelope);

            //5. The realised design DID move, and the deliberate adjustments report it against the design
            //   it grew from rather than against a capacity.
            Assert.NotEqual(designs_Before, Designs(designAirFlowCapacityEnvelope.AdjacencyCluster));
            Assert.Contains("no Approved Document F requirement was altered and no operating airflow was written", partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// A design already standing on the XBC15's rating has nothing left to give. The orchestrator says
        /// so in its own words, spends <b>no simulation</b> on it, appends no step - and does not reach for
        /// the larger product that is also on offer. No capacity rule is relaxed to produce an answer.
        /// </summary>
        [Fact]
        public void ADesignAlreadyAtXBC15sRating_ReportsNoUsefulHeadroomAndSimulatesNothing()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(190, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Null(partOOptimisationRun.Step_CapacityEnvelope);
            Assert.False(partOOptimisationRun.HasCapacityEnvelope);

            Assert.NotNull(partOOptimisationRun.CapacityEnvelope);
            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, partOOptimisationRun.CapacityEnvelope.Outcome);
            Assert.Contains("no useful headroom", partOOptimisationRun.CapacityEnvelopeDescription);

            //Still the XBC15, and the 500 l/s product on offer was never reached for.
            Assert.Equal(model_XBC15, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));
            Assert.Equal(model_XBC15, Assert.Single(partOOptimisationRun.CapacityEnvelope.Groups).VentilationUnitReference.Model);

            //Nothing was appended to the history, so nothing that did not happen is in the round count.
            Assert.Single(partOOptimisationRun.Steps);

            //And the design was left where it was rather than shrunk to create headroom to report.
            Assert.Equal(190, Design(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster, name_Bedroom, FlowClassification.Supply), 6);
        }

        // =================================================================================================
        // Iteration 2B under MANUAL authority. The identity on the unit is the engineer's, and 2B treats it
        // exactly as it treats an automatically selected one - as a ceiling, never as a decision to revisit.
        // =================================================================================================

        /// <summary>
        /// <b>A manually assigned MRXBOX standing on its own 150 l/s rating has nothing left to give, and
        /// the XBC15 is NOT silently selected.</b> The larger product is offered, is capable of far more,
        /// and is never reached for - 2B reports no useful headroom instead, which is the honest answer and
        /// the one that leaves the escalation decision with the engineer.
        /// </summary>
        [Fact]
        public void AManualMRXBOXAtItsRating_HasNoHeadroom_AndXBC15IsNotSelected()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(150, 0, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            Assert.Equal(model_MRXBOX, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));

            //The premise: a bigger capable product IS on offer.
            Assert.Contains(partOPreparationContext.VentilationUnitCapacityDescriptors, x => x.VentilationUnitReference.Model == model_XBC15);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            //Nothing to grow into, and nothing simulated for it.
            Assert.False(partOOptimisationRun.CapacityEnvelope?.IsScaled ?? false);
            Assert.Contains("headroom", partOOptimisationRun.CapacityEnvelopeDescription);

            //THE assertion: still the MRXBOX. No substitution.
            Assert.Equal(model_MRXBOX, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));
            Assert.DoesNotContain(model_XBC15, partOOptimisationRun.CapacityEnvelopeDescription);
        }

        /// <summary>
        /// <b>A manually assigned MRXBOX on a design BEYOND its rating holds the identity.</b> 175 l/s
        /// against 150 l/s of capability is a design that does not work; 2B reports it and changes neither
        /// the identity nor the design airflow. A capable alternative exists and naming it is a suggestion -
        /// the substitution is the engineer's to make.
        /// </summary>
        [Fact]
        public void AManualMRXBOXBeyondItsRating_HoldsTheIdentity_AndNeverSubstitutes()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(175, 0, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            Assert.Equal(model_MRXBOX, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));

            Dictionary<string, double> designs_Before = Designs(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster);
            Dictionary<string, double> requirements_Before = Requirements(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster);

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            //Held.
            Assert.Equal(model_MRXBOX, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));

            //And the design was NOT cut back to 150 to make the box fit, nor was any requirement touched.
            Assert.Equal(designs_Before, Designs(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));
            Assert.Equal(requirements_Before, Requirements(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));

            //A capable product exists - which is what makes a suggestion possible - and naming it here
            //changed nothing.
            Assert.Equal(model_XBC15, Analytical.Query.SelectSmallestCapableVentilationUnit(partOPreparationContext.VentilationUnitCapacityDescriptors, 175, 175).VentilationUnitReference.Model);
            Assert.Equal(model_MRXBOX, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));
        }

        /// <summary>
        /// <b>Convert to Manual, override one dwelling, and Iteration 2B respects the converted identity
        /// exactly.</b> The end-to-end contract: the assignment table takes the automatic MRXBOX, hands
        /// authority to the engineer, is overridden to the XBC15, commits that single identity onto the
        /// model - and the production envelope then grows the design to 190 and stops, because that is now
        /// the ceiling.
        /// </summary>
        [Fact]
        public void AConvertedThenOverriddenIdentity_IsTheCeilingTheEnvelopeRespects()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(150, 0, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            AdjacencyCluster adjacencyCluster = partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster;

            Assert.Equal(model_MRXBOX, SelectedModel(adjacencyCluster));

            AirHandlingUnit airHandlingUnit = Assert.Single(adjacencyCluster.GetObjects<AirHandlingUnit>());

            //The automatic answer, as a table.
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = PartOEquipmentAssignmentSet.Create(
                adjacencyCluster,
                [airHandlingUnit],
                [],
                [],
                partOPreparationContext.VentilationUnitCapacityDescriptors,
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts));

            Assert.Equal(model_MRXBOX, Assert.Single(partOEquipmentAssignmentSet.Assignments).VentilationUnitReference.Model);

            //Convert: an authority change that preserves the identity and writes nothing.
            partOEquipmentAssignmentSet.ConvertToManual();

            Assert.False(partOEquipmentAssignmentSet.HasChanges);
            Assert.Equal(model_MRXBOX, Assert.Single(partOEquipmentAssignmentSet.Assignments).VentilationUnitReference.Model);

            //Override: the engineer states the XBC15 instead.
            VentilationUnitReference ventilationUnitReference_XBC15 = partOPreparationContext.VentilationUnitCapacityDescriptors.Find(x => x.VentilationUnitReference.Model == model_XBC15).VentilationUnitReference;

            Assert.True(partOEquipmentAssignmentSet.Assign(airHandlingUnit.Guid, ventilationUnitReference_XBC15, out _));
            Assert.True(partOEquipmentAssignmentSet.Commit(adjacencyCluster, out _, out List<string> refusals));
            Assert.Empty(refusals);

            Assert.Equal(model_XBC15, SelectedModel(adjacencyCluster));

            //AND WRITTEN BACK. AnalyticalModel.AdjacencyCluster hands out a fresh COPY on every access, so
            //a commit into one access is discarded the moment the next access is made - the model would go
            //on reporting the MRXBOX while the assignment looked applied. Rebuilding the model from the
            //cluster the assignment was written into is what makes it stick, and it is exactly what
            //Modify.PreparePartOIteration does after committing an authored assignment.
            Assert.Equal(model_MRXBOX, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));

            partOOptimisationRun.AnalyticalModel_LastValid = new AnalyticalModel(partOOptimisationRun.AnalyticalModel_LastValid, adjacencyCluster);

            Assert.Equal(model_XBC15, SelectedModel(partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster));

            //And 2B now works within 190, because that is what the model says the dwelling is fitted with.
            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(partOOptimisationRun.CapacityEnvelope.Groups);

            Assert.Equal(model_XBC15, designAirFlowCapacityEnvelopeGroup.VentilationUnitReference.Model);
            Assert.Equal(190, designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps, 6);
            Assert.Equal(model_XBC15, SelectedModel(partOOptimisationRun.CapacityEnvelope.AdjacencyCluster));

            Assert.All(partOOptimisationRun.CapacityEnvelope.RoundCandidate.DwellingRounds, x => Assert.Equal(VentilationUnitSelectionOutcome.Kept, x.VentilationUnitSelectionOutcome));
        }

        /// <summary>
        /// <b>Narrowing the permitted pool after an assignment has been committed changes nothing 2B
        /// respects.</b> The pool is a candidate constraint on selection; the model's identity is the
        /// engineering fact. A procurement change flags the assignment and leaves both the identity and the
        /// ceiling exactly where they were.
        /// </summary>
        [Fact]
        public void NarrowingThePoolAfterCommit_ChangesNeitherTheIdentityNorTheCeiling()
        {
            PartOOptimisationRun partOOptimisationRun = FailingOnTheRealLadder(out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext);

            AdjacencyCluster adjacencyCluster = partOOptimisationRun.AnalyticalModel_LastValid.AdjacencyCluster;

            AirHandlingUnit airHandlingUnit = Assert.Single(adjacencyCluster.GetObjects<AirHandlingUnit>());

            Assert.Equal(model_XBC15, SelectedModel(adjacencyCluster));

            //The XBC15 leaves the permitted set entirely.
            VentilationUnitReference ventilationUnitReference_MRXBOX = partOPreparationContext.VentilationUnitCapacityDescriptors.Find(x => x.VentilationUnitReference.Model == model_MRXBOX).VentilationUnitReference;

            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = PartOEquipmentAssignmentSet.Create(
                adjacencyCluster,
                [airHandlingUnit],
                [],
                [],
                partOPreparationContext.VentilationUnitCapacityDescriptors,
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [ventilationUnitReference_MRXBOX]));

            PartOEquipmentAssignment partOEquipmentAssignment = Assert.Single(partOEquipmentAssignmentSet.Assignments);

            //Flagged, and nothing more.
            Assert.True(partOEquipmentAssignment.IsOutsideAllowedPool);
            Assert.Equal(model_XBC15, partOEquipmentAssignment.VentilationUnitReference.Model);
            Assert.False(partOEquipmentAssignmentSet.HasChanges);

            Assert.True(partOEquipmentAssignmentSet.Commit(adjacencyCluster, out List<string> notes, out _));
            Assert.Empty(notes);

            //The model still says XBC15, and 2B still stops at 190.
            Assert.Equal(model_XBC15, SelectedModel(adjacencyCluster));

            Modify.CapacityEnvelope(partOOptimisationRun, new PartOOptimisationSettings(), partOPreparationContext, partOSimulationContext);

            Assert.Equal(190, Assert.Single(partOOptimisationRun.CapacityEnvelope.Groups).SupplyDuty_After_Lps, 6);
            Assert.Equal(model_XBC15, SelectedModel(partOOptimisationRun.CapacityEnvelope.AdjacencyCluster));
        }

        // ---- Fixture ---------------------------------------------------------------------------------------
        /// <summary>
        /// <b>The real closeout fixture.</b> A run that stopped on capacity with one failing room, over a
        /// dwelling designed at <paramref name="design_Lps"/> on both sides, whose serving unit is selected
        /// as the <b>Nuaire XBOXER XBC15</b> (190/190 l/s) - deliberately NOT the smaller MRXBOX that is
        /// also offered and that the automatic rule would choose at a 150 l/s duty.
        /// <para>
        /// No ventilation route is stated, so the orchestration computes and records the whole envelope and
        /// then stops with its own refusal at the re-preparation stage, before any TAS work. That is the
        /// same seam the tests above use, and it is what makes the production path assertable here.
        /// </para>
        /// </summary>
        private static PartOOptimisationRun FailingOnTheRealLadder(out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext)
        {
            return FailingOnTheRealLadder(150, out partOPreparationContext, out partOSimulationContext);
        }

        private static PartOOptimisationRun FailingOnTheRealLadder(double design_Lps, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext)
        {
            //Index 1 is the XBC15 - the deliberately oversized selection the closeout is about.
            return FailingOnTheRealLadder(design_Lps, 1, out partOPreparationContext, out partOSimulationContext);
        }

        /// <summary>
        /// The same fixture with the selected product stated, so the manual-authority cases can put the
        /// SMALLER product on a design it cannot carry - which is what an engineer authoring an assignment
        /// by hand is able to do, and what the system then has to report rather than correct.
        /// </summary>
        private static PartOOptimisationRun FailingOnTheRealLadder(double design_Lps, int index_Selected, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext)
        {
            //The MRXBOX is offered FIRST, so an envelope that took the head of the list rather than reading
            //the model's own selection would visibly swap the plant.
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors =
            [
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Nuaire", model_MRXBOX, "MR-ECO-COOL-V"), 150, 150, 10),
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Nuaire", model_XBC15, null), 190, 190, 20),
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "Never Selected", null), 500, 500, 99),
            ];

            AdjacencyCluster adjacencyCluster = new();

            Zone zone = new("Flat 1");

            adjacencyCluster.AddObject(zone);

            AirHandlingUnit airHandlingUnit = new("MVHR-01", 20, 20);

            //THE EXPLICIT SELECTION - written as an identity and nothing else, exactly as
            //Analytical.Modify.SelectVentilationUnit and Analytical.Modify.AssignVentilationUnit both write
            //one. An automatically selected product and a manually assigned one are indistinguishable here,
            //which is the point: 2B does not need to know a human chose it.
            airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, ventilationUnitCapacityDescriptors[index_Selected].VentilationUnitReference);

            adjacencyCluster.AddObject(airHandlingUnit);

            VentilationSystem ventilationSystem = new("Flat 1", new VentilationSystemType("Fixture MVHR", "Fixture"));
            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, airHandlingUnit.Name);

            adjacencyCluster.AddObject(ventilationSystem);

            Space space_Bedroom = Room(adjacencyCluster, ventilationSystem, name_Bedroom, PartFTerminalRole.Supply, FlowClassification.Supply, 13, design_Lps);
            Space space_Kitchen = Room(adjacencyCluster, ventilationSystem, name_Kitchen, PartFTerminalRole.LocalKitchenExtract, FlowClassification.Extract, 13, design_Lps);

            adjacencyCluster.AddRelation(zone, space_Bedroom);
            adjacencyCluster.AddRelation(zone, space_Kitchen);

            AnalyticalModel analyticalModel = new("Fixture", null, null, null, adjacencyCluster, null, null);

            partOPreparationContext = new PartOPreparationContext(PartOIteration.BasePassive, [zone], [], ventilationUnitCapacityDescriptors);
            partOSimulationContext = new PartOSimulationContext(Path.GetTempPath(), "Fixture", null, SolarCalculationMethod.SAM, 1, 365);

            PartOOptimisationRun result = new(new PartOOptimisationSettings());

            PartOOptimisationStep partOOptimisationStep = new(0)
            {
                ProjectName = "Fixture-Opt00",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Fixture-Opt00.tsd"),
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            partOOptimisationStep.TM59Results.Add(new PartOTM59SpaceResult(space_Kitchen.Guid, space_Kitchen.Name, ">26 C hours", 300, 142, TM59ComplianceStatus.Fail, true));

            result.Steps.Add(partOOptimisationStep);

            result.AnalyticalModel_LastValid = analyticalModel;
            result.Path_TSD_LastValid = partOOptimisationStep.Path_TSD;
            result.StopReason = PartOOptimisationStopReason.CapacityReached;

            return result;
        }

        /// <summary>The product one model's single air handling unit is selected as.</summary>
        private static string SelectedModel(AdjacencyCluster adjacencyCluster)
        {
            AirHandlingUnit airHandlingUnit = Assert.Single(adjacencyCluster.GetObjects<AirHandlingUnit>());

            return airHandlingUnit.SelectedVentilationUnitReference()?.Model;
        }

        /// <summary>Every room's design airflow on both sides, so a whole model can be compared before and after.</summary>
        private static Dictionary<string, double> Designs(AdjacencyCluster adjacencyCluster)
        {
            Dictionary<string, double> result = [];

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                foreach (FlowClassification flowClassification in new[] { FlowClassification.Supply, FlowClassification.Extract })
                {
                    result[string.Format("{0} {1}", space.Name, flowClassification)] = System.Math.Round(Design(adjacencyCluster, space, flowClassification), 6);
                }
            }

            return result;
        }

        /// <summary>Every room's Approved Document F requirement on both sides - the values no envelope may move.</summary>
        private static Dictionary<string, double> Requirements(AdjacencyCluster adjacencyCluster)
        {
            Dictionary<string, double> result = [];

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                foreach (FlowClassification flowClassification in new[] { FlowClassification.Supply, FlowClassification.Extract })
                {
                    result[string.Format("{0} {1}", space.Name, flowClassification)] = adjacencyCluster.PartFRequiredFlowRate_Lps(space, flowClassification) ?? double.NaN;
                }
            }

            return result;
        }

        private static double Requirement(AdjacencyCluster adjacencyCluster, string name, FlowClassification flowClassification)
        {
            return adjacencyCluster.PartFRequiredFlowRate_Lps(Space(adjacencyCluster, name), flowClassification) ?? double.NaN;
        }

        private static double Design(AdjacencyCluster adjacencyCluster, string name, FlowClassification flowClassification)
        {
            return Design(adjacencyCluster, Space(adjacencyCluster, name), flowClassification);
        }

        private static double Design(AdjacencyCluster adjacencyCluster, Space space, FlowClassification flowClassification)
        {
            return adjacencyCluster.VentilationTerminals(space).VentilationTerminalDesignDuty_Lps(flowClassification) ?? 0;
        }

        private static Space Space(AdjacencyCluster adjacencyCluster, string name)
        {
            Space result = (adjacencyCluster.GetSpaces() ?? []).Find(x => x?.Name == name);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>
        /// Every inter-zone air movement in the model, as text. An envelope must not create, remove or
        /// re-rate one - that is the preparation's job, and doing it here would collapse design airflow into
        /// operating airflow.
        /// </summary>
        private static List<string> AirMovements(AdjacencyCluster adjacencyCluster)
        {
            List<string> result = (adjacencyCluster.GetObjects<SpaceAirMovement>() ?? []).ConvertAll(x => string.Format("space|{0}|{1}", x.Name, x.AirFlow));

            result.AddRange((adjacencyCluster.GetObjects<AirHandlingUnitAirMovement>() ?? []).ConvertAll(x => string.Format("ahu|{0}", x.Name)));

            result.Sort(StringComparer.Ordinal);

            return result;
        }


        /// <summary>
        /// <b>The real Flat 1 shape from the brief</b>, as a history to report on: a baseline, one completed
        /// ordinary round, and a studio carrying design airflow on <i>both</i> sides.
        /// <code>
        /// Studio 1_0  supply   requirement 30   design 40
        /// Studio 1_0  extract  requirement 13   design 22
        /// Bathroom_2  extract  requirement  8   design 18
        /// MVHR-01                               40/40 against a 150/150 product
        /// </code>
        /// </summary>
        private static PartOOptimisationRun FlatOneHistory()
        {
            PartOOptimisationRun result = new(new PartOOptimisationSettings());

            PartOOptimisationStep partOOptimisationStep_Baseline = new(0)
            {
                ProjectName = "Flat1-Opt00",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Flat1-Opt00.tsd"),
                WeatherData = "CIBSE Future Z1",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            result.Steps.Add(partOOptimisationStep_Baseline);

            PartOOptimisationStep partOOptimisationStep_Round = new(2)
            {
                ProjectName = "Flat1-Opt02",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Flat1-Opt02.tsd"),
                WeatherData = "CIBSE Future Z1",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            //The last accepted ordinary round, which is where the design the envelope grows comes from.
            partOOptimisationStep_Round.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Studio, name_Studio, FlowClassification.Supply, 35, 40, 30, false));
            partOOptimisationStep_Round.DerivedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bathroom_Studio, name_Bathroom_Studio, FlowClassification.Extract, 13, 18, 8, true));

            partOOptimisationStep_Round.UnitStates.Add(Unit(40, 40));

            result.Steps.Add(partOOptimisationStep_Round);

            result.AnalyticalModel_LastValid = new AnalyticalModel("last valid", null, null, null, new AdjacencyCluster(), null, null);
            result.Path_TSD_LastValid = partOOptimisationStep_Round.Path_TSD;
            result.StopReason = PartOOptimisationStopReason.CapacityReached;

            return result;
        }

        /// <summary>
        /// The Flat 1 capacity envelope, as <c>Modify.CapacityEnvelope</c> records one: the whole last valid
        /// design vector grown by x3.75 to the selected unit's 150/150 ceiling, with <b>every</b> space and
        /// direction the unit serves as a deliberate adjustment of the diagnostic.
        /// <para>
        /// Built directly rather than simulated, because what is under test here is the <i>presentation</i>.
        /// The arithmetic that produces these figures belongs to
        /// <c>SAM.Analytical.Modify.EvaluateDesignAirFlowCapacityEnvelope</c> and is pinned by
        /// <c>SAM.Tests.PartOCapacityEnvelopeTests</c>; restating it here would be a second copy of an
        /// engineering rule the UI does not own.
        /// </para>
        /// </summary>
        private static PartOOptimisationStep FlatOneEnvelope(PartOOptimisationRun partOOptimisationRun)
        {
            PartOOptimisationStep result = new(3, PartOOptimisationStepKind.CapacityEnvelope)
            {
                ProjectName = "Flat1-OptMax",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Flat1-OptMax.tsd"),
                WeatherData = "CIBSE Future Z1",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Pass,
                IsCompleted = true,
            };

            //x3.75, applied to everything: 40 -> 150, 22 -> 82.5, 18 -> 67.5. Nothing is derived - the
            //growth is balanced by construction.
            result.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Studio, name_Studio, FlowClassification.Supply, 40, 150, 30, false));
            result.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Studio, name_Studio, FlowClassification.Extract, 22, 82.5, 13, false));
            result.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bathroom_Studio, name_Bathroom_Studio, FlowClassification.Extract, 18, 67.5, 8, false));

            result.UnitStates.Add(Unit(150, 150));

            partOOptimisationRun.Steps.Add(result);

            partOOptimisationRun.AnalyticalModel_CapacityEnvelope = new AnalyticalModel("capacity envelope", null, null, null, new AdjacencyCluster(), null, null);
            partOOptimisationRun.Path_TSD_CapacityEnvelope = result.Path_TSD;

            return result;
        }

        /// <summary>
        /// The Flat 1 unit at a stated duty, always selected as the same 150/150 product - the headroom
        /// beside it is derived from the two, never stated separately.
        /// </summary>
        private static PartOOptimisationUnitState Unit(double supplyDuty_Lps, double extractDuty_Lps)
        {
            VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = new(new VentilationUnitReference("Test Fixture", "MVHR-150", null), 150, 150, 0);

            return new PartOOptimisationUnitState("MVHR-01", "Flat 1", supplyDuty_Lps, extractDuty_Lps, ventilationUnitCapacityDescriptor.VentilationUnitReference, ventilationUnitCapacityDescriptor, VentilationUnitSelectionOutcome.Kept, null);
        }

        /// <summary>
        /// What the visible rows of one direction total - the sum a reader performs to reconcile the table
        /// against the air handling unit's duty.
        /// </summary>
        private static double Sum(List<PartOOptimisationAirFlowRow> partOOptimisationAirFlowRows, string direction)
        {
            double result = 0;

            foreach (PartOOptimisationAirFlowRow partOOptimisationAirFlowRow in partOOptimisationAirFlowRows)
            {
                if (partOOptimisationAirFlowRow.Direction == direction)
                {
                    result += partOOptimisationAirFlowRow.Achieved_Lps;
                }
            }

            return result;
        }

        /// <summary>
        /// A baseline and one completed ordinary round, built directly - so what the run and the
        /// presentation do with a given history can be stated without a TAS simulation behind it.
        /// </summary>
        private static PartOOptimisationRun History(out AnalyticalModel analyticalModel_LastValid)
        {
            VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = new(new VentilationUnitReference("Test Fixture", "MVHR-150", null), 150, 150, 0);

            PartOOptimisationRun result = new(new PartOOptimisationSettings());

            PartOOptimisationStep partOOptimisationStep_Baseline = new(0)
            {
                ProjectName = "Fixture-Opt00",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Fixture-Opt00.tsd"),
                WeatherData = "CIBSE Future Z1",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            partOOptimisationStep_Baseline.UnitStates.Add(new PartOOptimisationUnitState("MVHR-01", "Flat 1", 30, 30, ventilationUnitCapacityDescriptor.VentilationUnitReference, ventilationUnitCapacityDescriptor, VentilationUnitSelectionOutcome.Kept, null));

            result.Steps.Add(partOOptimisationStep_Baseline);

            PartOOptimisationStep partOOptimisationStep_Round = new(1)
            {
                ProjectName = "Fixture-Opt01",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Fixture-Opt01.tsd"),
                WeatherData = "CIBSE Future Z1",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            partOOptimisationStep_Round.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Kitchen, name_Kitchen, FlowClassification.Extract, 22, 27, 13, false));
            partOOptimisationStep_Round.DerivedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bedroom, name_Bedroom, FlowClassification.Supply, 30, 35, 13, true));

            partOOptimisationStep_Round.TM59Results.Add(new PartOTM59SpaceResult(guid_Kitchen, name_Kitchen, ">26 C hours", 300, 142, TM59ComplianceStatus.Fail, true));

            partOOptimisationStep_Round.UnitStates.Add(new PartOOptimisationUnitState("MVHR-01", "Flat 1", 35, 35, ventilationUnitCapacityDescriptor.VentilationUnitReference, ventilationUnitCapacityDescriptor, VentilationUnitSelectionOutcome.Kept, null));

            result.Steps.Add(partOOptimisationStep_Round);

            analyticalModel_LastValid = new AnalyticalModel("last valid", null, null, null, new AdjacencyCluster(), null, null);

            result.AnalyticalModel_LastValid = analyticalModel_LastValid;
            result.Path_TSD_LastValid = partOOptimisationStep_Round.Path_TSD;
            result.StopReason = PartOOptimisationStopReason.CapacityReached;

            return result;
        }

        /// <summary>
        /// A completed capacity envelope step appended to a history - the state whose separation from the
        /// ordinary answer every test above is about.
        /// </summary>
        private static PartOOptimisationStep Envelope(PartOOptimisationRun partOOptimisationRun)
        {
            VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = new(new VentilationUnitReference("Test Fixture", "MVHR-150", null), 150, 150, 0);

            PartOOptimisationStep result = new(2, PartOOptimisationStepKind.CapacityEnvelope)
            {
                ProjectName = "Fixture-OptMax",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Fixture-OptMax.tsd"),
                WeatherData = "CIBSE Future Z1",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Pass,
                IsCompleted = true,
            };

            //The scaled vector: the kitchen taken from 27 to 63 and the balancing supply following it.
            result.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Kitchen, name_Kitchen, FlowClassification.Extract, 27, 63, 13, false));
            result.DerivedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bedroom, name_Bedroom, FlowClassification.Supply, 35, 71, 13, true));

            result.TM59Results.Add(new PartOTM59SpaceResult(guid_Kitchen, name_Kitchen, ">26 C hours", 100, 142, TM59ComplianceStatus.Pass, true));

            result.UnitStates.Add(new PartOOptimisationUnitState("MVHR-01", "Flat 1", 150, 150, ventilationUnitCapacityDescriptor.VentilationUnitReference, ventilationUnitCapacityDescriptor, VentilationUnitSelectionOutcome.Kept, null));

            partOOptimisationRun.Steps.Add(result);

            partOOptimisationRun.AnalyticalModel_CapacityEnvelope = new AnalyticalModel("capacity envelope", null, null, null, new AdjacencyCluster(), null, null);
            partOOptimisationRun.Path_TSD_CapacityEnvelope = result.Path_TSD;
            partOOptimisationRun.CapacityEnvelopeDescription = "DIAGNOSTIC ONLY - this is not an optimisation round and the design the optimisation accepted is unchanged.";

            return result;
        }

        /// <summary>
        /// A run that stopped with one failing room, over a real dwelling model whose serving unit is
        /// selected as a product rated at <paramref name="maximum_Lps"/> - so the envelope's own no-run
        /// decisions can be reached and asserted without any TAS work.
        /// </summary>
        private static PartOOptimisationRun Failing(PartOOptimisationStopReason partOOptimisationStopReason, out PartOPreparationContext partOPreparationContext, out PartOSimulationContext partOSimulationContext, double maximum_Lps = 150)
        {
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors =
            [
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "MVHR", null), maximum_Lps, maximum_Lps, 0),
            ];

            AdjacencyCluster adjacencyCluster = new();

            Zone zone = new("Flat 1");

            adjacencyCluster.AddObject(zone);

            AirHandlingUnit airHandlingUnit = new("MVHR-01", 20, 20);

            airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, ventilationUnitCapacityDescriptors[0].VentilationUnitReference);

            adjacencyCluster.AddObject(airHandlingUnit);

            VentilationSystem ventilationSystem = new("Flat 1", new VentilationSystemType("Fixture MVHR", "Fixture"));
            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, airHandlingUnit.Name);

            adjacencyCluster.AddObject(ventilationSystem);

            Space space_Bedroom = Room(adjacencyCluster, ventilationSystem, name_Bedroom, PartFTerminalRole.Supply, FlowClassification.Supply, 13, 30);
            Space space_Kitchen = Room(adjacencyCluster, ventilationSystem, name_Kitchen, PartFTerminalRole.LocalKitchenExtract, FlowClassification.Extract, 13, 30);

            adjacencyCluster.AddRelation(zone, space_Bedroom);
            adjacencyCluster.AddRelation(zone, space_Kitchen);

            AnalyticalModel analyticalModel = new("Fixture", null, null, null, adjacencyCluster, null, null);

            partOPreparationContext = new PartOPreparationContext(PartOIteration.BasePassive, [zone], [], ventilationUnitCapacityDescriptors);
            partOSimulationContext = new PartOSimulationContext(Path.GetTempPath(), "Fixture", null, SolarCalculationMethod.SAM, 1, 365);

            PartOOptimisationRun result = new(new PartOOptimisationSettings());

            PartOOptimisationStep partOOptimisationStep = new(0)
            {
                ProjectName = "Fixture-Opt00",
                Path_TSD = Path.Combine(Path.GetTempPath(), "Fixture-Opt00.tsd"),
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            partOOptimisationStep.TM59Results.Add(new PartOTM59SpaceResult(space_Kitchen.Guid, space_Kitchen.Name, ">26 C hours", 300, 142, TM59ComplianceStatus.Fail, true));

            result.Steps.Add(partOOptimisationStep);

            result.AnalyticalModel_LastValid = analyticalModel;
            result.Path_TSD_LastValid = partOOptimisationStep.Path_TSD;
            result.StopReason = partOOptimisationStopReason;

            return result;
        }

        private static Space Room(AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, string name, PartFTerminalRole partFTerminalRole, FlowClassification flowClassification, double requirement_Lps, double designFlowRate_Lps)
        {
            Space result = new(name);

            PartFVentilationTerminalRequirement partFVentilationTerminalRequirement = new(name + " requirement", result.Guid, partFTerminalRole)
            {
                ContinuousDesignFlowRate_Lps = requirement_Lps,
            };

            PartFSpaceData partFSpaceData = new();
            partFSpaceData.Terminals.Add(partFVentilationTerminalRequirement);

            result.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);

            adjacencyCluster.AddObject(result);

            VentilationTerminal ventilationTerminal = new(name + " terminal", flowClassification, designFlowRate_Lps);
            ventilationTerminal.SetValue(VentilationTerminalParameter.PartFTerminalReference, new PartFTerminalReference(partFVentilationTerminalRequirement));

            adjacencyCluster.AddObject(ventilationTerminal);
            adjacencyCluster.AddRelation(ventilationTerminal, result);
            adjacencyCluster.AddRelation(ventilationTerminal, ventilationSystem);

            adjacencyCluster.AddRelation(ventilationSystem, result);

            return result;
        }
    }
}
