// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One physical unit's cooling module in a PR5B (B4) pairing - SAM#111: what was resolved from the
    /// selected product, what it was materialised as, and what it did over the year.
    /// <para>
    /// <b>Four airflows, four fields, never one another.</b> <see cref="DesignSupplyFlowRate_Lps"/> /
    /// <see cref="DesignExtractFlowRate_Lps"/> are the unit's ventilation DesignAirFlow;
    /// <see cref="MaximumSupplyFlowRate_Lps"/> / <see cref="MaximumExtractFlowRate_Lps"/> its selected
    /// equipment capacity; <see cref="MaximumOperatingAirFlow_Lps"/> the validated ceiling of the cooling
    /// table's airflow axis; and <see cref="OperatingAirFlowMinimum_Lps"/>..<see cref="OperatingAirFlowMaximum_Lps"/>
    /// the recirculation the cooling loop actually carried. None is written from another.
    /// </para>
    /// <para>
    /// <b>What is not claimed.</b> The module is an aggregate thermal surrogate; <see cref="Cooling_kWh"/> is
    /// the air-side sensible duty implied by TAS's coil inlet and outlet. No electrical consumption,
    /// efficiency ratio, latent split or refrigerant behaviour is recorded, because none is evidenced.
    /// </para>
    /// </summary>
    public class PartOIteration3CoolingEvidence
    {
        public PartOIteration3CoolingEvidence(
            Guid guid_AirHandlingUnit,
            string name_AirHandlingUnit,
            string manufacturer,
            string model,
            string reference,
            string coolingModuleModel,
            string source,
            double maximumSupplyFlowRate_Lps,
            double maximumExtractFlowRate_Lps,
            double designSupplyFlowRate_Lps,
            double designExtractFlowRate_Lps,
            string table,
            string sha256_Table,
            double maximumOperatingAirFlow_Lps,
            double minimumOperatingAirFlow_Lps,
            double controlTemperature_Low_C,
            double controlTemperature_High_C,
            double flowFraction_Low,
            double flowFraction_High,
            double coolingEnableTemperature_C,
            string declaredRules)
        {
            Guid_AirHandlingUnit = guid_AirHandlingUnit;
            Name_AirHandlingUnit = name_AirHandlingUnit;
            Manufacturer = manufacturer;
            Model = model;
            Reference = reference;
            CoolingModuleModel = coolingModuleModel;
            Source = source;
            MaximumSupplyFlowRate_Lps = maximumSupplyFlowRate_Lps;
            MaximumExtractFlowRate_Lps = maximumExtractFlowRate_Lps;
            DesignSupplyFlowRate_Lps = designSupplyFlowRate_Lps;
            DesignExtractFlowRate_Lps = designExtractFlowRate_Lps;
            Table = table;
            Sha256_Table = sha256_Table;
            MaximumOperatingAirFlow_Lps = maximumOperatingAirFlow_Lps;
            MinimumOperatingAirFlow_Lps = minimumOperatingAirFlow_Lps;
            ControlTemperature_Low_C = controlTemperature_Low_C;
            ControlTemperature_High_C = controlTemperature_High_C;
            FlowFraction_Low = flowFraction_Low;
            FlowFraction_High = flowFraction_High;
            CoolingEnableTemperature_C = coolingEnableTemperature_C;
            DeclaredRules = declaredRules;
        }

        //------------------------------------------------------------------------------------ resolved

        public Guid Guid_AirHandlingUnit { get; }

        /// <summary>The unit's own materialised air system - the one the branch sits inside. Set once bound.</summary>
        public Guid Guid_AirSystem { get; private set; }

        public string Name_AirHandlingUnit { get; }

        public string Manufacturer { get; }

        public string Model { get; }

        public string Reference { get; }

        public string CoolingModuleModel { get; }

        public string Source { get; }

        /// <summary>SelectedEquipmentCapacity, supply side [l/s].</summary>
        public double MaximumSupplyFlowRate_Lps { get; }

        /// <summary>SelectedEquipmentCapacity, extract side [l/s].</summary>
        public double MaximumExtractFlowRate_Lps { get; }

        /// <summary>The unit's ventilation DesignAirFlow, supply side [l/s] - unchanged by the cooling module.</summary>
        public double DesignSupplyFlowRate_Lps { get; }

        /// <summary>The unit's ventilation DesignAirFlow, extract side [l/s] - unchanged by the cooling module.</summary>
        public double DesignExtractFlowRate_Lps { get; }

        /// <summary>The published table's axes, output and cell count, in words.</summary>
        public string Table { get; }

        /// <summary>SHA-256 of the published table as the catalogue states it, so the exact numbers are traceable.</summary>
        public string Sha256_Table { get; }

        /// <summary>The validated cooling-table airflow ceiling [l/s] - the table's airflow axis, never the capacity.</summary>
        public double MaximumOperatingAirFlow_Lps { get; }

        /// <summary>The recirculation at the law's lower point [l/s].</summary>
        public double MinimumOperatingAirFlow_Lps { get; }

        public double ControlTemperature_Low_C { get; }

        public double ControlTemperature_High_C { get; }

        public double FlowFraction_Low { get; }

        public double FlowFraction_High { get; }

        /// <summary>The declared cooling-enable temperature [degC] - the law's lower control temperature.</summary>
        public double CoolingEnableTemperature_C { get; }

        /// <summary>Every declared (not manufacturer) mapping rule, in words.</summary>
        public string DeclaredRules { get; }

        //------------------------------------------------------------------------------------ outcome

        /// <summary>Whether the route's evidence for this unit has been recorded.</summary>
        public bool HasOutcome { get; private set; }

        public int Count_Hours { get; private set; }

        public int Count_Cooling { get; private set; }

        public int Count_Heating { get; private set; }

        public int Count_BelowGate { get; private set; }

        public int Count_GateViolation { get; private set; }

        public int Count_OutOfRange { get; private set; }

        public int Count_OffLaw { get; private set; }

        public int Count_InPublishedDomain { get; private set; }

        public double OperatingAirFlowMinimum_Lps { get; private set; } = double.NaN;

        public double OperatingAirFlowMean_Lps { get; private set; } = double.NaN;

        public double OperatingAirFlowMaximum_Lps { get; private set; } = double.NaN;

        public double Cooling_kWh { get; private set; } = double.NaN;

        public double MaximumTableError_K { get; private set; } = double.NaN;

        public double MaximumCanonicalDeviation_Lps { get; private set; } = double.NaN;

        /// <summary>Binds the row to the unit's materialised air system. Once.</summary>
        public bool Bind(Guid guid_AirSystem)
        {
            if (guid_AirSystem == Guid.Empty || (Guid_AirSystem != Guid.Empty && Guid_AirSystem != guid_AirSystem))
            {
                return false;
            }

            Guid_AirSystem = guid_AirSystem;
            return true;
        }

        /// <summary>Records what the unit's branch did, verbatim from the route's own checked evidence.</summary>
        public void RecordOutcome(
            int count_Hours,
            int count_Cooling,
            int count_Heating,
            int count_BelowGate,
            int count_GateViolation,
            int count_OutOfRange,
            int count_OffLaw,
            int count_InPublishedDomain,
            double operatingAirFlowMinimum_Lps,
            double operatingAirFlowMean_Lps,
            double operatingAirFlowMaximum_Lps,
            double cooling_kWh,
            double maximumTableError_K,
            double maximumCanonicalDeviation_Lps)
        {
            HasOutcome = true;
            Count_Hours = count_Hours;
            Count_Cooling = count_Cooling;
            Count_Heating = count_Heating;
            Count_BelowGate = count_BelowGate;
            Count_GateViolation = count_GateViolation;
            Count_OutOfRange = count_OutOfRange;
            Count_OffLaw = count_OffLaw;
            Count_InPublishedDomain = count_InPublishedDomain;
            OperatingAirFlowMinimum_Lps = operatingAirFlowMinimum_Lps;
            OperatingAirFlowMean_Lps = operatingAirFlowMean_Lps;
            OperatingAirFlowMaximum_Lps = operatingAirFlowMaximum_Lps;
            Cooling_kWh = cooling_kWh;
            MaximumTableError_K = maximumTableError_K;
            MaximumCanonicalDeviation_Lps = maximumCanonicalDeviation_Lps;
        }

        /// <summary>Every resolved field stated, the row bound to its air system, and the outcome recorded with no refused behaviour in it.</summary>
        public bool IsComplete => Guid_AirHandlingUnit != Guid.Empty
            && Guid_AirSystem != Guid.Empty
            && !string.IsNullOrWhiteSpace(Source)
            && !string.IsNullOrWhiteSpace(Table)
            && !string.IsNullOrWhiteSpace(Sha256_Table)
            && Finite(MaximumOperatingAirFlow_Lps)
            && Finite(MinimumOperatingAirFlow_Lps)
            && Finite(CoolingEnableTemperature_C)
            && Finite(DesignSupplyFlowRate_Lps)
            && Finite(DesignExtractFlowRate_Lps)
            && HasOutcome
            && Count_Hours > 0
            && Count_Heating == 0
            && Count_GateViolation == 0
            && Count_OutOfRange == 0;

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}: {1} {2} ({3}) + cooling module '{4}'; capacity {5:0.###}/{6:0.###} l/s; DesignAirFlow {7:0.###}/{8:0.###} l/s; cooling ceiling {9:0.###} l/s, {10:0.###} l/s at {11:0.###} C -> {9:0.###} l/s at {12:0.###} C; gate {13:0.###} C{14}",
                Name_AirHandlingUnit,
                Manufacturer,
                Model,
                Reference,
                CoolingModuleModel,
                MaximumSupplyFlowRate_Lps,
                MaximumExtractFlowRate_Lps,
                DesignSupplyFlowRate_Lps,
                DesignExtractFlowRate_Lps,
                MaximumOperatingAirFlow_Lps,
                MinimumOperatingAirFlow_Lps,
                ControlTemperature_Low_C,
                ControlTemperature_High_C,
                CoolingEnableTemperature_C,
                HasOutcome
                    ? string.Format(
                        CultureInfo.InvariantCulture,
                        "; OperatingAirFlow {0:0.###}..{1:0.###} l/s (mean {2:0.###}); cooling {3} h, {4:0.#} kWh air-side sensible; heating {5} h; cooling below the gate {6} h",
                        OperatingAirFlowMinimum_Lps,
                        OperatingAirFlowMaximum_Lps,
                        OperatingAirFlowMean_Lps,
                        Count_Cooling,
                        Cooling_kWh,
                        Count_Heating,
                        Count_GateViolation)
                    : string.Empty);
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
                { "CoolingModuleModel", CoolingModuleModel },
                { "Source", Source },
                { "MaximumSupplyFlowRate_Lps", Number(MaximumSupplyFlowRate_Lps) },
                { "MaximumExtractFlowRate_Lps", Number(MaximumExtractFlowRate_Lps) },
                { "DesignSupplyFlowRate_Lps", Number(DesignSupplyFlowRate_Lps) },
                { "DesignExtractFlowRate_Lps", Number(DesignExtractFlowRate_Lps) },
                { "Table", Table },
                { "Sha256_Table", Sha256_Table },
                { "MaximumOperatingAirFlow_Lps", Number(MaximumOperatingAirFlow_Lps) },
                { "MinimumOperatingAirFlow_Lps", Number(MinimumOperatingAirFlow_Lps) },
                { "ControlTemperature_Low_C", Number(ControlTemperature_Low_C) },
                { "ControlTemperature_High_C", Number(ControlTemperature_High_C) },
                { "FlowFraction_Low", Number(FlowFraction_Low) },
                { "FlowFraction_High", Number(FlowFraction_High) },
                { "CoolingEnableTemperature_C", Number(CoolingEnableTemperature_C) },
                { "DeclaredRules", DeclaredRules },
                { "HasOutcome", HasOutcome },
            };

            if (HasOutcome)
            {
                result.Add("Count_Hours", (long)Count_Hours);
                result.Add("Count_Cooling", (long)Count_Cooling);
                result.Add("Count_Heating", (long)Count_Heating);
                result.Add("Count_BelowGate", (long)Count_BelowGate);
                result.Add("Count_GateViolation", (long)Count_GateViolation);
                result.Add("Count_OutOfRange", (long)Count_OutOfRange);
                result.Add("Count_OffLaw", (long)Count_OffLaw);
                result.Add("Count_InPublishedDomain", (long)Count_InPublishedDomain);
                result.Add("OperatingAirFlowMinimum_Lps", Number(OperatingAirFlowMinimum_Lps));
                result.Add("OperatingAirFlowMean_Lps", Number(OperatingAirFlowMean_Lps));
                result.Add("OperatingAirFlowMaximum_Lps", Number(OperatingAirFlowMaximum_Lps));
                result.Add("Cooling_kWh", Number(Cooling_kWh));
                result.Add("MaximumTableError_K", Number(MaximumTableError_K));
                result.Add("MaximumCanonicalDeviation_Lps", Number(MaximumCanonicalDeviation_Lps));
            }

            return result;
        }

        public static PartOIteration3CoolingEvidence FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return null;
            }

            PartOIteration3CoolingEvidence result = new(
                PartOIteration3Json.Guid(jsonObject, "Guid_AirHandlingUnit"),
                PartOIteration3Json.Text(jsonObject, "Name_AirHandlingUnit"),
                PartOIteration3Json.Text(jsonObject, "Manufacturer"),
                PartOIteration3Json.Text(jsonObject, "Model"),
                PartOIteration3Json.Text(jsonObject, "Reference"),
                PartOIteration3Json.Text(jsonObject, "CoolingModuleModel"),
                PartOIteration3Json.Text(jsonObject, "Source"),
                Value(jsonObject, "MaximumSupplyFlowRate_Lps"),
                Value(jsonObject, "MaximumExtractFlowRate_Lps"),
                Value(jsonObject, "DesignSupplyFlowRate_Lps"),
                Value(jsonObject, "DesignExtractFlowRate_Lps"),
                PartOIteration3Json.Text(jsonObject, "Table"),
                PartOIteration3Json.Text(jsonObject, "Sha256_Table"),
                Value(jsonObject, "MaximumOperatingAirFlow_Lps"),
                Value(jsonObject, "MinimumOperatingAirFlow_Lps"),
                Value(jsonObject, "ControlTemperature_Low_C"),
                Value(jsonObject, "ControlTemperature_High_C"),
                Value(jsonObject, "FlowFraction_Low"),
                Value(jsonObject, "FlowFraction_High"),
                Value(jsonObject, "CoolingEnableTemperature_C"),
                PartOIteration3Json.Text(jsonObject, "DeclaredRules"));

            Guid guid_AirSystem = PartOIteration3Json.Guid(jsonObject, "Guid_AirSystem");
            if (guid_AirSystem != Guid.Empty)
            {
                result.Bind(guid_AirSystem);
            }

            if (PartOIteration3Json.Boolean(jsonObject, "HasOutcome", false))
            {
                result.RecordOutcome(
                    PartOIteration3Json.Count(jsonObject, "Count_Hours", 0),
                    PartOIteration3Json.Count(jsonObject, "Count_Cooling", 0),
                    PartOIteration3Json.Count(jsonObject, "Count_Heating", -1),
                    PartOIteration3Json.Count(jsonObject, "Count_BelowGate", 0),
                    PartOIteration3Json.Count(jsonObject, "Count_GateViolation", -1),
                    PartOIteration3Json.Count(jsonObject, "Count_OutOfRange", -1),
                    PartOIteration3Json.Count(jsonObject, "Count_OffLaw", 0),
                    PartOIteration3Json.Count(jsonObject, "Count_InPublishedDomain", 0),
                    Value(jsonObject, "OperatingAirFlowMinimum_Lps"),
                    Value(jsonObject, "OperatingAirFlowMean_Lps"),
                    Value(jsonObject, "OperatingAirFlowMaximum_Lps"),
                    Value(jsonObject, "Cooling_kWh"),
                    Value(jsonObject, "MaximumTableError_K"),
                    Value(jsonObject, "MaximumCanonicalDeviation_Lps"));
            }

            return result;
        }

        private static JsonNode Number(double value)
        {
            return Finite(value) ? JsonValue.Create(value) : null;
        }

        private static double Value(JsonObject jsonObject, string key)
        {
            return jsonObject[key] is JsonValue jsonValue && jsonValue.TryGetValue(out double value) ? value : double.NaN;
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
