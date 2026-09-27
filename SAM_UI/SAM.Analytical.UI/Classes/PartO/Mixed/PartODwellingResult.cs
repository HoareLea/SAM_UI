// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One dwelling's outcome in one simulation - a screening run or the final mixed run.
    /// <para>
    /// <b>A tally, never a verdict of its own.</b> Each space's TM59 status is SAM's
    /// (<c>TM59AssessmentReportSpace.ComplianceStatus</c>); this only counts them per dwelling. See
    /// <c>Query.PartODwellingResults</c> for the rule, which is the TM59 result window's partial-assessment rule at
    /// dwelling scale: a failure is certain whatever else went unassessed, and a pass needs every space of the
    /// dwelling to have been assessed.
    /// </para>
    /// </summary>
    public class PartODwellingResult
    {
        /// <summary>How many failing space names are kept. The count is always exact; the list is only the first few.</summary>
        public const int FailingSpaceNamesKept = 12;

        public PartODwellingResult(Guid guid_Zone, PartODwellingOutcome partODwellingOutcome)
        {
            ZoneGuid = guid_Zone;
            Outcome = partODwellingOutcome;
        }

        public PartODwellingResult(PartODwellingResult partODwellingResult)
        {
            ZoneGuid = partODwellingResult.ZoneGuid;
            Outcome = partODwellingResult.Outcome;
            SpaceCount_Pass = partODwellingResult.SpaceCount_Pass;
            SpaceCount_Fail = partODwellingResult.SpaceCount_Fail;
            SpaceCount_NotAssessed = partODwellingResult.SpaceCount_NotAssessed;
            FailingSpaceNames.AddRange(partODwellingResult.FailingSpaceNames);
        }

        public Guid ZoneGuid { get; }

        public PartODwellingOutcome Outcome { get; }

        public int SpaceCount_Pass { get; set; }

        public int SpaceCount_Fail { get; set; }

        /// <summary>Spaces of the dwelling the assessment could not assess (no simulation space, or refused series).</summary>
        public int SpaceCount_NotAssessed { get; set; }

        /// <summary>The first <see cref="FailingSpaceNamesKept"/> failing spaces, by name, for the row's detail.</summary>
        public List<string> FailingSpaceNames { get; } = [];

        public JsonObject ToJsonObject()
        {
            JsonArray names = [];
            FailingSpaceNames.ForEach(x => names.Add(x));

            return new JsonObject
            {
                ["ZoneGuid"] = ZoneGuid.ToString(),
                ["Outcome"] = Outcome.ToString(),
                ["SpaceCount_Pass"] = SpaceCount_Pass,
                ["SpaceCount_Fail"] = SpaceCount_Fail,
                ["SpaceCount_NotAssessed"] = SpaceCount_NotAssessed,
                ["FailingSpaceNames"] = names,
            };
        }

        /// <summary>Reads one back, or null where it is not readable. An unknown outcome name is unreadable, never a pass.</summary>
        public static PartODwellingResult Read(JsonObject jsonObject)
        {
            if (jsonObject is null || !Guid.TryParse((string)jsonObject["ZoneGuid"], out Guid guid_Zone) || !Enum.TryParse((string)jsonObject["Outcome"], false, out PartODwellingOutcome partODwellingOutcome) || !Enum.IsDefined(typeof(PartODwellingOutcome), partODwellingOutcome))
            {
                return null;
            }

            PartODwellingResult result = new(guid_Zone, partODwellingOutcome)
            {
                SpaceCount_Pass = (int?)jsonObject["SpaceCount_Pass"] ?? 0,
                SpaceCount_Fail = (int?)jsonObject["SpaceCount_Fail"] ?? 0,
                SpaceCount_NotAssessed = (int?)jsonObject["SpaceCount_NotAssessed"] ?? 0,
            };

            if (jsonObject["FailingSpaceNames"] is JsonArray jsonArray)
            {
                foreach (JsonNode jsonNode in jsonArray)
                {
                    string name = (string)jsonNode;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        result.FailingSpaceNames.Add(name);
                    }
                }
            }

            return result;
        }
    }
}
