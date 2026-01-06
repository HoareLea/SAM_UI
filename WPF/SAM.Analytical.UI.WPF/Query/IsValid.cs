// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static bool IsValid(this ThermalTransmittanceFilter thermalTransmittanceFilter, DisplayThermalTransmittanceCalculationResult displayThermalTransmittanceCalculationResult)
        {
            if(thermalTransmittanceFilter == null || displayThermalTransmittanceCalculationResult == null)
            {
                return false;
            }

            if(!double.IsNaN(thermalTransmittanceFilter.LightTransmittance))
            {
                double? value = displayThermalTransmittanceCalculationResult.LightTransmittance;
                if(value == null || double.IsNaN(value.Value))
                {
                    return false;
                }

                if(value.Value < thermalTransmittanceFilter.LightTransmittance_Min || value.Value > thermalTransmittanceFilter.LightTransmittance_Max)
                {
                    return false;
                }
            }

            if (!double.IsNaN(thermalTransmittanceFilter.ThermalTransmittance))
            {
                double? value = displayThermalTransmittanceCalculationResult.ThermalTransmittance;
                if (value == null || double.IsNaN(value.Value))
                {
                    return false;
                }

                if (value.Value < thermalTransmittanceFilter.ThermalTransmittance_Min || value.Value > thermalTransmittanceFilter.ThermalTransmittance_Max)
                {
                    return false;
                }
            }

            if (!double.IsNaN(thermalTransmittanceFilter.TotalSolarEnergyTransmittance))
            {
                double? value = displayThermalTransmittanceCalculationResult.TotalSolarEnergyTransmittance;
                if (value == null || double.IsNaN(value.Value))
                {
                    return false;
                }

                if (value.Value < thermalTransmittanceFilter.TotalSolarEnergyTransmittance_Min || value.Value > thermalTransmittanceFilter.TotalSolarEnergyTransmittance_Max)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
