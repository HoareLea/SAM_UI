// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The seam between the "Set U-value" view-model and the U-value calculation. The app uses
    /// <see cref="TasUValueEvaluator"/> (real Tas TCD calculation on one STA worker); tests use a fake.
    /// </summary>
    public interface IUValueEvaluator
    {
        /// <summary>
        /// Evaluates <paramref name="request"/>. A cancelled token means the caller no longer wants the result:
        /// the returned task is cancelled, even if the calculation itself cannot be interrupted.
        /// </summary>
        Task<UValueEvaluation> EvaluateAsync(UValueEvaluationRequest request, CancellationToken cancellationToken);
    }
}
