namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static double Value(this MollierPoint mollierPoint, ChartDataType chartDataType)
        {
            if(mollierPoint == null || chartDataType == UI.ChartDataType.Undefined)
            {
                return double.NaN;
            }

            switch(chartDataType)
            {
                case UI.ChartDataType.DryBulbTemperature:
                    return mollierPoint.DryBulbTemperature;

                case UI.ChartDataType.RelativeHumidity:
                    return mollierPoint.RelativeHumidity;

                case UI.ChartDataType.HumidityRatio:
                    return mollierPoint.HumidityRatio;

                case UI.ChartDataType.DewPointTemperature:
                    return mollierPoint.DewPointTemperature();

                case UI.ChartDataType.WetBulbTemperature:
                    return mollierPoint.WetBulbTemperature();
            }

            return double.NaN;
        }
    }
}
