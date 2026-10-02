// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Globalization;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Adding a material to a library that is keyed by NAME without ever silently replacing another definition: an identical material is
    /// reused, a different one with the same name (e.g. the same IGDB product from v69 and v76) is added under a new name - "name (source)",
    /// then "name 2", "name 3"... - and the caller rewrites its <see cref="ConstructionLayer"/> to the returned name. Used when composing a
    /// draft (two panes of one name from different sources) and when saving into the user library.
    /// </summary>
    internal static class GlazingMaterialMerge
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
