// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Optional automatic screening: only the chosen, screenable strategies run; minimum screening skips only whole
    /// strategies that cannot contribute and never infers a result; a cancelled screening keeps the selection; and
    /// every piece of evidence is bound to the building it screened.
    /// </summary>
    public class PartOMixedDesignScreeningTests
    {
        private static readonly List<PartOScreeningStrategy> All = UI.Query.PartOScreeningStrategies();

        [Fact]
        public void TheStrategyList_IsEngineeringStrategies_InLeastInterventionOrder_WithIterationsAsDetailOnly()
        {
            Assert.Equal([PartOScreeningStrategy.Natural, PartOScreeningStrategy.MechanicalBaseline, PartOScreeningStrategy.SelectedProduct, PartOScreeningStrategy.Optimised, PartOScreeningStrategy.ActiveCooling], All);

            foreach (PartOScreeningStrategy partOScreeningStrategy in All)
            {
                //The label never carries an iteration number; the detail does, for traceability.
                Assert.DoesNotContain("Iteration", UI.Query.PartOScreeningStrategyLabel(partOScreeningStrategy));
                Assert.Contains("Iteration", UI.Query.PartOScreeningStrategyDetail(partOScreeningStrategy));
            }

            //Cooling is gated for PR3; optimisation is not yet screenable; the product strategy needs products.
            Assert.NotNull(UI.Query.PartOScreeningStrategyUnavailable(PartOScreeningStrategy.ActiveCooling, true));
            Assert.NotNull(UI.Query.PartOScreeningStrategyUnavailable(PartOScreeningStrategy.Optimised, true));
            Assert.NotNull(UI.Query.PartOScreeningStrategyUnavailable(PartOScreeningStrategy.SelectedProduct, false));
            Assert.Null(UI.Query.PartOScreeningStrategyUnavailable(PartOScreeningStrategy.SelectedProduct, true));
        }

        [Fact]
        public void Screening_IsOptional_ADesignCanBeBuiltWithoutIt()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedDesignFixture.Baseline(3), null, null, null);
            partOMixedDesignSession.SetNatural(partOMixedDesignSession.Rows);

            Assert.Empty(partOMixedDesignSession.State.Screening);
            Assert.True(partOMixedDesignSession.Readiness().CanBuild);

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            Assert.NotNull(Modify.BuildAndRunPartOMixedDesign(partOMixedDesignSession.WithSelection(), false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out _, fakeSimulator.Simulate));
        }

        [Fact]
        public void FullComparison_RunsOnlyTheChosenScreenableStrategies_ForEveryDwelling_WithProvenance()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(3);
            string fingerprint_Design = UI.Query.PartOScreeningDesignFingerprint(baseline);

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { Failing = _ => ["Flat 02"] };

            PartOScreeningOutcome outcome = Screen(baseline, [PartOScreeningStrategy.Natural, PartOScreeningStrategy.MechanicalBaseline, PartOScreeningStrategy.Optimised, PartOScreeningStrategy.ActiveCooling], PartOScreeningMode.FullComparison, fakeSimulator);

            //Two runs: the unavailable two were skipped with their reason, not simulated.
            Assert.Equal(2, fakeSimulator.Models.Count);
            Assert.Equal(["Block_Screen_Natural", "Block_Screen_MechanicalBaseline"], fakeSimulator.ProjectNames);
            Assert.NotNull(outcome.Steps.Single(x => x.Strategy == PartOScreeningStrategy.Optimised).Skipped);
            Assert.NotNull(outcome.Steps.Single(x => x.Strategy == PartOScreeningStrategy.ActiveCooling).Skipped);
            Assert.DoesNotContain(outcome.Steps, x => x.Strategy == PartOScreeningStrategy.SelectedProduct);

            //Each run is the homogeneous case, built by SAM: every dwelling natural, then every dwelling MVHR.
            Assert.Empty(fakeSimulator.Models[0].AdjacencyCluster.GetObjects<VentilationSystem>() ?? []);
            Assert.Equal(3, fakeSimulator.Models[1].GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies).Strategies.Count(x => x.VentilationMode == PartOVentilationMode.MVHR));

            foreach (PartOScreeningEvidence evidence in outcome.Evidence)
            {
                Assert.Equal(3, evidence.Count);
                Assert.Equal(fingerprint_Design, evidence.Fingerprint_Design);
                Assert.False(evidence.CatalogueOffered);
                Assert.NotNull(evidence.Path_TSD);
                Assert.Equal(PartODwellingOutcome.Fail, evidence.Outcome(PartOMixedDesignFixture.Zone(baseline, "Flat 02").Guid));
            }
        }

        [Fact]
        public void MinimumScreening_ScreensOnlyUnresolvedDwellings_StopsWhenNoneRemain_AndNeverInfersAPass()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(3);
            Guid flat01 = PartOMixedDesignFixture.Zone(baseline, "Flat 01").Guid;
            Guid flat02 = PartOMixedDesignFixture.Zone(baseline, "Flat 02").Guid;

            //Natural: Flat 02 fails. MVHR: everything passes.
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { Failing = x => x.AdjacencyCluster.GetObjects<VentilationSystem>()?.Count > 0 ? [] : ["Flat 02"] };

            VentilationUnitCapacityDescriptor product = new(new VentilationUnitReference("Maker", "Unit", "U-1"), 200, 200);

            PartOScreeningOutcome outcome = Screen(baseline, All, PartOScreeningMode.Minimum, fakeSimulator, [product]);

            //Natural for all three; MVHR baseline only for Flat 02; selected-product MVHR not run - nothing left.
            Assert.Equal(2, fakeSimulator.Models.Count);

            PartOScreeningEvidence natural = outcome.Evidence.Single(x => x.Strategy == PartOScreeningStrategy.Natural);
            PartOScreeningEvidence mvhr = outcome.Evidence.Single(x => x.Strategy == PartOScreeningStrategy.MechanicalBaseline);

            Assert.Equal(3, natural.Guids_Zone_Assessed.Count);
            Assert.Equal([flat02], mvhr.Guids_Zone_Assessed);

            //Flat 01 was never simulated under MVHR: NOT RUN, never PASS.
            Assert.Equal(PartODwellingOutcome.NotRun, mvhr.Outcome(flat01));
            Assert.Equal(PartODwellingOutcome.Pass, mvhr.Outcome(flat02));

            PartOScreeningStep step_Product = outcome.Steps.Single(x => x.Strategy == PartOScreeningStrategy.SelectedProduct);
            Assert.Null(step_Product.Evidence);
            Assert.Contains("already passes", step_Product.Skipped);
        }

        [Fact]
        public void MinimumScreening_SkipsAStrategyTheProjectDoesNotPermit()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(2);
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();

            PartOScreeningOutcome outcome = Screen(baseline, [PartOScreeningStrategy.Natural, PartOScreeningStrategy.MechanicalBaseline], PartOScreeningMode.Minimum, fakeSimulator, null, new PartOMixedDesignConstraints { NaturalVentilationAllowed = false });

            Assert.Equal(["Block_Screen_MechanicalBaseline"], fakeSimulator.ProjectNames);
            Assert.Contains("do not permit", outcome.Steps.Single(x => x.Strategy == PartOScreeningStrategy.Natural).Skipped);
        }

        [Fact]
        public void CancelledScreening_KeepsCompletedEvidence_DiscardsTheRunningStrategy_AndNeverTouchesTheSelection()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(2), PartOMixedDesignFixture.Mvhr);
            string json_Selection = baseline.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies).ToJsonObject().ToJsonString();
            string fingerprint = SimulationResultProvenance.Fingerprint(baseline);

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            int runs = 0;

            PartOStrategySetSimulation Simulate(AnalyticalModel analyticalModel, List<OverheatingScenario> overheatingScenarios, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken)
            {
                runs++;
                fakeSimulator.Cancel = runs == 2;
                return fakeSimulator.Simulate(analyticalModel, overheatingScenarios, partOSimulationContext, cancellationToken);
            }

            PartOScreeningOutcome outcome = Modify.ScreenPartODwellingStrategies(baseline, PartOMixedDesignFixture.Dwellings(baseline).Select(x => x.Guid), [PartOScreeningStrategy.Natural, PartOScreeningStrategy.MechanicalBaseline], PartOScreeningMode.FullComparison, null, null, x => PartOMixedDesignFixture.Context(x.ToString()), CancellationToken.None, Simulate);

            Assert.True(outcome.Cancelled);
            Assert.Equal([PartOScreeningStrategy.Natural], outcome.Evidence.Select(x => x.Strategy));
            Assert.True(outcome.Steps.Single(x => x.Strategy == PartOScreeningStrategy.MechanicalBaseline).Cancelled);

            //The selected design: exactly what it was.
            Assert.Equal(json_Selection, baseline.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies).ToJsonObject().ToJsonString());
            Assert.Equal(fingerprint, SimulationResultProvenance.Fingerprint(baseline));

            //And the session's selection is untouched by the evidence arriving.
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            partOMixedDesignSession.ApplyScreening(outcome.Evidence);
            Assert.All(partOMixedDesignSession.Rows, x => Assert.Equal(PartOVentilationMode.MVHR, x.Selected.VentilationMode));
        }

        [Fact]
        public void ScreeningRefusedBySam_IsReportedStructured_AndProducesNoEvidence()
        {
            //A result-bearing model is not a clean baseline: SAM refuses every screening of it.
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(2);
            AnalyticalModel runOutput = new(baseline);
            runOutput.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new Core.SAMCollection<OverheatingScenario>());

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOScreeningOutcome outcome = Screen(runOutput, [PartOScreeningStrategy.Natural], PartOScreeningMode.FullComparison, fakeSimulator);

            Assert.Empty(fakeSimulator.Models);
            Assert.Empty(outcome.Evidence);
            Assert.Contains(outcome.Steps.Single().Refusals, x => x.Reason == PartOMaterialisationRefusalReason.RunOutputBaseline);
        }

        [Fact]
        public void ScreeningProvenance_IgnoresTheSelection_ButNotTheBuildingOrTheCatalogue()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(2);
            string fingerprint = UI.Query.PartOScreeningDesignFingerprint(baseline);

            //Selecting strategies does not move it.
            Assert.Equal(fingerprint, UI.Query.PartOScreeningDesignFingerprint(PartOMixedDesignFixture.WithStrategies(baseline, PartOMixedDesignFixture.Natural)));

            //A project product setting does.
            AnalyticalModel withPool = new(baseline);
            withPool.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts));
            Assert.NotEqual(fingerprint, UI.Query.PartOScreeningDesignFingerprint(withPool));

            //A catalogue change makes product-selecting evidence stale, and only that.
            VentilationUnitCapacityDescriptor product = new(new VentilationUnitReference("Maker", "Unit", "U-1"), 200, 200);
            string catalogue_1 = UI.Query.PartOMixedCatalogueFingerprint(baseline, [product]);
            string catalogue_2 = UI.Query.PartOMixedCatalogueFingerprint(baseline, [new VentilationUnitCapacityDescriptor(product.VentilationUnitReference, 180, 200)]);

            PartOScreeningEvidence evidence_Product = new(PartOScreeningStrategy.SelectedProduct) { Fingerprint_Design = fingerprint, CatalogueOffered = true, Fingerprint_Catalogue = catalogue_1 };
            PartOScreeningEvidence evidence_Natural = new(PartOScreeningStrategy.Natural) { Fingerprint_Design = fingerprint };

            Assert.True(evidence_Product.IsCurrent(fingerprint, catalogue_1, out _));
            Assert.False(evidence_Product.IsCurrent(fingerprint, catalogue_2, out string reason));
            Assert.Contains("catalogue", reason);
            Assert.True(evidence_Natural.IsCurrent(fingerprint, catalogue_2, out _));
        }

        [Fact]
        public void EvidenceAndState_RoundTrip()
        {
            PartOScreeningEvidence evidence = new(PartOScreeningStrategy.MechanicalBaseline) { Fingerprint_Design = "d", Path_TSD = "x.tsd", Length_TSD = 5, Timestamp_TSD = 7 };
            Guid guid = Guid.NewGuid();
            evidence.Guids_Zone_Assessed.Add(guid);
            evidence.Add(new PartODwellingResult(guid, PartODwellingOutcome.Pass));

            PartOMixedDesignState state = new() { ScreeningMode = PartOScreeningMode.FullComparison };
            state.SetScreeningEvidence(evidence);
            state.Strategies_Screening.Add(PartOScreeningStrategy.Natural);

            PartOMixedDesignState read = PartOMixedDesignState.Read(state.ToJsonObject());

            Assert.Equal(PartOScreeningMode.FullComparison, read.ScreeningMode);
            Assert.Equal([PartOScreeningStrategy.Natural], read.Strategies_Screening);

            PartOScreeningEvidence evidence_Read = read.ScreeningEvidence(PartOScreeningStrategy.MechanicalBaseline);
            Assert.Equal(PartODwellingOutcome.Pass, evidence_Read.Outcome(guid));
            Assert.Equal(PartODwellingOutcome.NotRun, evidence_Read.Outcome(Guid.NewGuid()));
            Assert.Equal("d", evidence_Read.Fingerprint_Design);
            Assert.Equal(5, evidence_Read.Length_TSD);
        }

        private static PartOScreeningOutcome Screen(AnalyticalModel baseline, List<PartOScreeningStrategy> strategies, PartOScreeningMode partOScreeningMode, PartOMixedDesignFixture.FakeSimulator fakeSimulator, List<VentilationUnitCapacityDescriptor> descriptors = null, PartOMixedDesignConstraints constraints = null)
        {
            return Modify.ScreenPartODwellingStrategies(
                baseline,
                PartOMixedDesignFixture.Dwellings(baseline).Select(x => x.Guid),
                strategies,
                partOScreeningMode,
                constraints,
                descriptors,
                x => PartOMixedDesignFixture.Context(Create.PartOMixedProjectName(baseline, "Screen_" + x)),
                CancellationToken.None,
                fakeSimulator.Simulate);
        }
    }
}
