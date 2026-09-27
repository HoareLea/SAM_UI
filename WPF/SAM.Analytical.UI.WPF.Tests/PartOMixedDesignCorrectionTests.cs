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
    /// Regressions for the four Codex findings on SAM_UI#126 (c5f59bf): the catalogue setting, the run verdict over
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
            Assert.Equal(3, session.Rows.Count);
            Assert.DoesNotContain(session.Rows, x => x.Name == PartOMixedDesignFixture.Corridor);
            Assert.Contains("communal corridor", session.FinalText);
            Assert.Contains("significant risk", session.FinalText);
            Assert.Contains(PartOMixedDesignFixture.Corridor, session.FinalText);
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
        public void ProjectTestUnit_IsNotOffered_UnderAllCatalogueProducts()
        {
            //"All catalogue products" (also the default where the project sets none) never offers the test unit - SAM's
            //rule; before, the session offered it and SAM then refused the build.
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Mvhr);
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, new PartOProjectTestVentilationUnit("Project test unit", 200, 200));

            PartOMixedDesignSession session = new(baseline, null, [Product], null);

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
