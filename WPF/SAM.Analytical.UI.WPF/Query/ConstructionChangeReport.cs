// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>The heading of a saved report of an existing construction assigned in the Thermal Performance panel.</summary>
        public const string ConstructionChangeReportHeading = "CONSTRUCTION CHANGE";

        /// <summary>
        /// Where the report of a construction change applied at <paramref name="appliedAt"/> is saved: the model's own folder, as
        /// "&lt;model&gt;_ConstructionChange_&lt;yyyyMMdd-HHmmss&gt;.txt" (the naming of the U-value and glazing reports). Null when the model has not been saved.
        /// </summary>
        public static string Path_ConstructionChangeReport(string path_Model, DateTime appliedAt)
        {
            // The same rule as the U-value report's folder and name, with its own kind in the name.
            string path = Path_UValueChangeReport(path_Model, appliedAt);
            return path?.Replace("_UValueChange_", "_ConstructionChange_");
        }

        /// <summary>
        /// The scoped check after assigning an existing construction: the same SAM model-check rules the U-value report runs, over the construction
        /// now on the panels and the panels it was assigned to (read through the U-value check, which needs only those two).
        /// </summary>
        public static UValueCheckSummary ConstructionCheckSummary(AnalyticalModel analyticalModel, SetConstructionResult result)
        {
            if (result == null || !result.Succeeded)
            {
                return UValueCheckSummary(analyticalModel, null);
            }

            SetUValueRequest request = new SetUValueRequest() { ConstructionGuid = result.SourceConstruction.Guid, Scope = result.Scope };
            return UValueCheckSummary(analyticalModel, new SetUValueResult(request, result.SourceConstruction, result.Construction, null, null, false, double.NaN, double.NaN, result.PanelGuids));
        }

        /// <summary>
        /// The plain-text report of one applied construction change: a provenance block (model, when, the "may since have been undone" stamp), the
        /// change (the construction chosen and where it came from, the construction it replaced, U-value before and after on the heat-flow basis,
        /// scope and count, materials added), the notes shown before Apply, and the scoped check. The layout of the U-value and glazing reports;
        /// written from the result and the check alone, nothing is recalculated.
        /// </summary>
        public static string ConstructionChangeReportText(SetConstructionResult result, UValueCheckSummary checkSummary, string path_Model)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(ConstructionChangeReportHeading);
            stringBuilder.AppendLine(new string('=', ConstructionChangeReportHeading.Length));

            if (result == null || !result.Succeeded)
            {
                stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Not applied: {0}", result?.Error ?? "no change was made."));
                return stringBuilder.ToString();
            }

            Line(stringBuilder, "Model", string.IsNullOrWhiteSpace(path_Model) ? "(not saved)" : path_Model);
            Line(stringBuilder, "Applied", string.Format(CultureInfo.InvariantCulture, "applied at {0:yyyy-MM-dd HH:mm:ss}; may since have been undone", result.AppliedAt));
            Line(stringBuilder, "Method", "SAM Thermal Performance: an existing construction assigned (nothing generated); U-values by Tas TCD thermal transmittance");
            stringBuilder.AppendLine();

            string sourceName = result.SourceConstruction?.Name;
            string name = result.Construction?.Name;
            Line(stringBuilder, "Construction", string.Format(CultureInfo.InvariantCulture, "{0} -> {1} ({2}; {0} unchanged)", sourceName, name, result.ConstructionAdded ? "added to the model" : "already in the model"));
            Line(stringBuilder, "Identity", string.Format(CultureInfo.InvariantCulture, "Guid {0}", result.Construction?.Guid));
            Line(stringBuilder, "Source", string.Format(CultureInfo.InvariantCulture, "{0} ({1})", string.IsNullOrWhiteSpace(result.SourceLabel) ? "?" : result.SourceLabel, Kind_Report(result.SourceKind)));
            Line(stringBuilder, "Build-up", BuildUp_Report(result.Construction));
            Line(stringBuilder, "U-value", string.Format(CultureInfo.InvariantCulture, "{0} -> {1} W/m2K (target {2}; {3} heat flow)", U_Report(result.OldThermalTransmittance), U_Report(result.NewThermalTransmittance), U_Report(result.TargetThermalTransmittance), result.HeatFlowDirection.ToString().ToLowerInvariant()));
            Line(stringBuilder, "Scope", Scope_Report(result, sourceName));
            Line(stringBuilder, "Materials", result.MaterialNamesAdded.Count == 0
                ? "none added (the model has them all)"
                : string.Format(CultureInfo.InvariantCulture, "{0} added to the Material Library: {1}", result.MaterialNamesAdded.Count, string.Join(", ", result.MaterialNamesAdded)));

            if (result.Notes.Count != 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("NOTES SHOWN BEFORE APPLY");
                foreach (string note in result.Notes)
                {
                    stringBuilder.AppendLine("  " + note);
                }
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("CHECK (SAM model-check rules over the assigned construction and its panels)");
            stringBuilder.AppendLine(checkSummary?.Text ?? "Not run.");
            if (checkSummary != null)
            {
                foreach (LogRecord logRecord in checkSummary.Log.OrderByDescending(x => x.LogRecordType))
                {
                    stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0}: {1}", logRecord.LogRecordType, logRecord.Text));
                }
            }

            return stringBuilder.ToString();
        }

        private static string Kind_Report(GlazingSourceKind kind)
        {
            switch (kind)
            {
                case GlazingSourceKind.Model:
                    return "existing model construction";

                case GlazingSourceKind.Library:
                    return "default library";

                default:
                    return "added source";
            }
        }

        private static string BuildUp_Report(Construction construction)
        {
            return construction?.ConstructionLayers == null
                ? "?"
                : string.Join(" / ", construction.ConstructionLayers.Where(x => x != null).Select(x => string.Format(CultureInfo.InvariantCulture, "{0:0.#} mm {1}", x.Thickness * 1000, x.Name)));
        }

        private static string Scope_Report(SetConstructionResult result, string sourceName)
        {
            int count = result.PanelCount;
            string panels = count == 1 ? "panel" : "panels";
            return result.Scope == ThermalApplyScope.SelectedOnly
                ? string.Format(CultureInfo.InvariantCulture, "{0} selected {1} that used {2}", count, panels, sourceName)
                : string.Format(CultureInfo.InvariantCulture, "{0} {1} (all that used {2})", count, panels, sourceName);
        }
    }
}
