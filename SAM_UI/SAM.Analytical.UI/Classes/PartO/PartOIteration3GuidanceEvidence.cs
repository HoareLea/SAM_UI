// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One unit's manufacturer operating guidance as an Iteration 3 run resolved it (SAM#123) - the same
    /// numbers the run's resolution note states, kept as fields so a reader can be shown them as a table
    /// rather than as one long sentence.
    /// <para>
    /// <b>A presentation record, not an authority.</b> Every value is copied from the catalogue's resolved
    /// strategy and the unit's design duty at resolution time; nothing here is re-derived, and the run's
    /// behaviour does not read it. The note text in <see cref="PartOIteration3Record.Notes_Scope"/> is unchanged
    /// and remains the complete statement.
    /// </para>
    /// <para>
    /// <b>Additive.</b> Absent on every record written before it existed, which reads as none - a reader then
    /// falls back to the note text.
    /// </para>
    /// <para>
    /// <b>Manufacturer guidance, provisional, not certified performance.</b>
    /// </para>
    /// </summary>
    public class PartOIteration3GuidanceEvidence
    {
        public PartOIteration3GuidanceEvidence(
            Guid guid_AirHandlingUnit,
            string name_AirHandlingUnit,
            string reference,
            string coolingModuleModel,
            double designSupplyFlowRate_Lps,
            double designExtractFlowRate_Lps,
            double maximumSupplyFlowRate_Lps,
            double maximumExtractFlowRate_Lps,
            double elevatedAirFlow_Lps,
            double minimumElevatedAirFlow_Lps,
            double maximumElevatedAirFlow_Lps,
            string coolingActivation,
            double coolingActivationTemperature_C,
            string supplyTemperatureRule,
            double exchangerExtractFraction,
            double coilNetTemperatureDrop_K,
            double minimumSupplyTemperature_C)
        {
            Guid_AirHandlingUnit = guid_AirHandlingUnit;
            Name_AirHandlingUnit = name_AirHandlingUnit;
            Reference = reference;
            CoolingModuleModel = coolingModuleModel;
            DesignSupplyFlowRate_Lps = designSupplyFlowRate_Lps;
            DesignExtractFlowRate_Lps = designExtractFlowRate_Lps;
            MaximumSupplyFlowRate_Lps = maximumSupplyFlowRate_Lps;
            MaximumExtractFlowRate_Lps = maximumExtractFlowRate_Lps;
            ElevatedAirFlow_Lps = elevatedAirFlow_Lps;
            MinimumElevatedAirFlow_Lps = minimumElevatedAirFlow_Lps;
            MaximumElevatedAirFlow_Lps = maximumElevatedAirFlow_Lps;
            CoolingActivation = coolingActivation;
            CoolingActivationTemperature_C = coolingActivationTemperature_C;
            SupplyTemperatureRule = supplyTemperatureRule;
            ExchangerExtractFraction = exchangerExtractFraction;
            CoilNetTemperatureDrop_K = coilNetTemperatureDrop_K;
            MinimumSupplyTemperature_C = minimumSupplyTemperature_C;
        }

        public Guid Guid_AirHandlingUnit { get; }

        /// <summary>The explicitly selected cooling-stat room; empty on older records.</summary>
        public Guid Guid_CoolingStatSpace { get; set; }

        public string Name_AirHandlingUnit { get; }

        /// <summary>The selected product, as its catalogue reference.</summary>
        public string Reference { get; }

        /// <summary>The cooling module the product names, or null.</summary>
        public string CoolingModuleModel { get; }

        /// <summary>The dwelling's design (Part F requirement carried as design) supply [l/s].</summary>
        public double DesignSupplyFlowRate_Lps { get; }

        public double DesignExtractFlowRate_Lps { get; }

        /// <summary>The selected equipment's capacity - a ceiling, never a design airflow [l/s].</summary>
        public double MaximumSupplyFlowRate_Lps { get; }

        public double MaximumExtractFlowRate_Lps { get; }

        /// <summary>The operating airflow while cooling, each side [l/s].</summary>
        public double ElevatedAirFlow_Lps { get; }

        public double MinimumElevatedAirFlow_Lps { get; }

        public double MaximumElevatedAirFlow_Lps { get; }

        /// <summary>What switches cooling on, in words (the catalogue's own description).</summary>
        public string CoolingActivation { get; }

        public double CoolingActivationTemperature_C { get; }

        /// <summary>The supply-temperature rule while cooling, in the rule's own words.</summary>
        public string SupplyTemperatureRule { get; }

        /// <summary>The exchanger's recovery fraction at the operating airflow, unless bypassed.</summary>
        public double ExchangerExtractFraction { get; }

        /// <summary>What the coil takes off the air at the operating airflow [K].</summary>
        public double CoilNetTemperatureDrop_K { get; }

        /// <summary>The lowest supply the coil delivers [&#176;C], or NaN where none is stated.</summary>
        public double MinimumSupplyTemperature_C { get; }

        public JsonObject ToJsonObject()
        {
            return new JsonObject
            {
                { "Guid_AirHandlingUnit", Guid_AirHandlingUnit.ToString() },
                { "Guid_CoolingStatSpace", Guid_CoolingStatSpace.ToString() },
                { "Name_AirHandlingUnit", Name_AirHandlingUnit },
                { "Reference", Reference },
                { "CoolingModuleModel", CoolingModuleModel },
                { "DesignSupplyFlowRate_Lps", Number(DesignSupplyFlowRate_Lps) },
                { "DesignExtractFlowRate_Lps", Number(DesignExtractFlowRate_Lps) },
                { "MaximumSupplyFlowRate_Lps", Number(MaximumSupplyFlowRate_Lps) },
                { "MaximumExtractFlowRate_Lps", Number(MaximumExtractFlowRate_Lps) },
                { "ElevatedAirFlow_Lps", Number(ElevatedAirFlow_Lps) },
                { "MinimumElevatedAirFlow_Lps", Number(MinimumElevatedAirFlow_Lps) },
                { "MaximumElevatedAirFlow_Lps", Number(MaximumElevatedAirFlow_Lps) },
                { "CoolingActivation", CoolingActivation },
                { "CoolingActivationTemperature_C", Number(CoolingActivationTemperature_C) },
                { "SupplyTemperatureRule", SupplyTemperatureRule },
                { "ExchangerExtractFraction", Number(ExchangerExtractFraction) },
                { "CoilNetTemperatureDrop_K", Number(CoilNetTemperatureDrop_K) },
                { "MinimumSupplyTemperature_C", Number(MinimumSupplyTemperature_C) },
            };
        }

        public static PartOIteration3GuidanceEvidence FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return null;
            }

            return new PartOIteration3GuidanceEvidence(
                PartOIteration3Json.Guid(jsonObject, "Guid_AirHandlingUnit"),
                PartOIteration3Json.Text(jsonObject, "Name_AirHandlingUnit"),
                PartOIteration3Json.Text(jsonObject, "Reference"),
                PartOIteration3Json.Text(jsonObject, "CoolingModuleModel"),
                PartOIteration3Json.Number(jsonObject, "DesignSupplyFlowRate_Lps"),
                PartOIteration3Json.Number(jsonObject, "DesignExtractFlowRate_Lps"),
                PartOIteration3Json.Number(jsonObject, "MaximumSupplyFlowRate_Lps"),
                PartOIteration3Json.Number(jsonObject, "MaximumExtractFlowRate_Lps"),
                PartOIteration3Json.Number(jsonObject, "ElevatedAirFlow_Lps"),
                PartOIteration3Json.Number(jsonObject, "MinimumElevatedAirFlow_Lps"),
                PartOIteration3Json.Number(jsonObject, "MaximumElevatedAirFlow_Lps"),
                PartOIteration3Json.Text(jsonObject, "CoolingActivation"),
                PartOIteration3Json.Number(jsonObject, "CoolingActivationTemperature_C"),
                PartOIteration3Json.Text(jsonObject, "SupplyTemperatureRule"),
                PartOIteration3Json.Number(jsonObject, "ExchangerExtractFraction"),
                PartOIteration3Json.Number(jsonObject, "CoilNetTemperatureDrop_K"),
                PartOIteration3Json.Number(jsonObject, "MinimumSupplyTemperature_C"))
            {
                Guid_CoolingStatSpace = PartOIteration3Json.Guid(jsonObject, "Guid_CoolingStatSpace"),
            };
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}{2}", Name_AirHandlingUnit, Reference, string.IsNullOrWhiteSpace(CoolingModuleModel) ? string.Empty : " + " + CoolingModuleModel);
        }

        private static JsonNode Number(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? null : JsonValue.Create(value);
        }
    }
}
