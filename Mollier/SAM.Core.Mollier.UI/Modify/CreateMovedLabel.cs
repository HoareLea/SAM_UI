using SAM.Core.Mollier.UI.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        [Obsolete("To be Changed Maciek")] // TODO: Change (MollierProcess)
        public static void CreateMovedLabel(Chart chart, MollierControlSettings mollierControlSettings, double X, double Y, int Mollier_angle, int Psychrometric_angle, double Mollier_X, double Mollier_Y, double Psychrometric_X, double Psychrometric_Y, string LabelContent, ChartDataType chartDataType, ChartParameterType chartParameterType, bool fontChange = false, Color? color = null, string tag = null)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            double x = chartType == ChartType.Mollier ? Mollier_X : Psychrometric_X;
            double y = chartType == ChartType.Mollier ? Mollier_Y : Psychrometric_Y;
            //X, Y - coordinates of the point before moveing by x and y

            Series new_label = chart.Series.Add(String.Format(LabelContent + chartDataType.ToString() + Guid.NewGuid().ToString()));
            new_label.IsVisibleInLegend = false;
            new_label.ChartType = SeriesChartType.Spline;
            if (tag == "ColorPointLabel")//in save as pdf we want to move this label(colorpointlabel) so it has to be point not spline
            {
                new_label.ChartType = SeriesChartType.Point;
            }
            new_label.Color = Color.Transparent;
            new_label.SmartLabelStyle.Enabled = false;

            new_label.Points.AddXY(X + x, Y + y);
            new_label.Label = LabelContent;
            new_label.LabelAngle = chartType == ChartType.Mollier ? Mollier_angle % 90 : Psychrometric_angle % 90;
            new_label.LabelForeColor = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, chartParameterType, chartDataType);
            if (color != null)
            {
                new_label.LabelForeColor = (Color)color;
            }
            if (fontChange)
            {
                new_label.Font = SystemFonts.MenuFont;
            }
            if (tag != null)
            {
                new_label.Tag = tag;
            }
        }

    }
}
