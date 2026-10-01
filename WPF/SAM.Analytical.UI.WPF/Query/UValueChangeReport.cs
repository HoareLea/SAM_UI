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
        /// <summary>The heading of a saved "Set U-value" report.</summary>
        public const string UValueChangeReportHeading = "U-VALUE CHANGE";

        /// <summary>
        /// Where the report of a U-value change applied at <paramref name="appliedAt"/> is saved: the model's own
        /// folder (the default simulation output folder), as "&lt;model&gt;_UValueChange_&lt;yyyyMMdd-HHmmss&gt;.txt".
        /// Null when the model has not been saved (then only Copy All is offered).
        /// </summary>
        public static string Path_UValueChangeReport(string path_Model, DateTime appliedAt)
        {
            if (string.IsNullOrWhiteSpace(path_Model))
            {
                return null;
            }

            string directory;
            string name;
            try
            {
                directory = Path.GetDirectoryName(path_Model);
                name = Path.GetFileNameWithoutExtension(path_Model);
            }
            catch (ArgumentException)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return Path.Combine(directory, string.Format(CultureInfo.InvariantCulture, "{0}_UValueChange_{1:yyyyMMdd-HHmmss}.txt", name, appliedAt));
        }

        /// <summary>
        /// The plain-text report of one applied U-value change: a provenance block (model, when, the "may since have
        /// been undone" stamp), the change (constructions, layer, old/new thickness, old/new U, scope) and the
        /// scoped check. Written from the result and the check alone; nothing is recalculated.
        /// </summary>
        public static string UValueChangeReportText(SetUValueResult result, UValueCheckSummary checkSummary, string path_Model)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(UValueChangeReportHeading);
            stringBuilder.AppendLine(new string('=', UValueChangeReportHeading.Length));

            if (result == null || !result.Succeeded)
            {
                stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Not applied: {0}", result?.Error ?? "no change was made."));
                return stringBuilder.ToString();
            }

            Line(stringBuilder, "Model", string.IsNullOrWhiteSpace(path_Model) ? "(not saved)" : path_Model);
            Line(stringBuilder, "Applied", string.Format(CultureInfo.InvariantCulture, "applied at {0:yyyy-MM-dd HH:mm:ss}; may since have been undone", result.AppliedAt));
            Line(stringBuilder, "Method", "SAM Set U-value: Tas TCD thermal transmittance, one layer varied");
            stringBuilder.AppendLine();

            string sourceName = result.SourceConstruction?.Name;
            string name = result.Construction?.Name;
            Line(stringBuilder, "Construction", result.Mode == UValueApplyMode.ModifyInPlace
                ? string.Format(CultureInfo.InvariantCulture, "{0} (modified in place)", name)
                : string.Format(CultureInfo.InvariantCulture, "{0} -> {1} (new construction; {0} unchanged)", sourceName, name));

            Line(stringBuilder, "Layer", string.Format(CultureInfo.InvariantCulture, "{0}: {1} -> {2}", result.LayerIndex + 1, result.SourceMaterialName, result.MaterialName));
            Line(stringBuilder, "Thickness", string.Format(CultureInfo.InvariantCulture, "{0} mm -> {1} mm", Millimetres_Report(result.OldThickness), Millimetres_Report(result.NewThickness)));
            Line(stringBuilder, "U-value", string.Format(CultureInfo.InvariantCulture, "{0} -> {1} W/m2K (target {2}; {3} heat flow)", U_Report(result.OldThermalTransmittance), U_Report(result.NewThermalTransmittance), U_Report(result.TargetThermalTransmittance), result.HeatFlowDirection.ToString().ToLowerInvariant()));
            Line(stringBuilder, "Scope", Scope_Report(result, sourceName));
            Line(stringBuilder, "Material", result.MaterialAdded
                ? string.Format(CultureInfo.InvariantCulture, "{0} added to the Material Library", result.MaterialName)
                : string.Format(CultureInfo.InvariantCulture, "{0} already in the Material Library", result.MaterialName));
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("CHECK (SAM model-check rules over the changed construction and its panels)");
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

        private static void Line(StringBuilder stringBuilder, string label, string value)
        {
            stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-13} {1}", label + ":", value));
        }

        private static string Scope_Report(SetUValueResult result, string sourceName)
        {
            int count = result.PanelCount;
            string panels = count == 1 ? "panel" : "panels";
            switch (result.Scope)
            {
                case UValueApplyScope.SelectedPanels:
                    return string.Format(CultureInfo.InvariantCulture, "{0} selected {1} that used {2}", count, panels, sourceName);

                case UValueApplyScope.DontAssign:
                    return "not assigned to any panel";

                default:
                    return string.Format(CultureInfo.InvariantCulture, "{0} {1} (all that used {2})", count, panels, sourceName);
            }
        }

        private static string Millimetres_Report(double metres)
        {
            return double.IsNaN(metres) ? "?" : (metres * 1000).ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string U_Report(double thermalTransmittance)
        {
            return double.IsNaN(thermalTransmittance) ? "?" : thermalTransmittance.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
