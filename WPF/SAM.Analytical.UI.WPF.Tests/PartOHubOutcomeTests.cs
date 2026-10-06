// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Part O UX pass 3: the Hub's outcome line - what the most recent action did, or what the run says now.
    /// <para>
    /// Every run here goes through <see cref="PartORun"/>'s own transitions (prepare, complete, restore,
    /// invalidate) and every capability through <c>Modify.Capabilities</c>, the production path; every
    /// verdict through <see cref="PartOTM59ResultSummary"/>, the object the result window is given. The
    /// line is only ever a reader of those.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOHubOutcomeTests
    {
        // ----- what the run says now, with no session record ---------------------------------------------

        [Fact]
        public void APreparedRun_IsPreparedAndWaitingForTAS_NotSimulated()
        {
            PartORun partORun = Prepared(select: true);

            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.HubOutcome(null, partORun, Modify.Capabilities(partORun, out _));

            Assert.NotNull(partOWorkflowOutcome);
            Assert.Equal("○ Iteration 2 prepared — waiting for the full-year TAS run · No simulation has been run for it yet", partOWorkflowOutcome!.Text);
            Assert.Equal(PartOWorkflowOutcomeKind.Information, partOWorkflowOutcome.Kind);

            //Preparation is not a simulation: nothing claims a completion or a verdict.
            Assert.DoesNotContain("completed", partOWorkflowOutcome.Text);
            Assert.DoesNotContain("TM59", partOWorkflowOutcome.Text);
        }

        [Fact]
        public void AFreshRun_SaysNothing()
        {
            PartORun partORun = new();

            Assert.Null(Modify.HubOutcome(null, partORun, Modify.Capabilities(partORun, out _)));
        }

        /// <summary>A completed run whose results have gone: the production gate drops it, and the line says why.</summary>
        [Fact]
        public void ResultsThatHaveGone_AreUnavailable_WithTheRunsOwnReason()
        {
            PartORun partORun = Completed(out string path_TSD);

            File.Delete(path_TSD);

            PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(partORun, out _);

            Assert.False(partOWorkflowCapabilities.ResultsAvailable);

            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.HubOutcome(null, partORun, partOWorkflowCapabilities);

            Assert.NotNull(partOWorkflowOutcome);
            Assert.Equal(PartOWorkflowOutcomeKind.Warning, partOWorkflowOutcome!.Kind);
            Assert.Equal("!", partOWorkflowOutcome.Glyph);
            Assert.Equal("Previous Part O run is no longer valid — no results to review", partOWorkflowOutcome.Headline);
            Assert.Equal(partORun.InvalidationReason, partOWorkflowOutcome.ToolTip);
            Assert.StartsWith("The simulation results this run produced are no longer at", partOWorkflowOutcome.Detail);
        }

        /// <summary>
        /// A reopened run with no resume record is ready to review, and is neither named nor given a verdict:
        /// there is no saved record to name it from, and no assessment has run. (Named from a v2 record:
        /// <c>PartORunResumeNamingTests</c>.)
        /// </summary>
        [Fact]
        public void AReopenedRun_IsReadyToReview_WithNoVerdictAndNoName()
        {
            string path_TSD = Results();

            try
            {
                PartORun partORun = new();

                Assert.True(partORun.Restore(Stamped(path_TSD), null, out string refusal), refusal);

                PartOWorkflowOutcome? partOWorkflowOutcome = Modify.HubOutcome(null, partORun, Modify.Capabilities(partORun, out _));

                Assert.NotNull(partOWorkflowOutcome);
                Assert.Equal("○ Saved results reopened — ready to review · Review Results shows the TM59 verdict · no new simulation is needed", partOWorkflowOutcome!.Text);
                Assert.DoesNotContain("PASS", partOWorkflowOutcome.Text);
                Assert.DoesNotContain("FAIL", partOWorkflowOutcome.Text);
                Assert.DoesNotContain("Iteration", partOWorkflowOutcome.Text);

                //Reviewed: the verdict is now the assessment's, and the run is still not named.
                Assert.Equal("Saved results reviewed — TM59 FAIL", Modify.ReviewOutcome(Summary(PartOTM59Verdict.Fail), partORun)!.Headline);
            }
            finally
            {
                File.Delete(path_TSD);
            }
        }

        // ----- what the last action did -------------------------------------------------------------------

        [Fact]
        public void AReviewCancelled_SaysNoSimulationWasRun()
        {
            PartOWorkflowOutcome partOWorkflowOutcome = Modify.DeclinedOutcome(PartOWorkflowScenario.Text_Iteration1a);

            Assert.Equal("○ Iteration 1a review cancelled — no simulation was run · Cancelled before TAS · the model is unchanged", partOWorkflowOutcome.Text);
            Assert.Equal(PartOWorkflowOutcomeKind.Information, partOWorkflowOutcome.Kind);

            //It claims nothing about the run, so it holds over whatever state the run was already in.
            Assert.Null(partOWorkflowOutcome.RunState);

            PartORun partORun = Prepared(select: false);

            Assert.Same(partOWorkflowOutcome, Modify.HubOutcome(partOWorkflowOutcome, partORun, Modify.Capabilities(partORun, out _)));
        }

        [Theory]
        [InlineData(PartOTM59Verdict.Pass, "✓", PartOWorkflowOutcomeKind.Success)]
        [InlineData(PartOTM59Verdict.Fail, "✕", PartOWorkflowOutcomeKind.Fail)]
        [InlineData(PartOTM59Verdict.NotAssessed, "–", PartOWorkflowOutcomeKind.Warning)]
        [InlineData(PartOTM59Verdict.Unavailable, "!", PartOWorkflowOutcomeKind.Warning)]
        public void ACompletedRun_SaysItsTM59Verdict_FromTheWindowsOwnSummary(PartOTM59Verdict partOTM59Verdict, string glyph, PartOWorkflowOutcomeKind partOWorkflowOutcomeKind)
        {
            PartOTM59ResultSummary summary = Summary(partOTM59Verdict);

            PartOWorkflowOutcome partOWorkflowOutcome = Modify.CompletedOutcome(PartOWorkflowScenario.Text_Iteration1a, TimeSpan.FromSeconds(53), summary, 0);

            //No contradiction with the authority: the glyph and word are the summary's.
            Assert.Equal(summary.Glyph, partOWorkflowOutcome.Glyph);
            Assert.Equal(glyph, partOWorkflowOutcome.Glyph);
            Assert.Equal(string.Format("Iteration 1a completed — TM59 {0}", summary.VerdictText), partOWorkflowOutcome.Headline);
            Assert.Equal(partOWorkflowOutcomeKind, partOWorkflowOutcome.Kind);
            Assert.StartsWith("TAS simulation 53s", partOWorkflowOutcome.Detail);

            if (summary.Counts is not null)
            {
                Assert.EndsWith(summary.Counts, partOWorkflowOutcome.Detail);
            }

            //A state with no verdict carries its reason behind Show details.
            Assert.Equal(summary.Reason, partOWorkflowOutcome.ToolTip);
            Assert.Equal(PartORunState.WorkflowCompleted, partOWorkflowOutcome.RunState);
        }

        [Fact]
        public void ACompletedFail_ReadsAsTheSpecExample()
        {
            PartOWorkflowOutcome partOWorkflowOutcome = Modify.CompletedOutcome(PartOWorkflowScenario.Text_Iteration1a, TimeSpan.FromSeconds(53), Summary(PartOTM59Verdict.Fail), 2);

            Assert.Equal("✕ Iteration 1a completed — TM59 FAIL · TAS simulation 53s · 2 spaces assessed · 1 pass · 1 fail · 0 not assessed · 2 notes were shown", partOWorkflowOutcome.Text);
        }

        [Fact]
        public void AReview_SaysTheVerdict_AndThatNoSimulationWasRun()
        {
            PartORun partORun = Completed(out string path_TSD);

            try
            {
                PartOWorkflowOutcome? partOWorkflowOutcome = Modify.ReviewOutcome(Summary(PartOTM59Verdict.Pass), partORun);

                Assert.Equal("✓ Iteration 1a results reviewed — TM59 PASS · 2 spaces assessed · 2 pass · 0 fail · 0 not assessed · no simulation was run", partOWorkflowOutcome!.Text);
                Assert.Equal(PartOWorkflowOutcomeKind.Success, partOWorkflowOutcome.Kind);
            }
            finally
            {
                File.Delete(path_TSD);
            }
        }

        [Fact]
        public void ATASRunCancelled_KeepsThePreparation_AndHoldsOnlyWhileItIsKept()
        {
            PartOWorkflowOutcome partOWorkflowOutcome = Modify.SimulationCancelledOutcome(PartOWorkflowScenario.Text_Iteration2);

            Assert.Equal("○ Iteration 2 TAS run cancelled — no results were produced · The prepared iteration is kept, so it can be run again", partOWorkflowOutcome.Text);

            PartORun partORun = Prepared(select: true);

            Assert.Same(partOWorkflowOutcome, Modify.HubOutcome(partOWorkflowOutcome, partORun, Modify.Capabilities(partORun, out _)));

            //The model was edited since: the preparation is gone, and the line no longer says it was kept.
            partORun.Invalidate("The model was modified after the iteration was prepared.");

            PartOWorkflowOutcome? partOWorkflowOutcome_Shown = Modify.HubOutcome(partOWorkflowOutcome, partORun, Modify.Capabilities(partORun, out _));

            Assert.NotNull(partOWorkflowOutcome_Shown);
            Assert.Equal("Previous Part O run is no longer valid — no results to review", partOWorkflowOutcome_Shown!.Headline);
            Assert.Contains("Earlier in this session: " + partOWorkflowOutcome.Text, partOWorkflowOutcome_Shown.ToolTip);
        }

        /// <summary>A completed-with-results line is never shown over results that are gone.</summary>
        [Fact]
        public void ACompletedLine_IsSuperseded_WhenItsResultsAreGone()
        {
            PartORun partORun = Completed(out string path_TSD);

            PartOWorkflowOutcome partOWorkflowOutcome = Modify.CompletedOutcome(PartOWorkflowScenario.Text_Iteration1a, TimeSpan.FromSeconds(53), Summary(PartOTM59Verdict.Fail), 0);

            Assert.Same(partOWorkflowOutcome, Modify.HubOutcome(partOWorkflowOutcome, partORun, Modify.Capabilities(partORun, out _)));

            File.Delete(path_TSD);

            PartOWorkflowOutcome? partOWorkflowOutcome_Shown = Modify.HubOutcome(partOWorkflowOutcome, partORun, Modify.Capabilities(partORun, out _));

            Assert.NotNull(partOWorkflowOutcome_Shown);
            Assert.Equal("!", partOWorkflowOutcome_Shown!.Glyph);
            Assert.DoesNotContain("FAIL", partOWorkflowOutcome_Shown.Headline);
        }

        [Fact]
        public void ANotCompletedRun_SaysSo_WithTheReasonBehindDetails()
        {
            PartOWorkflowOutcome partOWorkflowOutcome = Modify.NotCompletedOutcome(PartOWorkflowScenario.Text_Iteration1a, "SAM Check found 2 errors. Fix them and run again.");

            Assert.Equal("! Iteration 1a not completed — no TM59 results · SAM Check found 2 errors.", partOWorkflowOutcome.Text);
            Assert.Equal("SAM Check found 2 errors. Fix them and run again.", partOWorkflowOutcome.ToolTip);
            Assert.Equal(PartOWorkflowOutcomeKind.Warning, partOWorkflowOutcome.Kind);
        }

        // ----- Iteration 2B, from its own stop reason ------------------------------------------------------

        [Fact]
        public void Iteration2B_Passed_IsCompletedPass()
        {
            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.OptimisationOutcome(Optimisation(PartOOptimisationStopReason.Passed, TM59ComplianceStatus.Pass));

            Assert.Equal("✓ Iteration 2B optimisation completed — TM59 PASS · 1 round · last valid design: run 1", partOWorkflowOutcome!.Text);
            Assert.Equal(PartOWorkflowOutcomeKind.Success, partOWorkflowOutcome.Kind);
            Assert.NotNull(partOWorkflowOutcome.ToolTip);
        }

        [Fact]
        public void Iteration2B_AtCapacity_WithTheLastValidDesignFailing_IsCompletedFail()
        {
            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.OptimisationOutcome(Optimisation(PartOOptimisationStopReason.CapacityReached, TM59ComplianceStatus.Fail));

            Assert.Equal("✕ Iteration 2B optimisation completed — TM59 FAIL · Stopped: capacity reached · 1 round · last valid design: run 1", partOWorkflowOutcome!.Text);
            Assert.Equal(PartOWorkflowOutcomeKind.Fail, partOWorkflowOutcome.Kind);
        }

        /// <summary>No verdict the run does not state: a stop whose last valid design has no Fail status says only that it stopped.</summary>
        [Theory]
        [InlineData(PartOOptimisationStopReason.SimulationFailed, "simulation failed")]
        [InlineData(PartOOptimisationStopReason.CapacityReached, "capacity reached")]
        public void Iteration2B_StoppedWithoutAStatedFailure_SaysOnlyThatItStopped(PartOOptimisationStopReason partOOptimisationStopReason, string word)
        {
            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.OptimisationOutcome(Optimisation(partOOptimisationStopReason, TM59ComplianceStatus.Undefined));

            Assert.Equal("Iteration 2B optimisation stopped — " + word, partOWorkflowOutcome!.Headline);
            Assert.Equal("!", partOWorkflowOutcome.Glyph);
            Assert.DoesNotContain("TM59", partOWorkflowOutcome.Headline);
        }

        [Fact]
        public void Iteration2B_Cancelled_AndRefused()
        {
            Assert.Equal("○ Iteration 2B optimisation cancelled · 1 round · last valid design: run 1", Modify.OptimisationOutcome(Optimisation(PartOOptimisationStopReason.Cancelled, TM59ComplianceStatus.Fail))!.Text);

            //Refused before starting: the refusal was shown, and the Hub says what the run says.
            Assert.Null(Modify.OptimisationOutcome(null));
        }

        // ----- the window -------------------------------------------------------------------------------

        /// <summary>
        /// Reopening the Hub does not manufacture a transient state: the session record is the caller's, and a
        /// new showing without it says only what the run says now.
        /// </summary>
        [WpfFact]
        public void ReopeningTheHub_DoesNotBringBackACancellation()
        {
            PartORun partORun = Prepared(select: false);

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                PartORun = partORun,
                Capabilities = Modify.Capabilities(partORun, out _),
                LastOutcome = Modify.DeclinedOutcome(PartOWorkflowScenario.Text_Iteration1a),
            };

            Assert.Contains("review cancelled", partOWorkflowWindow.LastOutcomeText);

            partOWorkflowWindow.Close();

            //What RunPartOWorkflow does on a new command: no record carried in.
            PartOWorkflowWindow partOWorkflowWindow_Reopened = new()
            {
                PartORun = partORun,
                Capabilities = Modify.Capabilities(partORun, out _),
            };

            Assert.Equal("○ Iteration 1a prepared — waiting for the full-year TAS run · No simulation has been run for it yet", partOWorkflowWindow_Reopened.LastOutcomeText);
            Assert.DoesNotContain("cancelled", partOWorkflowWindow_Reopened.LastOutcomeText);

            partOWorkflowWindow_Reopened.Close();

            PartOWorkflowWindow partOWorkflowWindow_Fresh = new()
            {
                PartORun = new PartORun(),
                Capabilities = new PartOWorkflowCapabilities(),
            };

            Assert.Equal(string.Empty, partOWorkflowWindow_Fresh.LastOutcomeText);

            partOWorkflowWindow_Fresh.Close();
        }

        [WpfFact]
        public void ShowDetails_OpensTheFullExplanationUnderTheLine()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                LastOutcome = Modify.NotCompletedOutcome(PartOWorkflowScenario.Text_Iteration1a, "SAM Check found 2 errors. Fix them and run again."),
            };

            Assert.StartsWith("! Iteration 1a not completed", partOWorkflowWindow.LastOutcomeText);
            Assert.False(partOWorkflowWindow.LastOutcomeDetailsShown);

            partOWorkflowWindow.ShowStatusDetails = true;

            Assert.True(partOWorkflowWindow.LastOutcomeDetailsShown);

            partOWorkflowWindow.ShowStatusDetails = false;

            Assert.False(partOWorkflowWindow.LastOutcomeDetailsShown);

            partOWorkflowWindow.Close();
        }

        // ----- fixtures ---------------------------------------------------------------------------------

        private static PartORun Prepared(bool select)
        {
            PartORun result = new();

            Assert.True(result.Prepare(Model("prepared"), Scenarios(), Context(select)));

            return result;
        }

        /// <summary>The production sequence to WorkflowCompleted: prepare, announce the results, write them, complete.</summary>
        private static PartORun Completed(out string path_TSD)
        {
            PartORun result = new();

            Assert.True(result.Prepare(Model("prepared"), Scenarios(), Context(false)));

            path_TSD = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOHubOutcomeTests_{0}.tsd", Guid.NewGuid()));

            Assert.True(result.ExpectResults(path_TSD));

            File.WriteAllText(path_TSD, string.Format("results - {0}", Guid.NewGuid()));

            Assert.True(result.Complete(Model("workflow"), path_TSD, new PartOSimulationContext(Path.GetTempPath(), "Fixture", null, SolarCalculationMethod.SAM, 1, 365), out string refusal), refusal);

            return result;
        }

        private static PartOPreparationContext Context(bool select)
        {
            return new PartOPreparationContext(
                PartOIteration.BasePassive,
                [new Zone("Flat 1")],
                [],
                select ? [new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test", "Model", "TEST-1"), 150, 150, 10)] : null);
        }

        private static string Results()
        {
            string result = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOHubOutcomeTests_{0}.tsd", Guid.NewGuid()));

            File.WriteAllText(result, string.Format("results - {0}", Guid.NewGuid()));

            return result;
        }

        private static AnalyticalModel Stamped(string path_TSD)
        {
            AnalyticalModel result = Model("run");

            result.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>(Scenarios()));
            result.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(result, path_TSD));

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

        private static PartOOptimisationRun Optimisation(PartOOptimisationStopReason partOOptimisationStopReason, TM59ComplianceStatus tM59ComplianceStatus_LastValid)
        {
            PartOOptimisationRun result = new(new PartOOptimisationSettings())
            {
                StopReason = partOOptimisationStopReason,
            };

            result.Steps.Add(new PartOOptimisationStep(0) { IsCompleted = true, OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Fail });
            result.Steps.Add(new PartOOptimisationStep(1) { IsCompleted = true, OccupiedSpaceComplianceStatus = tM59ComplianceStatus_LastValid });

            return result;
        }

        private static PartOTM59ResultSummary Summary(PartOTM59Verdict partOTM59Verdict)
        {
            return partOTM59Verdict switch
            {
                PartOTM59Verdict.Pass => PartOTM59ResultSummary.Create(Report(pass: 2, fail: 0), 0, null),
                PartOTM59Verdict.Fail => PartOTM59ResultSummary.Create(Report(pass: 1, fail: 1), 0, null),
                PartOTM59Verdict.NotAssessed => PartOTM59ResultSummary.Create(Report(pass: 2, fail: 0), 1, "Only part of the dwelling scope was assessed."),
                _ => PartOTM59ResultSummary.Unavailable("No results.", null),
            };
        }

        private static TM59AssessmentReport Report(int pass, int fail)
        {
            List<TMResult> tMResults = [];

            for (int i = 0; i < pass + fail; i++)
            {
                tMResults.Add(new TM59MechanicalVentilationResult(string.Format("Room {0}", i), "test", Guid.NewGuid().ToString(), TM52BuildingCategory.CategoryII, 8760, 262, i < pass ? 200 : 300, i < pass));
            }

            return new TM59AssessmentReport(null, tMResults, null, null);
        }
    }
}
