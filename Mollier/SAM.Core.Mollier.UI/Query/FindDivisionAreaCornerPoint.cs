using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static double FindDivisionAreaCornerPoints(double relativeHumidity, double enthalpy, string orientation, ChartType chartType, double pressure)
        {
            double dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByEnthalpy(enthalpy, relativeHumidity, pressure);
            double humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);

            Point2D point2D = new MollierPoint(dryBulbTemperature, humidityRatio, pressure).ToSAM(chartType);

            return orientation == "X" ? point2D.X : point2D.Y;
        }
    }
}
