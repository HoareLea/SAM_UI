using System.Drawing;
using System;
using System.Windows.Forms.DataVisualization.Charting;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static Series AddLabel_Unit(this Chart chart, Series series, MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings == null || chart == null)
            {
                return null;
            }

            if(mollierControlSettings.DisableUnits)
            {
                return null;
            }

            ConstantValueCurve constantValueCurve = series?.Tag as ConstantValueCurve;
            if (constantValueCurve == null)
            {
                return null;
            }

            if(series.Points == null || series.Points.Count == 0)
            {
                return null;
            }

            double x = series.Points[0].XValue;
            double y = series.Points[0].YValues[0];

            double value = constantValueCurve.Value;

            ChartDataType chartDataType = constantValueCurve.ChartDataType;

            ChartType chartType = mollierControlSettings.ChartType;

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
                    angle = Query.Angle(series, mollierControlSettings);
                    offset_X = chartType == ChartType.Mollier ? 0.3 : 0.2;
                    offset_Y = chartType == ChartType.Mollier ? -0.2 : 0.0002;
                    break;

                case ChartDataType.Enthalpy:
                    offset_X = chartType == ChartType.Mollier ? 0.3 : 0.2;
                    offset_Y = chartType == ChartType.Mollier ? -0.2 : -0.0002;
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

            if (mollierControlSettings.DisableUnits)
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
            if (mollierControlSettings.ChartType == ChartType.Psychrometric && chartDataType == ChartDataType.SpecificVolume)
            {
                angle = 90 - angle;
            }

            return AddLabel(chart, mollierControlSettings, x, y, angle, offset_X, offset_Y, text, chartDataType, ChartParameterType.Label, tag: series);
        }

        public static Series AddLabel_RelativeHumidity(this Series series, MollierControlSettings mollierControlSettings, int startIndex = 5)
        {
            ConstantValueCurve constantValueCurve = series?.Tag as ConstantValueCurve;
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


            int i = System.Convert.ToInt32(value / 10);

            int startIndex_Temp = startIndex;
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
            }

            if(vector2D == null)
            {
                return null;
            }

            int angle = System.Convert.ToInt32((vector2D.Angle(Vector2D.WorldX)) * 180 / System.Math.PI);
            string label = " Relative Humidity φ";
            series.SmartLabelStyle.Enabled = false;
            if (i == 5)
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
                series.Points[count - startIndex_Temp - i].Label = newLabel;
            }
            else if (!mollierControlSettings.DisableUnits)
            {
                series.Points[count - startIndex_Temp - i].Label = value.ToString() + "%";
            }
            series.Points[count - startIndex_Temp - i].LabelAngle = mollierControlSettings.ChartType == ChartType.Mollier ? - angle : angle - 180;
            series.Points[count - startIndex_Temp - i].LabelForeColor = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Unit, ChartDataType.RelativeHumidity);
        
            return series;
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
            result.LabelAngle = angle;
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
