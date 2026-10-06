// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the check before Apply found: the records the PROPOSED model has that the current model does not (the warnings and
    /// errors the change would introduce), as a one-line text and a <see cref="Log"/> for Details.
    /// </summary>
    public sealed class ThermalCheckDiff
    {
        internal ThermalCheckDiff(Log log, int errors, int warnings)
        {
            Log = log ?? new Log();
            NewErrors = errors;
            NewWarnings = warnings;
        }

        /// <summary>The new records (errors and warnings), for "Details".</summary>
        public Log Log { get; }

        public int NewErrors { get; }

        public int NewWarnings { get; }

        public bool Passed => NewErrors == 0 && NewWarnings == 0;

        /// <summary>✓ nothing new, ⚠ new warnings only, ✕ new errors.</summary>
        public string Glyph => NewErrors > 0 ? "✕" : NewWarnings > 0 ? "⚠" : "✓";

        /// <summary>E.g. "No new warnings", "3 new warnings", "1 new error and 2 new warnings".</summary>
        public string Text
        {
            get
            {
                if (Passed)
                {
                    return "No new warnings";
                }

                List<string> parts = new List<string>();
                if (NewErrors > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, "{0} new {1}", NewErrors, NewErrors == 1 ? "error" : "errors"));
                }

                if (NewWarnings > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, "{0} new {1}", NewWarnings, NewWarnings == 1 ? "warning" : "warnings"));
                }

                return string.Join(" and ", parts);
            }
        }
    }

    public static partial class Query
    {
        /// <summary>
        /// The check BEFORE Apply: the existing scoped U-value and glazing checks (<see cref="UValueCheckSummary"/>,
        /// <see cref="GlazingCheckSummary"/> - the rules behind Edit &gt; ModelCheck) run on the PROPOSED model for each change, and
        /// the same rules on the CURRENT model for the construction the change replaces and the same elements; what only the
        /// proposed model reports is new. Records are compared by their text with the new construction's name and Guid read as the
        /// one it replaces, so a problem the elements already have (a rule that names the construction) is not counted as new.
        /// Reads both models only.
        /// </summary>
        /// <param name="current">The model as it is now.</param>
        /// <param name="proposed">The model with the change set made (<c>Modify.ProposeThermalChange</c>).</param>
        /// <param name="result">The result of building <paramref name="proposed"/>; its requests line up with its results.</param>
        public static ThermalCheckDiff ThermalCheckDiff(AnalyticalModel current, AnalyticalModel proposed, ThermalChangeResult result)
        {
            Log log = new Log();
            if (current == null || proposed == null || result == null || !result.Succeeded || result.ChangeSet == null)
            {
                return new ThermalCheckDiff(log, 0, 0);
            }

            HashSet<string> seen = new HashSet<string>();

            for (int i = 0; i < result.UValueResults.Count; i++)
            {
                SetUValueResult after = result.UValueResults[i];
                SetUValueRequest request = result.ChangeSet.UValueRequests[i];

                // The current model, for the construction being replaced and the panels it reaches (the Guids survive the change).
                SetUValueResult before = new SetUValueResult(request, after.SourceConstruction, after.SourceConstruction, after.SourceMaterialName, after.SourceMaterialName, false, after.OldThickness, after.OldThickness, after.PanelGuids);

                Add(log, seen, UValueCheckSummary(current, before), UValueCheckSummary(proposed, after), after.Construction?.Name, after.Construction?.Guid, after.SourceConstruction?.Name, after.SourceConstruction?.Guid);
            }

            // An existing construction assigned to panels: the same scoped panel rules, read through the panel result they describe.
            for (int i = 0; i < result.ConstructionResults.Count; i++)
            {
                SetConstructionResult after = result.ConstructionResults[i];
                SetConstructionRequest request = result.ChangeSet.ConstructionRequests[i];

                SetUValueRequest asUValue = new SetUValueRequest() { ConstructionGuid = request.SourceConstructionGuid, Scope = after.Scope };
                SetUValueResult before = new SetUValueResult(asUValue, after.SourceConstruction, after.SourceConstruction, null, null, false, double.NaN, double.NaN, after.PanelGuids);
                SetUValueResult after_Panels = new SetUValueResult(asUValue, after.SourceConstruction, after.Construction, null, null, false, double.NaN, double.NaN, after.PanelGuids);

                Add(log, seen, UValueCheckSummary(current, before), UValueCheckSummary(proposed, after_Panels), after.Construction?.Name, after.Construction?.Guid, after.SourceConstruction?.Name, after.SourceConstruction?.Guid);
            }

            for (int i = 0; i < result.GlazingResults.Count; i++)
            {
                SetGlazingResult after = result.GlazingResults[i];
                SetGlazingRequest request = result.ChangeSet.GlazingRequests[i];

                SetGlazingResult before = new SetGlazingResult(request, after.SourceApertureConstruction, after.SourceApertureConstruction, false, new List<string>(), after.ApertureGuids, after.PanelGuids);

                Add(log, seen, GlazingCheckSummary(current, before), GlazingCheckSummary(proposed, after), after.ApertureConstruction?.Name, after.ApertureConstruction?.Guid, after.SourceApertureConstruction?.Name, after.SourceApertureConstruction?.Guid);
            }

            List<LogRecord> records = log.ToList();
            return new ThermalCheckDiff(log, records.Count(x => x.LogRecordType == LogRecordType.Error), records.Count(x => x.LogRecordType == LogRecordType.Warning));
        }

        // Adds the records of the proposed check that the current check does not have (errors and warnings only; messages never count).
        private static void Add(Log log, HashSet<string> seen, UValueCheckSummary summary_Current, UValueCheckSummary summary_Proposed, string name_New, Guid? guid_New, string name_Old, Guid? guid_Old)
        {
            HashSet<string> baseline = new HashSet<string>(summary_Current.Log.ToList().Select(x => Signature(x, name_New, guid_New, name_Old, guid_Old)));

            foreach (LogRecord logRecord in summary_Proposed.Log.ToList())
            {
                if (logRecord == null || (logRecord.LogRecordType != LogRecordType.Error && logRecord.LogRecordType != LogRecordType.Warning))
                {
                    continue;
                }

                string signature = Signature(logRecord, name_New, guid_New, name_Old, guid_Old);
                if (!baseline.Contains(signature) && seen.Add(signature))
                {
                    log.Add(logRecord);
                }
            }
        }

        // The record's kind and text with the new construction read as the old one (Guid first, then name).
        private static string Signature(LogRecord logRecord, string name_New, Guid? guid_New, string name_Old, Guid? guid_Old)
        {
            string text = logRecord?.Text ?? string.Empty;

            if (guid_New.HasValue && guid_Old.HasValue && guid_New != guid_Old)
            {
                text = text.Replace(guid_New.Value.ToString(), guid_Old.Value.ToString());
            }

            if (!string.IsNullOrEmpty(name_New) && !string.IsNullOrEmpty(name_Old) && name_New != name_Old)
            {
                text = text.Replace(name_New, name_Old);
            }

            return logRecord?.LogRecordType + "|" + text;
        }
    }
}
