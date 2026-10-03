// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// How a saved glazing system was built: one named <see cref="ParameterSet"/> ("SAM Glazing System Builder", fixed Guid) on the
    /// <see cref="ApertureConstruction"/>. It travels with the system into any model it is applied to, so it holds labels and FILE NAMES
    /// only, never a machine path. One set per object (SAM merges sets of the same name, later values winning - SAM#146/PR2A-0); a saved
    /// system is immutable, so its set is written once.
    /// </summary>
    public sealed class GlazingBuilderProvenance
    {
        public const string ParameterSetName = "SAM Glazing System Builder";

        public static readonly Guid ParameterSetGuid = new Guid("5a3e0e01-6b1d-4c1e-9a52-7c0f1e2d3b40");

        /// <summary>
        /// 1 = the first schema; 2 adds <see cref="SupersedesGuid"/> / <see cref="SupersedesName"/> (a system saved with "Save and replace"). Every new save
        /// writes 2; a reader takes a missing key as "none", so a version-1 system reads exactly as it did.
        /// </summary>
        public const int CurrentSchemaVersion = 2;

        // A value that may be NaN (no coefficient) is written as "NaN": plain JSON has no NaN.
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions() { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public DateTime CreatedUtc { get; set; }

        public string BasedOnName { get; set; }

        public Guid? BasedOnGuid { get; set; }

        /// <summary>The Guid of the system this one REPLACED (saved with "Save and replace"; that system is in the archive); null for any other save.</summary>
        public Guid? SupersedesGuid { get; set; }

        /// <summary>The name the replaced system had.</summary>
        public string SupersedesName { get; set; }

        public PanelType IntendedPanelType { get; set; } = PanelType.Undefined;

        /// <summary>The panel tilt the gap heat transfer coefficients were derived for [deg] (90 = vertical/wall, 0 = horizontal/roof, heat flow up).</summary>
        public double GapEvaluationTiltDegrees { get; set; } = double.NaN;

        /// <summary>How the gap heat transfer coefficients were derived (method and orientation basis), in words.</summary>
        public string GapHeatTransferBasis { get; set; }

        /// <summary>Panes, outermost first.</summary>
        public List<GlazingBuilderPaneRecord> Panes { get; set; } = new List<GlazingBuilderPaneRecord>();

        /// <summary>Gaps, outermost first.</summary>
        public List<GlazingBuilderGapRecord> Gaps { get; set; } = new List<GlazingBuilderGapRecord>();

        /// <summary>"None", or "Copied" (from <see cref="FrameCopiedFromName"/>).</summary>
        public string Frame { get; set; } = "None";

        public string FrameCopiedFromName { get; set; }

        public Guid? FrameCopiedFromGuid { get; set; }

        public double FrameWidth { get; set; } = double.NaN;

        /// <summary>The values Tas gave the draft when it was saved; null when there were none.</summary>
        public GlazingValues Performance { get; set; }

        /// <summary>What calculated <see cref="Performance"/>, e.g. "Tas TCD (SAM_Tas ThermalTransmittanceCalculator.CalculateGlazing)".</summary>
        public string PerformanceEngine { get; set; }

        public string SamTasVersion { get; set; }

        public ParameterSet ToParameterSet()
        {
            ParameterSet result = new ParameterSet(ParameterSetGuid, ParameterSetName);
            result.Add("Schema Version", SchemaVersion);
            result.Add("Created UTC", CreatedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(BasedOnName))
            {
                result.Add("Based On Name", BasedOnName);
            }

            if (BasedOnGuid.HasValue)
            {
                result.Add("Based On Guid", BasedOnGuid.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(SupersedesName))
            {
                result.Add("Supersedes Name", SupersedesName);
            }

            if (SupersedesGuid.HasValue)
            {
                result.Add("Supersedes Guid", SupersedesGuid.Value.ToString());
            }

            result.Add("Intended Panel Type", IntendedPanelType.ToString());
            AddNumber(result, "Gap Evaluation Tilt [deg]", GapEvaluationTiltDegrees);
            if (!string.IsNullOrWhiteSpace(GapHeatTransferBasis))
            {
                result.Add("Gap Heat Transfer Basis", GapHeatTransferBasis);
            }

            result.Add("Panes", (JsonArray)JsonSerializer.SerializeToNode(Panes ?? new List<GlazingBuilderPaneRecord>(), JsonOptions));
            result.Add("Gaps", (JsonArray)JsonSerializer.SerializeToNode(Gaps ?? new List<GlazingBuilderGapRecord>(), JsonOptions));
            result.Add("Frame", Frame ?? "None");
            if (!string.IsNullOrWhiteSpace(FrameCopiedFromName))
            {
                result.Add("Frame Copied From Name", FrameCopiedFromName);
            }

            if (FrameCopiedFromGuid.HasValue)
            {
                result.Add("Frame Copied From Guid", FrameCopiedFromGuid.Value.ToString());
            }

            AddNumber(result, "Frame Width [m]", FrameWidth);

            if (Performance != null)
            {
                AddNumber(result, "Performance Ug [W/m2K]", Performance.Ug);
                AddNumber(result, "Performance g [-]", Performance.G);
                AddNumber(result, "Performance LT [-]", Performance.LightTransmittance);
                AddNumber(result, "Performance Uf [W/m2K]", Performance.Uf);
            }

            if (!string.IsNullOrWhiteSpace(PerformanceEngine))
            {
                result.Add("Performance Engine", PerformanceEngine);
            }

            if (!string.IsNullOrWhiteSpace(SamTasVersion))
            {
                result.Add("SAM_Tas Version", SamTasVersion);
            }

            return result;
        }

        /// <summary>The provenance of a system saved by the Builder; null for any other system.</summary>
        public static GlazingBuilderProvenance FromApertureConstruction(ApertureConstruction apertureConstruction)
        {
            ParameterSet parameterSet = apertureConstruction?.GetParameterSet(ParameterSetName);
            return parameterSet == null ? null : FromParameterSet(parameterSet);
        }

        public static GlazingBuilderProvenance FromParameterSet(ParameterSet parameterSet)
        {
            if (parameterSet == null)
            {
                return null;
            }

            GlazingBuilderProvenance result = new GlazingBuilderProvenance()
            {
                SchemaVersion = parameterSet.Contains("Schema Version") ? parameterSet.ToInt("Schema Version") : 0,
                BasedOnName = parameterSet.ToString("Based On Name"),
                BasedOnGuid = ParseGuid(parameterSet.ToString("Based On Guid")),
                SupersedesName = parameterSet.ToString("Supersedes Name"),
                SupersedesGuid = ParseGuid(parameterSet.ToString("Supersedes Guid")),
                IntendedPanelType = Analytical.Query.PanelType(parameterSet.ToString("Intended Panel Type")),
                GapEvaluationTiltDegrees = Number(parameterSet, "Gap Evaluation Tilt [deg]"),
                GapHeatTransferBasis = parameterSet.ToString("Gap Heat Transfer Basis"),
                Frame = parameterSet.ToString("Frame") ?? "None",
                FrameCopiedFromName = parameterSet.ToString("Frame Copied From Name"),
                FrameCopiedFromGuid = ParseGuid(parameterSet.ToString("Frame Copied From Guid")),
                FrameWidth = Number(parameterSet, "Frame Width [m]"),
                PerformanceEngine = parameterSet.ToString("Performance Engine"),
                SamTasVersion = parameterSet.ToString("SAM_Tas Version"),
            };

            if (DateTime.TryParse(parameterSet.ToString("Created UTC"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime created))
            {
                result.CreatedUtc = created.ToUniversalTime();
            }

            JsonArray panes = parameterSet.ToJsonArray("Panes");
            if (panes != null)
            {
                result.Panes = panes.Deserialize<List<GlazingBuilderPaneRecord>>(JsonOptions) ?? new List<GlazingBuilderPaneRecord>();
            }

            JsonArray gaps = parameterSet.ToJsonArray("Gaps");
            if (gaps != null)
            {
                result.Gaps = gaps.Deserialize<List<GlazingBuilderGapRecord>>(JsonOptions) ?? new List<GlazingBuilderGapRecord>();
            }

            double ug = Number(parameterSet, "Performance Ug [W/m2K]");
            if (!double.IsNaN(ug))
            {
                result.Performance = new GlazingValues(ug, Number(parameterSet, "Performance g [-]"), Number(parameterSet, "Performance LT [-]"), Number(parameterSet, "Performance Uf [W/m2K]"));
            }

            return result;
        }

        private static void AddNumber(ParameterSet parameterSet, string name, double value)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                parameterSet.Add(name, value);
            }
        }

        private static double Number(ParameterSet parameterSet, string name)
        {
            return parameterSet.Contains(name) ? parameterSet.ToDouble(name) : double.NaN;
        }

        private static Guid? ParseGuid(string value)
        {
            return Guid.TryParse(value, out Guid guid) ? guid : (Guid?)null;
        }
    }

    /// <summary>One pane of <see cref="GlazingBuilderProvenance"/>.</summary>
    public sealed class GlazingBuilderPaneRecord
    {
        /// <summary>1 = the outermost pane.</summary>
        public int Position { get; set; }

        /// <summary>The material name the saved system's layer uses (after any rename on save).</summary>
        public string Material { get; set; }

        /// <summary>The pane's name in its source.</summary>
        public string OriginalName { get; set; }

        public string DisplayName { get; set; }

        public string Category { get; set; }

        public string SourceLabel { get; set; }

        /// <summary>The source file's name only (no folder).</summary>
        public string SourceFile { get; set; }

        public bool Reversed { get; set; }

        /// <summary>[m]</summary>
        public double Thickness { get; set; }
    }

    /// <summary>One gap of <see cref="GlazingBuilderProvenance"/>.</summary>
    public sealed class GlazingBuilderGapRecord
    {
        /// <summary>1 = the outermost gap.</summary>
        public int Position { get; set; }

        public string Gas { get; set; }

        /// <summary>[m]</summary>
        public double Thickness { get; set; }

        /// <summary>[W/m²K], NaN when it could not be derived.</summary>
        public double HeatTransferCoefficient { get; set; } = double.NaN;

        /// <summary>The panel tilt the coefficient was derived for [deg].</summary>
        public double TiltDegrees { get; set; } = double.NaN;

        public string Material { get; set; }
    }
}
