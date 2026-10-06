// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One U-value evaluation: which layer of which construction to vary, towards which target, on which
    /// heat-flow basis and within which thickness range. Immutable: it crosses to the evaluator's worker thread.
    /// </summary>
    public sealed class UValueEvaluationRequest
    {
        public UValueEvaluationRequest(Construction construction, MaterialLibrary materialLibrary, int layerIndex, double targetThermalTransmittance, HeatFlowDirection heatFlowDirection, bool external, double minThickness, double maxThickness)
        {
            Construction = construction == null ? null : new Construction(construction);
            MaterialLibrary = materialLibrary == null ? null : new MaterialLibrary(materialLibrary);
            LayerIndex = layerIndex;
            TargetThermalTransmittance = targetThermalTransmittance;
            HeatFlowDirection = heatFlowDirection;
            External = external;
            MinThickness = minThickness;
            MaxThickness = maxThickness;
        }

        /// <summary>The construction as it is now (a private copy).</summary>
        public Construction Construction { get; }

        /// <summary>The model's material library (a private copy).</summary>
        public MaterialLibrary MaterialLibrary { get; }

        /// <summary>The layer to vary; -1 lets the calculator pick (<c>Tas.Query.AdjustableLayerIndex</c>).</summary>
        public int LayerIndex { get; }

        /// <summary>Target U-value [W/m²K]; NaN asks only for the current U-value and the U-values at the range ends.</summary>
        public double TargetThermalTransmittance { get; }

        public HeatFlowDirection HeatFlowDirection { get; }

        public bool External { get; }

        /// <summary>Minimum layer thickness [m].</summary>
        public double MinThickness { get; }

        /// <summary>Maximum layer thickness [m].</summary>
        public double MaxThickness { get; }

        /// <summary>True when only the current U-value and the range ends are wanted, not a thickness.</summary>
        public bool ProbeOnly => double.IsNaN(TargetThermalTransmittance);
    }
}
