// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Core.Mollier.UI
{
    public enum ProcessCalculationType
    {
        [Description("Undefined")] Undefined,
        [Description("Dry Bulb Temperature")] DryBulbTemperature,
        [Description("Dry Bulb Temperature Difference")] DryBulbTemperatureDifference,
        [Description("Enthalpy Difference")] SpecificEnthalpyDifference,
        [Description("Relative Humidity")] RelativeHumidity,
        [Description("Humidity Ratio Difference")] HumidityRatioDifference,  
        [Description("Medium and Dry Bulb Temperature")] MediumAndDryBulbTemperature,
        [Description("Medium and Efficiency")] MediumAndEfficiency,
    }
}
