// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        // A trailing " U0.30" (or " U0.30 (2)") from an earlier "Set U-value", so SIM_EXT_SLD U0.50 -> 0.30 gives SIM_EXT_SLD U0.30.
        private static readonly Regex uValueSuffix = new Regex(@"\s+U\d+(\.\d+)?(\s+\(\d+\))?$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Name for the construction "Set U-value" creates: "&lt;source&gt; U&lt;u&gt;" (e.g. "SIM_EXT_SLD U0.50"),
        /// with " (2)", " (3)"... when taken. An earlier U suffix on the source name is replaced, not stacked.
        /// </summary>
        /// <param name="sourceName">The construction the new one is derived from.</param>
        /// <param name="thermalTransmittance">The U-value the new construction achieves [W/m²K].</param>
        /// <param name="existingNames">Names already in the model (compared case-insensitively).</param>
        public static string UValueConstructionName(string sourceName, double thermalTransmittance, IEnumerable<string> existingNames)
        {
            string baseName = uValueSuffix.Replace((sourceName ?? string.Empty).Trim(), string.Empty);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Construction";
            }

            string name = double.IsNaN(thermalTransmittance)
                ? baseName
                : string.Format(CultureInfo.InvariantCulture, "{0} U{1}", baseName, thermalTransmittance.ToString("0.00#", CultureInfo.InvariantCulture));

            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (existingNames != null)
            {
                foreach (string existingName in existingNames)
                {
                    if (existingName != null)
                    {
                        names.Add(existingName.Trim());
                    }
                }
            }

            string result = name;
            for (int index = 2; names.Contains(result); index++)
            {
                result = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", name, index);
            }

            return result;
        }
    }
}
