// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The Ug / g / LT / Uf already calculated for glazing systems this session, keyed by CONTENT (as <see cref="ConstructionUValueCache"/> is
    /// for opaque U-values): the pane and frame layers (thickness and the full definition of each material) and the additional heat transfer -
    /// not the system's Guid or name. So the Builder never asks Tas twice for a build-up it has seen (going back to an earlier gap width is
    /// instant), a draft and the same system saved under a new Guid share a value, and an edited material is never answered from an old one.
    /// Only calculated values are kept: a failure is asked again next time.
    /// </summary>
    public sealed class GlazingValuesCache
    {
        private readonly ConcurrentDictionary<string, GlazingValues> values = new ConcurrentDictionary<string, GlazingValues>();
        private int hits;
        private int misses;

        public int Count => values.Count;

        public int Hits => hits;

        public int Misses => misses;

        /// <summary>The content key of a system; null when it cannot be keyed (no pane layers, or a material it names is missing).</summary>
        public static string Key(ApertureConstruction apertureConstruction, MaterialLibrary materialLibrary)
        {
            List<ConstructionLayer> paneLayers = apertureConstruction?.PaneConstructionLayers;
            if (paneLayers == null || paneLayers.Count == 0 || materialLibrary == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();
            if (!Append(stringBuilder, paneLayers, materialLibrary))
            {
                return null;
            }

            stringBuilder.Append("|frame|");
            if (!Append(stringBuilder, apertureConstruction.FrameConstructionLayers, materialLibrary))
            {
                return null;
            }

            stringBuilder.Append('|').Append(Number(apertureConstruction, ApertureConstructionParameter.PaneAdditionalHeatTransfer));
            stringBuilder.Append('|').Append(Number(apertureConstruction, ApertureConstructionParameter.FrameAdditionalHeatTransfer));

            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()))).Replace("-", string.Empty);
            }
        }

        public bool TryGet(string key, out GlazingValues glazingValues)
        {
            glazingValues = null;
            if (key != null && values.TryGetValue(key, out glazingValues))
            {
                System.Threading.Interlocked.Increment(ref hits);
                return true;
            }

            System.Threading.Interlocked.Increment(ref misses);
            return false;
        }

        public void Set(string key, GlazingValues glazingValues)
        {
            if (key != null && glazingValues != null)
            {
                values[key] = glazingValues;
            }
        }

        private static bool Append(StringBuilder stringBuilder, List<ConstructionLayer> constructionLayers, MaterialLibrary materialLibrary)
        {
            foreach (ConstructionLayer constructionLayer in constructionLayers ?? new List<ConstructionLayer>())
            {
                IMaterial material = constructionLayer?.Name == null ? null : materialLibrary.GetMaterial(constructionLayer.Name);
                if (material == null)
                {
                    return false;
                }

                stringBuilder.Append(constructionLayer.Thickness.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(MaterialIdentity.Json(material)).Append(';');
            }

            return true;
        }

        private static string Number(ApertureConstruction apertureConstruction, ApertureConstructionParameter parameter)
        {
            return apertureConstruction.TryGetValue(parameter, out double value) && !double.IsNaN(value) ? value.ToString("R", CultureInfo.InvariantCulture) : "-";
        }
    }
}
