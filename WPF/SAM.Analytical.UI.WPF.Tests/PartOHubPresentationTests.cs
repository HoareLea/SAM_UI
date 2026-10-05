// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Prepare &amp; Run Hub's presentation pass: the workflow strip, the compact status labels, the one
    /// route line, and what is shown only when it is relevant. Every assertion here is about what is DRAWN;
    /// the inspection's statuses are asserted to be unchanged beside it.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOHubPresentationTests
    {
        // ----- plurals ------------------------------------------------------------------------------------

        [Fact]
        public void Counts_are_worded_in_the_right_number()
        {
            Assert.Equal("1 space", UI.Query.PartOCount(1, "space", "spaces"));
            Assert.Equal("0 spaces", UI.Query.PartOCount(0, "space", "spaces"));
            Assert.Equal("8 spaces", UI.Query.PartOCount(8, "space", "spaces"));
        }

        [WpfFact]
        public void No_status_line_uses_programmatic_plurals()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a());

            foreach (PartOWorkflowStatusRow partOWorkflowStatusRow in partOWorkflowWindow.StatusRows)
            {
                Assert.DoesNotContain("(s)", partOWorkflowStatusRow.FullDetail);
                Assert.DoesNotContain("(s)", partOWorkflowStatusRow.ShortDetail);
            }

            Assert.DoesNotContain("(s)", partOWorkflowWindow.ScopeDescription);

            partOWorkflowWindow.Close();
        }

        // ----- compact status ----------------------------------------------------------------------------

        [WpfFact]
        public void A_fresh_iteration_1a_reads_as_compact_first_level_lines()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a());

            PartOWorkflowStatusRow Row(PartOWorkflowStage partOWorkflowStage) => partOWorkflowWindow.StatusRows.Single(x => x.State.Stage == partOWorkflowStage);

            Assert.Equal("Ready", Row(PartOWorkflowStage.DwellingScope).StatusLabel);
            Assert.Equal("1 dwelling · 1 space", Row(PartOWorkflowStage.DwellingScope).ShortDetail);
            Assert.Equal("1/1 space mapped", Row(PartOWorkflowStage.InternalConditions).ShortDetail);
            Assert.Equal("1/1 space defined", Row(PartOWorkflowStage.PartFRequirements).ShortDetail);
            Assert.Equal("Not prepared", Row(PartOWorkflowStage.VentilationDesign).StatusLabel);
            Assert.Equal("Built by Prepare & Run", Row(PartOWorkflowStage.VentilationDesign).ShortDetail);
            Assert.Equal("Waiting", Row(PartOWorkflowStage.ModelCheck).StatusLabel);
            Assert.Equal("Not run", Row(PartOWorkflowStage.Simulation).StatusLabel);
            Assert.Equal("Not available", Row(PartOWorkflowStage.Results).StatusLabel);

            //The labels are presentation: the statuses underneath are the inspection's, as before.
            Assert.Equal(PartOWorkflowStageStatus.Prepare, Row(PartOWorkflowStage.VentilationDesign).State.Status);
            Assert.Equal(PartOWorkflowStageStatus.Pending, Row(PartOWorkflowStage.ModelCheck).State.Status);
            Assert.Equal(PartOWorkflowStageStatus.NotRun, Row(PartOWorkflowStage.Results).State.Status);

            //The complete sentence is never lost.
            Assert.Contains("1 of 1 eligible dwelling zone in scope, 1 space", Row(PartOWorkflowStage.DwellingScope).FullDetail);

            partOWorkflowWindow.Close();
        }

        [Fact]
        public void Every_status_has_a_glyph_as_well_as_a_colour()
        {
            foreach (PartOWorkflowStageStatus partOWorkflowStageStatus in System.Enum.GetValues(typeof(PartOWorkflowStageStatus)))
            {
                Assert.False(string.IsNullOrWhiteSpace(PartOWorkflowStatusRow.Glyph(partOWorkflowStageStatus)));

                foreach (PartOWorkflowStage partOWorkflowStage in System.Enum.GetValues(typeof(PartOWorkflowStage)))
                {
                    Assert.False(string.IsNullOrWhiteSpace(PartOWorkflowStatusRow.Label(partOWorkflowStage, partOWorkflowStageStatus)));
                }
            }
        }

        // ----- equipment said once -----------------------------------------------------------------------

        [WpfFact]
        public void Iteration_1a_says_once_that_no_manufacturer_unit_is_required()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a());

            Assert.EndsWith("· Design duty only · No manufacturer unit required", partOWorkflowWindow.ScenarioDescription);

            //The section is out of sight, the control still compact behind it, and the N/A Equipment row is
            //not drawn - but it is still one of the inspection's rows.
            Assert.False(partOWorkflowWindow.IsEquipmentSectionVisible);
            Assert.True(partOWorkflowWindow.IsEquipmentSelectionCompact);
            Assert.Contains(partOWorkflowWindow.StatusRows, x => x.State.Stage == PartOWorkflowStage.Equipment);
            Assert.DoesNotContain(partOWorkflowWindow.RenderedStatusGroups.SelectMany(x => x.Rows), x => x.State.Stage == PartOWorkflowStage.Equipment);
            Assert.Equal(partOWorkflowWindow.StatusRows.Count - 1, partOWorkflowWindow.RenderedStatusGroups.Sum(x => x.Rows.Count));

            partOWorkflowWindow.Close();
        }

        [WpfFact]
        public void Iteration_2_shows_the_equipment_section_and_draws_every_row()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit));

            Assert.True(partOWorkflowWindow.IsEquipmentSectionVisible);
            Assert.Equal(partOWorkflowWindow.StatusRows.Count, partOWorkflowWindow.RenderedStatusGroups.Sum(x => x.Rows.Count));

            partOWorkflowWindow.Close();
        }

        // ----- workflow strip ----------------------------------------------------------------------------

        [WpfFact]
        public void A_fresh_iteration_1a_is_configured_and_about_to_prepare()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a());

            IReadOnlyList<PartOWorkflowStep> steps = partOWorkflowWindow.WorkflowSteps;

            Assert.Equal([PartOWorkflowProgress.Name_Configure, PartOWorkflowProgress.Name_Prepare, PartOWorkflowProgress.Name_Simulate, PartOWorkflowProgress.Name_Review], steps.Select(x => x.Name));
            Assert.Equal([PartOWorkflowStepState.Done, PartOWorkflowStepState.Current, PartOWorkflowStepState.Upcoming, PartOWorkflowStepState.Upcoming], steps.Select(x => x.State));

            //Never colour alone: the state is a word and a glyph too.
            Assert.Equal("Done", steps[0].StateText);
            Assert.Equal("✓", steps[0].Glyph);
            Assert.Equal("Next", steps[1].StateText);

            Assert.Equal("Next: Prepare the ventilation design and run the Part O assessment.", partOWorkflowWindow.NextStepText);

            partOWorkflowWindow.Close();
        }

        [WpfFact]
        public void With_results_the_strip_offers_review_and_keeps_the_mixed_state_honest()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a(), resultsAvailable: true);

            IReadOnlyList<PartOWorkflowStep> steps = partOWorkflowWindow.WorkflowSteps;

            //The design is not reused (the inspection says the next run rebuilds it), the simulation is done,
            //and Review is what can be taken now.
            Assert.Equal([PartOWorkflowStepState.Done, PartOWorkflowStepState.Upcoming, PartOWorkflowStepState.Done, PartOWorkflowStepState.Current], steps.Select(x => x.State));
            Assert.StartsWith("Next: Review the TM59 results.", partOWorkflowWindow.NextStepText);

            //The design row says what "not prepared" means beside results that exist - the status is unchanged.
            PartOWorkflowStatusRow partOWorkflowStatusRow = partOWorkflowWindow.StatusRows.Single(x => x.State.Stage == PartOWorkflowStage.VentilationDesign);
            Assert.Equal(PartOWorkflowStageStatus.Prepare, partOWorkflowStatusRow.State.Status);
            Assert.StartsWith("Rebuilt for the next Prepare & Run", partOWorkflowStatusRow.ShortDetail);
            Assert.Contains("rebuilds the ventilation design", steps[1].Detail);
            Assert.Equal(string.Empty, partOWorkflowWindow.ReviewCaption);

            partOWorkflowWindow.Close();
        }

        [WpfFact]
        public void A_blocked_configuration_is_shown_as_blocked_with_no_next_step()
        {
            PartOWorkflowWindow partOWorkflowWindow = new();
            partOWorkflowWindow.CompleteInitialisation();

            IReadOnlyList<PartOWorkflowStep> steps = partOWorkflowWindow.WorkflowSteps;

            Assert.Equal(PartOWorkflowStepState.Blocked, steps[0].State);
            Assert.Equal("Blocked", steps[0].StateText);
            Assert.DoesNotContain(steps, x => x.State == PartOWorkflowStepState.Current);
            Assert.False(partOWorkflowWindow.CanRun);
            Assert.StartsWith("✕ Prepare & Run is unavailable", partOWorkflowWindow.NextStepText);

            partOWorkflowWindow.Close();
        }

        /// <summary>
        /// With reviewable results open, invalidating the preparation inputs blocks
        /// Prepare &amp; Run but not Review, which reads the existing run. Review stays the next step; the
        /// blocker is stated beside it.
        /// </summary>
        [WpfFact]
        public void Reviewable_results_stay_the_next_step_while_prepare_and_run_is_blocked()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a(), resultsAvailable: true);

            //No dwelling selected: the scope blocks the run.
            partOWorkflowWindow.Scope = PartOWorkflowScope.SelectedDwellings;
            partOWorkflowWindow.DwellingSelection.SetSelected(false);

            Assert.False(partOWorkflowWindow.CanRun);
            Assert.True(partOWorkflowWindow.CanReviewResults);

            IReadOnlyList<PartOWorkflowStep> steps = partOWorkflowWindow.WorkflowSteps;

            Assert.Equal(PartOWorkflowStepState.Blocked, steps[0].State);
            Assert.Equal(PartOWorkflowStepState.Current, steps.Single(x => x.Name == PartOWorkflowProgress.Name_Review).State);

            Assert.StartsWith("Next: Review the TM59 results.", partOWorkflowWindow.NextStepText);
            Assert.Contains("✕ Prepare & Run is unavailable — resolve the", partOWorkflowWindow.NextStepText);

            partOWorkflowWindow.Close();
        }

        [WpfFact]
        public void Optimise_2B_appears_on_the_strip_only_where_the_scenario_can_carry_it()
        {
            PartOWorkflowWindow partOWorkflowWindow_1a = Window(Scenario_1a());
            Assert.DoesNotContain(partOWorkflowWindow_1a.WorkflowSteps, x => x.Name == PartOWorkflowProgress.Name_Optimise);
            Assert.Equal("Iteration 2 only", partOWorkflowWindow_1a.OptimiseCaption);
            Assert.Equal("No results yet", partOWorkflowWindow_1a.ReviewCaption);
            Assert.Equal(partOWorkflowWindow_1a.OptimisationDescription, partOWorkflowWindow_1a.OptimiseToolTip);
            partOWorkflowWindow_1a.Close();

            PartOWorkflowWindow partOWorkflowWindow_2 = Window(PartOWorkflowScenario.Scenarios.Find(x => x.SupportsOptimisation));
            Assert.Contains(partOWorkflowWindow_2.WorkflowSteps, x => x.Name == PartOWorkflowProgress.Name_Optimise);
            Assert.Equal("After an Iteration 2 run", partOWorkflowWindow_2.OptimiseCaption);
            partOWorkflowWindow_2.Close();
        }

        // ----- Iteration 3 only when relevant ------------------------------------------------------------

        [WpfFact]
        public void The_iteration_3_panel_waits_for_a_reference_run()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a());

            Assert.False(partOWorkflowWindow.IsIteration3PanelVisible);
            Assert.False(partOWorkflowWindow.CanRunIteration3);

            partOWorkflowWindow.Close();
        }

        // ----- stays on screen ---------------------------------------------------------------------------

        /// <summary>
        /// Found live: a Hub opened at its owner's cascade position ran off the bottom of the working area,
        /// with the action row behind the taskbar. Shown low, it is moved up to fit when it first renders.
        /// </summary>
        [WpfFact]
        public void A_hub_opened_below_the_working_area_is_moved_up_to_fit()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a());

            System.Windows.Rect rect = System.Windows.SystemParameters.WorkArea;

            partOWorkflowWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            partOWorkflowWindow.ShowActivated = false;
            partOWorkflowWindow.Left = rect.Left + 10;
            partOWorkflowWindow.Top = rect.Bottom - 100;

            partOWorkflowWindow.Show();
            partOWorkflowWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            Assert.True(partOWorkflowWindow.Top + partOWorkflowWindow.ActualHeight <= rect.Bottom + 1, string.Format("bottom {0} is below the working area {1}", partOWorkflowWindow.Top + partOWorkflowWindow.ActualHeight, rect.Bottom));
            Assert.True(partOWorkflowWindow.Top >= rect.Top);

            partOWorkflowWindow.Close();
        }

        /// <summary>
        /// On a monitor shorter than the primary, a Hub taller than that monitor's
        /// working area could not be moved far enough, so the ceiling is capped to the monitor it is on. On
        /// a monitor shorter than the window's own minimum height, the minimum comes down too - WPF would
        /// otherwise hold the window at a height that does not fit.
        /// </summary>
        [Theory]
        //Fits already: untouched.
        [InlineData(100, 500, 520, 2000, 0, 1040, 100, 520, 956.8)]
        //Opened low on a 1080p secondary: moved up.
        [InlineData(700, 800, 520, 1300, 0, 1040, 240, 520, 956.8)]
        //Taller than the secondary's working area (ceiling from a 4K primary): capped, then placed.
        [InlineData(300, 1300, 520, 1900, 0, 1040, 83.2, 520, 956.8)]
        //A secondary above the primary, in negative coordinates.
        [InlineData(-200, 900, 520, 1000, -1080, -40, -940, 520, 956.8)]
        //Placed by the height WPF will really give it - never less than its minimum.
        [InlineData(600, 400, 520, 2000, 0, 1040, 520, 520, 956.8)]
        //A 768 px display at high scaling: the working area is shorter than MinHeight, which comes down.
        [InlineData(200, 553, 520, 2000, 0, 560, 44.8, 515.2, 515.2)]
        public void The_hub_is_placed_and_capped_against_the_monitor_it_is_on(double top, double height, double minHeight, double maxHeight, double areaTop, double areaBottom, double top_Expected, double minHeight_Expected, double maxHeight_Expected)
        {
            (double top_Result, double minHeight_Result, double maxHeight_Result) = PartOWorkflowWindow.Placement(top, height, minHeight, maxHeight, areaTop, areaBottom);

            Assert.Equal(maxHeight_Expected, maxHeight_Result, 3);
            Assert.Equal(minHeight_Expected, minHeight_Result, 3);
            Assert.Equal(top_Expected, top_Result, 3);

            //The action row - the bottom of the height WPF will really give it - is inside the working area.
            double height_Real = System.Math.Max(minHeight_Result, System.Math.Min(height, maxHeight_Result));
            Assert.True(top_Result + height_Real <= areaBottom + 1e-9);
            Assert.True(top_Result >= areaTop - 1e-9);
        }

        /// <summary>
        /// Placed near the bottom of its monitor, the Hub then grows with its content
        /// (Show details). The growth must not take the action row below the working area.
        /// </summary>
        [WpfFact]
        public void Growing_after_it_is_placed_keeps_the_action_row_on_screen()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(Scenario_1a());

            System.Windows.Rect rect = System.Windows.SystemParameters.WorkArea;

            partOWorkflowWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            partOWorkflowWindow.ShowActivated = false;
            partOWorkflowWindow.Left = rect.Left + 10;
            partOWorkflowWindow.Top = rect.Top + 10;

            partOWorkflowWindow.Show();
            partOWorkflowWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            //Flush with the bottom of the working area, then grown by its content.
            double height_Compact = partOWorkflowWindow.ActualHeight;
            partOWorkflowWindow.Top = rect.Bottom - height_Compact - 2;

            partOWorkflowWindow.ShowStatusDetails = true;
            partOWorkflowWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            Assert.True(partOWorkflowWindow.ActualHeight > height_Compact, "Show details did not make the Hub taller");
            Assert.True(partOWorkflowWindow.Top + partOWorkflowWindow.ActualHeight <= rect.Bottom + 1, string.Format("bottom {0} is below the working area {1}", partOWorkflowWindow.Top + partOWorkflowWindow.ActualHeight, rect.Bottom));

            partOWorkflowWindow.Close();
        }

        // ----- fixture -----------------------------------------------------------------------------------

        private static PartOWorkflowScenario Scenario_1a()
        {
            return PartOWorkflowScenario.Scenarios.Find(x => !x.SelectVentilationUnit && x.Option?.PartOVentilationMode == PartOVentilationMode.MVHR);
        }

        /// <summary>One flat, one TM59 bedroom carrying a continuous Part F supply.</summary>
        private static PartOWorkflowWindow Window(PartOWorkflowScenario partOWorkflowScenario, bool resultsAvailable = false)
        {
            AdjacencyCluster adjacencyCluster = new();

            Zone zone = new("Flat 1");
            zone.SetValue(ZoneParameter.IsDwelling, true);

            adjacencyCluster.AddObject(zone);

            Space space = new("Bedroom", null)
            {
                InternalCondition = new InternalCondition("TM59_Double Bedroom"),
            };

            PartFVentilationTerminalRequirement partFVentilationTerminalRequirement = new(space.Name + " requirement", space.Guid, PartFTerminalRole.Supply)
            {
                ContinuousDesignFlowRate_Lps = 13,
            };

            PartFSpaceData partFSpaceData = new();
            partFSpaceData.Terminals.Add(partFVentilationTerminalRequirement);

            space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);

            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddRelation(zone, space);

            PartOWorkflowWindow result = new()
            {
                AnalyticalModel = new AnalyticalModel("Block", null, null, null, adjacencyCluster, new MaterialLibrary("Materials"), new ProfileLibrary("Profiles")),
                PartORun = new PartORun(),
                Capabilities = new PartOWorkflowCapabilities { EquipmentAvailable = true, ResultsAvailable = resultsAvailable },
            };

            result.Restore(partOWorkflowScenario, PartOWorkflowScope.AllDwellings, null);

            result.CompleteInitialisation();

            return result;
        }
    }
}
