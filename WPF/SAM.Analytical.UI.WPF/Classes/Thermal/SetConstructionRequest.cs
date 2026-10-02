// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What <c>Modify.SetConstruction</c> applies: one EXISTING construction (from the model or a library, with the materials the model
    /// lacks) assigned to the panels that use the current construction. The alternative to <see cref="SetUValueRequest"/>, which generates
    /// a thickness variant. Built by <see cref="ConstructionAlternatives.CreateRequest"/>.
    /// </summary>
    public sealed class SetConstructionRequest
    {
        /// <summary>The construction the panels use now, by identity (never by name).</summary>
        public Guid SourceConstructionGuid { get; set; }

        /// <summary>The chosen construction. Identity is its Guid; it is added to the model unless the model has it already.</summary>
        public Construction Construction { get; set; }

        /// <summary>The materials of the chosen construction that the model does not have; only these are added.</summary>
        public IEnumerable<IMaterial> MaterialsToAdd { get; set; }

        public ThermalApplyScope Scope { get; set; } = ThermalApplyScope.AllUsing;

        /// <summary>The selected panels; used by <see cref="ThermalApplyScope.SelectedOnly"/>.</summary>
        public IEnumerable<Guid> SelectedPanelGuids { get; set; }

        /// <summary>U-value before / after on the heat-flow basis, as shown (for the result line); NaN when not available.</summary>
        public double OldThermalTransmittance { get; set; } = double.NaN;

        public double NewThermalTransmittance { get; set; } = double.NaN;

        /// <summary>The U-value the user asked for [W/m²K].</summary>
        public double TargetThermalTransmittance { get; set; } = double.NaN;

        public HeatFlowDirection HeatFlowDirection { get; set; }

        /// <summary>Where the construction comes from, e.g. "Model", "Default library" (provenance for the result).</summary>
        public string SourceLabel { get; set; }

        public GlazingSourceKind SourceKind { get; set; }
    }

    /// <summary>What <c>Modify.SetConstruction</c> did, for the result line and the check before Apply.</summary>
    public sealed class SetConstructionResult
    {
        internal SetConstructionResult(string error)
        {
            Error = error;
            PanelGuids = new List<Guid>();
            MaterialNamesAdded = new List<string>();
        }

        internal SetConstructionResult(SetConstructionRequest request, Construction sourceConstruction, Construction construction, bool constructionAdded, IReadOnlyList<string> materialNamesAdded, IReadOnlyList<Guid> panelGuids)
        {
            Scope = request.Scope;
            SourceConstruction = sourceConstruction;
            Construction = construction;
            ConstructionAdded = constructionAdded;
            MaterialNamesAdded = materialNamesAdded ?? new List<string>();
            PanelGuids = panelGuids ?? new List<Guid>();
            OldThermalTransmittance = request.OldThermalTransmittance;
            NewThermalTransmittance = request.NewThermalTransmittance;
            TargetThermalTransmittance = request.TargetThermalTransmittance;
            HeatFlowDirection = request.HeatFlowDirection;
            SourceLabel = request.SourceLabel;
            SourceKind = request.SourceKind;
            AppliedAt = DateTime.Now;
        }

        /// <summary>True when the change was applied (one Undo step).</summary>
        public bool Succeeded => Error == null;

        /// <summary>Why nothing was applied; null on success.</summary>
        public string Error { get; }

        public ThermalApplyScope Scope { get; }

        /// <summary>The construction the panels used before.</summary>
        public Construction SourceConstruction { get; }

        /// <summary>The construction now in the model (under its model name; a clashing name gets a suffix).</summary>
        public Construction Construction { get; }

        /// <summary>True when the chosen construction was added to the model by this change (it was not in the model).</summary>
        public bool ConstructionAdded { get; }

        /// <summary>The materials added to the model's Material Library by this change (only the chosen construction's).</summary>
        public IReadOnlyList<string> MaterialNamesAdded { get; }

        /// <summary>The panels assigned the construction.</summary>
        public IReadOnlyList<Guid> PanelGuids { get; }

        public int PanelCount => PanelGuids.Count;

        public double OldThermalTransmittance { get; }

        public double NewThermalTransmittance { get; }

        public double TargetThermalTransmittance { get; }

        public HeatFlowDirection HeatFlowDirection { get; }

        public string SourceLabel { get; }

        public GlazingSourceKind SourceKind { get; }

        public DateTime AppliedAt { get; }
    }
}
