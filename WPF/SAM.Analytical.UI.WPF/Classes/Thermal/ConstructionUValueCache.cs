// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The U-values already calculated for constructions "as they are", kept for the session so that a redraw, another target or another
    /// row never asks Tas again for a construction it has answered (the same idea as <see cref="GlazingSourceCache"/>, in memory).
    /// The key is the CONTENT that decides the U-value - the layers (material and thickness), the definition of each material, the
    /// heat-flow direction and the surface resistances - not the construction's Guid or name, so an edited material or a changed
    /// layer is never answered from an old value, and the same construction in two pools is calculated once. Only calculated
    /// values are kept: a failure (Tas unavailable) is asked again next time.
    /// </summary>
    public sealed class ConstructionUValueCache
    {
        private readonly ConcurrentDictionary<string, double> values = new ConcurrentDictionary<string, double>();
        private int hits;
        private int misses;

        /// <summary>How many U-values are kept.</summary>
        public int Count => values.Count;

        /// <summary>How many lookups were answered from the cache / were not (tests and the performance record read them).</summary>
        public int Hits => hits;

        public int Misses => misses;

        /// <summary>The key of <paramref name="construction"/> on a basis; null when it cannot be keyed (a material is missing).</summary>
        public static string Key(Construction construction, MaterialLibrary materialLibrary, HeatFlowDirection heatFlowDirection, bool external)
        {
            List<ConstructionLayer> constructionLayers = construction?.ConstructionLayers;
            if (constructionLayers == null || constructionLayers.Count == 0)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (ConstructionLayer constructionLayer in constructionLayers)
            {
                IMaterial material = constructionLayer?.Name == null ? null : materialLibrary?.GetMaterial(constructionLayer.Name);
                if (material == null)
                {
                    return null;
                }

                stringBuilder.Append(constructionLayer.Thickness.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(MaterialIdentity.Json(material)).Append(';');
            }

            stringBuilder.Append(heatFlowDirection).Append('|').Append(external);

            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()))).Replace("-", string.Empty);
            }
        }

        /// <summary>True when a U-value is kept for the key (no lookup is counted).</summary>
        public bool Contains(string key)
        {
            return key != null && values.ContainsKey(key);
        }

        /// <summary>The kept U-value; false (and a miss counted) when there is none.</summary>
        public bool TryGet(string key, out double thermalTransmittance)
        {
            thermalTransmittance = double.NaN;
            if (key != null && values.TryGetValue(key, out thermalTransmittance))
            {
                System.Threading.Interlocked.Increment(ref hits);
                return true;
            }

            System.Threading.Interlocked.Increment(ref misses);
            return false;
        }

        /// <summary>Keeps a calculated U-value (NaN, infinity, zero or a negative value is not one, and is ignored).</summary>
        public void Set(string key, double thermalTransmittance)
        {
            if (key != null && ConstructionUValue.Valid(thermalTransmittance))
            {
                values[key] = thermalTransmittance;
            }
        }
    }
}
