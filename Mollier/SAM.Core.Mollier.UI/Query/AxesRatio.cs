using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Returns OX axis to OY axis ratio
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="mollierControlSettings"></param>
        /// <returns></returns>
        public static double AxesRatio(this Chart chart, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            double realOXSize = chartType == ChartType.Mollier ? 30.45 : 28;
            double realOYSize = chartType == ChartType.Mollier ? 13.3 : 13.3;

            double OXRatio = chart.ChartAreas[0].AxisX.Maximum - chart.ChartAreas[0].AxisX.Minimum;
            double OYRatio = chart.ChartAreas[0].AxisY.Maximum - chart.ChartAreas[0].AxisY.Minimum;

            double xScale = OXRatio / realOXSize;
            double yScale = OYRatio / realOYSize;

            return xScale / yScale;
        }
    }
}
