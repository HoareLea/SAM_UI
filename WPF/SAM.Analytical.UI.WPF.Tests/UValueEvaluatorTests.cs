// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value PR2a: the evaluator seam. <see cref="StaSingleFlightWorker{TRequest, TResult}"/> runs one item at a
    /// time on one STA thread, debounces, and drops stale results; <see cref="TasUValueEvaluator"/> answers an
    /// unreachable target from the cached range ends (no bisection) and classifies with the PR1 rules. The TCD
    /// calls are replaced by <see cref="FakeTas"/>, whose physics matches the real calculator (PR2a spike).
    /// </summary>
    public class UValueEvaluatorTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private static UValueEvaluationRequest Request(double target, int layerIndex = -1, HeatFlowDirection heatFlowDirection = HeatFlowDirection.Horizontal, double min = 0.001, double max = 1.0)
        {
            return new UValueEvaluationRequest(UValueFixture.Wall(), UValueFixture.Materials(), layerIndex, target, heatFlowDirection, true, min, max);
        }

        // -------------------------------------------------------------------------------------------------
        // StaSingleFlightWorker
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task Worker_RunsOnAnStaThread()
        {
            using StaSingleFlightWorker<int, ApartmentState> worker = new StaSingleFlightWorker<int, ApartmentState>(x => Thread.CurrentThread.GetApartmentState(), TimeSpan.Zero);

            Assert.Equal(ApartmentState.STA, await worker.Submit(1).WaitAsync(Timeout));
        }

        [Fact]
        public async Task Worker_TypingBurst_RunsOnlyTheLastRequest_AndCancelsTheSupersededOnes()
        {
            List<int> ran = new List<int>();
            using StaSingleFlightWorker<int, int> worker = new StaSingleFlightWorker<int, int>(x => { lock (ran) { ran.Add(x); } return x * 10; }, TimeSpan.FromMilliseconds(200));

            Task<int> first = worker.Submit(1);
            Task<int> second = worker.Submit(2);
            Task<int> third = worker.Submit(3);

            Assert.Equal(30, await third.WaitAsync(Timeout));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
            Assert.Equal(new[] { 3 }, ran);
            Assert.Equal(1, worker.Started);
        }

        [Fact]
        public async Task Worker_CancellingARunningItem_CompletesItAsCancelledAtOnce_AndDropsItsResult()
        {
            ManualResetEventSlim release = new ManualResetEventSlim(false);
            ManualResetEventSlim running = new ManualResetEventSlim(false);
            using StaSingleFlightWorker<int, int> worker = new StaSingleFlightWorker<int, int>(x =>
            {
                if (x == 1)
                {
                    running.Set();
                    release.Wait(Timeout);
                }

                return x;
            }, TimeSpan.Zero);

            CancellationTokenSource stale = new CancellationTokenSource();
            Task<int> first = worker.Submit(1, stale.Token);
            Assert.True(running.Wait(Timeout));

            stale.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(Timeout));

            Task<int> second = worker.Submit(2);
            release.Set();

            Assert.Equal(2, await second.WaitAsync(Timeout));
            Assert.True(first.IsCanceled);
        }

        [Fact]
        public async Task Worker_ExceptionFaultsTheTask_AndTheWorkerKeepsRunning()
        {
            using StaSingleFlightWorker<int, int> worker = new StaSingleFlightWorker<int, int>(x => x == 1 ? throw new InvalidOperationException("boom") : x, TimeSpan.Zero);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => worker.Submit(1).WaitAsync(Timeout));
            Assert.Equal("boom", exception.Message);
            Assert.Equal(2, await worker.Submit(2).WaitAsync(Timeout));
        }

        [Fact]
        public async Task Worker_Dispose_CancelsThePendingItem()
        {
            StaSingleFlightWorker<int, int> worker = new StaSingleFlightWorker<int, int>(x => x, TimeSpan.FromSeconds(30));
            Task<int> pending = worker.Submit(1);

            worker.Dispose();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(Timeout));
            Assert.Throws<ObjectDisposedException>(() => { _ = worker.Submit(2); });
        }

        // -------------------------------------------------------------------------------------------------
        // TasUValueEvaluator
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public async Task Evaluator_ReachableTarget_RunsTheBisectionOnTheStaWorker()
        {
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = await evaluator.EvaluateAsync(Request(0.3), CancellationToken.None).WaitAsync(Timeout);

            Assert.True(evaluation.Reached);
            Assert.Equal(UValueCalculationFailure.None, evaluation.Failure);
            Assert.Null(evaluation.Message);
            Assert.Equal(UValueFixture.WoolIndex, evaluation.LayerIndex);
            Assert.Equal(0.26, evaluation.InitialThermalTransmittance, 2);
            Assert.Equal(UValueFixture.Thickness(0.3), evaluation.Thickness, 6);
            Assert.Equal(0.3, evaluation.CalculatedThermalTransmittance, 3);
            Assert.Equal(1, fakeTas.LayerThicknessCalls);
            Assert.Equal(ApartmentState.STA, fakeTas.LastApartment);
        }

        [Fact]
        public void Evaluator_RangeEnds_AreComputedOnce_PerConstructionLayerAndRange()
        {
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);
            Construction wall = UValueFixture.Wall();
            MaterialLibrary materialLibrary = UValueFixture.Materials();

            foreach (double target in new[] { 0.3, 0.35, 0.4 })
            {
                Assert.True(evaluator.Evaluate(new UValueEvaluationRequest(wall, materialLibrary, -1, target, HeatFlowDirection.Horizontal, true, 0.001, 1.0)).Reached);
            }

            Assert.Equal(1, fakeTas.ThermalTransmittanceCalls);
            Assert.Equal(3, fakeTas.LayerThicknessCalls);

            evaluator.Evaluate(new UValueEvaluationRequest(wall, materialLibrary, -1, 0.3, HeatFlowDirection.Horizontal, true, 0.001, 0.5));
            Assert.Equal(2, fakeTas.ThermalTransmittanceCalls);
        }

        [Fact]
        public void Evaluator_TargetBelowTheRange_IsUnreachableWithoutBisection_AndNamesTheBestUAtTheMaximumThickness()
        {
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = evaluator.Evaluate(Request(0.01));

            Assert.False(evaluation.Reached);
            Assert.Equal(UValueCalculationFailure.Unreachable, evaluation.Failure);
            Assert.Equal(0, fakeTas.LayerThicknessCalls);
            Assert.Equal(UValueFixture.U(1.0), evaluation.BestAchievableThermalTransmittance, 6);
            Assert.Equal(1.0, evaluation.BestAchievableThickness, 6);
            Assert.Contains("is not reachable by varying I01_Mineral Wool within 1-1000 mm.", evaluation.Message);
            Assert.Contains("Best achievable: U 0.025 W/m²K at 1000 mm.", evaluation.Message);
        }

        [Fact]
        public void Evaluator_TargetAboveTheRange_NamesTheBestUAtTheMinimumThickness()
        {
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = evaluator.Evaluate(Request(5));

            Assert.Equal(UValueCalculationFailure.Unreachable, evaluation.Failure);
            Assert.Equal(0, fakeTas.LayerThicknessCalls);
            Assert.Equal(0.001, evaluation.BestAchievableThickness, 6);
            Assert.Contains("at 1 mm.", evaluation.Message);
        }

        [Fact]
        public void Evaluator_TargetJustOutsideTheRange_WithinTolerance_IsReachedAtTheRangeEnd()
        {
            // 0.051 for 0.05 is a hit on the real calculator (PR1); the same tolerance applies at a range end.
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);
            double atMax = UValueFixture.U(1.0);

            UValueEvaluation evaluation = evaluator.Evaluate(Request(atMax - 0.005));

            Assert.True(evaluation.Reached);
            Assert.Equal(1.0, evaluation.Thickness, 6);
            Assert.Equal(atMax, evaluation.CalculatedThermalTransmittance, 6);
            Assert.Equal(0, fakeTas.LayerThicknessCalls);
        }

        [Fact]
        public void Evaluator_UndefinedHeatFlowDirection_UsesThePr1Message_AndCallsNoTcd()
        {
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = evaluator.Evaluate(Request(0.3, heatFlowDirection: HeatFlowDirection.Undefined));

            Assert.Equal(UValueCalculationFailure.HeatFlowUndefined, evaluation.Failure);
            Assert.Contains("heat-flow direction is undefined", evaluation.Message);
            Assert.Equal(0, fakeTas.ThermalTransmittanceCalls + fakeTas.LayerThicknessCalls);
        }

        [Fact]
        public void Evaluator_OnlyGasAndThinLayers_SaysThereIsNoAdjustableLayer()
        {
            Construction construction = new Construction("Gas only", new List<ConstructionLayer>() { new ConstructionLayer(UValueFixture.Air, 0.05), new ConstructionLayer(UValueFixture.Rainscreen, 0.003) });
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = evaluator.Evaluate(new UValueEvaluationRequest(construction, UValueFixture.Materials(), -1, 0.3, HeatFlowDirection.Horizontal, true, 0.001, 1));

            Assert.Equal(UValueCalculationFailure.NoAdjustableLayer, evaluation.Failure);
            Assert.Equal("No adjustable layer: all layers are gas, glass, or thinner than 10 mm.", evaluation.Message);
        }

        [Fact]
        public void Evaluator_TasUnavailable_UsesThePr1Message()
        {
            FakeTas fakeTas = new FakeTas() { Unavailable = true };
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = evaluator.Evaluate(Request(0.3));

            Assert.Equal(UValueCalculationFailure.Unavailable, evaluation.Failure);
            Assert.Contains("TCD could not run", evaluation.Message);
        }

        [Fact]
        public void Evaluator_ProbeOnly_ReturnsTheCurrentUAndTheRangeEnds()
        {
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = evaluator.Evaluate(Request(double.NaN));

            Assert.Equal(UValueCalculationFailure.None, evaluation.Failure);
            Assert.Equal(UValueFixture.U(0.08), evaluation.InitialThermalTransmittance, 6);
            Assert.Equal(UValueFixture.U(0.001), evaluation.MinThicknessThermalTransmittance, 6);
            Assert.Equal(UValueFixture.U(1.0), evaluation.MaxThicknessThermalTransmittance, 6);
            Assert.True(double.IsNaN(evaluation.Thickness));
            Assert.Equal(0, fakeTas.LayerThicknessCalls);
        }

        [Fact]
        public void Evaluator_LayerOverride_VariesThatLayer()
        {
            // The fixture physics only responds to the wool, so varying the board cannot reach 0.3: the result and
            // its message name the overridden layer, not the automatic one.
            FakeTas fakeTas = new FakeTas();
            using TasUValueEvaluator evaluator = UValueFixture.Evaluator(fakeTas);

            UValueEvaluation evaluation = evaluator.Evaluate(Request(0.3, layerIndex: 1));

            Assert.Equal(1, evaluation.LayerIndex);
            Assert.Equal(UValueCalculationFailure.Unreachable, evaluation.Failure);
            Assert.Contains("varying Cement Particleboard", evaluation.Message);
        }
    }
}
