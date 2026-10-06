// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The small, total readers the Iteration 3 pairing record is parsed with.
    ///
    /// <para><b>Total, deliberately</b></para>
    /// <para>
    /// A pairing record is read back in a later session from a file a person may have edited, truncated or
    /// produced with an older build. Every reader here answers a stated default for a key that is missing,
    /// null or of the wrong JSON type, and none of them throws. The record is then <b>validated</b> as a
    /// whole - <c>PartOIteration3Record.IsComplete</c> and the file fingerprints - rather than by
    /// exceptions escaping from halfway through a parse, which would lose the half that had been read.
    /// </para>
    /// <para>
    /// Written out, the same values round-trip exactly: guids as their canonical strings, integers as
    /// numbers, doubles as numbers, enums by name.
    /// </para>
    /// </summary>
    internal static class PartOIteration3Json
    {
        internal static string Text(JsonObject jsonObject, string name)
        {
            return jsonObject is not null && jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.TryGetValue(out string result)
                ? result
                : null;
        }

        internal static long Integer(JsonObject jsonObject, string name, long @default)
        {
            return jsonObject is not null && jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.TryGetValue(out long result)
                ? result
                : @default;
        }

        internal static int Count(JsonObject jsonObject, string name, int @default)
        {
            return (int)Integer(jsonObject, name, @default);
        }

        internal static double Number(JsonObject jsonObject, string name)
        {
            return jsonObject is not null && jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.TryGetValue(out double result)
                ? result
                : double.NaN;
        }

        internal static double? NullableNumber(JsonObject jsonObject, string name)
        {
            double result = Number(jsonObject, name);

            return double.IsNaN(result) ? (double?)null : result;
        }

        internal static bool Boolean(JsonObject jsonObject, string name, bool @default)
        {
            return jsonObject is not null && jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.TryGetValue(out bool result)
                ? result
                : @default;
        }

        internal static Guid Guid(JsonObject jsonObject, string name)
        {
            return System.Guid.TryParse(Text(jsonObject, name), out Guid result) ? result : System.Guid.Empty;
        }

        internal static TEnum Enum<TEnum>(JsonObject jsonObject, string name, TEnum @default) where TEnum : struct
        {
            return System.Enum.TryParse(Text(jsonObject, name), false, out TEnum result) ? result : @default;
        }

        internal static List<string> Texts(JsonObject jsonObject, string name)
        {
            List<string> result = [];

            if (jsonObject is null || !jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode) || jsonNode is not JsonArray jsonArray)
            {
                return result;
            }

            foreach (JsonNode jsonNode_Item in jsonArray)
            {
                if (jsonNode_Item is JsonValue jsonValue && jsonValue.TryGetValue(out string text))
                {
                    result.Add(text);
                }
            }

            return result;
        }

        internal static List<Guid> Guids(JsonObject jsonObject, string name)
        {
            List<Guid> result = [];

            foreach (string text in Texts(jsonObject, name))
            {
                if (System.Guid.TryParse(text, out Guid guid))
                {
                    result.Add(guid);
                }
            }

            return result;
        }

        internal static List<JsonObject> Objects(JsonObject jsonObject, string name)
        {
            List<JsonObject> result = [];

            if (jsonObject is null || !jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode) || jsonNode is not JsonArray jsonArray)
            {
                return result;
            }

            foreach (JsonNode jsonNode_Item in jsonArray)
            {
                if (jsonNode_Item is JsonObject jsonObject_Item)
                {
                    result.Add(jsonObject_Item);
                }
            }

            return result;
        }

        internal static JsonArray Array(IEnumerable<string> texts)
        {
            JsonArray result = [];

            foreach (string text in texts ?? [])
            {
                result.Add(JsonValue.Create(text));
            }

            return result;
        }

        internal static JsonArray Array(IEnumerable<Guid> guids)
        {
            JsonArray result = [];

            foreach (Guid guid in guids ?? [])
            {
                result.Add(JsonValue.Create(guid.ToString()));
            }

            return result;
        }
    }
}
