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
    ///
    /// <para><b>The project verdict is SAM's</b></para>
    /// <para>
    /// <see cref="Overall"/> is the production <c>TM59AssessmentReport.OccupiedSpaceComplianceStatus</c> of the
    /// combined run (<see cref="OccupiedSpaceComplianceStatus"/>), which covers every occupied space the assessment
    /// judged - one in no dwelling row included. The dwelling rows are a tally beside it, never its source. The
    /// communal corridor is dwelling-independent state: SAM reports it as <see cref="CorridorRiskStatus"/>, beside the
    /// verdict and never folded into it (SAM's rule), and it is never a row.
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

        /// <summary>
        /// How the run was simulated - SAM's record's own answer (<see cref="PartOMaterialisationRecord.Route"/>): the
        /// TAS Systems route for the whole model as soon as one dwelling is cooled, the IZAM route otherwise.
        /// </summary>
        public Enums.PartOSimulationRoute Route => Record?.Route ?? Enums.PartOSimulationRoute.Undefined;

        /// <summary>The TAS Systems document a Systems-route run simulated, or null for an IZAM-route run. Traceability only.</summary>
        public string Path_TPD { get; set; }

        /// <summary>
        /// What each cooled unit did, read back from TAS by the Systems route (<c>GuidanceCoolingResult.Summary</c>), one
        /// line per cooled unit. Empty for an uncooled run.
        /// </summary>
        public List<string> GuidanceSummaries { get; } = [];

        /// <summary>Why the production assessment reached no verdict for the run as a whole, or null.</summary>
        public string Refusal_Assessment { get; set; }

        /// <summary>
        /// How many spaces the production assessment covered but could not assess (no simulation space, refused series) -
        /// common spaces included. A pass with a hole in it is not a pass, exactly as in the TM59 result window. Null where
        /// not known.
        /// </summary>
        public int? SpaceCount_Unassessed { get; set; }

        /// <summary>The simulation case (weather, solar method) the run was simulated under - <c>Query.PartOSimulationCaseKey</c>.</summary>
        public string SimulationCaseKey { get; set; }

        /// <summary>SAM's production verdict over the run's occupied spaces, or null where it is not known.</summary>
        public TM59ComplianceStatus? OccupiedSpaceComplianceStatus { get; set; }

        /// <summary>SAM's communal-corridor risk for the run; <c>Undefined</c> where no communal corridor was assessed.</summary>
        public TM59RiskStatus CorridorRiskStatus { get; set; } = TM59RiskStatus.Undefined;

        /// <summary>The assessed communal corridors, by space name, with each one's own risk status.</summary>
        public List<(string Name, TM59RiskStatus RiskStatus)> Corridors { get; } = [];

        /// <summary>
        /// Why the saved evidence cannot be trusted as read - an unreadable or missing dwelling result, an unknown status
        /// - or null. Such evidence is never current and never a pass; nothing in it is silently dropped.
        /// </summary>
        public string ReadRefusal { get; private set; }

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
        /// The run as a whole - SAM's production verdict: FAIL where it failed (or, certainly, where a dwelling did); PASS
        /// only where it passed AND every assessed dwelling passed with no space unassessed; NOT ASSESSED otherwise,
        /// including evidence that could not be read. The corridor risk is reported beside it, never in it.
        /// </summary>
        public PartODwellingOutcome Overall
        {
            get
            {
                if (ReadRefusal is not null || results.Count == 0)
                {
                    return PartODwellingOutcome.NotAssessed;
                }

                if (OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Fail || Count(PartODwellingOutcome.Fail) != 0)
                {
                    return PartODwellingOutcome.Fail;
                }

                return OccupiedSpaceComplianceStatus == TM59ComplianceStatus.Pass && SpaceCount_Unassessed == 0 && Count(PartODwellingOutcome.Pass) == results.Count && Refusal_Assessment is null ? PartODwellingOutcome.Pass : PartODwellingOutcome.NotAssessed;
            }
        }

        /// <summary>The communal-corridor state in a few words, or null where no communal corridor was assessed.</summary>
        public string CorridorText
        {
            get
            {
                if (CorridorRiskStatus == TM59RiskStatus.Undefined || Corridors.Count == 0)
                {
                    return null;
                }

                List<string> names = Corridors.FindAll(x => CorridorRiskStatus != TM59RiskStatus.SignificantRisk || x.RiskStatus == TM59RiskStatus.SignificantRisk).ConvertAll(x => x.Name);

                return string.Format("communal corridor: {0} ({1})", CorridorRiskStatus == TM59RiskStatus.SignificantRisk ? "significant risk" : "acceptable", string.Join(", ", names));
            }
        }

        /// <summary>
        /// Whether this result still belongs to the baseline, strategies and catalogue in use, and to the results file
        /// it was assessed from. Fails closed, and names what moved.
        /// </summary>
        /// <param name="analyticalModel_Baseline">The open baseline, carrying the current selection.</param>
        /// <param name="ventilationUnitCapacityDescriptors">
        /// The catalogue as the NEXT build would offer it - null where products are not selected from it now. So a
        /// catalogue setting changed since the run makes it stale, exactly as a changed catalogue does.
        /// </param>
        public bool IsCurrent(AnalyticalModel analyticalModel_Baseline, IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors, out string reason)
        {
            return IsCurrent(analyticalModel_Baseline, ventilationUnitCapacityDescriptors, null, out reason);
        }

        /// <summary>
        /// As above, and for a run with a cooled dwelling also whether each cooled product still carries the manufacturer
        /// guidance it was built with - SAM's record rule, handed the templates the next build would offer.
        /// </summary>
        public bool IsCurrent(AnalyticalModel analyticalModel_Baseline, IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors, IEnumerable<VentilationUnitTemplate> ventilationUnitTemplates, out string reason)
        {
            reason = null;

            if (ReadRefusal is not null)
            {
                reason = ReadRefusal;
                return false;
            }

            if (Record is null)
            {
                reason = "The saved mixed result carries no materialisation record, so what it was built from cannot be proved.";
                return false;
            }

            //SAM's own staleness rule, asked rather than restated: strategies, then catalogue, then baseline.
            if (!Record.IsCurrent(analyticalModel_Baseline, ventilationUnitCapacityDescriptors, ventilationUnitTemplates, out reason))
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

            JsonArray jsonArray_Corridors = [];
            foreach ((string name, TM59RiskStatus tM59RiskStatus) in Corridors)
            {
                jsonArray_Corridors.Add(new JsonObject { ["Name"] = name, ["RiskStatus"] = tM59RiskStatus.ToString() });
            }

            JsonArray jsonArray_Guidance = [];
            foreach (string summary in GuidanceSummaries)
            {
                jsonArray_Guidance.Add(summary);
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
                ["Path_TPD"] = Path_TPD,
                ["GuidanceSummaries"] = jsonArray_Guidance,
                ["Refusal_Assessment"] = Refusal_Assessment,
                ["OccupiedSpaceComplianceStatus"] = OccupiedSpaceComplianceStatus?.ToString(),
                ["SpaceCount_Unassessed"] = SpaceCount_Unassessed,
                ["SimulationCaseKey"] = SimulationCaseKey,
                ["CorridorRiskStatus"] = CorridorRiskStatus.ToString(),
                ["Corridors"] = jsonArray_Corridors,
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
                Path_TPD = (string)jsonObject["Path_TPD"],
                Refusal_Assessment = (string)jsonObject["Refusal_Assessment"],
                SpaceCount_Unassessed = (int?)jsonObject["SpaceCount_Unassessed"],
                SimulationCaseKey = (string)jsonObject["SimulationCaseKey"],
            };

            if (DateTime.TryParse((string)jsonObject["CreatedUtc"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dateTime))
            {
                result.CreatedUtc = dateTime;
            }

            if (jsonObject["GuidanceSummaries"] is JsonArray jsonArray_Guidance)
            {
                foreach (JsonNode jsonNode in jsonArray_Guidance)
                {
                    if (jsonNode?.GetValue<string>() is string summary)
                    {
                        result.GuidanceSummaries.Add(summary);
                    }
                }
            }

            //Fail closed: an entry that cannot be read is not dropped - it makes the whole evidence unreadable.
            int count_Unreadable = 0;
            int count_Read = 0;
            if (jsonObject["Results"] is JsonArray jsonArray_Results)
            {
                foreach (JsonNode jsonNode in jsonArray_Results)
                {
                    PartODwellingResult partODwellingResult = PartODwellingResult.Read(jsonNode as JsonObject);
                    if (partODwellingResult is null)
                    {
                        count_Unreadable++;
                        continue;
                    }

                    count_Read++;
                    result.Add(partODwellingResult);
                }
            }

            if (count_Read != result.results.Count)
            {
                result.ReadRefusal = "The saved mixed run lists a dwelling's result more than once, so no verdict is reported from it. Build and run the mixed design again.";
            }
            else if (count_Unreadable != 0)
            {
                result.ReadRefusal = string.Format("{0} of the saved mixed run could not be read, so no verdict is reported from it. Build and run the mixed design again.", count_Unreadable == 1 ? "One dwelling result" : count_Unreadable + " dwelling results");
            }
            else if (result.Record?.ZoneGuids_Assessed is ICollection<Guid> guids_Assessed && guids_Assessed.Count != 0 && !new HashSet<Guid>(guids_Assessed).SetEquals(result.results.Keys))
            {
                result.ReadRefusal = "The saved mixed run's dwelling results do not match the dwellings it assessed, so no verdict is reported from it. Build and run the mixed design again.";
            }

            string text_Status = (string)jsonObject["OccupiedSpaceComplianceStatus"];
            if (!string.IsNullOrWhiteSpace(text_Status))
            {
                if (Enum.TryParse(text_Status, false, out TM59ComplianceStatus tM59ComplianceStatus) && Enum.IsDefined(typeof(TM59ComplianceStatus), tM59ComplianceStatus))
                {
                    result.OccupiedSpaceComplianceStatus = tM59ComplianceStatus;
                }
                else
                {
                    result.ReadRefusal ??= "The saved mixed run's TM59 verdict could not be read, so no verdict is reported from it. Build and run the mixed design again.";
                }
            }
            else if (result.Refusal_Assessment is null)
            {
                result.ReadRefusal ??= "The saved mixed run records no project TM59 verdict (it was written by an earlier build), so no verdict is reported from it. Build and run the mixed design again.";
            }

            string text_Corridor = (string)jsonObject["CorridorRiskStatus"];
            if (!string.IsNullOrWhiteSpace(text_Corridor))
            {
                if (Enum.TryParse(text_Corridor, false, out TM59RiskStatus tM59RiskStatus) && Enum.IsDefined(typeof(TM59RiskStatus), tM59RiskStatus))
                {
                    result.CorridorRiskStatus = tM59RiskStatus;
                }
                else
                {
                    result.ReadRefusal ??= "The saved mixed run's communal-corridor status could not be read. Build and run the mixed design again.";
                }
            }

            if (jsonObject["Corridors"] is JsonArray jsonArray_Corridors)
            {
                foreach (JsonNode jsonNode in jsonArray_Corridors)
                {
                    string name = (string)jsonNode?["Name"];
                    if (string.IsNullOrWhiteSpace(name) || !Enum.TryParse((string)jsonNode?["RiskStatus"], false, out TM59RiskStatus tM59RiskStatus_Corridor) || !Enum.IsDefined(typeof(TM59RiskStatus), tM59RiskStatus_Corridor))
                    {
                        result.ReadRefusal ??= "A communal corridor of the saved mixed run could not be read. Build and run the mixed design again.";
                        continue;
                    }

                    result.Corridors.Add((name, tM59RiskStatus_Corridor));
                }
            }

            return result;
        }
    }
}
