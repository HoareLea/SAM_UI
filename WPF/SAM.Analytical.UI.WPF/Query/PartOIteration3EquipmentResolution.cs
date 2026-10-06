// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        internal const string PartOIteration3OperatingAirFlowBasis = "DesignAirFlow × constant yearly schedule 1.0; OperatingAirFlow remains equal to DesignAirFlow.";

        internal const string PartOIteration3FanPowerSplitRule = "Certified total-both-fans SFP split equally: supply pressure = extract pressure = 1000 × SFP / 2, with overall efficiency 1.0.";

        internal const string PartOIteration3FanHeatGainAssumption = "Declared assumption, not manufacturer data: supply and extract HeatGainFactor are both 1.0 (all simulated fan load enters the air stream).";

        /// <summary>
        /// PR5A (SAM#111 plan §J): resolves every scoped air handling unit's already-selected product to
        /// its certified heat-recovery efficiency and specific fan power, and states the generic per-unit
        /// settings SAM_Systems' materialisation applies from them.
        /// <para>
        /// <b>All-or-nothing, by design.</b> One unit with no selection, an unresolvable reference, or
        /// missing certified data (E1/E2) refuses the WHOLE call - the caller (<c>Modify.RunPartOIteration3</c>)
        /// is expected to refuse the whole Selected-product attempt on any refusal here, never to
        /// materialise some units at parity and others manufacturer-aware within the one run. That mirrors
        /// SAM_Systems' own <c>MechanicalVentilationSettings.UnitSettings</c> contract: a partially
        /// configured graph is never produced.
        /// </para>
        /// <para>
        /// <b>No selection authority here.</b> The selected product is read, never chosen - Iteration 2's
        /// own selection (<c>AirHandlingUnitParameter.VentilationUnitReference</c>, via
        /// <c>Analytical.Query.SelectedVentilationUnitReference</c>) remains the only place a product is
        /// assigned. This resolves identity to certified figures and nothing more.
        /// </para>
        /// <para>
        /// <b>The declared fan-heat assumption.</b> A certified specific fan power is a whole-unit figure;
        /// SAM_Systems' own plan (§C) declares an equal pressure split between the supply and extract fan
        /// as a stated simplification, not manufacturer data. This resolver states the same simplification
        /// for the fan heat gain fraction: both fans are stated at <c>1.0</c> (all of the fan's simulated
        /// load enters the air stream), because neither in-scope product publishes a fan-heat split either.
        /// Both simplifications are recorded on <see cref="PartOIteration3EquipmentEvidence"/> as what they
        /// are - a declared assumption, not a certified figure.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The scoped working copy - the same one <c>IPartOIteration3Pipeline.Materialise</c> is handed.</param>
        /// <param name="ventilationUnitCatalogue">The catalogue read for this attempt.</param>
        /// <param name="unitSettings">Every resolved unit's settings, keyed by <c>AirHandlingUnit.Guid</c> - empty unless every unit resolved.</param>
        /// <param name="equipment">One evidence row per resolved unit, for the pairing record.</param>
        /// <param name="notes">What resolved, in words, for the ledger.</param>
        /// <returns>Every refusal. Empty means every scoped unit resolved.</returns>
        public static List<string> PartOIteration3EquipmentResolution(
            AdjacencyCluster adjacencyCluster,
            VentilationUnitCatalogue ventilationUnitCatalogue,
            out Dictionary<Guid, MechanicalVentilationUnitSettings> unitSettings,
            out List<PartOIteration3EquipmentEvidence> equipment,
            out List<string> notes)
        {
            unitSettings = [];
            equipment = [];
            notes = [];

            List<string> refusals = [];

            if (ventilationUnitCatalogue is null || ventilationUnitCatalogue.State == VentilationUnitCatalogueState.Unavailable)
            {
                refusals.Add(string.Format(
                    "The ventilation unit catalogue could not be read at '{0}', so no selected product could be resolved to certified data.",
                    ventilationUnitCatalogue?.Path ?? "<unresolved path>"));

                return refusals;
            }

            if (string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Directory)
                || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Path)
                || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Schema)
                || string.IsNullOrWhiteSpace(ventilationUnitCatalogue.Sha256))
            {
                refusals.Add("The ventilation unit catalogue was read without a complete directory, file, schema and SHA-256 provenance, so no selected product behaviour can be used auditably.");

                return refusals;
            }

            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster?.GetObjects<AirHandlingUnit>() ?? [];

            airHandlingUnits.RemoveAll(x => x is null);
            airHandlingUnits.Sort((x, y) => x.Guid.CompareTo(y.Guid));

            //SAM #114 scope removes a scoped-out ventilation system from the working copy but leaves its
            //AirHandlingUnit object behind. SAM_Systems materialises only the units a RETAINED ventilation
            //system names as its supply unit, and refuses settings for any other - so exactly those units
            //are resolved here. A scoped-out unit is neither required to carry a selection nor given
            //settings; it is named in the notes so its absence is visible.
            List<AirHandlingUnit> airHandlingUnits_NotMaterialised = airHandlingUnits.FindAll(x => adjacencyCluster.VentilationSystems(x).Count == 0);
            airHandlingUnits.RemoveAll(x => airHandlingUnits_NotMaterialised.Contains(x));

            foreach (AirHandlingUnit airHandlingUnit_NotMaterialised in airHandlingUnits_NotMaterialised)
            {
                notes.Add(string.Format(
                    "Air handling unit '{0}' is not named by any retained ventilation system, so it is not materialised and no product was resolved for it.",
                    airHandlingUnit_NotMaterialised.Name));
            }

            if (airHandlingUnits.Count == 0)
            {
                refusals.Add("The scoped design carries no air handling unit that a retained ventilation system names, so no selected product could be resolved.");
                return refusals;
            }

            List<VentilationUnitTemplate> ventilationUnitTemplates = ventilationUnitCatalogue?.Templates ?? [];

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                VentilationUnitReference ventilationUnitReference_Selected = Analytical.Query.SelectedVentilationUnitReference(airHandlingUnit);

                if (ventilationUnitReference_Selected is null || !ventilationUnitReference_Selected.IsValid)
                {
                    refusals.Add(string.Format(
                        "Air handling unit '{0}' has no selected ventilation unit product, so its manufacturer-aware behaviour could not be resolved. Select a product for it, or run this pairing in Parity mode.",
                        airHandlingUnit.Name));

                    continue;
                }

                VentilationUnitTemplate ventilationUnitTemplate = airHandlingUnit.SelectedVentilationUnitTemplate(ventilationUnitTemplates);

                if (ventilationUnitTemplate is null)
                {
                    refusals.Add(string.Format(
                        "Air handling unit '{0}' selects '{1}', which the ventilation unit catalogue does not hold (or holds ambiguously), so its certified data could not be resolved. No other product is substituted.",
                        airHandlingUnit.Name,
                        ventilationUnitReference_Selected));

                    continue;
                }

                if (string.IsNullOrWhiteSpace(ventilationUnitTemplate.Source))
                {
                    refusals.Add(string.Format(
                        "Air handling unit '{0}' selects '{1}', but that catalogue entry names no traceable source for its certified data.",
                        airHandlingUnit.Name,
                        ventilationUnitReference_Selected));

                    continue;
                }

                if (!adjacencyCluster.AirHandlingUnitDesignDuty(airHandlingUnit, out double supplyAirFlowRate_Lps, out double extractAirFlowRate_Lps))
                {
                    refusals.Add(string.Format(
                        "Air handling unit '{0}' states no design duty, so its selected product's certified data could not be looked up at a design airflow.",
                        airHandlingUnit.Name));

                    continue;
                }

                if (!adjacencyCluster.IsVentilationUnitSufficient(airHandlingUnit, ventilationUnitCatalogue.CapacityDescriptors, out string refusal_Capacity))
                {
                    refusals.Add(refusal_Capacity);

                    continue;
                }

                VentilationUnitOperatingParameters ventilationUnitOperatingParameters = ventilationUnitTemplate.VentilationUnitOperatingParameters(supplyAirFlowRate_Lps, extractAirFlowRate_Lps);

                if (!ventilationUnitOperatingParameters.IsHeatRecoveryResolved)
                {
                    refusals.Add(string.Format("Air handling unit '{0}' ({1}): {2}", airHandlingUnit.Name, ventilationUnitReference_Selected, ventilationUnitOperatingParameters.HeatRecoveryRefusal));
                }

                if (!ventilationUnitOperatingParameters.IsFanPerformanceResolved)
                {
                    refusals.Add(string.Format("Air handling unit '{0}' ({1}): {2}", airHandlingUnit.Name, ventilationUnitReference_Selected, ventilationUnitOperatingParameters.FanPerformanceRefusal));
                }

                if (!ventilationUnitOperatingParameters.IsHeatRecoveryResolved || !ventilationUnitOperatingParameters.IsFanPerformanceResolved)
                {
                    continue;
                }

                double specificFanPower_WPerLps = ventilationUnitOperatingParameters.SpecificFanPower_WPerLps;

                //Plan §C's declared equal split - a certified total, divided, not two certified halves.
                double fanPressure_Pa = 1000.0 * specificFanPower_WPerLps / 2.0;

                bool hasPartFRequirement = false;
                double partFRequiredSupplyFlowRate_Lps = 0;
                double partFRequiredExtractFlowRate_Lps = 0;

                foreach (VentilationSystem ventilationSystem in adjacencyCluster.VentilationSystems(airHandlingUnit))
                {
                    if (!adjacencyCluster.PartFRequiredSystemDuty(ventilationSystem, out double supplyRequirement_Lps, out double extractRequirement_Lps))
                    {
                        continue;
                    }

                    hasPartFRequirement = true;
                    partFRequiredSupplyFlowRate_Lps += supplyRequirement_Lps;
                    partFRequiredExtractFlowRate_Lps += extractRequirement_Lps;
                }

                unitSettings[airHandlingUnit.Guid] = new MechanicalVentilationUnitSettings
                {
                    HeatRecoverySensibleEfficiency = ventilationUnitOperatingParameters.SensibleHeatRecoveryEfficiency,
                    SupplyFanPressure_Pa = fanPressure_Pa,
                    ExtractFanPressure_Pa = fanPressure_Pa,
                    FanOverallEfficiency = 1.0,
                    //Declared, not certified - see the class remarks above.
                    SupplyFanHeatGainFactor = 1.0,
                    ExtractFanHeatGainFactor = 1.0,
                };

                equipment.Add(new PartOIteration3EquipmentEvidence(
                    airHandlingUnit.Guid,
                    airHandlingUnit.Name,
                    ventilationUnitReference_Selected.Manufacturer,
                    ventilationUnitReference_Selected.Model,
                    ventilationUnitReference_Selected.Reference,
                    ventilationUnitTemplate.Source,
                    ventilationUnitOperatingParameters.AirFlowRate_Lps,
                    ventilationUnitOperatingParameters.SensibleHeatRecoveryEfficiency,
                    ventilationUnitOperatingParameters.HeatRecoveryEfficiencyBasis.ToString(),
                    ventilationUnitOperatingParameters.HeatRecoveryClampedToDomain,
                    null,
                    ventilationUnitOperatingParameters.SpecificFanPower_WPerLps,
                    ventilationUnitOperatingParameters.SpecificFanPowerBasis.ToString(),
                    ventilationUnitOperatingParameters.FanPerformanceClampedToDomain,
                    null,
                    ventilationUnitTemplate.MaximumSupplyFlowRate_Lps,
                    ventilationUnitTemplate.MaximumExtractFlowRate_Lps,
                    supplyAirFlowRate_Lps,
                    extractAirFlowRate_Lps,
                    hasPartFRequirement ? partFRequiredSupplyFlowRate_Lps : null,
                    hasPartFRequirement ? partFRequiredExtractFlowRate_Lps : null,
                    PartOIteration3OperatingAirFlowBasis,
                    fanPressure_Pa,
                    fanPressure_Pa,
                    1.0,
                    1.0,
                    1.0,
                    PartOIteration3FanPowerSplitRule,
                    PartOIteration3FanHeatGainAssumption));

                notes.Add(string.Format("Air handling unit '{0}' resolved to {1}.", airHandlingUnit.Name, ventilationUnitOperatingParameters));
            }

            //All-or-nothing: a partial resolution is not offered to the caller as usable settings, even
            //where some units did resolve - the caller decides whether "some refused" is the whole run's
            //refusal, but this method never hands back a unitSettings dictionary that would materialise a
            //partially configured graph.
            if (refusals.Count != 0)
            {
                unitSettings = [];
                equipment = [];
            }

            return refusals;
        }
    }
}
