using System.Drawing;
using System;
using System.Windows.Forms.DataVisualization.Charting;
using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static Series AddLabel_Unit(this Chart chart, Series series, MollierControlSettings mollierControlSettings)
        {
            if (mollierControlSettings == null || chart == null)
            {
                return null;
            }

            if (mollierControlSettings.DisableUnits)
            {
                return null;
            }

            ConstantValueCurve constantValueCurve = series?.Tag as ConstantValueCurve;
            if (constantValueCurve == null)
            {
                return null;
            }

            if (series.Points == null || series.Points.Count == 0 || series.Points.Count == 1)
            {
                return null;
            }

            double x = System.Math.Max(series.Points[0].XValue, series.Points[1].XValue);
            double y = System.Math.Min(series.Points[0].YValues[0], series.Points[1].YValues[0]);

            double value = constantValueCurve.Value;

            ChartDataType chartDataType = constantValueCurve.ChartDataType;


            ChartType chartType = mollierControlSettings.ChartType;
            if(chartDataType == ChartDataType.Density && chartType == ChartType.Mollier)
            {
                x = System.Math.Min(series.Points[0].XValue, series.Points[1].XValue);
                y = System.Math.Max(series.Points[0].YValues[0], series.Points[1].YValues[0]);
            }

            double offset_X = 0;
            double offset_Y = 0;
            int angle = 0;
            switch(chartDataType)
            {
                case ChartDataType.SpecificVolume:
                    angle = Query.Angle(series, mollierControlSettings);
                    if (chartType == ChartType.Psychrometric)
                    {
                        angle = 90 - angle;
                    }

                    offset_X = chartType == ChartType.Mollier ? -0.5 : 0.7;
                    offset_Y = chartType == ChartType.Mollier ? 0 : -0.0015;
                    x = series.Points[series.Points.Count - 1].XValue;
                    y = series.Points[series.Points.Count - 1].YValues[0];
                    break;

                case ChartDataType.WetBulbTemperature:
                    offset_X = chartType == ChartType.Mollier ? 0 : -0.45;
                    offset_Y = chartType == ChartType.Mollier ? -1.2 : -0.00035;
                    x = series.Points[series.Points.Count - 1].XValue;
                    y = series.Points[series.Points.Count - 1].YValues[0];
                    break;

                case ChartDataType.Density:
                    return null;
                    angle = Query.Angle(series, mollierControlSettings); 
                    offset_X = chartType == ChartType.Mollier ? 0.3 : 0.2;
                    offset_Y = chartType == ChartType.Mollier ? -0.7 : 0.0002;
                    break;

                case ChartDataType.Enthalpy:
                    offset_X = chartType == ChartType.Mollier ? 1 : -0.8;
                    offset_Y = chartType == ChartType.Mollier ? -4 : -0.0004;
                    value /= 1000;
                    if (value % 10 != 0)
                    {
                        return null;
                    }
                    if(x == 0 || chartType == ChartType.Psychrometric)
                    {
                        x = series.Points[series.Points.Count - 1].XValue;
                        y = series.Points[series.Points.Count - 1].YValues[0];
                    }

                    break;

            }

            return AddLabel(chart, mollierControlSettings, x, y, angle, offset_X, offset_Y, value.ToString(), chartDataType, ChartParameterType.Unit, tag: series);
        }

        public static Series AddLabel_Label(this Chart chart, Series series, MollierControlSettings mollierControlSettings, string text, double offset_X, double offset_Y, int pointIndex = 0)
        {
            if (mollierControlSettings == null || chart == null || string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (mollierControlSettings.DisableLabels)
            {
                return null;
            }

            ConstantValueCurve constantValueCurve = series?.Tag as ConstantValueCurve;
            if (constantValueCurve == null)
            {
                return null;
            }

            if (series.Points == null || series.Points.Count == 0)
            {
                return null;
            }

            ChartDataType chartDataType = constantValueCurve.ChartDataType;

            double x = series.Points[pointIndex].XValue;
            double y = series.Points[pointIndex].YValues[0];

            int angle = Query.Angle(series, mollierControlSettings);

            if(chartDataType == ChartDataType.Enthalpy && mollierControlSettings.ChartType == ChartType.Mollier)
            {
                angle = 27;
            }

            if (chartDataType == ChartDataType.Enthalpy && mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                angle = 20;
            }
            //int angle = 10;
            // int angle = 29;
            if (mollierControlSettings.ChartType == ChartType.Psychrometric && chartDataType == ChartDataType.SpecificVolume)
            {
                angle = 90 - angle;
            }

            return AddLabel(chart, mollierControlSettings, x, y, angle, offset_X, offset_Y, text, chartDataType, ChartParameterType.Label, tag: series);
        }

        public static Series AddLabel_RelativeHumidity(this Series series, ChartArea chartArea, MollierControlSettings mollierControlSettings, int startIndex = 5)
        {
            ConstantValueCurve constantValueCurve = series?.Tag as ConstantValueCurve;
            ChartType chartType = mollierControlSettings.ChartType;
            if (constantValueCurve == null || series.Points == null)
            {
                return null;
            }

            int count = series.Points.Count;
            if (count < 2)
            {
                return null;
            }

            double value = constantValueCurve.Value;
            if(value == 0)
            {
                return null;
            }


            double labelLocationPercent = 0.8;
            int labelLocationIndex = System.Convert.ToInt32(count * labelLocationPercent); 

            double xMin = chartArea.AxisX.Minimum;
            double xMax = chartArea.AxisX.Maximum;
            double yMin = chartArea.AxisY.Minimum;
            double yMax = chartArea.AxisY.Maximum;


            double xValue = xMin + labelLocationPercent * (xMax - xMin);

            double min = double.MaxValue;

            List<DataPoint> relativeHumidityPoints = new List<DataPoint>();
            for (int i = 0; i < series.Points.Count; i++)
            {
                if (series.Points[i].XValue > xMax || series.Points[i].XValue < xMin || series.Points[i].YValues[0] < yMin || series.Points[i].YValues[0] > yMax)
                {
                    continue;
                }
                relativeHumidityPoints.Add(series.Points[i]);
            }

            if(relativeHumidityPoints.Count < 5)
            {
                return null;
            }


            
            int index = value > 60 ? System.Convert.ToInt32(relativeHumidityPoints.Count - 3) : System.Convert.ToInt32(relativeHumidityPoints.Count - 4);

            int ID = series.Points.ToList().IndexOf(relativeHumidityPoints[index]);

   

            Point2D point2 = new Point2D(series.Points[ID].XValue, series.Points[ID].YValues[0]);

            Point2D point1 = ID == 0 ? point2 : new Point2D(series.Points[ID - 1].XValue, series.Points[ID - 1].YValues[0]);
            Point2D point3 = ID == count - 1 ? point2 : new Point2D(series.Points[ID + 1].XValue, series.Points[ID + 1].YValues[0]);

            Vector2D vector2D = null;
            double range_difference = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min);
            switch (mollierControlSettings.ChartType)
            {
                case ChartType.Mollier:
                    range_difference *= 2;
                    point1.X *= range_difference;
                    point3.X *= range_difference;
                    vector2D = new Vector2D(point3, point1);
                    break;

                case ChartType.Psychrometric:
                    point3.X = 2 * point3.X - point1.X;
                    point3.Y *= 1000 * range_difference;
                    point1.Y *= 1000 * range_difference;
                    vector2D = new Vector2D(point1, point3);
                    break;
            }
            if (vector2D == null)
            {
                return null;
            }

            int angle = 0;
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                angle = - (90 - (System.Convert.ToInt32((vector2D.Angle(Vector2D.WorldX)) * 180 / System.Math.PI) % 90));
            }
            else
            {
                angle = -(System.Convert.ToInt32((vector2D.Angle(Vector2D.WorldX)) * 180 / System.Math.PI) ) % 90;
            }
            
            string label = " Relative Humidity φ";
            series.SmartLabelStyle.Enabled = false;
            if (constantValueCurve.Value == 50)
            {
                string newLabel = string.Empty;
                if (!mollierControlSettings.DisableUnits)
                {
                    newLabel += value.ToString() + " %";
                }
                if (!mollierControlSettings.DisableLabels)
                {
                    newLabel += label;
                }
                series.Points[ID].Label = newLabel;
            }
            else if (!mollierControlSettings.DisableUnits)
            {
                series.Points[ID].Label = value.ToString() + "%";
            }
            //  series.Points[ID].LabelAngle = mollierControlSettings.ChartType == ChartType.Mollier ? -angle : angle - 180;
            series.Points[ID].LabelAngle = angle;
            series.Points[ID].LabelForeColor = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Unit, ChartDataType.RelativeHumidity);

            return series;



          /* int startIndex_Temp = startIndex;
            while(count - (startIndex_Temp + 1) - i < 0)
            {
                startIndex_Temp--;
            }

            double range_difference = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min);
            Point2D point2D_1 = series.Points[count - (startIndex_Temp - 1) - i].ToSAM();
            Point2D point2D_2 = series.Points[count - (startIndex_Temp + 1) - i].ToSAM();

            Vector2D vector2D = null;
            switch (mollierControlSettings.ChartType)
            {
                case ChartType.Mollier:
                    range_difference *= 2;
                    point2D_1.X *= range_difference;
                    point2D_2.X *= range_difference;
                    vector2D = new Vector2D(point2D_2, point2D_1);
                    break;

                case ChartType.Psychrometric:
                    point2D_2.X = 2 * point2D_2.X - point2D_1.X;
                    point2D_2.Y *= 1000 * range_difference;
                    point2D_1.Y *= 1000 * range_difference;
                    vector2D = new Vector2D(point2D_1, point2D_2);
                    break;
            }*/


        }

        public static Series AddLabel(this Chart chart, MollierControlSettings mollierControlSettings, double x, double y, int angle, double offset_X, double offset_Y, string text, ChartDataType chartDataType, ChartParameterType chartParameterType, Color? color = null, object tag = null, Font font = null, SeriesChartType seriesChartType = SeriesChartType.Spline)
        {
            if(chart == null || mollierControlSettings == null || string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            Series result = chart.Series.Add(string.Format(text + chartDataType.ToString() + Guid.NewGuid().ToString()));
            result.IsVisibleInLegend = false;
            result.ChartType = seriesChartType;
            result.Color = Color.Transparent;
            result.SmartLabelStyle.Enabled = false;
            result.Points.AddXY(x + offset_X, y + offset_Y);
            result.Label = text;
            result.LabelAngle = angle % 90;
            result.LabelForeColor = color != null ? (Color)color : mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, chartParameterType, chartDataType);
            result.Tag = tag;

            if (font != null)
            {
                result.Font = font;
            }

            return result;
        }

    }
}
