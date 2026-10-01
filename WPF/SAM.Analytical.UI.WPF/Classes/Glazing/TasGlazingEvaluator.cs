// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The real <see cref="IGlazingEvaluator"/>: the existing Tas <c>ThermalTransmittanceCalculator.CalculateGlazing</c>
    /// (the calculation behind Tools > Glazing Calculator) on one STA worker. One TCD run calculates many systems
    /// (about 0.3 s for one, 3 s for 171), so a whole pool is calculated once and the table filters on the values.
    /// </summary>
    public sealed class TasGlazingEvaluator : IGlazingEvaluator, IDisposable
    {
        private readonly StaSingleFlightWorker<GlazingEvaluationRequest, GlazingEvaluation> worker;
        private readonly Func<ConstructionManager, IEnumerable<Guid>, List<GlazingCalculationResult>> calculateGlazing;

        public TasGlazingEvaluator()
            : this(null)
        {
        }

        /// <param name="calculateGlazing">Stand-in for the TCD run (tests); null for the real calculator.</param>
        internal TasGlazingEvaluator(Func<ConstructionManager, IEnumerable<Guid>, List<GlazingCalculationResult>> calculateGlazing)
        {
            this.calculateGlazing = calculateGlazing ?? ((constructionManager, guids) => new ThermalTransmittanceCalculator(constructionManager).CalculateGlazing(guids));
            worker = new StaSingleFlightWorker<GlazingEvaluationRequest, GlazingEvaluation>(Evaluate, TimeSpan.Zero, "SAM glazing evaluator");
        }

        public Task<GlazingEvaluation> EvaluateAsync(GlazingEvaluationRequest request, CancellationToken cancellationToken)
        {
            return worker.Submit(request, cancellationToken);
        }

        public void Dispose()
        {
            worker.Dispose();
        }

        /// <summary>One evaluation, synchronously, on the calling thread (which must be STA for TCD).</summary>
        internal GlazingEvaluation Evaluate(GlazingEvaluationRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Dictionary<Guid, GlazingValues> values = new Dictionary<Guid, GlazingValues>();
            string error = null;

            foreach (GlazingEvaluationBatch batch in request?.Batches ?? new List<GlazingEvaluationBatch>())
            {
                if (batch.Guids.Count == 0)
                {
                    continue;
                }

                List<GlazingCalculationResult> results;
                try
                {
                    results = calculateGlazing(batch.Source.ConstructionManager, batch.Guids);
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                    continue;
                }

                if (results == null || results.Count == 0)
                {
                    error = error ?? "Tas could not calculate the glazing values.";
                    continue;
                }

                foreach (GlazingCalculationResult result in results)
                {
                    if (result == null || !Guid.TryParse(result.Reference, out Guid guid))
                    {
                        continue;
                    }

                    double uf = result is ApertureGlazingCalculationResult apertureGlazingCalculationResult ? apertureGlazingCalculationResult.FrameThermalTransmittance : double.NaN;
                    values[guid] = new GlazingValues(result.ThermalTransmittance, result.TotalSolarEnergyTransmittance, result.LightTransmittance, uf);
                }
            }

            return new GlazingEvaluation(values, stopwatch.ElapsedMilliseconds, values.Count == 0 ? error : null);
        }
    }
}
