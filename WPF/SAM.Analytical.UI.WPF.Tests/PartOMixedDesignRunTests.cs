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
    /// The mixed Part O design's materialisation and run path: SAM's PR1 authority called over the clean baseline,
    /// structured refusals surfaced as they are, the baseline never written, the materialised copy simulated, and the
    /// final result independent of screening. TAS is a stand-in (<see cref="PartOMixedDesignFixture.FakeSimulator"/>),
    /// so this needs no licence.
    /// </summary>
    public class PartOMixedDesignRunTests
    {
        [Fact]
        public void Fixture_IsACleanBaseline_ThatSamMaterialisesMixed()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline();
            Assert.True(baseline.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> findings), string.Join("\n", findings));

            AnalyticalModel withSet = PartOMixedDesignFixture.WithStrategies(baseline, x => x.Name == "Flat 02" ? PartOMixedDesignFixture.Mvhr(x) : PartOMixedDesignFixture.Natural(x));

            PartOMaterialisation partOMaterialisation = withSet.MaterialisePartODwellingStrategies();
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);
        }

        [Fact]
        public void MixedRun_CallsSam_SimulatesTheMaterialisedCopy_AndNeverWritesTheBaseline()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => x.Name == "Flat 02" ? PartOMixedDesignFixture.Mvhr(x) : PartOMixedDesignFixture.Natural(x));
            string fingerprint_Before = SimulationResultProvenance.Fingerprint(baseline);
            string json_Before = baseline.ToJsonObject().ToJsonString();

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { Failing = _ => ["Flat 03"] };

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_Mixed"), CancellationToken.None, out PartOStrategySetRun run, fakeSimulator.Simulate);

            Assert.NotNull(evidence);

            //The simulated model is SAM's materialisation, a different instance carrying the record - never the baseline.
            AnalyticalModel simulated = Assert.Single(fakeSimulator.Models);
            Assert.NotSame(baseline, simulated);
            Assert.True(simulated.HasValue(Analytical.AnalyticalModelParameter.PartOMaterialisationRecord));
            Assert.False(baseline.HasValue(Analytical.AnalyticalModelParameter.PartOMaterialisationRecord));
            Assert.Contains(simulated.AdjacencyCluster.GetObjects<VentilationSystem>() ?? [], x => true);

            //The baseline is byte-for-byte what it was.
            Assert.Equal(fingerprint_Before, SimulationResultProvenance.Fingerprint(baseline));
            Assert.Equal(json_Before, baseline.ToJsonObject().ToJsonString());
            Assert.True(baseline.IsPartOCleanBaseline(out _));

            //Per dwelling, with what each ran as - SAM's own set, not text.
            Assert.Equal(PartODwellingOutcome.Pass, evidence.Result(PartOMixedDesignFixture.Zone(baseline, "Flat 01").Guid).Outcome);
            Assert.Equal(PartODwellingOutcome.Fail, evidence.Result(PartOMixedDesignFixture.Zone(baseline, "Flat 03").Guid).Outcome);
            Assert.Equal(PartODwellingOutcome.Fail, evidence.Overall);
            Assert.Equal(PartOVentilationMode.MVHR, evidence.Strategies.Strategy(PartOMixedDesignFixture.Zone(baseline, "Flat 02").Guid).VentilationMode);
            Assert.Equal(new[] { "Flat 03 Bathroom", "Flat 03 Bedroom" }, evidence.Result(PartOMixedDesignFixture.Zone(baseline, "Flat 03").Guid).FailingSpaceNames.OrderBy(x => x));

            //SAM's record is current against the baseline it was built from.
            Assert.True(evidence.Record.IsCurrent(baseline, null, out string reason), reason);
        }

        [Fact]
        public void StructuredRefusals_AreSurfacedAsSamStatesThem_AndNothingIsSimulated()
        {
            //Flat 03 has no strategy: SAM refuses MissingStrategy for it - absence is never read as natural.
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => x.Name == "Flat 03" ? null : PartOMixedDesignFixture.Natural(x));

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();

            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out PartOStrategySetRun run, fakeSimulator.Simulate);

            Assert.Null(evidence);
            Assert.Empty(fakeSimulator.Models);
            Assert.False(run.IsMaterialised);

            PartOMaterialisationRefusal refusal = Assert.Single(run.Refusals, x => x.Reason == PartOMaterialisationRefusalReason.MissingStrategy);
            Assert.Equal(PartOMixedDesignFixture.Zone(baseline, "Flat 03").Guid, refusal.ZoneGuid);

            //Surfaced by reason, with SAM's own message - never a generic "could not build".
            string text = Modify.RefusalText(run.Refusals);
            Assert.Contains(Core.Query.Description(PartOMaterialisationRefusalReason.MissingStrategy), text);
            Assert.Contains(refusal.Message, text);
        }

        [Fact]
        public void CoolingRequested_IsRefusedBySam_NotBypassed()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => new PartODwellingStrategy(x.Guid, PartOVentilationMode.MVHR, null, PartOActiveCooling.SupplyAirCooling));

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out PartOStrategySetRun run, fakeSimulator.Simulate);

            Assert.Contains(run.Refusals, x => x.Reason == PartOMaterialisationRefusalReason.CoolingGated);
            Assert.Empty(fakeSimulator.Models);
        }

        [Fact]
        public void NotCleanBaseline_IsRefusedWithSamsFindings()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);

            //A simulated model carries its scenarios: SAM says it is run output, not a baseline.
            PartOMaterialisation first = baseline.MaterialisePartODwellingStrategies();
            Assert.True(first.IsMaterialised, first.Refusal);

            PartOMixedDesignSession partOMixedDesignSession = new(first.AnalyticalModel, null, null, null);
            Assert.False(partOMixedDesignSession.IsCleanBaseline);
            Assert.False(partOMixedDesignSession.Readiness().CanBuild);
            Assert.Contains(partOMixedDesignSession.BaselineFindings, x => x.Reason == PartOMaterialisationRefusalReason.MaterialisedBaseline);
        }

        [Fact]
        public void Rerun_AfterEditingOneDwelling_ReconstructsTheWholeDesignFromTheBaseline()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { Failing = _ => ["Flat 03"] };
            PartOMixedRunEvidence first = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out _, fakeSimulator.Simulate);

            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            partOMixedDesignSession.ApplyFinal(first);

            PartOMixedDwellingRow flat03 = partOMixedDesignSession.Rows.Single(x => x.Name == "Flat 03");
            Assert.True(flat03.FinalFail);
            Assert.Equal(new[] { flat03 }, partOMixedDesignSession.Rows.Where(x => PartOMixedDesignSession.Matches(x, PartOMixedDwellingFilter.FailingFinal, null)));

            //Change Flat 03 only.
            Assert.Null(partOMixedDesignSession.SetMvhr([flat03], null));
            Assert.True(partOMixedDesignSession.IsDirty);

            //The previous result is not the edited design's result - never shown as current.
            Assert.False(flat03.FinalCurrent);
            Assert.Equal("STALE", flat03.FinalText);

            AnalyticalModel baseline_Edited = partOMixedDesignSession.WithSelection();
            fakeSimulator.Failing = _ => [];
            PartOMixedRunEvidence second = Modify.BuildAndRunPartOMixedDesign(baseline_Edited, false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out _, fakeSimulator.Simulate);

            //A fresh materialisation of the WHOLE building from the baseline - a new model, not the previous one patched.
            Assert.Equal(2, fakeSimulator.Models.Count);
            Assert.NotSame(fakeSimulator.Models[0], fakeSimulator.Models[1]);
            Assert.False(fakeSimulator.Models[1].HasValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance));
            Assert.Equal(3, second.Results.Count);
            Assert.Equal(PartOVentilationMode.MVHR, second.Strategies.Strategy(flat03.ZoneGuid).VentilationMode);
            Assert.Equal(PartOVentilationMode.NaturalVentilation, second.Strategies.Strategy(partOMixedDesignSession.Rows.Single(x => x.Name == "Flat 01").ZoneGuid).VentilationMode);

            //The first run is stale against the edited baseline, and SAM names the strategy change.
            Assert.False(first.Record.IsCurrent(baseline_Edited, null, out string reason));
            Assert.Contains("strategy", reason);
        }

        [Fact]
        public void CancelledRun_ProducesNoResult_AndLeavesTheBaselineUntouched()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            string fingerprint = SimulationResultProvenance.Fingerprint(baseline);

            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new() { Cancel = true };
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context(), CancellationToken.None, out PartOStrategySetRun run, fakeSimulator.Simulate);

            Assert.Null(evidence);
            Assert.True(run.Cancelled);
            Assert.Equal(fingerprint, SimulationResultProvenance.Fingerprint(baseline));
        }

        [Fact]
        public void DwellingTally_FailIsCertain_PassNeedsEverySpaceAssessed()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(2);
            AdjacencyCluster adjacencyCluster = baseline.AdjacencyCluster;
            Zone flat01 = PartOMixedDesignFixture.Zone(baseline, "Flat 01");
            Zone flat02 = PartOMixedDesignFixture.Zone(baseline, "Flat 02");

            Space bedroom01 = adjacencyCluster.GetSpaces().Single(x => x.Name == "Flat 01 Bedroom");
            Space bathroom01 = adjacencyCluster.GetSpaces().Single(x => x.Name == "Flat 01 Bathroom");
            Space bedroom02 = adjacencyCluster.GetSpaces().Single(x => x.Name == "Flat 02 Bedroom");
            Space bathroom02 = adjacencyCluster.GetSpaces().Single(x => x.Name == "Flat 02 Bathroom");

            Dictionary<Guid, TM59ComplianceStatus> statuses = new()
            {
                [bedroom01.Guid] = TM59ComplianceStatus.Pass,
                [bathroom01.Guid] = TM59ComplianceStatus.Pass,
                [bedroom02.Guid] = TM59ComplianceStatus.Pass,
            };

            //Flat 02's bathroom is unassessed: its bedroom passing is not a dwelling pass.
            List<PartODwellingResult> results = Query.PartODwellingResults(statuses, [bathroom02.Guid], adjacencyCluster, [flat01.Guid, flat02.Guid]);
            Assert.Equal(PartODwellingOutcome.Pass, results[0].Outcome);
            Assert.Equal(PartODwellingOutcome.NotAssessed, results[1].Outcome);

            //A failure is certain whatever else went unassessed.
            statuses[bedroom02.Guid] = TM59ComplianceStatus.Fail;
            results = Query.PartODwellingResults(statuses, [bathroom02.Guid], adjacencyCluster, [flat02.Guid]);
            Assert.Equal(PartODwellingOutcome.Fail, results[0].Outcome);
            Assert.Equal(1, results[0].SpaceCount_Fail);

            //No assessment at all is NOT ASSESSED for every dwelling - never a pass.
            results = Query.PartODwellingResults((PartOTM59Assessment)null, adjacencyCluster, [flat01.Guid]);
            Assert.Equal(PartODwellingOutcome.NotAssessed, results[0].Outcome);
        }
    }
}
