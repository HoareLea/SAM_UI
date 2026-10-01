// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The real <see cref="IUValueEvaluator"/>: the existing Tas <see cref="ThermalTransmittanceCalculator"/> on one
    /// STA worker with debounce and stale-result cancellation (<see cref="StaSingleFlightWorker{TRequest, TResult}"/>).
    /// <para>
    /// <b>Range ends first.</b> U falls monotonically as the layer gets thicker, so the U-values at the minimum
    /// and maximum thickness bound every reachable target. They cost one TCD run (about 0.22 s) and are cached
    /// per construction, layer and range, so an unreachable target is answered without the bisection, which
    /// runs to exhaustion on an unreachable target (about 2.7 s in the PR2a spike against about 0.25 s for a
    /// reachable one). The same values give the best achievable U.
    /// </para>
    /// <para>
    /// <b>One rule set.</b> Failures are classified by the PR1 <c>Query.UValueCalculationFailure</c> /
    /// <c>Query.UValueCalculationMessage</c>, never re-derived here.
    /// </para>
    /// </summary>
    public sealed class TasUValueEvaluator : IUValueEvaluator, IDisposable
    {
        /// <summary>Quiet period before an evaluation starts.</summary>
        public static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(300);

        private readonly StaSingleFlightWorker<UValueEvaluationRequest, UValueEvaluation> worker;
        private readonly Func<ConstructionManager, IEnumerable<Guid>, List<ThermalTransmittanceCalculationResult>> calculateThermalTransmittances;
        private readonly Func<ConstructionManager, LayerThicknessCalculationData, LayerThicknessCalculationResult> calculateLayerThickness;

        // Touched only on the worker thread.
        private readonly Dictionary<string, ThermalTransmittanceCalculationResult[]> rangeEnds = new Dictionary<string, ThermalTransmittanceCalculationResult[]>();

        public TasUValueEvaluator()
            : this(DefaultDebounce)
        {
        }

        public TasUValueEvaluator(TimeSpan debounce)
            : this(debounce, null, null)
        {
        }

        /// <param name="calculateThermalTransmittances">Stand-in for the TCD U-value run (tests); null for the real calculator.</param>
        /// <param name="calculateLayerThickness">Stand-in for the TCD bisection (tests); null for the real calculator.</param>
        internal TasUValueEvaluator(TimeSpan debounce, Func<ConstructionManager, IEnumerable<Guid>, List<ThermalTransmittanceCalculationResult>> calculateThermalTransmittances, Func<ConstructionManager, LayerThicknessCalculationData, LayerThicknessCalculationResult> calculateLayerThickness)
        {
            this.calculateThermalTransmittances = calculateThermalTransmittances ?? ((constructionManager, guids) => new ThermalTransmittanceCalculator(constructionManager).Calculate(guids));
            this.calculateLayerThickness = calculateLayerThickness ?? ((constructionManager, data) => new ThermalTransmittanceCalculator(constructionManager).Calculate(data));
            worker = new StaSingleFlightWorker<UValueEvaluationRequest, UValueEvaluation>(Evaluate, debounce, "SAM U-value evaluator");
        }

        public Task<UValueEvaluation> EvaluateAsync(UValueEvaluationRequest request, CancellationToken cancellationToken)
        {
            return worker.Submit(request, cancellationToken);
        }

        public void Dispose()
        {
            worker.Dispose();
        }

        /// <summary>One evaluation, synchronously, on the calling thread (which must be STA for TCD).</summary>
        internal UValueEvaluation Evaluate(UValueEvaluationRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Construction construction = request?.Construction;
            if (construction == null || request.MaterialLibrary == null)
            {
                return new UValueEvaluation(request, UValueCalculationFailure.Unavailable, "There is no construction to calculate.", -1, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, stopwatch.ElapsedMilliseconds);
            }

            ConstructionManager constructionManager = new ConstructionManager(null, new Construction[] { construction }, request.MaterialLibrary);

            LayerThicknessCalculationData data = new LayerThicknessCalculationData(construction.Name, request.LayerIndex, request.ProbeOnly ? 1 : request.TargetThermalTransmittance, request.HeatFlowDirection, request.External)
            {
                ThicknessRange = new Range<double>(request.MinThickness, request.MaxThickness),
            };

            UValueCalculationFailure failure = data.UValueCalculationFailure(constructionManager);
            if (failure != UValueCalculationFailure.None)
            {
                return new UValueEvaluation(request, failure, data.UValueCalculationMessage(constructionManager), request.LayerIndex, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, stopwatch.ElapsedMilliseconds);
            }

            int layerIndex = request.LayerIndex == -1 ? Tas.Query.AdjustableLayerIndex(construction, request.MaterialLibrary) : request.LayerIndex;
            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if (layerIndex < 0 || constructionLayers == null || layerIndex >= constructionLayers.Count)
            {
                LayerThicknessCalculationResult noLayer = new LayerThicknessCalculationResult(Tas.Query.Source(), construction.Name, -1, double.NaN, 0, data.ThermalTransmittance, double.NaN);
                return new UValueEvaluation(request, noLayer.UValueCalculationFailure(), noLayer.UValueCalculationMessage(data, constructionManager), -1, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, stopwatch.ElapsedMilliseconds);
            }

            ThermalTransmittanceCalculationResult[] ends = RangeEnds(request, layerIndex);
            double initial = ends?[0]?.GetThermalTransmittance(request.HeatFlowDirection, request.External) ?? double.NaN;
            double atMin = ends?[1]?.GetThermalTransmittance(request.HeatFlowDirection, request.External) ?? double.NaN;
            double atMax = ends?[2]?.GetThermalTransmittance(request.HeatFlowDirection, request.External) ?? double.NaN;

            if (double.IsNaN(initial))
            {
                LayerThicknessCalculationResult unavailable = null;
                return new UValueEvaluation(request, unavailable.UValueCalculationFailure(), unavailable.UValueCalculationMessage(data, constructionManager), layerIndex, double.NaN, double.NaN, double.NaN, atMin, atMax, stopwatch.ElapsedMilliseconds);
            }

            if (request.ProbeOnly)
            {
                return new UValueEvaluation(request, UValueCalculationFailure.None, null, layerIndex, initial, double.NaN, double.NaN, atMin, atMax, stopwatch.ElapsedMilliseconds);
            }

            double target = request.TargetThermalTransmittance;
            data.LayerIndex = layerIndex;

            // Outside the band the bisection cannot succeed: answer from the range end instead. The end itself is
            // a hit when it is within the PR1 tolerance of the target (as 0.051 for 0.05 is).
            LayerThicknessCalculationResult result = null;
            if (!double.IsNaN(atMax) && target < atMax)
            {
                result = new LayerThicknessCalculationResult(Tas.Query.Source(), construction.Name, layerIndex, request.MaxThickness, initial, target, atMax);
            }
            else if (!double.IsNaN(atMin) && target > atMin)
            {
                result = new LayerThicknessCalculationResult(Tas.Query.Source(), construction.Name, layerIndex, request.MinThickness, initial, target, atMin);
            }
            else
            {
                result = calculateLayerThickness(constructionManager, data);
            }

            failure = result.UValueCalculationFailure();
            string message = result.UValueCalculationMessage(data, constructionManager);

            UValueEvaluation evaluation = new UValueEvaluation(
                request,
                failure,
                message,
                result?.LayerIndex ?? layerIndex,
                double.IsNaN(result?.InitialThermalTransmittance ?? double.NaN) ? initial : result.InitialThermalTransmittance,
                failure == UValueCalculationFailure.None ? result.Thickness : double.NaN,
                failure == UValueCalculationFailure.None ? result.CalculatedThermalTransmittance : double.NaN,
                atMin,
                atMax,
                stopwatch.ElapsedMilliseconds);

            if (failure == UValueCalculationFailure.Unreachable && !double.IsNaN(evaluation.BestAchievableThermalTransmittance))
            {
                message = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} Best achievable: U {1} W/m²K at {2} mm.",
                    message,
                    evaluation.BestAchievableThermalTransmittance.ToString("0.###", CultureInfo.CurrentCulture),
                    (evaluation.BestAchievableThickness * 1000).ToString("0.#", CultureInfo.CurrentCulture));

                evaluation = new UValueEvaluation(request, failure, message, evaluation.LayerIndex, evaluation.InitialThermalTransmittance, double.NaN, double.NaN, atMin, atMax, stopwatch.ElapsedMilliseconds);
            }

            return evaluation;
        }

        // [as is, layer at min thickness, layer at max thickness] from one TCD run, cached per construction, layer and range.
        private ThermalTransmittanceCalculationResult[] RangeEnds(UValueEvaluationRequest request, int layerIndex)
        {
            Construction construction = request.Construction;
            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;

            string key = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2:R}|{3:R}|{4}",
                construction.Guid,
                layerIndex,
                request.MinThickness,
                request.MaxThickness,
                string.Join(";", constructionLayers.Select(x => string.Format(CultureInfo.InvariantCulture, "{0}:{1:R}", x?.Name, x?.Thickness))));

            if (rangeEnds.TryGetValue(key, out ThermalTransmittanceCalculationResult[] cached))
            {
                return cached;
            }

            Construction atMin = WithLayerThickness(construction, layerIndex, request.MinThickness, " [min]");
            Construction atMax = WithLayerThickness(construction, layerIndex, request.MaxThickness, " [max]");

            ConstructionManager constructionManager = new ConstructionManager(null, new Construction[] { construction, atMin, atMax }, request.MaterialLibrary);
            List<ThermalTransmittanceCalculationResult> results = calculateThermalTransmittances(constructionManager, new Guid[] { construction.Guid, atMin.Guid, atMax.Guid });

            ThermalTransmittanceCalculationResult[] ends = new ThermalTransmittanceCalculationResult[]
            {
                results?.Find(x => x?.Reference == construction.Guid.ToString()),
                results?.Find(x => x?.Reference == atMin.Guid.ToString()),
                results?.Find(x => x?.Reference == atMax.Guid.ToString()),
            };

            // Do not cache a failed run (Tas may become available later).
            if (ends[0] != null)
            {
                rangeEnds[key] = ends;
            }

            return ends;
        }

        private static Construction WithLayerThickness(Construction construction, int layerIndex, double thickness, string suffix)
        {
            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            constructionLayers[layerIndex] = new ConstructionLayer(constructionLayers[layerIndex].Name, thickness);

            Construction result = new Construction(Guid.NewGuid(), construction, construction.Name + suffix);
            return new Construction(result, constructionLayers);
        }
    }
}
