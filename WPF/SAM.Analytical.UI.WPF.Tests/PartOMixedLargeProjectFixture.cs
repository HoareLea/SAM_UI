// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// A large project made from a real one (PR4): the clean baseline's block - its spaces, panels (with their apertures)
    /// and zones - repeated on a grid, each copy with its own guids and names ("Flat 1 #12", "Kitchen_4 #12"). The copies
    /// are spaced far apart so that no block shades or adjoins another: every block is the same building, which makes the
    /// original block's result the reference for all of them. Constructions, internal conditions and the heating and
    /// cooling systems are shared, as in a real project with a few types of each.
    /// </summary>
    internal static class PartOMixedLargeProjectFixture
    {
        internal const string Separator = " #";

        private static readonly Regex Regex_Guid = new("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.Compiled);

        /// <param name="copies">How many blocks in all, the original included.</param>
        internal static AnalyticalModel Replicate(AnalyticalModel analyticalModel, int copies, double pitch_X = 150, double pitch_Y = 100)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            //What each copy owns: its rooms, their envelope and openings, its dwellings and common zones.
            HashSet<string> guids_Owned = [];
            adjacencyCluster.GetSpaces()?.ForEach(x => guids_Owned.Add(x.Guid.ToString()));
            adjacencyCluster.GetZones()?.ForEach(x => guids_Owned.Add(x.Guid.ToString()));
            foreach (Panel panel in adjacencyCluster.GetPanels() ?? [])
            {
                guids_Owned.Add(panel.Guid.ToString());
                panel.Apertures?.ForEach(x => guids_Owned.Add(x.Guid.ToString()));
            }

            string text = adjacencyCluster.ToJsonObject().ToJsonString();
            JsonObject jsonObject_Result = JsonNode.Parse(text)!.AsObject();
            Merger merger = new(jsonObject_Result);

            int columns = (int)Math.Ceiling(Math.Sqrt(copies));
            for (int copy = 1; copy < copies; copy++)
            {
                Dictionary<string, string> map = [];
                string text_Copy = Regex_Guid.Replace(text, match =>
                {
                    if (!guids_Owned.Contains(match.Value))
                    {
                        return match.Value;
                    }

                    if (!map.TryGetValue(match.Value, out string? guid))
                    {
                        guid = Guid.NewGuid().ToString();
                        map[match.Value] = guid;
                    }

                    return guid;
                });

                JsonObject jsonObject_Copy = JsonNode.Parse(text_Copy)!.AsObject();
                Rename(jsonObject_Copy, Separator + copy);

                AdjacencyCluster adjacencyCluster_Copy = new(jsonObject_Copy);
                adjacencyCluster_Copy.Transform(Transform3D.GetTranslation((copy % columns) * pitch_X, (copy / columns) * pitch_Y, 0));

                merger.Merge(adjacencyCluster_Copy.ToJsonObject());
            }

            return new AnalyticalModel(analyticalModel, new AdjacencyCluster(jsonObject_Result));
        }

        /// <summary>The block a replicated object belongs to: 0 for the original, else the number after the separator.</summary>
        internal static int Block(string? name)
        {
            int index = name?.LastIndexOf(Separator, StringComparison.Ordinal) ?? -1;
            return index >= 0 && int.TryParse(name![(index + Separator.Length)..], out int block) ? block : 0;
        }

        /// <summary>The name without its copy suffix: the room or dwelling of the original block.</summary>
        internal static string? Original(string? name)
        {
            int index = name?.LastIndexOf(Separator, StringComparison.Ordinal) ?? -1;
            return index >= 0 ? name![..index] : name;
        }

        private static void Rename(JsonObject jsonObject_AdjacencyCluster, string suffix)
        {
            foreach (JsonNode? jsonNode in jsonObject_AdjacencyCluster["Objects"]!.AsArray())
            {
                string? type = jsonNode?["Key"]?.GetValue<string>();
                if (type != "SAM.Analytical.Space" && type != "SAM.Analytical.Zone")
                {
                    continue;
                }

                foreach (JsonNode? jsonNode_Object in jsonNode!["Value"]!.AsArray())
                {
                    JsonObject jsonObject = jsonNode_Object!["Value"]!.AsObject();
                    string name = jsonObject["Name"]!.GetValue<string>();
                    jsonObject["Name"] = name + suffix;

                    //The space's own Part F data names it too.
                    if (type == "SAM.Analytical.Space")
                    {
                        RenameSpaceName(jsonObject, name, name + suffix);
                    }
                }
            }
        }

        private static void RenameSpaceName(JsonNode? jsonNode, string name, string name_New)
        {
            switch (jsonNode)
            {
                case JsonObject jsonObject:
                    foreach (KeyValuePair<string, JsonNode?> keyValuePair in jsonObject.ToList())
                    {
                        if (keyValuePair.Key == "SpaceName" && keyValuePair.Value is JsonValue jsonValue && jsonValue.TryGetValue(out string? value) && value == name)
                        {
                            jsonObject[keyValuePair.Key] = name_New;
                        }
                        else
                        {
                            RenameSpaceName(keyValuePair.Value, name, name_New);
                        }
                    }
                    break;

                case JsonArray jsonArray:
                    foreach (JsonNode? jsonNode_Item in jsonArray)
                    {
                        RenameSpaceName(jsonNode_Item, name, name_New);
                    }
                    break;
            }
        }

        /// <summary>Adds a cluster's objects and relations to another's JSON: new objects appended, shared ones' relations united.</summary>
        private sealed class Merger
        {
            private readonly Dictionary<string, (JsonArray Array, HashSet<string> Guids)> objects = [];

            private readonly Dictionary<string, (JsonArray Array, Dictionary<string, JsonArray> Related)> relations = [];

            private readonly JsonObject jsonObject;

            internal Merger(JsonObject jsonObject)
            {
                this.jsonObject = jsonObject;

                foreach (JsonNode? jsonNode in jsonObject["Objects"]!.AsArray())
                {
                    JsonArray jsonArray = jsonNode!["Value"]!.AsArray();
                    objects[jsonNode["Key"]!.GetValue<string>()] = (jsonArray, [.. jsonArray.Select(x => x!["Key"]!.GetValue<string>())]);
                }

                foreach (JsonNode? jsonNode in jsonObject["Relations"]!.AsArray())
                {
                    JsonArray jsonArray = jsonNode!["Value"]!.AsArray();
                    relations[jsonNode["Key"]!.GetValue<string>()] = (jsonArray, jsonArray.ToDictionary(x => x!["Key"]!.GetValue<string>(), x => x!["Value"]!.AsArray()));
                }
            }

            internal void Merge(JsonObject jsonObject_Copy)
            {
                foreach (JsonNode? jsonNode in jsonObject_Copy["Objects"]!.AsArray())
                {
                    string type = jsonNode!["Key"]!.GetValue<string>();
                    if (!objects.TryGetValue(type, out (JsonArray Array, HashSet<string> Guids) entry))
                    {
                        entry = (new JsonArray(), []);
                        jsonObject["Objects"]!.AsArray().Add(new JsonObject { ["Key"] = type, ["Value"] = entry.Array });
                        objects[type] = entry;
                    }

                    foreach (JsonNode? jsonNode_Object in jsonNode["Value"]!.AsArray())
                    {
                        if (entry.Guids.Add(jsonNode_Object!["Key"]!.GetValue<string>()))
                        {
                            entry.Array.Add(jsonNode_Object.DeepClone());
                        }
                    }
                }

                foreach (JsonNode? jsonNode in jsonObject_Copy["Relations"]!.AsArray())
                {
                    string type = jsonNode!["Key"]!.GetValue<string>();
                    if (!relations.TryGetValue(type, out (JsonArray Array, Dictionary<string, JsonArray> Related) entry))
                    {
                        entry = (new JsonArray(), []);
                        jsonObject["Relations"]!.AsArray().Add(new JsonObject { ["Key"] = type, ["Value"] = entry.Array });
                        relations[type] = entry;
                    }

                    foreach (JsonNode? jsonNode_Relation in jsonNode["Value"]!.AsArray())
                    {
                        string guid = jsonNode_Relation!["Key"]!.GetValue<string>();
                        List<string> related = [.. jsonNode_Relation["Value"]!.AsArray().Select(x => x!.GetValue<string>())];

                        if (!entry.Related.TryGetValue(guid, out JsonArray? jsonArray))
                        {
                            jsonArray = [];
                            entry.Array.Add(new JsonObject { ["Key"] = guid, ["Value"] = jsonArray });
                            entry.Related[guid] = jsonArray;
                        }

                        HashSet<string> existing = [.. jsonArray.Select(x => x!.GetValue<string>())];
                        foreach (string guid_Related in related)
                        {
                            if (existing.Add(guid_Related))
                            {
                                jsonArray.Add(guid_Related);
                            }
                        }
                    }
                }
            }
        }
    }
}
