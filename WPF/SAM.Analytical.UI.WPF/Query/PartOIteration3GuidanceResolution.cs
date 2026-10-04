// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// SAM#123: each scoped air handling unit's already-selected product, resolved to the operating
        /// strategy its manufacturer's guidance states - the catalogue's data, resolved domain-side by
        /// <c>SAM.Analytical.Systems.Query.MechanicalVentilationGuidanceSettings</c>. This method only
        /// orchestrates: it holds no product constant and makes no engineering choice of its own.
        /// <para>
        /// <b>Manufacturer guidance, provisional, not certified performance.</b> A unit with no selection, an
        /// unresolvable reference, a product whose entry states no guidance strategy, or a strategy that cannot
        /// be resolved for the dwelling refuses the whole run rather than falling back for that unit.
        /// </para>
        /// </summary>
        public static List<string> PartOIteration3GuidanceResolution(
            AdjacencyCluster adjacencyCluster,
            VentilationUnitCatalogue ventilationUnitCatalogue,
            out Dictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings,
            out List<string> notes)
        {
            return PartOIteration3GuidanceResolution(adjacencyCluster, ventilationUnitCatalogue, out guidanceSettings, out notes, out List<PartOIteration3GuidanceEvidence> _);
        }

        /// <summary>
        /// The same resolution, also answering each resolved unit's guidance as fields
        /// (<see cref="PartOIteration3GuidanceEvidence"/>) for presentation. The notes, the settings and the
        /// refusals are exactly those of the overload above.
        /// </summary>
        public static List<string> PartOIteration3GuidanceResolution(
            AdjacencyCluster adjacencyCluster,
            VentilationUnitCatalogue ventilationUnitCatalogue,
            out Dictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings,
            out List<string> notes,
            out List<PartOIteration3GuidanceEvidence> evidence)
        {
            guidanceSettings = [];
            notes = [];
            evidence = [];

            List<string> refusals = [];

            if (ventilationUnitCatalogue is null || ventilationUnitCatalogue.State == VentilationUnitCatalogueState.Unavailable
                || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Sha256) || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Schema))
            {
                refusals.Add(string.Format(
                    "The ventilation unit catalogue could not be read with complete provenance at '{0}', so no selected product's manufacturer guidance could be resolved.",
                    ventilationUnitCatalogue?.Path ?? "<unresolved path>"));

                return refusals;
            }

            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster?.GetObjects<AirHandlingUnit>() ?? [];
            airHandlingUnits.RemoveAll(x => x is null || adjacencyCluster.VentilationSystems(x).Count == 0);
            airHandlingUnits.Sort((x, y) => x.Guid.CompareTo(y.Guid));

            if (airHandlingUnits.Count == 0)
            {
                refusals.Add("The scoped design carries no air handling unit that a retained ventilation system names, so no manufacturer guidance could be resolved.");
                return refusals;
            }

            List<VentilationUnitTemplate> ventilationUnitTemplates = ventilationUnitCatalogue.Templates ?? [];

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                VentilationUnitReference ventilationUnitReference_Selected = Analytical.Query.SelectedVentilationUnitReference(airHandlingUnit);

                if (ventilationUnitReference_Selected is null || !ventilationUnitReference_Selected.IsValid)
                {
                    refusals.Add(string.Format("Air handling unit '{0}' has no selected ventilation unit product, so no manufacturer guidance could be resolved for it.", airHandlingUnit.Name));
                    continue;
                }

                VentilationUnitTemplate ventilationUnitTemplate = airHandlingUnit.SelectedVentilationUnitTemplate(ventilationUnitTemplates);

                if (ventilationUnitTemplate is null)
                {
                    refusals.Add(string.Format("Air handling unit '{0}' selects '{1}', which the ventilation unit catalogue does not hold (or holds ambiguously). No other product is substituted.", airHandlingUnit.Name, ventilationUnitReference_Selected));
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

                MechanicalVentilationGuidanceSettings mechanicalVentilationGuidanceSettings = ventilationUnitTemplate.MechanicalVentilationGuidanceSettings(out string refusal_Guidance);

                if (mechanicalVentilationGuidanceSettings is null)
                {
                    refusals.Add(string.Format("Air handling unit '{0}' selects '{1}', whose catalogue entry {2}", airHandlingUnit.Name, ventilationUnitReference_Selected, refusal_Guidance));
                    continue;
                }

                guidanceSettings[airHandlingUnit.Guid] = mechanicalVentilationGuidanceSettings;

                VentilationUnitOperatingStrategy strategy = mechanicalVentilationGuidanceSettings.OperatingStrategy;
                double elevated_Lps = strategy.ElevatedAirFlow_Lps;

                notes.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "MANUFACTURER GUIDANCE (provisional, not certified performance): '{0}' uses {1}{2}. Design (Part F requirement carried as design) {3:0.###} l/s supply / {4:0.###} l/s extract; equipment capacity {5:0.###} / {6:0.###} l/s; elevated operating airflow while cooling {7:0.###} l/s each side (stated range {8:0.###}-{9:0.###} l/s). Cooling switched by the {10} above {11:0.###} C; supply while cooling = {13}, at {7:0.###} l/s exchanger fraction {12:0.####} (unless bypassed), net coil drop {14:0.###} K, not below {15:0.###} C; bypass/recovery on the unit's own intake and extract sensors, independent of the cooling-stat; exchanger topology MVRE with a supply DX coil.",
                    airHandlingUnit.Name,
                    ventilationUnitReference_Selected,
                    string.IsNullOrWhiteSpace(ventilationUnitTemplate.CoolingModuleModel) ? string.Empty : " + " + ventilationUnitTemplate.CoolingModuleModel,
                    supplyAirFlowRate_Lps,
                    extractAirFlowRate_Lps,
                    ventilationUnitTemplate.MaximumSupplyFlowRate_Lps,
                    ventilationUnitTemplate.MaximumExtractFlowRate_Lps,
                    elevated_Lps,
                    strategy.MinimumElevatedAirFlow_Lps,
                    strategy.MaximumElevatedAirFlow_Lps,
                    Core.Query.Description(strategy.CoolingActivationSignal).ToLowerInvariant(),
                    strategy.CoolingActivationTemperature_C,
                    strategy.CoolingSupplyTemperatureRule.ExchangerExtractFraction(elevated_Lps),
                    strategy.CoolingSupplyTemperatureRule,
                    strategy.CoolingSupplyTemperatureRule.CoilNetTemperatureDrop_K(elevated_Lps),
                    strategy.CoolingSupplyTemperatureRule.MinimumSupplyTemperature_C));

                evidence.Add(new PartOIteration3GuidanceEvidence(
                    airHandlingUnit.Guid,
                    airHandlingUnit.Name,
                    ventilationUnitReference_Selected.ToString(),
                    string.IsNullOrWhiteSpace(ventilationUnitTemplate.CoolingModuleModel) ? null : ventilationUnitTemplate.CoolingModuleModel,
                    supplyAirFlowRate_Lps,
                    extractAirFlowRate_Lps,
                    ventilationUnitTemplate.MaximumSupplyFlowRate_Lps,
                    ventilationUnitTemplate.MaximumExtractFlowRate_Lps,
                    elevated_Lps,
                    strategy.MinimumElevatedAirFlow_Lps,
                    strategy.MaximumElevatedAirFlow_Lps,
                    Core.Query.Description(strategy.CoolingActivationSignal),
                    strategy.CoolingActivationTemperature_C,
                    strategy.CoolingSupplyTemperatureRule?.ToString(),
                    strategy.CoolingSupplyTemperatureRule.ExchangerExtractFraction(elevated_Lps),
                    strategy.CoolingSupplyTemperatureRule.CoilNetTemperatureDrop_K(elevated_Lps),
                    strategy.CoolingSupplyTemperatureRule.MinimumSupplyTemperature_C)
                {
                    Guid_CoolingStatSpace = mechanicalVentilationGuidanceSettings.CoolingStatSpaceGuid,
                });
            }

            return refusals;
        }
    }
}
