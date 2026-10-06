// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Part O UX pass 5 - the Iteration 2B journey.</b>
    /// <list type="bullet">
    /// <item>Entry: a completed Iteration 2 run is an Iteration 2B starting point on <c>Modify.CanOptimise</c>'s
    /// answer alone, whether or not it was prepared with optimisation settings. The settings are confirmed when
    /// 2B starts (<see cref="PartOOptimisationStart"/>), pre-filled, and validated by
    /// <c>PartOOptimisationSettings.IsValid</c>.</item>
    /// <item>Result: <see cref="PartOOptimisationSummary"/> - one wording per stop reason, PASS only on a
    /// Passed stop, counts that only count, the capacity envelope stated apart, and the detail collapsed.</item>
    /// </list>
    /// No TAS COM type is touched and no optimisation is run; the histories are built directly.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOIteration2BJourneyTests
    {
        private static readonly Guid guid_Bedroom = new("7b0e3c3e-0000-4000-8000-000000000001");

        private static readonly Guid guid_Kitchen = new("7b0e3c3e-0000-4000-8000-000000000002");

        private static readonly Guid guid_Living = new("7b0e3c3e-0000-4000-8000-000000000003");

        // ---- Entry -----------------------------------------------------------------------------------

        /// <summary>
        /// The prerequisite Pass 5 removes: a completed Iteration 2 run prepared WITHOUT optimisation settings
        /// is now offered 2B, on <c>CanOptimise</c>'s answer. Before, it was refused with "Prepare and run the
        /// iteration again with the follow-on optimisation ticked" - a full-year TAS run to change two numbers
        /// the preparation never reads.
        /// </summary>
        [Fact]
        public void ACompletedIteration2RunPreparedWithoutSettings_IsOfferedIteration2B()
        {
            PartORun partORun = Completed(Context(select: true, settings: null), out string _);

            Assert.Null(partORun.PreparationContext.OptimisationSettings);

            PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(partORun, out _);

            Assert.True(partOWorkflowCapabilities.OptimisationAvailable, partOWorkflowCapabilities.OptimisationRefusal);
            Assert.Null(partOWorkflowCapabilities.OptimisationRefusal);
        }

        /// <summary>
        /// Eligibility itself is unchanged: an Iteration 1a run (no selected unit) is still refused, in
        /// <c>CanOptimise</c>'s own words - nothing is widened in SAM_UI.
        /// </summary>
        [Fact]
        public void EligibilityIsStillCanOptimisesAnswer()
        {
            PartORun partORun = Completed(Context(select: false, settings: null), out string _);

            PartOWorkflowCapabilities partOWorkflowCapabilities = Modify.Capabilities(partORun, out _);

            Assert.False(partOWorkflowCapabilities.OptimisationAvailable);

            Assert.False(Modify.CanOptimise(partORun, null, out string refusal));
            Assert.Equal(refusal, partOWorkflowCapabilities.OptimisationRefusal);
        }

        /// <summary>
        /// Cancelling the confirmation runs nothing and changes nothing: no optimisation, no confirmed settings,
        /// the run still where it was and the model untouched.
        /// </summary>
        [Fact]
        public void CancellingTheConfirmation_RunsNothing()
        {
            PartORun partORun = Completed(Context(select: true, settings: null), out string path_TSD);

            AnalyticalModel analyticalModel = Model("loaded");
            UIAnalyticalModel uIAnalyticalModel = new(analyticalModel);

            int modified = 0;
            uIAnalyticalModel.Modified += (s, e) => modified++;

            PartOOptimisationStart? shown = null;

            PartOOptimisationRun? partOOptimisationRun = Modify.RunPartOOptimisationResult(uIAnalyticalModel, partORun, null, null, out PartOOptimisationSettings? partOOptimisationSettings_Confirmed, x =>
            {
                shown = x;

                return null;
            });

            Assert.NotNull(shown);
            Assert.Null(partOOptimisationRun);
            Assert.Null(partOOptimisationSettings_Confirmed);

            Assert.Equal(PartORunState.WorkflowCompleted, partORun.State);
            Assert.Equal(path_TSD, partORun.Path_TSD);
            Assert.Equal(0, modified);
        }

        /// <summary>
        /// Pre-fill, in order: the settings recorded with the run's own preparation, else the ones last
        /// confirmed in this session, else the defaults - and the confirmation says which. Always a copy, so
        /// editing it never writes into the run's record.
        /// </summary>
        [Fact]
        public void TheConfirmation_PreFillsFromTheRecordThenTheSessionThenTheDefaults()
        {
            PartOOptimisationSettings recorded = new() { AirFlowStep_Lps = 7, MaximumIterations = 3, CapacityEnvelope = false, WarmStart = false };
            PartOOptimisationSettings session = new() { AirFlowStep_Lps = 2.5, MaximumIterations = 6 };

            PartORun partORun_Recorded = Completed(Context(select: true, settings: recorded), out string _);

            PartOOptimisationStart start_Recorded = PartOOptimisationStart.Create(partORun_Recorded, session);

            Assert.Equal(7, start_Recorded.Settings.AirFlowStep_Lps);
            Assert.Equal(3, start_Recorded.Settings.MaximumIterations);
            Assert.False(start_Recorded.Settings.CapacityEnvelope);
            Assert.Contains("recorded when this run was prepared", start_Recorded.SettingsSource, StringComparison.Ordinal);
            Assert.NotSame(recorded, start_Recorded.Settings);

            start_Recorded.Settings.AirFlowStep_Lps = 99;
            Assert.Equal(7, partORun_Recorded.PreparationContext.OptimisationSettings.AirFlowStep_Lps);

            PartORun partORun_None = Completed(Context(select: true, settings: null), out string _);

            PartOOptimisationStart start_Session = PartOOptimisationStart.Create(partORun_None, session);

            Assert.Equal(2.5, start_Session.Settings.AirFlowStep_Lps);
            Assert.Equal(6, start_Session.Settings.MaximumIterations);
            Assert.Contains("last used in this session", start_Session.SettingsSource, StringComparison.Ordinal);

            PartOOptimisationStart start_Default = PartOOptimisationStart.Create(partORun_None, null);

            Assert.Equal(PartOOptimisationSettings.DefaultAirFlowStep_Lps, start_Default.Settings.AirFlowStep_Lps);
            Assert.Equal(PartOOptimisationSettings.DefaultMaximumIterations, start_Default.Settings.MaximumIterations);
            Assert.Contains("default settings", start_Default.SettingsSource, StringComparison.Ordinal);
        }

        /// <summary>
        /// The confirmation names the run it starts from off the run's own record, and says what 2B may and
        /// may not change. A design an earlier 2B left behind is named as such, from the same results-file
        /// convention the optimiser continues its numbering by.
        /// </summary>
        [Fact]
        public void TheConfirmation_StatesWhatItStartsFromAndWhatItMayChange()
        {
            PartORun partORun = Completed(Context(select: true, settings: null), out string path_TSD);

            PartOOptimisationStart partOOptimisationStart = PartOOptimisationStart.Create(partORun, null);

            PartOOptimisationSummary.Fact fact_Start = partOOptimisationStart.Facts.Single(x => x.Label == "Starting from");
            Assert.Contains("completed Iteration 2 run", fact_Start.Value, StringComparison.Ordinal);

            PartOOptimisationSummary.Fact fact_Results = partOOptimisationStart.Facts.Single(x => x.Label == "Results");
            Assert.Equal(Path.GetFileName(path_TSD), fact_Results.Value);
            Assert.Equal(path_TSD, fact_Results.Detail);

            Assert.Contains("1 dwelling in scope", partOOptimisationStart.Facts.Single(x => x.Label == "Dwellings").Value, StringComparison.Ordinal);

            Assert.Contains(PartOOptimisationStart.Keeps, x => x.Contains("selected ventilation unit", StringComparison.Ordinal));
            Assert.Contains(PartOOptimisationStart.Keeps, x => x.Contains("Approved Document F", StringComparison.Ordinal));
            Assert.Contains(PartOOptimisationStart.Changes, x => x.Contains("fail TM59", StringComparison.Ordinal));

            //A design kept by an earlier optimisation: its results file carries the round.
            PartORun partORun_Opt = Completed(Context(select: true, settings: null), out string _, "Fixture-Opt03");

            PartOOptimisationStart partOOptimisationStart_Opt = PartOOptimisationStart.Create(partORun_Opt, null);

            //Only the result files continue the numbering - this run's rounds count from 1.
            PartOOptimisationSummary.Fact fact_Opt = partOOptimisationStart_Opt.Facts.Single(x => x.Label == "Starting from");
            Assert.Contains("earlier Iteration 2B run, saved as round 3 (-Opt03)", fact_Opt.Value, StringComparison.Ordinal);
            Assert.DoesNotContain("numbering continues", fact_Opt.Value, StringComparison.Ordinal);
            Assert.Contains("numbers its own rounds from 1", fact_Opt.Detail, StringComparison.Ordinal);
            Assert.Contains("saved as -Opt04", fact_Opt.Detail, StringComparison.Ordinal);
        }

        /// <summary>
        /// The settings rule is <c>PartOOptimisationSettings.IsValid</c>'s: only the parsing is the
        /// confirmation's. Unparseable text and values IsValid refuses are both refused, in words.
        /// </summary>
        [Fact]
        public void TheConfirmation_ValidatesWithTheSettingsAuthority()
        {
            PartOOptimisationStart partOOptimisationStart = PartOOptimisationStart.Create(Completed(Context(select: true, settings: null), out string _), null);

            Assert.Null(partOOptimisationStart.Parse("abc", "10", true, true, out string? refusal_Step));
            Assert.Contains("'abc' is not an airflow step", refusal_Step, StringComparison.Ordinal);

            Assert.Null(partOOptimisationStart.Parse("5", "many", true, true, out string? refusal_Limit));
            Assert.Contains("'many' is not a number of rounds", refusal_Limit, StringComparison.Ordinal);

            Assert.Null(partOOptimisationStart.Parse("0", "10", true, true, out string? refusal_Zero));
            Assert.False(new PartOOptimisationSettings { AirFlowStep_Lps = 0 }.IsValid(out string refusal_Authority));
            Assert.Equal(refusal_Authority, refusal_Zero);

            PartOOptimisationSettings? partOOptimisationSettings = partOOptimisationStart.Parse("2.5", "4", false, true, out string? refusal_None);

            Assert.Null(refusal_None);
            Assert.NotNull(partOOptimisationSettings);
            Assert.Equal(2.5, partOOptimisationSettings.AirFlowStep_Lps);
            Assert.Equal(4, partOOptimisationSettings.MaximumIterations);
            Assert.False(partOOptimisationSettings.CapacityEnvelope);
            Assert.True(partOOptimisationSettings.WarmStart);
        }

        /// <summary>The window: Start is offered only while the typed settings are usable, and says why not.</summary>
        [WpfFact]
        public void TheStartWindow_OffersStartOnlyForUsableSettings()
        {
            PartOOptimisationStartWindow partOOptimisationStartWindow = new()
            {
                Start = PartOOptimisationStart.Create(Completed(Context(select: true, settings: null), out string _), null),
            };

            Assert.True(partOOptimisationStartWindow.CanStart);
            Assert.Null(partOOptimisationStartWindow.Refusal);

            partOOptimisationStartWindow.AirFlowStepText = "abc";

            Assert.False(partOOptimisationStartWindow.CanStart);
            Assert.Contains("'abc' is not an airflow step", partOOptimisationStartWindow.Refusal, StringComparison.Ordinal);
            Assert.Equal(Visibility.Visible, partOOptimisationStartWindow.textBlock_Refusal.Visibility);

            partOOptimisationStartWindow.AirFlowStepText = "3";
            partOOptimisationStartWindow.MaximumIterationsText = "2";

            Assert.True(partOOptimisationStartWindow.CanStart);
            Assert.Equal(3, partOOptimisationStartWindow.Settings.AirFlowStep_Lps);
            Assert.Equal(2, partOOptimisationStartWindow.Settings.MaximumIterations);

            //Nothing is confirmed until Start is pressed.
            Assert.Null(partOOptimisationStartWindow.ConfirmedSettings);
        }

        // ---- Stop reason and verdict -----------------------------------------------------------------

        /// <summary>
        /// Every real stop reason has its own headline - materially different outcomes are never folded into
        /// one "completed" - and every one says what to do next.
        /// </summary>
        [Fact]
        public void EveryStopReason_HasItsOwnHeadlineAndANextStep()
        {
            List<string> headlines = [];

            foreach (PartOOptimisationStopReason partOOptimisationStopReason in Enum.GetValues(typeof(PartOOptimisationStopReason)))
            {
                if (partOOptimisationStopReason == PartOOptimisationStopReason.Running)
                {
                    continue;
                }

                PartOOptimisationRun partOOptimisationRun = History(rounds: 2);
                partOOptimisationRun.StopReason = partOOptimisationStopReason;

                PartOOptimisationSummary partOOptimisationSummary = PartOOptimisationSummary.Create(partOOptimisationRun);

                Assert.False(string.IsNullOrWhiteSpace(partOOptimisationSummary.StopHeadline));
                Assert.False(string.IsNullOrWhiteSpace(partOOptimisationSummary.NextStep));
                Assert.DoesNotContain("Completed", partOOptimisationSummary.StopHeadline, StringComparison.Ordinal);

                headlines.Add(partOOptimisationSummary.StopHeadline);
            }

            Assert.Equal(headlines.Count, headlines.Distinct().Count());
        }

        /// <summary>
        /// A baseline that already passes ran no round, and says so rather than "target reached".
        /// </summary>
        [Fact]
        public void APassingBaseline_SaysNoOptimisationWasNeeded()
        {
            PartOOptimisationRun partOOptimisationRun = History(rounds: 0, lastStatus: TM59ComplianceStatus.Pass);
            partOOptimisationRun.StopReason = PartOOptimisationStopReason.Passed;

            PartOOptimisationSummary partOOptimisationSummary = PartOOptimisationSummary.Create(partOOptimisationRun);

            Assert.Equal(PartOTM59Verdict.Pass, partOOptimisationSummary.Verdict);
            Assert.StartsWith("No optimisation needed", partOOptimisationSummary.StopHeadline, StringComparison.Ordinal);
            Assert.Equal("The starting design (run 0) — no round changed it", partOOptimisationSummary.Facts.Single(x => x.Label == "Kept design").Value);
            Assert.Equal("None", partOOptimisationSummary.Facts.Single(x => x.Label == "Design airflow changed").Value);
        }

        /// <summary>
        /// PASS only on a Passed stop - the optimiser's authority, which already refused a pass over part of
        /// the scope. A kept design whose assessment passed but whose run stopped on AssessmentFailed (the
        /// partial-scope refusal) is NOT ASSESSED, never PASS; a kept design that failed is FAIL; no kept design
        /// is UNAVAILABLE.
        /// </summary>
        [Fact]
        public void TheVerdict_IsPassOnlyOnAPassedStop()
        {
            PartOOptimisationRun partOOptimisationRun_Capacity = History(rounds: 2);
            partOOptimisationRun_Capacity.StopReason = PartOOptimisationStopReason.CapacityReached;
            Assert.Equal(PartOTM59Verdict.Fail, PartOOptimisationSummary.Create(partOOptimisationRun_Capacity).Verdict);

            PartOOptimisationRun partOOptimisationRun_Partial = History(rounds: 1, lastStatus: TM59ComplianceStatus.Pass);
            partOOptimisationRun_Partial.StopReason = PartOOptimisationStopReason.AssessmentFailed;
            Assert.Equal(PartOTM59Verdict.NotAssessed, PartOOptimisationSummary.Create(partOOptimisationRun_Partial).Verdict);

            PartOOptimisationRun partOOptimisationRun_None = new(new PartOOptimisationSettings())
            {
                StopReason = PartOOptimisationStopReason.AssessmentFailed,
            };
            partOOptimisationRun_None.Steps.Add(new PartOOptimisationStep(0));
            Assert.Equal(PartOTM59Verdict.Unavailable, PartOOptimisationSummary.Create(partOOptimisationRun_None).Verdict);
            Assert.Equal("TM59 UNAVAILABLE", PartOOptimisationSummary.Create(partOOptimisationRun_None).VerdictText);
        }

        /// <summary>
        /// A Cancel request that did not become the stop is explained - the run stopped for its own reason
        /// first - and never changes the stop reason. A real cancellation, and no request, say nothing extra.
        /// </summary>
        [Fact]
        public void ACancelRequestThatWasNotTheStop_IsExplainedNotHidden()
        {
            PartOOptimisationRun partOOptimisationRun = History(rounds: 2);
            partOOptimisationRun.StopReason = PartOOptimisationStopReason.SimulationFailed;

            PartOOptimisationSummary partOOptimisationSummary = PartOOptimisationSummary.Create(partOOptimisationRun, cancelRequested: true);

            Assert.NotNull(partOOptimisationSummary.CancelNote);
            Assert.StartsWith("Stopped: a round's TAS simulation did not complete", partOOptimisationSummary.StopHeadline, StringComparison.Ordinal);
            Assert.Equal(PartOOptimisationStopReason.SimulationFailed, partOOptimisationRun.StopReason);

            Assert.Null(PartOOptimisationSummary.Create(partOOptimisationRun, cancelRequested: false).CancelNote);

            partOOptimisationRun.StopReason = PartOOptimisationStopReason.Cancelled;
            Assert.Null(PartOOptimisationSummary.Create(partOOptimisationRun, cancelRequested: true).CancelNote);
        }

        /// <summary>
        /// The next step never directs the engineer to an action that is not available. The
        /// optimiser drops the run on a cancelled or failed round, so "2B again continues from the kept design"
        /// is said only where the caller found the run still holding it; otherwise the step says a new
        /// completed Iteration 2 run is needed first.
        /// </summary>
        [Fact]
        public void TheNextStep_OffersContinuingOnlyWhereTheRunStillHoldsTheKeptDesign()
        {
            PartOOptimisationRun partOOptimisationRun = History(rounds: 2);

            foreach (PartOOptimisationStopReason partOOptimisationStopReason in new[] { PartOOptimisationStopReason.Cancelled, PartOOptimisationStopReason.SimulationFailed, PartOOptimisationStopReason.IterationLimitReached })
            {
                partOOptimisationRun.StopReason = partOOptimisationStopReason;

                string next_Dropped = PartOOptimisationSummary.Create(partOOptimisationRun, canContinue: false).NextStep;
                Assert.Contains("cannot continue from this stop", next_Dropped, StringComparison.Ordinal);
                Assert.Contains("Prepare & Run", next_Dropped, StringComparison.Ordinal);
                Assert.DoesNotContain("continues from the kept design", next_Dropped, StringComparison.Ordinal);

                string next_Held = PartOOptimisationSummary.Create(partOOptimisationRun, canContinue: true).NextStep;
                Assert.Contains("continues from the kept design", next_Held, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Settings confirmed on any route - the Results ribbon included, which carries no
        /// session state of its own - pre-fill the next confirmation in the same application session.
        /// </summary>
        [Fact]
        public void SettingsConfirmedThisSession_PreFillTheNextConfirmationOnEveryRoute()
        {
            PartOOptimisationSettings? before = Modify.partOOptimisationSettings_LastConfirmed;

            try
            {
                Modify.partOOptimisationSettings_LastConfirmed = new PartOOptimisationSettings { AirFlowStep_Lps = 4, MaximumIterations = 7 };

                PartORun partORun = Completed(Context(select: true, settings: null), out string _);

                PartOOptimisationStart? shown = null;

                //The ribbon's call: no session settings of its own.
                Modify.RunPartOOptimisationResult(new UIAnalyticalModel(Model("loaded")), partORun, null, null, out PartOOptimisationSettings? _, x =>
                {
                    shown = x;

                    return null;
                });

                Assert.NotNull(shown);
                Assert.Equal(4, shown.Settings.AirFlowStep_Lps);
                Assert.Equal(7, shown.Settings.MaximumIterations);
                Assert.Contains("last used in this session", shown.SettingsSource, StringComparison.Ordinal);
            }
            finally
            {
                Modify.partOOptimisationSettings_LastConfirmed = before;
            }
        }

        /// <summary>
        /// Live acceptance, 26 Sep: a reopened Iteration 2 run with the scenario box back on its first
        /// scenario said "Iteration 2 only" beside Optimise. Where a run with results exists, the reason is that
        /// run's - <c>CanOptimise</c>'s refusal - and a reopened run's caption says it needs a live run.
        /// </summary>
        [WpfFact]
        public void AReopenedRun_SaysWhy2BIsUnavailable_InTheAuthoritysWords_WhateverTheScenarioBox()
        {
            const string refusal = "This Part O run was reopened from a saved model, so its results can be reviewed but not continued.";

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                AnalyticalModel = Model("reopened"),
                PartORun = new PartORun(),
                Capabilities = new PartOWorkflowCapabilities { EquipmentAvailable = true, ResultsAvailable = true, ResultsRestored = true, OptimisationAvailable = false, OptimisationRefusal = refusal },
            };

            partOWorkflowWindow.Restore(PartOWorkflowScenario.Scenarios.Find(x => !x.SupportsOptimisation), PartOWorkflowScope.AllDwellings, null);
            partOWorkflowWindow.CompleteInitialisation();

            Assert.False(partOWorkflowWindow.CanOptimise);
            Assert.Equal(refusal, partOWorkflowWindow.OptimiseToolTip);
            Assert.Equal("Needs a live run", partOWorkflowWindow.OptimiseCaption);

            partOWorkflowWindow.Close();
        }

        /// <summary>The Hub line says "round limit", the word every 2B window uses for the same setting.</summary>
        [Fact]
        public void TheHubLine_SaysRoundLimit_LikeTheResultWindow()
        {
            PartOOptimisationRun partOOptimisationRun = History(rounds: 2);
            partOOptimisationRun.StopReason = PartOOptimisationStopReason.IterationLimitReached;

            PartOWorkflowOutcome? partOWorkflowOutcome = Modify.OptimisationOutcome(partOOptimisationRun);

            Assert.NotNull(partOWorkflowOutcome);
            Assert.Contains("Stopped: round limit reached", partOWorkflowOutcome.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("iteration limit", partOWorkflowOutcome.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ---- What changed ----------------------------------------------------------------------------

        /// <summary>
        /// The counts only count the run's record: distinct design spaces among a step's Fail rows, and the
        /// spaces the COMPLETED rounds adjusted - targeted apart from balancing. A round that did not complete
        /// is not part of the kept design and is not counted; the rounds line says one did not complete.
        /// </summary>
        [Fact]
        public void TheSummary_CountsTheRecordAndOnlyTheCompletedRounds()
        {
            PartOOptimisationRun partOOptimisationRun = History(rounds: 2);

            //A third round that was attempted and failed - its adjustments are not in the kept design.
            PartOOptimisationStep partOOptimisationStep_Failed = new(3);
            partOOptimisationStep_Failed.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Living, "Living", FlowClassification.Supply, 30, 35, 13, false));
            partOOptimisationRun.Steps.Add(partOOptimisationStep_Failed);
            partOOptimisationRun.StopReason = PartOOptimisationStopReason.SimulationFailed;

            PartOOptimisationSummary partOOptimisationSummary = PartOOptimisationSummary.Create(partOOptimisationRun);

            Assert.StartsWith("3 run, 2 completed in full · limit 10 · 5 l/s step", partOOptimisationSummary.Facts.Single(x => x.Label == "Rounds").Value, StringComparison.Ordinal);

            //Baseline: kitchen and bedroom both fail (two rows for the kitchen, one space).
            Assert.Equal("Run 0 · production TM59 status Fail · 2 spaces with a failing TM59 check", partOOptimisationSummary.Facts.Single(x => x.Label == "Starting design").Value);

            //Kept: round 2, the kitchen still fails.
            Assert.Equal("Run 2 · production TM59 status Fail · 1 space with a failing TM59 check", partOOptimisationSummary.Facts.Single(x => x.Label == "Kept design").Value);

            //Kitchen targeted in both rounds, bedroom derived in round 1 and targeted in round 2 - counted once,
            //as targeted. The living room belongs to the failed round only.
            Assert.Equal("2 spaces · 2 targeted, 0 balancing", partOOptimisationSummary.Facts.Single(x => x.Label == "Design airflow changed").Value);
        }

        /// <summary>
        /// The capacity envelope: stated only where it was asked for; "Calculated (diagnostic, not adopted)"
        /// where it ran, with the run's own description on the tooltip; "Not calculated" with the reason
        /// otherwise.
        /// </summary>
        [Fact]
        public void TheCapacityEnvelope_IsStatedApartAndOnlyWhereAskedFor()
        {
            PartOOptimisationRun partOOptimisationRun_Off = History(rounds: 1, capacityEnvelope: false);
            partOOptimisationRun_Off.StopReason = PartOOptimisationStopReason.CapacityReached;

            Assert.DoesNotContain(PartOOptimisationSummary.Create(partOOptimisationRun_Off).Facts, x => x.Label == "Capacity envelope");

            PartOOptimisationRun partOOptimisationRun_NotRun = History(rounds: 1);
            partOOptimisationRun_NotRun.StopReason = PartOOptimisationStopReason.RebalanceRefused;
            partOOptimisationRun_NotRun.CapacityEnvelopeDescription = "The optimisation stopped at 'Rebalance Refused', so no capacity envelope was calculated.";

            PartOOptimisationSummary.Fact fact_NotRun = PartOOptimisationSummary.Create(partOOptimisationRun_NotRun).Facts.Single(x => x.Label == "Capacity envelope");
            Assert.Equal("Not calculated", fact_NotRun.Value);
            Assert.Equal(partOOptimisationRun_NotRun.CapacityEnvelopeDescription, fact_NotRun.Detail);

            PartOOptimisationRun partOOptimisationRun_Run = History(rounds: 1);
            partOOptimisationRun_Run.StopReason = PartOOptimisationStopReason.CapacityReached;
            partOOptimisationRun_Run.Steps.Add(new PartOOptimisationStep(2, PartOOptimisationStepKind.CapacityEnvelope)
            {
                IsCompleted = true,
                OccupiedSpaceComplianceStatus = TM59ComplianceStatus.Pass,
            });
            partOOptimisationRun_Run.AnalyticalModel_CapacityEnvelope = Model("envelope");
            partOOptimisationRun_Run.CapacityEnvelopeDescription = "Grown to the unit ceiling.";

            PartOOptimisationSummary partOOptimisationSummary_Run = PartOOptimisationSummary.Create(partOOptimisationRun_Run);
            PartOOptimisationSummary.Fact fact_Run = partOOptimisationSummary_Run.Facts.Single(x => x.Label == "Capacity envelope");

            Assert.StartsWith("Calculated (diagnostic, not adopted)", fact_Run.Value, StringComparison.Ordinal);
            Assert.Contains("Grown to the unit ceiling.", fact_Run.Detail, StringComparison.Ordinal);

            //And the envelope never becomes the verdict: the kept design failed.
            Assert.Equal(PartOTM59Verdict.Fail, partOOptimisationSummary_Run.Verdict);
        }

        // ---- The result window -----------------------------------------------------------------------

        /// <summary>
        /// Simple by default: the verdict and the stop reason first, the engineering detail collapsed, and
        /// nothing removed - both histories are bound, and the notes open with the run's own complete
        /// statement.
        /// </summary>
        [WpfFact]
        public void TheResultWindow_LeadsWithTheOutcomeAndCollapsesTheDetail()
        {
            PartOOptimisationRun partOOptimisationRun = History(rounds: 2);
            partOOptimisationRun.StopReason = PartOOptimisationStopReason.CapacityReached;

            PartOOptimisationResultWindow partOOptimisationResultWindow = new();
            partOOptimisationResultWindow.Show(partOOptimisationRun, false);

            Assert.Equal("TM59 FAIL", partOOptimisationResultWindow.textBlock_Verdict.Text);
            Assert.Equal("✕", partOOptimisationResultWindow.textBlock_VerdictGlyph.Text);
            Assert.Equal("Stopped: selected ventilation unit capacity reached", partOOptimisationResultWindow.textBlock_StopReason.Text);
            Assert.Equal(partOOptimisationResultWindow.Summary.NextStep, partOOptimisationResultWindow.run_NextStep.Text);
            Assert.Equal(Visibility.Collapsed, partOOptimisationResultWindow.textBlock_CancelNote.Visibility);

            Assert.False(partOOptimisationResultWindow.expander_Detail.IsExpanded);

            Assert.NotEmpty((System.Collections.IEnumerable)partOOptimisationResultWindow.dataGrid_AirFlow.ItemsSource);
            Assert.NotEmpty((System.Collections.IEnumerable)partOOptimisationResultWindow.dataGrid_Unit.ItemsSource);

            Assert.StartsWith(partOOptimisationRun.Description, partOOptimisationResultWindow.textBox_Diagnostics.Text, StringComparison.Ordinal);

            Assert.Same(partOOptimisationResultWindow.Summary.Facts, partOOptimisationResultWindow.itemsControl_Facts.ItemsSource);
        }

        /// <summary>
        /// Evidence only: renders the Start confirmation and the result window - synthetic histories, no TAS -
        /// into <c>SAM_PARTO_2B_EVIDENCE</c>. Without that variable it does nothing.
        /// </summary>
        [WpfFact]
        public void Evidence_renders_the_start_confirmation_and_the_result_window()
        {
            string directory = Environment.GetEnvironmentVariable("SAM_PARTO_2B_EVIDENCE");

            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);

            PartOOptimisationStartWindow partOOptimisationStartWindow = new()
            {
                Start = PartOOptimisationStart.Create(Completed(Context(select: true, settings: null), out string _, "Block-A-Iteration2"), null),
            };

            PartOWorkflowEvidenceHarness.Render(partOOptimisationStartWindow, Path.Combine(directory, "2b-start.png"), 640, 0);

            partOOptimisationStartWindow.AirFlowStepText = "0";

            PartOWorkflowEvidenceHarness.Render(partOOptimisationStartWindow, Path.Combine(directory, "2b-start-invalid.png"), 640, 0);
            partOOptimisationStartWindow.Close();

            foreach ((string name, PartOOptimisationStopReason stopReason, int rounds, TM59ComplianceStatus lastStatus, bool cancelRequested, bool expanded) in new[]
            {
                ("2b-result-capacity.png", PartOOptimisationStopReason.CapacityReached, 3, TM59ComplianceStatus.Fail, false, false),
                ("2b-result-capacity-detail.png", PartOOptimisationStopReason.CapacityReached, 3, TM59ComplianceStatus.Fail, false, true),
                ("2b-result-passed.png", PartOOptimisationStopReason.Passed, 2, TM59ComplianceStatus.Pass, false, false),
                ("2b-result-baseline-passes.png", PartOOptimisationStopReason.Passed, 0, TM59ComplianceStatus.Pass, false, false),
                ("2b-result-simulation-failed-after-cancel.png", PartOOptimisationStopReason.SimulationFailed, 2, TM59ComplianceStatus.Fail, true, false),
            })
            {
                PartOOptimisationRun partOOptimisationRun = History(rounds, lastStatus);
                partOOptimisationRun.StopReason = stopReason;
                partOOptimisationRun.CapacityEnvelopeDescription = stopReason == PartOOptimisationStopReason.Passed
                    ? "The optimisation reached a design in which every eligible occupied space passes its production TM59 criteria, so there is nothing for a capacity envelope to diagnose and none was calculated."
                    : "The optimisation stopped at a failure, so no capacity envelope was calculated.";

                PartOOptimisationResultWindow partOOptimisationResultWindow = new();
                partOOptimisationResultWindow.Show(partOOptimisationRun, cancelRequested);
                partOOptimisationResultWindow.expander_Detail.IsExpanded = expanded;

                PartOWorkflowEvidenceHarness.Render(partOOptimisationResultWindow, Path.Combine(directory, name), 1180, 0);
                partOOptimisationResultWindow.Close();
            }
        }

        // ---- Fixture ---------------------------------------------------------------------------------

        private static AnalyticalModel Model(string name)
        {
            return new AnalyticalModel(name, null, null, null, new AdjacencyCluster(), null, null);
        }

        private static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)];
        }

        /// <summary>An Iteration 2 (select) or 1a preparation context, with or without recorded 2B settings.</summary>
        private static PartOPreparationContext Context(bool select, PartOOptimisationSettings? settings)
        {
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = select
                ? [new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "MVHR-150", null), 150, 150, 0)]
                : null;

            return new PartOPreparationContext(PartOIteration.BasePassive, [new Zone("Flat 1")], [], ventilationUnitCapacityDescriptors)
            {
                OptimisationSettings = settings,
            };
        }

        /// <summary>
        /// A run driven through the production sequence to <c>WorkflowCompleted</c> - prepare, announce the
        /// results file, let the workflow write it, complete - as <c>PartOOptimisationTests</c> does.
        /// </summary>
        private static PartORun Completed(PartOPreparationContext partOPreparationContext, out string path_TSD, string? name = null)
        {
            PartORun result = new();

            Assert.True(result.Prepare(Model("prepared"), Scenarios(), partOPreparationContext));

            path_TSD = Path.Combine(Path.GetTempPath(), string.Format("{0}.tsd", name ?? string.Format("SAM_PartOIteration2BJourneyTests_{0}", Guid.NewGuid())));

            Assert.True(result.ExpectResults(path_TSD));

            File.WriteAllText(path_TSD, string.Format("results this workflow wrote - {0}", Guid.NewGuid()));

            PartOSimulationContext partOSimulationContext = new(Path.GetTempPath(), "Fixture", null, SolarCalculationMethod.SAM, 1, 365);

            Assert.True(result.Complete(Model("workflow"), path_TSD, partOSimulationContext, out string refusal), refusal);

            return result;
        }

        /// <summary>
        /// A baseline where the kitchen (two failing checks) and the bedroom fail, then <paramref name="rounds"/>
        /// completed rounds. Round 1 targets the kitchen and derives the bedroom; later rounds target both. The
        /// last round's kitchen still fails unless <paramref name="lastStatus"/> says Pass.
        /// </summary>
        private static PartOOptimisationRun History(int rounds, TM59ComplianceStatus lastStatus = TM59ComplianceStatus.Fail, bool capacityEnvelope = true)
        {
            VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = new(new VentilationUnitReference("Test Fixture", "MVHR-150", null), 150, 150, 0);

            PartOOptimisationRun result = new(new PartOOptimisationSettings { CapacityEnvelope = capacityEnvelope });

            PartOOptimisationStep partOOptimisationStep_Baseline = new(0)
            {
                Path_TSD = Path.Combine(Path.GetTempPath(), "Fixture.tsd"),
                OccupiedSpaceComplianceStatus = rounds == 0 ? lastStatus : TM59ComplianceStatus.Fail,
                IsCompleted = true,
            };

            if (rounds != 0 || lastStatus == TM59ComplianceStatus.Fail)
            {
                partOOptimisationStep_Baseline.TM59Results.Add(new PartOTM59SpaceResult(guid_Kitchen, "Kitchen", "Criterion 1", 200, 142, TM59ComplianceStatus.Fail, true));
                partOOptimisationStep_Baseline.TM59Results.Add(new PartOTM59SpaceResult(guid_Kitchen, "Kitchen", "Criterion 2", 30, 20, TM59ComplianceStatus.Fail, true));
                partOOptimisationStep_Baseline.TM59Results.Add(new PartOTM59SpaceResult(guid_Bedroom, "Bedroom", "Criterion 1", 300, 262, TM59ComplianceStatus.Fail, true));
            }

            partOOptimisationStep_Baseline.UnitStates.Add(new PartOOptimisationUnitState("MVHR-01", "Flat 1", 30, 30, ventilationUnitCapacityDescriptor.VentilationUnitReference, ventilationUnitCapacityDescriptor, VentilationUnitSelectionOutcome.Kept, null));

            result.Steps.Add(partOOptimisationStep_Baseline);

            for (int iteration = 1; iteration <= rounds; iteration++)
            {
                bool last = iteration == rounds;

                PartOOptimisationStep partOOptimisationStep = new(iteration)
                {
                    Path_TSD = Path.Combine(Path.GetTempPath(), string.Format("Fixture-Opt{0:00}.tsd", iteration)),
                    OccupiedSpaceComplianceStatus = last ? lastStatus : TM59ComplianceStatus.Fail,
                    IsCompleted = true,
                };

                partOOptimisationStep.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Kitchen, "Kitchen", FlowClassification.Extract, 22, 27, 13, false));

                if (iteration == 1)
                {
                    partOOptimisationStep.DerivedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bedroom, "Bedroom", FlowClassification.Supply, 30, 35, 13, true));
                }
                else
                {
                    partOOptimisationStep.TargetedAdjustments.Add(new DesignAirFlowAdjustment(guid_Bedroom, "Bedroom", FlowClassification.Supply, 35, 40, 13, false));
                }

                TM59ComplianceStatus status_Kitchen = last && lastStatus == TM59ComplianceStatus.Pass ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.Fail;

                partOOptimisationStep.TM59Results.Add(new PartOTM59SpaceResult(guid_Kitchen, "Kitchen", "Criterion 1", 150, 142, status_Kitchen, true));
                partOOptimisationStep.TM59Results.Add(new PartOTM59SpaceResult(guid_Bedroom, "Bedroom", "Criterion 1", 100, 262, TM59ComplianceStatus.Pass, true));

                partOOptimisationStep.UnitStates.Add(new PartOOptimisationUnitState("MVHR-01", "Flat 1", 35, 35, ventilationUnitCapacityDescriptor.VentilationUnitReference, ventilationUnitCapacityDescriptor, VentilationUnitSelectionOutcome.Kept, null));

                result.Steps.Add(partOOptimisationStep);
            }

            result.AnalyticalModel_LastValid = Model("last valid");
            result.Path_TSD_LastValid = result.Step_LastValid?.Path_TSD;

            return result;
        }
    }
}
