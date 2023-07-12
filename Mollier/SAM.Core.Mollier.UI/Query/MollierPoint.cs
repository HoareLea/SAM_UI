namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static MollierPoint MollierPoint(double x, double y, double pressure, ChartType chartType)
        {
            if(double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(pressure) || chartType == ChartType.Undefined)
            {
                return null;
            }

            double humidityRatio = chartType == ChartType.Mollier ? x / 1000 : y;
            double dryBulbTemperature = chartType == ChartType.Mollier ? Mollier.Query.DryBulbTemperature_ByDiagramTemperature(y, humidityRatio, pressure) : x;

            MollierPoint result = new MollierPoint(dryBulbTemperature, humidityRatio, pressure);
            return result;
        }

        public static MollierPoint MollierPoint(double x, double y, MollierControlSettings mollierControlSettings)
        {
            if (double.IsNaN(x) || double.IsNaN(y) || mollierControlSettings == null)
            {
                return null;
            }

            return MollierPoint(x, y, mollierControlSettings.Pressure, mollierControlSettings.ChartType);
        }
    }
}

