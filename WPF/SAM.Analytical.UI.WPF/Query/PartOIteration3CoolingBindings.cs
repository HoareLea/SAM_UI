// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// PR5B (SAM#111): binds every resolved cooling row to the one recirculation branch SAM_Systems
        /// materialised for its unit - by the unit's guid, never a name - and refuses where a unit has no
        /// branch, two branches, or a branch outside its own air system, or where a branch exists that no row
        /// resolved.
        /// </summary>
        public static List<string> PartOIteration3CoolingBindings(IEnumerable<PartOIteration3CoolingEvidence> cooling, MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation)
        {
            List<string> result = [];

            Dictionary<Guid, Guid> airSystem_By_AirHandlingUnit = [];
            foreach (MechanicalVentilationBinding mechanicalVentilationBinding in mechanicalVentilationMaterialisation?.Bindings ?? [])
            {
                if (mechanicalVentilationBinding.BindingType == MechanicalVentilationBindingType.AirSystem)
                {
                    airSystem_By_AirHandlingUnit[mechanicalVentilationBinding.Guid_Analytical] = mechanicalVentilationBinding.Guid_Systems;
                }
            }

            Dictionary<Guid, MechanicalVentilationRecirculationCooling> branch_By_AirHandlingUnit = [];
            foreach (MechanicalVentilationRecirculationCooling mechanicalVentilationRecirculationCooling in mechanicalVentilationMaterialisation?.RecirculationCoolings ?? [])
            {
                if (branch_By_AirHandlingUnit.ContainsKey(mechanicalVentilationRecirculationCooling.Guid_AirHandlingUnit))
                {
                    result.Add(string.Format("Air handling unit {0} was materialised with two recirculation cooling branches.", mechanicalVentilationRecirculationCooling.Guid_AirHandlingUnit));
                    continue;
                }

                branch_By_AirHandlingUnit[mechanicalVentilationRecirculationCooling.Guid_AirHandlingUnit] = mechanicalVentilationRecirculationCooling;
            }

            HashSet<Guid> guids_Bound = [];

            foreach (PartOIteration3CoolingEvidence partOIteration3CoolingEvidence in cooling ?? [])
            {
                if (!branch_By_AirHandlingUnit.TryGetValue(partOIteration3CoolingEvidence.Guid_AirHandlingUnit, out MechanicalVentilationRecirculationCooling mechanicalVentilationRecirculationCooling))
                {
                    result.Add(string.Format("Air handling unit '{0}' resolved a cooling module, and no recirculation cooling branch was materialised for it.", partOIteration3CoolingEvidence.Name_AirHandlingUnit));
                    continue;
                }

                if (!airSystem_By_AirHandlingUnit.TryGetValue(partOIteration3CoolingEvidence.Guid_AirHandlingUnit, out Guid guid_AirSystem) || guid_AirSystem != mechanicalVentilationRecirculationCooling.Guid_AirSystem)
                {
                    result.Add(string.Format("The recirculation cooling branch of air handling unit '{0}' is not inside that unit's own air system.", partOIteration3CoolingEvidence.Name_AirHandlingUnit));
                    continue;
                }

                if (!partOIteration3CoolingEvidence.Bind(guid_AirSystem))
                {
                    result.Add(string.Format("The cooling row of air handling unit '{0}' is already bound to another air system.", partOIteration3CoolingEvidence.Name_AirHandlingUnit));
                    continue;
                }

                guids_Bound.Add(partOIteration3CoolingEvidence.Guid_AirHandlingUnit);
            }

            foreach (Guid guid_AirHandlingUnit in branch_By_AirHandlingUnit.Keys)
            {
                if (!guids_Bound.Contains(guid_AirHandlingUnit))
                {
                    result.Add(string.Format("A recirculation cooling branch was materialised for air handling unit {0}, which resolved no cooling module.", guid_AirHandlingUnit));
                }
            }

            return result;
        }

        /// <summary>
        /// PR5B (SAM#111): records each unit's outcome, verbatim, from the route's own checked recirculation
        /// cooling evidence - and refuses where a bound unit has none. Nothing is recomputed here: the route
        /// already refused any heating, any cooling below a gate and any recirculation outside its law.
        /// </summary>
        public static List<string> PartOIteration3CoolingOutcomes(IEnumerable<PartOIteration3CoolingEvidence> cooling, RecirculationCoolingResults recirculationCoolingResults)
        {
            List<string> result = [];

            if (recirculationCoolingResults is null || !recirculationCoolingResults.IsComplete)
            {
                result.Add("The route returned no complete recirculation cooling evidence, so no unit's cooling module can be shown to have behaved as built.");
                return result;
            }

            foreach (PartOIteration3CoolingEvidence partOIteration3CoolingEvidence in cooling ?? [])
            {
                RecirculationCoolingResult recirculationCoolingResult = recirculationCoolingResults.Result(partOIteration3CoolingEvidence.Guid_AirSystem);

                if (recirculationCoolingResult is null || recirculationCoolingResult.Guid_AirHandlingUnit != partOIteration3CoolingEvidence.Guid_AirHandlingUnit)
                {
                    result.Add(string.Format("The route's cooling evidence has no result for air handling unit '{0}'.", partOIteration3CoolingEvidence.Name_AirHandlingUnit));
                    continue;
                }

                partOIteration3CoolingEvidence.RecordOutcome(
                    recirculationCoolingResult.Count,
                    recirculationCoolingResult.Count_Cooling,
                    recirculationCoolingResult.Count_Heating,
                    recirculationCoolingResult.Count_BelowGate,
                    recirculationCoolingResult.Count_GateViolation,
                    recirculationCoolingResult.Count_OutOfRange,
                    recirculationCoolingResult.Count_OffLaw,
                    recirculationCoolingResult.Count_InPublishedDomain,
                    recirculationCoolingResult.OperatingAirFlowMinimum_Lps,
                    recirculationCoolingResult.OperatingAirFlowMean_Lps,
                    recirculationCoolingResult.OperatingAirFlowMaximum_Lps,
                    recirculationCoolingResult.Cooling_kWh,
                    recirculationCoolingResult.MaximumTableError_K,
                    recirculationCoolingResult.MaximumCanonicalDeviation_Lps);

                if (!partOIteration3CoolingEvidence.IsComplete)
                {
                    result.Add(string.Format("The cooling row of air handling unit '{0}' is not complete after its outcome was recorded.", partOIteration3CoolingEvidence.Name_AirHandlingUnit));
                }
            }

            return result;
        }

        /// <summary>
        /// PR5B: the hourly OperatingAirFlow history as CSV - one row per unit and hour: the recirculation
        /// airflow, the mixed-return temperature the law and the gate acted on, the coil outlet and the outdoor
        /// dry bulb. Invariant culture, 0-based hour of year. OperatingAirFlow only: no DesignAirFlow appears.
        /// </summary>
        public static string PartOIteration3OperatingAirFlowCsv(RecirculationCoolingResults recirculationCoolingResults, IEnumerable<PartOIteration3CoolingEvidence> cooling)
        {
            Dictionary<Guid, string> name_By_AirSystem = [];
            foreach (PartOIteration3CoolingEvidence partOIteration3CoolingEvidence in cooling ?? [])
            {
                name_By_AirSystem[partOIteration3CoolingEvidence.Guid_AirSystem] = partOIteration3CoolingEvidence.Name_AirHandlingUnit;
            }

            System.Text.StringBuilder stringBuilder = new("AirHandlingUnit,Guid_AirHandlingUnit,Guid_AirSystem,Hour,OperatingAirFlow_Lps,MixedReturnTemperature_C,CoilOutletTemperature_C,OutdoorTemperature_C");
            stringBuilder.AppendLine();

            System.Globalization.CultureInfo cultureInfo = System.Globalization.CultureInfo.InvariantCulture;

            foreach (RecirculationCoolingResult recirculationCoolingResult in recirculationCoolingResults?.Results ?? [])
            {
                name_By_AirSystem.TryGetValue(recirculationCoolingResult.Guid_AirSystem, out string name);

                double[] operatingAirFlow_Lps = recirculationCoolingResult.OperatingAirFlow_Lps;
                double[] mixedReturnTemperature_C = recirculationCoolingResult.MixedReturnTemperature_C;
                double[] supplyTemperature_C = recirculationCoolingResult.SupplyTemperature_C;
                double[] outdoorTemperature_C = recirculationCoolingResult.OutdoorTemperature_C;

                for (int i = 0; i < operatingAirFlow_Lps.Length; i++)
                {
                    stringBuilder.AppendLine(string.Format(
                        cultureInfo,
                        "\"{0}\",{1},{2},{3},{4:R},{5:R},{6:R},{7:R}",
                        (name ?? string.Empty).Replace("\"", "\"\""),
                        recirculationCoolingResult.Guid_AirHandlingUnit,
                        recirculationCoolingResult.Guid_AirSystem,
                        recirculationCoolingResult.StartHour + i,
                        operatingAirFlow_Lps[i],
                        mixedReturnTemperature_C[i],
                        supplyTemperature_C[i],
                        outdoorTemperature_C[i]));
                }
            }

            return stringBuilder.ToString();
        }
    }
}
