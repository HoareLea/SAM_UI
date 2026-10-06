// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Adding a material to a library that is keyed by NAME without ever silently replacing another definition: an identical material is
    /// reused, a different one with the same name (e.g. the same IGDB product from v69 and v76) is added under a new name - "name (source)",
    /// then "name 2", "name 3"... - and the caller rewrites its <see cref="ConstructionLayer"/> to the returned name. Used when composing a
    /// draft (two panes of one name from different sources), when saving into a user library and when archiving a removed entry. Nothing in
    /// it is glazing-specific.
    /// </summary>
    internal static class LibraryMaterialMerge
    {
        /// <summary>Adds <paramref name="material"/> (or finds its identical twin) and returns the name the layer must use.</summary>
        internal static string Add(MaterialLibrary materialLibrary, IMaterial material, string sourceLabel = null)
        {
            if (materialLibrary == null || material?.Name == null)
            {
                return material?.Name;
            }

            IMaterial existing = materialLibrary.GetMaterial(material.Name);
            if (existing == null)
            {
                materialLibrary.Add(material);
                return material.Name;
            }

            if (MaterialIdentity.Same(existing, material))
            {
                return material.Name;
            }

            string label = DraftPane.FileNameOnly(sourceLabel);
            int number = string.IsNullOrWhiteSpace(label) ? 2 : 1;
            for (; number < 10000; number++)
            {
                string name = number == 1
                    ? string.Format(CultureInfo.InvariantCulture, "{0} ({1})", material.Name, label.Trim())
                    : string.Format(CultureInfo.InvariantCulture, "{0} {1}", material.Name, number);

                IMaterial renamed = Rename(material, name);
                if (renamed == null)
                {
                    return null;
                }

                existing = materialLibrary.GetMaterial(name);
                if (existing == null)
                {
                    materialLibrary.Add(renamed);
                    return name;
                }

                if (MaterialIdentity.Same(existing, renamed))
                {
                    return name;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes from <paramref name="materialLibrary"/> the materials named in <paramref name="candidates"/> (case-sensitive, as material names
        /// are) that no name in <paramref name="referenced"/> uses, and returns them. A material that is not a candidate is never touched, and a
        /// candidate that something still references is never removed - so pruning after a Remove can only ever drop what the removed entry alone used.
        /// </summary>
        internal static List<IMaterial> Prune(MaterialLibrary materialLibrary, IEnumerable<string> candidates, IEnumerable<string> referenced)
        {
            List<IMaterial> result = new List<IMaterial>();
            if (materialLibrary == null || candidates == null)
            {
                return result;
            }

            HashSet<string> names_Referenced = new HashSet<string>((referenced ?? Enumerable.Empty<string>()).Where(x => x != null));
            foreach (string name in candidates.Where(x => x != null).Distinct())
            {
                if (names_Referenced.Contains(name))
                {
                    continue;
                }

                IMaterial material = materialLibrary.GetMaterial(name);
                if (material != null && materialLibrary.Remove(material))
                {
                    result.Add(material);
                }
            }

            return result;
        }

        /// <summary>The names of the materials the layers of <paramref name="apertureConstruction"/> (pane and frame) use.</summary>
        internal static IEnumerable<string> ReferencedNames(ApertureConstruction apertureConstruction)
        {
            return (apertureConstruction?.PaneConstructionLayers ?? new List<ConstructionLayer>())
                .Concat(apertureConstruction?.FrameConstructionLayers ?? new List<ConstructionLayer>())
                .Select(x => x?.Name)
                .Where(x => x != null);
        }

        /// <summary>The names of the materials the layers of <paramref name="construction"/> use.</summary>
        internal static IEnumerable<string> ReferencedNames(Construction construction)
        {
            return (construction?.ConstructionLayers ?? new List<ConstructionLayer>()).Select(x => x?.Name).Where(x => x != null);
        }

        /// <summary>Copies of <paramref name="constructionLayers"/> with the material names in <paramref name="names"/> replaced (null stays null).</summary>
        internal static List<ConstructionLayer> RenameLayers(List<ConstructionLayer> constructionLayers, IReadOnlyDictionary<string, string> names)
        {
            return constructionLayers?.Select(x => new ConstructionLayer(x.Name != null && names.TryGetValue(x.Name, out string name) ? name : x.Name, x.Thickness)).ToList();
        }

        /// <summary>A copy of <paramref name="material"/> under another name (and its own Guid); everything else unchanged.</summary>
        internal static IMaterial Rename(IMaterial material, string name)
        {
            JsonObject jsonObject = material?.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            jsonObject["Name"] = name;
            jsonObject["Guid"] = Guid.NewGuid().ToString();
            return Core.Create.IJSAMObject<IMaterial>(jsonObject);
        }
    }
}
