namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {

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

        public static Units.UnitType DefaultUnitType(this ChartDataType chartDataType)
        {
            if(chartDataType == UI.ChartDataType.Undefined)
            {
                return Units.UnitType.Undefined;
            }

            switch(chartDataType)
            {
                case UI.ChartDataType.DryBulbTemperature:
                    return Units.UnitType.Celsius;

                case UI.ChartDataType.RelativeHumidity:
                    return Units.UnitType.Percent;

                case UI.ChartDataType.HumidityRatio:
                    return Units.UnitType.GramPerKilogram;

                case UI.ChartDataType.DewPointTemperature:
                    return Units.UnitType.Celsius;

                case UI.ChartDataType.WetBulbTemperature:
                    return Units.UnitType.Celsius;

                case UI.ChartDataType.Enthalpy:
                    return Units.UnitType.KilojulePerKilogram;
            }

            return Units.UnitType.Undefined;
        }
    }
}
