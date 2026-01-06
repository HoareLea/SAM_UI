using SAM.Core.Mollier.UI.Classes;
using System;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        public static Series AddLabel(this Chart chart, ChartLabel chartLabel, MollierControlSettings mollierControlSettings)
        {
            if(chart == null || chartLabel == null)
            {
                return null;
            }

            Series result = chart.Series.Add(Guid.NewGuid().ToString());
            result.SmartLabelStyle.Enabled = false;
            result.IsVisibleInLegend = false;
            result.Color = mollierControlSettings != null && mollierControlSettings.VisualizeSolver ? Color.Red : Color.Transparent;
            result.ChartType = SeriesChartType.Point;
            result.Points.AddXY(chartLabel.Position.X, chartLabel.Position.Y);
            result.Label = chartLabel.Text;
            result.LabelAngle = System.Convert.ToInt32(chartLabel.Angle) % 90;
            result.LabelForeColor = chartLabel.Color;
            result.Tag = chartLabel.Tag;

            return result;
        }
    }
}
