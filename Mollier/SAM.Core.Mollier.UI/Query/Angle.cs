using SAM.Geometry.Planar;
using System;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{

    public static partial class Query
    {
        [Obsolete("To remove")] 
        public static int Angle(Series series, MollierControlSettings mollierControlSettings)
        {
            if(series == null || mollierControlSettings == null)
            {
                return 0;
            }

            ConstantValueCurve constantValueCurve = series.Tag as ConstantValueCurve;
            if(constantValueCurve == null)
            {
                return 0;
            }

            ChartType chartType = mollierControlSettings.ChartType;

            //takes series (line must be straight) and chartType and returns angle of label along the line

            double range_difference = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min);
            Point2D a = new Point2D(); a.X = series.Points[0].XValue; a.Y = series.Points[0].YValues[0];
            Point2D b = new Point2D(); b.X = series.Points[1].XValue * range_difference; b.Y = series.Points[1].YValues[0];

            a.X = series.Points[0].XValue;
            a.Y = chartType == ChartType.Mollier ? series.Points[0].YValues[0] : series.Points[0].YValues[0] * 1000;
            b.X = chartType == ChartType.Mollier ? series.Points[1].XValue * range_difference * 2 : 2 * series.Points[1].XValue - a.X;
            b.Y = chartType == ChartType.Mollier ? series.Points[1].YValues[0] : series.Points[1].YValues[0] * 1000 * range_difference;

            Vector2D vector = chartType == ChartType.Mollier ? new Vector2D(a, b) : new Vector2D(a, b);
            if (Core.Query.AlmostEqual(vector.Length, 0))
            {
                return 0;
            }

            int result = System.Convert.ToInt32(vector.Angle(Vector2D.WorldX) * 180 / System.Math.PI);

            if(chartType == ChartType.Psychrometric && constantValueCurve.ChartDataType == Mollier.ChartDataType.SpecificVolume)
            {
                while (result > 90)
                {
                    result -= 90;
                }

                while (result < -90)
                {
                    result += 90;
                }

                return result;
            }

            return chartType == ChartType.Mollier ? result : 180 - result;

        }
    }
}
