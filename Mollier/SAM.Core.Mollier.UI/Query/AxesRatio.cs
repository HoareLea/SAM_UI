using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Returns OX axis to OY axis length ratio
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

            Control control = chart.Parent;
            Form form = control.FindForm();

            if(form == null)
            {
                return 1;
            }

            double screenWidth = Screen.GetWorkingArea(form).Width;
            double screenHeight = Screen.GetWorkingArea(form).Height;

            double widthFactor = (screenWidth / (double)form.ClientSize.Width);
            double heightFactor = (screenHeight / (double)form.ClientSize.Height);
            double screenFactor = widthFactor / heightFactor;


            return xScale / yScale * screenFactor;
        }
    }
}
