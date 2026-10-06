// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>How <see cref="Query.ComposeGlazingSystem"/> composes a draft.</summary>
    public sealed class GlazingComposeOptions
    {
        /// <summary>The Guid of the composed system; null for the draft's <see cref="GlazingSystemDraft.EvaluationGuid"/> (Save passes a new one).</summary>
        public Guid? Guid { get; set; }

        /// <summary>The definition of each gas (λ, ρ, cp, μ); null for SAM's default gas library (<c>Analytical.Query.DefaultGasMaterial</c>).</summary>
        public Func<DefaultGasType, GasMaterial> GasSource { get; set; }
    }

    /// <summary>
    /// A draft composed into what SAM stores: a complete <see cref="ApertureConstruction"/> (pane layers INSIDE → OUTSIDE) with a
    /// <see cref="MaterialLibrary"/> holding ONLY the materials it names, plus what the Builder needs to describe it. Pure data: composing
    /// never touches a model, a library file or Tas.
    /// </summary>
    public sealed class GlazingComposition
    {
        internal GlazingComposition(ApertureConstruction apertureConstruction, MaterialLibrary materialLibrary, IEnumerable<GlazingDraftIssue> issues, GlazingBuilderProvenance provenance, IReadOnlyDictionary<string, string> materialSourceLabels)
        {
            ApertureConstruction = apertureConstruction;
            MaterialLibrary = materialLibrary;
            Issues = (issues ?? Enumerable.Empty<GlazingDraftIssue>()).ToList();
            Provenance = provenance;
            MaterialSourceLabels = materialSourceLabels ?? new Dictionary<string, string>();

            List<string> missing = new List<string>();
            foreach (ConstructionLayer constructionLayer in (apertureConstruction?.PaneConstructionLayers ?? new List<ConstructionLayer>()).Concat(apertureConstruction?.FrameConstructionLayers ?? new List<ConstructionLayer>()))
            {
                if (constructionLayer?.Name == null || materialLibrary?.GetMaterial(constructionLayer.Name) == null)
                {
                    missing.Add(constructionLayer?.Name ?? string.Empty);
                }
            }

            MissingMaterials = missing;
            ContentKey = IsComplete ? GlazingValuesCache.Key(apertureConstruction, materialLibrary) : null;
        }

        public ApertureConstruction ApertureConstruction { get; }

        /// <summary>The materials the system names - and nothing else.</summary>
        public MaterialLibrary MaterialLibrary { get; }

        /// <summary>What composing found (e.g. a gas without a definition); the full list is <see cref="Query.CheckGlazingDraft"/>'s.</summary>
        public IReadOnlyList<GlazingDraftIssue> Issues { get; }

        /// <summary>The layer names whose material is not in <see cref="MaterialLibrary"/>.</summary>
        public IReadOnlyList<string> MissingMaterials { get; }

        /// <summary>
        /// True when every layer has its material and the system has panes. An incomplete system is never calculated or saved (Tas would
        /// silently calculate a blank layer for a missing material).
        /// </summary>
        public bool IsComplete => ApertureConstruction != null && MissingMaterials.Count == 0 && (ApertureConstruction.PaneConstructionLayers?.Count ?? 0) != 0;

        /// <summary>The <see cref="GlazingValuesCache"/> key (content, not Guid or name); null when incomplete.</summary>
        public string ContentKey { get; }

        /// <summary>How it was built (no performance and no creation time yet: Save adds them).</summary>
        public GlazingBuilderProvenance Provenance { get; }

        /// <summary>The source label of each pane / frame material, by the material's name in <see cref="MaterialLibrary"/> (used to name a material renamed on save).</summary>
        public IReadOnlyDictionary<string, string> MaterialSourceLabels { get; }

        /// <summary>The system as a transient one-system source, which is what the existing glazing evaluator calculates.</summary>
        public GlazingSource ToSource(string label = "Glazing System Builder draft")
        {
            return new GlazingSource(GlazingSourceKind.Loaded, label, new ConstructionManager(new List<ApertureConstruction>() { ApertureConstruction }, null, MaterialLibrary));
        }
    }
}
