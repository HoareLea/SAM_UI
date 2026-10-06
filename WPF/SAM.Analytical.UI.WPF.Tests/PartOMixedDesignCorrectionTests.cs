// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Regressions for the four review findings on SAM_UI#126 (c5f59bf): the catalogue setting, the run verdict over
    /// automatically assessed common spaces, the project test unit as a product, and an unreadable sidecar result.
    /// Each failed on c5f59bf.
    /// </summary>
    public class PartOMixedDesignCorrectionTests
    {
        private static readonly VentilationUnitCapacityDescriptor Product = new(new VentilationUnitReference("Maker", "Unit", "U-1"), 200, 200);

        // ---- P1: the catalogue setting is a build input -----------------------------------------------------------

        [Fact]
        public void ChangingTheCatalogueSetting_MakesTheFinalResultStale_AndChangingItBackRestoresIt()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Mvhr);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, true, [Product], PartOMixedDesignFixture.Context("Block_Catalogue"), CancellationToken.None, out _, fakeSimulator.Simulate);
            Assert.NotNull(evidence);

            PartOMixedDesignState state = new() { FinalRun = evidence };
            PartOMixedDesignSession session = new(baseline, null, [Product], state);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            Assert.True(session.CatalogueOffered);
            Assert.True(session.FinalCurrent, session.FinalStale);

            //Unticked: the next build would select no product, so the result no longer describes the design.
            session.CatalogueOffered = false;
            Assert.False(session.FinalCurrent);
            Assert.Contains("catalogue", session.FinalStale);
            Assert.All(session.Rows, x => Assert.False(x.FinalCurrent));

            session.CatalogueOffered = true;
            Assert.True(session.FinalCurrent, session.FinalStale);
        }

        [Fact]
        public void UntickingTheCatalogue_FlagsAnExplicitProduct_AndBlocksTheBuild_WithoutRewritingIt()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => new PartODwellingStrategy(x.Guid, PartOVentilationMode.MVHR, x.Name == "Flat 01" ? Product.VentilationUnitReference : null));
            PartOMixedDesignSession session = new(baseline, null, [Product], null);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            PartOMixedDwellingRow row = session.Rows.Single(x => x.Name == "Flat 01");
            Assert.False(row.NeedsAttention);
            Assert.True(session.Readiness().CanBuild);

            session.CatalogueOffered = false;
            Assert.True(row.NeedsAttention);
            Assert.Contains("cannot be built", row.Attention);
            Assert.False(session.Readiness().CanBuild);
            Assert.Equal(Product.VentilationUnitReference.ToString(), row.Selected!.VentilationUnitReference!.ToString());
            Assert.DoesNotContain(session.Rows, x => x != row && x.NeedsAttention);

            session.CatalogueOffered = true;
            Assert.False(row.NeedsAttention);
            Assert.True(session.Readiness().CanBuild);
        }

        [Fact]
        public void AnExplicitProductOutsideThePermittedPool_NeedsAttention_AndBlocksTheBuild()
        {
            VentilationUnitCapacityDescriptor other = new(new VentilationUnitReference("Maker", "Other", "O-1"), 200, 200);
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => new PartODwellingStrategy(x.Guid, PartOVentilationMode.MVHR, x.Name == "Flat 01" ? other.VentilationUnitReference : null));
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [Product.VentilationUnitReference]));

            PartOMixedDesignSession session = new(baseline, null, [Product, other], null);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            PartOMixedDwellingRow row = session.Rows.Single(x => x.Name == "Flat 01");

            Assert.True(row.NeedsAttention);
            Assert.Contains("permitted product pool", row.Attention);
            Assert.False(session.Readiness().CanBuild);
            Assert.Equal("Other", row.Selected!.VentilationUnitReference!.Model);
        }

        [Fact]
        public void MvhrSuggestion_ComesOnlyFromTheScreeningOfTheCurrentEquipmentMode()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Mvhr);
            PartOMixedDesignSession session = new(baseline, null, [Product], null);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            Assert.True(session.CatalogueOffered);

            //MVHR baseline (generic units) passed; nothing else was screened.
            PartOScreeningEvidence evidence = PartOMixedDesignSessionTests.Evidence(session, PartOScreeningStrategy.MechanicalBaseline, [.. session.Rows.Select(x => (x, PartODwellingOutcome.Pass))]);
            session.ApplyScreening([evidence]);

            //Products are offered: applying it would build selected products, not what was screened - so no suggestion.
            Assert.All(session.Rows, x => Assert.Null(x.Suggestion?.DwellingStrategy));
            Assert.Contains("selects products from the catalogue", session.Rows[0].SuggestionReason);

            //Generic units: the screening matches, so it is suggested.
            session.CatalogueOffered = false;
            Assert.All(session.Rows, x => Assert.NotNull(x.Suggestion?.DwellingStrategy));
        }

        // ---- P1: the run verdict is the production TM59 verdict ------------------------------------------------------

        [Fact]
        public void RunVerdict_CountsAnOccupiedSpaceOutsideEveryDwellingRow()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { FailingCommonOccupiedSpace = true };

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Common"), CancellationToken.None, out _, fakeSimulator.Simulate);

            //Every dwelling passes; SAM's production verdict fails - the project result is SAM's.
            Assert.All(evidence.Results, x => Assert.Equal(PartODwellingOutcome.Pass, x.Outcome));
            Assert.Equal(PartODwellingOutcome.Fail, evidence.Overall);

            PartOMixedDesignSession session = new(baseline, null, null, new PartOMixedDesignState { FinalRun = evidence });
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            Assert.StartsWith("Final mixed run: FAIL", session.FinalText);
        }

        [Fact]
        public void CorridorRisk_IsPartOfTheProjectResult_ButNeverARow_AndNeverAFailure()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { CorridorAtRisk = true };

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Corridor"), CancellationToken.None, out _, fakeSimulator.Simulate);

            //SAM's rule (TM59AssessmentReport): a corridor over its reference is a risk status beside the occupied-space
            //verdict, never folded into it.
            Assert.Equal(PartODwellingOutcome.Pass, evidence.Overall);

            //Kept through the sidecar.
            PartOMixedRunEvidence reopened = PartOMixedRunEvidence.Read(JsonNode.Parse(evidence.ToJsonObject().ToJsonString()) as JsonObject);

            PartOMixedDesignSession session = new(baseline, null, null, new PartOMixedDesignState { FinalRun = reopened });
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            Assert.Equal(3, session.Rows.Count);
            Assert.DoesNotContain(session.Rows, x => x.Name == PartOMixedDesignFixture.Corridor);
            Assert.Contains("communal corridor", session.FinalText);
            Assert.Contains("significant risk", session.FinalText);
            Assert.Contains(PartOMixedDesignFixture.Corridor, session.FinalText);
        }

        [Fact]
        public void RunVerdict_IsNotAPass_WhereACommonSpaceWentUnassessed()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { UnassessedCommonSpace = true };

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Hole"), CancellationToken.None, out _, fakeSimulator.Simulate);

            //Every dwelling and SAM's occupied-space verdict pass - but a covered space has no result: not a pass (the TM59
            //window's partial-assessment rule), and kept through the sidecar.
            Assert.All(evidence.Results, x => Assert.Equal(PartODwellingOutcome.Pass, x.Outcome));
            Assert.Equal(PartODwellingOutcome.NotAssessed, evidence.Overall);
            Assert.Equal(PartODwellingOutcome.NotAssessed, PartOMixedRunEvidence.Read(JsonNode.Parse(evidence.ToJsonObject().ToJsonString()) as JsonObject).Overall);
        }

        // ---- P1: evidence is bound to the simulation case it ran under ----------------------------------------------

        [Fact]
        public void ChangingTheSimulationCase_MakesTheFinalResultAndScreeningStale_AndChangingItBackRestoresThem()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Case"), CancellationToken.None, out _, fakeSimulator.Simulate);
            Assert.Equal(PartOMixedDesignFixture.CaseKey, evidence.SimulationCaseKey);

            PartOMixedDesignSession session = new(baseline, null, null, new PartOMixedDesignState { FinalRun = evidence });
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            session.ApplyScreening([PartOMixedDesignSessionTests.Evidence(session, PartOScreeningStrategy.Natural, [.. session.Rows.Select(x => (x, PartODwellingOutcome.Pass))])]);
            Assert.True(session.FinalCurrent, session.FinalStale);
            Assert.Single(session.CurrentScreening());

            //Another weather file or solar method: neither result describes the case selected now.
            session.SimulationCaseKey = "another weather|TAS";
            Assert.False(session.FinalCurrent);
            Assert.Contains("different simulation case", session.FinalStale);
            Assert.Empty(session.CurrentScreening());
            Assert.Equal("STALE", session.Rows[0].Screening(PartOScreeningStrategy.Natural));

            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            Assert.True(session.FinalCurrent, session.FinalStale);
            Assert.Single(session.CurrentScreening());
        }

        [Fact]
        public void EvidenceRecordingNoSimulationCase_IsNotCurrent()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_NoCase"), CancellationToken.None, out _, fakeSimulator.Simulate);
            evidence.SimulationCaseKey = null;

            PartOMixedDesignSession session = new(baseline, null, null, new PartOMixedDesignState { FinalRun = evidence });
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            Assert.False(session.FinalCurrent);
            Assert.Contains("does not record the simulation case", session.FinalStale);
        }

        // ---- P1: a run that did not complete re-validates the previous result ------------------------------------------

        [Fact]
        public void RevalidateFinal_AfterAnIncompleteRunRewroteTheResultsFile_ShowsThePreviousResultAsStale()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Cancelled"), CancellationToken.None, out _, fakeSimulator.Simulate);

            PartOMixedDesignSession session = new(baseline, null, null, new PartOMixedDesignState { FinalRun = evidence });
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            Assert.True(session.FinalCurrent, session.FinalStale);

            //A cancelled rerun had already started writing the same results file.
            System.IO.File.AppendAllText(evidence.Path_TSD, "partial");

            session.RevalidateFinal();
            Assert.False(session.FinalCurrent);
            Assert.Contains("rewritten", session.FinalStale);
        }

        // ---- P2: the project test unit is a product ---------------------------------------------------------------

        [Fact]
        public void ProjectTestUnit_IsOfferedAndSelected_WhereTheCatalogueHasNoProducts_AndThePoolIncludesIt()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Mvhr);
            PartOProjectTestVentilationUnit partOProjectTestVentilationUnit = new("Project test unit", 200, 200);
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, partOProjectTestVentilationUnit);

            //SAM's rule: the engineer ticked the test unit into the project's selected pool.
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOProjectTestVentilationUnit.CapacityDescriptors().Select(x => x.VentilationUnitReference)));

            PartOMixedDesignSession session = new(baseline, null, null, null);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            Assert.True(session.CatalogueHasProducts);
            Assert.True(session.CatalogueOffered);
            Assert.Contains(session.AllowedProducts, x => x.VentilationUnitReference?.Model == "Project test unit" || x.VentilationUnitReference?.ToString().Contains("Project test unit") == true);
            Assert.Null(UI.Query.PartOScreeningStrategyUnavailable(PartOScreeningStrategy.SelectedProduct, session.CatalogueHasProducts));

            //Built as the session offers it: SAM selects the project test unit, as Iteration 2 does.
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            Modify.BuildAndRunPartOMixedDesign(baseline, session.CatalogueOffered, session.Descriptors, PartOMixedDesignFixture.Context("Block_TestUnit"), CancellationToken.None, out PartOStrategySetRun run, fakeSimulator.Simulate);

            Assert.True(run.IsMaterialised, string.Join(" ", run.Refusals.Select(x => x.Message)));
            Assert.NotEmpty(run.Materialisation!.VentilationUnitSelections);
            Assert.All(run.Materialisation.VentilationUnitSelections, x => Assert.Contains("Project test unit", x.ToString()));
        }

        [Fact]
        public void ProjectTestUnit_IsScreened_AsTheSelectedProductStrategy_WhereItIsTheOnlyEligibleProduct()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline();
            PartOProjectTestVentilationUnit partOProjectTestVentilationUnit = new("Project test unit", 200, 200);
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, partOProjectTestVentilationUnit);
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, partOProjectTestVentilationUnit.CapacityDescriptors().Select(x => x.VentilationUnitReference)));

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOScreeningOutcome outcome = Modify.ScreenPartODwellingStrategies(
                baseline,
                PartOMixedDesignFixture.Dwellings(baseline).Select(x => x.Guid),
                [PartOScreeningStrategy.SelectedProduct],
                PartOScreeningMode.FullComparison,
                null,
                [],
                x => PartOMixedDesignFixture.Context(Create.PartOMixedProjectName(baseline, "Screen_" + x)),
                CancellationToken.None,
                fakeSimulator.Simulate);

            //Run, not skipped as unavailable - and SAM selected the test unit in the screened model.
            AnalyticalModel screened = Assert.Single(fakeSimulator.Models);
            Assert.Single(outcome.Evidence);
            Assert.Contains(screened.AdjacencyCluster.GetObjects<AirHandlingUnit>() ?? [], x => x.ToJsonObject().ToJsonString().Contains("Project test"));
        }

        [Fact]
        public void SidecarListingADwellingTwice_IsNeitherAPassNorCurrent()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Twice"), CancellationToken.None, out _, fakeSimulator.Simulate);

            JsonObject jsonObject = (JsonObject)JsonNode.Parse(evidence.ToJsonObject().ToJsonString())!;
            JsonArray results = (JsonArray)jsonObject["Results"]!;
            results.Add(results[0]!.DeepClone());

            PartOMixedRunEvidence read = PartOMixedRunEvidence.Read(jsonObject);

            Assert.NotEqual(PartODwellingOutcome.Pass, read.Overall);
            Assert.False(read.IsCurrent(baseline, null, out string reason));
            Assert.Contains("more than once", reason);
        }

        [Fact]
        public void ProjectTestUnit_IsNotOffered_UnderAllCatalogueProducts()
        {
            //"All catalogue products" (also the default where the project sets none) never offers the test unit - SAM's
            //rule; before, the session offered it and SAM then refused the build.
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Mvhr);
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, new PartOProjectTestVentilationUnit("Project test unit", 200, 200));

            PartOMixedDesignSession session = new(baseline, null, [Product], null);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            Assert.DoesNotContain(session.AllowedProducts, x => x.VentilationUnitReference?.ToString().Contains("Project test unit") == true);
            Assert.Contains(session.AllowedProducts, x => x.VentilationUnitReference?.Model == "Unit");
        }

        // ---- P2: an unreadable sidecar result fails closed ----------------------------------------------------------

        [Fact]
        public void SidecarWithAnUnreadableDwellingResult_IsNeitherAPassNorCurrent()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Corrupt"), CancellationToken.None, out _, fakeSimulator.Simulate);
            Assert.Equal(PartODwellingOutcome.Pass, evidence.Overall);
            Assert.True(evidence.IsCurrent(baseline, null, out string reason_Before), reason_Before);

            JsonObject jsonObject = (JsonObject)JsonNode.Parse(evidence.ToJsonObject().ToJsonString())!;
            ((JsonObject)((JsonArray)jsonObject["Results"]!)[0]!)["Outcome"] = "Maybe";

            PartOMixedRunEvidence read = PartOMixedRunEvidence.Read(jsonObject);

            Assert.NotEqual(PartODwellingOutcome.Pass, read.Overall);
            Assert.False(read.IsCurrent(baseline, null, out string reason));
            Assert.Contains("could not be read", reason);
        }

        [Fact]
        public void SidecarMissingAnAssessedDwellingsResult_IsNeitherAPassNorCurrent()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Missing"), CancellationToken.None, out _, fakeSimulator.Simulate);

            JsonObject jsonObject = (JsonObject)JsonNode.Parse(evidence.ToJsonObject().ToJsonString())!;
            ((JsonArray)jsonObject["Results"]!).RemoveAt(0);

            PartOMixedRunEvidence read = PartOMixedRunEvidence.Read(jsonObject);

            Assert.NotEqual(PartODwellingOutcome.Pass, read.Overall);
            Assert.False(read.IsCurrent(baseline, null, out _));
        }
    }
}
