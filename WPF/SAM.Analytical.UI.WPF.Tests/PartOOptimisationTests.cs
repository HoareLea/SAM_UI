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
    /// <b>Approved Document O Iteration 2B - the automatic optimisation's own policy, gates and history.</b>
    /// <para>
    /// The engineering of a design airflow round belongs to
    /// <c>SAM.Analytical.Modify.EvaluateTargetedDesignAirFlows</c> and is tested there. What is tested here
    /// is everything this layer decides: which rooms an automatic round may target and which it deliberately
    /// cannot, which runs may start an optimisation at all, and whether the history it produces states the
    /// four authorities - requirement, design, capacity, TM59 - apart from one another and describes a fixed
    /// step honestly.
    /// </para>
    /// <para>
    /// No TAS COM type is touched, so this runs on a machine with no TAS installed - the same discipline
    /// <c>PartORunLineageTests</c> keeps.
    /// </para>
    /// </summary>
    public class PartOOptimisationTests
    {
        private const string name_Bedroom = "Bedroom";

        private const string name_Kitchen = "Kitchen";

        private const string name_Bathroom = "Bathroom";

        private const string name_Corridor = "Corridor";

        private const string name_AirHandlingUnitZone = "MVHR-01";

        //Stable, so a fixture that moves the same room in two rounds moves the same ROOM - the history
        //keys on the design space guid, which is the whole point of it.
        private static readonly Guid guid_Kitchen = new("11111111-1111-1111-1111-111111111111");

        private static readonly Guid guid_Bedroom = new("22222222-2222-2222-2222-222222222222");

        private static readonly Guid guid_Bathroom = new("33333333-3333-3333-3333-333333333333");

        // ---- Settings ------------------------------------------------------------------------------------

        /// <summary>
        /// A step of zero would re-simulate the same design every round, and a negative one is not an
        /// optimisation. Both are refused before any TAS time is spent.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(double.NaN)]
        public void AnUnusableAirFlowStep_IsRefused(double airFlowStep_Lps)
        {
            PartOOptimisationSettings partOOptimisationSettings = new()
            {
                AirFlowStep_Lps = airFlowStep_Lps,
            };

            Assert.False(partOOptimisationSettings.IsValid(out string refusal));
            Assert.False(string.IsNullOrWhiteSpace(refusal));
        }

        /// <summary>The iteration guard is mandatory and has to leave at least one round to run.</summary>
        [Fact]
        public void AnIterationLimitBelowOne_IsRefused()
        {
            PartOOptimisationSettings partOOptimisationSettings = new()
            {
                MaximumIterations = 0,
            };

            Assert.False(partOOptimisationSettings.IsValid(out string _));
        }

        /// <summary>The Iteration 2B v1 defaults, stated once so a change to them is a deliberate act.</summary>
        [Fact]
        public void TheDefaults_AreTheSettledIteration2BValues()
        {
            PartOOptimisationSettings partOOptimisationSettings = new();

            Assert.Equal(5, partOOptimisationSettings.AirFlowStep_Lps);
            Assert.True(partOOptimisationSettings.MaximumIterations > 1);
            Assert.True(partOOptimisationSettings.IsValid(out string _));
        }

        // ---- Target selection ----------------------------------------------------------------------------

        /// <summary>
        /// A room with only extract terminals is targeted on extract; one with only supply terminals on
        /// supply; and the step is added to the room's <b>current design</b>, not to its Approved Document F
        /// requirement.
        /// </summary>
        [Fact]
        public void FailingRooms_AreTargetedOnTheSideTheyHaveTerminalsFor()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(
                analyticalModel,
                [Fail(analyticalModel, name_Kitchen), Fail(analyticalModel, name_Bedroom)],
                zones,
                5);

            Assert.Equal(2, partOOptimisationTargetSelection.Targets.Count);

            DesignAirFlowTarget designAirFlowTarget_Kitchen = Target(partOOptimisationTargetSelection, name_Kitchen);

            Assert.Equal(FlowClassification.Extract, designAirFlowTarget_Kitchen.FlowClassification);
            Assert.Equal(27, designAirFlowTarget_Kitchen.DesignFlowRate_Lps, 6);

            DesignAirFlowTarget designAirFlowTarget_Bedroom = Target(partOOptimisationTargetSelection, name_Bedroom);

            Assert.Equal(FlowClassification.Supply, designAirFlowTarget_Bedroom.FlowClassification);
            Assert.Equal(35, designAirFlowTarget_Bedroom.DesignFlowRate_Lps, 6);
        }

        /// <summary>
        /// A passing room is not a target. Raising a design airflow the assessment did not ask about is not
        /// an optimisation - it is an unrequested change.
        /// </summary>
        [Fact]
        public void PassingRooms_AreNotTargeted()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(
                analyticalModel,
                [Fail(analyticalModel, name_Kitchen), Pass(analyticalModel, name_Bedroom), Pass(analyticalModel, name_Bathroom)],
                zones,
                5);

            DesignAirFlowTarget designAirFlowTarget = Assert.Single(partOOptimisationTargetSelection.Targets);

            Assert.Equal(name_Kitchen, designAirFlowTarget.SpaceName);
        }

        /// <summary>
        /// The <b>production</b> compliance status decides, and nothing else. A room whose margin is negative
        /// but whose production verdict is a pass is not targeted - the criteria differ in whether a zero
        /// margin passes, and re-deriving the verdict here would overrule the calculation.
        /// </summary>
        [Fact]
        public void TheProductionComplianceStatusDecides_NotTheMargin()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            Space space = Space(analyticalModel, name_Kitchen);

            //Actual above Limit, which a re-derivation would call a failure - and a production status that
            //says otherwise.
            PartOTM59SpaceResult partOTM59SpaceResult = new(space.Guid, space.Name, ">26 C hours", 300, 142, TM59ComplianceStatus.Pass, true);

            Assert.True(partOTM59SpaceResult.Margin < 0);

            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(analyticalModel, [partOTM59SpaceResult], zones, 5);

            Assert.Empty(partOOptimisationTargetSelection.Targets);
        }

        /// <summary>
        /// A naturally ventilated failure is a real problem and is <b>not</b> a mechanical design airflow
        /// target. Raising a mechanical airflow is not how it is solved.
        /// </summary>
        [Fact]
        public void ANaturalVentilationFailure_IsNotAMechanicalTarget()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            Space space = Space(analyticalModel, name_Kitchen);

            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(
                analyticalModel,
                [new PartOTM59SpaceResult(space.Guid, space.Name, "Criterion 1", 300, 142, TM59ComplianceStatus.Fail, false)],
                zones,
                5);

            Assert.Empty(partOOptimisationTargetSelection.Targets);
        }

        /// <summary>
        /// A communal corridor and the simulation-only zone the preparation builds for an air handling unit
        /// are both outside the Part O dwelling scope, and are excluded <b>by scope</b> rather than by name -
        /// so nothing depends on what anybody called them.
        /// </summary>
        [Fact]
        public void SpacesOutsideTheDwellingScope_AreNotTargeted()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(
                analyticalModel,
                [Fail(analyticalModel, name_Corridor), Fail(analyticalModel, name_AirHandlingUnitZone), Fail(analyticalModel, name_Kitchen)],
                zones,
                5);

            DesignAirFlowTarget designAirFlowTarget = Assert.Single(partOOptimisationTargetSelection.Targets);

            Assert.Equal(name_Kitchen, designAirFlowTarget.SpaceName);

            Assert.Equal(2, partOOptimisationTargetSelection.NotOptimisable.Count);
            Assert.All(partOOptimisationTargetSelection.NotOptimisable, x => Assert.Contains("outside the current Part O dwelling scope", x));
        }

        /// <summary>
        /// A failing room in scope with no design terminal at all is named as not automatically optimisable,
        /// with the reason - and no terminal is created for it.
        /// </summary>
        [Fact]
        public void AFailingRoomWithNoDesignTerminal_IsNotAutomaticallyOptimisable()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones, out Space space_NoTerminal);

            PartOOptimisationTargetSelection partOOptimisationTargetSelection = Query.PartOOptimisationTargets(
                analyticalModel,
                [new PartOTM59SpaceResult(space_NoTerminal.Guid, space_NoTerminal.Name, ">26 C hours", 300, 142, TM59ComplianceStatus.Fail, true)],
                zones,
                5);

            Assert.Empty(partOOptimisationTargetSelection.Targets);

            string description = Assert.Single(partOOptimisationTargetSelection.NotOptimisable);

            Assert.Contains("no Approved Document O design supply or extract terminal", description);
            Assert.Contains("A terminal was not created", description);
        }

        // ---- Eligibility ---------------------------------------------------------------------------------

        /// <summary>Only a completed run can start an optimisation - a prepared one has no results to read.</summary>
        [Fact]
        public void APreparedRunThatWasNeverSimulated_CannotBeOptimised()
        {
            PartORun partORun = new();

            Assert.True(partORun.Prepare(Model("prepared"), Scenarios(), null), "the fixture preparation should be adoptable");

            Assert.False(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string refusal));
            Assert.Contains("not been simulated", refusal);
        }

        /// <summary>
        /// An Iteration 1a run - prepared with no product catalogue - has no selected unit for an
        /// optimisation to work within, and is refused before any TAS time is spent.
        /// </summary>
        [Fact]
        public void ARunPreparedWithoutEquipmentSelection_CannotBeOptimised()
        {
            PartORun partORun = Completed(Context(select: false), SimulationContext(true), out string _);

            Assert.False(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string refusal));
            Assert.Contains("Iteration 1a", refusal);
        }

        /// <summary>
        /// Natural ventilation is not a mechanical airflow optimisation target - the route is asked of SAM
        /// rather than decided here.
        /// </summary>
        [Fact]
        public void ANaturallyVentilatedRun_CannotBeOptimised()
        {
            PartORun partORun = Completed(Context(select: true, partOIteration: PartOIteration.BaseNaturalVentilation), SimulationContext(true), out string _);

            Assert.False(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string refusal));
            Assert.Contains("mechanical design airflow", refusal);
        }

        /// <summary>
        /// A run whose recorded TAS case is not the full year cannot be repeated as one, so an optimisation
        /// would be comparing rounds simulated over different periods.
        /// </summary>
        [Fact]
        public void ARunWhoseRecordedCaseIsNotTheFullYear_CannotBeOptimised()
        {
            PartORun partORun = Completed(Context(select: true), SimulationContext(false), out string _);

            Assert.False(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string refusal));
            Assert.Contains("full-year", refusal);
        }

        /// <summary>
        /// A run completed <b>without</b> its TAS case recorded cannot start an optimisation - there is
        /// nothing to rerun the same weather from.
        /// <para>
        /// This is the shape of a real defect: <c>Modify.Simulate</c> built the context and then completed
        /// the run through the context-less overload, so every baseline produced through the window was
        /// refused here with no way for a user to tell why. The command now completes with the case it
        /// actually ran; this pins the invariant that made it matter.
        /// </para>
        /// </summary>
        [Fact]
        public void ARunCompletedWithoutItsTasCase_CannotBeOptimised()
        {
            PartORun partORun = new();

            Assert.True(partORun.Prepare(Model("prepared"), Scenarios(), Context(select: true)));

            string path_TSD = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOOptimisationTests_{0}.tsd", Guid.NewGuid()));

            Assert.True(partORun.ExpectResults(path_TSD));

            File.WriteAllText(path_TSD, string.Format("results this workflow wrote - {0}", Guid.NewGuid()));

            //The overload that records no case - what Simulate used to call.
            Assert.True(partORun.Complete(Model("workflow"), path_TSD, out string refusal_Complete), refusal_Complete);

            Assert.Null(partORun.SimulationContext);

            Assert.False(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string refusal));
            Assert.Contains("full-year TAS case", refusal);
        }

        /// <summary>An eligible Iteration 2 run can start one.</summary>
        [Fact]
        public void ACompletedIteration2Run_CanBeOptimised()
        {
            PartORun partORun = Completed(Context(select: true), SimulationContext(true), out string _);

            Assert.True(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string refusal), refusal);
        }

        /// <summary>
        /// A model replaced from outside drops the run, and the optimisation is then refused rather than run
        /// against a design its results no longer describe.
        /// </summary>
        [Fact]
        public void AStaleRun_CannotBeOptimised()
        {
            PartORun partORun = Completed(Context(select: true), SimulationContext(true), out string _);

            Assert.True(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string _));

            //Somebody edited, imported or undid something. Unannounced, so the run is dropped.
            partORun.NotifyModified();

            Assert.Equal(PartORunState.None, partORun.State);
            Assert.False(Modify.CanOptimise(partORun, new PartOOptimisationSettings(), out string refusal));
            Assert.False(string.IsNullOrWhiteSpace(refusal));
        }

        /// <summary>Unusable settings are refused before anything runs, not discovered mid-optimisation.</summary>
        [Fact]
        public void UnusableSettings_RefuseBeforeAnythingRuns()
        {
            PartORun partORun = Completed(Context(select: true), SimulationContext(true), out string _);

            Assert.False(Modify.CanOptimise(partORun, new PartOOptimisationSettings { AirFlowStep_Lps = 0 }, out string refusal));
            Assert.Contains("airflow step", refusal);
        }

        // ---- The run's recorded case ---------------------------------------------------------------------

        /// <summary>
        /// The preparation inputs and the TAS case are carried on the run, so an optimisation repeats the
        /// same preparation and the same weather rather than asking again.
        /// </summary>
        [Fact]
        public void ACompletedRun_CarriesItsPreparationAndItsTasCase()
        {
            PartOPreparationContext partOPreparationContext = Context(select: true);
            PartOSimulationContext partOSimulationContext = SimulationContext(true);

            PartORun partORun = Completed(partOPreparationContext, partOSimulationContext, out string _);

            Assert.Same(partOPreparationContext, partORun.PreparationContext);
            Assert.Same(partOSimulationContext, partORun.SimulationContext);
        }

        /// <summary>
        /// A dropped run's recorded case is cleared with everything else, so a workflow announced to it
        /// cannot hand its weather and its dwelling scope to a differently prepared successor.
        /// </summary>
        [Fact]
        public void ADroppedRun_ForgetsItsPreparationAndItsTasCase()
        {
            PartORun partORun = Completed(Context(select: true), SimulationContext(true), out string _);

            partORun.NotifyModified();

            Assert.Null(partORun.PreparationContext);
            Assert.Null(partORun.SimulationContext);
        }

        /// <summary>
        /// Every iteration gets its own project name and therefore its own TBD and TSD - no round can
        /// overwrite the results that are the evidence for another.
        /// </summary>
        [Fact]
        public void EveryIteration_HasItsOwnProjectNameAndResultsFile()
        {
            PartOSimulationContext partOSimulationContext = SimulationContext(true);

            List<string> names = [];

            for (int i = 0; i <= 3; i++)
            {
                names.Add(partOSimulationContext.ProjectName_Iteration(i));
            }

            Assert.Equal(names.Count, new HashSet<string>(names).Count);

            Assert.Equal("Fixture-Opt00", names[0]);
            Assert.Equal("Fixture-Opt03", names[3]);
        }

        /// <summary>
        /// An optimisation run a second time over the design the first one left behind continues the
        /// numbering rather than starting again at <c>-Opt01</c> and overwriting the first run's evidence.
        /// <para>
        /// The iteration is read back off the name of the run being started from, which is a recorded fact -
        /// the results file that produced that design.
        /// </para>
        /// </summary>
        [Fact]
        public void ASecondOptimisation_ContinuesTheNumberingRatherThanOverwriting()
        {
            PartOSimulationContext partOSimulationContext = SimulationContext(true);

            //A first optimisation starts from a baseline carrying no suffix, so it numbers from 1.
            Assert.Equal(0, PartOSimulationContext.Iteration_ProjectName(partOSimulationContext.ProjectName));
            Assert.Equal("Fixture-Opt01", partOSimulationContext.ProjectName_Iteration(0 + 1));

            //It stops at run 10. A second optimisation starting from that design reads 10 back off it...
            string projectName_LastValid = partOSimulationContext.ProjectName_Iteration(10);

            Assert.Equal("Fixture-Opt10", projectName_LastValid);

            int iteration_Baseline = PartOSimulationContext.Iteration_ProjectName(projectName_LastValid);

            Assert.Equal(10, iteration_Baseline);

            //...and numbers its own first round 11, so nothing the first run wrote is touched.
            Assert.Equal("Fixture-Opt11", partOSimulationContext.ProjectName_Iteration(iteration_Baseline + 1));
        }

        /// <summary>
        /// A project name that merely happens to contain the suffix's letters is not an iteration - it
        /// reads as 0 and the numbering starts where it should.
        /// </summary>
        [Theory]
        [InlineData("Fixture", 0)]
        [InlineData("Fixture-Optional", 0)]
        [InlineData("Fixture-Opt", 0)]
        [InlineData("Fixture-Opt00", 0)]
        [InlineData("Fixture-Opt07", 7)]
        [InlineData("Fixture-Opt10", 10)]
        [InlineData("Fixture-Opt01-Opt03", 3)]
        [InlineData(null, 0)]
        public void AProjectNamesIteration_IsReadBackOnlyWhereItIsReallyThere(string projectName, int expected)
        {
            Assert.Equal(expected, PartOSimulationContext.Iteration_ProjectName(projectName));
        }

        // ---- History -------------------------------------------------------------------------------------

        /// <summary>
        /// The history keeps TARGETED and DERIVED apart on every row, and shows the Approved Document F
        /// requirement beside the design so a reader can see the floor was never moved.
        /// </summary>
        [Fact]
        public void TheHistory_DistinguishesTargetedFromDerived()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun);

            //Run 1 specifically: run 0 now states where each room started, and those rows are BASELINE.
            PartOOptimisationAirFlowRow row_Targeted = rows.Find(x => x.Run == "1" && x.Space == name_Kitchen);
            PartOOptimisationAirFlowRow row_Derived = rows.Find(x => x.Run == "1" && x.Space == name_Bedroom);

            Assert.Equal("TARGETED", row_Targeted.Type);
            Assert.Equal(27, row_Targeted.Requested_Lps);
            Assert.Equal(27, row_Targeted.Achieved_Lps);
            Assert.Equal(13, row_Targeted.Requirement_Lps);

            Assert.Equal("DERIVED", row_Derived.Type);

            //Nobody asked for the derived room, and the history says so rather than printing a request that
            //was never made.
            Assert.Null(row_Derived.Requested_Lps);
            Assert.Equal(35, row_Derived.Achieved_Lps);
        }

        /// <summary>
        /// The equipment history keeps duty, maximum and headroom apart, and never says a product was
        /// reselected - Iteration 2B does not buy equipment.
        /// </summary>
        [Fact]
        public void TheEquipmentHistory_KeepsDutyAndCapacityApartAndNeverReselects()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            List<PartOOptimisationUnitRow> rows = PartOOptimisationUnitRow.Rows(partOOptimisationRun);

            Assert.Equal(2, rows.Count);

            Assert.Equal("30/30", rows[0].Duty);
            Assert.Equal("150/150", rows[0].Maximum);
            Assert.Equal("120/120", rows[0].Headroom);
            Assert.Equal("Selected", rows[0].Equipment);

            Assert.Equal("35/35", rows[1].Duty);
            Assert.Equal("150/150", rows[1].Maximum);
            Assert.Equal("Kept", rows[1].Equipment);

            Assert.All(rows, x => Assert.DoesNotContain("Reselected", x.Equipment));

            //The same product throughout.
            Assert.Equal(rows[0].Product, rows[1].Product);
        }

        /// <summary>
        /// A pass at a configured step is described as the <b>first tested passing design at that step</b>,
        /// and never as a minimum required airflow - no search was run between the last failing design and
        /// this one.
        /// </summary>
        [Fact]
        public void APassAtAFixedStep_IsNeverDescribedAsAMinimum()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            partOOptimisationRun.StopReason = PartOOptimisationStopReason.Passed;

            string description = partOOptimisationRun.Description;

            Assert.Contains("FIRST TESTED PASSING DESIGN", description);
            Assert.Contains("not a minimum required airflow", description);
        }

        /// <summary>
        /// A capacity stop keeps the last valid design and says the equipment was the limit - a real
        /// engineering answer, reported as one rather than as a failure of the process.
        /// </summary>
        [Fact]
        public void ACapacityStop_KeepsTheLastValidDesignAndSaysWhy()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            partOOptimisationRun.StopReason = PartOOptimisationStopReason.CapacityReached;

            Assert.Equal(1, partOOptimisationRun.Step_LastValid.Iteration);
            Assert.Contains("cannot carry another full", partOOptimisationRun.Description);
            Assert.Contains("never made automatically", partOOptimisationRun.Description);
            Assert.False(partOOptimisationRun.IsPassed);
        }

        /// <summary>
        /// A cancelled or failed round is recorded but is <b>not</b> a result: it never becomes the last
        /// valid design, and the run never reads as passed.
        /// </summary>
        [Fact]
        public void ACancelledRound_NeverBecomesASuccessfulResult()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            //A third round that was cancelled part way through - recorded, and never completed.
            PartOOptimisationStep partOOptimisationStep = new(2)
            {
                ProjectName = "Fixture-Opt02",
            };

            partOOptimisationStep.Refusals.Add("Cancelled during the simulation of this round.");

            partOOptimisationRun.Steps.Add(partOOptimisationStep);
            partOOptimisationRun.StopReason = PartOOptimisationStopReason.Cancelled;

            Assert.False(partOOptimisationStep.IsCompleted);
            Assert.Equal(1, partOOptimisationRun.Step_LastValid.Iteration);
            Assert.False(partOOptimisationRun.IsPassed);
            Assert.Contains("is kept; the cancelled one is recorded and is not a result", partOOptimisationRun.Description);
        }

        /// <summary>
        /// The baseline is iteration 0 and every round is stored beside it - an optimisation that returned
        /// only its final model could not be audited.
        /// </summary>
        [Fact]
        public void TheBaselineAndEveryRound_AreStored()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            Assert.Equal(2, partOOptimisationRun.Steps.Count);
            Assert.True(partOOptimisationRun.Step_Baseline.IsBaseline);
            Assert.Equal(1, partOOptimisationRun.Rounds);

            Assert.Equal("Fixture-Opt00.tsd", Path.GetFileName(partOOptimisationRun.Steps[0].Path_TSD));
            Assert.Equal("Fixture-Opt01.tsd", Path.GetFileName(partOOptimisationRun.Steps[1].Path_TSD));

            //The same weather case for every round, which is what makes the TM59 movement attributable to
            //the airflow change.
            Assert.Equal(partOOptimisationRun.Steps[0].WeatherData, partOOptimisationRun.Steps[1].WeatherData);
        }

        /// <summary>
        /// The history begins at the baseline, not at the first round. Every room the optimisation later
        /// touched gets a run-0 row saying where it started and what TM59 made of it there - otherwise a
        /// reader has to take the first round's "before" on trust, and a room whose first move comes in a
        /// later round has no stated origin at all.
        /// </summary>
        [Fact]
        public void TheHistory_StatesWhereEveryTouchedRoomStarted()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun);

            List<PartOOptimisationAirFlowRow> rows_Baseline = rows.FindAll(x => x.Run == "0");

            Assert.Equal(2, rows_Baseline.Count);
            Assert.All(rows_Baseline, x => Assert.Equal("BASELINE", x.Type));

            //Nobody asked for a baseline value - it is where the design already was.
            Assert.All(rows_Baseline, x => Assert.Null(x.Requested_Lps));

            PartOOptimisationAirFlowRow row_Kitchen = rows_Baseline.Find(x => x.Space == name_Kitchen);

            //Exactly the Before_Lps of the first adjustment that moved it - a recorded fact, not an
            //inference - and the baseline step's own production verdict.
            Assert.Equal(22, row_Kitchen.DesignBefore_Lps, 6);
            Assert.Equal(22, row_Kitchen.Achieved_Lps, 6);
            Assert.Equal(13, row_Kitchen.Requirement_Lps, 6);

            PartOOptimisationAirFlowRow row_Bedroom = rows_Baseline.Find(x => x.Space == name_Bedroom);

            Assert.Equal(30, row_Bedroom.DesignBefore_Lps, 6);

            //And the rounds still follow it.
            Assert.Contains(rows, x => x.Run == "1" && x.Type == "TARGETED");
            Assert.Contains(rows, x => x.Run == "1" && x.Type == "DERIVED");
        }

        /// <summary>
        /// A room that first moves in a later round still gets a baseline row, and it is listed once
        /// however many rounds went on to move it.
        /// </summary>
        [Fact]
        public void TheHistory_StatesTheOriginOfARoomThatFirstMovesLater()
        {
            PartOOptimisationRun partOOptimisationRun = History();

            //A second round that moves the kitchen again and a room nothing had touched before.
            PartOOptimisationStep partOOptimisationStep = new(2)
            {
                ProjectName = "Fixture-Opt02",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            partOOptimisationStep.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bathroom, name_Bathroom, FlowClassification.Extract, 8, 13, 8, false));
            partOOptimisationStep.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Kitchen, name_Kitchen, FlowClassification.Extract, 27, 32, 13, false));

            partOOptimisationRun.Steps.Add(partOOptimisationStep);

            List<PartOOptimisationAirFlowRow> rows_Baseline = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun).FindAll(x => x.Run == "0");

            //Three rooms have now moved at some point, and each is stated once.
            Assert.Equal(3, rows_Baseline.Count);

            PartOOptimisationAirFlowRow row_Bathroom = rows_Baseline.Find(x => x.Space == name_Bathroom);

            Assert.Equal(8, row_Bathroom.DesignBefore_Lps, 6);

            //The kitchen keeps the value it started at, not the one round 2 found it at.
            Assert.Equal(22, rows_Baseline.Find(x => x.Space == name_Kitchen).DesignBefore_Lps, 6);
        }

        // ---- A pass over a subset is not a pass ------------------------------------------------------

        /// <summary>
        /// A pass whose assessment never looked at an <b>in-scope</b> room is not a pass. A room whose
        /// simulated counterpart does not resolve is excluded before the calculation runs, so the combined
        /// status can genuinely be <c>Pass</c> over a subset - and believing it would end the run on the
        /// claim that every eligible occupied space passes, having never assessed one of them.
        /// </summary>
        [Fact]
        public void APassThatNeverLookedAtAnInScopeRoom_IsNotAPass()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            Space space_Bedroom = Space(analyticalModel, name_Bedroom);

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, [], null);

            //The bedroom produced no result at all; the other rooms were assessed.
            PartOTM59Assessment partOTM59Assessment = new(null, null, null, null, [space_Bedroom.Guid], null);

            string refusal = Modify.PartialAssessment(analyticalModel, partOPreparationContext, partOTM59Assessment);

            Assert.NotNull(refusal);
            Assert.Contains("'Bedroom'", refusal);
            Assert.DoesNotContain("'Kitchen'", refusal);
            Assert.DoesNotContain("'Bathroom'", refusal);
        }

        /// <summary>
        /// Only the <b>dwelling scope</b> is asked about. A communal corridor and the simulation-only zone
        /// an air handling unit gets were never part of the run's claim, so their absence from the
        /// assessment is not a hole in it.
        /// </summary>
        [Fact]
        public void APassWhoseOnlyUnassessedRoomsAreOutsideTheScope_IsStillAPass()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            Space space_Corridor = Space(analyticalModel, name_Corridor);
            Space space_AirHandlingUnitZone = Space(analyticalModel, name_AirHandlingUnitZone);

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, [], null);

            PartOTM59Assessment partOTM59Assessment = new(null, null, null, null, [space_Corridor.Guid, space_AirHandlingUnitZone.Guid], null);

            Assert.Null(Modify.PartialAssessment(analyticalModel, partOPreparationContext, partOTM59Assessment));
        }

        /// <summary>A pass that looked at every in-scope room is a pass - the guard adds nothing to it.</summary>
        [Fact]
        public void APassThatLookedAtEveryInScopeRoom_IsAPass()
        {
            AnalyticalModel analyticalModel = Model(out List<Zone> zones);

            PartOPreparationContext partOPreparationContext = new(PartOIteration.BasePassive, zones, [], null);

            PartOTM59Assessment partOTM59Assessment = new(null, null, null, null, [], null);

            Assert.Null(Modify.PartialAssessment(analyticalModel, partOPreparationContext, partOTM59Assessment));
        }

        // ---- The round count ---------------------------------------------------------------------------

        /// <summary>
        /// A round nobody attempted is not a round. A run that stopped before any target could be taken -
        /// every failure out of scope or without a terminal - is a baseline and nothing else: no run 1 in
        /// the history, and a round count of zero. The recording side of that guarantee sits behind the TAS
        /// simulation seam and is not reachable here; what this pins is the contract the run reports.
        /// </summary>
        [Fact]
        public void ARoundNobodyAttempted_IsNotARound()
        {
            PartOOptimisationRun partOOptimisationRun = new(new PartOOptimisationSettings());

            PartOOptimisationStep partOOptimisationStep_Baseline = new(0)
            {
                ProjectName = "Fixture-Opt00",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            partOOptimisationRun.Steps.Add(partOOptimisationStep_Baseline);
            partOOptimisationRun.StopReason = PartOOptimisationStopReason.NoEligibleTargets;

            Assert.Equal(0, partOOptimisationRun.Rounds);
            Assert.Same(partOOptimisationStep_Baseline, partOOptimisationRun.Step_LastValid);
            Assert.Contains("0 optimisation round(s)", partOOptimisationRun.Description);
        }

        // ---- Fixture -------------------------------------------------------------------------------------

        private static AnalyticalModel Model(out List<Zone> zones)
        {
            return Model(out zones, out Space _);
        }

        /// <summary>
        /// One dwelling zone holding three rooms with design terminals and one with none, plus two spaces
        /// outside the dwelling scope entirely - a communal corridor and the simulation-only zone an air
        /// handling unit gets.
        /// <code>
        /// Flat 1   Bedroom  supply  30      Kitchen extract 22      Bathroom extract 8      Store (no terminal)
        /// outside  Corridor                 MVHR-01
        /// </code>
        /// </summary>
        private static AnalyticalModel Model(out List<Zone> zones, out Space space_NoTerminal)
        {
            AdjacencyCluster adjacencyCluster = new();

            Zone zone = new("Flat 1");

            adjacencyCluster.AddObject(zone);

            Space space_Bedroom = Room(adjacencyCluster, name_Bedroom, FlowClassification.Supply, 30);
            Space space_Kitchen = Room(adjacencyCluster, name_Kitchen, FlowClassification.Extract, 22);
            Space space_Bathroom = Room(adjacencyCluster, name_Bathroom, FlowClassification.Extract, 8);

            space_NoTerminal = new Space("Store");
            adjacencyCluster.AddObject(space_NoTerminal);

            foreach (Space space in new[] { space_Bedroom, space_Kitchen, space_Bathroom, space_NoTerminal })
            {
                adjacencyCluster.AddRelation(zone, space);
            }

            //Outside the dwelling scope, and not related to the zone - which is the only thing that keeps
            //them out. Nothing here reads their names.
            adjacencyCluster.AddObject(new Space(name_Corridor));
            adjacencyCluster.AddObject(new Space(name_AirHandlingUnitZone));

            zones = [zone];

            return new AnalyticalModel("Fixture", null, null, null, adjacencyCluster, null, null);
        }

        private static Space Room(AdjacencyCluster adjacencyCluster, string name, FlowClassification flowClassification, double designFlowRate_Lps)
        {
            Space result = new(name);

            adjacencyCluster.AddObject(result);

            VentilationTerminal ventilationTerminal = new(name + " terminal", flowClassification, designFlowRate_Lps);

            adjacencyCluster.AddObject(ventilationTerminal);
            adjacencyCluster.AddRelation(ventilationTerminal, result);

            return result;
        }

        private static Space Space(AnalyticalModel analyticalModel, string name)
        {
            Space result = (analyticalModel.AdjacencyCluster.GetSpaces() ?? []).Find(x => x?.Name == name);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>A production mechanical FAIL for one room.</summary>
        private static PartOTM59SpaceResult Fail(AnalyticalModel analyticalModel, string name)
        {
            Space space = Space(analyticalModel, name);

            return new PartOTM59SpaceResult(space.Guid, space.Name, ">26 C hours", 300, 142, TM59ComplianceStatus.Fail, true);
        }

        /// <summary>And a production mechanical PASS.</summary>
        private static PartOTM59SpaceResult Pass(AnalyticalModel analyticalModel, string name)
        {
            Space space = Space(analyticalModel, name);

            return new PartOTM59SpaceResult(space.Guid, space.Name, ">26 C hours", 100, 142, TM59ComplianceStatus.Pass, true);
        }

        private static DesignAirFlowTarget Target(PartOOptimisationTargetSelection partOOptimisationTargetSelection, string name)
        {
            DesignAirFlowTarget result = partOOptimisationTargetSelection.Targets.Find(x => x.SpaceName == name);

            Assert.NotNull(result);

            return result;
        }

        private static AnalyticalModel Model(string name)
        {
            return new AnalyticalModel(name, null, null, null, new AdjacencyCluster(), null, null);
        }

        private static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)];
        }

        private static PartOPreparationContext Context(bool select, PartOIteration partOIteration = PartOIteration.BasePassive)
        {
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = select
                ? [new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "MVHR-150", null), 150, 150, 0)]
                : null;

            return new PartOPreparationContext(partOIteration, [new Zone("Flat 1")], [], ventilationUnitCapacityDescriptors)
            {
                OptimisationSettings = new PartOOptimisationSettings(),
            };
        }

        private static PartOSimulationContext SimulationContext(bool fullYear)
        {
            return new PartOSimulationContext(Path.GetTempPath(), "Fixture", null, SolarCalculationMethod.SAM, 1, fullYear ? 365 : 30);
        }

        /// <summary>
        /// A run driven through the production sequence to <c>WorkflowCompleted</c>, carrying both recorded
        /// contexts - the state an optimisation may start from.
        /// <para>
        /// The sequence is the one <c>Modify.PreparePartOIteration</c> and <c>Modify.Simulate</c> perform:
        /// prepare with the preparation's own inputs, announce the results file <b>before</b> the workflow,
        /// let the workflow write it, then complete with the case that ran. A helper that skipped the arming
        /// would exercise a caller that does not exist.
        /// </para>
        /// </summary>
        private static PartORun Completed(PartOPreparationContext partOPreparationContext, PartOSimulationContext partOSimulationContext, out string path_TSD)
        {
            PartORun result = new();

            Assert.True(result.Prepare(Model("prepared"), Scenarios(), partOPreparationContext));

            path_TSD = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOOptimisationTests_{0}.tsd", Guid.NewGuid()));

            Assert.True(result.ExpectResults(path_TSD));

            //What the workflow does to the results file: writes it.
            File.WriteAllText(path_TSD, string.Format("results this workflow wrote - {0}", Guid.NewGuid()));

            Assert.True(result.Complete(Model("workflow"), path_TSD, partOSimulationContext, out string refusal), refusal);

            return result;
        }

        /// <summary>
        /// A two-iteration history - a baseline at 30/30 l/s and one round that targeted the kitchen and
        /// derived the bedroom - built directly, so what the presentation does with a given history can be
        /// stated without a TAS simulation behind it.
        /// </summary>
        private static PartOOptimisationRun History()
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

            partOOptimisationStep_Round.TM59Results.Add(new PartOTM59SpaceResult(guid_Kitchen, name_Kitchen, ">26 C hours", 200, 142, TM59ComplianceStatus.Fail, true));
            partOOptimisationStep_Round.TM59Results.Add(new PartOTM59SpaceResult(guid_Bedroom, name_Bedroom, ">26 C hours", 100, 262, TM59ComplianceStatus.Pass, true));

            partOOptimisationStep_Round.UnitStates.Add(new PartOOptimisationUnitState("MVHR-01", "Flat 1", 35, 35, ventilationUnitCapacityDescriptor.VentilationUnitReference, ventilationUnitCapacityDescriptor, VentilationUnitSelectionOutcome.Kept, null));

            result.Steps.Add(partOOptimisationStep_Round);

            return result;
        }
    }
}
