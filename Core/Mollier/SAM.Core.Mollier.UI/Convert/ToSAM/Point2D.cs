using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static Point2D ToSAM(this DataPoint dataPoint, int indexY = 0)
        {
            if(dataPoint == null || indexY == -1)
            {
                return null;
            }

            return new Point2D(dataPoint.XValue, dataPoint.YValues[indexY]);
        }
        public static Point2D ToSAM(this MollierPoint mollierPoint, ChartType chartType)
        {
            if(mollierPoint == null)
            {
                return null;
            }
            double humidityRatio = mollierPoint.HumidityRatio;
            double dryBulbTemperature = mollierPoint.DryBulbTemperature;
            double diagramTemperature = double.NaN;

            if (chartType == ChartType.Mollier)
            {
                diagramTemperature = Mollier.Query.DiagramTemperature(mollierPoint);
            }

            double x = chartType == ChartType.Mollier ? humidityRatio * 1000 : dryBulbTemperature;
            double y = chartType == ChartType.Mollier ? diagramTemperature : humidityRatio * 1000;

            return new Point2D(x, y);
        }

    }
}

