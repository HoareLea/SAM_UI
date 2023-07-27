using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System;
using System.Windows.Forms;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        [Obsolete("To be changed Maciek")]
        public static void FindPoints(Control control, Chart chart, MollierControlSettings mollierControlSettings, List<UIMollierPoint> mollierPoints)
        {
            bool generate = mollierControlSettings.FindPoint;
            double percent = mollierControlSettings.Percent;
            ChartType chartType = mollierControlSettings.ChartType;

            foreach (Series series_Temp in chart.Series)
            {
                if (series_Temp.Tag == "FoundPoint")
                {
                    series_Temp.Enabled = false;
                }
            }
            if (generate == false || mollierPoints == null || mollierPoints.Count < 4 || percent > 100 || percent < 0)//if too 
            {
                return;
            }

            int index = System.Convert.ToInt32((1 - percent / 100) * mollierPoints.Count) - 1;
            if (index < 0)
            {
                index = 0;
            }

            List<UIMollierPoint> uIMollierPoints = new List<UIMollierPoint>(mollierPoints);//copy of mollierPoints
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Point;
            series.BorderWidth = 4;
            series.MarkerColor = Color.Red;
            series.MarkerSize = 10;
            series.MarkerStyle = MarkerStyle.Circle;
            series.Tag = "FoundPoint";

            Series series1 = chart.Series.Add(Guid.NewGuid().ToString());
            series1.IsVisibleInLegend = false;
            series1.ChartType = SeriesChartType.Point;
            series1.BorderWidth = 4;
            series1.MarkerColor = Color.Red;
            series1.MarkerSize = 15;
            series1.MarkerStyle = MarkerStyle.Circle;
            series1.Tag = "FoundPointToolTip"; 
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                series1.Points.AddXY((mollierControlSettings.HumidityRatio_Min + mollierControlSettings.HumidityRatio_Max) / 2, (mollierControlSettings.Temperature_Min + mollierControlSettings.Temperature_Max) / 4);
            }
            else
            {
                series1.Points.AddXY((mollierControlSettings.Temperature_Min + mollierControlSettings.Temperature_Max) / 4, (mollierControlSettings.HumidityRatio_Min + mollierControlSettings.HumidityRatio_Max) / 2000);
            }

            switch (mollierControlSettings.FindPointType)
            {
                case Mollier.ChartDataType.DryBulbTemperature:
                    uIMollierPoints.Sort((x, y) => x.MollierPoint.DryBulbTemperature.CompareTo(y.MollierPoint.DryBulbTemperature));
                    break;
                case Mollier.ChartDataType.Enthalpy:
                    uIMollierPoints.Sort((x, y) => x.MollierPoint.Enthalpy.CompareTo(y.MollierPoint.Enthalpy));
                    break;
            }

            Point2D point = Convert.ToSAM(uIMollierPoints[index].MollierPoint, chartType);
            series.Points.AddXY(point.X, point.Y);

            string foundPointLabel = ToolTipText(uIMollierPoints[index].MollierPoint, chartType);
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                Modify.AddLabel(chart, mollierControlSettings, series1.Points[0].XValue, series1.Points[0].YValues[0], 0, 0, 0, foundPointLabel, Mollier.ChartDataType.Undefined, ChartParameterType.Point, Color.Black, "ColorPointLabel");
            }
            else
            {
                Modify.AddLabel(chart, mollierControlSettings, series1.Points[0].XValue, series1.Points[0].YValues[0], 0, 0, 0.001 * -11 * Query.ScaleVector2D(control, mollierControlSettings).Y, foundPointLabel, Mollier.ChartDataType.Undefined, ChartParameterType.Point, Color.Black, "ColorPointLabel");
            }
        }
    }
}
