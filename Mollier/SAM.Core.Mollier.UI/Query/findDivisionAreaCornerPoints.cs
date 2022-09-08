using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static double FindDivisionAreaCornerPoints(double relativeHumidity, double enthalpy, string orientation, ChartType chartType, double pressure)
        {
            double X, Y;
            double dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByEnthalpy(enthalpy * 1000, relativeHumidity, pressure);
            double humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);

            X = chartType == ChartType.Mollier ? humidityRatio * 1000 : dryBulbTemperature;
            Y = chartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(dryBulbTemperature, humidityRatio) : humidityRatio;

            return orientation == "X" ? X : Y;
        }
    }
}
