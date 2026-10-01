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
    /// One complete glazing system the user can choose: an aperture construction (pane stack with gas gaps, plus frame
    /// layers when it has them) from a <see cref="GlazingSource"/>. Never a single pane material, which does not define
    /// a Ug. <b>Identity is the Guid</b>: several systems can share a name (the default library has five
    /// "SIM_EXT_GLZ"), so the name is only shown.
    /// </summary>
    public sealed class GlazingCandidate
    {
        internal GlazingCandidate(ApertureConstruction apertureConstruction, GlazingSource source, IReadOnlyDictionary<string, IMaterial> modelMaterials)
        {
            ApertureConstruction = apertureConstruction ?? throw new ArgumentNullException(nameof(apertureConstruction));
            Source = source ?? throw new ArgumentNullException(nameof(source));

            Dictionary<string, IMaterial> materials_Source = source.GetMaterials();
            List<string> names = new List<string>();
            foreach (ConstructionLayer constructionLayer in (apertureConstruction.PaneConstructionLayers ?? new List<ConstructionLayer>()).Concat(apertureConstruction.FrameConstructionLayers ?? new List<ConstructionLayer>()))
            {
                if (constructionLayer?.Name != null && !names.Contains(constructionLayer.Name))
                {
                    names.Add(constructionLayer.Name);
                }
            }

            // Which materials Apply must add to the model, and whether a name clash blocks the candidate.
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
                    if (!Same(material, material_Model))
                    {
                        MaterialIssue = string.Format(CultureInfo.CurrentCulture, "Its material '{0}' differs from the model's material of the same name.", name);
                        break;
                    }

                    continue;
                }

                toAdd.Add(material);
            }

            MaterialsToAdd = toAdd;
        }

        public Guid Guid => ApertureConstruction.Guid;

        public string Name => ApertureConstruction.Name;

        /// <summary>The last 6 characters of the Guid: tells same-named systems apart in a tooltip (libraries often share the first group).</summary>
        public string ShortId => ApertureConstruction.Guid.ToString().Substring(30);

        public ApertureConstruction ApertureConstruction { get; }

        public GlazingSource Source { get; }

        public GlazingSourceKind Kind => Source.Kind;

        public ApertureType ApertureType => ApertureConstruction.ApertureType;

        public bool HasFrame => ApertureConstruction.HasFrameConstructionLayers();

        /// <summary>The description from the construction (e.g. a TCD description), or empty.</summary>
        public string Description => ApertureConstruction.TryGetValue(ApertureConstructionParameter.Description, out string description) && description != null ? description : string.Empty;

        /// <summary>The pane build-up, e.g. "6 Clear 6mm / 12 Argon / 6 Clear 6mm" (thickness in mm, then the material).</summary>
        public string PaneBuildUp => BuildUp(ApertureConstruction.PaneConstructionLayers);

        /// <summary>The frame build-up, or empty for a system without frame layers.</summary>
        public string FrameBuildUp => BuildUp(ApertureConstruction.FrameConstructionLayers);

        /// <summary>The materials of this system that the model does not have yet (Apply adds exactly these).</summary>
        public IReadOnlyList<IMaterial> MaterialsToAdd { get; }

        /// <summary>
        /// Why this system cannot be applied to the model (a material it needs is missing from its source, or a
        /// material of the same name but another definition is already in the model); null when nothing blocks it.
        /// </summary>
        public string MaterialIssue { get; }

        private static string BuildUp(List<ConstructionLayer> constructionLayers)
        {
            if (constructionLayers == null || constructionLayers.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" / ", constructionLayers.Where(x => x != null).Select(x => string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", x.Thickness * 1000, x.Name)));
        }

        // Same definition = same JSON once the object's own Guid and the parameters without a value are left out (a material
        // copied between libraries keeps its properties but may get a new Guid, and a NaN parameter is not written to a file,
        // so a material read from a file would otherwise differ from the same one still in memory).
        private static bool Same(IMaterial material_1, IMaterial material_2)
        {
            if (ReferenceEquals(material_1, material_2))
            {
                return true;
            }

            if (material_1 == null || material_2 == null || material_1.GetType() != material_2.GetType())
            {
                return false;
            }

            return Json(material_1) == Json(material_2);
        }

        private static string Json(IMaterial material)
        {
            string json = material.ToJsonObject()?.ToJsonString() ?? string.Empty;
            json = System.Text.RegularExpressions.Regex.Replace(json, "\"Guid\":\"[0-9a-fA-F-]{36}\",?", string.Empty);
            json = System.Text.RegularExpressions.Regex.Replace(json, @",\{""Name"":""[^""]*""\}", string.Empty);
            return System.Text.RegularExpressions.Regex.Replace(json, @"\{""Name"":""[^""]*""\},?", string.Empty);
        }
    }
}
