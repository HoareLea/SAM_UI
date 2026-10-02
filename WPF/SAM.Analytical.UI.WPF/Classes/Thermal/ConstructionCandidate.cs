// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One EXISTING construction a row's panels could be given instead of the generated thickness variant: a construction already in
    /// the model, or one from the default library (later: a loaded source). <b>Identity is the Guid</b> (several constructions can
    /// share a name); the name is only shown. It stays outside the analytical model until Apply, and then only it and the materials
    /// it lacks enter. The same rules as <see cref="GlazingCandidate"/>: a material missing from the source, or one that shares a
    /// name with a model material of another definition, blocks it.
    /// </summary>
    public sealed class ConstructionCandidate
    {
        internal ConstructionCandidate(Construction construction, GlazingSource source, IReadOnlyDictionary<string, IMaterial> modelMaterials)
        {
            Construction = construction ?? throw new ArgumentNullException(nameof(construction));
            Source = source ?? throw new ArgumentNullException(nameof(source));

            Dictionary<string, IMaterial> materials_Source = source.GetMaterials();
            List<string> names = new List<string>();
            foreach (ConstructionLayer constructionLayer in construction.ConstructionLayers ?? new List<ConstructionLayer>())
            {
                if (constructionLayer?.Name != null && !names.Contains(constructionLayer.Name))
                {
                    names.Add(constructionLayer.Name);
                }
            }

            List<IMaterial> toAdd = new List<IMaterial>();
            foreach (string name in names)
            {
                if (!materials_Source.TryGetValue(name, out IMaterial material))
                {
                    MaterialIssue = string.Format(CultureInfo.CurrentCulture, "Its material '{0}' is not in {1}.", name, source.Label);
                    break;
                }

                if (modelMaterials != null && modelMaterials.TryGetValue(name, out IMaterial material_Model))
                {
                    if (!MaterialIdentity.Same(material, material_Model))
                    {
                        MaterialIssue = string.Format(CultureInfo.CurrentCulture, "Its material '{0}' differs from the model's material of the same name.", name);
                        MaterialDiffers = true;
                        break;
                    }

                    continue;
                }

                toAdd.Add(material);
            }

            MaterialsToAdd = toAdd;
        }

        public Guid Guid => Construction.Guid;

        public string Name => Construction.Name;

        /// <summary>The last 6 characters of the Guid: tells same-named constructions apart in a tooltip.</summary>
        public string ShortId => Construction.Guid.ToString().Substring(30);

        public Construction Construction { get; }

        public GlazingSource Source { get; }

        public GlazingSourceKind Kind => Source.Kind;

        /// <summary>The build-up outside to inside, e.g. "50 Air / 12 Board / 80 Mineral Wool" (thickness in mm, then the material).</summary>
        public string BuildUp => string.Join(" / ", (Construction.ConstructionLayers ?? new List<ConstructionLayer>()).Where(x => x != null).Select(x => string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", x.Thickness * 1000, x.Name)));

        /// <summary>The Default Panel Type the construction was made for (<see cref="PanelType.Undefined"/> when it has none).</summary>
        public PanelType PanelType => Analytical.Query.PanelType(Construction);

        /// <summary>The materials of this construction that the model does not have yet (Apply adds exactly these).</summary>
        public IReadOnlyList<IMaterial> MaterialsToAdd { get; }

        /// <summary>Why it cannot be applied to the model (a material missing from its source, or a material of the same name but another definition in the model); null when nothing blocks it.</summary>
        public string MaterialIssue { get; }

        /// <summary>True when <see cref="MaterialIssue"/> is a material that differs from the model's.</summary>
        public bool MaterialDiffers { get; }
    }
}
