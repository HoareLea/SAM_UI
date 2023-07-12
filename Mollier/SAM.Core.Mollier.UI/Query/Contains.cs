using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static bool Contains(Series series, ChartType chartType, MollierPoint mollierPoint, double tolerance = SAM.Core.Tolerance.Distance)
        {
            if(series == null || chartType == ChartType.Undefined || mollierPoint == null)
            {
                return false;
            }

            foreach(DataPoint dataPoint in series.Points)
            {
                double dryBulbTemperature = chartType == ChartType.Mollier ? mollierPoint.HumidityRatio : mollierPoint.HumidityRatio; ;
                double humidityRatio = chartType == ChartType.Mollier ? mollierPoint.HumidityRatio : mollierPoint.HumidityRatio;
            }

            throw new System.NotImplementedException();
        }
    }
}

