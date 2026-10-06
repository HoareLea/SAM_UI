// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        internal const string PartOIteration3CoolingDeclaredRules =
            "Declared mapping, not manufacturer data: the recirculation ceiling is the cooling table's own airflow-axis maximum (validated performance, never the unit's capacity); "
            + "the cooling-enable temperature is the flow-fraction law's lower control temperature; the table is held at its published edges (no extrapolation); "
            + "each room's share of the ceiling is its floor-area share; the recirculation fan is a heat-gain-free pressure-flow surrogate copied from the unit's supply fan, "
            + "not a manufacturer fan; the module is an aggregate thermal surrogate - no electrical, efficiency-ratio, latent or refrigerant behaviour is modelled.";

        /// <summary>
        /// PR5B (SAM#111): resolves every scoped air handling unit's already-selected product to its published
        /// cooling module - the supply-air temperature table and the flow-fraction law the catalogue carries -
        /// and states the generic cooling settings SAM_Systems materialises from them.
        /// <para>
        /// <b>All-or-nothing, and read, never chosen</b> - exactly as
        /// <see cref="PartOIteration3EquipmentResolution"/>: Iteration 2's selection is the only place a product
        /// is assigned; a unit with no selection, an unresolvable reference, a product that cannot carry its
        /// design duty, or no valid cooling data refuses the whole call.
        /// </para>
        /// <para>
        /// <b>Only the cooling layer.</b> Nothing here resolves heat recovery or fan performance, so B4 is B0
        /// plus the cooling module and B4 - B0 is the cooling layer alone. Nothing here reads a manufacturer or
        /// model name as a condition.
        /// </para>
        /// <para>
        /// <b>The ceiling is validated performance.</b> It is the table's own airflow axis - a figure the table
        /// does not state is not validated - and it is refused where it exceeds the selected capacity. It is
        /// never written as, or from, a ventilation design airflow.
        /// </para>
        /// </summary>
        public static List<string> PartOIteration3CoolingResolution(
            AdjacencyCluster adjacencyCluster,
            VentilationUnitCatalogue ventilationUnitCatalogue,
            out Dictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings,
            out List<PartOIteration3CoolingEvidence> cooling,
            out List<string> notes)
        {
            coolingSettings = [];
            cooling = [];
            notes = [];

            List<string> refusals = [];

            if (ventilationUnitCatalogue is null || ventilationUnitCatalogue.State == VentilationUnitCatalogueState.Unavailable)
            {
                refusals.Add(string.Format(
                    "The ventilation unit catalogue could not be read at '{0}', so no selected product's cooling module could be resolved.",
                    ventilationUnitCatalogue?.Path ?? "<unresolved path>"));

                return refusals;
            }

            if (string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Directory)
                || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Path)
                || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Schema)
                || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Sha256))
            {
                refusals.Add("The ventilation unit catalogue was read without a complete directory, file, schema and SHA-256 provenance, so no cooling module can be used auditably.");

                return refusals;
            }

            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster?.GetObjects<AirHandlingUnit>() ?? [];
            airHandlingUnits.RemoveAll(x => x is null);
            airHandlingUnits.Sort((x, y) => x.Guid.CompareTo(y.Guid));

            //The units SAM_Systems materialises - those a retained ventilation system names (SAM #114 scope).
            List<AirHandlingUnit> airHandlingUnits_NotMaterialised = airHandlingUnits.FindAll(x => adjacencyCluster.VentilationSystems(x).Count == 0);
            airHandlingUnits.RemoveAll(x => airHandlingUnits_NotMaterialised.Contains(x));

            foreach (AirHandlingUnit airHandlingUnit_NotMaterialised in airHandlingUnits_NotMaterialised)
            {
                notes.Add(string.Format(
                    "Air handling unit '{0}' is not named by any retained ventilation system, so it is not materialised and no cooling module was resolved for it.",
                    airHandlingUnit_NotMaterialised.Name));
            }

            if (airHandlingUnits.Count == 0)
            {
                refusals.Add("The scoped design carries no air handling unit that a retained ventilation system names, so no cooling module could be resolved.");
                return refusals;
            }

            List<VentilationUnitTemplate> ventilationUnitTemplates = ventilationUnitCatalogue.Templates ?? [];

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                VentilationUnitReference ventilationUnitReference_Selected = Analytical.Query.SelectedVentilationUnitReference(airHandlingUnit);

                if (ventilationUnitReference_Selected is null || !ventilationUnitReference_Selected.IsValid)
                {
                    refusals.Add(string.Format(
                        "Air handling unit '{0}' has no selected ventilation unit product, so no cooling module could be resolved for it. Select a product for it, or run this pairing in Parity mode.",
                        airHandlingUnit.Name));

                    continue;
                }

                VentilationUnitTemplate ventilationUnitTemplate = airHandlingUnit.SelectedVentilationUnitTemplate(ventilationUnitTemplates);

                if (ventilationUnitTemplate is null)
                {
                    refusals.Add(string.Format(
                        "Air handling unit '{0}' selects '{1}', which the ventilation unit catalogue does not hold (or holds ambiguously), so its cooling module could not be resolved. No other product is substituted.",
                        airHandlingUnit.Name,
                        ventilationUnitReference_Selected));

                    continue;
                }

                if (string.IsNullOrWhiteSpace(ventilationUnitTemplate.Source))
                {
                    refusals.Add(string.Format("Air handling unit '{0}' selects '{1}', but that catalogue entry names no traceable source for its data.", airHandlingUnit.Name, ventilationUnitReference_Selected));
                    continue;
                }

                if (!adjacencyCluster.AirHandlingUnitDesignDuty(airHandlingUnit, out double supplyAirFlowRate_Lps, out double extractAirFlowRate_Lps))
                {
                    refusals.Add(string.Format("Air handling unit '{0}' states no design duty, so its selected product could not be checked against it.", airHandlingUnit.Name));
                    continue;
                }

                if (!adjacencyCluster.IsVentilationUnitSufficient(airHandlingUnit, ventilationUnitCatalogue.CapacityDescriptors, out string refusal_Capacity))
                {
                    refusals.Add(refusal_Capacity);
                    continue;
                }

                VentilationUnitPerformanceTable table = ventilationUnitTemplate.PerformanceTable;
                FlowFractionControlCurve flowFractionControlCurve = ventilationUnitTemplate.FlowFractionByControlTemperature;

                if (table is null || table.Output(VentilationUnitPerformanceOutput.Name_SupplyAirTemperature) is null || flowFractionControlCurve is null)
                {
                    refusals.Add(string.Format(
                        "Air handling unit '{0}' selects '{1}', which publishes no cooling module - no supply-air temperature table and flow-fraction law - so there is nothing to materialise.",
                        airHandlingUnit.Name,
                        ventilationUnitReference_Selected));

                    continue;
                }

                VentilationUnitPerformanceAxis axis_AirFlow = table.Axis(VentilationUnitPerformanceAxis.Name_AirFlowRate);
                double ceiling_Lps = axis_AirFlow?.Maximum ?? double.NaN;

                MechanicalVentilationCoolingSettings mechanicalVentilationCoolingSettings = new()
                {
                    SupplyAirTemperatureTable = table,
                    FlowFractionByControlTemperature = flowFractionControlCurve,
                    MaximumOperatingAirFlow_Lps = ceiling_Lps,
                    CoolingEnableTemperature_C = flowFractionControlCurve.MinimumControlTemperature_C,
                };

                string refusal_Cooling = mechanicalVentilationCoolingSettings.Refusal();
                if (refusal_Cooling is not null)
                {
                    refusals.Add(string.Format("Air handling unit '{0}' ({1}) has a cooling module that {2}", airHandlingUnit.Name, ventilationUnitReference_Selected, refusal_Cooling));
                    continue;
                }

                //Validated performance may not exceed what the unit can move.
                if (!(ceiling_Lps <= ventilationUnitTemplate.MaximumSupplyFlowRate_Lps) || !(ceiling_Lps <= ventilationUnitTemplate.MaximumExtractFlowRate_Lps))
                {
                    refusals.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "Air handling unit '{0}' ({1}): the cooling table's {2:0.###} l/s ceiling exceeds the selected unit's {3:0.###}/{4:0.###} l/s capacity.",
                        airHandlingUnit.Name,
                        ventilationUnitReference_Selected,
                        ceiling_Lps,
                        ventilationUnitTemplate.MaximumSupplyFlowRate_Lps,
                        ventilationUnitTemplate.MaximumExtractFlowRate_Lps));

                    continue;
                }

                coolingSettings[airHandlingUnit.Guid] = mechanicalVentilationCoolingSettings;

                double[] controlTemperatures = flowFractionControlCurve.ControlTemperatures_C;
                double[] flowFractions = flowFractionControlCurve.FlowFractions;

                cooling.Add(new PartOIteration3CoolingEvidence(
                    airHandlingUnit.Guid,
                    airHandlingUnit.Name,
                    ventilationUnitReference_Selected.Manufacturer,
                    ventilationUnitReference_Selected.Model,
                    ventilationUnitReference_Selected.Reference,
                    ventilationUnitTemplate.CoolingModuleModel,
                    ventilationUnitTemplate.Source,
                    ventilationUnitTemplate.MaximumSupplyFlowRate_Lps,
                    ventilationUnitTemplate.MaximumExtractFlowRate_Lps,
                    supplyAirFlowRate_Lps,
                    extractAirFlowRate_Lps,
                    TableDescription(table),
                    Sha256(table),
                    ceiling_Lps,
                    mechanicalVentilationCoolingSettings.MinimumOperatingAirFlow_Lps,
                    controlTemperatures[0],
                    controlTemperatures[1],
                    flowFractions[0],
                    flowFractions[1],
                    mechanicalVentilationCoolingSettings.CoolingEnableTemperature_C,
                    PartOIteration3CoolingDeclaredRules));

                notes.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "Air handling unit '{0}' resolved to the cooling module of {1}: {2:0.###}..{3:0.###} l/s recirculation, enabled from {4:0.###} C.",
                    airHandlingUnit.Name,
                    ventilationUnitReference_Selected,
                    mechanicalVentilationCoolingSettings.MinimumOperatingAirFlow_Lps,
                    ceiling_Lps,
                    mechanicalVentilationCoolingSettings.CoolingEnableTemperature_C));
            }

            if (refusals.Count != 0)
            {
                coolingSettings = [];
                cooling = [];
            }

            return refusals;
        }

        private static string TableDescription(VentilationUnitPerformanceTable table)
        {
            StringBuilder stringBuilder = new(string.Format("{0} [{1}] over", VentilationUnitPerformanceOutput.Name_SupplyAirTemperature, table.Output(VentilationUnitPerformanceOutput.Name_SupplyAirTemperature)?.Unit));

            for (int i = 0; i < table.AxisCount; i++)
            {
                VentilationUnitPerformanceAxis axis = table.Axis(i);
                stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, " {0} {1:0.###}..{2:0.###} {3} ({4})", axis.Name, axis.Minimum, axis.Maximum, axis.Unit, axis.Count));
            }

            stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, ": {0} cells, held at the published edges.", table.PointCount));

            return stringBuilder.ToString();
        }

        private static string Sha256(VentilationUnitPerformanceTable table)
        {
            using SHA256 sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(table.ToJsonObject().ToJsonString()))).Replace("-", string.Empty);
        }
    }
}
