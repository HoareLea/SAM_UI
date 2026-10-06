// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Calculates Ug / Uf / g / light transmittance for glazing systems. The view-model talks to this seam: the real
    /// implementation drives Tas TCD (<see cref="TasGlazingEvaluator"/>), tests substitute a fake.
    /// </summary>
    public interface IGlazingEvaluator
    {
        Task<GlazingEvaluation> EvaluateAsync(GlazingEvaluationRequest request, CancellationToken cancellationToken);
    }

    /// <summary>The systems of one <see cref="GlazingSource"/> to calculate (each source has its own materials).</summary>
    public sealed class GlazingEvaluationBatch
    {
        public GlazingEvaluationBatch(GlazingSource source, IEnumerable<Guid> guids)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Guids = new List<Guid>(guids ?? new Guid[0]);
        }

        public GlazingSource Source { get; }

        public IReadOnlyList<Guid> Guids { get; }
    }

    public sealed class GlazingEvaluationRequest
    {
        public GlazingEvaluationRequest(IEnumerable<GlazingEvaluationBatch> batches)
        {
            Batches = new List<GlazingEvaluationBatch>(batches ?? new GlazingEvaluationBatch[0]);
        }

        public IReadOnlyList<GlazingEvaluationBatch> Batches { get; }
    }

    public sealed class GlazingEvaluation
    {
        public GlazingEvaluation(IReadOnlyDictionary<Guid, GlazingValues> values, long elapsedMilliseconds, string? error = null)
        {
            Values = values ?? new Dictionary<Guid, GlazingValues>();
            ElapsedMilliseconds = elapsedMilliseconds;
            Error = error;
        }

        /// <summary>The values by system Guid; a system Tas could not calculate is absent.</summary>
        public IReadOnlyDictionary<Guid, GlazingValues> Values { get; }

        public long ElapsedMilliseconds { get; }

        /// <summary>Why nothing could be calculated (Tas unavailable); null otherwise.</summary>
        public string? Error { get; }
    }
}
