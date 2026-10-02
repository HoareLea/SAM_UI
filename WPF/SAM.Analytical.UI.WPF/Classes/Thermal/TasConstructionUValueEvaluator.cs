// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The real <see cref="IConstructionUValueEvaluator"/>: the existing Tas <see cref="ThermalTransmittanceCalculator"/> on one STA
    /// worker (TCD is COM). It adds no calculation of its own: the constructions of a request go to ONE
    /// <c>Calculate(guids)</c> call (in chunks, so a very large pool cannot hold one TCD document for minutes), and each result is read on
    /// the request's heat-flow basis. A construction whose material is not in the request's Material Library is reported as not
    /// calculated rather than sent to Tas.
    /// <para>
    /// Requests are served in the order they arrive, none dropped: unlike the single-flight worker of the target evaluation there is
    /// no "newer supersedes older", because two rows can ask at once and each needs its answer. A cancelled request that has not
    /// started is skipped; one that is running finishes and its result is dropped by the caller (the cache keeps it, keyed by basis).
    /// </para>
    /// </summary>
    public sealed class TasConstructionUValueEvaluator : IConstructionUValueEvaluator, IDisposable
    {
        /// <summary>Constructions per Tas run.</summary>
        public const int ChunkSize = 40;

        private sealed class Item
        {
            public Item(ConstructionUValueRequest request, CancellationToken cancellationToken)
            {
                Request = request;
                CancellationToken = cancellationToken;
                Completion = new TaskCompletionSource<IReadOnlyList<ConstructionUValue>>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public ConstructionUValueRequest Request { get; }

            public CancellationToken CancellationToken { get; }

            public TaskCompletionSource<IReadOnlyList<ConstructionUValue>> Completion { get; }
        }

        private readonly Func<ConstructionManager, IEnumerable<Guid>, List<ThermalTransmittanceCalculationResult>> calculate;
        private readonly BlockingCollection<Item> queue = new BlockingCollection<Item>();
        private readonly object gate = new object();
        private Thread thread;

        public TasConstructionUValueEvaluator()
            : this(null)
        {
        }

        /// <param name="calculate">Stand-in for the TCD U-value run (tests); null for the real calculator.</param>
        internal TasConstructionUValueEvaluator(Func<ConstructionManager, IEnumerable<Guid>, List<ThermalTransmittanceCalculationResult>> calculate)
        {
            this.calculate = calculate ?? ((constructionManager, guids) => new ThermalTransmittanceCalculator(constructionManager).Calculate(guids));
        }

        public Task<IReadOnlyList<ConstructionUValue>> EvaluateAsync(ConstructionUValueRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Item item = new Item(request, cancellationToken);
            CancellationTokenRegistration registration = cancellationToken.Register(() => item.Completion.TrySetCanceled(cancellationToken));
            item.Completion.Task.ContinueWith(x => registration.Dispose(), TaskScheduler.Default);

            try
            {
                EnsureThread();
                queue.Add(item);
            }
            catch (InvalidOperationException)
            {
                item.Completion.TrySetCanceled();
            }

            return item.Completion.Task;
        }

        public void Dispose()
        {
            queue.CompleteAdding();
        }

        // The worker starts with the first request, so an evaluator that is only called synchronously (a test stand-in) owns no thread.
        private void EnsureThread()
        {
            lock (gate)
            {
                if (thread != null)
                {
                    return;
                }

                thread = new Thread(Run) { IsBackground = true, Name = "SAM construction U-value evaluator" };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
        }

        private void Run()
        {
            foreach (Item item in queue.GetConsumingEnumerable())
            {
                if (item.CancellationToken.IsCancellationRequested)
                {
                    item.Completion.TrySetCanceled(item.CancellationToken);
                    continue;
                }

                try
                {
                    item.Completion.TrySetResult(Evaluate(item.Request));
                }
                catch (Exception exception)
                {
                    item.Completion.TrySetException(exception);
                }
            }
        }

        /// <summary>One batch, synchronously, on the calling thread (which must be STA for TCD).</summary>
        internal IReadOnlyList<ConstructionUValue> Evaluate(ConstructionUValueRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<ConstructionUValue> results = new List<ConstructionUValue>();

            // What can be sent to Tas: every layer's material is in the request's library.
            List<Construction> eligible = new List<Construction>();
            foreach (Construction construction in request.Constructions)
            {
                string missing = MissingMaterial(construction, request.MaterialLibrary);
                if (missing != null)
                {
                    results.Add(new ConstructionUValue(construction.Guid, double.NaN, missing.Length == 0 ? "It has no usable layers." : string.Format(CultureInfo.CurrentCulture, "Its material '{0}' is not in the Material Library.", missing), 0));
                }
                else
                {
                    eligible.Add(construction);
                }
            }

            for (int index = 0; index < eligible.Count; index += ChunkSize)
            {
                List<Construction> chunk = eligible.GetRange(index, Math.Min(ChunkSize, eligible.Count - index));

                List<ThermalTransmittanceCalculationResult> calculated = null;
                string failure = null;
                try
                {
                    ConstructionManager constructionManager = new ConstructionManager(null, chunk, request.MaterialLibrary);
                    calculated = calculate(constructionManager, chunk.Select(x => x.Guid).ToList());
                }
                catch (Exception exception)
                {
                    failure = exception.Message;
                }

                foreach (Construction construction in chunk)
                {
                    ThermalTransmittanceCalculationResult result = calculated?.Find(x => x?.Reference == construction.Guid.ToString());
                    double u = result?.GetThermalTransmittance(request.HeatFlowDirection, request.External) ?? double.NaN;
                    results.Add(new ConstructionUValue(construction.Guid, u, double.IsNaN(u) ? (failure ?? "Tas did not return a U-value for it.") : null, 0));
                }
            }

            long elapsed = stopwatch.ElapsedMilliseconds;
            return results.Select(x => new ConstructionUValue(x.Guid, x.ThermalTransmittance, x.Message, elapsed)).ToList();
        }

        private static string MissingMaterial(Construction construction, MaterialLibrary materialLibrary)
        {
            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if (constructionLayers == null || constructionLayers.Count == 0)
            {
                return string.Empty;
            }

            foreach (ConstructionLayer constructionLayer in constructionLayers)
            {
                if (constructionLayer?.Name == null || materialLibrary?.GetMaterial(constructionLayer.Name) == null)
                {
                    return constructionLayer?.Name ?? string.Empty;
                }
            }

            return null;
        }
    }
}
