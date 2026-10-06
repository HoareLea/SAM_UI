// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.UI;
using SAM.Core;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The Part O pre-simulation gate: SAM Check runs before TAS, and a fatal result stops the run.</b>
    ///
    /// <para><b>Where the gate is</b></para>
    /// <para>
    /// <c>Modify.RunPartOSimulation</c> is the single chokepoint every Part O TAS simulation goes through -
    /// a full run, an isolated run, Iteration 1a, 1b and 2, every Iteration 2B round and the capacity
    /// envelope diagnostic. It builds a <see cref="PartOPreSimulationCheck"/> over the model it is about to
    /// convert, before the gbXML, before the TBD and before the workflow, and returns a refusal instead of
    /// a model when the check is not valid. There is no second validation framework here: the check IS
    /// <c>SAM.Analytical.Create.Log</c>, the authority behind the <c>SAMAnalytical.Check</c> component and
    /// the Check command.
    /// </para>
    ///
    /// <para><b>What these tests pin</b></para>
    /// <para>
    /// The decision, which is what the gate acts on: whether a model is fatal, what the refusal says, that
    /// warnings stay warnings, that an isolated run is judged on the DERIVED model it was handed, and that
    /// checking changes nothing. The intended order - <b>prepared model, Check, TAS conversion, TAS
    /// simulation</b> - is what <c>RunPartOSimulation</c> then executes.
    /// </para>
    /// <para>
    /// The humidistat rule itself, and the transposed-limits defect it was written for, are SAM's and are
    /// pinned in <c>SAM.Tests.PartOHumidistatTests</c> and
    /// <c>SAM.Tests.PartOIterationPreparationTests</c>. Nothing here restates them; the invalid movement
    /// below is used only as a model this gate must refuse.
    /// </para>
    /// </summary>
    public class PartOPreSimulationCheckTests
    {
        // ---- Fatal stops the run -----------------------------------------------------------------------

        /// <summary>
        /// A model carrying a fatal defect is not valid, and therefore not simulated: the gate returns a
        /// refusal in place of a model, and <c>RunPartOSimulation</c>'s callers show it.
        /// </summary>
        [Fact]
        public void AFatalModel_IsNotValid_AndRefuses()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = new(Model(Movement("MVHR-01", 100, 0)));

            Assert.False(partOPreSimulationCheck.IsValid);
            Assert.NotNull(partOPreSimulationCheck.Refusal());
        }

        /// <summary>
        /// The refusal is a usable report: it says the <b>pre-simulation validation</b> failed and that TAS
        /// was not started, and it names the offending object and its Guid - because that is how
        /// <c>Create.Log</c> writes its records, and this passes them through rather than summarising them
        /// away.
        /// </summary>
        [Fact]
        public void TheRefusal_SaysWhatFailedAndNamesTheObject()
        {
            AirHandlingUnitAirMovement airHandlingUnitAirMovement = Movement("MVHR-01", 100, 0);

            PartOPreSimulationCheck partOPreSimulationCheck = new(Model(airHandlingUnitAirMovement));

            string refusal = partOPreSimulationCheck.Refusal();

            Assert.Contains("pre-simulation validation", refusal);
            Assert.Contains("SAMAnalytical.Check", refusal);
            Assert.Contains("TAS was not started", refusal);
            Assert.Contains("MVHR-01", refusal);
            Assert.Contains("AirHandlingUnitAirMovement", refusal);
            Assert.Contains(airHandlingUnitAirMovement.Guid.ToString(), refusal);
        }

        /// <summary>The fatal records are the Errors, and only those.</summary>
        [Fact]
        public void TheErrors_AreTheErrorRecords()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = new(Model(Movement("MVHR-01", 100, 0)));

            LogRecord logRecord = Assert.Single(partOPreSimulationCheck.Errors);

            Assert.Equal(LogRecordType.Error, logRecord.LogRecordType);
            Assert.Contains("overlapping humidity limits", logRecord.Text);
        }

        /// <summary>
        /// A long list is capped so the message stays readable, and says how many it did not print - the
        /// full list is in the log the gate shows.
        /// </summary>
        [Fact]
        public void ManyErrors_AreCappedInTheRefusal_AndTheRemainderIsCounted()
        {
            List<AirHandlingUnitAirMovement> airHandlingUnitAirMovements = [];
            for (int i = 1; i <= 5; i++)
            {
                airHandlingUnitAirMovements.Add(Movement(string.Format("MVHR-{0:00}", i), 100, 0));
            }

            PartOPreSimulationCheck partOPreSimulationCheck = new(Model([.. airHandlingUnitAirMovements]));

            Assert.Equal(5, partOPreSimulationCheck.Errors.Count);

            string refusal = partOPreSimulationCheck.Refusal(2);

            Assert.Contains("5 errors", refusal);
            Assert.Contains("and 3 more", refusal);
        }

        // ---- Warnings stay warnings --------------------------------------------------------------------

        /// <summary>
        /// <b>A model with warnings and no errors is simulated.</b> The gate reports them and returns no
        /// refusal - which is what keeps TAS's intentional "Zone 'MVHR-01' is missing internal conditions on
        /// some daytypes" a warning rather than a reason not to run.
        /// </summary>
        [Fact]
        public void AModelWithOnlyWarnings_IsValid_AndDoesNotRefuse()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = new(Model());

            //The fixture model has no spaces at all, which Create.Log reports as a Warning - so this is a
            //real warning being carried, not an empty log passing by default.
            Assert.NotEmpty(partOPreSimulationCheck.Warnings);
            Assert.Empty(partOPreSimulationCheck.Errors);

            Assert.True(partOPreSimulationCheck.IsValid);
            Assert.Null(partOPreSimulationCheck.Refusal());
        }

        /// <summary>
        /// Warnings are reported alongside the errors when there are errors too, and the refusal says
        /// plainly that they were not what stopped the run.
        /// </summary>
        [Fact]
        public void TheRefusal_SaysWarningsWereNotWhatStoppedIt()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = new(Model(Movement("MVHR-01", 100, 0)));

            Assert.NotEmpty(partOPreSimulationCheck.Warnings);
            Assert.Contains("Warnings do not prevent a simulation", partOPreSimulationCheck.Refusal());
        }

        /// <summary>
        /// A model shaped like the corrected Part O preparation - the generated unit stating no humidity
        /// control as a valid pair - passes. This is the ordinary non-isolated case, and it is valid.
        /// </summary>
        [Fact]
        public void ACorrectedPartOModel_IsValid()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = new(Model(Movement("MVHR-01", 0, 100)));

            Assert.Empty(partOPreSimulationCheck.Errors);
            Assert.True(partOPreSimulationCheck.IsValid);
            Assert.Null(partOPreSimulationCheck.Refusal());
        }

        // ---- Isolation -------------------------------------------------------------------------------

        /// <summary>
        /// <b>An isolated run is judged on the derived isolated model, not on the full building.</b>
        /// <para>
        /// <c>Analytical.Modify.PreparePartOIteration</c> applies the isolation and the run adopts its
        /// output, so the model <c>RunPartOSimulation</c> is handed - and this check is built over - IS the
        /// derived one. A source model that would fail cannot condemn a derived model that passes.
        /// </para>
        /// </summary>
        [Fact]
        public void AnIsolatedRun_IsJudgedOnTheDerivedModel_NotTheSource()
        {
            AnalyticalModel analyticalModel_Source = Model(Movement("MVHR-01", 100, 0));
            AnalyticalModel analyticalModel_Derived = Model(Movement("MVHR-01", 0, 100));

            Assert.False(new PartOPreSimulationCheck(analyticalModel_Source).IsValid);
            Assert.True(new PartOPreSimulationCheck(analyticalModel_Derived).IsValid);
        }

        /// <summary>
        /// And the converse, which is the half that matters for safety: a defect the derivation introduced
        /// is caught even though the building it came from was fine.
        /// </summary>
        [Fact]
        public void AnIsolatedRun_CatchesADefectOnlyTheDerivedModelHas()
        {
            AnalyticalModel analyticalModel_Source = Model(Movement("MVHR-01", 0, 100));
            AnalyticalModel analyticalModel_Derived = Model(Movement("MVHR-01", 100, 0));

            Assert.True(new PartOPreSimulationCheck(analyticalModel_Source).IsValid);
            Assert.False(new PartOPreSimulationCheck(analyticalModel_Derived).IsValid);
        }

        // ---- The gate is read-only, and fails closed ---------------------------------------------------

        /// <summary>
        /// <b>Checking does not change the model.</b> The gate runs over the model about to be simulated, so
        /// anything it modified would be a change to the simulated design made by the validation of it.
        /// </summary>
        [Fact]
        public void TheCheck_DoesNotModifyTheModel()
        {
            AnalyticalModel analyticalModel = Model(Movement("MVHR-01", 100, 0));

            string before = analyticalModel.ToJsonObject().ToJsonString();

            new PartOPreSimulationCheck(analyticalModel);

            Assert.Equal(before, analyticalModel.ToJsonObject().ToJsonString());
        }

        /// <summary>
        /// No model is not a pass. "Nothing was checked" must never read as "nothing was wrong", so the
        /// gate fails closed and says so.
        /// </summary>
        [Fact]
        public void NoModel_IsNotValid()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = new(null);

            Assert.False(partOPreSimulationCheck.IsValid);
            Assert.Null(partOPreSimulationCheck.Log);
            Assert.Contains("No model was supplied", partOPreSimulationCheck.Refusal());
        }

        /// <summary>
        /// The log the gate shows is the whole log, warnings included - the report a person reads, not just
        /// the fatal lines.
        /// </summary>
        [Fact]
        public void TheLog_IsTheWholeLog()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = new(Model(Movement("MVHR-01", 100, 0)));

            Log log = partOPreSimulationCheck.Log;

            Assert.NotNull(log);
            Assert.Contains(log, x => x.LogRecordType == LogRecordType.Error);
            Assert.Contains(log, x => x.LogRecordType == LogRecordType.Warning);
        }

        // ---- What the gate covers ----------------------------------------------------------------------

        /// <summary>
        /// <b>A prepared Part O run is gated.</b> That state is reached by the preparation the ribbon runs
        /// and by <c>PartORun.Prepare</c> in each Iteration 2B round and the capacity envelope, so the
        /// normal run, the isolated run, 1a, 1b, 2, every 2B round and the envelope are all covered by this
        /// one decision.
        /// </summary>
        [Fact]
        public void APreparedRun_IsGated()
        {
            PartORun partORun = new();
            Assert.True(partORun.Prepare(Model(), Scenarios()));

            Assert.Equal(PartORunState.Prepared, partORun.State);
            Assert.NotNull(PartOPreSimulationCheck.Gate(partORun, Model()));
        }

        /// <summary>
        /// <b>And a fatal model on a prepared run is what stops the workflow.</b> The gate returns a check
        /// that is not valid, which is the condition <c>RunPartOSimulation</c> turns into a refusal and a
        /// null model - so no gbXML is written, no TBD is built and no workflow is started.
        /// </summary>
        [Fact]
        public void APreparedRunWithAFatalModel_IsGatedAndInvalid()
        {
            PartORun partORun = new();
            Assert.True(partORun.Prepare(Model(), Scenarios()));

            PartOPreSimulationCheck partOPreSimulationCheck = PartOPreSimulationCheck.Gate(partORun, Model(Movement("MVHR-01", 100, 0)));

            Assert.NotNull(partOPreSimulationCheck);
            Assert.False(partOPreSimulationCheck.IsValid);
            Assert.NotNull(partOPreSimulationCheck.Refusal());
        }

        /// <summary>
        /// <b>The ordinary Simulate command is not gated.</b> It reaches the same method with no run, and
        /// making SAM Check a hard gate on every TAS simulation in SAM is a larger change than the Part O
        /// contract: a long-standing model that simulates today must not stop simulating because the Part O
        /// path grew a gate. The Check command is still available to it on demand.
        /// </summary>
        [Fact]
        public void NoRun_IsNotGated()
        {
            Assert.Null(PartOPreSimulationCheck.Gate(null, Model(Movement("MVHR-01", 100, 0))));
        }

        /// <summary>A run that has not been prepared is not a Part O simulation either.</summary>
        [Fact]
        public void AnUnpreparedRun_IsNotGated()
        {
            PartORun partORun = new();

            Assert.Equal(PartORunState.None, partORun.State);
            Assert.Null(PartOPreSimulationCheck.Gate(partORun, Model(Movement("MVHR-01", 100, 0))));
        }

        /// <summary>
        /// And a run that was prepared and then invalidated - an edit after the preparation, say - drops out
        /// of the gate with the rest of the Part O lifecycle, because it is no longer a Part O run.
        /// </summary>
        [Fact]
        public void AnInvalidatedRun_IsNotGated()
        {
            PartORun partORun = new();
            Assert.True(partORun.Prepare(Model(), Scenarios()));

            partORun.Invalidate("The model was replaced.");

            Assert.Equal(PartORunState.None, partORun.State);
            Assert.Null(PartOPreSimulationCheck.Gate(partORun, Model(Movement("MVHR-01", 100, 0))));
        }

        /// <summary>
        /// <b>The gate is judged on the model it is handed, not on anything the run holds.</b> Two calls on
        /// the same prepared run over two different models give two different answers - which is what makes
        /// "an isolated run is checked on its derived model" a property of the call site rather than a hope.
        /// </summary>
        [Fact]
        public void TheGate_JudgesTheModelItIsHanded()
        {
            PartORun partORun = new();
            Assert.True(partORun.Prepare(Model(), Scenarios()));

            Assert.True(PartOPreSimulationCheck.Gate(partORun, Model(Movement("MVHR-01", 0, 100))).IsValid);
            Assert.False(PartOPreSimulationCheck.Gate(partORun, Model(Movement("MVHR-01", 100, 0))).IsValid);
        }

        // ---- The fixture -------------------------------------------------------------------------------

        /// <summary>
        /// The smallest model <c>Create.Log</c> reports on without erroring, plus one <b>well formed</b>
        /// MVHR per movement handed in: a room, the unit, the unit's air movement, and the supply the unit
        /// delivers to that room.
        /// <para>
        /// Well formed on purpose, so that the only thing a test below varies is the humidity limit pair.
        /// A unit that delivers nothing is itself an error - its generated TAS plant zone would take in no
        /// air - and a fixture that tripped that rule would be testing the wrong thing. That rule is
        /// pinned where it belongs, in <c>SAM.Tests.PartOPlantZoneIntakeTests</c>.
        /// </para>
        /// <para>
        /// The cluster still carries no panels, which <c>Create.Log</c> reports as a Warning - and that is
        /// what makes this fixture useful, because a test asserting "warnings do not refuse" is then
        /// asserting something. An air handling unit's air movement is checked before the space and panel
        /// checks are reached, since it is related to the unit rather than to any space.
        /// </para>
        /// </summary>
        private static AnalyticalModel Model(params AirHandlingUnitAirMovement[] airHandlingUnitAirMovements)
        {
            AdjacencyCluster adjacencyCluster = new();

            int index = 0;
            foreach (AirHandlingUnitAirMovement airHandlingUnitAirMovement in airHandlingUnitAirMovements ?? [])
            {
                index++;

                Space space = new(string.Format("Room {0}", index));
                adjacencyCluster.AddObject(space);

                AirHandlingUnit airHandlingUnit = new(airHandlingUnitAirMovement.Name, 20, 20);
                adjacencyCluster.AddObject(airHandlingUnit);

                adjacencyCluster.AddObject(airHandlingUnitAirMovement);
                adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

                //What the unit DELIVERS, which is what its intake is sized from.
                SpaceAirMovement spaceAirMovement = new(
                    string.Format("Room {0} supply", index),
                    0.03,
                    new ObjectReference(airHandlingUnit).ToString(),
                    new ObjectReference(space).ToString());

                adjacencyCluster.AddObject(spaceAirMovement);
                adjacencyCluster.AddRelation(spaceAirMovement, airHandlingUnit);
                adjacencyCluster.AddRelation(spaceAirMovement, space);
            }

            return new AnalyticalModel(
                "Block",
                null,
                null,
                null,
                adjacencyCluster,
                new MaterialLibrary("Materials"),
                new ProfileLibrary("Profiles"));
        }

        /// <summary>One stated overheating scenario, which a run needs to be preparable at all.</summary>
        private static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(SAM.Analytical.Enums.PartOAssessmentScope.Dwelling, System.Guid.NewGuid(), SAM.Analytical.Enums.PartOIteration.BasePassive)];
        }

        /// <summary>An air movement stating one humidity lower limit and one upper limit, and nothing else.</summary>
        private static AirHandlingUnitAirMovement Movement(string name, double lowerLimit, double upperLimit)
        {
            return new AirHandlingUnitAirMovement(
                name,
                null,
                null,
                new Profile("Lower Limit", ProfileType.Humidification, new double[] { lowerLimit }),
                new Profile("Upper Limit", ProfileType.Dehumidification, new double[] { upperLimit }),
                null);
        }
    }
}
