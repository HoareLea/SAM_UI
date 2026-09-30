// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Review iteration window's UX pass: the scenario it is headed with, the explicit decision that
    /// starts TAS, the collapsed diagnostics, and what each answer leads to. The engineering tables and the
    /// preparation are not under test here - their own tests are unchanged.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOReviewIterationTests
    {
        // ----- the scenario heading ----------------------------------------------------------------------

        /// <summary>
        /// The journey review found an Iteration 2 review headed "Iteration 1a — MVHR design duty (no
        /// manufacturer unit)": Iteration 2 prepares as the 1a engine iteration with a catalogue, and the
        /// summary printed the engine option. Each scenario is now headed exactly as the Hub names it.
        /// </summary>
        [Theory]
        [InlineData(PartOVentilationMode.MVHR, false, PartOWorkflowScenario.Text_Iteration1a)]
        [InlineData(PartOVentilationMode.NaturalVentilation, false, PartOWorkflowScenario.Text_Iteration1b)]
        [InlineData(PartOVentilationMode.MVHR, true, PartOWorkflowScenario.Text_Iteration2)]
        public void The_review_is_headed_with_the_Hub_scenario(PartOVentilationMode partOVentilationMode, bool selectVentilationUnit, string expected)
        {
            PartOVentilationStrategyOption option = Option(partOVentilationMode);

            PartOReviewSummary partOReviewSummary = Modify.Summary(new PartOIterationPreparation(), option, NoCatalogue(), selectVentilationUnit, null, null);

            Assert.Equal(expected, partOReviewSummary.Scenario);
            Assert.StartsWith(expected, partOReviewSummary.Text);
            Assert.DoesNotContain("(s)", partOReviewSummary.Text);

            //The Hub's own scenario for the same choice says the same thing.
            Assert.Equal(expected, PartOWorkflowScenario.Find(option, selectVentilationUnit)!.Text);
        }

        [Fact]
        public void An_Iteration_2_review_never_says_Iteration_1a()
        {
            PartOReviewSummary partOReviewSummary = Modify.Summary(new PartOIterationPreparation(), Option(PartOVentilationMode.MVHR), NoCatalogue(), true, null, null);

            Assert.DoesNotContain("Iteration 1a", partOReviewSummary.Text);
        }

        /// <summary>A 1a or 1b run uses no catalogue, so its first-level equipment line does not describe one.</summary>
        [Fact]
        public void A_design_duty_only_review_does_not_lead_with_the_catalogue()
        {
            VentilationUnitCatalogue ventilationUnitCatalogue = NoCatalogue();

            PartOReviewSummary partOReviewSummary = Modify.Summary(new PartOIterationPreparation(), Option(PartOVentilationMode.MVHR), ventilationUnitCatalogue, false, null, null);

            Assert.DoesNotContain(ventilationUnitCatalogue.Description, partOReviewSummary.Equipment);

            //Kept, one level down, and in Copy All.
            Assert.Equal(ventilationUnitCatalogue.Description, partOReviewSummary.EquipmentDetail);
            Assert.Contains(ventilationUnitCatalogue.Description, partOReviewSummary.Text);
        }

        [WpfFact]
        public void The_window_shows_the_summary_it_is_given()
        {
            PartOPreparationWindow partOPreparationWindow = new()
            {
                ReviewSummary = Modify.Summary(new PartOIterationPreparation(), Option(PartOVentilationMode.MVHR), NoCatalogue(), true, null, null),
            };

            Assert.Equal(PartOWorkflowScenario.Text_Iteration2, partOPreparationWindow.ScenarioHeading);
            Assert.StartsWith(PartOWorkflowScenario.Text_Iteration2, partOPreparationWindow.CopyAllText());

            partOPreparationWindow.Close();
        }

        // ----- the decision ------------------------------------------------------------------------------

        /// <summary>
        /// From the Prepare &amp; Run Hub, acceptance continues into TAS, and the primary action says so.
        /// </summary>
        [WpfFact]
        public void From_the_Hub_the_action_says_Accept_and_Run_TAS()
        {
            PartOPreparationWindow partOPreparationWindow = new() { Intent = Modify.ReviewIntent_PrepareAndRun };

            Assert.Equal("Accept & Run TAS", partOPreparationWindow.AcceptActionText);
            Assert.Contains("starts the full-year TAS simulation", partOPreparationWindow.DecisionCaption);
            Assert.Equal("Cancel", partOPreparationWindow.CancelActionText);

            //Enter never starts a TAS run; Esc declines.
            Assert.False(partOPreparationWindow.IsAcceptDefault);
            Assert.True(partOPreparationWindow.IsCancelTheCancelButton);

            partOPreparationWindow.Close();
        }

        /// <summary>
        /// Codex on #113: the Prepare Iteration ribbon command opens the same window and stops once the model
        /// is adopted, so "Accept &amp; Run TAS" promised a run that never came. Its review now says
        /// "Accept Preparation" and states that no simulation is started.
        /// </summary>
        [WpfFact]
        public void From_Prepare_Iteration_the_action_says_Accept_Preparation()
        {
            PartOPreparationWindow partOPreparationWindow = new() { Intent = Modify.ReviewIntent_PrepareIteration };

            Assert.Equal("Accept Preparation", partOPreparationWindow.AcceptActionText);
            Assert.Contains("No TAS simulation is started", partOPreparationWindow.DecisionCaption);
            Assert.DoesNotContain("Run TAS", partOPreparationWindow.DecisionCaption);
            Assert.Equal("Cancel", partOPreparationWindow.CancelActionText);
            Assert.False(partOPreparationWindow.IsAcceptDefault);

            partOPreparationWindow.Close();
        }

        /// <summary>A window nobody told otherwise never promises TAS.</summary>
        [WpfFact]
        public void An_unconfigured_review_does_not_promise_TAS()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            Assert.Equal(PartOReviewIntent.PrepareOnly, partOPreparationWindow.Intent);
            Assert.Equal("Accept Preparation", partOPreparationWindow.AcceptActionText);

            partOPreparationWindow.Close();
        }

        [Fact]
        public void Each_entry_point_states_its_own_intent()
        {
            Assert.Equal(PartOReviewIntent.PrepareAndRun, Modify.ReviewIntent_PrepareAndRun);
            Assert.Equal(PartOReviewIntent.PrepareOnly, Modify.ReviewIntent_PrepareIteration);
        }

        /// <summary>
        /// The gate before Prepare &amp; Run's <c>SimulatePartO</c>: only a run intent with an adopted model
        /// continues. A prepare-only acceptance, a decline and a refusal never do.
        /// </summary>
        [Theory]
        [InlineData(PartOReviewIntent.PrepareAndRun, PartOPreparationResult.Adopted, true)]
        [InlineData(PartOReviewIntent.PrepareAndRun, PartOPreparationResult.Declined, false)]
        [InlineData(PartOReviewIntent.PrepareAndRun, PartOPreparationResult.NotPrepared, false)]
        [InlineData(PartOReviewIntent.PrepareOnly, PartOPreparationResult.Adopted, false)]
        [InlineData(PartOReviewIntent.PrepareOnly, PartOPreparationResult.Declined, false)]
        [InlineData(PartOReviewIntent.PrepareOnly, PartOPreparationResult.NotPrepared, false)]
        public void Only_an_accepted_Hub_review_continues_to_TAS(PartOReviewIntent partOReviewIntent, PartOPreparationResult partOPreparationResult, bool expected)
        {
            Assert.Equal(expected, Modify.ContinuesToSimulation(partOReviewIntent, partOPreparationResult));
        }

        /// <summary>
        /// Prepare Iteration's acceptance adopts the preparation into the run - the run is Prepared - and stops
        /// there: the gate says no simulation follows, and the run holds no results to assess. The open design
        /// model is not replaced (PR-4), and with no input changed it is not written at all.
        /// </summary>
        [Fact]
        public void Prepare_only_acceptance_adopts_and_does_not_continue_to_TAS()
        {
            Prepared prepared = new();

            int modified = 0;
            prepared.UIAnalyticalModel.Modified += (s, e) => modified++;

            PartOPreparationResult partOPreparationResult = prepared.Conclude(true);

            Assert.Equal(PartOPreparationResult.Adopted, partOPreparationResult);
            Assert.Equal(PartORunState.Prepared, prepared.PartORun.State);
            Assert.Equal(0, modified);

            Assert.False(Modify.ContinuesToSimulation(Modify.ReviewIntent_PrepareIteration, partOPreparationResult));
            Assert.False(prepared.PartORun.CanAssess);
        }

        /// <summary>Hub acceptance is the same adoption, and it is the one case the gate lets into TAS.</summary>
        [Fact]
        public void Hub_acceptance_adopts_and_continues_to_TAS()
        {
            Prepared prepared = new();

            PartOPreparationResult partOPreparationResult = prepared.Conclude(true);

            Assert.Equal(PartOPreparationResult.Adopted, partOPreparationResult);
            Assert.Equal(PartORunState.Prepared, prepared.PartORun.State);
            Assert.True(Modify.ContinuesToSimulation(Modify.ReviewIntent_PrepareAndRun, partOPreparationResult));
        }

        [Fact]
        public void Cancel_continues_neither_path()
        {
            PartOPreparationResult partOPreparationResult = new Prepared().Conclude(false);

            Assert.Equal(PartOPreparationResult.Declined, partOPreparationResult);
            Assert.False(Modify.ContinuesToSimulation(Modify.ReviewIntent_PrepareAndRun, partOPreparationResult));
            Assert.False(Modify.ContinuesToSimulation(Modify.ReviewIntent_PrepareIteration, partOPreparationResult));
        }

        [WpfFact]
        public void Cancel_answers_no_and_Accept_answers_yes()
        {
            Assert.False(Answer(x => x.PressCancel()));
            Assert.True(Answer(x => x.PressAccept()));
        }

        /// <summary>
        /// Cancel leads to nothing: the loaded model is not replaced, the run is not moved to Prepared - which
        /// Prepare &amp; Run requires before it will call TAS - and the answer is Declined, which the Hub turns
        /// into its line and returns on before the simulation step.
        /// </summary>
        [Fact]
        public void Cancel_adopts_nothing_and_cannot_reach_TAS()
        {
            Prepared prepared = new();

            int modified = 0;
            prepared.UIAnalyticalModel.Modified += (s, e) => modified++;

            PartOPreparationResult partOPreparationResult = prepared.Conclude(false);

            Assert.Equal(PartOPreparationResult.Declined, partOPreparationResult);
            Assert.Equal(0, modified);
            Assert.NotEqual(PartORunState.Prepared, prepared.PartORun.State);
            Assert.False(prepared.PartORun.CanAssess);

            //A closed window (no answer) is a decline too.
            Assert.Equal(PartOPreparationResult.Declined, new Prepared().Conclude(null));
        }

        /// <summary>
        /// Accept &amp; Run TAS adopts the prepared model into the run, and the run is Prepared - the state
        /// Prepare &amp; Run simulates from. The loaded design model is NOT replaced by the prepared one (PR-4):
        /// the run holds the prepared model, and the window keeps the design.
        /// </summary>
        [Fact]
        public void Accept_follows_the_production_adoption()
        {
            Prepared prepared = new();

            int modified = 0;
            prepared.UIAnalyticalModel.Modified += (s, e) => modified++;

            PartOPreparationResult partOPreparationResult = prepared.Conclude(true);

            Assert.Equal(PartOPreparationResult.Adopted, partOPreparationResult);
            Assert.Equal(PartORunState.Prepared, prepared.PartORun.State);
            Assert.Equal(0, modified);
            Assert.Same(prepared.AnalyticalModel_Prepared, prepared.PartORun.AnalyticalModel_Prepared);

            //The run carries the systems the preparation built, as the direct adoption does.
            Assert.NotEmpty(prepared.PartORun.Guids_VentilationSystem_Prepared);
        }

        [Fact]
        public void The_Hub_says_a_review_was_cancelled_before_TAS()
        {
            PartOWorkflowOutcome partOWorkflowOutcome = Modify.DeclinedOutcome(PartOWorkflowScenario.Text_Iteration2);

            Assert.Equal(PartOWorkflowOutcomeKind.Information, partOWorkflowOutcome.Kind);
            Assert.StartsWith("Iteration 2 review cancelled", partOWorkflowOutcome.Headline);
            Assert.Contains("Cancelled before TAS", partOWorkflowOutcome.Text);
            Assert.Contains("no simulation was run", partOWorkflowOutcome.Text);

            //Never colour alone: the line carries a glyph and words.
            Assert.StartsWith("○ ", partOWorkflowOutcome.Text);
        }

        [WpfFact]
        public void The_Hub_shows_the_declined_line()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                LastOutcome = Modify.DeclinedOutcome(PartOWorkflowScenario.Text_Iteration1a),
            };

            Assert.StartsWith("○ Iteration 1a review cancelled — no simulation was run", partOWorkflowWindow.LastOutcomeText);

            partOWorkflowWindow.Close();
        }

        // ----- diagnostics -------------------------------------------------------------------------------

        [WpfFact]
        public void Warnings_and_notes_are_collapsed_with_every_line_still_reachable()
        {
            List<string> warnings = [.. Enumerable.Range(1, 8).Select(x => string.Format("Space 'Room {0}' is still related to ventilation system 'NV 1'.", x))];
            List<string> notes = [.. Enumerable.Range(1, 38).Select(x => string.Format("Note {0}.", x))];

            PartOPreparationWindow partOPreparationWindow = new();
            partOPreparationWindow.SetDiagnostics(notes, warnings, null);

            Assert.Equal("Warnings (8) · Notes (38)", partOPreparationWindow.DiagnosticsHeader);

            Assert.False(partOPreparationWindow.ShowDiagnostics);
            Assert.False(partOPreparationWindow.IsDiagnosticsTextShown);

            //Collapsed is not removed: the box, and Copy All, hold every line.
            foreach (string line in warnings.Concat(notes))
            {
                Assert.Contains(line, partOPreparationWindow.DiagnosticsText);
                Assert.Contains(line, partOPreparationWindow.CopyAllText());
            }

            partOPreparationWindow.ShowDiagnostics = true;

            Assert.True(partOPreparationWindow.IsDiagnosticsTextShown);

            partOPreparationWindow.Close();
        }

        /// <summary>
        /// Found by the live pass: the switch answered only a mouse Click, so toggling it through UI
        /// Automation - or any path that sets IsChecked without a click - ticked the box and showed nothing.
        /// </summary>
        [WpfFact]
        public void The_details_switch_answers_a_toggle_that_is_not_a_click()
        {
            PartOPreparationWindow partOPreparationWindow = new();
            partOPreparationWindow.SetDiagnostics(["A note."], ["A warning."], null);

            Assert.False(partOPreparationWindow.IsDiagnosticsTextShown);

            ((System.Windows.Automation.Provider.IToggleProvider)new System.Windows.Automation.Peers.CheckBoxAutomationPeer(partOPreparationWindow.checkBox_ShowDiagnostics)).Toggle();

            Assert.True(partOPreparationWindow.IsDiagnosticsTextShown);

            partOPreparationWindow.checkBox_ShowDiagnostics.IsChecked = false;

            Assert.False(partOPreparationWindow.IsDiagnosticsTextShown);

            partOPreparationWindow.Close();
        }

        [WpfFact]
        public void A_refusal_opens_the_diagnostics()
        {
            PartOPreparationWindow partOPreparationWindow = new();
            partOPreparationWindow.SetDiagnostics(["A note."], ["A warning."], ["Flat 3: no permitted product can meet the design duty."]);

            Assert.True(partOPreparationWindow.ShowDiagnostics);
            Assert.True(partOPreparationWindow.IsDiagnosticsTextShown);
            Assert.Contains("Flat 3: no permitted product", partOPreparationWindow.DiagnosticsText);

            partOPreparationWindow.Close();
        }

        // ----- equipment controls ------------------------------------------------------------------------

        /// <summary>
        /// Iteration 1a and 1b have no equipment selection, so the authority controls are not drawn; 1b has no
        /// dwelling unit at all, so a sentence stands in for the empty table.
        /// </summary>
        [WpfFact]
        public void Equipment_controls_are_drawn_only_where_selection_is_in_play()
        {
            PartOPreparationWindow partOPreparationWindow_1a = new()
            {
                EquipmentRows = [new PartOEquipmentRow("MVHR-01", "MVHR 1", 30, 30, null, null, "Flat 1")],
            };

            Assert.False(partOPreparationWindow_1a.AreEquipmentControlsShown);
            Assert.False(partOPreparationWindow_1a.IsNoEquipmentShown);

            //The 1a row names its dwelling as the Iteration 2 row does.
            Assert.Equal("Flat 1", partOPreparationWindow_1a.EquipmentRows[0].Dwelling);

            partOPreparationWindow_1a.Close();

            PartOPreparationWindow partOPreparationWindow_1b = new()
            {
                EquipmentRows = [],
            };

            Assert.False(partOPreparationWindow_1b.AreEquipmentControlsShown);
            Assert.True(partOPreparationWindow_1b.IsNoEquipmentShown);

            partOPreparationWindow_1b.Close();
        }

        // ----- helpers -----------------------------------------------------------------------------------

        private static PartOVentilationStrategyOption Option(PartOVentilationMode partOVentilationMode)
        {
            return PartOVentilationStrategyOption.Options.Find(x => x.PartOVentilationMode == partOVentilationMode)!;
        }

        private static VentilationUnitCatalogue NoCatalogue()
        {
            return VentilationUnitCatalogue.Read(Path.Combine(Path.GetTempPath(), string.Format("SAM_NoCatalogue_{0}", Guid.NewGuid())));
        }

        /// <summary>
        /// Shows the window as the orchestrator does - modally - and presses a button once it is up. A timer
        /// closes it regardless, so a failure cannot hang the test run.
        /// </summary>
        private static bool? Answer(Action<PartOPreparationWindow> press)
        {
            PartOPreparationWindow partOPreparationWindow = new()
            {
                ShowActivated = false,
                Left = -32000,
            };

            partOPreparationWindow.ContentRendered += (s, e) => partOPreparationWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => press(partOPreparationWindow)));

            DispatcherTimer dispatcherTimer = new() { Interval = TimeSpan.FromSeconds(10) };
            dispatcherTimer.Tick += (s, e) =>
            {
                dispatcherTimer.Stop();

                partOPreparationWindow.Close();
            };
            dispatcherTimer.Start();

            bool? result = partOPreparationWindow.ShowDialog();

            dispatcherTimer.Stop();

            return result;
        }

        /// <summary>
        /// A preparation as Modify.PreparePartOIteration holds it at the moment the Review window closes: the
        /// loaded model, a prepared copy with the built systems, the context, and a run not yet Prepared.
        /// </summary>
        private sealed class Prepared
        {
            public Prepared()
            {
                AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids_VentilationSystem, out List<Zone> zones);

                UIAnalyticalModel = new UIAnalyticalModel(PartOIteration3Fixture.Model(new AdjacencyCluster(adjacencyCluster)));

                AnalyticalModel_Prepared = PartOIteration3Fixture.Model(new AdjacencyCluster(adjacencyCluster));
                AdjacencyCluster_Prepared = AnalyticalModel_Prepared.AdjacencyCluster;

                PartOIterationPreparation = new PartOIterationPreparation();
                PartOIterationPreparation.OverheatingScenarios.AddRange(PartOIteration3Fixture.Scenarios());

                foreach (Guid guid in guids_VentilationSystem)
                {
                    PartOIterationPreparation.VentilationSystems.Add(adjacencyCluster.GetObject<VentilationSystem>(guid));
                }

                PartOVentilationStrategyOption option = Option(PartOVentilationMode.MVHR);

                Request = new PartOWorkflowRequest(option, PartOWorkflowScope.AllDwellings, zones, false);

                PartOPreparationContext = new PartOPreparationContext(PartOIteration.BasePassive, zones, null, null);
            }

            public UIAnalyticalModel UIAnalyticalModel { get; }

            public PartORun PartORun { get; } = new();

            public AnalyticalModel AnalyticalModel_Prepared { get; }

            public AdjacencyCluster AdjacencyCluster_Prepared { get; }

            public PartOIterationPreparation PartOIterationPreparation { get; }

            public PartOWorkflowRequest Request { get; }

            public PartOPreparationContext PartOPreparationContext { get; }

            public PartOPreparationResult Conclude(bool? accepted)
            {
                return Modify.ConcludePartOReview(accepted, UIAnalyticalModel, PartORun, Request, AnalyticalModel_Prepared, AdjacencyCluster_Prepared, PartOIterationPreparation, PartOPreparationContext, null, null);
            }
        }
    }
}
