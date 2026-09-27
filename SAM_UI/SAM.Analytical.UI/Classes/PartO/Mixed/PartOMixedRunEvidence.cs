// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The outcome of the final mixed run: ONE annual TAS simulation of the model SAM materialised from the clean
    /// baseline and the selected strategies, assessed by the production TM59 assessment.
    ///
    /// <para><b>The current design authority - while it is current</b></para>
    /// <para>
    /// It supersedes every screening result for the dwellings it assessed. It is bound to what it was built from by
    /// SAM's own <see cref="PartOMaterialisationRecord"/> (baseline, strategy and catalogue fingerprints) and to its
    /// results file by length and write time. <see cref="IsCurrent"/> asks both and names what moved; a result that
    /// is not current is shown as the previous design's result, never as the current one's.
    /// </para>
    ///
    /// <para><b>What each dwelling actually ran as</b></para>
    /// <para>
    /// <see cref="Strategies"/> is the strategy set the materialisation read, in SAM's own serialisation - so a
    /// dwelling whose selection has since been edited still says what its result was simulated with, and nothing is
    /// reconstructed from text.
    /// </para>
    /// </summary>
    public class PartOMixedRunEvidence
    {
        private readonly Dictionary<Guid, PartODwellingResult> results = [];

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public PartOMaterialisationRecord Record { get; set; }

        /// <summary>The strategies the materialisation was built from.</summary>
        public PartODwellingStrategySet Strategies { get; set; }

        public bool CatalogueOffered { get; set; }

        public string Path_TSD { get; set; }

        public long Length_TSD { get; set; }

        public long Timestamp_TSD { get; set; }

        /// <summary>The run's own persisted model beside its results - the model a detailed review reopens.</summary>
        public string Path_RunModel { get; set; }

        /// <summary>Why the production assessment reached no verdict for the run as a whole, or null.</summary>
        public string Refusal_Assessment { get; set; }

        public List<PartODwellingResult> Results => [.. results.Values];

        public void Add(PartODwellingResult partODwellingResult)
        {
            if (partODwellingResult is not null && partODwellingResult.ZoneGuid != Guid.Empty)
            {
                results[partODwellingResult.ZoneGuid] = partODwellingResult;
            }
        }

        public PartODwellingResult Result(Guid guid_Zone)
        {
            return results.TryGetValue(guid_Zone, out PartODwellingResult partODwellingResult) ? partODwellingResult : null;
        }

        public int Count(PartODwellingOutcome partODwellingOutcome)
        {
            int result = 0;
            foreach (PartODwellingResult partODwellingResult in results.Values)
            {
                if (partODwellingResult.Outcome == partODwellingOutcome)
                {
                    result++;
                }
            }

            return result;
        }

        /// <summary>
        /// The run as a whole: FAIL where any dwelling failed, PASS only where every assessed dwelling passed, and
        /// NOT ASSESSED otherwise.
        /// </summary>
        public PartODwellingOutcome Overall
        {
            get
            {
                if (results.Count == 0)
                {
                    return PartODwellingOutcome.NotAssessed;
                }

                if (Count(PartODwellingOutcome.Fail) != 0)
                {
                    return PartODwellingOutcome.Fail;
                }

                return Count(PartODwellingOutcome.Pass) == results.Count && Refusal_Assessment is null ? PartODwellingOutcome.Pass : PartODwellingOutcome.NotAssessed;
            }
        }

        /// <summary>
        /// Whether this result still belongs to the baseline, strategies and catalogue in use, and to the results file
        /// it was assessed from. Fails closed, and names what moved.
        /// </summary>
        /// <param name="analyticalModel_Baseline">The open baseline, carrying the current selection.</param>
        /// <param name="ventilationUnitCapacityDescriptors">
        /// The catalogue as it would be offered now - null where the run did not offer one.
        /// </param>
        public bool IsCurrent(AnalyticalModel analyticalModel_Baseline, IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors, out string reason)
        {
            reason = null;

            if (Record is null)
            {
                reason = "The saved mixed result carries no materialisation record, so what it was built from cannot be proved.";
                return false;
            }

            //SAM's own staleness rule, asked rather than restated: strategies, then catalogue, then baseline.
            if (!Record.IsCurrent(analyticalModel_Baseline, CatalogueOffered ? ventilationUnitCapacityDescriptors : null, out reason))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Path_TSD) || !File.Exists(Path_TSD))
            {
                reason = string.Format("The mixed run's results file '{0}' is no longer there, so its TM59 result cannot be reviewed. Build and run the mixed design again.", Path_TSD ?? "-");
                return false;
            }

            FileInfo fileInfo = new(Path_TSD);
            if (fileInfo.Length != Length_TSD || fileInfo.LastWriteTimeUtc.Ticks != Timestamp_TSD)
            {
                reason = string.Format("The mixed run's results file '{0}' has been rewritten since it was assessed - by another run - so this result no longer describes it. Build and run the mixed design again.", Path_TSD);
                return false;
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonArray jsonArray_Results = [];
            foreach (PartODwellingResult partODwellingResult in results.Values)
            {
                jsonArray_Results.Add(partODwellingResult.ToJsonObject());
            }

            return new JsonObject
            {
                ["CreatedUtc"] = CreatedUtc.ToString("o", CultureInfo.InvariantCulture),
                ["Record"] = Record?.ToJsonObject(),
                ["Strategies"] = Strategies?.ToJsonObject(),
                ["CatalogueOffered"] = CatalogueOffered,
                ["Path_TSD"] = Path_TSD,
                ["Length_TSD"] = Length_TSD,
                ["Timestamp_TSD"] = Timestamp_TSD,
                ["Path_RunModel"] = Path_RunModel,
                ["Refusal_Assessment"] = Refusal_Assessment,
                ["Results"] = jsonArray_Results,
            };
        }

        public static PartOMixedRunEvidence Read(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return null;
            }

            PartOMixedRunEvidence result = new()
            {
                Record = jsonObject["Record"] is JsonObject jsonObject_Record ? new PartOMaterialisationRecord((JsonObject)jsonObject_Record.DeepClone()) : null,
                Strategies = jsonObject["Strategies"] is JsonObject jsonObject_Strategies ? new PartODwellingStrategySet((JsonObject)jsonObject_Strategies.DeepClone()) : null,
                CatalogueOffered = (bool?)jsonObject["CatalogueOffered"] ?? false,
                Path_TSD = (string)jsonObject["Path_TSD"],
                Length_TSD = (long?)jsonObject["Length_TSD"] ?? 0,
                Timestamp_TSD = (long?)jsonObject["Timestamp_TSD"] ?? 0,
                Path_RunModel = (string)jsonObject["Path_RunModel"],
                Refusal_Assessment = (string)jsonObject["Refusal_Assessment"],
            };

            if (DateTime.TryParse((string)jsonObject["CreatedUtc"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dateTime))
            {
                result.CreatedUtc = dateTime;
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
