// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Accept optimised airflow: a completed 2B-style result for one dwelling is accepted through SAM's
    /// <c>Modify.AcceptPartODwellingDesign</c>, the dwelling becomes Optimised MVHR (retained design + fingerprint, no
    /// airflow), and the whole mixed model is rebuilt from the clean baseline. The source here is the all-MVHR
    /// materialisation with Flat 03 raised, balanced, exactly as a 2B round raises a dwelling on its run copy.
    /// </summary>
    public class PartOMixedDesignAcceptTests
    {
        private const double Raise_Lps = 3.0;

        [Fact]
        public void Preview_AsksSam_AndChangesNothing()
        {
            AnalyticalModel baseline = Baseline();
            PartOMixedDesignSession session = new(baseline, null, null, null);
            PartOMixedDwellingRow row = session.Rows.Single(x => x.Name == "Flat 03");

            PartODwellingDesignAcceptance acceptance = session.PreviewAcceptDesign(row, Source(baseline));

            Assert.True(acceptance.IsAccepted, acceptance.Refusal);
            Assert.Equal(2, acceptance.Changes.Count);
            Assert.All(acceptance.Changes, x => Assert.Equal(x.Before_Lps + Raise_Lps, x.After_Lps, 6));

            Assert.Same(baseline, session.Baseline);
            Assert.False(session.IsDirty);
            Assert.Equal(PartODesignAirFlowBasis.PartFRequirement, row.Selected!.DesignAirFlowBasis);
        }

        [Fact]
        public void Accept_EditsOnlyThatDwelling_SelectsOptimisedMvhr_AndTheRebuildUsesTheAcceptedAirflow()
        {
            AnalyticalModel baseline = Baseline();
            PartOMixedDesignSession session = new(baseline, null, null, null);
            PartOMixedDwellingRow row = session.Rows.Single(x => x.Name == "Flat 03");
            List<string> others_Before = session.Rows.Where(x => x != row).Select(x => x.SelectedText).ToList();

            PartODwellingDesignAcceptance acceptance = session.PreviewAcceptDesign(row, Source(baseline));
            Assert.Null(session.AcceptDesign(row, acceptance));

            //The dwelling: Optimised MVHR, guarded by SAM's fingerprint, carrying no airflow.
            Assert.Equal(PartOVentilationMode.MVHR, row.Selected!.VentilationMode);
            Assert.Equal(PartODesignAirFlowBasis.RetainedDesign, row.Selected.DesignAirFlowBasis);
            Assert.Equal(acceptance.DesignFingerprint, row.Selected.DesignFingerprint);
            string json_Strategy = row.Selected.ToJsonObject().ToJsonString();
            Assert.DoesNotContain("_Lps", json_Strategy);
            Assert.DoesNotContain("FlowRate", json_Strategy);
            Assert.StartsWith("Optimised MVHR", row.SelectedText);

            //Nothing else moved; the baseline is still clean and carries terminals for Flat 03 only; it is a pending edit.
            Assert.Equal(others_Before, session.Rows.Where(x => x != row).Select(x => x.SelectedText).ToList());
            Assert.True(session.IsCleanBaseline);
            Assert.True(session.IsDirty);
            AdjacencyCluster adjacencyCluster = session.Baseline.AdjacencyCluster;
            foreach (Zone zone in PartOMixedDesignFixture.Dwellings(session.Baseline))
            {
                int count = (adjacencyCluster.GetRelatedObjects<Space>(zone) ?? []).Sum(x => adjacencyCluster.GetRelatedObjects<VentilationTerminal>(x)?.Count ?? 0);
                Assert.Equal(zone.Name == "Flat 03" ? 2 : 0, count);
            }

            Assert.Contains("1 Optimised MVHR", session.Readiness().Text);

            //Rebuilt from the baseline with the whole selection: Flat 03 at the accepted airflow, Flat 02 at its requirement.
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(session.WithSelection(), false, null, PartOMixedDesignFixture.Context("Block_Accept"), CancellationToken.None, out PartOStrategySetRun run, fakeSimulator.Simulate);
            Assert.True(run.IsMaterialised, string.Join(" ", run.Refusals.Select(x => x.Message)));
            Assert.NotNull(evidence);

            AdjacencyCluster adjacencyCluster_Run = Assert.Single(fakeSimulator.Models).AdjacencyCluster;
            Assert.Equal(8.0 + Raise_Lps, Flow(adjacencyCluster_Run, "Flat 03 Bedroom", FlowClassification.Supply), 6);
            Assert.Equal(8.0 + Raise_Lps, Flow(adjacencyCluster_Run, "Flat 03 Bathroom", FlowClassification.Extract), 6);
            Assert.Equal(8.0, Flow(adjacencyCluster_Run, "Flat 02 Bedroom", FlowClassification.Supply), 6);
            Assert.Equal(0.0, Flow(adjacencyCluster_Run, "Flat 01 Bedroom", FlowClassification.Supply), 6);
        }

        [Fact]
        public void Accept_MakesAnEarlierFinalResultStale_AndSurvivesReopening()
        {
            AnalyticalModel baseline = Baseline();
            PartOMixedDesignFixture.FakeSimulator fakeSimulator = new();
            PartOMixedRunEvidence evidence = Modify.BuildAndRunPartOMixedDesign(baseline, false, null, PartOMixedDesignFixture.Context("Block_AcceptStale"), CancellationToken.None, out _, fakeSimulator.Simulate);

            PartOMixedDesignSession session = new(baseline, null, null, new PartOMixedDesignState { FinalRun = evidence });
            Assert.True(session.FinalCurrent, session.FinalStale);

            PartOMixedDwellingRow row = session.Rows.Single(x => x.Name == "Flat 03");
            Assert.Null(session.AcceptDesign(row, session.PreviewAcceptDesign(row, Source(baseline))));
            Assert.False(session.FinalCurrent);

            //Saved and reopened: the selection and the accepted design come back, and SAM materialises them.
            AnalyticalModel saved = session.WithSelection();
            PartOMixedDesignSession reopened = new(saved, null, null, null);
            PartOMixedDwellingRow row_Reopened = reopened.Rows.Single(x => x.Name == "Flat 03");

            Assert.Equal(PartODesignAirFlowBasis.RetainedDesign, row_Reopened.Selected!.DesignAirFlowBasis);
            Assert.False(reopened.IsDirty);
            Assert.True(reopened.IsCleanBaseline);

            PartOMaterialisation partOMaterialisation = saved.MaterialisePartODwellingStrategies();
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);
        }

        [Fact]
        public void Accept_OfASourceWithoutTheDesign_IsRefusedBySam_AndChangesNothing()
        {
            AnalyticalModel baseline = Baseline();
            PartOMixedDesignSession session = new(baseline, null, null, null);
            PartOMixedDwellingRow row = session.Rows.Single(x => x.Name == "Flat 03");

            //The clean baseline states no design terminals, so there is no 2B design in it to accept.
            PartODwellingDesignAcceptance acceptance = session.PreviewAcceptDesign(row, baseline);
            Assert.False(acceptance.IsAccepted);
            Assert.NotNull(session.AcceptDesign(row, acceptance));

            Assert.Same(baseline, session.Baseline);
            Assert.False(session.IsDirty);
            Assert.Equal(PartODesignAirFlowBasis.PartFRequirement, row.Selected!.DesignAirFlowBasis);
        }

        [Fact]
        public void Accept_WhereTheProjectForbidsAnOptimisedDesign_IsRefused_AndChangesNothing()
        {
            AnalyticalModel baseline = Baseline();
            PartOMixedDesignSession session = new(baseline, null, null, null);
            session.Constraints.OptimisationAllowed = false;
            PartOMixedDwellingRow row = session.Rows.Single(x => x.Name == "Flat 03");

            Assert.NotNull(session.AcceptDesign(row, session.PreviewAcceptDesign(row, Source(baseline))));
            Assert.Same(baseline, session.Baseline);
            Assert.False(session.IsDirty);
        }

        /// <summary>Flat 01 natural, Flats 02 and 03 MVHR at the requirement - saved on the clean baseline.</summary>
        private static AnalyticalModel Baseline() => PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => x.Name == "Flat 01" ? PartOMixedDesignFixture.Natural(x) : PartOMixedDesignFixture.Mvhr(x));

        /// <summary>A 2B-style result: the all-MVHR run copy with Flat 03's supply and extract each raised, balanced.</summary>
        private static AnalyticalModel Source(AnalyticalModel baseline)
        {
            PartOMaterialisation partOMaterialisation = PartOMixedDesignFixture.WithStrategies(baseline, PartOMixedDesignFixture.Mvhr).MaterialisePartODwellingStrategies();
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);

            AdjacencyCluster adjacencyCluster = partOMaterialisation.AnalyticalModel.AdjacencyCluster;
            foreach ((string name, FlowClassification flowClassification) in new[] { ("Flat 03 Bedroom", FlowClassification.Supply), ("Flat 03 Bathroom", FlowClassification.Extract) })
            {
                Space space = adjacencyCluster.GetSpaces().Single(x => x.Name == name);
                adjacencyCluster.SetSpaceDesignFlowRate(space, flowClassification, Flow(adjacencyCluster, name, flowClassification) + Raise_Lps, out _, out List<string> refusals);
                Assert.Empty(refusals);
            }

            return new AnalyticalModel(partOMaterialisation.AnalyticalModel, adjacencyCluster);
        }

        private static double Flow(AdjacencyCluster adjacencyCluster, string name_Space, FlowClassification flowClassification)
        {
            Space space = adjacencyCluster.GetSpaces().Single(x => x.Name == name_Space);
            return (adjacencyCluster.GetRelatedObjects<VentilationTerminal>(space) ?? []).Where(x => x.FlowClassification == flowClassification).Sum(x => x.DesignFlowRate_Lps ?? 0);
        }
    }
}
