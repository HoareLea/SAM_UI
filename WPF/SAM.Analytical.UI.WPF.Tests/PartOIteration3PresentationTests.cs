// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// What the Iteration 3 window shows, and - more importantly - what it refuses to show.
    ///
    /// <para><b>The rule this file exists for</b></para>
    /// <para>
    /// On a refusal there is <b>no Candidate B number anywhere in the window</b>. Not a pooled bias, not
    /// a room's mean, not a TM59 verdict, not a filtered grid with nothing in it. A grid of temperatures
    /// beside a refusal is the most convincing wrong answer this window could give, because every number
    /// in it would be a real measurement - just not of the run being reported.
    /// </para>
    /// <para>
    /// What replaces it is the refused stage, the reasons verbatim, the artifacts <b>this attempt</b>
    /// genuinely produced, and the stages that never ran.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOIteration3PresentationTests
    {
        private static readonly Guid guid_Room_1 = new("aaaaaaaa-0000-0000-0000-000000000001");

        private static readonly Guid guid_Room_2 = new("bbbbbbbb-0000-0000-0000-000000000002");

        private static readonly Guid guid_Dwelling_1 = new("11111111-1111-1111-1111-111111111111");

        private static readonly Guid guid_Dwelling_2 = new("22222222-2222-2222-2222-222222222222");

        private static PartOIteration3Comparison Comparison()
        {
            List<PartOIteration3Room> rooms =
            [
                new PartOIteration3Room(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1"),
                new PartOIteration3Room(guid_Room_2, "Bedroom 2", guid_Dwelling_2, "Flat 2"),
            ];

            Dictionary<Guid, double[]> series_A = new()
            {
                { guid_Room_1, [20.0, 20.0] },
                { guid_Room_2, [24.0, 24.0] },
            };

            Dictionary<Guid, double[]> series_B = new()
            {
                { guid_Room_1, [21.0, 21.0] },
                { guid_Room_2, [24.0, 24.0] },
            };

            List<PartOIteration3CriterionComparison> criteria =
            [
                //Same verdict both sides.
                new PartOIteration3CriterionComparison(guid_Room_1, "Bedroom 2", guid_Dwelling_1, "Flat 1", "TM59 Criterion A", true, 10, 32, TM59ComplianceStatus.Pass, 11, 32, TM59ComplianceStatus.Pass),

                //Changed, and a failure.
                new PartOIteration3CriterionComparison(guid_Room_2, "Bedroom 2", guid_Dwelling_2, "Flat 2", "TM59 Criterion B", false, 30, 32, TM59ComplianceStatus.Pass, 40, 32, TM59ComplianceStatus.Fail),
            ];

            return PartOIteration3Comparison.Create(rooms, series_A, series_B, criteria, out List<string> _);
        }

        private static PartOIteration3Result Result_Complete(bool selectedProduct = false)
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                partOIteration3Ledger.Complete(partOIteration3Stage, string.Format("{0} did its work.", Core.Query.Description(partOIteration3Stage)));
            }

            PartOIteration3Record partOIteration3Record = new()
            {
                ProjectName_ReferenceA = "Flat",
                ProjectName_CandidateB = "Flat-It3B",
                Fingerprint_Scenario = "weather=CIBSE 2021 Leeds_TRY | solar=TAS | days 1-365",
                Count_AirSystem = 3,
                Count_Connection_Supply = 6,
                Count_Connection_Extract = 4,
                Count_Connection_Transfer = 4,
                Method_ResultantTemperature = "the Approved Document O thermostat bridge",
            };

            if (selectedProduct)
            {
                partOIteration3Record.BehaviourMode = PartOIteration3BehaviourMode.SelectedProduct;
                partOIteration3Record.Directory_VentilationUnitCatalogue = "C:\\SAM\\VentilationUnits";
                partOIteration3Record.Path_VentilationUnitCatalogue = "C:\\SAM\\VentilationUnits\\VentilationUnitCatalogue.JSON";
                partOIteration3Record.Schema_VentilationUnitCatalogue = "VentilationUnitCatalogue:v2";
                partOIteration3Record.Sha256_VentilationUnitCatalogue = new string('A', 64);

                PartOIteration3EquipmentEvidence equipmentEvidence = new(
                    Guid.NewGuid(), "MVHR-01", "Manufacturer", "Model", "Revision", "Certified source",
                    60.0, 0.86, "SupplyTemperatureEfficiency", false, null,
                    0.62, "TotalBothFans", false, null,
                    150.0, 150.0, 60.0, 60.0, 50.0, 55.0,
                    "DesignAirFlow × constant yearly schedule 1.0",
                    310.0, 310.0, 1.0, 1.0, 1.0,
                    "Certified total-both-fans SFP split equally",
                    "Declared assumption: all simulated fan load enters the air stream");

                Assert.True(equipmentEvidence.BindAirSystem(Guid.NewGuid()));
                partOIteration3Record.Add(equipmentEvidence);
            }

            partOIteration3Record.Adopt(partOIteration3Ledger);

            return new PartOIteration3Result(
                partOIteration3Ledger,
                partOIteration3Record,
                Comparison(),
                new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Pass, null, null, null, null, null, "A report", "C:\\out\\Flat-TM59.txt", 9),
                new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Fail, null, null, null, null, null, "B report", "C:\\out\\Flat-It3B-Bridge-TM59.txt", 9),
                "C:\\out\\Flat-TM59.txt",
                "C:\\out\\Flat-It3B-Bridge-TM59.txt",
                "C:\\out\\Flat-Iteration3.json",
                false,
                ["a note"]);
        }

        private static PartOIteration3Result Result_Refused()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "ready", ["C:\\out\\Flat-Iteration3.json (created)"]);
            partOIteration3Ledger.Complete(PartOIteration3Stage.ReferenceA, "A");
            partOIteration3Ledger.Complete(PartOIteration3Stage.ReferenceATM59, "A TM59");
            partOIteration3Ledger.Refuse(
                PartOIteration3Stage.SystemScope,
                "The ventilation design under assessment could not be scoped.",
                ["Ventilation system 'MV 1' carries a design Supply terminal of 20 l/s in 'Corridor_1'."]);

            PartOIteration3Record partOIteration3Record = new();

            partOIteration3Record.Adopt(partOIteration3Ledger);

            return new PartOIteration3Result(
                partOIteration3Ledger,
                partOIteration3Record,
                //Deliberately supplied: the result type drops it, and the window must not show one either.
                Comparison(),
                new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Pass, null, null, null, null, null, "A report", "C:\\out\\Flat-TM59.txt", 9),
                null,
                "C:\\out\\Flat-TM59.txt",
                null,
                "C:\\out\\Flat-Iteration3.json",
                false,
                null);
        }

        [WpfFact]
        public void A_complete_pairing_shows_its_grid_its_summary_and_both_reports()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Complete(),
            };

            Assert.Equal(2, partOIteration3ResultWindow.Rows_Visible.Count);

            string text = partOIteration3ResultWindow.CopyAllText();

            Assert.Contains("COMPLETE", text);
            Assert.Contains("Bedroom 2", text);
            Assert.Contains("TM59 Criterion A", text);
            Assert.Contains("TM59 Criterion B", text);
        }

        [WpfFact]
        public void A_selected_product_pairing_names_its_product_values_assumptions_and_catalogue_provenance()
        {
            PartOIteration3Result partOIteration3Result = Result_Complete(selectedProduct: true);
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = partOIteration3Result,
            };

            string text = partOIteration3ResultWindow.CopyAllText();
            string report = PartOIteration3ReportText.Text(partOIteration3Result, partOIteration3ResultWindow.Rows_Visible, System.Globalization.CultureInfo.InvariantCulture, provenance: true);

            Assert.Contains("Selected product", text);
            Assert.Contains("Manufacturer / Model / Revision", text);
            Assert.Contains("HR 0.86", text);
            Assert.Contains("SFP 0.62", text);
            Assert.Contains("Declared assumption", text);

            Assert.Contains("VentilationUnitCatalogue:v2", report);
            Assert.Contains(new string('A', 64), report);
            Assert.Contains("Certified source", report);
            Assert.Contains("150", report);
            Assert.Contains("310", report);
        }

        /// <summary>
        /// The whole point. Nothing of Candidate B's numbers reaches the window, and what does reach it
        /// is the refusal, its reasons verbatim and the stages that never ran.
        /// </summary>
        [WpfFact]
        public void A_refused_pairing_shows_no_candidate_B_number_at_all()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Refused(),
            };

            Assert.Empty(partOIteration3ResultWindow.Rows_Visible);

            string text = partOIteration3ResultWindow.CopyAllText();

            Assert.Contains("REFUSED at System scope", text);
            Assert.Contains("Ventilation system 'MV 1' carries a design Supply terminal of 20 l/s in 'Corridor_1'.", text);
            Assert.Contains("Not run:", text);
            Assert.Contains("Materialisation", text);

            //No room, no criterion, no statistic.
            Assert.DoesNotContain("Bedroom 2", text);
            Assert.DoesNotContain("TM59 Criterion", text);
            Assert.DoesNotContain("Bias B-A", text);
        }

        [WpfFact]
        public void A_refused_pairing_reports_only_the_artifacts_this_attempt_produced()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Refused(),
            };

            string text = partOIteration3ResultWindow.CopyAllText();

            Assert.Contains("Files this attempt created or updated:", text);
            Assert.Contains("C:\\out\\Flat-Iteration3.json (created)", text);
        }

        [WpfFact]
        public void The_changed_filter_shows_only_the_criteria_the_two_cases_disagree_about()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Complete(),
            };

            PartOIteration3Row partOIteration3Row = Assert.Single(Filter(partOIteration3ResultWindow, "checkBox_Changed"));

            Assert.Equal("TM59 Criterion B", partOIteration3Row.Criterion);
            Assert.True(partOIteration3Row.Changed);
        }

        [WpfFact]
        public void The_failures_filter_shows_only_the_criteria_either_case_failed()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Complete(),
            };

            PartOIteration3Row partOIteration3Row = Assert.Single(Filter(partOIteration3ResultWindow, "checkBox_Failures"));

            Assert.True(partOIteration3Row.IsFailure);
        }

        [WpfFact]
        public void The_search_narrows_by_dwelling_room_or_criterion_and_is_case_insensitive()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Complete(),
            };

            System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)partOIteration3ResultWindow.FindName("textBox_Search");

            textBox.Text = "flat 2";

            PartOIteration3Row partOIteration3Row = Assert.Single(partOIteration3ResultWindow.Rows_Visible);

            Assert.Equal("Flat 2", partOIteration3Row.Dwelling);

            textBox.Text = "criterion a";

            Assert.Equal("TM59 Criterion A", Assert.Single(partOIteration3ResultWindow.Rows_Visible).Criterion);

            textBox.Text = string.Empty;

            Assert.Equal(2, partOIteration3ResultWindow.Rows_Visible.Count);
        }

        /// <summary>
        /// Re-exporting an unchanged pairing produces the same bytes: invariant culture for every number,
        /// the rows in their built order, and nothing read off a rendered control.
        /// </summary>
        [WpfFact]
        public void Copy_all_is_deterministic_for_an_unchanged_pairing()
        {
            PartOIteration3Result partOIteration3Result = Result_Complete();

            string First()
            {
                PartOIteration3ResultWindow partOIteration3ResultWindow = new()
                {
                    Result = partOIteration3Result,
                };

                return partOIteration3ResultWindow.CopyAllText();
            }

            Assert.Equal(First(), First());
        }

        private static List<PartOIteration3Row> Filter(PartOIteration3ResultWindow partOIteration3ResultWindow, string name)
        {
            System.Windows.Controls.CheckBox checkBox = (System.Windows.Controls.CheckBox)partOIteration3ResultWindow.FindName(name);

            checkBox.IsChecked = true;

            return partOIteration3ResultWindow.Rows_Visible;
        }
    }
}
