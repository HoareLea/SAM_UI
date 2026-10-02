// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The U-values of SEVERAL constructions as they are (no layer is varied), calculated together: the Thermal Performance panel's
    /// construction alternatives need the U-value of every existing construction in a pool, so they are answered by ONE
    /// Tas run per pool (the existing <c>ThermalTransmittanceCalculator</c> takes any number of constructions) instead of one
    /// call per construction per redraw. The real implementation is <see cref="TasConstructionUValueEvaluator"/>; tests pass a stand-in.
    /// </summary>
    public interface IConstructionUValueEvaluator
    {
        /// <summary>
        /// Calculates every construction of <paramref name="request"/>. A cancelled token means the caller no longer wants the result:
        /// the returned task is cancelled, even if the calculation itself cannot be interrupted.
        /// </summary>
        Task<IReadOnlyList<ConstructionUValue>> EvaluateAsync(ConstructionUValueRequest request, CancellationToken cancellationToken);
    }

    /// <summary>The constructions of one pool and the heat-flow basis to calculate them on. Immutable: it crosses to the worker thread.</summary>
    public sealed class ConstructionUValueRequest
    {
        public ConstructionUValueRequest(IEnumerable<Construction> constructions, MaterialLibrary materialLibrary, HeatFlowDirection heatFlowDirection, bool external)
        {
            Constructions = (constructions ?? Enumerable.Empty<Construction>()).Where(x => x != null).Select(x => new Construction(x)).ToList();
            MaterialLibrary = materialLibrary == null ? null : new MaterialLibrary(materialLibrary);
            HeatFlowDirection = heatFlowDirection;
            External = external;
        }

        /// <summary>The constructions (private copies), identified by Guid.</summary>
        public IReadOnlyList<Construction> Constructions { get; }

        /// <summary>The materials the constructions name (a private copy).</summary>
        public MaterialLibrary MaterialLibrary { get; }

        public HeatFlowDirection HeatFlowDirection { get; }

        /// <summary>External surface resistances.</summary>
        public bool External { get; }
    }

    /// <summary>The U-value of one construction of a <see cref="ConstructionUValueRequest"/>; NaN when it could not be calculated, with the reason.</summary>
    public sealed class ConstructionUValue
    {
        public ConstructionUValue(Guid guid, double thermalTransmittance, string message, long elapsedMilliseconds)
        {
            Guid = guid;
            ThermalTransmittance = thermalTransmittance;
            Message = message;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        /// <summary>The construction, by identity.</summary>
        public Guid Guid { get; }

        /// <summary>U-value [W/m²K] on the request's basis; NaN when not calculated.</summary>
        public double ThermalTransmittance { get; }

        /// <summary>Why it was not calculated; null when it was.</summary>
        public string Message { get; }

        /// <summary>Wall-clock time of the whole batch this belongs to [ms] (the same for every construction of a batch).</summary>
        public long ElapsedMilliseconds { get; }

        public bool Calculated => !double.IsNaN(ThermalTransmittance);
    }
}
