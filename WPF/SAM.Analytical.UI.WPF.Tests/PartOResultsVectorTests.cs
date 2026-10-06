// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Every run summary shows the complete design ventilation vector, not only what that round moved.</b>
    /// <para>
    /// The fixture is the reported case. Flat 1: <c>Studio 1_0</c> at 30 l/s supply and 22 l/s extract,
    /// <c>Bathroom_2</c> at 8 l/s extract. An ordinary Iteration 2B round targets the studio's supply and the
    /// bathroom's extract; the studio's extract is not targeted and does not move.
    /// </para>
    /// <para>
    /// The history used to be built from the adjustments alone, and an adjustment exists only where something
    /// changed - so the studio's 22 l/s extract appeared in no row of the baseline or of any ordinary round,
    /// and the table read as though that direction had been removed. The simulation was right and the network
    /// balanced; only the summary was misleading. It is now printed with an explicit <c>UNCHANGED</c> state,
    /// which keeps what EXISTS and what CHANGED clearly apart.
    /// </para>
    /// </summary>
    public class PartOResultsVectorTests
    {
        private const string name_Studio = "Studio 1_0";
        private const string name_Bathroom = "Bathroom_2";

        private static readonly Guid guid_Studio = new("33333333-3333-3333-3333-333333333333");
        private static readonly Guid guid_Bathroom = new("44444444-4444-4444-4444-444444444444");

        // ---- The reported defect ----------------------------------------------------------------------

        /// <summary>
        /// The studio's extract is not targeted by any round, and it must still be visible at the baseline
        /// and at every optimisation run - the exact row that used to be missing.
        /// </summary>
        [Fact]
        public void AnUntargetedButExistingDirection_IsVisibleInBaselineAndInEveryRun()
        {
            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(Run());

            foreach (string run in new[] { "0", "1", "2" })
            {
                PartOOptimisationAirFlowRow row = Row(rows, run, name_Studio, "Extract");

                Assert.True(row is not null, string.Format("Run {0} does not show the studio's extract at all, so it reads as having been removed.", run));
                Assert.Equal(22, row.Achieved_Lps);
            }
        }

        /// <summary>
        /// Every run shows all three relevant Space + Direction rows of the dwelling, and no more.
        /// </summary>
        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        public void EveryRun_ShowsTheCompleteRelevantVector(string run)
        {
            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(Run()).FindAll(x => x.Run == run);

            Assert.Equal(3, rows.Count);

            Assert.NotNull(Row(rows, run, name_Studio, "Supply"));
            Assert.NotNull(Row(rows, run, name_Studio, "Extract"));
            Assert.NotNull(Row(rows, run, name_Bathroom, "Extract"));
        }

        // ---- What exists and what changed stay apart --------------------------------------------------

        /// <summary>
        /// An unchanged direction must never be labelled TARGETED. Nobody asked for it, and saying otherwise
        /// would claim an engineering decision that was never made - the same misreading the targeted/derived
        /// split exists to prevent.
        /// </summary>
        [Fact]
        public void AnUnchangedDirection_IsNotLabelledTargeted()
        {
            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(Run());

            PartOOptimisationAirFlowRow row = Row(rows, "1", name_Studio, "Extract");

            Assert.Equal("UNCHANGED", row.Type);
            Assert.Null(row.Requested_Lps);
        }

        /// <summary>
        /// On an unchanged row the design did not move, so before and achieved are the same figure, and the
        /// Approved Document F requirement is still stated beside them.
        /// </summary>
        [Fact]
        public void AnUnchangedRow_StatesTheDesignItKeptAndTheRequirementItMeets()
        {
            PartOOptimisationAirFlowRow row = Row(PartOOptimisationAirFlowRow.Rows(Run()), "1", name_Studio, "Extract");

            Assert.Equal(22, row.DesignBefore_Lps);
            Assert.Equal(22, row.Achieved_Lps);
            Assert.Equal(8, row.Requirement_Lps);
        }

        /// <summary>The targeted and derived rows keep their own states and figures beside the unchanged one.</summary>
        [Fact]
        public void TargetedAndDerivedRows_AreUnaffectedByTheCompletion()
        {
            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(Run());

            PartOOptimisationAirFlowRow row_Targeted = Row(rows, "1", name_Studio, "Supply");
            Assert.Equal("TARGETED", row_Targeted.Type);
            Assert.Equal(30, row_Targeted.DesignBefore_Lps);
            Assert.Equal(35, row_Targeted.Requested_Lps);
            Assert.Equal(35, row_Targeted.Achieved_Lps);

            PartOOptimisationAirFlowRow row_Derived = Row(rows, "1", name_Bathroom, "Extract");
            Assert.Equal("DERIVED", row_Derived.Type);
            Assert.Null(row_Derived.Requested_Lps);
            Assert.Equal(13, row_Derived.Achieved_Lps);
        }

        /// <summary>
        /// The baseline states the complete baseline vector, at the design the baseline actually carried.
        /// </summary>
        [Fact]
        public void TheBaseline_StatesTheCompleteBaselineVector()
        {
            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(Run()).FindAll(x => x.Run == "0");

            Assert.All(rows, x => Assert.Equal("BASELINE", x.Type));
            Assert.All(rows, x => Assert.Null(x.Requested_Lps));

            Assert.Equal(30, Row(rows, "0", name_Studio, "Supply").Achieved_Lps);
            Assert.Equal(22, Row(rows, "0", name_Studio, "Extract").Achieved_Lps);
            Assert.Equal(8, Row(rows, "0", name_Bathroom, "Extract").Achieved_Lps);
        }

        /// <summary>
        /// Run 2 moved nothing at all. Its rows are still the complete vector, carrying run 1's design.
        /// </summary>
        [Fact]
        public void ARunThatMovedNothing_StillStatesTheWholeVector()
        {
            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(Run()).FindAll(x => x.Run == "2");

            Assert.All(rows, x => Assert.Equal("UNCHANGED", x.Type));

            Assert.Equal(35, Row(rows, "2", name_Studio, "Supply").Achieved_Lps);
            Assert.Equal(22, Row(rows, "2", name_Studio, "Extract").Achieved_Lps);
            Assert.Equal(13, Row(rows, "2", name_Bathroom, "Extract").Achieved_Lps);
        }

        // ---- The semantics that must not move ---------------------------------------------------------

        /// <summary>
        /// <c>PartFRequiredAirFlow != DesignAirFlow</c>. Completing the table adds rows; it never touches the
        /// Approved Document F requirement, which is stated per row and is never a design figure.
        /// </summary>
        [Fact]
        public void ThePartFRequirement_IsStatedOnEveryRowAndIsNeverTheDesign()
        {
            foreach (PartOOptimisationAirFlowRow row in PartOOptimisationAirFlowRow.Rows(Run()))
            {
                Assert.True(row.Requirement_Lps > 0, "Every row states what Approved Document F requires of its room.");
                Assert.True(row.Achieved_Lps >= row.Requirement_Lps, string.Format("{0} {1} fell below its Part F requirement.", row.Space, row.Direction));
            }

            //The studio's extract requirement is 8 l/s and its design is 22 l/s. The two are different
            //quantities and the table keeps them in different columns.
            PartOOptimisationAirFlowRow row_Studio = Row(PartOOptimisationAirFlowRow.Rows(Run()), "1", name_Studio, "Extract");

            Assert.Equal(8, row_Studio.Requirement_Lps);
            Assert.Equal(22, row_Studio.Achieved_Lps);
        }

        /// <summary>
        /// A run recorded before the design vector was captured - or a step whose model could not be read -
        /// still prints its adjustments. Completing the table must not be able to empty it.
        /// </summary>
        [Fact]
        public void ARunWithNoRecordedVector_StillPrintsItsAdjustments()
        {
            PartOOptimisationRun partOOptimisationRun = Run(vector: false);

            List<PartOOptimisationAirFlowRow> rows = PartOOptimisationAirFlowRow.Rows(partOOptimisationRun);

            Assert.Equal("TARGETED", Row(rows, "1", name_Studio, "Supply").Type);
            Assert.Equal("DERIVED", Row(rows, "1", name_Bathroom, "Extract").Type);

            //And the studio's extract is genuinely unavailable rather than silently invented.
            Assert.Null(Row(rows, "1", name_Studio, "Extract"));
        }

        // ---- Fixture ----------------------------------------------------------------------------------

        private static PartOOptimisationAirFlowRow Row(List<PartOOptimisationAirFlowRow> rows, string run, string space, string direction)
        {
            return rows.Find(x => x.Run == run && x.Space == space && x.Direction == direction);
        }

        /// <summary>
        /// Flat 1 as reported: a baseline, one round that targets the studio's supply and derives the
        /// bathroom's extract, and a second round that moves nothing. The studio's 22 l/s extract exists
        /// throughout and is never targeted.
        /// </summary>
        private static PartOOptimisationRun Run(bool vector = true)
        {
            PartOOptimisationRun result = new(new PartOOptimisationSettings());

            result.Steps.Add(Step(0, vector, 30, 22, 8));

            PartOOptimisationStep partOOptimisationStep_Round1 = Step(1, vector, 35, 22, 13);

            partOOptimisationStep_Round1.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Studio, name_Studio, FlowClassification.Supply, 30, 35, 13, false));
            partOOptimisationStep_Round1.DerivedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bathroom, name_Bathroom, FlowClassification.Extract, 8, 13, 8, true));

            result.Steps.Add(partOOptimisationStep_Round1);

            //A round that moved nothing - no adjustment of any kind. Its rows can only come from the vector.
            result.Steps.Add(Step(2, vector, 35, 22, 13));

            return result;
        }

        private static PartOOptimisationStep Step(int iteration, bool vector, double studioSupply_Lps, double studioExtract_Lps, double bathroomExtract_Lps)
        {
            PartOOptimisationStep result = new(iteration)
            {
                ProjectName = string.Format("Flat1-Opt{0:00}", iteration),
                Path_TSD = Path.Combine(Path.GetTempPath(), string.Format("Flat1-Opt{0:00}.tsd", iteration)),
                WeatherData = "CIBSE Future Z1",
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            if (!vector)
            {
                return result;
            }

            //Exactly what Modify.Record writes onto a step: the complete vector, read off that step's model.
            //The Approved Document F requirements - studio 13 l/s supply, studio 8 l/s extract, bathroom
            //8 l/s extract - are properties of the rooms and do not move with the design.
            result.DesignAirFlowStates.Add(new PartODesignAirFlowState(guid_Studio, name_Studio, FlowClassification.Supply, studioSupply_Lps, 13));
            result.DesignAirFlowStates.Add(new PartODesignAirFlowState(guid_Studio, name_Studio, FlowClassification.Extract, studioExtract_Lps, 8));
            result.DesignAirFlowStates.Add(new PartODesignAirFlowState(guid_Bathroom, name_Bathroom, FlowClassification.Extract, bathroomExtract_Lps, 8));

            return result;
        }
    }
}
