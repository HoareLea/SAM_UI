// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Every line the Iteration 3 window puts on screen, composed from one
    /// <see cref="PartOIteration3Result"/> and from nothing else - <b>the one authority for that text</b>.
    ///
    /// <para><b>Why it is here and not in the window</b></para>
    /// <para>
    /// The window used to compose its own outcome, summary, refusal, comparison and notes, and
    /// <c>Copy All</c> then read them back off the rendered controls. Persisting the same review to a file
    /// could then either read a window that may not exist, or compose the text a second time - and a
    /// second composition is a second answer waiting to disagree with the one an engineer read on screen.
    /// So the composition lives here, the window renders what this returns, and the persisted report is
    /// assembled from the same calls.
    /// </para>
    ///
    /// <para><b>It states, and computes nothing</b></para>
    /// <para>
    /// Every number is copied from the comparison, the statistics, the ledger or the pairing record. No
    /// statistic is recomputed for persistence, and nothing appears here that the review did not already
    /// produce.
    /// </para>
    ///
    /// <para><b>Deterministic where it matters</b></para>
    /// <para>
    /// The culture is the caller's: the window passes the current culture, so a person reads numbers
    /// formatted for them, and the persisted file passes the invariant culture, so re-saving an unchanged
    /// completed pairing produces the same bytes on any machine. Rows are written in the order they are
    /// handed over, which the comparison has already ordered by identity.
    /// </para>
    /// </summary>
    public static class PartOIteration3ReportText
    {
        /// <summary>The schema the persisted report states, in both its text header and its JSON.</summary>
        public const string CurrentSchema = "PartOIteration3Report:v2";

        /// <summary>What an empty provenance table says, so a blank section is never read as a lost one.</summary>
        private const string None = "\tNONE RECORDED";

        /// <summary>The headline: which pairing, and whether it completed or refused.</summary>
        public static string Outcome(PartOIteration3Result partOIteration3Result)
        {
            if (partOIteration3Result is null)
            {
                return string.Empty;
            }

            return partOIteration3Result.IsComplete
                ? string.Format(
                    "Iteration 3 {0} COMPLETE. Reference A {1}; Candidate B {2}.",
                    partOIteration3Result.IsRestored ? "review" : "run",
                    Verdict(partOIteration3Result.Assessment_ReferenceA),
                    Verdict(partOIteration3Result.Assessment_CandidateB))
                : string.Format(
                    "Iteration 3 {0} REFUSED at {1}. No Candidate B result is presented.",
                    partOIteration3Result.IsRestored ? "review" : "run",
                    partOIteration3Result.Ledger.Stage_Refused.HasValue
                        ? Core.Query.Description(partOIteration3Result.Ledger.Stage_Refused.Value)
                        : "an unrecorded stage");
        }

        /// <summary>Where the pairing came from, and the shape of the graph it was produced over.</summary>
        public static string Summary(PartOIteration3Result partOIteration3Result)
        {
            if (partOIteration3Result is null)
            {
                return string.Empty;
            }

            PartOIteration3Record partOIteration3Record = partOIteration3Result.Record;

            StringBuilder stringBuilder = new();

            stringBuilder.Append(partOIteration3Result.IsRestored
                ? "Reopened from the persisted pairing record. No TAS simulation was run and no TAS file was written."
                : "Produced in this session.");

            if (partOIteration3Record is not null)
            {
                stringBuilder.Append(string.Format(
                    " Reference A '{0}' against Candidate B '{1}'. Case: {2}.",
                    partOIteration3Record.ProjectName_ReferenceA ?? "?",
                    partOIteration3Record.ProjectName_CandidateB ?? "?",
                    partOIteration3Record.Fingerprint_Scenario ?? "not recorded"));

                stringBuilder.Append(string.Format(
                    " Ventilation equipment behaviour: {0}.",
                    Core.Query.Description(partOIteration3Record.BehaviourMode)));

                if (partOIteration3Record.Equipment.Count != 0)
                {
                    stringBuilder.Append(string.Format(" {0} selected product(s) resolved and recorded.", partOIteration3Record.Equipment.Count));
                }

                if (partOIteration3Record.Cooling.Count != 0)
                {
                    stringBuilder.Append(string.Format(" {0} cooling module(s) resolved, materialised as internal recirculation and evidenced hour by hour.", partOIteration3Record.Cooling.Count));
                }

                if (partOIteration3Record.Count_AirSystem != 0)
                {
                    stringBuilder.Append(string.Format(
                        " {0} physical air system(s), {1} bound room(s), {2} directed leg(s) ({3} supply, {4} extract, {5} transfer).",
                        partOIteration3Record.Count_AirSystem,
                        partOIteration3Record.Bindings.Count,
                        partOIteration3Record.Count_Connection_Supply + partOIteration3Record.Count_Connection_Extract + partOIteration3Record.Count_Connection_Transfer,
                        partOIteration3Record.Count_Connection_Supply,
                        partOIteration3Record.Count_Connection_Extract,
                        partOIteration3Record.Count_Connection_Transfer));
                }

                if (!string.IsNullOrWhiteSpace(partOIteration3Record.Method_ResultantTemperature))
                {
                    stringBuilder.Append(string.Format(" Resultant temperature obtained by: {0}.", partOIteration3Record.Method_ResultantTemperature));
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// The refusal, in the order a person needs it: what refused, why in the authority's own words,
        /// what this attempt genuinely produced, and what never ran.
        /// </summary>
        public static string Refusal(PartOIteration3Result partOIteration3Result)
        {
            if (partOIteration3Result is null)
            {
                return string.Empty;
            }

            StringBuilder stringBuilder = new();

            PartOIteration3Ledger partOIteration3Ledger = partOIteration3Result.Ledger;

            if (partOIteration3Ledger.Stage_Refused.HasValue)
            {
                PartOIteration3StageState partOIteration3StageState = partOIteration3Ledger.State(partOIteration3Ledger.Stage_Refused.Value);

                stringBuilder.AppendLine(string.Format("REFUSED at {0}: {1}", partOIteration3StageState.Name, partOIteration3StageState.Detail));

                foreach (string reason in partOIteration3StageState.Reasons)
                {
                    stringBuilder.AppendLine(string.Format("  - {0}", reason));
                }
            }

            List<string> artifacts = partOIteration3Ledger.Artifacts;

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(artifacts.Count == 0
                ? "This attempt produced no file. Any Candidate B file in the output folder was left by an earlier attempt and is not evidence of this one."
                : "Files this attempt created or updated:");

            foreach (string artifact in artifacts)
            {
                stringBuilder.AppendLine(string.Format("  - {0}", artifact));
            }

            List<string> notRun = [];
            foreach (PartOIteration3StageState partOIteration3StageState in partOIteration3Ledger.Stages)
            {
                if (partOIteration3StageState.Status == PartOIteration3StageStatus.NotRun)
                {
                    notRun.Add(partOIteration3StageState.Name);
                }
            }

            if (notRun.Count != 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(string.Format("Not run: {0}.", string.Join(", ", notRun)));
            }

            return stringBuilder.ToString().TrimEnd();
        }

        /// <summary>The pooled A/B statistics line. Empty where the pairing produced no comparison.</summary>
        public static string Comparison(PartOIteration3Result partOIteration3Result, CultureInfo cultureInfo)
        {
            if (partOIteration3Result?.Comparison is null)
            {
                return string.Empty;
            }

            PartOIteration3Statistics partOIteration3Statistics = partOIteration3Result.Comparison.Statistics;

            return string.Format(
                cultureInfo ?? CultureInfo.InvariantCulture,
                "{0} room(s), {1} hourly value(s) each side. Mean A {2:0.###} °C, mean B {3:0.###} °C, mean bias B−A {4:0.###} K, RMSE {5:0.###} K, maximum |B−A| {6:0.###} K in '{7}' at hour {8}. {9} TM59 criterion outcome(s) differ.",
                partOIteration3Statistics.Count_Rooms,
                partOIteration3Statistics.Count_Values,
                partOIteration3Statistics.Mean_A,
                partOIteration3Statistics.Mean_B,
                partOIteration3Statistics.MeanBias,
                partOIteration3Statistics.RootMeanSquareError,
                partOIteration3Statistics.MaximumAbsoluteDifference,
                partOIteration3Statistics.Name_Space_MaximumAbsoluteDifference ?? "-",
                partOIteration3Statistics.Hour_MaximumAbsoluteDifference,
                partOIteration3Result.Comparison.Count_Changed);
        }

        /// <summary>The per-dwelling statistics line. Empty where the pairing produced no comparison.</summary>
        public static string Dwellings(PartOIteration3Result partOIteration3Result, CultureInfo cultureInfo)
        {
            if (partOIteration3Result?.Comparison is null)
            {
                return string.Empty;
            }

            StringBuilder stringBuilder = new();

            foreach (PartOIteration3DwellingStatistics partOIteration3DwellingStatistics in partOIteration3Result.Comparison.Dwellings)
            {
                if (stringBuilder.Length != 0)
                {
                    stringBuilder.Append("   |   ");
                }

                stringBuilder.Append(string.Format(
                    cultureInfo ?? CultureInfo.InvariantCulture,
                    "{0}: bias {1:0.###} K, RMSE {2:0.###} K, max {3:0.###} K",
                    partOIteration3DwellingStatistics.Name_Dwelling ?? "-",
                    partOIteration3DwellingStatistics.Statistics.MeanBias,
                    partOIteration3DwellingStatistics.Statistics.RootMeanSquareError,
                    partOIteration3DwellingStatistics.Statistics.MaximumAbsoluteDifference));
            }

            return stringBuilder.ToString();
        }

        /// <summary>The notes: what the run or review recorded, and where this pairing's files are.</summary>
        public static string Diagnostics(PartOIteration3Result partOIteration3Result)
        {
            if (partOIteration3Result is null)
            {
                return "Nothing was recorded.";
            }

            StringBuilder stringBuilder = new();

            foreach (string note in partOIteration3Result.Notes)
            {
                stringBuilder.AppendLine(note);
            }

            foreach (string note in partOIteration3Result.Record?.Notes_Scope ?? [])
            {
                stringBuilder.AppendLine(note);
            }

            if (partOIteration3Result.Record is not null)
            {
                stringBuilder.AppendLine(string.Format(
                    "Ventilation equipment behaviour: {0}",
                    Core.Query.Description(partOIteration3Result.Record.BehaviourMode)));

                foreach (PartOIteration3EquipmentEvidence partOIteration3EquipmentEvidence in partOIteration3Result.Record.Equipment)
                {
                    stringBuilder.AppendLine(string.Format("Equipment: {0}", partOIteration3EquipmentEvidence));
                    stringBuilder.AppendLine(string.Format("  Fan-power mapping: {0}", partOIteration3EquipmentEvidence.FanPowerSplitRule));
                    stringBuilder.AppendLine(string.Format("  Fan-heat assumption: {0}", partOIteration3EquipmentEvidence.FanHeatGainAssumption));
                }

                foreach (PartOIteration3CoolingEvidence partOIteration3CoolingEvidence in partOIteration3Result.Record.Cooling)
                {
                    stringBuilder.AppendLine(string.Format("Cooling: {0}", partOIteration3CoolingEvidence));
                    stringBuilder.AppendLine(string.Format("  Table: {0}", partOIteration3CoolingEvidence.Table));
                    stringBuilder.AppendLine(string.Format("  Declared rules: {0}", partOIteration3CoolingEvidence.DeclaredRules));
                }
            }

            if (!string.IsNullOrWhiteSpace(partOIteration3Result.Path_Record))
            {
                stringBuilder.AppendLine(string.Format("Pairing record: {0}", partOIteration3Result.Path_Record));
            }

            if (!string.IsNullOrWhiteSpace(partOIteration3Result.Path_Report))
            {
                stringBuilder.AppendLine(string.Format("Review report: {0}", partOIteration3Result.Path_Report));
            }

            if (!string.IsNullOrWhiteSpace(partOIteration3Result.Path_Report_Json))
            {
                stringBuilder.AppendLine(string.Format("Review report (structured): {0}", partOIteration3Result.Path_Report_Json));
            }

            if (!string.IsNullOrWhiteSpace(partOIteration3Result.Refusal_Report))
            {
                stringBuilder.AppendLine(partOIteration3Result.Refusal_Report);
            }

            return stringBuilder.Length == 0 ? "Nothing was recorded." : stringBuilder.ToString();
        }

        /// <summary>
        /// The whole review as tab-separated text - what <c>Copy All</c> writes and what is persisted
        /// beside the pairing record.
        /// <para>
        /// <paramref name="rows"/> is the row set to tabulate. The window hands in the rows it is showing,
        /// so a copy matches the screen; persistence hands in every row, so the saved report is not
        /// whatever filter someone happened to be holding.
        /// </para>
        /// <para>
        /// <paramref name="provenance"/> adds the pairing's identity - which Reference A, which Candidate
        /// B, which design state, which rooms, which files. The window does not show all of it, so it is
        /// off there and on for the persisted report, which has to be readable in a year's time without
        /// the model in front of you.
        /// </para>
        /// </summary>
        public static string Text(
            PartOIteration3Result partOIteration3Result,
            IEnumerable<PartOIteration3Row> rows,
            CultureInfo cultureInfo = null,
            bool provenance = false)
        {
            if (partOIteration3Result is null)
            {
                return string.Empty;
            }

            cultureInfo ??= CultureInfo.InvariantCulture;

            StringBuilder stringBuilder = new();

            stringBuilder.AppendLine(Outcome(partOIteration3Result));
            stringBuilder.AppendLine(Summary(partOIteration3Result));

            if (!partOIteration3Result.IsComplete)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(Refusal(partOIteration3Result));
            }

            if (provenance)
            {
                Provenance(stringBuilder, partOIteration3Result, cultureInfo);
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Stage\tStatus\tDetail");

            foreach (PartOIteration3StageState partOIteration3StageState in partOIteration3Result.Ledger.Stages)
            {
                stringBuilder.AppendLine(string.Format("{0}\t{1}\t{2}", partOIteration3StageState.Name, partOIteration3StageState.StatusText, partOIteration3StageState.Detail));

                foreach (string reason in partOIteration3StageState.Reasons)
                {
                    stringBuilder.AppendLine(string.Format("\tREFUSED\t{0}", reason));
                }

                foreach (string artifact in partOIteration3StageState.Artifacts)
                {
                    stringBuilder.AppendLine(string.Format("\tARTIFACT\t{0}", artifact));
                }
            }

            if (partOIteration3Result.IsComplete)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(Comparison(partOIteration3Result, cultureInfo));
                stringBuilder.AppendLine(Dwellings(partOIteration3Result, cultureInfo));

                stringBuilder.AppendLine();
                stringBuilder.AppendLine("Dwelling\tSpace\tTM59 criterion\tMechanical\tA actual\tA limit\tA status\tB actual\tB limit\tB status\tDelta actual\tHours\tMean A\tMean B\tBias B-A\tRMSE\tMax |B-A|\tat hour");

                foreach (PartOIteration3Row partOIteration3Row in rows ?? [])
                {
                    stringBuilder.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:0.###}\t{13:0.###}\t{14:0.###}\t{15:0.###}\t{16:0.###}\t{17}",
                        partOIteration3Row.Dwelling,
                        partOIteration3Row.Space,
                        partOIteration3Row.Criterion,
                        partOIteration3Row.Mechanical,
                        Number(partOIteration3Row.Actual_A),
                        Number(partOIteration3Row.Limit_A),
                        partOIteration3Row.Status_A,
                        Number(partOIteration3Row.Actual_B),
                        Number(partOIteration3Row.Limit_B),
                        partOIteration3Row.Status_B,
                        Number(partOIteration3Row.Delta_Actual),
                        partOIteration3Row.Count,
                        partOIteration3Row.Mean_A,
                        partOIteration3Row.Mean_B,
                        partOIteration3Row.MeanBias,
                        partOIteration3Row.RootMeanSquareError,
                        partOIteration3Row.MaximumAbsoluteDifference,
                        partOIteration3Row.Hour_MaximumAbsoluteDifference));
                }
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Diagnostics(partOIteration3Result));

            return stringBuilder.ToString();
        }

        /// <summary>
        /// The identity of the pairing this report belongs to, copied from its record: which results,
        /// which design state, which scope, which rooms, which files.
        /// <para>
        /// A persisted report is read away from the session that produced it, so "which A/B pairing is
        /// this, and of which design state?" has to be answerable from the file alone.
        /// </para>
        /// </summary>
        private static void Provenance(StringBuilder stringBuilder, PartOIteration3Result partOIteration3Result, CultureInfo cultureInfo)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("PAIRING PROVENANCE");

            stringBuilder.AppendLine(string.Format("Report schema\t{0}", CurrentSchema));
            stringBuilder.AppendLine(string.Format("Presented as\t{0}", partOIteration3Result.IsRestored ? "a review of a persisted pairing" : "a run produced in session"));
            stringBuilder.AppendLine(string.Format("Pairing record\t{0}", partOIteration3Result.Path_Record ?? "<none>"));

            PartOIteration3Record partOIteration3Record = partOIteration3Result.Record;

            if (partOIteration3Record is null)
            {
                stringBuilder.AppendLine("No pairing record was assembled, so this report names no pairing identity.");

                return;
            }

            stringBuilder.AppendLine(string.Format("Record schema\t{0}", partOIteration3Record.Schema ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Attempt\t{0}", partOIteration3Record.Guid_Run));
            stringBuilder.AppendLine(string.Format("Written (UTC)\t{0}", Utc(partOIteration3Record.Ticks_Utc)));

            stringBuilder.AppendLine(string.Format("Reference A project\t{0}", partOIteration3Record.ProjectName_ReferenceA ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Reference A results\t{0}", partOIteration3Record.Path_TSD_ReferenceA ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Reference A model\t{0}", partOIteration3Record.Path_Model_ReferenceA ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Reference A design fingerprint\t{0}", partOIteration3Record.Fingerprint_Model_ReferenceA ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Reference A scenario fingerprint\t{0}", partOIteration3Record.Fingerprint_Scenarios_ReferenceA ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Reference A TM59\t{0}", Core.Query.Description(partOIteration3Record.Status_ReferenceA)));

            stringBuilder.AppendLine(string.Format("Candidate B project\t{0}", partOIteration3Record.ProjectName_CandidateB ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Ventilation equipment behaviour\t{0}", Core.Query.Description(partOIteration3Record.BehaviourMode)));
            stringBuilder.AppendLine(string.Format("Candidate B TM59\t{0}", Core.Query.Description(partOIteration3Record.Status_CandidateB)));
            stringBuilder.AppendLine(string.Format("Resultant temperature\t{0}", partOIteration3Record.Method_ResultantTemperature ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Provider matches its result file\t{0}", partOIteration3Record.ProviderMatchesResultFile));
            stringBuilder.AppendLine(string.Format("Provider identity values\t{0}", partOIteration3Record.Count_ProviderIdentityValues.ToString(cultureInfo)));

            stringBuilder.AppendLine(string.Format("Thermal case (weather, solar, period, options)\t{0}", partOIteration3Record.Fingerprint_Scenario ?? "<none>"));
            stringBuilder.AppendLine(string.Format("Weather used (QA)\t{0}; {1}; latitude {2}; longitude {3}; elevation {4} m",
                partOIteration3Record.WeatherName ?? "UNAVAILABLE",
                partOIteration3Record.WeatherDescription ?? "UNAVAILABLE",
                Number(partOIteration3Record.WeatherLatitude, cultureInfo),
                Number(partOIteration3Record.WeatherLongitude, cultureInfo),
                Number(partOIteration3Record.WeatherElevation, cultureInfo)));
            stringBuilder.AppendLine(string.Format("Weather dry-bulb peak (QA)\t{0} C at zero-based hour {1}",
                partOIteration3Record.WeatherPeakDryBulb_C.HasValue ? Number(partOIteration3Record.WeatherPeakDryBulb_C, cultureInfo) : "UNAVAILABLE",
                partOIteration3Record.WeatherPeakHour?.ToString(cultureInfo) ?? "UNAVAILABLE"));

            foreach (PartOIteration3GuidanceEvidence guidance in partOIteration3Record.Guidance)
            {
                stringBuilder.AppendLine(string.Format("Cooling control\t{0}: stat room {1}; activation setpoint {2} C",
                    guidance.Name_AirHandlingUnit,
                    guidance.Guid_CoolingStatSpace == Guid.Empty ? "UNAVAILABLE" : guidance.Guid_CoolingStatSpace.ToString(),
                    Number(guidance.CoolingActivationTemperature_C, cultureInfo)));
            }
            PartOIteration3FileRecord operation = partOIteration3Record.File(PartOIteration3Roles.GuidanceOperation);
            stringBuilder.AppendLine(string.Format("Operating read-back\t{0}", operation?.Path ?? "UNAVAILABLE"));
            stringBuilder.AppendLine(operation is null
                ? "Operating read-back series: UNAVAILABLE."
                : "Operating read-back series: stat-room, intake, extract, exchanger leaving, supply temperature, supply/extract airflow, DX duty; NORMAL/COOLING and BYPASS/RECOVERY are UNAVAILABLE where hourly evidence is insufficient.");

            stringBuilder.AppendLine(string.Format("No-IZAM source: IZAMs removed\t{0}", partOIteration3Record.RemovedIZAMs));
            stringBuilder.AppendLine(string.Format("No-IZAM source: mechanical ventilation gains removed\t{0}", partOIteration3Record.RemovedMechanicalVentilationGains));

            stringBuilder.AppendLine(string.Format("Ventilation catalogue directory\t{0}", partOIteration3Record.Directory_VentilationUnitCatalogue ?? "<none - Parity mode>"));
            stringBuilder.AppendLine(string.Format("Ventilation catalogue file\t{0}", partOIteration3Record.Path_VentilationUnitCatalogue ?? "<none - Parity mode>"));
            stringBuilder.AppendLine(string.Format("Ventilation catalogue schema\t{0}", partOIteration3Record.Schema_VentilationUnitCatalogue ?? "<none - Parity mode>"));
            stringBuilder.AppendLine(string.Format("Ventilation catalogue SHA-256\t{0}", partOIteration3Record.Sha256_VentilationUnitCatalogue ?? "<none - Parity mode>"));

            stringBuilder.AppendLine(string.Format("Air systems\t{0}", partOIteration3Record.Count_AirSystem));
            stringBuilder.AppendLine(string.Format(
                "Directed legs\t{0} supply, {1} extract, {2} transfer",
                partOIteration3Record.Count_Connection_Supply,
                partOIteration3Record.Count_Connection_Extract,
                partOIteration3Record.Count_Connection_Transfer));

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Systems scope");

            if (partOIteration3Record.Guids_VentilationSystem_Prepared.Count == 0
                && partOIteration3Record.Guids_VentilationSystem_ScopedOut.Count == 0
                && partOIteration3Record.Notes_Scope.Count == 0)
            {
                stringBuilder.AppendLine(None);
            }

            foreach (Guid guid in partOIteration3Record.Guids_VentilationSystem_Prepared)
            {
                stringBuilder.AppendLine(string.Format("\tPREPARED\t{0}", guid));
            }

            foreach (Guid guid in partOIteration3Record.Guids_VentilationSystem_ScopedOut)
            {
                stringBuilder.AppendLine(string.Format("\tSCOPED OUT\t{0}", guid));
            }

            foreach (string note in partOIteration3Record.Notes_Scope)
            {
                stringBuilder.AppendLine(string.Format("\tNOTE\t{0}", note));
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Equipment\tAHU guid\tAir system guid\tAHU\tManufacturer\tModel\tReference\tSource\tCapacity SUP/EXT (l/s)\tPart F SUP/EXT (l/s)\tDesign SUP/EXT (l/s)\tOperating basis\tHR efficiency\tHR basis\tSFP W/(l/s)\tSFP basis\tFan pressure SUP/EXT (Pa)\tFan efficiency\tHeat gain SUP/EXT\tAssumptions");

            if (partOIteration3Record.Equipment.Count == 0)
            {
                stringBuilder.AppendLine(None);
            }

            foreach (PartOIteration3EquipmentEvidence partOIteration3EquipmentEvidence in partOIteration3Record.Equipment)
            {
                stringBuilder.AppendLine(string.Format(
                    cultureInfo,
                    "\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7:0.###} / {8:0.###}\t{9} / {10}\t{11:0.###} / {12:0.###}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18} / {19}\t{20}\t{21} / {22}\t{23}; {24}",
                    partOIteration3EquipmentEvidence.Guid_AirHandlingUnit,
                    partOIteration3EquipmentEvidence.Guid_AirSystem,
                    partOIteration3EquipmentEvidence.Name_AirHandlingUnit,
                    partOIteration3EquipmentEvidence.Manufacturer,
                    partOIteration3EquipmentEvidence.Model,
                    partOIteration3EquipmentEvidence.Reference,
                    partOIteration3EquipmentEvidence.Source,
                    partOIteration3EquipmentEvidence.MaximumSupplyFlowRate_Lps,
                    partOIteration3EquipmentEvidence.MaximumExtractFlowRate_Lps,
                    Number(partOIteration3EquipmentEvidence.PartFRequiredSupplyFlowRate_Lps, cultureInfo),
                    Number(partOIteration3EquipmentEvidence.PartFRequiredExtractFlowRate_Lps, cultureInfo),
                    partOIteration3EquipmentEvidence.DesignSupplyFlowRate_Lps,
                    partOIteration3EquipmentEvidence.DesignExtractFlowRate_Lps,
                    partOIteration3EquipmentEvidence.OperatingAirFlowBasis,
                    Number(partOIteration3EquipmentEvidence.SensibleHeatRecoveryEfficiency, cultureInfo),
                    partOIteration3EquipmentEvidence.HeatRecoveryEfficiencyBasis,
                    Number(partOIteration3EquipmentEvidence.SpecificFanPower_WPerLps, cultureInfo),
                    partOIteration3EquipmentEvidence.SpecificFanPowerBasis,
                    Number(partOIteration3EquipmentEvidence.SupplyFanPressure_Pa, cultureInfo),
                    Number(partOIteration3EquipmentEvidence.ExtractFanPressure_Pa, cultureInfo),
                    Number(partOIteration3EquipmentEvidence.FanOverallEfficiency, cultureInfo),
                    Number(partOIteration3EquipmentEvidence.SupplyFanHeatGainFactor, cultureInfo),
                    Number(partOIteration3EquipmentEvidence.ExtractFanHeatGainFactor, cultureInfo),
                    partOIteration3EquipmentEvidence.FanPowerSplitRule,
                    partOIteration3EquipmentEvidence.FanHeatGainAssumption));
            }

            //PR5B: the cooling module per unit - the four airflows kept in four columns, never one another.
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Cooling\tAHU guid\tAir system guid\tAHU\tManufacturer\tModel\tReference\tCooling module\tSource\tCapacity SUP/EXT (l/s)\tDesignAirFlow SUP/EXT (l/s)\tCooling ceiling (l/s)\tLaw (C -> fraction)\tGate (C)\tTable\tTable SHA-256\tOperatingAirFlow min/mean/max (l/s)\tCooling h\tHeating h\tBelow gate h\tCooling below gate h\tOut of range h\tOff law h\tIn published domain h\tAir-side sensible (kWh)\tTable error max (K)\tVentilation deviation max (l/s)\tDeclared rules");

            if (partOIteration3Record.Cooling.Count == 0)
            {
                stringBuilder.AppendLine(None);
            }

            foreach (PartOIteration3CoolingEvidence partOIteration3CoolingEvidence in partOIteration3Record.Cooling)
            {
                stringBuilder.AppendLine(string.Format(
                    cultureInfo,
                    "\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8:0.###} / {9:0.###}\t{10:0.###} / {11:0.###}\t{12:0.###}\t{13:0.###} -> {14:0.###}, {15:0.###} -> {16:0.###}\t{17:0.###}\t{18}\t{19}\t{20:0.###} / {21:0.###} / {22:0.###}\t{23}\t{24}\t{25}\t{26}\t{27}\t{28}\t{29}\t{30:0.#}\t{31:0.######}\t{32:0.####}\t{33}",
                    partOIteration3CoolingEvidence.Guid_AirHandlingUnit,
                    partOIteration3CoolingEvidence.Guid_AirSystem,
                    partOIteration3CoolingEvidence.Name_AirHandlingUnit,
                    partOIteration3CoolingEvidence.Manufacturer,
                    partOIteration3CoolingEvidence.Model,
                    partOIteration3CoolingEvidence.Reference,
                    partOIteration3CoolingEvidence.CoolingModuleModel,
                    partOIteration3CoolingEvidence.Source,
                    partOIteration3CoolingEvidence.MaximumSupplyFlowRate_Lps,
                    partOIteration3CoolingEvidence.MaximumExtractFlowRate_Lps,
                    partOIteration3CoolingEvidence.DesignSupplyFlowRate_Lps,
                    partOIteration3CoolingEvidence.DesignExtractFlowRate_Lps,
                    partOIteration3CoolingEvidence.MaximumOperatingAirFlow_Lps,
                    partOIteration3CoolingEvidence.ControlTemperature_Low_C,
                    partOIteration3CoolingEvidence.FlowFraction_Low,
                    partOIteration3CoolingEvidence.ControlTemperature_High_C,
                    partOIteration3CoolingEvidence.FlowFraction_High,
                    partOIteration3CoolingEvidence.CoolingEnableTemperature_C,
                    partOIteration3CoolingEvidence.Table,
                    partOIteration3CoolingEvidence.Sha256_Table,
                    partOIteration3CoolingEvidence.OperatingAirFlowMinimum_Lps,
                    partOIteration3CoolingEvidence.OperatingAirFlowMean_Lps,
                    partOIteration3CoolingEvidence.OperatingAirFlowMaximum_Lps,
                    partOIteration3CoolingEvidence.Count_Cooling,
                    partOIteration3CoolingEvidence.Count_Heating,
                    partOIteration3CoolingEvidence.Count_BelowGate,
                    partOIteration3CoolingEvidence.Count_GateViolation,
                    partOIteration3CoolingEvidence.Count_OutOfRange,
                    partOIteration3CoolingEvidence.Count_OffLaw,
                    partOIteration3CoolingEvidence.Count_InPublishedDomain,
                    partOIteration3CoolingEvidence.Cooling_kWh,
                    partOIteration3CoolingEvidence.MaximumTableError_K,
                    partOIteration3CoolingEvidence.MaximumCanonicalDeviation_Lps,
                    partOIteration3CoolingEvidence.DeclaredRules));
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Bound rooms\tDwelling\tSpace\tSpace guid\tDwelling guid\tAir system guid\tSystem space guid\tNative zone\tDesign supply (l/s)\tDesign extract (l/s)");

            if (partOIteration3Record.Bindings.Count == 0)
            {
                stringBuilder.AppendLine(None);
            }

            foreach (PartOIteration3BindingRecord partOIteration3BindingRecord in partOIteration3Record.Bindings)
            {
                stringBuilder.AppendLine(string.Format(
                    cultureInfo,
                    "\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                    partOIteration3BindingRecord.Name_Dwelling,
                    partOIteration3BindingRecord.Name_Space,
                    partOIteration3BindingRecord.Guid_Space,
                    partOIteration3BindingRecord.Guid_Dwelling,
                    partOIteration3BindingRecord.Guid_AirSystem,
                    partOIteration3BindingRecord.Guid_SystemSpace,
                    partOIteration3BindingRecord.Reference_SystemZone,
                    Number(partOIteration3BindingRecord.DesignFlowRate_Supply_Lps, cultureInfo),
                    Number(partOIteration3BindingRecord.DesignFlowRate_Extract_Lps, cultureInfo)));
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Pairing files\tRole\tPath\tLength\tLast write (UTC)");

            if (partOIteration3Record.Files.Count == 0)
            {
                stringBuilder.AppendLine(None);
            }

            foreach (PartOIteration3FileRecord partOIteration3FileRecord in partOIteration3Record.Files)
            {
                stringBuilder.AppendLine(string.Format(
                    "\t{0}\t{1}\t{2}\t{3}",
                    partOIteration3FileRecord.Role,
                    partOIteration3FileRecord.Path,
                    partOIteration3FileRecord.Length,
                    Utc(partOIteration3FileRecord.Ticks_Utc)));
            }
        }

        private static string Verdict(PartOIteration3Assessment partOIteration3Assessment)
        {
            return partOIteration3Assessment is null || !partOIteration3Assessment.IsAssessed
                ? "was not assessed"
                : Core.Query.Description(partOIteration3Assessment.OccupiedSpaceComplianceStatus);
        }

        /// <summary>A missing count is an em dash, exactly as the grid renders it - so the two agree.</summary>
        private static string Number(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "—";
        }

        private static string Number(double? value, CultureInfo cultureInfo)
        {
            return value.HasValue ? value.Value.ToString("0.###", cultureInfo) : "—";
        }

        /// <summary>A recorded tick count as a sortable UTC stamp, or a plain marker where none was recorded.</summary>
        private static string Utc(long ticks)
        {
            return ticks <= 0 || ticks > DateTime.MaxValue.Ticks
                ? "<not recorded>"
                : new DateTime(ticks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }
    }
}
