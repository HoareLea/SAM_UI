// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One place glazing systems come from: the model, the default library, or a file loaded for this window. It holds the
    /// systems (aperture constructions) together with the materials they name, as a <see cref="ConstructionManager"/>,
    /// which is also what Tas needs to calculate them. The model is never touched by creating one.
    /// <para>
    /// The same pool also carries the OPAQUE constructions of a source (<see cref="GetConstructions"/>): the Thermal Performance
    /// panel's construction alternatives read them from it, so a source is one thing for panels and apertures alike.
    /// </para>
    /// </summary>
    public sealed class GlazingSource
    {
        public GlazingSource(GlazingSourceKind kind, string label, ConstructionManager constructionManager)
        {
            Kind = kind;
            Label = label ?? string.Empty;
            ConstructionManager = constructionManager;
        }

        public GlazingSourceKind Kind { get; }

        /// <summary>Shown in the table, e.g. "Model", "Default library", "Constructions.tcd".</summary>
        public string Label { get; }

        public ConstructionManager ConstructionManager { get; }

        /// <summary>
        /// A short plain-language note about the source that is not a candidate list, e.g. "This file contains 11,664
        /// panes and no glazing systems." Null when there is nothing to say.
        /// </summary>
        public string Note { get; set; }

        /// <summary>The systems of the given aperture type, de-duplicated by Guid within the source.</summary>
        public List<ApertureConstruction> GetApertureConstructions(ApertureType apertureType)
        {
            List<ApertureConstruction> result = new List<ApertureConstruction>();
            HashSet<Guid> guids = new HashSet<Guid>();
            foreach (ApertureConstruction apertureConstruction in ConstructionManager?.ApertureConstructions ?? new List<ApertureConstruction>())
            {
                if (apertureConstruction != null && apertureConstruction.ApertureType == apertureType && guids.Add(apertureConstruction.Guid))
                {
                    result.Add(apertureConstruction);
                }
            }

            return result;
        }

        /// <summary>The opaque (panel) constructions of the source, de-duplicated by Guid within the source.</summary>
        public List<Construction> GetConstructions()
        {
            List<Construction> result = new List<Construction>();
            HashSet<Guid> guids = new HashSet<Guid>();
            foreach (Construction construction in ConstructionManager?.Constructions ?? new List<Construction>())
            {
                if (construction != null && guids.Add(construction.Guid))
                {
                    result.Add(construction);
                }
            }

            return result;
        }

        /// <summary>The materials of the source by name (the first of a name wins, as the library keys by name).</summary>
        public Dictionary<string, IMaterial> GetMaterials()
        {
            Dictionary<string, IMaterial> result = new Dictionary<string, IMaterial>();
            foreach (IMaterial material in ConstructionManager?.MaterialLibrary?.GetMaterials() ?? new List<IMaterial>())
            {
                if (material?.Name != null && !result.ContainsKey(material.Name))
                {
                    result.Add(material.Name, material);
                }
            }

            return result;
        }

        /// <summary>The panel constructions already in the model, with the model's own materials. Read only.</summary>
        public static GlazingSource ConstructionsFromModel(AnalyticalModel analyticalModel)
        {
            MaterialLibrary materialLibrary = analyticalModel?.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");

            List<Construction> constructions = new List<Construction>();
            HashSet<Guid> guids = new HashSet<Guid>();
            foreach (Construction construction in analyticalModel?.AdjacencyCluster?.GetConstructions() ?? new List<Construction>())
            {
                if (construction != null && guids.Add(construction.Guid))
                {
                    constructions.Add(construction);
                }
            }

            return new GlazingSource(GlazingSourceKind.Model, "Model", new ConstructionManager(null, constructions, materialLibrary));
        }

        /// <summary>The default construction library with the default Material Library. Read only.</summary>
        public static GlazingSource ConstructionsFromDefaultLibrary()
        {
            ConstructionLibrary constructionLibrary = Analytical.Query.DefaultConstructionLibrary();
            MaterialLibrary materialLibrary = Analytical.Query.DefaultMaterialLibrary();

            return new GlazingSource(GlazingSourceKind.Library, "Default library", new ConstructionManager(null, constructionLibrary?.GetConstructions(), materialLibrary));
        }

        /// <summary>The systems already in the model, with the model's own materials. Read only.</summary>
        public static GlazingSource FromModel(AnalyticalModel analyticalModel)
        {
            MaterialLibrary materialLibrary = analyticalModel?.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");

            List<ApertureConstruction> apertureConstructions = new List<ApertureConstruction>();
            HashSet<Guid> guids = new HashSet<Guid>();
            foreach (ApertureConstruction apertureConstruction in analyticalModel?.AdjacencyCluster?.GetApertureConstructions() ?? new List<ApertureConstruction>())
            {
                if (apertureConstruction != null && guids.Add(apertureConstruction.Guid))
                {
                    apertureConstructions.Add(apertureConstruction);
                }
            }

            return new GlazingSource(GlazingSourceKind.Model, "Model", new ConstructionManager(apertureConstructions, null, materialLibrary));
        }

        /// <summary>The default aperture construction library with the default Material Library. Read only.</summary>
        public static GlazingSource FromDefaultLibrary()
        {
            ApertureConstructionLibrary apertureConstructionLibrary = Analytical.Query.DefaultApertureConstructionLibrary();
            MaterialLibrary materialLibrary = Analytical.Query.DefaultMaterialLibrary();

            return new GlazingSource(GlazingSourceKind.Library, "Default library", new ConstructionManager(apertureConstructionLibrary?.GetApertureConstructions(), null, materialLibrary));
        }
    }
}
