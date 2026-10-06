// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>One answer of <see cref="DraftGlazingEvaluator"/>.</summary>
    public sealed class DraftGlazingEvaluation
    {
        internal DraftGlazingEvaluation(DraftGlazingEvaluationState state, long generation, string contentKey, GlazingValues values, bool fromCache, string reason, GlazingDraftValidation validation)
        {
            State = state;
            Generation = generation;
            ContentKey = contentKey;
            Values = values;
            FromCache = fromCache;
            Reason = reason;
            Validation = validation;
        }

        public DraftGlazingEvaluationState State { get; }

        /// <summary>The request number this answers; only the newest request's answer is current.</summary>
        public long Generation { get; }

        /// <summary>The content the values belong to (<see cref="GlazingValuesCache.Key"/>); null when the draft could not be composed completely.</summary>
        public string ContentKey { get; }

        /// <summary>Ug / g / LT / Uf when <see cref="DraftGlazingEvaluationState.Calculated"/>; null otherwise.</summary>
        public GlazingValues Values { get; }

        public bool FromCache { get; }

        /// <summary>Why there are no values (shown as "Not calculated: …"); null when calculated.</summary>
        public string Reason { get; }

        /// <summary>The authoring check of the draft as it was when asked.</summary>
        public GlazingDraftValidation Validation { get; }
    }

    /// <summary>
    /// Performance of a Glazing System Builder draft while it is edited, through the EXISTING glazing calculation (the
    /// <see cref="IGlazingEvaluator"/> seam; for real, <see cref="TasGlazingEvaluator"/> = SAM_Tas <c>ThermalTransmittanceCalculator.CalculateGlazing</c>
    /// on a transient one-system <see cref="Analytical.ConstructionManager"/> - no model, a throw-away TCD). No glazing physics here.
    /// <list type="bullet">
    /// <item><b>Snapshot</b>: each request composes and checks the draft at once, so later edits never leak into a running calculation.</item>
    /// <item><b>Structural errors</b>: no Tas call; "Not calculated" with the reason.</item>
    /// <item><b>Cache</b>: content-keyed (<see cref="GlazingValuesCache"/>): a build-up seen before answers at once.</item>
    /// <item><b>Debounce</b> (<see cref="DefaultDebounce"/>): a burst of edits asks Tas once, for the last state.</item>
    /// <item><b>Generation</b>: every request gets a number; an answer that is not the newest comes back
    /// <see cref="DraftGlazingEvaluationState.Superseded"/> and must not be shown (its values, still correct for their content, are cached).</item>
    /// <item><b>Failure</b>: Tas unavailable, an exception, an empty result or a non-physical Ug (Tas swallows its failures and leaves 0) is
    /// "Not calculated" - never a value.</item>
    /// </list>
    /// It owns its own <see cref="TasGlazingEvaluator"/> (its own STA worker) unless one is supplied, so the Builder never queues behind the
    /// Thermal Performance candidate list. It holds no model and writes nothing.
    /// </summary>
    public sealed class DraftGlazingEvaluator : IDisposable
    {
        /// <summary>How long edits must settle before Tas is asked (the plan's 300-400 ms).</summary>
        public static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(350);

        private readonly IGlazingEvaluator evaluator;
        private readonly bool ownsEvaluator;
        private readonly TimeSpan debounce;
        private readonly GlazingComposeOptions composeOptions;
        private readonly object gate = new object();
        private CancellationTokenSource pending;
        private long generation;
        private bool disposed;

        public DraftGlazingEvaluator()
            : this(null)
        {
        }

        /// <param name="evaluator">The calculation to use; null for a NEW <see cref="TasGlazingEvaluator"/> owned (and disposed) by this one.</param>
        /// <param name="debounce">null for <see cref="DefaultDebounce"/>.</param>
        /// <param name="cache">A cache to share; null for a new one.</param>
        /// <param name="composeOptions">How drafts are composed (tests pass their own gas definitions).</param>
        public DraftGlazingEvaluator(IGlazingEvaluator evaluator, TimeSpan? debounce = null, GlazingValuesCache cache = null, GlazingComposeOptions composeOptions = null)
        {
            ownsEvaluator = evaluator == null;
            this.evaluator = evaluator ?? new TasGlazingEvaluator();
            this.debounce = debounce ?? DefaultDebounce;
            Cache = cache ?? new GlazingValuesCache();
            this.composeOptions = composeOptions;
        }

        public GlazingValuesCache Cache { get; }

        /// <summary>The calculation this evaluator asks (its own Tas evaluator unless one was supplied).</summary>
        internal IGlazingEvaluator Evaluator => evaluator;

        internal bool OwnsEvaluator => ownsEvaluator;

        /// <summary>The newest request's number.</summary>
        public long Generation => Interlocked.Read(ref generation);

        /// <summary>The newest answer that was not superseded; null before the first.</summary>
        public DraftGlazingEvaluation Latest { get; private set; }

        /// <summary>Asks for the performance of <paramref name="draft"/> as it is now; supersedes any earlier request.</summary>
        public async Task<DraftGlazingEvaluation> EvaluateAsync(GlazingSystemDraft draft, CancellationToken cancellationToken = default)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            // A superseded request's source is cancelled, not disposed: its token may still be in use by its own (finishing) call.
            CancellationToken token;
            long current;
            lock (gate)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                pending?.Cancel();
                pending = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                token = pending.Token;
                current = ++generation;
            }

            // Snapshot: compose and check now, on the caller's thread.
            GlazingComposition composition = draft.ComposeGlazingSystem(composeOptions);
            GlazingDraftValidation validation = draft.CheckGlazingDraft(composition);
            string key = composition?.ContentKey;

            if (validation.HasErrors || composition == null || !composition.IsComplete)
            {
                return Publish(new DraftGlazingEvaluation(DraftGlazingEvaluationState.NotCalculated, current, key, null, false, "Not calculated: correct the errors in the build-up first.", validation));
            }

            if (Cache.TryGet(key, out GlazingValues cached))
            {
                return Publish(new DraftGlazingEvaluation(DraftGlazingEvaluationState.Calculated, current, key, cached, true, null, validation));
            }

            try
            {
                if (debounce > TimeSpan.Zero)
                {
                    await Task.Delay(debounce, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return Superseded(current, key, validation);
            }

            if (current != Generation)
            {
                return Superseded(current, key, validation);
            }

            Guid guid = composition.ApertureConstruction.Guid;
            GlazingEvaluation evaluation;
            try
            {
                GlazingEvaluationRequest request = new GlazingEvaluationRequest(new[] { new GlazingEvaluationBatch(composition.ToSource(), new[] { guid }) });
                evaluation = await evaluator.EvaluateAsync(request, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return Superseded(current, key, validation);
            }
            catch (Exception exception)
            {
                return Publish(new DraftGlazingEvaluation(DraftGlazingEvaluationState.NotCalculated, current, key, null, false, "Not calculated: " + exception.Message, validation));
            }

            GlazingValues values = null;
            evaluation?.Values?.TryGetValue(guid, out values);
            bool usable = values != null && !double.IsNaN(values.Ug) && values.Ug > 0;
            if (usable)
            {
                // Correct for its content even if a newer request came meanwhile.
                Cache.Set(key, values);
            }

            if (current != Generation)
            {
                return Superseded(current, key, validation);
            }

            if (!usable)
            {
                string reason = !string.IsNullOrWhiteSpace(evaluation?.Error) ? evaluation.Error : "Tas returned no values for this build-up.";
                return Publish(new DraftGlazingEvaluation(DraftGlazingEvaluationState.NotCalculated, current, key, null, false, "Not calculated: " + reason, validation));
            }

            return Publish(new DraftGlazingEvaluation(DraftGlazingEvaluationState.Calculated, current, key, values, false, null, validation));
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                pending?.Cancel();
                pending = null;
            }

            if (ownsEvaluator)
            {
                (evaluator as IDisposable)?.Dispose();
            }
        }

        private DraftGlazingEvaluation Superseded(long current, string key, GlazingDraftValidation validation)
        {
            return new DraftGlazingEvaluation(DraftGlazingEvaluationState.Superseded, current, key, null, false, "Superseded by a newer edit.", validation);
        }

        private DraftGlazingEvaluation Publish(DraftGlazingEvaluation evaluation)
        {
            lock (gate)
            {
                if (evaluation.Generation == generation)
                {
                    Latest = evaluation;
                    return evaluation;
                }
            }

            return Superseded(evaluation.Generation, evaluation.ContentKey, evaluation.Validation);
        }
    }
}
