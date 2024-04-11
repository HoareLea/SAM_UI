using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        public static Series AddMollierPoint(this Chart chart, ChartType chartType, UIMollierPoint uIMollierPoint, MollierControlSettings mollierControlSettings, DisplayPointType displayPointType = DisplayPointType.Undefined)
        {
            if(chart == null || chartType == ChartType.Undefined || uIMollierPoint == null)
            {
                return null;
            }

            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            Point2D point2D = Convert.ToSAM(uIMollierPoint, chartType);
            
            int index = series.Points.AddXY(point2D.X, point2D.Y);

            UIMollierPointAppearance uIMollierPointAppearance = uIMollierPoint.UIMollierAppearance as UIMollierPointAppearance;
            if (displayPointType != DisplayPointType.Undefined)
            {
                uIMollierPointAppearance = Create.UIMollierPointAppearance(displayPointType, uIMollierPointAppearance.Label);
            }

            Color color = uIMollierPointAppearance.Color;
            int size = uIMollierPointAppearance.Size;
            if (mollierControlSettings != null && !Core.Query.Similar(mollierControlSettings.PointColor, Color.Empty))
            {
                color = mollierControlSettings.PointColor;
            }

            if(color == Color.Empty)
            {
                color = series.Color;
            }

            if (mollierControlSettings != null && mollierControlSettings.PointSize != -1)
            {
                size = mollierControlSettings.PointSize;
            }

            if (size < 1)
            {
                size = series.MarkerSize;
            }

            if (mollierControlSettings != null && mollierControlSettings.DisablePoint)
            {
                color = Color.Empty;
                size = 0;
            }

            series.Color = color;

            series.MarkerSize = size;
            series.MarkerColor = color;

            Color borderColor = uIMollierPointAppearance.BorderColor;
            int borderWidth = uIMollierPointAppearance.BorderSize;
            if (mollierControlSettings != null && !Core.Query.Similar(mollierControlSettings.PointBorderColor, Color.Empty))
            {
                borderColor = mollierControlSettings.PointBorderColor;
            }

            if(borderColor == Color.Empty)
            {
                borderColor = series.MarkerBorderColor;
            }

            if (mollierControlSettings != null && mollierControlSettings.PointBorderSize != -1)
            {
                borderWidth = mollierControlSettings.PointBorderSize;
            }

            if (borderWidth < 1)
            {
                borderWidth = series.MarkerBorderWidth;
            }

            if (mollierControlSettings != null && mollierControlSettings.DisablePointBoarder)
            {
                borderColor = Color.Empty;
                borderWidth = 0;
            }

            series.MarkerBorderWidth = borderWidth;
            series.MarkerBorderColor = borderColor;

            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Point;

            series.MarkerStyle = MarkerStyle.Circle;
            series.Points[index].ToolTip = Query.ToolTipText(uIMollierPoint, chartType);
            series.Tag = uIMollierPoint;

            return series;
        }
    }
}
