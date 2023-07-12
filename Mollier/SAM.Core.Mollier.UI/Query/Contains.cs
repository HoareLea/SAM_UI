using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static bool Contains(this Series series, ChartType chartType, MollierPoint mollierPoint, double tolerance = Tolerance.Distance)
        {
            if(series == null || chartType == ChartType.Undefined || mollierPoint == null)
            {
                return false;
            }

            foreach(DataPoint dataPoint in series.Points)
            {
                MollierPoint mollierPoint_Temp = MollierPoint(dataPoint.XValue, dataPoint.YValues[0], mollierPoint.Pressure, chartType);
                if(mollierPoint.DryBulbTemperature.AlmostEqual(mollierPoint_Temp.DryBulbTemperature, tolerance) && mollierPoint.HumidityRatio.AlmostEqual(mollierPoint_Temp.HumidityRatio, tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains(this Series series, double x, double y, double tolerance = Tolerance.Distance)
        {
            if(series == null || double.IsNaN(x) || double.IsNaN(y))
            {
                return false;
            }

            foreach(DataPoint dataPoint in series.Points)
            {
                if(!dataPoint.XValue.AlmostEqual(x, tolerance))
                {
                    continue;
                }

                foreach(double y_Temp in dataPoint.YValues)
                {
                    if (!y_Temp.AlmostEqual(y, tolerance))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }
    }
}

