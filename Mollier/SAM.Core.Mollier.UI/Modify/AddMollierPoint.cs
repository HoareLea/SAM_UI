using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        public static Series AddMollierPoint(this Chart chart, ChartType chartType, UIMollierPoint uIMollierPoint, bool disableBoarder = false)
        {
            if(chart == null || chartType == ChartType.Undefined || uIMollierPoint == null)
            {
                return null;
            }

            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            Point2D point2D = Convert.ToSAM(uIMollierPoint, chartType);
            
            int index = series.Points.AddXY(point2D.X, point2D.Y);

            UIMollierPointAppearance uIMollierPointAppearance = uIMollierPoint.UIMollierAppearance as UIMollierPointAppearance;

            series.MarkerSize = uIMollierPointAppearance.Size == -1 ? series.MarkerSize : uIMollierPointAppearance.Size;
            series.Color = uIMollierPointAppearance.Color == Color.Empty ? series.Color : uIMollierPointAppearance.Color;

            if(disableBoarder)
            {
                series.MarkerBorderWidth = 0;
                series.MarkerBorderColor = Color.Empty;
            }
            else
            {
                series.MarkerBorderWidth = uIMollierPointAppearance.BorderSize == -1 ? series.MarkerSize : uIMollierPointAppearance.BorderSize;
                series.MarkerBorderColor = uIMollierPointAppearance.BorderColor == Color.Empty ? series.MarkerBorderColor : uIMollierPointAppearance.BorderColor;
            }

            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Point;

            series.MarkerStyle = MarkerStyle.Circle;
            series.Points[index].ToolTip = Query.ToolTipText(uIMollierPoint, chartType, uIMollierPointAppearance.Label);
            series.Tag = uIMollierPoint;

            return series;
        }
    }
}
