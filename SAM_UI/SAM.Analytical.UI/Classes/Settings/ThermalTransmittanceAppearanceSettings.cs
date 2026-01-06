// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ThermalTransmittanceAppearanceSettings : ValueAppearanceSettings
    {
        public ThermalTransmittanceAppearanceSettings()
            :base()
        {

        }

        public ThermalTransmittanceAppearanceSettings(ThermalTransmittanceAppearanceSettings thermalTransmittanceAppearanceSettings)
            : base(thermalTransmittanceAppearanceSettings)
        {
            if(thermalTransmittanceAppearanceSettings != null)
            {

            }
        }

        public ThermalTransmittanceAppearanceSettings(JObject jObject)
            : base(jObject)
        {

        }

        public override bool TryGetValue<T>(IJSAMObject jSAMObject, out T value)
        {
            value = default;
            
            if(!(jSAMObject is Panel || jSAMObject is Aperture))
            {
                return false;
            }

            if(jSAMObject is Panel)
            {
                if(!((SAMObject)jSAMObject).TryGetValue(PanelParameter.ThermalTransmittance, out double thermalTransmittance))
                {
                    return false;
                }

                return Core.Query.TryConvert(thermalTransmittance, out value);
            }

            if (jSAMObject is Aperture)
            {
                if (!((SAMObject)jSAMObject).TryGetValue(ApertureParameter.ThermalTransmittance, out double thermalTransmittance))
                {
                    return false;
                }

                return Core.Query.TryConvert(thermalTransmittance, out value);
            }

            return false;
        }
    }
}
