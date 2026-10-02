// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-1: performance of a draft through the EXISTING glazing evaluator seam - debounce, content-keyed cache, generation numbers
    /// (a superseded answer is never current), no Tas call for a draft with errors, and an explicit "Not calculated" for every way Tas can
    /// fail. <see cref="FakeDraftTas"/> stands in for Tas.
    /// </summary>
    public class GlazingBuilderEvaluatorTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(40);

        private static DraftGlazingEvaluator Evaluator(FakeDraftTas tas, TimeSpan? debounce = null)
        {
            return new DraftGlazingEvaluator(tas, debounce ?? TimeSpan.Zero, null, BuilderFixture.Options());
        }

        [Fact]
        public void The_DefaultDebounce_IsBetween300And400ms()
        {
            Assert.InRange(DraftGlazingEvaluator.DefaultDebounce.TotalMilliseconds, 300, 400);
        }

        [Fact]
        public void By_Default_ItOwnsItsOwnTasEvaluator_NotThePanels()
        {
            using ThermalEditServices services = new ThermalEditServices(sources: () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()));
            using DraftGlazingEvaluator evaluator = new DraftGlazingEvaluator();

            Assert.True(evaluator.OwnsEvaluator);
            Assert.IsType<TasGlazingEvaluator>(evaluator.Evaluator);
            Assert.NotSame(services.GlazingEvaluator, evaluator.Evaluator);

            FakeDraftTas supplied = new FakeDraftTas();
            using DraftGlazingEvaluator shared = new DraftGlazingEvaluator(supplied);
            Assert.False(shared.OwnsEvaluator);
            Assert.Same(supplied, shared.Evaluator);
        }

        [Fact]
        public async Task A_Draft_IsCalculatedThroughTheEvaluator_OnATransientOneSystemSource()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using DraftGlazingEvaluator evaluator = Evaluator(tas);

            DraftGlazingEvaluation evaluation = await evaluator.EvaluateAsync(BuilderFixture.Double()).WaitAsync(Timeout);

            Assert.Equal(DraftGlazingEvaluationState.Calculated, evaluation.State);
            Assert.Equal(5.7 / 2, evaluation.Values.Ug, 9);
            Assert.Equal(1.8, evaluation.Values.Uf, 9);
            Assert.False(evaluation.FromCache);
            Assert.Equal(1, tas.Calls);
            Assert.Same(evaluation, evaluator.Latest);
        }

        [Fact]
        public async Task A_BurstOfEdits_AsksTasOnce_ForTheLastState()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using DraftGlazingEvaluator evaluator = Evaluator(tas, TimeSpan.FromMilliseconds(150));
            GlazingSystemDraft draft = BuilderFixture.Double();

            List<Task<DraftGlazingEvaluation>> requests = new List<Task<DraftGlazingEvaluation>>();
            foreach (double millimetres in new double[] { 10, 12, 14, 16, 18 })
            {
                ((DraftGap)draft.Layers[1]).Thickness = millimetres / 1000;
                requests.Add(evaluator.EvaluateAsync(draft));
            }

            DraftGlazingEvaluation[] results = await Task.WhenAll(requests).WaitAsync(Timeout);

            Assert.All(results.Take(4), x => Assert.Equal(DraftGlazingEvaluationState.Superseded, x.State));
            Assert.Equal(DraftGlazingEvaluationState.Calculated, results.Last().State);
            Assert.Equal(1, tas.Calls);
            Assert.StartsWith("Argon_18mm_", tas.Requests.Single()[1]);
            Assert.Equal(5, evaluator.Generation);
        }

        [Fact]
        public async Task A_StaleAnswer_IsDropped_ButItsValuesAreCachedForTheirContent()
        {
            FakeDraftTas tas = new FakeDraftTas();
            tas.Hold();
            using DraftGlazingEvaluator evaluator = Evaluator(tas);
            GlazingSystemDraft first = BuilderFixture.Double();
            GlazingSystemDraft second = BuilderFixture.Triple();

            Task<DraftGlazingEvaluation> running = evaluator.EvaluateAsync(first);
            while (tas.Calls == 0)
            {
                await Task.Delay(5);
            }

            Task<DraftGlazingEvaluation> newer = evaluator.EvaluateAsync(second);
            tas.Release();

            DraftGlazingEvaluation stale = await running.WaitAsync(Timeout);
            DraftGlazingEvaluation current = await newer.WaitAsync(Timeout);

            Assert.Equal(DraftGlazingEvaluationState.Superseded, stale.State);
            Assert.Null(stale.Values);
            Assert.Equal(DraftGlazingEvaluationState.Calculated, current.State);
            Assert.Equal(5.7 / 3, current.Values.Ug, 9);
            Assert.Same(current, evaluator.Latest);

            // The stale call's values were right for ITS content: asking for it again is a cache hit.
            DraftGlazingEvaluation again = await evaluator.EvaluateAsync(BuilderFixture.Double()).WaitAsync(Timeout);
            Assert.True(again.FromCache);
            Assert.Equal(2, tas.Calls);
        }

        [Fact]
        public async Task The_Cache_IsKeyedByContent_NotByDraftGuidOrName()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using DraftGlazingEvaluator evaluator = Evaluator(tas);
            GlazingSystemDraft draft = BuilderFixture.Double();

            await evaluator.EvaluateAsync(draft).WaitAsync(Timeout);
            DraftGlazingEvaluation twin = await evaluator.EvaluateAsync(BuilderFixture.Double("Another name")).WaitAsync(Timeout);
            ((DraftGap)draft.Layers[1]).Thickness = 0.014;
            DraftGlazingEvaluation changed = await evaluator.EvaluateAsync(draft).WaitAsync(Timeout);
            ((DraftGap)draft.Layers[1]).Thickness = 0.016;
            DraftGlazingEvaluation back = await evaluator.EvaluateAsync(draft).WaitAsync(Timeout);

            Assert.True(twin.FromCache);
            Assert.False(changed.FromCache);
            Assert.True(back.FromCache);
            Assert.Equal(2, tas.Calls);
            Assert.Equal(2, evaluator.Cache.Count);
        }

        [Fact]
        public async Task A_DraftWithErrors_IsNotCalculated_AndTasIsNotAsked()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using DraftGlazingEvaluator evaluator = Evaluator(tas);
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.Layers.Add(BuilderFixture.Gap()); // gas on the inside edge

            DraftGlazingEvaluation evaluation = await evaluator.EvaluateAsync(draft).WaitAsync(Timeout);

            Assert.Equal(DraftGlazingEvaluationState.NotCalculated, evaluation.State);
            Assert.StartsWith("Not calculated", evaluation.Reason);
            Assert.True(evaluation.Validation.Has(GlazingDraftIssueCodes.GasAtEdge));
            Assert.Equal(0, tas.Calls);

            GlazingSystemDraft missing = new GlazingSystemDraft() { Name = "M" };
            missing.Add(BuilderFixture.Pane(BuilderFixture.ClearPane()), BuilderFixture.Gap(), DraftPane.Missing("Gone", 0.004));
            Assert.Equal(DraftGlazingEvaluationState.NotCalculated, (await evaluator.EvaluateAsync(missing).WaitAsync(Timeout)).State);
            Assert.Equal(0, tas.Calls);
        }

        [Fact]
        public async Task Warnings_DoNotStopTheCalculation()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using DraftGlazingEvaluator evaluator = Evaluator(tas);
            GlazingSystemDraft draft = BuilderFixture.Double();
            draft.IntendedPanelType = PanelType.Undefined;
            draft.Frame = DraftFrame.None();

            DraftGlazingEvaluation evaluation = await evaluator.EvaluateAsync(draft).WaitAsync(Timeout);

            Assert.Equal(DraftGlazingEvaluationState.Calculated, evaluation.State);
            Assert.True(double.IsNaN(evaluation.Values.Uf));
        }

        [Theory]
        [InlineData("empty", "Tas returned no values")]
        [InlineData("error", "Tas is not installed.")]
        [InlineData("throw", "TCD is not registered.")]
        [InlineData("zero", "Tas returned no values")]
        public async Task Every_TasFailure_IsNotCalculated_WithItsReason_AndIsNotCached(string failure, string reason)
        {
            FakeDraftTas tas = new FakeDraftTas();
            switch (failure)
            {
                case "empty":
                    tas.Empty = true;
                    break;
                case "error":
                    tas.Error = "Tas is not installed.";
                    break;
                case "throw":
                    tas.Throw = true;
                    break;
                default:
                    tas.Ug = 0;
                    break;
            }

            using DraftGlazingEvaluator evaluator = Evaluator(tas);

            DraftGlazingEvaluation evaluation = await evaluator.EvaluateAsync(BuilderFixture.Double()).WaitAsync(Timeout);
            await evaluator.EvaluateAsync(BuilderFixture.Double()).WaitAsync(Timeout);

            Assert.Equal(DraftGlazingEvaluationState.NotCalculated, evaluation.State);
            Assert.Null(evaluation.Values);
            Assert.Contains(reason, evaluation.Reason);
            Assert.StartsWith("Not calculated: ", evaluation.Reason);
            Assert.Equal(0, evaluator.Cache.Count);
            Assert.Equal(2, tas.Calls);
        }

        [Fact]
        public async Task The_Request_IsASnapshot_LaterEditsDoNotReachIt()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using DraftGlazingEvaluator evaluator = Evaluator(tas, Short);
            GlazingSystemDraft draft = BuilderFixture.Double();

            Task<DraftGlazingEvaluation> request = evaluator.EvaluateAsync(draft);
            draft.Layers.Clear();
            DraftGlazingEvaluation evaluation = await request.WaitAsync(Timeout);

            Assert.Equal(DraftGlazingEvaluationState.Calculated, evaluation.State);
            Assert.Equal(3, tas.Requests.Single().Count);
        }

        [Fact]
        public async Task A_Disposed_Evaluator_RefusesNewRequests()
        {
            FakeDraftTas tas = new FakeDraftTas();
            DraftGlazingEvaluator evaluator = Evaluator(tas, TimeSpan.FromSeconds(5));
            Task<DraftGlazingEvaluation> pending = evaluator.EvaluateAsync(BuilderFixture.Double());

            evaluator.Dispose();

            Assert.Equal(DraftGlazingEvaluationState.Superseded, (await pending.WaitAsync(Timeout)).State);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => evaluator.EvaluateAsync(BuilderFixture.Double()));
            Assert.Equal(0, tas.Calls);
        }
    }
}
