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
    /// One pane in the Glazing System Builder's pane browser: a light projection of a <see cref="TransparentMaterial"/> of a pane source (an IGDB
    /// file, the default library, the model's materials) with the thermal and optical values a person needs to choose a pane. The material itself
    /// is kept so choosing the entry hands the original to the draft (<see cref="DraftPane"/> keeps its own copy). The source is known by label
    /// and file NAME only - never a path.
    /// </summary>
    public sealed class GlazingPaneEntry
    {
        private readonly string searchText;

        internal GlazingPaneEntry(TransparentMaterial material, string sourceLabel, string sourceFileName)
        {
            Material = material ?? throw new ArgumentNullException(nameof(material));
            SourceLabel = sourceLabel ?? string.Empty;
            SourceFileName = sourceFileName;

            Name = material.Name ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(material.DisplayName) ? Name : material.DisplayName;
            Category = material.TryGetValue(ParameterizedSAMObjectParameter.Category, out Category category) && category != null ? category.ToString("\\") ?? string.Empty : string.Empty;

            ThicknessMillimetres = material.TryGetValue(Core.MaterialParameter.DefaultThickness, out double thickness) && thickness > 0 ? thickness * 1000 : double.NaN;
            SolarTransmittance = Get(material, TransparentMaterialParameter.SolarTransmittance);
            LightTransmittance = Get(material, TransparentMaterialParameter.LightTransmittance);
            ExternalEmissivity = Get(material, TransparentMaterialParameter.ExternalEmissivity);
            InternalEmissivity = Get(material, TransparentMaterialParameter.InternalEmissivity);
            ExternalSolarReflectance = Get(material, TransparentMaterialParameter.ExternalSolarReflectance);
            InternalSolarReflectance = Get(material, TransparentMaterialParameter.InternalSolarReflectance);
            ThermalConductivity = material.ThermalConductivity;

            searchText = string.Join("\n", new[] { Name, DisplayName, Category }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();
        }

        /// <summary>The pane material as its source has it (do not change it).</summary>
        public TransparentMaterial Material { get; }

        public string Name { get; }

        public string DisplayName { get; }

        /// <summary>The material's category (for IGDB: the manufacturer / product family folder), or empty.</summary>
        public string Category { get; }

        public string SourceLabel { get; }

        public string SourceFileName { get; }

        /// <summary>Thickness [mm]; NaN when the material has none.</summary>
        public double ThicknessMillimetres { get; }

        public double SolarTransmittance { get; }

        public double LightTransmittance { get; }

        /// <summary>Emissivity of the face towards the OUTSIDE of the built-up system (a coating there shows as a low value).</summary>
        public double ExternalEmissivity { get; }

        public double InternalEmissivity { get; }

        public double ExternalSolarReflectance { get; }

        public double InternalSolarReflectance { get; }

        /// <summary>Thermal conductivity [W/mK].</summary>
        public double ThermalConductivity { get; }

        public string ThicknessText => double.IsNaN(ThicknessMillimetres) ? "–" : ThicknessMillimetres.ToString("0.#", CultureInfo.CurrentCulture);

        public string SolarTransmittanceText => Format(SolarTransmittance, "0.00");

        public string LightTransmittanceText => Format(LightTransmittance, "0.00");

        /// <summary>"0.025 / 0.84": the outside-facing and the inside-facing emissivity; a low-e coating is the small one.</summary>
        public string EmissivityText => Format(ExternalEmissivity, "0.###") + " / " + Format(InternalEmissivity, "0.###");

        /// <summary>The text a screen reader and type-ahead use for the row.</summary>
        public override string ToString() => DisplayName;

        public string Tooltip => string.Join(Environment.NewLine, new[]
        {
            DisplayName == Name ? Name : DisplayName + "  (" + Name + ")",
            string.IsNullOrWhiteSpace(Category) ? null : Category,
            string.Format(CultureInfo.CurrentCulture, "Thickness {0} mm · conductivity {1} W/mK", ThicknessText, Format(ThermalConductivity, "0.###")),
            string.Format(CultureInfo.CurrentCulture, "Solar transmittance {0} · light transmittance {1}", SolarTransmittanceText, LightTransmittanceText),
            string.Format(CultureInfo.CurrentCulture, "Emissivity outside-facing / inside-facing {0}", EmissivityText),
            string.Format(CultureInfo.CurrentCulture, "Solar reflectance outside-facing / inside-facing {0} / {1}", Format(ExternalSolarReflectance, "0.00"), Format(InternalSolarReflectance, "0.00")),
            "From " + (string.IsNullOrWhiteSpace(SourceLabel) ? "–" : SourceLabel),
        }.Where(x => x != null));

        /// <summary>True when every word of <paramref name="words"/> (already lower case) is in the name, display name or category.</summary>
        internal bool Matches(IReadOnlyList<string> words)
        {
            foreach (string word in words)
            {
                if (searchText.IndexOf(word, StringComparison.Ordinal) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The draft's pane for this entry (the draft keeps its own copy of the material).</summary>
        internal DraftPane ToDraftPane()
        {
            return new DraftPane(Material, double.NaN, SourceLabel, SourceFileName);
        }

        private static double Get(TransparentMaterial material, TransparentMaterialParameter parameter)
        {
            return material.TryGetValue(parameter, out double value) ? value : double.NaN;
        }

        private static string Format(double value, string format)
        {
            return double.IsNaN(value) ? "–" : value.ToString(format, CultureInfo.CurrentCulture);
        }
    }
}
