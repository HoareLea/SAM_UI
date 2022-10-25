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
                    return Units.UnitType.GramPerKilogram;
                case ProcessParameterType.EnthalpyDifference:
                    return Units.UnitType.Kilojule;
            }
            return Units.Query.UnitType(Units.UnitStyle.SI, UnitCategory(processParameterType));

        }
    }
}
