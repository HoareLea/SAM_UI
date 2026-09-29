// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Part O / TM59 presentation pass of 29 Sep 2026: the natural ventilation review, the saved TM59
    /// report's provenance, the read-only review's progress window and which case the Hub's primary Review
    /// action opens. Presentation only - no engineering figure is recomputed by anything tested here.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOPresentationPolishTests : IDisposable
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

        private static PartOReviewSummary Summary(bool mechanical)
        {
            return new PartOReviewSummary(
                mechanical ? PartOWorkflowScenario.Text_Iteration2 : PartOWorkflowScenario.Text_Iteration1b,
                "Whole building",
                null,
                mechanical ? "MVHR" : "NV",
                mechanical ? "30.0 l/s supply · 30.0 l/s extract" : "No mechanical design duty",
                null,
                mechanical ? "Equipment selection ran" : "No equipment selection",
                null,
                "1 overheating scenario stated",
                mechanical);
        }

        //-------------------------------------------------------------------------------------------------
        //1. Natural ventilation review
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// Iteration 1b has no mechanical design duty and no equipment: its space table keeps the Part F
        /// requirement as a reference and no longer shows mechanical Design SUP/EXT columns - on screen or in
        /// Copy All.
        /// </summary>
        [WpfFact]
        public void The_natural_ventilation_review_shows_Part_F_and_no_mechanical_design_airflow()
        {
            PartOPreparationWindow partOPreparationWindow = new() { ReviewSummary = Summary(false), EquipmentRows = [] };

            Assert.False(partOPreparationWindow.ShowsSpaceDesignAirflow);

            string text = partOPreparationWindow.CopyAllText();
            Assert.Contains("Dwelling / Zone\tSpace\tPart F required (l/s)" + Environment.NewLine, text);
            Assert.DoesNotContain("Part F required (l/s)\tDesign SUP (l/s)", text);

            //No empty equipment table either: the window shows a sentence, and so does the copy.
            Assert.True(partOPreparationWindow.IsNoEquipmentShown);
            Assert.DoesNotContain("Design SUP", text);
            Assert.Contains("No dwelling units:", text);

            partOPreparationWindow.Close();
        }

        [WpfFact]
        public void The_mechanical_review_still_shows_design_airflow_beside_Part_F()
        {
            PartOPreparationWindow partOPreparationWindow = new() { ReviewSummary = Summary(true) };

            Assert.True(partOPreparationWindow.ShowsSpaceDesignAirflow);
            Assert.Contains("Part F required (l/s)\tDesign SUP (l/s)\tDesign EXT (l/s)", partOPreparationWindow.CopyAllText());

            partOPreparationWindow.Close();
        }

        [Fact]
        public void The_summary_of_a_natural_ventilation_preparation_has_no_mechanical_design_duty()
        {
            PartOVentilationStrategyOption option = PartOVentilationStrategyOption.Options.Find(x => x.PartOVentilationMode == PartOVentilationMode.NaturalVentilation)!;
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(Path.Combine(Path.GetTempPath(), string.Format("SAM_NoCatalogue_{0}", Guid.NewGuid())));

            PartOReviewSummary partOReviewSummary = Modify.Summary(new PartOIterationPreparation(), option, ventilationUnitCatalogue, false, null, null);

            Assert.False(partOReviewSummary.HasMechanicalDesignDuty);
            Assert.Equal("No mechanical design duty", partOReviewSummary.Duty);
        }

        //-------------------------------------------------------------------------------------------------
        //2. The saved TM59 report's provenance
        //-------------------------------------------------------------------------------------------------

        private static TM59AssessmentReport Report()
        {
            List<TMResult> tMResults = [new TM59MechanicalVentilationResult("Bedroom 1", "test", Guid.NewGuid().ToString(), TM52BuildingCategory.CategoryII, 8760, 262, 200, true)];

            return new TM59AssessmentReport(null, tMResults, null, null);
        }

        /// <summary>
        /// The provenance block sits directly under the report's title, and SAM's own report follows it
        /// verbatim - the assessment text is never altered.
        /// </summary>
        [Fact]
        public void The_saved_report_is_headed_by_its_Part_O_case_and_keeps_the_assessment_verbatim()
        {
            TM59AssessmentReport tM59AssessmentReport = Report();

            string text = Modify.PartOTM59ReportText(tM59AssessmentReport, ["Iteration / scenario: Iteration 1b", "Weather: London"]);

            string heading = TM59AssessmentReportFormatter.Heading + Environment.NewLine + new string('=', TM59AssessmentReportFormatter.Heading.Length) + Environment.NewLine;

            Assert.StartsWith(heading + Modify.PartOTM59ReportProvenanceHeading + Environment.NewLine + "Iteration / scenario: Iteration 1b" + Environment.NewLine + "Weather: London" + Environment.NewLine, text);

            //Everything SAM wrote, in order, after the block.
            Assert.EndsWith(tM59AssessmentReport.ToString().Substring(heading.Length), text);
        }

        [Fact]
        public void No_provenance_writes_the_report_exactly_as_before()
        {
            TM59AssessmentReport tM59AssessmentReport = Report();

            Assert.Equal(tM59AssessmentReport.ToString(), Modify.PartOTM59ReportText(tM59AssessmentReport, null));
            Assert.Equal(tM59AssessmentReport.ToString(), Modify.PartOTM59ReportText(tM59AssessmentReport, []));
        }

        [Fact]
        public void The_report_file_written_beside_the_results_carries_the_provenance()
        {
            string path_TSD = Path.Combine(directory, "Flat.tsd");

            Assert.True(Modify.SavePartOTM59Report(path_TSD, Report(), out string path_TM59Report, out string refusal, ["Iteration / scenario: Iteration 2"]));
            Assert.Null(refusal);

            string text = File.ReadAllText(path_TM59Report);

            Assert.Contains(Modify.PartOTM59ReportProvenanceHeading, text);
            Assert.Contains("Iteration / scenario: Iteration 2", text);
            Assert.StartsWith(TM59AssessmentReportFormatter.Heading, text);
        }

        /// <summary>The provenance comes from the result window's own facts: TM59 method and category included.</summary>
        [Fact]
        public void The_provenance_states_the_method_and_says_when_no_weather_is_held()
        {
            List<string> lines = PartOTM59ResultSummary.ReportProvenance(new PartORun(), Report(), [("Case", "Iteration 3 · reference case")]);

            Assert.StartsWith("Case:", lines[0]);
            Assert.Contains(lines, x => x.StartsWith("TM59 method:") && x.Contains("CIBSE TM59:2017"));
            Assert.Contains(lines, x => x.StartsWith("Weather:") && x.Contains("not recorded"));
        }

        //-------------------------------------------------------------------------------------------------
        //3. A short read-only review shows no second window
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A delayed host that finishes before its delay never brings a window up, and neither its start nor
        /// its end holds the job up.
        /// </summary>
        [Fact]
        public void A_review_that_finishes_before_the_delay_shows_no_window_and_does_not_wait()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            PartOProgressHost partOProgressHost = new("Checking TM59 results", "reading", ["TM59 assessment"], false, true, false, TimeSpan.FromSeconds(30));

            partOProgressHost.Start(0);

            Assert.False(partOProgressHost.IsShown);

            partOProgressHost.Dispose();

            stopwatch.Stop();

            Assert.False(partOProgressHost.IsShown);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), stopwatch.Elapsed.ToString());
        }

        //-------------------------------------------------------------------------------------------------
        //5. The primary Review action opens the case in front of the person
        //-------------------------------------------------------------------------------------------------

        private PartOIteration3Eligibility Eligibility_Reviewable()
        {
            string path_TSD = Path.Combine(directory, "Flat.tsd");

            PartOIteration3Ledger partOIteration3Ledger = new();
            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                partOIteration3Ledger.Complete(partOIteration3Stage, "done");
            }

            PartOIteration3Record partOIteration3Record = new()
            {
                BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                Status_ReferenceA = TM59ComplianceStatus.Pass,
                Status_CandidateB = TM59ComplianceStatus.Pass,
                Ticks_Utc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc).Ticks,
            };
            partOIteration3Record.Adopt(partOIteration3Ledger);

            File.WriteAllText(PartOIteration3Paths.Path_Record_ForResults(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance), partOIteration3Record.ToString());

            return new PartOIteration3Eligibility(false, "No completed Part O run is available.", true, null, null, Query.PartOIteration3PairingStatuses(path_TSD));
        }

        /// <summary>
        /// Working in Iteration 3 - the Hub reopened after an Iteration 3 run or opened result - the primary
        /// Review action opens the Iteration 3 result, and says so, instead of silently opening the reference
        /// iteration's TM59.
        /// </summary>
        [WpfFact]
        public void In_Iteration_3_the_primary_review_action_opens_the_Iteration_3_result()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                Iteration3Eligibility = Eligibility_Reviewable(),
                Iteration3Mode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                Iteration3InFocus = true,
            };

            partOWorkflowWindow.CompleteInitialisation();

            Assert.True(partOWorkflowWindow.ReviewOpensIteration3);
            Assert.Equal("Review Iteration 3 result", partOWorkflowWindow.ReviewActionText);
            Assert.True(partOWorkflowWindow.CanReviewResults);

            //A method with no completed result: the action is the scenario's own again.
            partOWorkflowWindow.Iteration3Mode = PartOIteration3BehaviourMode.Parity;

            Assert.False(partOWorkflowWindow.ReviewOpensIteration3);
            Assert.Equal("Review Results", partOWorkflowWindow.ReviewActionText);

            partOWorkflowWindow.Close();
        }

        [WpfFact]
        public void Outside_Iteration_3_the_primary_review_action_is_the_scenarios_own()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                Iteration3Eligibility = Eligibility_Reviewable(),
                Iteration3Mode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
            };

            partOWorkflowWindow.CompleteInitialisation();

            Assert.False(partOWorkflowWindow.ReviewOpensIteration3);
            Assert.Equal("Review Results", partOWorkflowWindow.ReviewActionText);

            partOWorkflowWindow.Close();
        }
    }
}
