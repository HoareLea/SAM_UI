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
        /// <summary>The heading of a saved "Set glazing" report.</summary>
        public const string GlazingChangeReportHeading = "GLAZING CHANGE";

        /// <summary>
        /// Where the report of a glazing change applied at <paramref name="appliedAt"/> is saved: the model's own
        /// folder, as "&lt;model&gt;_GlazingChange_&lt;yyyyMMdd-HHmmss&gt;.txt". Null when the model has not been saved
        /// (then only Copy All is offered).
        /// </summary>
        public static string Path_GlazingChangeReport(string path_Model, DateTime appliedAt)
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

            return Path.Combine(directory, string.Format(CultureInfo.InvariantCulture, "{0}_GlazingChange_{1:yyyyMMdd-HHmmss}.txt", name, appliedAt));
        }

        /// <summary>
        /// The plain-text report of one applied glazing change: a provenance block (model, when, the "may since have
        /// been undone" stamp), the change (aperture constructions, build-up, Ug / Uf / g / light / Uw old and new, the
        /// Uw basis, scope, materials added) and the scoped check. Written from the result and the check alone; nothing
        /// is recalculated.
        /// </summary>
        public static string GlazingChangeReportText(SetGlazingResult result, UValueCheckSummary checkSummary, string path_Model)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(GlazingChangeReportHeading);
            stringBuilder.AppendLine(new string('=', GlazingChangeReportHeading.Length));

            if (result == null || !result.Succeeded)
            {
                stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Not applied: {0}", result?.Error ?? "no change was made."));
                return stringBuilder.ToString();
            }

            Line_Glazing(stringBuilder, "Model", string.IsNullOrWhiteSpace(path_Model) ? "(not saved)" : path_Model);
            Line_Glazing(stringBuilder, "Applied", string.Format(CultureInfo.InvariantCulture, "applied at {0:yyyy-MM-dd HH:mm:ss}; may since have been undone", result.AppliedAt));
            Line_Glazing(stringBuilder, "Method", "SAM Set glazing: complete glazing systems compared by Tas TCD (Ug, Uf, g, light transmittance); overall Uw weighed by the apertures' pane and frame areas");
            stringBuilder.AppendLine();

            ApertureConstruction source = result.SourceApertureConstruction;
            ApertureConstruction chosen = result.ApertureConstruction;
            Line_Glazing(stringBuilder, "Glazing", result.ApertureConstructionAdded
                ? string.Format(CultureInfo.InvariantCulture, "{0} -> {1} (added to the model; {0} unchanged)", source?.Name, chosen?.Name)
                : string.Format(CultureInfo.InvariantCulture, "{0} -> {1} (already in the model)", source?.Name, chosen?.Name));
            Line_Glazing(stringBuilder, "Pane", string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", BuildUp_Report(source?.PaneConstructionLayers), BuildUp_Report(chosen?.PaneConstructionLayers)));
            Line_Glazing(stringBuilder, "Frame", string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", BuildUp_Report(source?.FrameConstructionLayers), BuildUp_Report(chosen?.FrameConstructionLayers)));

            GlazingValues before = result.OldValues;
            GlazingValues after = result.Values;
            Line_Glazing(stringBuilder, "Ug", string.Format(CultureInfo.InvariantCulture, "{0} -> {1} W/m2K", U_Glazing(before?.Ug ?? double.NaN), U_Glazing(after?.Ug ?? double.NaN)));
            Line_Glazing(stringBuilder, "Uf", string.Format(CultureInfo.InvariantCulture, "{0} -> {1} W/m2K", Uf_Glazing(before, source), Uf_Glazing(after, chosen)));
            Line_Glazing(stringBuilder, "g", string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", U_Glazing(before?.G ?? double.NaN), U_Glazing(after?.G ?? double.NaN)));
            Line_Glazing(stringBuilder, "Light", string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", U_Glazing(before?.LightTransmittance ?? double.NaN), U_Glazing(after?.LightTransmittance ?? double.NaN)));
            Line_Glazing(stringBuilder, "Uw", string.Format(CultureInfo.InvariantCulture, "{0} -> {1} W/m2K ({2}{3})", U_Glazing(result.OldUw), U_Glazing(result.NewUw), result.UwBasis == GlazingUwBasis.Approximate ? "approx. Uw (80/20)" : "area-weighted over the apertures' pane and frame areas", double.IsNaN(result.TargetUw) ? string.Empty : string.Format(CultureInfo.InvariantCulture, "; target at most {0}", U_Glazing(result.TargetUw))));
            Line_Glazing(stringBuilder, "Scope", Scope_Glazing(result, source?.Name));
            Line_Glazing(stringBuilder, "Materials", result.MaterialNamesAdded.Count == 0
                ? "none added to the Material Library"
                : string.Format(CultureInfo.InvariantCulture, "{0} added to the Material Library: {1}", result.MaterialNamesAdded.Count, string.Join("; ", result.MaterialNamesAdded)));
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("CHECK (SAM model-check rules over the applied glazing, its apertures and their panels)");
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

        private static void Line_Glazing(StringBuilder stringBuilder, string label, string value)
        {
            stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-13} {1}", label + ":", value));
        }

        private static string Scope_Glazing(SetGlazingResult result, string sourceName)
        {
            int count = result.ApertureCount;
            string apertures = count == 1 ? "aperture" : "apertures";
            switch (result.Scope)
            {
                case ThermalApplyScope.SelectedOnly:
                    return string.Format(CultureInfo.InvariantCulture, "{0} selected {1} that used {2}", count, apertures, sourceName);

                case ThermalApplyScope.DontAssign:
                    return "not assigned to any aperture";

                default:
                    return string.Format(CultureInfo.InvariantCulture, "{0} {1} (all that used {2})", count, apertures, sourceName);
            }
        }

        private static string BuildUp_Report(System.Collections.Generic.List<ConstructionLayer> constructionLayers)
        {
            if (constructionLayers == null || constructionLayers.Count == 0)
            {
                return "none";
            }

            return string.Join(" / ", constructionLayers.Where(x => x != null).Select(x => string.Format(CultureInfo.InvariantCulture, "{0:0.#} mm {1}", x.Thickness * 1000, x.Name)));
        }

        // The frame's U-value, or "none" for a system without frame layers (it has no frame, so no Uf).
        private static string Uf_Glazing(GlazingValues values, ApertureConstruction apertureConstruction)
        {
            return apertureConstruction != null && !apertureConstruction.HasFrameConstructionLayers() ? "none" : U_Glazing(values?.Uf ?? double.NaN);
        }

        private static string U_Glazing(double value)
        {
            return double.IsNaN(value) ? "?" : value.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
