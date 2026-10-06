// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The Part O presentation pass, and the line it must not cross.</b>
    ///
    /// <para><b>What is under test</b></para>
    /// <para>
    /// How the Part O family READS: what is collapsed on a route that cannot use it, how the status list is
    /// arranged, how many warnings there are and how they are shown, one spelling per concept, and numbers
    /// that line up with an absence shown as an absence. Every one of these is a presentation decision.
    /// </para>
    ///
    /// <para><b>What each test also asserts</b></para>
    /// <para>
    /// That the engineering answer underneath is untouched. Grouping the status list moves no status;
    /// rendering a NaN as an em dash leaves the value NaN; collapsing two identical warnings leaves both in
    /// the record Copy All copies; renaming a scenario moves no route word, no iteration and no enum. A
    /// presentation pass that quietly changed one of those would pass a screenshot review and fail an
    /// assessment.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOConsistencyPolishTests
    {
        private const string model_MRXBOX = "MRXBOXAB-ECO5-AECV";

        private const string model_XBC15 = "XBC15";

        // =================================================================================================
        // 1. Iteration 1a / 1b: the equipment section is a summary, not a screenful of greyed controls
        // =================================================================================================

        /// <summary>
        /// With no manufacturer unit selected - Iteration 1a and 1b - the equipment section shows its
        /// compact read-only summary and the full, actionable controls are gone from the layout entirely
        /// rather than merely disabled.
        /// </summary>
        [WpfFact]
        public void WithNoUnitSelected_TheEquipmentSectionIsACompactSummary()
        {
            PartOEquipmentSelectionControl control = new()
            {
                VentilationUnitCatalogue = Catalogue(),
                IsSelectionEnabled = false,
            };

            Assert.True(control.IsCompact);
            Assert.False(control.IsFullSectionVisible);

            Assert.Equal("Not used on this route; design duty is independent of manufacturer equipment.", control.CompactEquipmentDescription);
            Assert.Equal("Not used on this route.", control.CompactProjectTestDescription);

            //The catalogue stays readable, and stays closed: it must not be prominent on a route that
            //cannot use it.
            Assert.False(control.IsCompactCatalogueExpanded);
            Assert.Contains("selectable ventilation unit product(s) available", control.CompactCatalogueDescription);
        }

        /// <summary>
        /// And on Iteration 2 the section is exactly what it was: the modes, the catalogue grid and the
        /// project test panel, all present and all actionable.
        /// </summary>
        [WpfFact]
        public void WithAUnitSelected_TheEquipmentSectionIsFullyAvailable()
        {
            PartOEquipmentSelectionControl control = new()
            {
                VentilationUnitCatalogue = Catalogue(),
                IsSelectionEnabled = true,
            };

            Assert.False(control.IsCompact);
            Assert.True(control.IsFullSectionVisible);

            //Still the same three modes over the same catalogue, and still editable in the pooled mode.
            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool);

            Assert.True(control.IsPoolEditable);
            Assert.Equal(2, control.CatalogueProductRows.Count);
        }

        /// <summary>
        /// <b>The compact catalogue disclosure shows the PRODUCTS, not just a count of them.</b>
        ///
        /// <para><b>The regression this pins</b></para>
        /// <para>
        /// Before the compact summary existed, the Iteration 1a / 1b catalogue was disabled rather than
        /// hidden, and an engineer comparing routes could still read which products existed and what they
        /// could move. A disclosure that offered only the count sentence took that away. So the reference
        /// view carries the same identity and capacity columns the Iteration 2 grid carries.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheCompactCatalogueDisclosure_ShowsTheProductsReadOnly()
        {
            PartOEquipmentSelectionControl control = new()
            {
                VentilationUnitCatalogue = Catalogue(),
                IsSelectionEnabled = false,
            };

            Assert.True(control.IsCompact);

            //Collapsed by default, so normal 1a / 1b stays clean - the engineer opens it deliberately.
            Assert.False(control.IsCompactCatalogueExpanded);

            //The SAME rows, by object identity: one notion of what the catalogue holds, not a second copy
            //that could drift from it.
            List<PartOCatalogueProductRow> rows = [.. control.CompactCatalogueRows.OfType<PartOCatalogueProductRow>()];

            Assert.Equal(2, rows.Count);
            Assert.Equal(control.CatalogueProductRows.Count, rows.Count);

            foreach (PartOCatalogueProductRow partOCatalogueProductRow in control.CatalogueProductRows)
            {
                Assert.Single(rows, x => ReferenceEquals(x, partOCatalogueProductRow));
            }

            //And the products themselves are readable - identity and both capability ceilings.
            PartOCatalogueProductRow partOCatalogueProductRow_XBC15 = rows.Find(x => x.Model == model_XBC15);

            Assert.NotNull(partOCatalogueProductRow_XBC15);
            Assert.Equal("Nuaire", partOCatalogueProductRow_XBC15.Manufacturer);
            Assert.Equal("Catalogue", partOCatalogueProductRow_XBC15.Origin);
            Assert.Equal(190, partOCatalogueProductRow_XBC15.MaximumSupply_Lps);
            Assert.Equal(190, partOCatalogueProductRow_XBC15.MaximumExtract_Lps);

            //The count sentence stays too, above the grid.
            Assert.Contains("selectable ventilation unit product(s) available", control.CompactCatalogueDescription);
        }

        /// <summary>
        /// <b>And it is not actionable.</b> Read-only by construction rather than by being greyed: there is
        /// no "Use" column at all, so there is nothing to tick, no pool to edit and no way for this view to
        /// write to a row.
        /// </summary>
        [WpfFact]
        public void TheCompactCatalogueDisclosure_IsNotActionable()
        {
            PartOEquipmentSelectionControl control = new()
            {
                VentilationUnitCatalogue = Catalogue(),
                IsSelectionEnabled = false,
            };

            Assert.True(control.IsCompactCatalogueReadOnly);

            List<string> headers = [.. control.CompactCatalogueColumnHeaders];

            Assert.Equal(
                new[] { "Origin", "Manufacturer", "Model", "Variant", "Max SUP (l/s)", "Max EXT (l/s)" },
                headers);

            Assert.DoesNotContain("Use", headers);

            //The mode radio buttons, the editable catalogue and the project-test panel are all still gone
            //from the layout - the reference view added a table, not a way to configure anything.
            Assert.False(control.IsFullSectionVisible);
            Assert.False(control.IsPoolEditable);

            //And the preselection this control reports is untouched by any of it.
            Assert.Equal(PartOEquipmentSelectionMode.AutomaticAllProducts, control.Mode);
        }

        /// <summary>
        /// Iteration 2's own catalogue is unchanged by the reference view existing: the actionable grid
        /// still has its <c>Use</c> column, is still editable in the pooled mode, and ticking a row still
        /// moves the project's permitted pool.
        /// </summary>
        [WpfFact]
        public void TheIteration2Catalogue_IsUnchangedByTheReferenceView()
        {
            PartOEquipmentSelectionControl control = new()
            {
                VentilationUnitCatalogue = Catalogue(),
                IsSelectionEnabled = true,
            };

            Assert.False(control.IsCompact);
            Assert.True(control.IsFullSectionVisible);

            List<string> headers = [.. control.CatalogueColumnHeaders];

            Assert.Equal("Use", headers[0]);
            Assert.Contains("Max SUP (l/s)", headers);
            Assert.Contains("Max EXT (l/s)", headers);

            control.EquipmentSelection = new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool);

            Assert.True(control.IsPoolEditable);

            //Untick the MRXBOX: the pool follows, exactly as it did before.
            PartOCatalogueProductRow partOCatalogueProductRow = control.CatalogueProductRows.Find(x => x.Model == model_MRXBOX);

            Assert.NotNull(partOCatalogueProductRow);

            partOCatalogueProductRow.IsUsed = false;

            PartOEquipmentSelection partOEquipmentSelection = control.EquipmentSelection;

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOEquipmentSelection.Mode);
            Assert.Single(partOEquipmentSelection.AllowedVentilationUnitReferences);
            Assert.Equal(model_XBC15, partOEquipmentSelection.AllowedVentilationUnitReferences[0].Model);
        }

        /// <summary>
        /// The single-command window reaches the same two states from its own input - the tick that says
        /// whether a manufacturer unit is selected at all.
        /// </summary>
        [WpfFact]
        public void ThePrepareIterationWindow_CollapsesEquipmentWithoutTheTick()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                VentilationUnitCatalogue = Catalogue(),
            };

            //The catalogue setter ticks the box, because Iteration 2 is the current stage.
            Assert.False(partOIterationWindow.IsEquipmentSelectionCompact);

            partOIterationWindow.SelectVentilationUnitChecked = false;

            Assert.True(partOIterationWindow.IsEquipmentSelectionCompact);

            partOIterationWindow.SelectVentilationUnitChecked = true;

            Assert.False(partOIterationWindow.IsEquipmentSelectionCompact);
        }

        /// <summary>
        /// And Prepare &amp; Run reaches them from the chosen scenario, which is where the 1a / 1b / 2
        /// difference lives on that window. The applicability rule is the scenario's own - nothing here
        /// re-derives it.
        /// </summary>
        [WpfFact]
        public void PrepareAndRun_CollapsesEquipmentOnTheScenariosThatSelectNoUnit()
        {
            foreach (PartOWorkflowScenario partOWorkflowScenario in PartOWorkflowScenario.Scenarios)
            {
                PartOWorkflowWindow partOWorkflowWindow = Window(partOWorkflowScenario);

                Assert.Equal(!partOWorkflowScenario.SelectVentilationUnit, partOWorkflowWindow.IsEquipmentSelectionCompact);
            }
        }

        // =================================================================================================
        // 2. The status list is grouped, and grouping changes no status
        // =================================================================================================

        /// <summary>
        /// <b>The whole list, in two groups, with nothing added and nothing lost.</b> Every row the
        /// inspection produced appears exactly once, in the order it was produced, carrying the same stage,
        /// the same status word and the same detail sentence.
        /// </summary>
        [WpfFact]
        public void GroupingTheStatusList_MovesNoStatusAndLosesNoRow()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit));

            List<PartOWorkflowStatusRow> rows = partOWorkflowWindow.StatusRows;
            List<PartOWorkflowStatusGroup> groups = partOWorkflowWindow.StatusGroups;

            Assert.Equal(Enum.GetValues<PartOWorkflowStage>().Length, rows.Count);
            Assert.Equal(2, groups.Count);

            List<PartOWorkflowStatusRow> flattened = [];
            foreach (PartOWorkflowStatusGroup partOWorkflowStatusGroup in groups)
            {
                Assert.True(partOWorkflowStatusGroup.HasRows);

                flattened.AddRange(partOWorkflowStatusGroup.Rows);
            }

            Assert.Equal(rows.Count, flattened.Count);

            //Object identity, so nothing was re-derived on the way into a group.
            foreach (PartOWorkflowStatusRow partOWorkflowStatusRow in rows)
            {
                Assert.Single(flattened, x => ReferenceEquals(x, partOWorkflowStatusRow));
            }

            //And each row still says exactly what its stage state says.
            foreach (PartOWorkflowStatusRow partOWorkflowStatusRow in rows)
            {
                Assert.Equal(partOWorkflowStatusRow.State.StatusText, partOWorkflowStatusRow.StatusText);
                Assert.Equal(partOWorkflowStatusRow.State.Detail, partOWorkflowStatusRow.Detail);
                Assert.Equal(partOWorkflowStatusRow.State.Detail, partOWorkflowStatusRow.FullDetail);
            }
        }

        /// <summary>
        /// The two groups are the ones the engineer needs to tell apart: what the CURRENT CONFIGURATION says,
        /// and what the RUN says. That division is what makes "Ventilation design: NEEDS PREPARATION" beside
        /// "Results: READY" legible rather than contradictory.
        /// <para>
        /// This fixture has no results, so the run heading is the neutral one - see
        /// <see cref="TheRunHeading_ClaimsAnExistingRunOnlyWhenThereIsOne"/>.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheTwoGroups_AreTheConfigurationAndTheRun()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit));

            List<PartOWorkflowStatusGroup> groups = partOWorkflowWindow.StatusGroups;

            Assert.Equal(PartOWorkflowStatusGroup.Name_Configuration, groups[0].Name);
            Assert.Equal(PartOWorkflowStatusGroup.Name_Run, groups[1].Name);

            List<PartOWorkflowStage> stages_Configuration = groups[0].Rows.ConvertAll(x => x.State.Stage);
            List<PartOWorkflowStage> stages_Run = groups[1].Rows.ConvertAll(x => x.State.Stage);

            Assert.Equal(
                new[]
                {
                    PartOWorkflowStage.DwellingScope,
                    PartOWorkflowStage.InternalConditions,
                    PartOWorkflowStage.PartFRequirements,
                    PartOWorkflowStage.VentilationDesign,
                    PartOWorkflowStage.Equipment,
                },
                stages_Configuration);

            Assert.Equal(
                new[]
                {
                    PartOWorkflowStage.ModelCheck,
                    PartOWorkflowStage.Simulation,
                    PartOWorkflowStage.Results,
                },
                stages_Run);
        }

        /// <summary>
        /// <b>The run heading does not claim a run that has not happened.</b>
        ///
        /// <para><b>The regression this pins</b></para>
        /// <para>
        /// The heading was <c>Existing run / results</c> unconditionally. On a fresh model the three rows
        /// beneath it read <c>Model check PENDING</c>, <c>Simulation NOT RUN</c> and
        /// <c>Results NOT RUN</c> - so the heading asserted an existing run directly above three rows
        /// saying there is none, which is the very ambiguity the split exists to remove.
        /// </para>
        /// <para>
        /// The word is now used only where the capabilities say results exist. That is a label chosen from
        /// a fact the application already publishes, not a new state: this test also asserts that the rows
        /// and their statuses are identical in both cases, so nothing but the heading moved.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheRunHeading_ClaimsAnExistingRunOnlyWhenThereIsOne()
        {
            PartOWorkflowScenario partOWorkflowScenario = PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit);

            //Nothing has run.
            PartOWorkflowWindow partOWorkflowWindow_Fresh = Window(partOWorkflowScenario);

            PartOWorkflowStatusGroup partOWorkflowStatusGroup_Fresh = partOWorkflowWindow_Fresh.StatusGroups[1];

            Assert.Equal(PartOWorkflowStatusGroup.Name_Run, partOWorkflowStatusGroup_Fresh.Name);
            Assert.Equal("Run / results", partOWorkflowStatusGroup_Fresh.Name);
            Assert.DoesNotContain("Existing", partOWorkflowStatusGroup_Fresh.Name);

            //And the rows it heads say so themselves, which is what made the old heading a contradiction.
            Assert.Equal(PartOWorkflowStageStatus.Pending, Row(partOWorkflowStatusGroup_Fresh, PartOWorkflowStage.ModelCheck).State.Status);
            Assert.Equal(PartOWorkflowStageStatus.NotRun, Row(partOWorkflowStatusGroup_Fresh, PartOWorkflowStage.Simulation).State.Status);
            Assert.Equal(PartOWorkflowStageStatus.NotRun, Row(partOWorkflowStatusGroup_Fresh, PartOWorkflowStage.Results).State.Status);

            //A run HAS produced results - the case the word was written for.
            PartOWorkflowWindow partOWorkflowWindow_Results = Window(partOWorkflowScenario, true);

            PartOWorkflowStatusGroup partOWorkflowStatusGroup_Results = partOWorkflowWindow_Results.StatusGroups[1];

            Assert.Equal(PartOWorkflowStatusGroup.Name_Run_Existing, partOWorkflowStatusGroup_Results.Name);
            Assert.Equal("Existing run / results", partOWorkflowStatusGroup_Results.Name);

            Assert.Equal(PartOWorkflowStageStatus.Ready, Row(partOWorkflowStatusGroup_Results, PartOWorkflowStage.Results).State.Status);

            //ONLY the heading moved: both groups hold the same stages in the same order, and the
            //configuration heading is untouched.
            Assert.Equal(
                partOWorkflowStatusGroup_Fresh.Rows.ConvertAll(x => x.State.Stage),
                partOWorkflowStatusGroup_Results.Rows.ConvertAll(x => x.State.Stage));

            Assert.Equal(PartOWorkflowStatusGroup.Name_Configuration, partOWorkflowWindow_Fresh.StatusGroups[0].Name);
            Assert.Equal(PartOWorkflowStatusGroup.Name_Configuration, partOWorkflowWindow_Results.StatusGroups[0].Name);
        }

        /// <summary>
        /// <b>No new status token is invented.</b> Every status word on screen is a
        /// <see cref="PartOWorkflowStageStatus"/> description, so the grouping cannot have introduced a
        /// "stale" or "out of date" state that nothing in the application can decide.
        /// </summary>
        [WpfFact]
        public void TheGroupedList_InventsNoStatusToken()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit));

            List<string> statusTexts = [];
            foreach (PartOWorkflowStageStatus partOWorkflowStageStatus in Enum.GetValues<PartOWorkflowStageStatus>())
            {
                statusTexts.Add(SAM.Core.Query.Description(partOWorkflowStageStatus));
            }

            foreach (PartOWorkflowStatusRow partOWorkflowStatusRow in partOWorkflowWindow.StatusRows)
            {
                Assert.Contains(partOWorkflowStatusRow.StatusText, statusTexts);
            }
        }

        /// <summary>
        /// <b>The Iteration 1b Part F line, shortened on screen and complete behind a disclosure.</b> The
        /// long four-sentence explanation is the one that proves no mechanical system was invented AND that
        /// System 1 provision was not sized either, so not one word of it may be lost - it moves to the
        /// row's own "Why is this N/A?" and to its tooltip.
        /// </summary>
        [WpfFact]
        public void OnIteration1b_ThePartFLineIsOneSentence_AndTheFullTextStaysAvailable()
        {
            PartOWorkflowScenario partOWorkflowScenario = PartOWorkflowScenario.Scenarios.Find(
                x => !x.SelectVentilationUnit && x.Option?.PartOVentilationMode == PartOVentilationMode.NaturalVentilation);

            Assert.NotNull(partOWorkflowScenario);

            PartOWorkflowWindow partOWorkflowWindow = Window(partOWorkflowScenario);

            PartOWorkflowStatusRow partOWorkflowStatusRow = partOWorkflowWindow.StatusRows.Find(x => x.State.Stage == PartOWorkflowStage.PartFRequirements);

            Assert.NotNull(partOWorkflowStatusRow);
            Assert.Equal(PartOWorkflowStageStatus.NotApplicable, partOWorkflowStatusRow.State.Status);

            //The token beside it already says N/A, so the sentence states the fact and does not repeat it.
            Assert.Equal("N/A", partOWorkflowStatusRow.StatusText);
            Assert.Equal("Continuous mechanical rates are not applied on the natural-ventilation route.", partOWorkflowStatusRow.ShortDetail);

            //Every word of the original, still there and still reachable.
            Assert.True(partOWorkflowStatusRow.HasMoreDetail);
            Assert.Equal("Why is this N/A?", partOWorkflowStatusRow.DisclosureHeader);
            Assert.Equal(System.Windows.Visibility.Visible, partOWorkflowStatusRow.DisclosureVisibility);

            Assert.Contains("was NOT applied", partOWorkflowStatusRow.FullDetail);
            Assert.Contains("System 1", partOWorkflowStatusRow.FullDetail);
            Assert.Contains("no mechanical system was invented", partOWorkflowStatusRow.FullDetail);
        }

        /// <summary>
        /// Where no shorter sentence was supplied, a row shortens itself by CUTTING its own detail at the
        /// first full stop - so the one-line form is always a prefix of what the inspection said and can
        /// never be a paraphrase of it.
        /// </summary>
        [Fact]
        public void AShortenedRow_IsAlwaysAPrefixOfWhatTheInspectionSaid()
        {
            PartOWorkflowStageState partOWorkflowStageState = new(
                PartOWorkflowStage.Simulation,
                PartOWorkflowStageStatus.NotRun,
                "An iteration is prepared and waiting. This second sentence is the detail a one-line row cannot carry.");

            PartOWorkflowStatusRow partOWorkflowStatusRow = new(partOWorkflowStageState);

            Assert.Equal("An iteration is prepared and waiting.", partOWorkflowStatusRow.ShortDetail);
            Assert.StartsWith(partOWorkflowStatusRow.ShortDetail, partOWorkflowStatusRow.FullDetail, StringComparison.Ordinal);
            Assert.True(partOWorkflowStatusRow.HasMoreDetail);

            //And a one-sentence detail is shown whole, with no disclosure offered for nothing.
            PartOWorkflowStatusRow partOWorkflowStatusRow_Short = new(new PartOWorkflowStageState(
                PartOWorkflowStage.Results,
                PartOWorkflowStageStatus.NotRun,
                "There are no results to review yet."));

            Assert.Equal("There are no results to review yet.", partOWorkflowStatusRow_Short.ShortDetail);
            Assert.False(partOWorkflowStatusRow_Short.HasMoreDetail);
            Assert.Equal(System.Windows.Visibility.Collapsed, partOWorkflowStatusRow_Short.DisclosureVisibility);
        }

        /// <summary>
        /// <b>And the grouped list really renders.</b> A nested <c>ItemsControl</c> whose inner template
        /// bound the wrong property would show empty groups and throw nothing at all - so the real template
        /// is loaded over a real group and laid out, and the text it paints is read back: the heading, every
        /// stage name in that group, and every one-line explanation.
        /// </summary>
        [WpfFact]
        public void TheGroupedList_RendersEachHeadingAndEveryRow()
        {
            PartOWorkflowWindow partOWorkflowWindow = Window(PartOWorkflowScenario.Scenarios.Find(x => x.SelectVentilationUnit));

            System.Windows.Controls.ItemsControl itemsControl = (System.Windows.Controls.ItemsControl)partOWorkflowWindow.FindName("itemsControl_Status");

            Assert.NotNull(itemsControl);
            Assert.NotNull(itemsControl.ItemTemplate);

            foreach (PartOWorkflowStatusGroup partOWorkflowStatusGroup in partOWorkflowWindow.StatusGroups)
            {
                System.Windows.FrameworkElement frameworkElement = (System.Windows.FrameworkElement)itemsControl.ItemTemplate.LoadContent();

                frameworkElement.DataContext = partOWorkflowStatusGroup;

                frameworkElement.Measure(new System.Windows.Size(900, 4000));
                frameworkElement.Arrange(new System.Windows.Rect(0, 0, 900, 4000));
                frameworkElement.UpdateLayout();

                List<string> texts = Texts(frameworkElement);

                Assert.Contains(partOWorkflowStatusGroup.Name, texts);

                foreach (PartOWorkflowStatusRow partOWorkflowStatusRow in partOWorkflowStatusGroup.Rows)
                {
                    Assert.Contains(partOWorkflowStatusRow.Name, texts);

                    //The status as drawn: a word AND a glyph, never colour alone.
                    Assert.Contains(partOWorkflowStatusRow.StatusLabel, texts);
                    Assert.Contains(partOWorkflowStatusRow.StatusGlyph, texts);
                    Assert.Contains(partOWorkflowStatusRow.ShortDetail, texts);

                    //And the inspection's complete sentence is in the row's own tree, behind Show details.
                    Assert.Contains(partOWorkflowStatusRow.FullDetail, texts);
                }
            }
        }

        // =================================================================================================
        // 3. Warnings: counted, grouped, and never abridged
        // =================================================================================================

        /// <summary>
        /// The header counts each kind that is present, and identical lines are collapsed with a
        /// <c>× N</c> - while the complete record still holds every line.
        /// </summary>
        [Fact]
        public void IdenticalWarnings_AreCollapsedForReading_AndKeptInFull()
        {
            PartODiagnosticSummary partODiagnosticSummary = new(
                ["A note."],
                ["Flat 1 Bedroom has no Part F requirement.", "Flat 1 Bedroom has no Part F requirement.", "Flat 2 Kitchen is unresolved."],
                []);

            Assert.Equal(3, partODiagnosticSummary.WarningCount);
            Assert.Equal(1, partODiagnosticSummary.NoteCount);
            Assert.Equal(0, partODiagnosticSummary.RefusalCount);

            Assert.Contains("Warnings (3)", partODiagnosticSummary.Header);
            Assert.Contains("Notes (1)", partODiagnosticSummary.Header);
            Assert.DoesNotContain("Refusals", partODiagnosticSummary.Header);

            Assert.True(partODiagnosticSummary.IsGrouped);
            Assert.Contains("× 2", partODiagnosticSummary.GroupedText);

            //The unique warning survives grouping, and so does the note.
            Assert.Contains("Flat 2 Kitchen is unresolved.", partODiagnosticSummary.GroupedText);
            Assert.Contains("A note.", partODiagnosticSummary.GroupedText);

            //And the complete record is every line, ungrouped.
            string[] lines = partODiagnosticSummary.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(4, lines.Length);
            Assert.DoesNotContain("×", partODiagnosticSummary.Text);
        }

        /// <summary>
        /// Two warnings that differ by a single character are two warnings. No space name is stripped, no
        /// pattern is extracted and nothing is normalised to manufacture a match.
        /// </summary>
        [Fact]
        public void WarningsThatDifferAtAll_AreNeverCollapsedTogether()
        {
            PartODiagnosticSummary partODiagnosticSummary = new(
                null,
                ["Flat 1 Bedroom is unresolved.", "Flat 2 Bedroom is unresolved."],
                null);

            Assert.False(partODiagnosticSummary.IsGrouped);
            Assert.Equal(partODiagnosticSummary.Text, partODiagnosticSummary.GroupedText);

            Assert.Contains("Flat 1 Bedroom is unresolved.", partODiagnosticSummary.GroupedText);
            Assert.Contains("Flat 2 Bedroom is unresolved.", partODiagnosticSummary.GroupedText);
        }

        /// <summary>
        /// Refusals keep their own count and stay first, so a refusal cannot be read as one of the warnings.
        /// </summary>
        [Fact]
        public void ARefusal_IsCountedAndReportedAsARefusal()
        {
            PartODiagnosticSummary partODiagnosticSummary = new(null, ["A warning."], ["A refusal."]);

            Assert.Contains("Refusals (1)", partODiagnosticSummary.Header);
            Assert.StartsWith("REFUSAL: A refusal.", partODiagnosticSummary.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Two identical refusals stay two refusals.</b> A refusal is the most consequential line this
        /// window carries, and a person counting them on screen has to be counting refusals rather than
        /// distinct texts - so the collapsing applies to warnings and to nothing else.
        /// </summary>
        [Fact]
        public void IdenticalRefusalsAndNotes_AreNeverCollapsed()
        {
            PartODiagnosticSummary partODiagnosticSummary = new(
                ["Same note.", "Same note."],
                null,
                ["Same refusal.", "Same refusal."]);

            Assert.Equal(2, partODiagnosticSummary.RefusalCount);
            Assert.Equal(2, partODiagnosticSummary.NoteCount);

            Assert.False(partODiagnosticSummary.IsGrouped);
            Assert.DoesNotContain("×", partODiagnosticSummary.GroupedText);

            Assert.Equal(2, Occurrences(partODiagnosticSummary.GroupedText, "REFUSAL: Same refusal."));
            Assert.Equal(2, Occurrences(partODiagnosticSummary.GroupedText, "NOTE: Same note."));
        }

        /// <summary>
        /// <b>Copy All copies every warning, whatever the box is showing.</b> The window shows the grouped
        /// view by default, the tick lists every line, and the clipboard text is the complete record in
        /// both states.
        /// </summary>
        [WpfFact]
        public void CopyAll_ExposesEveryRawWarning()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            partOPreparationWindow.SetDiagnostics(
                [],
                ["Duplicated warning.", "Duplicated warning.", "Unique warning."],
                ["A refusal."]);

            Assert.Contains("Refusals (1)", partOPreparationWindow.DiagnosticsHeader);
            Assert.Contains("Warnings (3)", partOPreparationWindow.DiagnosticsHeader);

            //Grouped on screen, and the tick is on the window because it has something to do.
            Assert.True(partOPreparationWindow.IsDiagnosticsGrouped);
            Assert.True(partOPreparationWindow.IsShowEveryLineOffered);
            Assert.False(partOPreparationWindow.ShowEveryLine);
            Assert.Contains("× 2", partOPreparationWindow.DiagnosticsText);

            //Complete in the record Copy All copies - three warning lines, not two.
            string full = partOPreparationWindow.DiagnosticsFullText;

            Assert.DoesNotContain("×", full);
            Assert.Equal(2, Occurrences(full, "WARNING: Duplicated warning."));
            Assert.Contains("WARNING: Unique warning.", full);
            Assert.Contains("REFUSAL: A refusal.", full);

            //And the tick puts the complete record on screen.
            partOPreparationWindow.ShowEveryLine = true;

            Assert.Equal(full, partOPreparationWindow.DiagnosticsText);
        }

        /// <summary>
        /// <b>Where nothing is duplicated the tick is not on the window at all.</b>
        /// <para>
        /// Hidden rather than greyed. A permanently disabled tick advertises a capability that never
        /// arrives and leaves the engineer working out why they cannot use it - which is exactly what
        /// happened in native acceptance, because on today's Part O warnings the collapsing is always a
        /// no-op (see the next test). The box shows every line anyway, and Copy All is complete.
        /// </para>
        /// </summary>
        [WpfFact]
        public void WithNoDuplicates_TheShowEveryLineTickIsNotOnTheWindow()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            partOPreparationWindow.SetDiagnostics(null, ["One warning."], null);

            Assert.False(partOPreparationWindow.IsDiagnosticsGrouped);
            Assert.False(partOPreparationWindow.IsShowEveryLineOffered);

            //Nothing was collapsed, so what is on screen already IS every line.
            Assert.Equal(partOPreparationWindow.DiagnosticsFullText, partOPreparationWindow.DiagnosticsText);
            Assert.Contains("WARNING: One warning.", partOPreparationWindow.DiagnosticsText);
        }

        /// <summary>
        /// <b>And that is the normal case for Part O, which is why the tick is hidden rather than greyed.</b>
        ///
        /// <para><b>Why no two Part O warnings are ever identical</b></para>
        /// <para>
        /// Every warning this window can receive names its space:
        /// <c>Modify.AddPartOBaseMVHRSystem</c>'s stale-relation warning names the space and the system,
        /// and <c>Query.ReconcileVentilationSystemDesignDuty</c>'s headroom and shortfall warnings name the
        /// space, the direction and both airflows. So a hundred flats produce a hundred distinct lines, and
        /// even one space's supply and extract lines differ. The two warnings below are shaped exactly like
        /// the real ones, and the collapsing does nothing to them - by design, and asserted here rather
        /// than assumed.
        /// </para>
        /// <para>
        /// <b>Nothing is stripped to make them match.</b> That is the whole point: collapsing by anything
        /// other than the complete raw string would mean deciding that one warning stands for another.
        /// </para>
        /// </summary>
        [WpfFact]
        public void RealPartOWarningsNameTheirSpace_SoTheTickNeverAppears()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            partOPreparationWindow.SetDiagnostics(
                null,
                [
                    "Space 'Flat 1 Bedroom': the design supply terminals total 13 l/s against the 11 l/s Approved Document F sized, so 2 l/s of that room's airflow is design headroom above the requirement.",
                    "Space 'Flat 2 Bedroom': the design supply terminals total 13 l/s against the 11 l/s Approved Document F sized, so 2 l/s of that room's airflow is design headroom above the requirement.",
                ],
                null);

            Assert.Equal(2, partOPreparationWindow.WarningCount);
            Assert.Contains("Warnings (2)", partOPreparationWindow.DiagnosticsHeader);

            //Two spaces, two lines, no "× 2" and no tick.
            Assert.False(partOPreparationWindow.IsDiagnosticsGrouped);
            Assert.False(partOPreparationWindow.IsShowEveryLineOffered);
            Assert.DoesNotContain("×", partOPreparationWindow.DiagnosticsText);

            Assert.Contains("Flat 1 Bedroom", partOPreparationWindow.DiagnosticsText);
            Assert.Contains("Flat 2 Bedroom", partOPreparationWindow.DiagnosticsText);
        }

        // =================================================================================================
        // 4. One spelling per concept
        // =================================================================================================

        /// <summary>
        /// The four scenario names, spelled once each - and the duplication the pass removed:
        /// "base MVHR (MVHR)".
        /// </summary>
        [Fact]
        public void TheScenarioNames_AreSpelledOnceAndNeverDuplicateTheRouteWord()
        {
            Assert.Equal("Iteration 1a — MVHR design duty (no manufacturer unit)", PartOWorkflowScenario.Text_Iteration1a);
            Assert.Equal("Iteration 1b — Natural ventilation (no mechanical system)", PartOWorkflowScenario.Text_Iteration1b);
            Assert.Equal("Iteration 2 — MVHR with manufacturer unit", PartOWorkflowScenario.Text_Iteration2);
            Assert.Equal("Iteration 2B — TM59 optimisation", PartOWorkflowScenario.Text_Iteration2B);

            //Both pickers use them, so the two windows cannot name the same scenario differently.
            List<string> texts_Scenario = PartOWorkflowScenario.Scenarios.ConvertAll(x => x.Text);

            Assert.Contains(PartOWorkflowScenario.Text_Iteration1a, texts_Scenario);
            Assert.Contains(PartOWorkflowScenario.Text_Iteration1b, texts_Scenario);
            Assert.Contains(PartOWorkflowScenario.Text_Iteration2, texts_Scenario);

            foreach (PartOVentilationStrategyOption partOVentilationStrategyOption in PartOVentilationStrategyOption.Options)
            {
                Assert.DoesNotContain("MVHR (MVHR)", partOVentilationStrategyOption.Text);
                Assert.DoesNotContain("(NV)", partOVentilationStrategyOption.Text);

                Assert.True(
                    partOVentilationStrategyOption.Text == PartOWorkflowScenario.Text_Iteration1a
                        || partOVentilationStrategyOption.Text == PartOWorkflowScenario.Text_Iteration1b,
                    partOVentilationStrategyOption.Text);
            }
        }

        /// <summary>
        /// <b>The review window's header states the route once.</b>
        /// <para>
        /// It used to print the settled mode and then the canonical word in brackets, which on the
        /// mechanical route read <c>MVHR (MVHR)</c> - flagged by native acceptance, and rightly.
        /// </para>
        /// <para>
        /// The two are the same statement, and the helper proves that by reading the canonical word back
        /// through SAM rather than assuming it. Where they somehow disagreed - which
        /// <c>Modify.PreparePartOIteration</c> refuses - both are still stated, because a header that hid
        /// that would be the far worse failure.
        /// </para>
        /// </summary>
        [Fact]
        public void TheRouteIsStatedOnce_AndADisagreementWouldStillStateBoth()
        {
            Assert.Equal("MVHR", Query.PartOVentilationRouteText(PartOVentilationMode.MVHR, "MVHR"));
            Assert.Equal("NV", Query.PartOVentilationRouteText(PartOVentilationMode.NaturalVentilation, "NV"));

            //The literal reading native acceptance flagged, gone.
            Assert.DoesNotContain("MVHR (MVHR)", Query.PartOVentilationRouteText(PartOVentilationMode.MVHR, "MVHR"));

            //Every option the UI can actually offer states its route in one word.
            foreach (PartOVentilationStrategyOption partOVentilationStrategyOption in PartOVentilationStrategyOption.Options)
            {
                string route = Query.PartOVentilationRouteText(partOVentilationStrategyOption.PartOVentilationMode, partOVentilationStrategyOption.VentilationStrategy);

                Assert.Equal(partOVentilationStrategyOption.VentilationStrategy, route);
                Assert.DoesNotContain("(", route);
            }

            //A disagreement is reported rather than hidden - and so is a route with no word handed in.
            Assert.Contains("the preparation settled on", Query.PartOVentilationRouteText(PartOVentilationMode.NaturalVentilation, "MVHR"));
            Assert.Equal(SAM.Core.Query.Description(PartOVentilationMode.MVHR), Query.PartOVentilationRouteText(PartOVentilationMode.MVHR, null));
        }

        /// <summary>
        /// <b>And the route word itself did not move.</b> Renaming what the picker SHOWS must not touch the
        /// canonical value the preparation reads, nor which iteration it is paired with.
        /// </summary>
        [Fact]
        public void RenamingThePickerText_MovedNoRouteWordAndNoIteration()
        {
            foreach (PartOVentilationStrategyOption partOVentilationStrategyOption in PartOVentilationStrategyOption.Options)
            {
                PartOVentilationMode partOVentilationMode = Analytical.Query.PartOVentilationMode(partOVentilationStrategyOption.VentilationStrategy, out string refusal);

                Assert.Null(refusal);
                Assert.Equal(partOVentilationStrategyOption.PartOVentilationMode, partOVentilationMode);
                Assert.Equal(partOVentilationMode, Analytical.Query.PartOIterationVentilationMode(partOVentilationStrategyOption.PartOIteration, out string _));
            }

            Assert.Equal("MVHR", PartOVentilationStrategyOption.Options.Find(x => x.PartOIteration == PartOIteration.BasePassive)?.VentilationStrategy);
            Assert.Equal("NV", PartOVentilationStrategyOption.Options.Find(x => x.PartOIteration == PartOIteration.BaseNaturalVentilation)?.VentilationStrategy);
        }

        /// <summary>
        /// <b>The project test product's identity, spelled unambiguously.</b> Its manufacturer field is the
        /// literal words "Project test", so a unit somebody called "test" joined to
        /// <c>Project test test</c> - which reads as a typo. The picker states the name and the provenance
        /// separately instead.
        /// </summary>
        [Fact]
        public void TheProjectTestProduct_IsLabelledUnambiguously()
        {
            PartOProjectTestVentilationUnit partOProjectTestVentilationUnit = new("test", 80, 80);

            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = Analytical.Query.CapacityDescriptors(partOProjectTestVentilationUnit);

            VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = Assert.Single(ventilationUnitCapacityDescriptors);

            Assert.Equal("test (project test) — 80 / 80 l/s", Query.PartOProductLabel(ventilationUnitCapacityDescriptor));

            //The identity itself is untouched - it is still the two fields a pool holds and an assignment
            //writes, and it still reads as "Project test test" through the analytical API.
            Assert.Equal("Project test", ventilationUnitCapacityDescriptor.VentilationUnitReference.Manufacturer);
            Assert.Equal("test", ventilationUnitCapacityDescriptor.VentilationUnitReference.Model);
            Assert.Equal("Project test test", ventilationUnitCapacityDescriptor.VentilationUnitReference.ToString());

            //A manufacturer product is spelled exactly as it always was.
            Assert.Equal(
                "Nuaire MRXBOXAB-ECO5-AECV (MR-ECO-COOL-V) — 150 / 150 l/s",
                Query.PartOProductLabel(new VentilationUnitCapacityDescriptor(MRXBOXReference(), 150, 150, 10)));
        }

        /// <summary>
        /// The catalogue row's Origin column is ordinary engineer-facing text rather than a shout, in both
        /// of its two states.
        /// </summary>
        [Fact]
        public void TheOriginColumn_IsOrdinaryText()
        {
            Assert.Equal("Catalogue", new PartOCatalogueProductRow(new VentilationUnitCapacityDescriptor(MRXBOXReference(), 150, 150, 10), true).Origin);
            Assert.Equal("Project test", new PartOCatalogueProductRow(new VentilationUnitCapacityDescriptor(MRXBOXReference(), 150, 150, 10), true, true).Origin);
        }

        /// <summary>
        /// Capacity and design columns carry their unit in the header, once - <c>Max SUP (l/s)</c> and
        /// <c>Design SUP (l/s)</c> - and the Part F column names the document rather than a system.
        /// </summary>
        [WpfFact]
        public void TheTableHeaders_CarryTheUnitOnceAndUseOneVocabulary()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            List<string> headers_Equipment = Headers(partOPreparationWindow, "dataGrid_Equipment");

            Assert.Contains("Design SUP (l/s)", headers_Equipment);
            Assert.Contains("Design EXT (l/s)", headers_Equipment);
            Assert.Contains("Max SUP (l/s)", headers_Equipment);
            Assert.Contains("Max EXT (l/s)", headers_Equipment);
            Assert.Contains("SUP headroom (l/s)", headers_Equipment);
            Assert.Contains("EXT headroom (l/s)", headers_Equipment);

            List<string> headers_Spaces = Headers(partOPreparationWindow, "dataGrid_Spaces");

            Assert.Contains("Part F required (l/s)", headers_Spaces);
            Assert.Contains("Design SUP (l/s)", headers_Spaces);
            Assert.Contains("Design EXT (l/s)", headers_Spaces);

            //No column repeats a unit in its own body, and none uses the old two-line spelling.
            foreach (string header in headers_Equipment)
            {
                Assert.DoesNotContain("\n", header);
            }
        }

        /// <summary>
        /// Every numeric column in both tables is right-aligned, and the text columns are not.
        /// </summary>
        [WpfFact]
        public void NumericColumns_AreRightAligned()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            foreach (string name in new[] { "dataGrid_Equipment", "dataGrid_Spaces" })
            {
                System.Windows.Controls.DataGrid dataGrid = (System.Windows.Controls.DataGrid)partOPreparationWindow.FindName(name);

                foreach (System.Windows.Controls.DataGridColumn dataGridColumn in dataGrid.Columns)
                {
                    if (dataGridColumn is not System.Windows.Controls.DataGridTextColumn dataGridTextColumn)
                    {
                        continue;
                    }

                    string header = dataGridColumn.Header as string;

                    bool numeric = header is not null && header.Contains("(l/s)");

                    Assert.Equal(numeric, IsRightAligned(dataGridTextColumn.ElementStyle));
                }
            }
        }

        /// <summary>
        /// <b>The group separator is a border, and nothing more than a border.</b>
        /// <para>
        /// An explicit <c>CellStyle</c> replaces the implicit style, so the thing worth pinning is that this
        /// one adds two setters and takes nothing away: no <c>Template</c>, no <c>Background</c>, no
        /// <c>Foreground</c>. A separator that quietly replaced the cell's chrome - or its selection
        /// highlight - would be a far worse table than one with no separators at all.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheGroupSeparatorStyle_AddsABorderAndTakesNothingAway()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            System.Windows.Style style = (System.Windows.Style)partOPreparationWindow.FindResource("Style_GroupCell");

            Assert.NotNull(style);
            Assert.Equal(typeof(System.Windows.Controls.DataGridCell), style.TargetType);

            List<System.Windows.DependencyProperty> dependencyProperties = [];

            foreach (System.Windows.SetterBase setterBase in style.Setters)
            {
                System.Windows.Setter setter = Assert.IsType<System.Windows.Setter>(setterBase);

                dependencyProperties.Add(setter.Property);
            }

            Assert.Equal(
                new[]
                {
                    System.Windows.Controls.Control.BorderThicknessProperty,
                    System.Windows.Controls.Control.BorderBrushProperty,
                },
                dependencyProperties);

            //And the columns that carry it are exactly the three concepts' first columns - Design, Max and
            //headroom - so the table reads as three pairs rather than as six loose numbers.
            List<string> headers = [];

            System.Windows.Controls.DataGrid dataGrid = (System.Windows.Controls.DataGrid)partOPreparationWindow.FindName("dataGrid_Equipment");

            foreach (System.Windows.Controls.DataGridColumn dataGridColumn in dataGrid.Columns)
            {
                if (dataGridColumn is System.Windows.Controls.DataGridTextColumn dataGridTextColumn
                    && ReferenceEquals(dataGridTextColumn.CellStyle, style))
                {
                    headers.Add(dataGridColumn.Header as string);
                }
            }

            Assert.Equal(new[] { "Design SUP (l/s)", "Max SUP (l/s)", "SUP headroom (l/s)" }, headers);
        }

        /// <summary>
        /// <b>The whole assignment row fits the window it opens in.</b>
        ///
        /// <para><b>The regression this pins</b></para>
        /// <para>
        /// Moving the unit into each heading - <c>Design supply / l/s</c> on two lines becoming
        /// <c>Design SUP (l/s)</c> on one - made four columns wider, and the fixed widths went from 1145px
        /// to 1205px inside a 1140px window. The review opened horizontally scrolled with the
        /// <c>Status</c> column, the one that says whether a dwelling's product will do, off the right
        /// edge. A table an engineer has to scroll before it answers its own question is worse than the
        /// two-line headings it replaced.
        /// </para>
        /// <para>
        /// So: one star-sized column absorbs the slack, every other column is sized for its own header,
        /// and the fixed total has to leave room for the star column's minimum inside the default width -
        /// after window chrome, the grid margins and a vertical scrollbar, which is what the allowance
        /// below stands for. Asserted arithmetically rather than by rendering, because a DataGrid will not
        /// lay a row out offscreen.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheAssignmentRow_FitsTheDefaultWindowWidth()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            System.Windows.Controls.DataGrid dataGrid = (System.Windows.Controls.DataGrid)partOPreparationWindow.FindName("dataGrid_Equipment");

            Assert.NotNull(dataGrid);

            double width_Fixed = 0;
            double width_StarMinimum = 0;
            int count_Star = 0;

            foreach (System.Windows.Controls.DataGridColumn dataGridColumn in dataGrid.Columns)
            {
                if (dataGridColumn.Width.IsStar)
                {
                    count_Star++;

                    width_StarMinimum += dataGridColumn.MinWidth;

                    continue;
                }

                width_Fixed += dataGridColumn.Width.DisplayValue;
            }

            //At least one column has to be flexible, or widening the window buys empty space on the right
            //while the row still cannot fit when it is narrowed.
            Assert.True(count_Star >= 1, "The assignment grid needs a flexible column.");

            //Window chrome (two resize borders), the Grid's 10px margins and the DataGrid's own vertical
            //scrollbar, which is present the moment there are more dwellings than rows on screen.
            const double allowance = 16 + 20 + 17;

            Assert.True(
                width_Fixed + width_StarMinimum + allowance <= partOPreparationWindow.Width,
                string.Format(
                    "The assignment row needs {0}px ({1} fixed + {2} minimum for {3} flexible column(s)) plus {4}px of chrome, but the window opens at {5}px - so the Status column would open off-screen.",
                    width_Fixed + width_StarMinimum + allowance,
                    width_Fixed,
                    width_StarMinimum,
                    count_Star,
                    allowance,
                    partOPreparationWindow.Width));

            //The space table is narrower than the assignment table and must stay so - it has no flexible
            //column and would scroll on its own if it grew past the window.
            System.Windows.Controls.DataGrid dataGrid_Spaces = (System.Windows.Controls.DataGrid)partOPreparationWindow.FindName("dataGrid_Spaces");

            double width_Spaces = 0;

            foreach (System.Windows.Controls.DataGridColumn dataGridColumn in dataGrid_Spaces.Columns)
            {
                width_Spaces += dataGridColumn.Width.IsStar ? dataGridColumn.MinWidth : dataGridColumn.Width.DisplayValue;
            }

            Assert.True(
                width_Spaces + allowance <= partOPreparationWindow.Width,
                string.Format("The space table needs {0}px but the window opens at {1}px.", width_Spaces + allowance, partOPreparationWindow.Width));
        }

        /// <summary>
        /// The Part O family says <b>TAS</b>, and the conversion dialog says what it does rather than
        /// "TBD".
        /// <para>
        /// <b>"Convert to TAS", not "and simulate."</b> On the ordinary route the Simulate box is the
        /// user's and may be left unticked while SAP or the domestic-overheating XML is selected, so the
        /// conversion is the only thing this title can promise. The Part O route's own title claims the
        /// simulation, and there it is locked on - see
        /// <c>PartOSimulateDefaultsTests.PartO_TheDialogSaysWhichRouteItIs</c>.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheProductName_IsSpelledTAS()
        {
            SimulateWindow simulateWindow = new();

            Assert.Equal("Convert to TAS", simulateWindow.Title);
            Assert.DoesNotContain("simulate", simulateWindow.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TBD", simulateWindow.Title, StringComparison.Ordinal);

            Assert.DoesNotContain("Tas", new PartOPreparationWindow().Title, StringComparison.Ordinal);
            Assert.DoesNotContain("Tas", new PartOTM59ResultWindow().Title, StringComparison.Ordinal);
            Assert.DoesNotContain("Tas", new PartOOptimisationResultWindow().Title, StringComparison.Ordinal);
        }

        // =================================================================================================
        // 5. Table hygiene: an absence shown as an absence, and a live selection count
        // =================================================================================================

        /// <summary>
        /// <b>A missing figure is an em dash, and the value underneath is still NaN.</b> A space that was
        /// never sized carries no Approved Document F requirement, which is not zero - so the cell must not
        /// say "0.0", and it must not say "NaN" either.
        /// </summary>
        [Fact]
        public void AMissingFigure_IsShownAsAnEmDash_AndTheValueIsUnchanged()
        {
            PartOAirFlowConverter partOAirFlowConverter = new();

            Assert.Equal("—", partOAirFlowConverter.Convert(double.NaN, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("—", partOAirFlowConverter.Convert(double.PositiveInfinity, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("—", partOAirFlowConverter.Convert(null, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));

            //A stated figure is formatted, and zero stays zero - it is a rate, not an absence.
            Assert.Equal("12.3", partOAirFlowConverter.Convert(12.34, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("0.0", partOAirFlowConverter.Convert(0d, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));

            //And the row's own value is untouched by any of it.
            Space space = new("Bedroom", null);

            PartOSpaceRow partOSpaceRow = new(space);

            Assert.True(double.IsNaN(partOSpaceRow.PartFRequired_Lps));
            Assert.Equal("—", partOAirFlowConverter.Convert(partOSpaceRow.PartFRequired_Lps, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));

            //A space with no requirement says why, rather than leaving a blank cell unexplained - and it
            //reports the absent RECORD rather than judging the space. Nothing here reads a space type, so
            //a habitable room that AddVent PartF was never run over must not be told its type is exempt.
            Assert.Equal("No continuous Part F requirement is recorded for this space.", partOSpaceRow.PartFRequiredDescription);

            Assert.DoesNotContain("space type", partOSpaceRow.PartFRequiredDescription);
        }

        /// <summary>
        /// <b>Copy All renders a missing value exactly as the grid does.</b>
        ///
        /// <para><b>The regression this pins</b></para>
        /// <para>
        /// The cells went through the converter and the export did not, so an absent airflow painted as an
        /// em dash on screen and pasted as <c>NaN</c> - up to four times per equipment row on an
        /// Iteration 1a or 1b table, where no product is selected and the maximum and both headroom
        /// figures are all absent. This text is pasted into an issue, a report or an email, so a table
        /// that disagrees with the one that was reviewed is the one thing it must not be.
        /// </para>
        /// <para>
        /// Both now call <c>PartOAirFlowConverter.Text</c>, and this asserts the export against the
        /// converter's own output rather than against a repeated literal.
        /// </para>
        /// </summary>
        [WpfFact]
        public void CopyAll_RendersAMissingValueTheSameWayTheGridDoes()
        {
            //An Iteration 1a row: a real design duty, and no product at all - so the maximum and both
            //headroom figures are absent. Plus a space that AddVent PartF never sized.
            PartOPreparationWindow partOPreparationWindow = new()
            {
                EquipmentRows = [new PartOEquipmentRow("MVHR-01", "Flat 1 MVHR", 30, 30, null)],
                SpaceRows = [new PartOSpaceRow(new Space("Bedroom", null))],
            };

            partOPreparationWindow.SetDiagnostics(null, null, null);

            string text = partOPreparationWindow.CopyAllText();

            //The failure, stated directly.
            Assert.DoesNotContain("NaN", text, StringComparison.Ordinal);

            //And the absence is present as an absence - the same string the converter gives the cell.
            PartOAirFlowConverter partOAirFlowConverter = new();

            string unresolved = (string)partOAirFlowConverter.Convert(double.NaN, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);

            Assert.Equal(PartOAirFlowConverter.Unresolved, unresolved);
            Assert.Contains(unresolved, text);

            //The figures that DO exist are still figures, and still formatted as the cells format them.
            string designSupply = (string)partOAirFlowConverter.Convert(30d, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);

            Assert.Contains(designSupply, text);

            //Every airflow in both tables agrees with the converter, cell for cell.
            foreach (PartOEquipmentRow partOEquipmentRow in partOPreparationWindow.EquipmentRows)
            {
                foreach (double value_Lps in new[]
                {
                    partOEquipmentRow.DesignSupplyDuty_Lps,
                    partOEquipmentRow.DesignExtractDuty_Lps,
                    partOEquipmentRow.MaximumSupply_Lps,
                    partOEquipmentRow.MaximumExtract_Lps,
                    partOEquipmentRow.SupplyHeadroom_Lps,
                    partOEquipmentRow.ExtractHeadroom_Lps,
                })
                {
                    Assert.Contains(PartOAirFlowConverter.Text(value_Lps), text);
                }
            }

            foreach (PartOSpaceRow partOSpaceRow in partOPreparationWindow.SpaceRows)
            {
                Assert.Contains(PartOAirFlowConverter.Text(partOSpaceRow.PartFRequired_Lps), text);
            }

            //The underlying values are untouched by any of it - this is formatting, not substitution.
            Assert.True(double.IsNaN(partOPreparationWindow.EquipmentRows[0].MaximumSupply_Lps));
            Assert.True(double.IsNaN(partOPreparationWindow.SpaceRows[0].PartFRequired_Lps));
            Assert.Equal(30, partOPreparationWindow.EquipmentRows[0].DesignSupplyDuty_Lps);
        }

        /// <summary>
        /// An unresolved dwelling stays an em dash and is never guessed from a space's name - the existing
        /// precedence, restated here because the display conversion above must not be mistaken for it.
        /// </summary>
        [Fact]
        public void AnUnresolvedDwelling_StaysAnEmDash()
        {
            Assert.Equal("—", new PartOSpaceRow(new Space("Bedroom", null)).Dwelling);
            Assert.Equal("Flat 1", new PartOSpaceRow(new Space("Bedroom", null), "Flat 1").Dwelling);
        }

        /// <summary>
        /// <b>The selection count is live and is a proportion.</b> "3 of 412 dwellings selected" says both
        /// what a bulk assignment would touch and how much of the project that is.
        /// </summary>
        [WpfFact]
        public void TheSelectionCount_IsLiveAndStatesTheWhole()
        {
            PartOPreparationWindow partOPreparationWindow = new()
            {
                EquipmentAssignmentSet = ManualSet(),
            };

            Assert.Equal("0 of 3 dwellings selected", partOPreparationWindow.BulkSelectionDescription);
            Assert.False(partOPreparationWindow.CanApplyToSelected);

            partOPreparationWindow.SelectEquipmentRows([DwellingGuid("Flat 1"), DwellingGuid("Flat 2")]);

            Assert.Equal("2 of 3 dwellings selected", partOPreparationWindow.BulkSelectionDescription);

            //Still not offered: the prerequisites are BOTH a selection and a chosen product.
            Assert.False(partOPreparationWindow.CanApplyToSelected);

            partOPreparationWindow.BulkProduct = partOPreparationWindow.BulkProducts.Find(x => x.VentilationUnitReference.Model == model_XBC15);

            Assert.True(partOPreparationWindow.CanApplyToSelected);

            partOPreparationWindow.SelectEquipmentRows([]);

            Assert.Equal("0 of 3 dwellings selected", partOPreparationWindow.BulkSelectionDescription);
            Assert.False(partOPreparationWindow.CanApplyToSelected);
        }

        /// <summary>
        /// <b>A bulk assignment says what it did.</b> Twelve rows changing off the top of a scrolled table
        /// is otherwise indistinguishable from a click that did nothing - so the window states the product
        /// and the count, inline, and states nothing about airflow because none moved.
        /// </summary>
        [WpfFact]
        public void ABulkAssignment_ConfirmsWhatItDid()
        {
            PartOEquipmentAssignmentSet partOEquipmentAssignmentSet = ManualSet();

            PartOPreparationWindow partOPreparationWindow = new()
            {
                EquipmentAssignmentSet = partOEquipmentAssignmentSet,
            };

            Assert.Equal(string.Empty, partOPreparationWindow.BulkConfirmation);

            partOPreparationWindow.SelectEquipmentRows([DwellingGuid("Flat 1"), DwellingGuid("Flat 2")]);
            partOPreparationWindow.BulkProduct = partOPreparationWindow.BulkProducts.Find(x => x.VentilationUnitReference.Model == model_XBC15);

            Assert.True(partOPreparationWindow.ApplyToSelected());

            Assert.Equal("Assigned Nuaire XBC15 to 2 dwellings.", partOPreparationWindow.BulkConfirmation);

            //Presentation feedback only: the assignments are the set's, and the design duties did not move.
            Assert.Equal(model_XBC15, partOEquipmentAssignmentSet.Assignments.Find(x => x.DwellingName == "Flat 1").VentilationUnitReference.Model);
            Assert.Equal(model_MRXBOX, partOEquipmentAssignmentSet.Assignments.Find(x => x.DwellingName == "Flat 3").VentilationUnitReference.Model);

            Assert.Equal(23, partOEquipmentAssignmentSet.Assignments.Find(x => x.DwellingName == "Flat 1").DesignSupplyDuty_Lps);
            Assert.Equal(63, partOEquipmentAssignmentSet.Assignments.Find(x => x.DwellingName == "Flat 3").DesignSupplyDuty_Lps);

            //And it is cleared the moment it would be read as being about a different selection.
            partOPreparationWindow.SelectEquipmentRows([DwellingGuid("Flat 3")]);

            Assert.Equal(string.Empty, partOPreparationWindow.BulkConfirmation);
        }

        /// <summary>
        /// A single dwelling is confirmed in the singular, so the message never reads "1 dwellings".
        /// </summary>
        [WpfFact]
        public void ASingleDwellingAssignment_IsConfirmedInTheSingular()
        {
            PartOPreparationWindow partOPreparationWindow = new()
            {
                EquipmentAssignmentSet = ManualSet(),
            };

            partOPreparationWindow.SelectEquipmentRows([DwellingGuid("Flat 1")]);
            partOPreparationWindow.BulkProduct = partOPreparationWindow.BulkProducts.Find(x => x.VentilationUnitReference.Model == model_XBC15);

            Assert.True(partOPreparationWindow.ApplyToSelected());

            Assert.Equal("Assigned Nuaire XBC15 to 1 dwelling.", partOPreparationWindow.BulkConfirmation);
        }

        // =================================================================================================
        // Fixtures
        // =================================================================================================

        /// <summary>Every string a laid-out element tree actually paints.</summary>
        private static List<string> Texts(System.Windows.DependencyObject dependencyObject)
        {
            List<string> result = [];

            Collect(dependencyObject, result);

            return result;
        }

        private static void Collect(System.Windows.DependencyObject dependencyObject, List<string> texts)
        {
            if (dependencyObject is System.Windows.Controls.TextBlock textBlock)
            {
                texts.Add(textBlock.Text);
            }

            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(dependencyObject);

            for (int i = 0; i < count; i++)
            {
                Collect(System.Windows.Media.VisualTreeHelper.GetChild(dependencyObject, i), texts);
            }
        }

        private static bool IsRightAligned(System.Windows.Style style)
        {
            while (style is not null)
            {
                foreach (System.Windows.SetterBase setterBase in style.Setters)
                {
                    if (setterBase is System.Windows.Setter setter
                        && setter.Property == System.Windows.FrameworkElement.HorizontalAlignmentProperty
                        && Equals(setter.Value, System.Windows.HorizontalAlignment.Right))
                    {
                        return true;
                    }
                }

                style = style.BasedOn;
            }

            return false;
        }

        private static List<string> Headers(System.Windows.Window window, string name)
        {
            System.Windows.Controls.DataGrid dataGrid = (System.Windows.Controls.DataGrid)window.FindName(name);

            Assert.NotNull(dataGrid);

            List<string> result = [];

            foreach (System.Windows.Controls.DataGridColumn dataGridColumn in dataGrid.Columns)
            {
                if (dataGridColumn.Header is string header)
                {
                    result.Add(header);
                }
            }

            return result;
        }

        private static int Occurrences(string text, string value)
        {
            int result = 0;

            int index = text.IndexOf(value, StringComparison.Ordinal);

            while (index >= 0)
            {
                result++;

                index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
            }

            return result;
        }

        /// <summary>One group's row for a stage, or a failure naming the stage that was missing.</summary>
        private static PartOWorkflowStatusRow Row(PartOWorkflowStatusGroup partOWorkflowStatusGroup, PartOWorkflowStage partOWorkflowStage)
        {
            PartOWorkflowStatusRow result = partOWorkflowStatusGroup.Rows.Find(x => x.State.Stage == partOWorkflowStage);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>Prepare &amp; Run over a real catalogue, in a stated scenario.</summary>
        /// <param name="resultsAvailable">
        /// Whether the session has results to review - the capability the run heading reads.
        /// </param>
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
                VentilationUnitCatalogue = Catalogue(),
                Capabilities = new PartOWorkflowCapabilities { EquipmentAvailable = true, ResultsAvailable = resultsAvailable },
            };

            result.Restore(partOWorkflowScenario, PartOWorkflowScope.AllDwellings, null);

            result.CompleteInitialisation();

            return result;
        }

        private static VentilationUnitReference MRXBOXReference()
        {
            return new VentilationUnitReference("Nuaire", model_MRXBOX, "MR-ECO-COOL-V");
        }

        private static VentilationUnitReference XBC15Reference()
        {
            return new VentilationUnitReference("Nuaire", model_XBC15, null);
        }

        /// <summary>
        /// The accepted fixture's shape under MANUAL authority: three dwellings at 23/23, 63/63 and 63/63
        /// l/s, each already holding the MRXBOX.
        /// </summary>
        private static PartOEquipmentAssignmentSet ManualSet()
        {
            return new PartOEquipmentAssignmentSet(
                [
                    Assignment("Flat 1", 23, 23),
                    Assignment("Flat 2", 63, 63),
                    Assignment("Flat 3", 63, 63),
                ],
                [
                    new VentilationUnitCapacityDescriptor(MRXBOXReference(), 150, 150, 10),
                    new VentilationUnitCapacityDescriptor(XBC15Reference(), 190, 190, 20),
                ],
                new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));
        }

        private static PartOEquipmentAssignment Assignment(string dwelling, double supplyDuty_Lps, double extractDuty_Lps)
        {
            return new PartOEquipmentAssignment(
                DwellingGuid(dwelling),
                string.Format("MVHR-{0}", dwelling),
                string.Format("{0} MVHR", dwelling),
                dwelling,
                supplyDuty_Lps,
                extractDuty_Lps,
                MRXBOXReference());
        }

        /// <summary>A stable guid per dwelling name, so the fixtures are addressable and repeatable.</summary>
        private static Guid DwellingGuid(string dwelling)
        {
            byte[] bytes = new byte[16];

            for (int i = 0; i < dwelling.Length && i < 16; i++)
            {
                bytes[i] = (byte)dwelling[i];
            }

            return new Guid(bytes);
        }

        /// <summary>
        /// The two shipped products, written to a temporary directory and read back through the production
        /// reader - so a window is given a real catalogue rather than a stub.
        /// </summary>
        private static VentilationUnitCatalogue Catalogue()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_ConsistencyPolish_{0}", Guid.NewGuid()));

            Directory.CreateDirectory(directory);

            File.WriteAllText(Path.Combine(directory, "VentilationUnitCatalogue.JSON"), """
            {
              "Schema": "VentilationUnitCatalogue:v1",
              "Templates": [
                {
                  "_type": "SAM.Analytical.VentilationUnitTemplate,SAM.Analytical",
                  "Name": "Nuaire MRXBOXAB-ECO5-AECV (MR-ECO-COOL-V)",
                  "VentilationUnitReference": {
                    "_type": "SAM.Analytical.VentilationUnitReference,SAM.Analytical",
                    "Name": "Nuaire MRXBOXAB-ECO5-AECV",
                    "Manufacturer": "Nuaire",
                    "Model": "MRXBOXAB-ECO5-AECV",
                    "Reference": "MR-ECO-COOL-V"
                  },
                  "Source": "Written by this test at the shipped catalogue's published capacity. A template needs a traceable source to be valid at all.",
                  "MaximumSupplyFlowRate_Lps": 150,
                  "MaximumExtractFlowRate_Lps": 150,
                  "Rank": 10
                },
                {
                  "_type": "SAM.Analytical.VentilationUnitTemplate,SAM.Analytical",
                  "Name": "Nuaire XBOXER XBC15",
                  "VentilationUnitReference": {
                    "_type": "SAM.Analytical.VentilationUnitReference,SAM.Analytical",
                    "Name": "Nuaire XBC15",
                    "Manufacturer": "Nuaire",
                    "Model": "XBC15"
                  },
                  "Source": "Written by this test at the shipped catalogue's published capacity. A template needs a traceable source to be valid at all.",
                  "MaximumSupplyFlowRate_Lps": 190,
                  "MaximumExtractFlowRate_Lps": 190,
                  "Rank": 20
                }
              ]
            }
            """);

            return VentilationUnitCatalogue.Read(directory);
        }
    }
}
