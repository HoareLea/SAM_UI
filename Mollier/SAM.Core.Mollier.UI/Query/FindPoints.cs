using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        // query -> znajdowanie punktu o percent : FindPoints( IEnumerable<MollierPoint>, MollierControlSettings, ChartDataType, double percent)
        [Obsolete("To be changed Maciek")] // TODO: Change (MollierProcess)
        public static void FindPoints(Control control, Chart chart, MollierControlSettings mollierControlSettings, List<UIMollierPoint> mollierPoints)
        {
            bool generate = mollierControlSettings.FindPoint;
            double percent = mollierControlSettings.Percent;
            string chartDataType = mollierControlSettings.FindPointType;
            foreach (Series series_Temp in chart.Series)
            {
                if (series_Temp.Tag == "ColorPoint")
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
            series.Tag = "ColorPoint";
            Series series1 = chart.Series.Add(Guid.NewGuid().ToString());
            series1.IsVisibleInLegend = false;
            series1.ChartType = SeriesChartType.Point;
            series1.BorderWidth = 4;
            series1.MarkerColor = Color.Red;
            series1.MarkerSize = 15;
            series1.MarkerStyle = MarkerStyle.Circle;
            series1.Tag = "ColorPointLabelSquare";
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                series1.Points.AddXY((mollierControlSettings.HumidityRatio_Min + mollierControlSettings.HumidityRatio_Max) / 2, (mollierControlSettings.Temperature_Min + mollierControlSettings.Temperature_Max) / 4);
            }
            else
            {
                series1.Points.AddXY((mollierControlSettings.Temperature_Min + mollierControlSettings.Temperature_Max) / 4, (mollierControlSettings.HumidityRatio_Min + mollierControlSettings.HumidityRatio_Max) / 2000);
            }
            switch (chartDataType)
            {
                case "Temperature":
                    uIMollierPoints.Sort((x, y) => x.MollierPoint.DryBulbTemperature.CompareTo(y.MollierPoint.DryBulbTemperature));
                    UIMollierPoint uIMollierPoint_Temperature = uIMollierPoints[index];
                    double X_Temperature = mollierControlSettings.ChartType == ChartType.Mollier ? uIMollierPoint_Temperature.MollierPoint.HumidityRatio * 1000 : uIMollierPoint_Temperature.MollierPoint.DryBulbTemperature;
                    double Y_Temperature = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(uIMollierPoint_Temperature.MollierPoint) : uIMollierPoint_Temperature.MollierPoint.HumidityRatio;
                    series.Points.AddXY(X_Temperature, Y_Temperature);
                    string name_Temperature = Query.ToolTipText(uIMollierPoint_Temperature.MollierPoint, mollierControlSettings.ChartType, "Temperature " + percent.ToString() + "%") + "\nUnmet hours: " + System.Math.Ceiling(percent / 100 * uIMollierPoints.Count).ToString();
                    Modify.CreateMovedLabel(chart, mollierControlSettings, series1.Points[0].XValue, series1.Points[0].YValues[0], 0, 0, 0, -16 * Query.ScaleVector2D(control, mollierControlSettings).Y, 0, 0, name_Temperature, Mollier.ChartDataType.Undefined, ChartParameterType.Point, color: Color.Black, tag: "ColorPointLabel");
                    break;
                case "Enthalpy":
                    uIMollierPoints.Sort((x, y) => x.MollierPoint.Enthalpy.CompareTo(y.MollierPoint.Enthalpy));
                    MollierPoint mollierPoint_Enthalpy = uIMollierPoints[index].MollierPoint;
                    double X_Enthalpy = mollierControlSettings.ChartType == ChartType.Mollier ? mollierPoint_Enthalpy.HumidityRatio * 1000 : mollierPoint_Enthalpy.DryBulbTemperature;
                    double Y_Enthalpy = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(mollierPoint_Enthalpy) : mollierPoint_Enthalpy.HumidityRatio;
                    series.Points.AddXY(X_Enthalpy, Y_Enthalpy);

                    string name_Enthalpy = Query.ToolTipText(mollierPoint_Enthalpy, mollierControlSettings.ChartType, "Enthalpy " + percent.ToString() + "%") + "\nUnmet hours: " + System.Math.Ceiling(percent / 100 * uIMollierPoints.Count).ToString();
                    Modify.CreateMovedLabel(chart, mollierControlSettings, series1.Points[0].XValue, series1.Points[0].YValues[0], 0, 0, 0, -16 * Query.ScaleVector2D(control, mollierControlSettings).Y, 0, 0, name_Enthalpy, Mollier.ChartDataType.Undefined, ChartParameterType.Point, color: Color.Black, tag: "ColorPointLabel");
                    break;
            }
        }
    }
}
