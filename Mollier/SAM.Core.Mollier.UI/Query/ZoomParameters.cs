using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="series_List">List of all series added to the chart</param>
        /// <param name="chartType">Type of the chart</param>
        /// <param name="x_Min">X axis minimum value</param>
        /// <param name="x_Max">X axis maximum value</param>
        /// <param name="y_Min">Y axis minimum value</param>
        /// <param name="y_Max">Y axis maximum value</param>
        /// <param name="x_Factor">X scale of zoom</param>
        /// <param name="y_Factor">Y scale of zoom</param>
        public static void ZoomParameters(IEnumerable<Series> series_List, ChartType chartType, out double x_Min, out double x_Max, out double y_Min, out double y_Max, double x_Factor = 5, double y_Factor = 5)
        {
            List<DataPoint> dataPoints = new List<DataPoint>();
            foreach (Series series in series_List)
            {
                if (!(series.Tag is IMollierProcess) && !(series.Tag is List<MollierPoint>))
                {
                    continue;
                }

                foreach (DataPoint dataPoint in series.Points)
                {
                    dataPoints.Add(dataPoint);
                }
            }
            x_Min = double.MaxValue;
            x_Max = double.MinValue;
            y_Min = double.MaxValue;
            y_Max = double.MinValue;
            foreach (DataPoint dataPoint in dataPoints)
            {
                if (dataPoint.XValue > x_Max)
                {
                    x_Max = dataPoint.XValue;
                }
                if (dataPoint.XValue < x_Min)
                {
                    x_Min = dataPoint.XValue;
                }

                if (dataPoint.YValues[0] > y_Max)
                {
                    y_Max = dataPoint.YValues[0];
                }
                if (dataPoint.YValues[0] < y_Min)
                {
                    y_Min = dataPoint.YValues[0];
                }
            }
            if(chartType == ChartType.Psychrometric)
            {
                y_Max *= 1000;
                y_Min *= 1000;
            }
            x_Min = x_Min % x_Factor == 0 ? System.Math.Floor(x_Min / x_Factor) * x_Factor - x_Factor : System.Math.Floor(x_Min / x_Factor) * x_Factor;
            x_Max = x_Max % x_Factor == 0 ? System.Math.Ceiling(x_Max / x_Factor) * x_Factor + x_Factor : System.Math.Ceiling(x_Max / x_Factor) * x_Factor;
            y_Min = y_Min % y_Factor == 0 ? System.Math.Floor(y_Min / y_Factor) * y_Factor - y_Factor : System.Math.Floor(y_Min / y_Factor) * y_Factor;
            y_Max = y_Max % y_Factor == 0 ? System.Math.Ceiling(y_Max / y_Factor) * y_Factor + y_Factor : System.Math.Ceiling(y_Max / y_Factor) * y_Factor;
        }
    }
}
