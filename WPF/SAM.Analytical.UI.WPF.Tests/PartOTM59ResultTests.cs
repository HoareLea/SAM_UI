// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The TM59 result window's summary-first pass: the verdict, the space counts, the states with no verdict,
    /// and the detailed report kept underneath. Every summary here is built from a real
    /// <see cref="TM59AssessmentReport"/> over real <c>TMResult</c> objects - the production objects the report
    /// text is written from - and nothing is parsed out of the report, except by the one test that checks the
    /// counts agree with it.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOTM59ResultTests
    {
        // ----- the verdict and the counts -----------------------------------------------------------------

        /// <summary>
        /// The journey review's H3: the verdict was line 7 of a monospace report. The window now heads itself
        /// with it, and with the counts, as the 25 Sep smoke run's report reads - 8 occupied spaces, 2 pass,
        /// 6 fail, and the corridor that no scenario covers.
        /// </summary>
        [Fact]
        public void AFailingAssessment_IsHeadedFail_WithItsCounts()
        {
            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(Report(pass: 2, fail: 6), 1, null);

            Assert.Equal(PartOTM59Verdict.Fail, summary.Verdict);
            Assert.Equal("TM59 assessment — FAIL", summary.Heading);
            Assert.Equal("8 spaces assessed · 2 pass · 6 fail · 1 not assessed", summary.Counts);
            Assert.Equal(TM59ComplianceStatus.Fail, summary.ComplianceStatus);
            Assert.Null(summary.Reason);
            Assert.True(summary.HasVerdict);
        }

        [Fact]
        public void APassingAssessment_IsHeadedPass()
        {
            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(Report(pass: 3, fail: 0), 0, null);

            Assert.Equal(PartOTM59Verdict.Pass, summary.Verdict);
            Assert.Equal("TM59 assessment — PASS", summary.Heading);
            Assert.Equal("3 spaces assessed · 3 pass · 0 fail · 0 not assessed", summary.Counts);
        }

        /// <summary>The verdict never depends on colour alone: each one has its own glyph and its own word.</summary>
        [Fact]
        public void EveryVerdict_HasItsOwnGlyphAndWord()
        {
            List<PartOTM59ResultSummary> summaries =
            [
                PartOTM59ResultSummary.Create(Report(pass: 1, fail: 0), 0, null),
                PartOTM59ResultSummary.Create(Report(pass: 1, fail: 1), 0, null),
                PartOTM59ResultSummary.Create(Report(pass: 1, fail: 0), 1, "partial"),
                PartOTM59ResultSummary.Unavailable("gone", null),
            ];

            Assert.Equal(4, summaries.Select(x => x.Verdict).Distinct().Count());
            Assert.Equal(4, summaries.Select(x => x.Glyph).Distinct().Count());
            Assert.Equal(4, summaries.Select(x => x.VerdictText).Distinct().Count());
            Assert.All(summaries, x => Assert.Contains(x.VerdictText, x.Heading));
        }

        /// <summary>
        /// The Hub's outcome line has always said "TM59 Pass" / "TM59 Fail" in the assessment's own words; it
        /// still does.
        /// </summary>
        [Theory]
        [InlineData(1, 0, TM59ComplianceStatus.Pass)]
        [InlineData(1, 1, TM59ComplianceStatus.Fail)]
        public void TheHubWord_IsTheAssessmentsOwn_ForAPassOrAFail(int pass, int fail, TM59ComplianceStatus expected)
        {
            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(Report(pass, fail), 0, null);

            Assert.Equal(Core.Query.Description(expected), summary.VerdictWord);
        }

        /// <summary>
        /// The counts are the report's own rows read another way, so they cannot disagree with its tables: the
        /// PASS and FAIL rows of the mechanical ventilation table are exactly the pass and fail counts.
        /// </summary>
        [Fact]
        public void TheCounts_AgreeWithTheReportsOwnRows()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(pass: 5, fail: 3);

            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(tM59AssessmentReport, 0, null);

            List<string> rows = [.. tM59AssessmentReport.ToString().Split(["\r\n", "\n"], StringSplitOptions.None).Where(x => x.StartsWith("Room ", StringComparison.Ordinal))];

            Assert.Equal(rows.Count(x => x.TrimEnd().EndsWith("PASS", StringComparison.Ordinal)), summary.SpaceCount_Pass);
            Assert.Equal(rows.Count(x => x.TrimEnd().EndsWith("FAIL", StringComparison.Ordinal)), summary.SpaceCount_Fail);
            Assert.Equal(rows.Count, summary.SpaceCount_Assessed);
        }

        /// <summary>
        /// The counts are a tally of SAM's structured per-space status and nothing else - on a report mixing a
        /// bedroom that passes Criterion 1 but fails Criterion 2, a passing living room and a failing
        /// mechanical kitchen. The bedroom is one failing space (not a pass and a fail), because SAM says so.
        /// </summary>
        [Fact]
        public void TheCounts_AreATallyOfSAMsStructuredSpaceStatus()
        {
            TM59AssessmentReport tM59AssessmentReport = new(
                null,
                [Mechanical("Kitchen", "kitchen", false)],
                [
                    new TM59NaturalVentilationBedroomResult("Bedroom", "test", "bedroom", TM52BuildingCategory.CategoryII, 3000, 90, 10, 3285, 3000, 90, 32, 40, true),
                    new TM59NaturalVentilationResult("Living", "test", "living", TM52BuildingCategory.CategoryII, 3000, 90, 3000, 90, 10, true),
                ],
                null);

            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(tM59AssessmentReport, 0, null);

            Assert.Equal(TM59ComplianceStatus.Fail, tM59AssessmentReport.OccupiedSpaces.Single(x => x.Reference == "bedroom").ComplianceStatus);
            Assert.Equal(tM59AssessmentReport.OccupiedSpaces.Count, summary.SpaceCount_Assessed);
            Assert.Equal(tM59AssessmentReport.OccupiedSpaces.Count(x => x.ComplianceStatus == TM59ComplianceStatus.Pass), summary.SpaceCount_Pass);
            Assert.Equal(tM59AssessmentReport.OccupiedSpaces.Count(x => x.ComplianceStatus == TM59ComplianceStatus.Fail), summary.SpaceCount_Fail);
            Assert.Equal("3 spaces assessed · 1 pass · 2 fail · 0 not assessed", summary.Counts);
            Assert.Equal(PartOTM59Verdict.Fail, summary.Verdict);
        }

        /// <summary>
        /// The Hub and the result window cannot disagree: the window is given the summary instance itself
        /// (never a copy, and nothing is read back off it), and the Hub's Review line and Prepare &amp; Run
        /// suffix are worded from that same instance - for every verdict.
        /// </summary>
        [WpfTheory]
        [InlineData(PartOTM59Verdict.Pass)]
        [InlineData(PartOTM59Verdict.Fail)]
        [InlineData(PartOTM59Verdict.NotAssessed)]
        [InlineData(PartOTM59Verdict.Unavailable)]
        public void TheHubAndTheWindow_ReadOneSummary_AndCannotDisagree(PartOTM59Verdict partOTM59Verdict)
        {
            PartOTM59ResultSummary summary = partOTM59Verdict switch
            {
                PartOTM59Verdict.Pass => PartOTM59ResultSummary.Create(Report(pass: 2, fail: 0), 0, null),
                PartOTM59Verdict.Fail => PartOTM59ResultSummary.Create(Report(pass: 1, fail: 1), 0, null),
                PartOTM59Verdict.NotAssessed => PartOTM59ResultSummary.Create(Report(pass: 2, fail: 0), 1, "partial"),
                _ => PartOTM59ResultSummary.Unavailable("gone", null),
            };

            Assert.Equal(partOTM59Verdict, summary.Verdict);

            PartOTM59ResultWindow partOTM59ResultWindow = Modify.ResultWindow(summary, "REPORT", [], [], null);

            Assert.Same(summary, partOTM59ResultWindow.ResultSummary);
            Assert.Equal(summary.Heading, partOTM59ResultWindow.VerdictHeading);

            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.ReviewOutcome(summary);

            Assert.NotNull(partOWorkflowOutcome);
            Assert.Equal(string.Format("Results reviewed — TM59 {0}", summary.VerdictText), partOWorkflowOutcome!.Headline);
            Assert.Equal(summary.Glyph, partOWorkflowOutcome.Glyph);
            Assert.EndsWith("no simulation was run", partOWorkflowOutcome.Detail);

            //Prepare & Run's closing line is worded from the same instance too.
            PartOWorkflowOutcome partOWorkflowOutcome_Completed = Modify.CompletedOutcome(PartOWorkflowScenario.Text_Iteration1a, TimeSpan.FromSeconds(53), summary, 0);
            Assert.EndsWith(" — TM59 " + summary.VerdictText, partOWorkflowOutcome_Completed.Headline);
            Assert.Equal(summary.Glyph, partOWorkflowOutcome_Completed.Glyph);

            //The Hub's word and the window's heading word are the same verdict.
            Assert.Equal(summary.VerdictText, summary.VerdictWord.ToUpperInvariant());
            Assert.EndsWith(summary.VerdictText, partOTM59ResultWindow.VerdictHeading, StringComparison.Ordinal);

            //The line's kind follows the verdict: pass, fail, anything else needs attention.
            Assert.Equal(partOTM59Verdict switch { PartOTM59Verdict.Pass => PartOWorkflowOutcomeKind.Success, PartOTM59Verdict.Fail => PartOWorkflowOutcomeKind.Fail, _ => PartOWorkflowOutcomeKind.Warning }, partOWorkflowOutcome.Kind);

            partOTM59ResultWindow.Close();
        }

        [Fact]
        public void NoRun_GivesNoHubLine()
        {
            Assert.Null(Modify.ReviewOutcome(null));
        }

        /// <summary>
        /// Two dwellings can both have a "Kitchen". The counts are by identity, as the report's own grouping
        /// is, so two kitchens are two spaces.
        /// </summary>
        [Fact]
        public void SpacesSharingAName_AreCountedByIdentity()
        {
            TM59AssessmentReport tM59AssessmentReport = new(null, [Mechanical("Kitchen", "a", true), Mechanical("Kitchen", "b", false)], null, null);

            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(tM59AssessmentReport, 0, null);

            Assert.Equal(2, summary.SpaceCount_Assessed);
            Assert.Equal(1, summary.SpaceCount_Pass);
            Assert.Equal(1, summary.SpaceCount_Fail);
        }

        // ----- the states with no verdict -----------------------------------------------------------------

        /// <summary>
        /// A pass over part of the dwelling scope is not a pass - the existing Part O guard Iteration 2B stops
        /// on. The window says NOT ASSESSED with that guard's own reason, and keeps the production status it
        /// read (Pass) for the report below it, which still says so.
        /// </summary>
        [Fact]
        public void APassOverPartOfTheScope_IsNotAssessed_WithTheGuardsReason()
        {
            const string refusal = "The production TM59 assessment returned a pass, but 1 space(s) inside the Part O dwelling scope produced no result at all.";

            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(Report(pass: 3, fail: 0), 1, refusal);

            Assert.Equal(PartOTM59Verdict.NotAssessed, summary.Verdict);
            Assert.Equal("TM59 assessment — NOT ASSESSED", summary.Heading);
            Assert.Equal(refusal, summary.Reason);
            Assert.Equal(TM59ComplianceStatus.Pass, summary.ComplianceStatus);
            Assert.False(summary.HasVerdict);
            Assert.Equal("Not assessed", summary.VerdictWord);
        }

        /// <summary>A failure is certain whatever else went unassessed, so a partial scope never softens it.</summary>
        [Fact]
        public void AFailureOverPartOfTheScope_IsStillAFailure()
        {
            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(Report(pass: 1, fail: 1), 2, null);

            Assert.Equal(PartOTM59Verdict.Fail, summary.Verdict);
            Assert.EndsWith("2 not assessed", summary.Counts);
        }

        /// <summary>
        /// No occupied space assessed: no verdict, and a >28 °C row is never promoted into one - it stays
        /// information only, beside the counts.
        /// </summary>
        [Fact]
        public void NoOccupiedSpaceAssessed_IsNotAssessed_AndA28CRowStaysInformation()
        {
            TM59AssessmentReport tM59AssessmentReport = new(null, null, null, [new TM59CorridorResult("Corridor_1", "test", Guid.NewGuid().ToString(), TM52BuildingCategory.CategoryII, 8760, 87, 400, false, 8760)]);

            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(tM59AssessmentReport, 0, null);

            Assert.Equal(PartOTM59Verdict.NotAssessed, summary.Verdict);
            Assert.Equal("0 spaces assessed · 0 pass · 0 fail · 0 not assessed", summary.Counts);
            Assert.NotNull(summary.Reason);
            Assert.Contains(summary.Facts, x => x.Label == "Other >28 °C checks" && x.Value.Contains("information only"));
        }

        [Fact]
        public void AnUnavailableAssessment_SaysWhy_AndClaimsNoCounts()
        {
            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Unavailable("The results file has changed since the run completed.", null);

            Assert.Equal(PartOTM59Verdict.Unavailable, summary.Verdict);
            Assert.Equal("TM59 assessment — UNAVAILABLE", summary.Heading);
            Assert.Null(summary.Counts);
            Assert.Null(summary.ComplianceStatus);
            Assert.Equal("The results file has changed since the run completed.", summary.Reason);
        }

        /// <summary>An assessment that produced nothing is summarised as unavailable, with its own refusal.</summary>
        [Fact]
        public void AnAssessmentThatProducedNothing_IsUnavailable()
        {
            PartOTM59Assessment partOTM59Assessment = new(null, null, null, null, null, "The simulation results could not be read.");

            PartOTM59ResultSummary summary = PartOTM59ResultSummary.Create(partOTM59Assessment, null);

            Assert.Equal(PartOTM59Verdict.Unavailable, summary.Verdict);
            Assert.Equal("The simulation results could not be read.", summary.Reason);
            Assert.Empty(partOTM59Assessment.SpaceGuids_NoResult);
        }

        // ----- the context ----------------------------------------------------------------------------------

        /// <summary>A run is named as the Hub names it, from its own preparation record, for every scenario.</summary>
        [Fact]
        public void APreparedRun_IsNamedAsTheHubNamesIt()
        {
            foreach (PartOVentilationStrategyOption option in PartOVentilationStrategyOption.Options)
            {
                PartOPreparationContext partOPreparationContext = new(option.PartOIteration, [], [], null);

                Assert.Equal(PartOWorkflowScenario.Find(option, false)?.Text, PartOWorkflowScenario.Find(partOPreparationContext)?.Text);
            }

            Assert.Null(PartOWorkflowScenario.Find((PartOPreparationContext)null!));
        }

        [Fact]
        public void TheThermalModelScope_IsSpelledOnceForBothWindows()
        {
            Assert.Equal("Whole building", Query.PartOThermalModelScopeText(null, out string? detail));
            Assert.Null(detail);
        }

        [Fact]
        public void TheMethod_IsTheCategoryTheResultsState()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(pass: 1, fail: 0);

            List<PartOTM59ResultSummary.Fact> facts = PartOTM59ResultSummary.RunFacts(new PartORun(), tM59AssessmentReport, null, null);

            Assert.Contains(facts, x => x.Label == "Method" && x.Value == "CIBSE TM59:2017 · TM52 Category II");

            //Nothing prepared, nothing simulated: no scenario, route or results file is claimed.
            Assert.DoesNotContain(facts, x => x.Label == "Scenario" || x.Label == "Results" || x.Label == "Weather");
        }

        // ----- the window -----------------------------------------------------------------------------------

        [WpfFact]
        public void TheWindow_OpensOnTheVerdict_WithTheReasonsCollapsed_AndTheReportBelow()
        {
            PartOTM59ResultWindow partOTM59ResultWindow = Window(PartOTM59ResultSummary.Create(Report(pass: 2, fail: 6), 1, null), "REPORT TEXT", ["No overheating scenario covers space 'Corridor_1'."]);

            Assert.Equal("1 reason", partOTM59ResultWindow.DiagnosticsLabel);
            Assert.False(partOTM59ResultWindow.DiagnosticsShown);
            Assert.True(partOTM59ResultWindow.ReportShown);
            Assert.Equal("REPORT TEXT", partOTM59ResultWindow.Report);

            string copyAll = partOTM59ResultWindow.CopyAllText;
            Assert.StartsWith("TM59 assessment — FAIL", copyAll, StringComparison.Ordinal);
            Assert.Contains("8 spaces assessed · 2 pass · 6 fail · 1 not assessed", copyAll);
            Assert.Contains("REPORT TEXT", copyAll);
            Assert.Contains("Corridor_1", copyAll);
            Assert.Contains(PartOTM59ResultSummary.Caveat, copyAll);

            partOTM59ResultWindow.Close();
        }

        /// <summary>Where there is no verdict, the reasons are the explanation, so they open.</summary>
        [WpfFact]
        public void TheReasons_OpenWhereThereIsNoVerdict()
        {
            PartOTM59ResultWindow partOTM59ResultWindow = Window(PartOTM59ResultSummary.Create(Report(pass: 1, fail: 0), 1, "partial"), "REPORT TEXT", ["Space 'Bedroom' could not be resolved."]);

            Assert.True(partOTM59ResultWindow.DiagnosticsShown);
            Assert.True(partOTM59ResultWindow.ReportShown);

            partOTM59ResultWindow.Close();
        }

        /// <summary>No assessment, no report section - and no pass or fail anywhere in the window.</summary>
        [WpfFact]
        public void AnUnavailableResult_DrawsNoReport_AndNoVerdict()
        {
            PartOTM59ResultWindow partOTM59ResultWindow = Window(PartOTM59ResultSummary.Unavailable("The results file is missing.", null), null, []);

            Assert.False(partOTM59ResultWindow.ReportShown);
            Assert.DoesNotContain("PASS", partOTM59ResultWindow.CopyAllText);
            Assert.DoesNotContain("FAIL", partOTM59ResultWindow.CopyAllText);
            Assert.Contains("The results file is missing.", partOTM59ResultWindow.CopyAllText);

            partOTM59ResultWindow.Close();
        }

        // ----- helpers ----------------------------------------------------------------------------------------

        private static PartOTM59ResultWindow Window(PartOTM59ResultSummary summary, string? report, List<string> refusals)
        {
            PartOTM59ResultWindow partOTM59ResultWindow = new()
            {
                Report = report ?? string.Empty,
            };

            partOTM59ResultWindow.SetDiagnostics(refusals, []);
            partOTM59ResultWindow.ResultSummary = summary;

            return partOTM59ResultWindow;
        }

        /// <summary>A report over mechanically ventilated rooms: the first <paramref name="pass"/> pass, the rest fail.</summary>
        private static TM59AssessmentReport Report(int pass, int fail)
        {
            List<TMResult> tMResults = [];

            for (int i = 0; i < pass + fail; i++)
            {
                tMResults.Add(Mechanical(string.Format("Room {0}", i), Guid.NewGuid().ToString(), i < pass));
            }

            return new TM59AssessmentReport(null, tMResults, null, null);
        }

        private static TMResult Mechanical(string name, string reference, bool pass)
        {
            return new TM59MechanicalVentilationResult(name, "test", reference, TM52BuildingCategory.CategoryII, 8760, 262, pass ? 200 : 300, pass);
        }
    }
}
