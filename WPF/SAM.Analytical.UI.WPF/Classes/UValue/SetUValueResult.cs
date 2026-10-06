// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What <c>Modify.SetUValue</c> did, for the window's result line and the U-VALUE CHANGE report.
    /// Thicknesses in metres, U-values in W/m²K.
    /// </summary>
    public sealed class SetUValueResult
    {
        internal SetUValueResult(string error)
        {
            Error = error;
            PanelGuids = new List<Guid>();
        }

        internal SetUValueResult(SetUValueRequest request, Construction sourceConstruction, Construction construction, string sourceMaterialName, string materialName, bool materialAdded, double oldThickness, double newThickness, IReadOnlyList<Guid> panelGuids)
        {
            Mode = request.Mode;
            Scope = request.Mode == UValueApplyMode.ModifyInPlace ? ThermalApplyScope.AllUsing : request.Scope;
            LayerIndex = request.LayerIndex;
            OldThermalTransmittance = request.InitialThermalTransmittance;
            NewThermalTransmittance = request.CalculatedThermalTransmittance;
            TargetThermalTransmittance = request.TargetThermalTransmittance;
            HeatFlowDirection = request.HeatFlowDirection;
            SourceConstruction = sourceConstruction;
            Construction = construction;
            SourceMaterialName = sourceMaterialName;
            MaterialName = materialName;
            MaterialAdded = materialAdded;
            OldThickness = oldThickness;
            NewThickness = newThickness;
            PanelGuids = panelGuids ?? new List<Guid>();
            AppliedAt = DateTime.Now;
        }

        /// <summary>True when the change was applied (one Undo step).</summary>
        public bool Succeeded => Error == null;

        /// <summary>Why nothing was applied; null on success.</summary>
        public string Error { get; }

        public UValueApplyMode Mode { get; }

        /// <summary>The effective scope (<see cref="ThermalApplyScope.AllUsing"/> for in-place).</summary>
        public ThermalApplyScope Scope { get; }

        /// <summary>The construction before the change.</summary>
        public Construction SourceConstruction { get; }

        /// <summary>The construction after the change: the new one, or the source modified in place.</summary>
        public Construction Construction { get; }

        public int LayerIndex { get; }

        /// <summary>The layer's material before the change.</summary>
        public string SourceMaterialName { get; }

        /// <summary>The layer's material after the change (named as the legacy flow names it: "&lt;material&gt;_&lt;thickness&gt;m").</summary>
        public string MaterialName { get; }

        /// <summary>True when <see cref="MaterialName"/> was added to the model's Material Library by this change.</summary>
        public bool MaterialAdded { get; }

        public double OldThickness { get; }

        public double NewThickness { get; }

        public double OldThermalTransmittance { get; }

        public double NewThermalTransmittance { get; }

        public double TargetThermalTransmittance { get; }

        public HeatFlowDirection HeatFlowDirection { get; }

        /// <summary>The panels assigned the changed construction by this change (none for "don't assign").</summary>
        public IReadOnlyList<Guid> PanelGuids { get; }

        public int PanelCount => PanelGuids.Count;

        /// <summary>When the change was applied (the report says it may since have been undone).</summary>
        public DateTime AppliedAt { get; }
    }
}
