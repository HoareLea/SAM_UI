﻿using System.ComponentModel;

namespace SAM.Core.Mollier.UI
{
    public enum ChartDataType
    {
        [Description("Undefined")] Undefined,
        [Description("Relative Humidity")] RelativeHumidity,
        [Description("Diagram Temperature")] DiagramTemperature,
        [Description("Specific Volume")] SpecificVolume,
        [Description("Density")] Density,
        [Description("Enthalpy")] Enthalpy,
        [Description("Wet Bulb Temperature")] WetBulbTemperature,
        [Description("Dew Point Temperature")] DewPointTemperature,
        [Description("Humidity Ratio")] HumidityRatio,
        [Description("Dry Bulb Temperature")] DryBulbTemperature,
        [Description("Heating Process")] HeatingProcess,
        [Description("Cooling Process")] CoolingProcess,
        [Description("Heat Recovery Process")] HeatRecoveryProcess,
        [Description("Mixing Process")] MixingProcess,
        [Description("Steam Humidification Process")] SteamHumidificationProcess,
        [Description("Undefined Process")] UndefinedProcess,
    }
}
