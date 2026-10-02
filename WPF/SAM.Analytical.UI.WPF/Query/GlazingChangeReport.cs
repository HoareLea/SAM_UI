// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
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
            Line_Glazing(stringBuilder, "Guid", chosen == null ? "?" : chosen.Guid.ToString());
            Line_Glazing(stringBuilder, "Source", string.Format(CultureInfo.InvariantCulture, "{0} ({1})", FileName_Glazing(result.SourceLabel) ?? "?", Kind_Glazing(result.SourceKind)));
            BuiltFrom_Glazing(stringBuilder, result.BuilderProvenance);
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

        // A further line of the value above, aligned with it.
        private static void Continue_Glazing(StringBuilder stringBuilder, string value)
        {
            stringBuilder.AppendLine(new string(' ', 14) + value);
        }

        private static string Kind_Glazing(GlazingSourceKind kind)
        {
            switch (kind)
            {
                case GlazingSourceKind.Model:
                    return "existing model system";

                case GlazingSourceKind.Library:
                    return "SAM default library";

                case GlazingSourceKind.User:
                    return "user glazing library";

                default:
                    return "added source";
            }
        }

        // How a system made with the Glazing System Builder was built, from the provenance it carries (labels and file names only, never a
        // folder: a source label given as a path is cut to its file name here too). A system without it says so.
        private static void BuiltFrom_Glazing(StringBuilder stringBuilder, GlazingBuilderProvenance provenance)
        {
            if (provenance == null)
            {
                Line_Glazing(stringBuilder, "Built from", "not made with the Glazing System Builder (no Builder provenance)");
                return;
            }

            string created = provenance.CreatedUtc == default ? "date unknown" : provenance.CreatedUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
            string basedOn = string.IsNullOrWhiteSpace(provenance.BasedOnName)
                ? "not based on another system"
                : provenance.BasedOnGuid.HasValue ? string.Format(CultureInfo.InvariantCulture, "based on {0} ({1})", provenance.BasedOnName, provenance.BasedOnGuid.Value) : "based on " + provenance.BasedOnName;
            string intended = provenance.IntendedPanelType == PanelType.Undefined ? string.Empty : "; intended for " + provenance.IntendedPanelType;
            Line_Glazing(stringBuilder, "Built from", string.Format(CultureInfo.InvariantCulture, "SAM Glazing System Builder, saved {0}; {1}{2}", created, basedOn, intended));

            List<string> panes = (provenance.Panes ?? new List<GlazingBuilderPaneRecord>()).OrderBy(x => x.Position).Select(x =>
            {
                string name = !string.IsNullOrWhiteSpace(x.OriginalName) ? x.OriginalName : x.Material;
                string from = FileName_Glazing(!string.IsNullOrWhiteSpace(x.SourceLabel) ? x.SourceLabel : x.SourceFile);
                string savedAs = !string.IsNullOrWhiteSpace(x.Material) && x.Material != name && x.Material != name + " Reversed" ? string.Format(CultureInfo.InvariantCulture, ", saved as {0}", x.Material) : string.Empty;
                return string.Format(CultureInfo.InvariantCulture, "{0}. {1}{2} [{3:0.#} mm, from {4}{5}]", x.Position, name, x.Reversed ? " (reversed)" : string.Empty, x.Thickness * 1000, from ?? "?", savedAs);
            }).ToList();
            Continue_Glazing(stringBuilder, "Panes (outside -> inside): " + (panes.Count == 0 ? "none recorded" : string.Join(" | ", panes)));

            List<string> gaps = (provenance.Gaps ?? new List<GlazingBuilderGapRecord>()).OrderBy(x => x.Position).Select(x => string.Format(
                CultureInfo.InvariantCulture,
                "{0}. {1} {2:0.#} mm (HTC {3} W/m2K at {4} deg)",
                x.Position,
                x.Gas,
                x.Thickness * 1000,
                U_Glazing(x.HeatTransferCoefficient),
                double.IsNaN(x.TiltDegrees) ? "?" : x.TiltDegrees.ToString("0", CultureInfo.InvariantCulture))).ToList();
            Continue_Glazing(stringBuilder, "Gaps (outside -> inside): " + (gaps.Count == 0 ? "none" : string.Join(" | ", gaps)));

            string width = double.IsNaN(provenance.FrameWidth) ? "no width entered" : string.Format(CultureInfo.InvariantCulture, "width {0:0.#} mm", provenance.FrameWidth * 1000);
            Continue_Glazing(stringBuilder, string.Equals(provenance.Frame, "Copied", StringComparison.OrdinalIgnoreCase)
                ? string.Format(CultureInfo.InvariantCulture, "Frame: copied from {0}, {1}", string.IsNullOrWhiteSpace(provenance.FrameCopiedFromName) ? "?" : provenance.FrameCopiedFromName, width)
                : "Frame: none");
        }

        // The file name of a label that may have been given as a path; null for none.
        private static string FileName_Glazing(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            int index = trimmed.LastIndexOfAny(new[] { '\\', '/' });
            return index < 0 ? trimmed : trimmed.Substring(index + 1);
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
