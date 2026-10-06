// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One physical air handling unit's manufacturer-aware resolution, as the pairing record keeps it -
    /// PR5A (SAM#111 plan §I).
    ///
    /// <para><b>A verbatim carry, exactly like every other field on <see cref="PartOIteration3Record"/></b></para>
    /// <para>
    /// Every value here is <c>SAM.Analytical.Query.VentilationUnitOperatingParameters</c>'s own answer,
    /// copied - never recomputed, never a fallback. A refused quantity is recorded as refused, with its own
    /// sentence, never as a zero or as the other quantity's value. <see cref="DesignAirFlowRate_Lps"/> is
    /// the design duty the resolver was asked about - the lookup coordinate, never the equipment's
    /// capacity - so the frozen invariant stays visible in the record itself:
    /// </para>
    /// <code>
    /// PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow
    /// </code>
    /// </summary>
    public class PartOIteration3EquipmentEvidence
    {
        public PartOIteration3EquipmentEvidence(
            Guid guid_AirHandlingUnit,
            string name_AirHandlingUnit,
            string manufacturer,
            string model,
            string reference,
            string source,
            double designAirFlowRate_Lps,
            double? sensibleHeatRecoveryEfficiency,
            string heatRecoveryEfficiencyBasis,
            bool heatRecoveryClampedToDomain,
            string heatRecoveryRefusal,
            double? specificFanPower_WPerLps,
            string specificFanPowerBasis,
            bool fanPerformanceClampedToDomain,
            string fanPerformanceRefusal,
            double maximumSupplyFlowRate_Lps = double.NaN,
            double maximumExtractFlowRate_Lps = double.NaN,
            double designSupplyFlowRate_Lps = double.NaN,
            double designExtractFlowRate_Lps = double.NaN,
            double? partFRequiredSupplyFlowRate_Lps = null,
            double? partFRequiredExtractFlowRate_Lps = null,
            string operatingAirFlowBasis = null,
            double? supplyFanPressure_Pa = null,
            double? extractFanPressure_Pa = null,
            double? fanOverallEfficiency = null,
            double? supplyFanHeatGainFactor = null,
            double? extractFanHeatGainFactor = null,
            string fanPowerSplitRule = null,
            string fanHeatGainAssumption = null)
        {
            Guid_AirHandlingUnit = guid_AirHandlingUnit;
            Name_AirHandlingUnit = name_AirHandlingUnit;
            Manufacturer = manufacturer;
            Model = model;
            Reference = reference;
            Source = source;
            DesignAirFlowRate_Lps = designAirFlowRate_Lps;
            SensibleHeatRecoveryEfficiency = sensibleHeatRecoveryEfficiency;
            HeatRecoveryEfficiencyBasis = heatRecoveryEfficiencyBasis;
            HeatRecoveryClampedToDomain = heatRecoveryClampedToDomain;
            HeatRecoveryRefusal = heatRecoveryRefusal;
            SpecificFanPower_WPerLps = specificFanPower_WPerLps;
            SpecificFanPowerBasis = specificFanPowerBasis;
            FanPerformanceClampedToDomain = fanPerformanceClampedToDomain;
            FanPerformanceRefusal = fanPerformanceRefusal;
            MaximumSupplyFlowRate_Lps = maximumSupplyFlowRate_Lps;
            MaximumExtractFlowRate_Lps = maximumExtractFlowRate_Lps;
            DesignSupplyFlowRate_Lps = designSupplyFlowRate_Lps;
            DesignExtractFlowRate_Lps = designExtractFlowRate_Lps;
            PartFRequiredSupplyFlowRate_Lps = partFRequiredSupplyFlowRate_Lps;
            PartFRequiredExtractFlowRate_Lps = partFRequiredExtractFlowRate_Lps;
            OperatingAirFlowBasis = operatingAirFlowBasis;
            SupplyFanPressure_Pa = supplyFanPressure_Pa;
            ExtractFanPressure_Pa = extractFanPressure_Pa;
            FanOverallEfficiency = fanOverallEfficiency;
            SupplyFanHeatGainFactor = supplyFanHeatGainFactor;
            ExtractFanHeatGainFactor = extractFanHeatGainFactor;
            FanPowerSplitRule = fanPowerSplitRule;
            FanHeatGainAssumption = fanHeatGainAssumption;
        }

        public Guid Guid_AirHandlingUnit { get; }

        /// <summary>The materialised air system this analytical unit became. Empty until materialisation binds it.</summary>
        public Guid Guid_AirSystem { get; private set; }

        /// <summary>Display only. Never joined on.</summary>
        public string Name_AirHandlingUnit { get; }

        /// <summary>The selected product's identity - <c>AirHandlingUnitParameter.VentilationUnitReference</c>, carried verbatim.</summary>
        public string Manufacturer { get; }

        public string Model { get; }

        public string Reference { get; }

        /// <summary>The certified document the resolved figures were transcribed from.</summary>
        public string Source { get; }

        /// <summary>
        /// The balanced design airflow [l/s] the resolver was asked about - the mean of the unit's supply
        /// and extract design duty. A lookup coordinate, never a capacity and never an operating airflow.
        /// </summary>
        public double DesignAirFlowRate_Lps { get; }

        public double MaximumSupplyFlowRate_Lps { get; }

        public double MaximumExtractFlowRate_Lps { get; }

        public double DesignSupplyFlowRate_Lps { get; }

        public double DesignExtractFlowRate_Lps { get; }

        public double? PartFRequiredSupplyFlowRate_Lps { get; }

        public double? PartFRequiredExtractFlowRate_Lps { get; }

        /// <summary>How the operating airflow was obtained. PR5A states design airflow times the unchanged 1.0 schedule.</summary>
        public string OperatingAirFlowBasis { get; }

        /// <summary>The certified sensible heat recovery efficiency [-], or null where refused - see <see cref="HeatRecoveryRefusal"/>.</summary>
        public double? SensibleHeatRecoveryEfficiency { get; }

        public string HeatRecoveryEfficiencyBasis { get; }

        /// <summary>Whether the efficiency was held at the nearest published operating point.</summary>
        public bool HeatRecoveryClampedToDomain { get; }

        /// <summary>Why the efficiency could not be resolved, in words. Null where it was.</summary>
        public string HeatRecoveryRefusal { get; }

        /// <summary>The certified specific fan power [W/(l/s)], or null where refused - see <see cref="FanPerformanceRefusal"/>.</summary>
        public double? SpecificFanPower_WPerLps { get; }

        public string SpecificFanPowerBasis { get; }

        public bool FanPerformanceClampedToDomain { get; }

        /// <summary>Why the fan power could not be resolved, in words. Null where it was.</summary>
        public string FanPerformanceRefusal { get; }

        public double? SupplyFanPressure_Pa { get; }

        public double? ExtractFanPressure_Pa { get; }

        public double? FanOverallEfficiency { get; }

        public double? SupplyFanHeatGainFactor { get; }

        public double? ExtractFanHeatGainFactor { get; }

        /// <summary>The declared mapping from the certified whole-unit SFP to two generic fan settings.</summary>
        public string FanPowerSplitRule { get; }

        /// <summary>The declared fan-to-air heat assumption; never presented as a certified product value.</summary>
        public string FanHeatGainAssumption { get; }

        /// <summary>Whether both quantities resolved. A run in Selected-product mode refuses wherever this is false for any scoped unit.</summary>
        public bool IsResolved => SensibleHeatRecoveryEfficiency.HasValue
            && !double.IsNaN(SensibleHeatRecoveryEfficiency.Value)
            && !double.IsInfinity(SensibleHeatRecoveryEfficiency.Value)
            && SpecificFanPower_WPerLps.HasValue
            && !double.IsNaN(SpecificFanPower_WPerLps.Value)
            && !double.IsInfinity(SpecificFanPower_WPerLps.Value)
            && HeatRecoveryRefusal is null
            && FanPerformanceRefusal is null;

        /// <summary>Whether the persisted row carries the full identity, authority, mapping and lineage PR5A requires.</summary>
        public bool IsComplete => Guid_AirHandlingUnit != Guid.Empty
            && Guid_AirSystem != Guid.Empty
            && !string.IsNullOrWhiteSpace(Manufacturer)
            && !string.IsNullOrWhiteSpace(Model)
            && !string.IsNullOrWhiteSpace(Source)
            && IsFiniteNonNegative(MaximumSupplyFlowRate_Lps)
            && IsFiniteNonNegative(MaximumExtractFlowRate_Lps)
            && IsFinitePositive(DesignSupplyFlowRate_Lps)
            && IsFinitePositive(DesignExtractFlowRate_Lps)
            && IsFinitePositive(DesignAirFlowRate_Lps)
            && !string.IsNullOrWhiteSpace(OperatingAirFlowBasis)
            && IsStatedBasis(HeatRecoveryEfficiencyBasis)
            && IsStatedBasis(SpecificFanPowerBasis)
            && IsResolved
            && IsFiniteNonNegative(SupplyFanPressure_Pa)
            && IsFiniteNonNegative(ExtractFanPressure_Pa)
            && IsFinitePositive(FanOverallEfficiency)
            && IsFiniteNonNegative(SupplyFanHeatGainFactor)
            && IsFiniteNonNegative(ExtractFanHeatGainFactor)
            && !string.IsNullOrWhiteSpace(FanPowerSplitRule)
            && !string.IsNullOrWhiteSpace(FanHeatGainAssumption);

        /// <summary>
        /// Whether a certified figure states what it is a ratio of. A figure without a basis - or with the
        /// resolver's own "Undefined" - cannot be read, so a row carrying one is not complete.
        /// </summary>
        private static bool IsStatedBasis(string basis)
        {
            return !string.IsNullOrWhiteSpace(basis) && !string.Equals(basis, "Undefined", StringComparison.Ordinal);
        }

        /// <summary>Attaches the deterministic SAM_Systems lineage after materialisation.</summary>
        public bool BindAirSystem(Guid guid_AirSystem)
        {
            if (guid_AirSystem == Guid.Empty || (Guid_AirSystem != Guid.Empty && Guid_AirSystem != guid_AirSystem))
            {
                return false;
            }

            Guid_AirSystem = guid_AirSystem;

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new()
            {
                { "Guid_AirHandlingUnit", Guid_AirHandlingUnit.ToString() },
                { "Guid_AirSystem", Guid_AirSystem == Guid.Empty ? null : Guid_AirSystem.ToString() },
                { "Name_AirHandlingUnit", Name_AirHandlingUnit },
                { "Manufacturer", Manufacturer },
                { "Model", Model },
                { "Reference", Reference },
                { "Source", Source },
                { "DesignAirFlowRate_Lps", DesignAirFlowRate_Lps },
                { "OperatingAirFlowBasis", OperatingAirFlowBasis },
                { "HeatRecoveryEfficiencyBasis", HeatRecoveryEfficiencyBasis },
                { "HeatRecoveryClampedToDomain", HeatRecoveryClampedToDomain },
                { "SpecificFanPowerBasis", SpecificFanPowerBasis },
                { "FanPerformanceClampedToDomain", FanPerformanceClampedToDomain },
                { "FanPowerSplitRule", FanPowerSplitRule },
                { "FanHeatGainAssumption", FanHeatGainAssumption },
            };

            Add(result, "MaximumSupplyFlowRate_Lps", MaximumSupplyFlowRate_Lps);
            Add(result, "MaximumExtractFlowRate_Lps", MaximumExtractFlowRate_Lps);
            Add(result, "DesignSupplyFlowRate_Lps", DesignSupplyFlowRate_Lps);
            Add(result, "DesignExtractFlowRate_Lps", DesignExtractFlowRate_Lps);

            Add(result, "PartFRequiredSupplyFlowRate_Lps", PartFRequiredSupplyFlowRate_Lps);
            Add(result, "PartFRequiredExtractFlowRate_Lps", PartFRequiredExtractFlowRate_Lps);

            Add(result, "SupplyFanPressure_Pa", SupplyFanPressure_Pa);
            Add(result, "ExtractFanPressure_Pa", ExtractFanPressure_Pa);
            Add(result, "FanOverallEfficiency", FanOverallEfficiency);
            Add(result, "SupplyFanHeatGainFactor", SupplyFanHeatGainFactor);
            Add(result, "ExtractFanHeatGainFactor", ExtractFanHeatGainFactor);

            Add(result, "SensibleHeatRecoveryEfficiency", SensibleHeatRecoveryEfficiency);

            if (HeatRecoveryRefusal is not null)
            {
                result.Add("HeatRecoveryRefusal", HeatRecoveryRefusal);
            }

            Add(result, "SpecificFanPower_WPerLps", SpecificFanPower_WPerLps);

            if (FanPerformanceRefusal is not null)
            {
                result.Add("FanPerformanceRefusal", FanPerformanceRefusal);
            }

            return result;
        }

        public static PartOIteration3EquipmentEvidence FromJsonObject(JsonObject jsonObject)
        {
            return jsonObject is null
                ? null
                : Read(jsonObject);
        }

        private static PartOIteration3EquipmentEvidence Read(JsonObject jsonObject)
        {
            PartOIteration3EquipmentEvidence result = new(
                    PartOIteration3Json.Guid(jsonObject, "Guid_AirHandlingUnit"),
                    PartOIteration3Json.Text(jsonObject, "Name_AirHandlingUnit"),
                    PartOIteration3Json.Text(jsonObject, "Manufacturer"),
                    PartOIteration3Json.Text(jsonObject, "Model"),
                    PartOIteration3Json.Text(jsonObject, "Reference"),
                    PartOIteration3Json.Text(jsonObject, "Source"),
                    PartOIteration3Json.NullableNumber(jsonObject, "DesignAirFlowRate_Lps") ?? double.NaN,
                    PartOIteration3Json.NullableNumber(jsonObject, "SensibleHeatRecoveryEfficiency"),
                    PartOIteration3Json.Text(jsonObject, "HeatRecoveryEfficiencyBasis"),
                    PartOIteration3Json.Boolean(jsonObject, "HeatRecoveryClampedToDomain", false),
                    PartOIteration3Json.Text(jsonObject, "HeatRecoveryRefusal"),
                    PartOIteration3Json.NullableNumber(jsonObject, "SpecificFanPower_WPerLps"),
                    PartOIteration3Json.Text(jsonObject, "SpecificFanPowerBasis"),
                    PartOIteration3Json.Boolean(jsonObject, "FanPerformanceClampedToDomain", false),
                    PartOIteration3Json.Text(jsonObject, "FanPerformanceRefusal"),
                    PartOIteration3Json.NullableNumber(jsonObject, "MaximumSupplyFlowRate_Lps") ?? double.NaN,
                    PartOIteration3Json.NullableNumber(jsonObject, "MaximumExtractFlowRate_Lps") ?? double.NaN,
                    PartOIteration3Json.NullableNumber(jsonObject, "DesignSupplyFlowRate_Lps") ?? double.NaN,
                    PartOIteration3Json.NullableNumber(jsonObject, "DesignExtractFlowRate_Lps") ?? double.NaN,
                    PartOIteration3Json.NullableNumber(jsonObject, "PartFRequiredSupplyFlowRate_Lps"),
                    PartOIteration3Json.NullableNumber(jsonObject, "PartFRequiredExtractFlowRate_Lps"),
                    PartOIteration3Json.Text(jsonObject, "OperatingAirFlowBasis"),
                    PartOIteration3Json.NullableNumber(jsonObject, "SupplyFanPressure_Pa"),
                    PartOIteration3Json.NullableNumber(jsonObject, "ExtractFanPressure_Pa"),
                    PartOIteration3Json.NullableNumber(jsonObject, "FanOverallEfficiency"),
                    PartOIteration3Json.NullableNumber(jsonObject, "SupplyFanHeatGainFactor"),
                    PartOIteration3Json.NullableNumber(jsonObject, "ExtractFanHeatGainFactor"),
                    PartOIteration3Json.Text(jsonObject, "FanPowerSplitRule"),
                    PartOIteration3Json.Text(jsonObject, "FanHeatGainAssumption"));

            Guid guid_AirSystem = PartOIteration3Json.Guid(jsonObject, "Guid_AirSystem");
            if (guid_AirSystem != Guid.Empty)
            {
                result.BindAirSystem(guid_AirSystem);
            }

            return result;
        }

        private static void Add(JsonObject jsonObject, string name, double? value)
        {
            if (value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value))
            {
                jsonObject.Add(name, value.Value);
            }
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
        }

        private static bool IsFiniteNonNegative(double? value)
        {
            return value.HasValue && IsFiniteNonNegative(value.Value);
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
        }

        private static bool IsFinitePositive(double? value)
        {
            return value.HasValue && IsFinitePositive(value.Value);
        }

        public override string ToString()
        {
            string heatRecovery = SensibleHeatRecoveryEfficiency.HasValue
                ? string.Format("HR {0:0.###} ({1}{2})", SensibleHeatRecoveryEfficiency.Value, HeatRecoveryEfficiencyBasis, HeatRecoveryClampedToDomain ? ", clamped" : string.Empty)
                : string.Format("HR refused: {0}", HeatRecoveryRefusal);

            string fanPerformance = SpecificFanPower_WPerLps.HasValue
                ? string.Format("SFP {0:0.###} W/(l/s) ({1}{2})", SpecificFanPower_WPerLps.Value, SpecificFanPowerBasis, FanPerformanceClampedToDomain ? ", clamped" : string.Empty)
                : string.Format("SFP refused: {0}", FanPerformanceRefusal);

            return string.Format(
                "{0} ({1}): {2} / {3} / {4} at {5:0.###} l/s - {6}; {7}",
                Name_AirHandlingUnit ?? "?",
                Guid_AirHandlingUnit,
                Manufacturer ?? "-",
                Model ?? "-",
                Reference ?? "-",
                DesignAirFlowRate_Lps,
                heatRecovery,
                fanPerformance);
        }
    }
}
