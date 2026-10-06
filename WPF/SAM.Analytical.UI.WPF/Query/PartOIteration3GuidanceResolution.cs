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
            IEnumerable<Zone> zones_Dwelling,
            PartODwellingStrategySet partODwellingStrategySet,
            VentilationUnitCatalogue ventilationUnitCatalogue,
            out Dictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings,
            out List<string> notes)
        {
            return PartOIteration3GuidanceResolution(adjacencyCluster, zones_Dwelling, partODwellingStrategySet, ventilationUnitCatalogue, out guidanceSettings, out notes, out List<PartOIteration3GuidanceEvidence> _);
        }

        /// <summary>
        /// The same resolution, also answering each resolved unit's guidance as fields
        /// (<see cref="PartOIteration3GuidanceEvidence"/>) for presentation. The notes, the settings and the
        /// refusals are exactly those of the overload above.
        /// </summary>
        public static List<string> PartOIteration3GuidanceResolution(
            AdjacencyCluster adjacencyCluster,
            IEnumerable<Zone> zones_Dwelling,
            PartODwellingStrategySet partODwellingStrategySet,
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

            if (partODwellingStrategySet is null || !partODwellingStrategySet.IsValid)
            {
                refusals.Add("The prepared design has no valid saved Part O dwelling strategy selections, so its cooling control rooms cannot be resolved.");
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

                HashSet<Guid> guids_Served = [];
                foreach (VentilationSystem ventilationSystem in adjacencyCluster.VentilationSystems(airHandlingUnit))
                {
                    foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [])
                    {
                        guids_Served.Add(space.Guid);
                    }
                }

                List<Zone> zones_Served = [];
                foreach (Zone zone_Dwelling in zones_Dwelling ?? [])
                {
                    Zone zone = zone_Dwelling is null ? null : adjacencyCluster.GetObject<Zone>(zone_Dwelling.Guid);
                    if (zone is not null && (adjacencyCluster.GetRelatedObjects<Space>(zone) ?? []).Exists(x => guids_Served.Contains(x.Guid)))
                    {
                        zones_Served.Add(zone);
                    }
                }

                if (zones_Served.Count != 1)
                {
                    refusals.Add(string.Format("Air handling unit '{0}' must serve exactly one prepared Part O dwelling to resolve its cooling control room; it serves {1}.", airHandlingUnit.Name, zones_Served.Count));
                    continue;
                }

                Zone zone_Served = zones_Served[0];
                PartODwellingStrategy strategy_Dwelling = partODwellingStrategySet.Strategy(zone_Served.Guid);
                Guid guid_Stat = strategy_Dwelling?.CoolingStatSpaceGuid ?? Guid.Empty;
                List<Space> spaces_Dwelling = adjacencyCluster.GetRelatedObjects<Space>(zone_Served) ?? [];
                if (guid_Stat == Guid.Empty || !spaces_Dwelling.Exists(x => x.Guid == guid_Stat) || !guids_Served.Contains(guid_Stat))
                {
                    refusals.Add(string.Format("Air handling unit '{0}' serves dwelling '{1}', which has no valid selected cooling control room among the rooms it serves (selected room GUID: {2}).", airHandlingUnit.Name, zone_Served.Name, guid_Stat));
                    continue;
                }

                mechanicalVentilationGuidanceSettings.CoolingStatSpaceGuid = guid_Stat;

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
