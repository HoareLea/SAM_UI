namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Returns value of point depends on chart data type
        /// </summary>
        /// <param name="mollierPoint">Mollier point</param>
        /// <param name="chartDataType">Chart data type</param>
        /// <returns>Value of point</returns>
        public static double Value(this MollierPoint mollierPoint, ChartDataType chartDataType)
        {
            if(mollierPoint == null || chartDataType == ChartDataType.Undefined)
            {
                return double.NaN;
            }

            switch(chartDataType)
            {
                case ChartDataType.DryBulbTemperature:
                    return mollierPoint.DryBulbTemperature;

                case ChartDataType.RelativeHumidity:
                    return mollierPoint.RelativeHumidity;

                case ChartDataType.HumidityRatio:
                    return mollierPoint.HumidityRatio;

                case ChartDataType.DewPointTemperature:
                    return mollierPoint.DewPointTemperature();

                case ChartDataType.WetBulbTemperature:
                    return mollierPoint.WetBulbTemperature();
            }

            return double.NaN;
        }
    }
}
