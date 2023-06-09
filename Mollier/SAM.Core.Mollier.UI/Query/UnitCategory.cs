namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {

        public static Units.UnitCategory UnitCategory(this ProcessParameterType processParameterType)
        {
            switch (processParameterType) 
            {
                case ProcessParameterType.DryBulbTemperature:
                    return Units.UnitCategory.Temperature;
                case ProcessParameterType.DryBulbTemperatureDifference:
                    return Units.UnitCategory.Temperature;
                case ProcessParameterType.HumidityRatioDifference:
                case ProcessParameterType.HumidityRatio:
                    return Units.UnitCategory.HumidityRatio;
                case ProcessParameterType.FlowTemperature:
                    return Units.UnitCategory.Temperature;
                case ProcessParameterType.ReturnTemperature:
                    return Units.UnitCategory.Temperature;
                case ProcessParameterType.Airflow:
                    return Units.UnitCategory.AirFlow;
                case ProcessParameterType.Efficiency:
                    return Units.UnitCategory.Efficiency;
                case ProcessParameterType.LatentHeatRecoveryEfficiency:
                    return Units.UnitCategory.Efficiency;
                case ProcessParameterType.SensibleHeatRecoveryEfficiency:
                    return Units.UnitCategory.Efficiency;
                case ProcessParameterType.RelativeHumidity:
                    return Units.UnitCategory.RelativeHumidity;
                case ProcessParameterType.SpecificEnthalpyDifference:
                    return Units.UnitCategory.SpecificEnthaply;
                case ProcessParameterType.Load:
                    return Units.UnitCategory.Power;
                case ProcessParameterType.Undefined:
                    return Units.UnitCategory.Undefined;
            }
            return Units.UnitCategory.Undefined;

        }
    }
}
