// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Text.RegularExpressions;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// When two materials are "the same": materials are matched by NAME everywhere in SAM, so a candidate construction (glazing or
    /// opaque) whose material shares a name with a model material of another definition must not be applied (it would silently
    /// take the model's properties). Shared by <see cref="GlazingCandidate"/> and <see cref="ConstructionCandidate"/>.
    /// </summary>
    internal static class MaterialIdentity
    {
        // Same definition = same JSON once the object's own Guid and the parameters without a value are left out (a material
        // copied between libraries keeps its properties but may get a new Guid, and a NaN parameter is not written to a file,
        // so a material read from a file would otherwise differ from the same one still in memory).
        internal static bool Same(IMaterial material_1, IMaterial material_2)
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

        /// <summary>The definition of a material as text, without its Guid: equal for materials that are the same (see <see cref="Same"/>).</summary>
        internal static string Json(IMaterial material)
        {
            string json = material?.ToJsonObject()?.ToJsonString() ?? string.Empty;
            json = Regex.Replace(json, "\"Guid\":\"[0-9a-fA-F-]{36}\",?", string.Empty);
            json = Regex.Replace(json, @",\{""Name"":""[^""]*""\}", string.Empty);
            return Regex.Replace(json, @"\{""Name"":""[^""]*""\},?", string.Empty);
        }
    }
}
