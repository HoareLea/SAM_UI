// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The mixed Part O design matrix and its rules, without the window: dwelling rows, Suggested vs Selected, bulk
    /// assignment under project constraints, and reopening with honest staleness.
    /// </summary>
    public class PartOMixedDesignSessionTests
    {
        // ---- Matrix -------------------------------------------------------------------------------------------------

        [Fact]
        public void Rows_AreDwellingsOnly_InNaturalOrder_AndCommonSpacesAreNotStrategyRows()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(12, corridor: true, category: "Block");

            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);

            Assert.Equal(12, partOMixedDesignSession.Rows.Count);
            Assert.DoesNotContain(partOMixedDesignSession.Rows, x => x.Name == PartOMixedDesignFixture.Corridor);
            Assert.Equal(1, partOMixedDesignSession.CommonZoneCount);

            //"Flat 02" before "Flat 10", and a row per dwelling, with its space count.
            Assert.Equal(Enumerable.Range(1, 12).Select(x => string.Format("Flat {0:00}", x)), partOMixedDesignSession.Rows.Select(x => x.Name));
            Assert.All(partOMixedDesignSession.Rows, x => Assert.Equal(2, x.SpaceCount));
            Assert.Equal(["Block A", "Block B"], partOMixedDesignSession.Rows.Select(x => x.Group).Distinct().OrderBy(x => x));

            Assert.True(PartOMixedDesignSession.NaturalCompare("Flat 2", "Flat 10") < 0);
        }

        [Fact]
        public void Filters_AndSearch_NarrowTheRows()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedDesignFixture.Baseline(4), null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            partOMixedDesignSession.SetNatural([rows[0], rows[1]]);

            Assert.Equal(2, rows.Count(x => PartOMixedDesignSession.Matches(x, PartOMixedDwellingFilter.NotSelected, null)));
            Assert.Equal(["Flat 03"], rows.Where(x => PartOMixedDesignSession.Matches(x, PartOMixedDwellingFilter.All, "03")).Select(x => x.Name));
            Assert.Empty(rows.Where(x => PartOMixedDesignSession.Matches(x, PartOMixedDwellingFilter.FailingFinal, null)));

            //A constraint changed after selection: the selection is flagged, never silently rewritten.
            partOMixedDesignSession.Constraints.NaturalVentilationAllowed = false;
            partOMixedDesignSession.Refresh();

            Assert.Equal(2, rows.Count(x => PartOMixedDesignSession.Matches(x, PartOMixedDwellingFilter.NeedsAttention, null)));
            Assert.Equal(PartOVentilationMode.NaturalVentilation, rows[0].Selected.VentilationMode);
            Assert.False(partOMixedDesignSession.Readiness().CanBuild);
        }

        [Fact]
        public void Readiness_CountsTheSelectedDesign_AndBlocksMissingSelections()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedDesignFixture.Baseline(4), null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            partOMixedDesignSession.SetNatural([rows[0]]);
            partOMixedDesignSession.SetMvhr([rows[1], rows[2]], null);

            PartOMixedReadiness partOMixedReadiness = partOMixedDesignSession.Readiness();
            Assert.Equal(4, partOMixedReadiness.DwellingCount);
            Assert.Equal(1, partOMixedReadiness.Natural);
            Assert.Equal(2, partOMixedReadiness.Mvhr);
            Assert.Equal(1, partOMixedReadiness.NotSelected);
            Assert.False(partOMixedReadiness.CanBuild);
            Assert.Contains("1 not selected", partOMixedReadiness.Text);

            partOMixedDesignSession.SetMvhr([rows[3]], null);
            Assert.True(partOMixedDesignSession.Readiness().CanBuild);
        }

        [Fact]
        public void SamRefusals_AttachToTheirDwellings_AndClearWhenThatDwellingIsEdited()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(3);
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            partOMixedDesignSession.SetNatural([rows[0], rows[1]]);

            //Check design: SAM refuses the unselected Flat 03, by zone.
            PartOMaterialisation partOMaterialisation = partOMixedDesignSession.WithSelection().MaterialisePartODwellingStrategies();
            Assert.False(partOMaterialisation.IsMaterialised);
            partOMixedDesignSession.SetRefusals(partOMaterialisation.Refusals);

            Assert.True(rows[2].NeedsAttention);
            Assert.Contains(Core.Query.Description(PartOMaterialisationRefusalReason.MissingStrategy), rows[2].Attention);
            Assert.False(rows[0].NeedsAttention);
            Assert.Equal([rows[2]], rows.Where(x => PartOMixedDesignSession.Matches(x, PartOMixedDwellingFilter.NeedsAttention, null)));

            //Fixed: the refusal answered the previous selection, so it goes, and the design can be built.
            partOMixedDesignSession.SetMvhr([rows[2]], null);
            Assert.False(rows[2].NeedsAttention);
            Assert.True(partOMixedDesignSession.Readiness().CanBuild);
        }

        // ---- Suggested vs Selected ------------------------------------------------------------------------------

        [Fact]
        public void Screening_NeverSilentlyOverwritesTheSelection_AndApplySuggestionsIsExplicit()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(3);
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            //A manual override: Flat 01 forced to MVHR although it will pass naturally.
            partOMixedDesignSession.SetMvhr([rows[0]], null);

            PartOScreeningEvidence natural = Evidence(partOMixedDesignSession, PartOScreeningStrategy.Natural, (rows[0], PartODwellingOutcome.Pass), (rows[1], PartODwellingOutcome.Pass), (rows[2], PartODwellingOutcome.Fail));
            partOMixedDesignSession.ApplyScreening([natural]);

            //Screening arrived: nothing selected changed.
            Assert.Equal(PartOVentilationMode.MVHR, rows[0].Selected.VentilationMode);
            Assert.Null(rows[1].Selected);
            Assert.Null(rows[2].Selected);

            //Suggested and selected are separate, and visibly so.
            Assert.Equal("Natural ventilation", rows[0].SuggestionText);
            Assert.StartsWith("MVHR", rows[0].SelectedText);
            Assert.True(rows[0].SuggestionDiffers);
            Assert.Equal("—", rows[2].SuggestionText);

            //Apply suggestions shows what would change, and changes nothing until applied.
            List<PartOMixedSelectionChange> changes = partOMixedDesignSession.SuggestionChanges([rows[1], rows[2]]);
            PartOMixedSelectionChange change = Assert.Single(changes);
            Assert.Same(rows[1], change.Row);
            Assert.Null(rows[1].Selected);

            partOMixedDesignSession.Apply(changes);
            Assert.Equal(PartOVentilationMode.NaturalVentilation, rows[1].Selected.VentilationMode);

            //The manual override is preserved: it was not in the applied set.
            Assert.Equal(PartOVentilationMode.MVHR, rows[0].Selected.VentilationMode);
        }

        [Fact]
        public void Suggestion_FollowsLeastIntervention_FilteredByConstraints_AndNeverReadsNotRunAsPass()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedDesignFixture.Baseline(3), null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            PartOScreeningEvidence natural = Evidence(partOMixedDesignSession, PartOScreeningStrategy.Natural, (rows[0], PartODwellingOutcome.Pass), (rows[1], PartODwellingOutcome.Fail));
            PartOScreeningEvidence mvhr = Evidence(partOMixedDesignSession, PartOScreeningStrategy.MechanicalBaseline, (rows[0], PartODwellingOutcome.Pass), (rows[1], PartODwellingOutcome.Pass));
            partOMixedDesignSession.ApplyScreening([mvhr, natural]);

            Assert.Equal(PartOScreeningStrategy.Natural, rows[0].Suggestion.Strategy);
            Assert.Equal(PartOScreeningStrategy.MechanicalBaseline, rows[1].Suggestion.Strategy);

            //Flat 03 was not assessed by either run: NOT RUN, and no suggestion.
            Assert.Equal("NOT RUN", rows[2].Screening_Natural);
            Assert.False(rows[2].Suggestion.CanApply);

            //Unscreenable strategies say so; they are not NOT RUN.
            Assert.Equal("UNAVAILABLE", rows[0].Screening_Optimised);
            Assert.Equal("UNAVAILABLE", rows[0].Screening_ActiveCooling);

            //The project requires mechanical ventilation: Natural passed, but MVHR is suggested - and the reason says why.
            partOMixedDesignSession.Constraints.NaturalVentilationAllowed = false;
            partOMixedDesignSession.Refresh();

            Assert.Equal(PartOScreeningStrategy.MechanicalBaseline, rows[0].Suggestion.Strategy);
            Assert.Contains("requires mechanical ventilation", rows[0].SuggestionReason);
        }

        [Fact]
        public void StaleScreening_IsShownStale_AndNeverSuggests()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(2);
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            PartOScreeningEvidence natural = Evidence(partOMixedDesignSession, PartOScreeningStrategy.Natural, (rows[0], PartODwellingOutcome.Pass), (rows[1], PartODwellingOutcome.Pass));
            natural.Fingerprint_Design = "a different building";
            partOMixedDesignSession.ApplyScreening([natural]);

            Assert.Equal("STALE", rows[0].Screening_Natural);
            Assert.False(rows[0].Suggestion.CanApply);
            Assert.NotNull(partOMixedDesignSession.ScreeningStale(PartOScreeningStrategy.Natural));
        }

        // ---- Bulk assignment ---------------------------------------------------------------------------------------

        [Fact]
        public void BulkAssignment_SetsEverySelectedRow_AndConstraintsRefuseItWhole()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedDesignFixture.Baseline(25), null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            Assert.Null(partOMixedDesignSession.SetMvhr(rows.Take(20), null));
            Assert.Equal(20, rows.Count(x => x.Selected?.VentilationMode == PartOVentilationMode.MVHR));

            partOMixedDesignSession.Constraints.NaturalVentilationAllowed = false;

            string refusal = partOMixedDesignSession.SetNatural(rows);
            Assert.Contains("mechanical ventilation", refusal);

            //Nothing half-applied.
            Assert.Equal(20, rows.Count(x => x.Selected?.VentilationMode == PartOVentilationMode.MVHR));
            Assert.DoesNotContain(rows, x => x.Selected?.VentilationMode == PartOVentilationMode.NaturalVentilation);

            Assert.NotNull(partOMixedDesignSession.SetNatural([]));
        }

        [Fact]
        public void ProductAssignment_OnlyFromThePermittedPool()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(3);

            VentilationUnitCapacityDescriptor small = new(new VentilationUnitReference("Maker", "Small", "S-1"), 50, 50);
            VentilationUnitCapacityDescriptor large = new(new VentilationUnitReference("Maker", "Large", "L-1"), 150, 150);

            //The project pool permits only the small product.
            AnalyticalModel withPool = new(baseline);
            withPool.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [small.VentilationUnitReference]));

            PartOMixedDesignSession partOMixedDesignSession = new(withPool, null, [small, large], null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            Assert.True(partOMixedDesignSession.CatalogueOffered);
            Assert.Equal(["Small"], partOMixedDesignSession.AllowedProducts.Select(x => x.VentilationUnitReference.Model));

            Assert.Null(partOMixedDesignSession.SetMvhr([rows[0], rows[1]], small.VentilationUnitReference));
            Assert.Equal("Small", rows[0].Selected.VentilationUnitReference.Model);
            Assert.Contains("Small", rows[1].SelectedText);

            Assert.NotNull(partOMixedDesignSession.SetMvhr([rows[2]], large.VentilationUnitReference));
            Assert.Null(rows[2].Selected);
        }

        [Fact]
        public void RetainedDesign_NeedsBaselineTerminals_AndRecordsOnlySamsFingerprint()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(2);

            //Flat 01's design terminals realised on the baseline, as the D3 acceptance does.
            AdjacencyCluster adjacencyCluster = baseline.AdjacencyCluster;
            List<Space> spaces_01 = adjacencyCluster.GetRelatedObjects<Space>(PartOMixedDesignFixture.Zone(baseline, "Flat 01"));
            adjacencyCluster.RealizePartFVentilationTerminals(spaces_01, out _, out List<string> refusals);
            Assert.Empty(refusals);
            baseline = new AnalyticalModel(baseline, adjacencyCluster);
            Assert.True(baseline.IsPartOCleanBaseline(out _));

            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            List<PartOMixedDwellingRow> rows = [.. partOMixedDesignSession.Rows];

            //Not MVHR yet: refused.
            Assert.NotNull(partOMixedDesignSession.SetRetainedDesign([rows[0]]));

            partOMixedDesignSession.SetMvhr(rows, null);

            //Flat 02 carries no terminals: nothing to retain, and the assignment is refused whole.
            Assert.Contains("Flat 02", partOMixedDesignSession.SetRetainedDesign(rows));
            Assert.Equal(PartODesignAirFlowBasis.PartFRequirement, rows[0].Selected.DesignAirFlowBasis);

            Assert.Null(partOMixedDesignSession.SetRetainedDesign([rows[0]]));
            Assert.Equal(PartODesignAirFlowBasis.RetainedDesign, rows[0].Selected.DesignAirFlowBasis);
            Assert.Equal(baseline.AdjacencyCluster.PartODwellingDesignFingerprint(PartOMixedDesignFixture.Zone(baseline, "Flat 01")), rows[0].Selected.DesignFingerprint);
            Assert.StartsWith("Optimised MVHR", rows[0].SelectedText);

            //And SAM builds it.
            PartOMaterialisation partOMaterialisation = partOMixedDesignSession.WithSelection().MaterialisePartODwellingStrategies();
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);

            //Optimisation not allowed by the project: the retained design is flagged.
            partOMixedDesignSession.Constraints.OptimisationAllowed = false;
            partOMixedDesignSession.Refresh();
            Assert.True(rows[0].NeedsAttention);
        }

        // ---- Persistence and reopen -------------------------------------------------------------------------------

        [Fact]
        public void SavedSelection_IsSamAuthorityOnTheModel_AndDraftIsNotTheModel()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(2);
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);

            partOMixedDesignSession.SetNatural(partOMixedDesignSession.Rows);
            Assert.True(partOMixedDesignSession.IsDirty);
            Assert.False(baseline.HasValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies));

            AnalyticalModel saved = partOMixedDesignSession.WithSelection();
            partOMixedDesignSession.Rebase(saved);
            Assert.False(partOMixedDesignSession.IsDirty);

            //Reopened from JSON: the strategies come back from the model itself.
            AnalyticalModel reopened = new(saved.ToJsonObject());
            PartOMixedDesignSession partOMixedDesignSession_Reopened = new(reopened, null, null, null);
            Assert.All(partOMixedDesignSession_Reopened.Rows, x => Assert.Equal(PartOVentilationMode.NaturalVentilation, x.Selected.VentilationMode));
            Assert.False(partOMixedDesignSession_Reopened.IsDirty);
        }

        [Fact]
        public void Reopen_RestoresEvidence_AndReportsStalenessHonestly_AndNeverInfersASelection()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SAM_PartOMixed_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path_Model = Path.Combine(directory, "Block.sam");
            string path_State = PartOMixedDesignState.Path_State(path_Model);
            Assert.Equal(Path.Combine(directory, "Block.partomixed.json"), path_State);

            try
            {
                AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(3), PartOMixedDesignFixture.Natural);

                PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { Failing = _ => ["Flat 02"] };
                PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out _, fakeSimulator.Simulate);

                PartOMixedDesignSession partOMixedDesignSession = new(baseline, path_Model, null, null);
                partOMixedDesignSession.Constraints.OptimisationAllowed = false;
                partOMixedDesignSession.ApplyScreening([Evidence(partOMixedDesignSession, PartOScreeningStrategy.Natural, (partOMixedDesignSession.Rows[0], PartODwellingOutcome.Pass))]);
                partOMixedDesignSession.ApplyFinal(evidence);
                Assert.True(partOMixedDesignSession.FinalCurrent, partOMixedDesignSession.FinalStale);
                Assert.True(partOMixedDesignSession.State.Write(path_State, out string note), note);

                // ---- Same baseline: everything current --------------------------------------------------------------
                PartOMixedDesignSession reopened = new(new AnalyticalModel(baseline.ToJsonObject()), path_Model, null, PartOMixedDesignState.Read(path_State));
                Assert.True(reopened.FinalCurrent, reopened.FinalStale);
                Assert.False(reopened.Constraints.OptimisationAllowed);
                Assert.Equal("FAIL", reopened.Rows.Single(x => x.Name == "Flat 02").FinalText);
                Assert.Equal("PASS", reopened.Rows[0].Screening_Natural);

                // ---- A baseline without the saved selection: the final result is stale, and nothing is inferred ------
                AnalyticalModel withoutSelection = new(baseline);
                withoutSelection.RemoveValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies);

                PartOMixedDesignSession reopened_NoSelection = new(withoutSelection, path_Model, null, PartOMixedDesignState.Read(path_State));
                Assert.False(reopened_NoSelection.FinalCurrent);
                Assert.Equal("STALE", reopened_NoSelection.Rows[0].FinalText);

                //The run's own snapshot of what it ran as is never written back as the selection.
                Assert.All(reopened_NoSelection.Rows, x => Assert.Null(x.Selected));

                //Screening is bound to the building, not the selection: still current.
                Assert.Equal("PASS", reopened_NoSelection.Rows[0].Screening_Natural);

                // ---- The building changed: screening and final both stale ------------------------------------------
                AdjacencyCluster adjacencyCluster_Changed = baseline.AdjacencyCluster;
                Space space_Changed = new(adjacencyCluster_Changed.GetSpaces()[0]);
                space_Changed.SetValue(SpaceParameter.Volume, 99.0);
                adjacencyCluster_Changed.AddObject(space_Changed);
                AnalyticalModel changed = new(baseline, adjacencyCluster_Changed);
                PartOMixedDesignSession reopened_Changed = new(changed, path_Model, null, PartOMixedDesignState.Read(path_State));
                Assert.False(reopened_Changed.FinalCurrent);
                Assert.Equal("STALE", reopened_Changed.Rows[0].Screening_Natural);

                // ---- The results file rewritten by another run: stale ---------------------------------------------
                File.AppendAllText(evidence.Path_TSD, "rewritten");
                PartOMixedDesignSession reopened_Rewritten = new(baseline, path_Model, null, PartOMixedDesignState.Read(path_State));
                Assert.False(reopened_Rewritten.FinalCurrent);
                Assert.Contains("rewritten", reopened_Rewritten.FinalStale);

                // ---- An unknown schema is no state at all ---------------------------------------------------------
                File.WriteAllText(path_State, new JsonObject { ["Schema"] = "PartOMixedDesign:v99" }.ToJsonString());
                Assert.Null(PartOMixedDesignState.Read(path_State));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void PersistedOutcomes_ReadBackByName_AndAnUnknownOneIsNeverAPass()
        {
            PartODwellingResult partODwellingResult = new(Guid.NewGuid(), PartODwellingOutcome.Fail) { SpaceCount_Fail = 2 };
            partODwellingResult.FailingSpaceNames.Add("Bedroom");

            PartODwellingResult read = PartODwellingResult.Read(partODwellingResult.ToJsonObject());
            Assert.Equal(PartODwellingOutcome.Fail, read.Outcome);
            Assert.Equal(["Bedroom"], read.FailingSpaceNames);

            JsonObject jsonObject = partODwellingResult.ToJsonObject();
            jsonObject["Outcome"] = "Passed";
            Assert.Null(PartODwellingResult.Read(jsonObject));
        }

        // ---- Helpers ------------------------------------------------------------------------------------------------

        internal static PartOScreeningEvidence Evidence(PartOMixedDesignSession partOMixedDesignSession, PartOScreeningStrategy partOScreeningStrategy, params (PartOMixedDwellingRow Row, PartODwellingOutcome Outcome)[] outcomes)
        {
            PartOScreeningEvidence result = new(partOScreeningStrategy)
            {
                Fingerprint_Design = UI.Query.PartOScreeningDesignFingerprint(partOMixedDesignSession.Baseline),
            };

            foreach ((PartOMixedDwellingRow row, PartODwellingOutcome outcome) in outcomes)
            {
                result.Guids_Zone_Assessed.Add(row.ZoneGuid);
                result.Add(new PartODwellingResult(row.ZoneGuid, outcome));
            }

            return result;
        }
    }
}
