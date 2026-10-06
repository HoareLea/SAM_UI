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

            string text = Modify.PartOTM59ReportText(tM59AssessmentReport, ["Scenario: Iteration 1b", "Weather: London"]);

            string heading = TM59AssessmentReportFormatter.Heading + Environment.NewLine + new string('=', TM59AssessmentReportFormatter.Heading.Length) + Environment.NewLine;

            Assert.StartsWith(heading + Modify.PartOTM59ReportProvenanceHeading + Environment.NewLine + "Scenario: Iteration 1b" + Environment.NewLine + "Weather: London" + Environment.NewLine, text);

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

            Assert.True(Modify.SavePartOTM59Report(path_TSD, Report(), out string path_TM59Report, out string refusal, ["Scenario: Iteration 2"]));
            Assert.Null(refusal);

            string text = File.ReadAllText(path_TM59Report);

            Assert.Contains(Modify.PartOTM59ReportProvenanceHeading, text);
            Assert.Contains("Scenario: Iteration 2", text);
            Assert.StartsWith(TM59AssessmentReportFormatter.Heading, text);
        }

        /// <summary>The provenance comes from the result window's own facts: TM59 method and category included.</summary>
        [Fact]
        public void The_provenance_states_the_method_and_says_when_no_weather_is_held()
        {
            List<string> lines = PartOTM59ResultSummary.ReportProvenance(new PartORun(), Report(), [("Assessment context", "Iteration 3 — Reference case")]);

            Assert.StartsWith("Assessment context:", lines[0]);
            Assert.Contains(lines, x => x.StartsWith("TM59 method:") && x.Contains("CIBSE TM59:2017"));
            Assert.Contains(lines, x => x.StartsWith("Weather:") && x.Contains("not recorded"));
        }

        /// <summary>
        /// A completed Iteration 1a run, as the TM59 report writers see it: its scenario, its results file and its
        /// TM59 method are all the run's own.
        /// </summary>
        private PartORun CompletedRun()
        {
            Zone zone = new("Flat 1");
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(zone);

            PartORun partORun = new();
            Assert.True(partORun.Prepare(new AnalyticalModel("prepared", null, null, null, adjacencyCluster, null, null), [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)], new PartOPreparationContext(PartOIteration.BasePassive, [zone], [], null)));

            string path_TSD = Path.Combine(directory, "Flat.tsd");
            Assert.True(partORun.ExpectResults(path_TSD));
            File.WriteAllText(path_TSD, "results");

            Assert.True(partORun.Complete(new AnalyticalModel("workflow", null, null, null, new AdjacencyCluster(), null, null), path_TSD, new PartOSimulationContext(directory, "Flat", null, SolarCalculationMethod.SAM, 1, 365), out string refusal), refusal);

            return partORun;
        }

        private static string Value(List<string> lines, string label)
        {
            string line = Assert.Single(lines, x => x.StartsWith(label + ":", StringComparison.Ordinal));

            return line.Substring(label.Length + 1).Trim();
        }

        /// <summary>
        /// The Iteration 1a results reassessed as Iteration 3's reference case are headed by what the results file
        /// physically is - the Iteration 1a scenario - with Iteration 3 beneath it as the assessment they serve,
        /// never the other way round. Everything else the header stated is still there, from the same run.
        /// </summary>
        [Fact]
        public void The_Iteration_3_reference_report_is_headed_by_its_own_scenario_then_its_assessment_context()
        {
            PartORun partORun = CompletedRun();

            List<string> lines = [.. Modify.PartOIteration3ReportProvenance(partORun, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)(partORun.Path_TSD, Report())];

            Assert.Equal("Scenario:", lines[0].Split(' ')[0]);
            Assert.Equal(PartOWorkflowScenario.Text_Iteration1a, Value(lines, "Scenario"));
            Assert.Equal("Iteration 3 — Reference case", Value(lines, "Assessment context"));
            Assert.StartsWith("Assessment context:", lines[1]);

            //The confusing pairing is gone: no "Case" line, and no Iteration 3 wording in the scenario.
            Assert.DoesNotContain(lines, x => x.StartsWith("Case:", StringComparison.Ordinal) || x.StartsWith("Iteration / scenario:", StringComparison.Ordinal));
            Assert.DoesNotContain("Iteration 3", Value(lines, "Scenario"));

            //Preserved, and from the run: the source results are the Iteration 1a TSD that was assessed.
            Assert.Equal(partORun.Path_TSD, Value(lines, "Source TAS result"));
            Assert.Contains("CIBSE TM59:2017", Value(lines, "TM59 method"));
            Assert.Contains(lines, x => x.StartsWith("Thermal model scope:", StringComparison.Ordinal));
            Assert.Contains(lines, x => x.StartsWith("Weather:", StringComparison.Ordinal));

            //The labels still line up as one block.
            Assert.Equal(lines[0].IndexOf(PartOWorkflowScenario.Text_Iteration1a, StringComparison.Ordinal), lines[1].IndexOf("Iteration 3", StringComparison.Ordinal));
        }

        /// <summary>An ordinary report of a run's own results states its scenario and no assessment context at all.</summary>
        [Fact]
        public void A_standalone_report_states_its_scenario_and_no_assessment_context()
        {
            PartORun partORun = CompletedRun();

            List<string> lines = PartOTM59ResultSummary.ReportProvenance(partORun, Report());

            Assert.StartsWith("Scenario:", lines[0]);
            Assert.Equal(PartOWorkflowScenario.Text_Iteration1a, Value(lines, "Scenario"));
            Assert.DoesNotContain(lines, x => x.StartsWith("Assessment context:", StringComparison.Ordinal) || x.StartsWith("Reference case:", StringComparison.Ordinal) || x.StartsWith("Case:", StringComparison.Ordinal));
            Assert.Equal(partORun.Path_TSD, Value(lines, "Source TAS result"));
        }

        /// <summary>
        /// The system case's bridge results ARE Iteration 3's, so Iteration 3 is its scenario and the reference case
        /// is named beneath it; the run's own scenario is not repeated as if the bridge were Iteration 1a's.
        /// </summary>
        [Fact]
        public void The_Iteration_3_system_report_is_headed_by_Iteration_3_and_names_its_reference_case()
        {
            PartORun partORun = CompletedRun();
            string path_TSD_Bridge = Path.Combine(directory, "Flat-It3BMG-Bridge.tsd");

            List<string> lines = [.. Modify.PartOIteration3ReportProvenance(partORun, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)(path_TSD_Bridge, Report())];

            Assert.StartsWith("Scenario:", lines[0]);
            Assert.StartsWith("Iteration 3 — Explicit system and cooling assessment · system case", Value(lines, "Scenario"));
            Assert.StartsWith("Reference case:", lines[1]);
            Assert.Equal(Query.PartOIterationText(partORun), Value(lines, "Reference case"));
            Assert.DoesNotContain(lines, x => x.StartsWith("Assessment context:", StringComparison.Ordinal));
            Assert.Single(lines, x => x.StartsWith("Scenario:", StringComparison.Ordinal));
            Assert.Equal(path_TSD_Bridge, Value(lines, "Source TAS result"));
        }

        /// <summary>The saved reference-case file, end to end: the hierarchy sits under the PART O CASE heading.</summary>
        [Fact]
        public void The_saved_reference_report_file_carries_the_scenario_then_the_assessment_context()
        {
            PartORun partORun = CompletedRun();
            IEnumerable<string> provenance = Modify.PartOIteration3ReportProvenance(partORun, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)(partORun.Path_TSD, Report());

            Assert.True(Modify.SavePartOTM59Report(partORun.Path_TSD, Report(), out string path_TM59Report, out string refusal, provenance), refusal);

            string[] lines = File.ReadAllLines(path_TM59Report);
            int index = Array.IndexOf(lines, Modify.PartOTM59ReportProvenanceHeading);

            Assert.True(index > 0);
            Assert.StartsWith("Scenario:", lines[index + 1]);
            Assert.EndsWith(PartOWorkflowScenario.Text_Iteration1a, lines[index + 1]);
            Assert.StartsWith("Assessment context:", lines[index + 2]);
            Assert.EndsWith("Iteration 3 — Reference case", lines[index + 2]);
        }

        /// <summary>
        /// The Iteration 3 system case's report (<c>...-It3BMG-Bridge-TM59.txt</c>) is written through the same
        /// provenance writer as every other Part O report: it names the iteration, the case and its method, and
        /// the bridge results it was assessed from - taken from the run, never from the file name.
        /// </summary>
        [Fact]
        public void The_Iteration_3_bridge_report_is_headed_by_its_provenance()
        {
            PartORun partORun = new();
            string path_TSD_Bridge = Path.Combine(directory, "Model-It3BMG-Bridge.tsd");

            IEnumerable<string> provenance = Modify.PartOIteration3ReportProvenance(partORun, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)(path_TSD_Bridge, Report());

            Assert.True(Modify.SavePartOTM59Report(path_TSD_Bridge, Report(), out string path_TM59Report, out string refusal, provenance), refusal);

            string text = File.ReadAllText(path_TM59Report);

            Assert.EndsWith("-It3BMG-Bridge-TM59.txt", path_TM59Report);
            Assert.Contains(Modify.PartOTM59ReportProvenanceHeading, text);
            Assert.Contains("Iteration 3", text);
            Assert.Contains("system case", text);
            Assert.Contains("Reference case:", text);
            Assert.Contains("TM59 method:", text);
            Assert.Contains("Weather:", text);
            Assert.Contains("Source TAS result:", text);
            Assert.Contains(path_TSD_Bridge, text);
            Assert.True(text.IndexOf(Modify.PartOTM59ReportProvenanceHeading, StringComparison.Ordinal) < text.IndexOf("Source TAS result:", StringComparison.Ordinal));
        }

        /// <summary>
        /// A case that was assessed is headed by its own verdict in the report window - never "unavailable"
        /// beside a pass.
        /// </summary>
        [Theory]
        [InlineData(TM59ComplianceStatus.Pass, PartOTM59Verdict.Pass)]
        [InlineData(TM59ComplianceStatus.Fail, PartOTM59Verdict.Fail)]
        [InlineData(TM59ComplianceStatus.Undefined, PartOTM59Verdict.NotAssessed)]
        public void An_assessed_case_is_never_headed_unavailable(TM59ComplianceStatus tM59ComplianceStatus, PartOTM59Verdict partOTM59Verdict)
        {
            PartOTM59ResultSummary partOTM59ResultSummary = PartOTM59ResultSummary.ForStatus(tM59ComplianceStatus);

            Assert.Equal(partOTM59Verdict, partOTM59ResultSummary.Verdict);
            Assert.DoesNotContain("UNAVAILABLE", partOTM59ResultSummary.Heading);
            Assert.DoesNotContain("No TM59 assessment was produced", partOTM59ResultSummary.Text);
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
