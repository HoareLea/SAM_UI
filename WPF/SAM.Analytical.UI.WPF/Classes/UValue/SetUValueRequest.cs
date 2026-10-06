// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What <c>Modify.SetUValue</c> applies: an evaluated layer thickness for one construction, how (new
    /// construction or in place) and to which panels. Built by <see cref="UValueViewModel.CreateRequest"/>.
    /// </summary>
    public sealed class SetUValueRequest
    {
        /// <summary>The source construction, by identity (never by name).</summary>
        public Guid ConstructionGuid { get; set; }

        /// <summary>The layer whose thickness changes.</summary>
        public int LayerIndex { get; set; }

        /// <summary>The evaluated thickness [m]; rounded to 1 mm on apply, as the legacy flow does.</summary>
        public double Thickness { get; set; } = double.NaN;

        /// <summary>U-value before the change [W/m²K] (for the result / report).</summary>
        public double InitialThermalTransmittance { get; set; } = double.NaN;

        /// <summary>The evaluated U-value at <see cref="Thickness"/> [W/m²K] (for the result / report).</summary>
        public double CalculatedThermalTransmittance { get; set; } = double.NaN;

        /// <summary>The U-value the user asked for [W/m²K] (for the result / report).</summary>
        public double TargetThermalTransmittance { get; set; } = double.NaN;

        /// <summary>Heat-flow basis the U-values were calculated for (for the result / report).</summary>
        public HeatFlowDirection HeatFlowDirection { get; set; }

        public UValueApplyMode Mode { get; set; } = UValueApplyMode.NewConstruction;

        /// <summary>Ignored for <see cref="UValueApplyMode.ModifyInPlace"/>, which always affects every panel using the construction.</summary>
        public ThermalApplyScope Scope { get; set; } = ThermalApplyScope.AllUsing;

        /// <summary>The selected panels; used by <see cref="ThermalApplyScope.SelectedOnly"/>.</summary>
        public IEnumerable<Guid> SelectedPanelGuids { get; set; }

        /// <summary>Name for <see cref="UValueApplyMode.NewConstruction"/>; when empty or taken, <c>Query.UValueConstructionName</c> is used.</summary>
        public string NewConstructionName { get; set; }
    }
}
