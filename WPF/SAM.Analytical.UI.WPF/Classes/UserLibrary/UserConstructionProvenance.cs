// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Where a construction saved to "My constructions" came from: one named <see cref="ParameterSet"/> ("SAM User Construction", fixed Guid) on
    /// the <see cref="Construction"/>. It travels with the construction into any model it is applied to, so it holds labels and FILE NAMES only,
    /// never a machine path (the origin model is recorded by name, the added source by file name). One set per object (SAM merges sets of the same
    /// name, later values winning - SAM#146/PR2A-0); a saved construction is immutable, so its set is written once, at the Save.
    /// </summary>
    public sealed class UserConstructionProvenance
    {
        public const string ParameterSetName = "SAM User Construction";

        public static readonly Guid ParameterSetGuid = new Guid("9c2b6d3e-4f1a-4a7e-b3d8-2e5f7a1c9b60");

        /// <summary>The first schema. A reader takes a missing key as "none", so a later schema can add keys without breaking this one.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>What calculated the U-value recorded at the Save.</summary>
        public const string TasEngine = "Tas TCD (SAM_Tas ThermalTransmittanceCalculator)";

        // A value that may be NaN is simply not written (the reader takes a missing key as "not recorded").
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public DateTime CreatedUtc { get; set; }

        public UserConstructionOrigin SavedFrom { get; set; } = UserConstructionOrigin.Model;

        /// <summary>For <see cref="UserConstructionOrigin.AddedSource"/> (and "My constructions"): the source's file name only (no folder).</summary>
        public string SavedFromSource { get; set; }

        /// <summary>The name of the construction this one was made from (for a generated variant: the construction it adjusts).</summary>
        public string BasedOnName { get; set; }

        public Guid? BasedOnGuid { get; set; }

        /// <summary>The name of the model it was saved from. A NAME only - never a path.</summary>
        public string OriginModelName { get; set; }

        /// <summary>U-value [W/m²K] when it was saved; NaN when none was known.</summary>
        public double ThermalTransmittance { get; set; } = double.NaN;

        /// <summary>The U-value the generated variant was solved for [W/m²K]; NaN for anything else.</summary>
        public double TargetThermalTransmittance { get; set; } = double.NaN;

        /// <summary>The heat-flow direction the U-value is for ("Horizontal", "Up", "Down"); null when none was known.</summary>
        public string HeatFlowDirection { get; set; }

        /// <summary>The heat-flow basis in words, e.g. "Horizontal heat flow, external wall (from the panels)".</summary>
        public string HeatFlowBasis { get; set; }

        /// <summary>What calculated <see cref="ThermalTransmittance"/>, e.g. <see cref="TasEngine"/>.</summary>
        public string Engine { get; set; }

        /// <summary>How it was obtained, e.g. "Layer thickness solved for the target U (Tas)", "U-value of the construction as it is (Tas)".</summary>
        public string Route { get; set; }

        public string SamTasVersion { get; set; }

        /// <summary>A copy (every member is a value or an immutable string), so a Save never changes the provenance it was given.</summary>
        internal UserConstructionProvenance Clone()
        {
            return (UserConstructionProvenance)MemberwiseClone();
        }

        public ParameterSet ToParameterSet()
        {
            ParameterSet result = new ParameterSet(ParameterSetGuid, ParameterSetName);
            result.Add("Schema Version", SchemaVersion);
            result.Add("Created UTC", CreatedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            result.Add("Saved From", SavedFrom.ToString());
            AddText(result, "Saved From Source", DraftPane.FileNameOnly(SavedFromSource));
            AddText(result, "Based On Name", BasedOnName);
            if (BasedOnGuid.HasValue)
            {
                result.Add("Based On Guid", BasedOnGuid.Value.ToString());
            }

            AddText(result, "Origin Model Name", DraftPane.FileNameOnly(OriginModelName));
            AddNumber(result, "U-value At Save [W/m2K]", ThermalTransmittance);
            AddNumber(result, "Target U-value [W/m2K]", TargetThermalTransmittance);
            AddText(result, "Heat Flow Direction", HeatFlowDirection);
            AddText(result, "Heat Flow Basis", HeatFlowBasis);
            AddText(result, "Engine", Engine);
            AddText(result, "Route", Route);
            AddText(result, "SAM_Tas Version", SamTasVersion);
            return result;
        }

        /// <summary>The provenance of a construction saved to "My constructions"; null for any other construction.</summary>
        public static UserConstructionProvenance FromConstruction(Construction construction)
        {
            ParameterSet parameterSet = construction?.GetParameterSet(ParameterSetName);
            return parameterSet == null ? null : FromParameterSet(parameterSet);
        }

        public static UserConstructionProvenance FromParameterSet(ParameterSet parameterSet)
        {
            if (parameterSet == null)
            {
                return null;
            }

            UserConstructionProvenance result = new UserConstructionProvenance()
            {
                SchemaVersion = parameterSet.Contains("Schema Version") ? parameterSet.ToInt("Schema Version") : 0,
                SavedFromSource = parameterSet.ToString("Saved From Source"),
                BasedOnName = parameterSet.ToString("Based On Name"),
                BasedOnGuid = Guid.TryParse(parameterSet.ToString("Based On Guid"), out Guid guid) ? guid : (Guid?)null,
                OriginModelName = parameterSet.ToString("Origin Model Name"),
                ThermalTransmittance = Number(parameterSet, "U-value At Save [W/m2K]"),
                TargetThermalTransmittance = Number(parameterSet, "Target U-value [W/m2K]"),
                HeatFlowDirection = parameterSet.ToString("Heat Flow Direction"),
                HeatFlowBasis = parameterSet.ToString("Heat Flow Basis"),
                Engine = parameterSet.ToString("Engine"),
                Route = parameterSet.ToString("Route"),
                SamTasVersion = parameterSet.ToString("SAM_Tas Version"),
            };

            if (Enum.TryParse(parameterSet.ToString("Saved From"), out UserConstructionOrigin origin))
            {
                result.SavedFrom = origin;
            }

            if (DateTime.TryParse(parameterSet.ToString("Created UTC"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime created))
            {
                result.CreatedUtc = created.ToUniversalTime();
            }

            return result;
        }

        /// <summary>The lines of the details pane and the report: where it was saved from and how its U-value was obtained.</summary>
        public IReadOnlyList<string> Lines()
        {
            List<string> lines = new List<string>
            {
                "Saved from: " + SavedFromText,
            };

            if (!string.IsNullOrWhiteSpace(BasedOnName))
            {
                lines.Add("Based on: " + BasedOnName + (BasedOnGuid.HasValue ? " [" + BasedOnGuid.Value.ToString().Substring(30) + "]" : string.Empty));
            }

            if (!string.IsNullOrWhiteSpace(OriginModelName))
            {
                lines.Add("Model: " + OriginModelName);
            }

            lines.Add("U-value at save: " + (double.IsNaN(ThermalTransmittance) ? "not recorded" : ThermalTransmittance.ToString("0.000", CultureInfo.CurrentCulture) + " W/m²K"));
            if (!double.IsNaN(TargetThermalTransmittance))
            {
                lines.Add("Target: " + TargetThermalTransmittance.ToString("0.000", CultureInfo.CurrentCulture) + " W/m²K");
            }

            if (!string.IsNullOrWhiteSpace(HeatFlowBasis))
            {
                lines.Add("Heat-flow basis: " + HeatFlowBasis);
            }

            if (!string.IsNullOrWhiteSpace(Route))
            {
                lines.Add("Route: " + Route);
            }

            if (!string.IsNullOrWhiteSpace(Engine))
            {
                lines.Add("Engine: " + Engine + (string.IsNullOrWhiteSpace(SamTasVersion) ? string.Empty : " " + SamTasVersion));
            }

            return lines;
        }

        /// <summary>"Generated variant", "Model", "Default library", "added source Constructions.tcd", "My constructions".</summary>
        public string SavedFromText
        {
            get
            {
                switch (SavedFrom)
                {
                    case UserConstructionOrigin.GeneratedVariant:
                        return "generated variant";

                    case UserConstructionOrigin.DefaultLibrary:
                        return "default library";

                    case UserConstructionOrigin.AddedSource:
                        return string.IsNullOrWhiteSpace(SavedFromSource) ? "added source" : "added source " + SavedFromSource;

                    case UserConstructionOrigin.MyConstructions:
                        return "My constructions";

                    case UserConstructionOrigin.ConstructionEditor:
                        return "Constructions editor";

                    default:
                        return "model";
                }
            }
        }

        private static void AddText(ParameterSet parameterSet, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parameterSet.Add(name, value);
            }
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
    }
}
