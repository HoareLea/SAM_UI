﻿namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Returns default unit type for parameter type
        /// </summary>
        /// <param name="processParameterType">Process parameter type</param>
        /// <returns>Default unit</returns>
        public static Units.UnitType DefaultUnitType(this ProcessParameterType processParameterType)
        {
            switch (processParameterType)
            {
                case ProcessParameterType.Efficiency:
                case ProcessParameterType.LatentHeatRecoveryEfficiency:
                case ProcessParameterType.SensibleHeatRecoveryEfficiency:
                case ProcessParameterType.RelativeHumidity:
                    return Units.UnitType.Percent;

                case ProcessParameterType.HumidityRatioDifference:
                case ProcessParameterType.HumidityRatio:
                    return Units.UnitType.GramPerKilogram;

                case ProcessParameterType.SpecificEnthalpyDifference:
                    return Units.UnitType.KilojulePerKilogram;
            }
            return Units.Query.UnitType(Units.UnitStyle.SI, UnitCategory(processParameterType));
        }

        /// <summary>
        /// Returns default unit type for chart data type
        /// </summary>
        /// <param name="chartDataType">Chart data type</param>
        /// <returns>Default unit</returns>
        public static Units.UnitType DefaultUnitType(this ChartDataType chartDataType)
        {
            if(chartDataType == ChartDataType.Undefined)
            {
                return Units.UnitType.Undefined;
            }

            switch(chartDataType)
            {
                case ChartDataType.DryBulbTemperature:
                    return Units.UnitType.Celsius;

                case ChartDataType.RelativeHumidity:
                    return Units.UnitType.Percent;

                case ChartDataType.HumidityRatio:
                    return Units.UnitType.GramPerKilogram;

                case ChartDataType.DewPointTemperature:
                    return Units.UnitType.Celsius;

                case ChartDataType.WetBulbTemperature:
                    return Units.UnitType.Celsius;

                case ChartDataType.Enthalpy:
                    return Units.UnitType.KilojulePerKilogram;
            }

            return Units.UnitType.Undefined;
        }
    }
}
