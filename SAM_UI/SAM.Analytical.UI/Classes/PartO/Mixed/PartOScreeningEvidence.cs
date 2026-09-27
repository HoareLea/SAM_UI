// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The per-dwelling outcome of ONE completed screening simulation: one strategy, applied homogeneously to the
    /// dwellings it assessed, from the clean baseline.
    ///
    /// <para><b>Evidence, never authority</b></para>
    /// <para>
    /// Screening says how a dwelling fared when every screened dwelling had the same strategy. The selected design
    /// is the designer's, and the final mixed run is the only result that can supersede it; nothing in this class
    /// is ever copied into either. A dwelling it did not assess has no entry here and reads as
    /// <see cref="PartODwellingOutcome.NotRun"/>.
    /// </para>
    ///
    /// <para><b>Bound to what it screened</b></para>
    /// <para>
    /// <see cref="Fingerprint_Design"/> is the baseline's model fingerprint with the selected strategy set left out
    /// (<c>Query.PartOScreeningDesignFingerprint</c>) - so selecting or editing strategies does not make screening
    /// stale, and any change to the building does. Where the run offered the product catalogue,
    /// <see cref="Fingerprint_Catalogue"/> is SAM's catalogue fingerprint over every selection-relevant field. Either
    /// moving makes the evidence stale; stale evidence is shown as stale, never as the result it used to be.
    /// </para>
    /// </summary>
    public class PartOScreeningEvidence
    {
        private readonly Dictionary<Guid, PartODwellingResult> results = [];

        public PartOScreeningEvidence(PartOScreeningStrategy partOScreeningStrategy)
        {
            Strategy = partOScreeningStrategy;
        }

        public PartOScreeningStrategy Strategy { get; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public string Fingerprint_Design { get; set; }

        /// <summary>Whether the screening offered the product catalogue. Null catalogue fingerprint when it did not.</summary>
        public bool CatalogueOffered { get; set; }

        public string Fingerprint_Catalogue { get; set; }

        /// <summary>The dwellings the run assessed. A dwelling outside it was not simulated under this strategy.</summary>
        public List<Guid> Guids_Zone_Assessed { get; } = [];

        public string Path_TSD { get; set; }

        public long Length_TSD { get; set; }

        public long Timestamp_TSD { get; set; }

        public int Count => results.Count;

        public List<PartODwellingResult> Results => [.. results.Values];

        public void Add(PartODwellingResult partODwellingResult)
        {
            if (partODwellingResult is not null && partODwellingResult.ZoneGuid != Guid.Empty)
            {
                results[partODwellingResult.ZoneGuid] = partODwellingResult;
            }
        }

        /// <summary>This dwelling's result, or null where this run did not assess it.</summary>
        public PartODwellingResult Result(Guid guid_Zone)
        {
            return results.TryGetValue(guid_Zone, out PartODwellingResult partODwellingResult) ? partODwellingResult : null;
        }

        /// <summary>
        /// This dwelling's cell: the run's outcome where it assessed the dwelling, <see cref="PartODwellingOutcome.NotRun"/>
        /// where it did not. Never a pass by default.
        /// </summary>
        public PartODwellingOutcome Outcome(Guid guid_Zone)
        {
            return Result(guid_Zone)?.Outcome ?? PartODwellingOutcome.NotRun;
        }

        /// <summary>
        /// Whether this evidence still describes the baseline and catalogue now in use - and if not, which half moved.
        /// </summary>
        /// <param name="fingerprint_Design">The baseline's current <c>Query.PartOScreeningDesignFingerprint</c>.</param>
        /// <param name="fingerprint_Catalogue">The current catalogue fingerprint, used only where this run offered one.</param>
        public bool IsCurrent(string fingerprint_Design, string fingerprint_Catalogue, out string reason)
        {
            reason = null;

            if (string.IsNullOrEmpty(Fingerprint_Design) || Fingerprint_Design != fingerprint_Design)
            {
                reason = "The building has changed since this screening was simulated, so it no longer describes the model. Screen again.";
                return false;
            }

            if (CatalogueOffered && (string.IsNullOrEmpty(Fingerprint_Catalogue) || Fingerprint_Catalogue != fingerprint_Catalogue))
            {
                reason = "The ventilation unit catalogue or the project's product settings have changed since this screening selected its products. Screen again.";
                return false;
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonArray zones = [];
            Guids_Zone_Assessed.ForEach(x => zones.Add(x.ToString()));

            JsonArray jsonArray_Results = [];
            foreach (PartODwellingResult partODwellingResult in results.Values)
            {
                jsonArray_Results.Add(partODwellingResult.ToJsonObject());
            }

            return new JsonObject
            {
                ["Strategy"] = Strategy.ToString(),
                ["CreatedUtc"] = CreatedUtc.ToString("o", CultureInfo.InvariantCulture),
                ["Fingerprint_Design"] = Fingerprint_Design,
                ["CatalogueOffered"] = CatalogueOffered,
                ["Fingerprint_Catalogue"] = Fingerprint_Catalogue,
                ["Guids_Zone_Assessed"] = zones,
                ["Path_TSD"] = Path_TSD,
                ["Length_TSD"] = Length_TSD,
                ["Timestamp_TSD"] = Timestamp_TSD,
                ["Results"] = jsonArray_Results,
            };
        }

        /// <summary>Reads one back, or null where it is not readable. An unknown strategy name is unreadable.</summary>
        public static PartOScreeningEvidence Read(JsonObject jsonObject)
        {
            if (jsonObject is null || !Enum.TryParse((string)jsonObject["Strategy"], false, out PartOScreeningStrategy partOScreeningStrategy) || !Enum.IsDefined(typeof(PartOScreeningStrategy), partOScreeningStrategy) || partOScreeningStrategy == PartOScreeningStrategy.Undefined)
            {
                return null;
            }

            PartOScreeningEvidence result = new(partOScreeningStrategy)
            {
                Fingerprint_Design = (string)jsonObject["Fingerprint_Design"],
                CatalogueOffered = (bool?)jsonObject["CatalogueOffered"] ?? false,
                Fingerprint_Catalogue = (string)jsonObject["Fingerprint_Catalogue"],
                Path_TSD = (string)jsonObject["Path_TSD"],
                Length_TSD = (long?)jsonObject["Length_TSD"] ?? 0,
                Timestamp_TSD = (long?)jsonObject["Timestamp_TSD"] ?? 0,
            };

            if (DateTime.TryParse((string)jsonObject["CreatedUtc"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dateTime))
            {
                result.CreatedUtc = dateTime;
            }

            if (jsonObject["Guids_Zone_Assessed"] is JsonArray zones)
            {
                foreach (JsonNode jsonNode in zones)
                {
                    if (Guid.TryParse((string)jsonNode, out Guid guid))
                    {
                        result.Guids_Zone_Assessed.Add(guid);
                    }
                }
            }

            if (jsonObject["Results"] is JsonArray jsonArray_Results)
            {
                foreach (JsonNode jsonNode in jsonArray_Results)
                {
                    result.Add(PartODwellingResult.Read(jsonNode as JsonObject));
                }
            }

            return result;
        }
    }
}
