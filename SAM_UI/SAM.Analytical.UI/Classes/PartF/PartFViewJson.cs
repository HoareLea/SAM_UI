// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Small JSON readers for the Part F view-presentation types.
    /// <para>
    /// A near-twin of <c>PartFJson</c> in <c>SAM.Analytical</c>, and deliberately not that class. That one
    /// is an internal helper of the Part F model layer, and making it public so a user-interface assembly
    /// could borrow it would widen SAM core's surface for a convenience. The duplication is a dozen lines
    /// of null handling; the alternative is a permanent public API neither layer wanted.
    /// </para>
    /// </summary>
    internal static class PartFViewJson
    {
        public static bool Boolean(JsonObject jsonObject, string name, bool @default)
        {
            return jsonObject is not null && jsonObject.ContainsKey(name) && jsonObject[name] is not null
                ? jsonObject[name].GetValue<bool>()
                : @default;
        }

        public static double? NullableDouble(JsonObject jsonObject, string name)
        {
            return jsonObject is not null && jsonObject.ContainsKey(name) && jsonObject[name] is not null
                ? jsonObject[name].GetValue<double>()
                : null;
        }

        public static string String(JsonObject jsonObject, string name)
        {
            return jsonObject is not null && jsonObject.ContainsKey(name) && jsonObject[name] is not null
                ? jsonObject[name].GetValue<string>()
                : null;
        }

        public static Guid Guid(JsonObject jsonObject, string name)
        {
            string text = String(jsonObject, name);

            return System.Guid.TryParse(text, out Guid result) ? result : System.Guid.Empty;
        }
    }
}
