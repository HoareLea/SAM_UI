using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static MollierPoint ToMollier(this Point2D point2D, ChartType chartType, double pressure)
        {
            double x = point2D.X;
            double y = point2D.Y;

            double humidityRatio = chartType == ChartType.Mollier ? x / 1000 : y;
            double dryBulbTemperature = chartType == ChartType.Mollier ? y : x / 1000;

            return new MollierPoint(dryBulbTemperature, humidityRatio, pressure);
        }

    }
}

