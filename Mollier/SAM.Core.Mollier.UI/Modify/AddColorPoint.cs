using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        /// <summary>
        /// Highlights and created series for calculated point by % of enthalpy or temperature
        /// on the chart
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="mollierPoints">Mollier Points on chart</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of created series</returns>
        public static List<Series> AddColorPoint(this Chart chart, List<UIMollierPoint> mollierPoints, MollierControlSettings mollierControlSettings)
        {
            if(mollierPoints == null || mollierControlSettings.FindPoint == false)
            {
                return null;
            }
            Point2D colorPoint = mollierPoints.FindPoint(mollierControlSettings);
            if(colorPoint == null)
            {
                return null;
            }
            
            List<Series> result = new List<Series>();

            result.Add(chart.addColorPoint(colorPoint, mollierControlSettings));
            result.Add(chart.addColorPointLabel(colorPoint, mollierControlSettings));

            return result; 
        }
        /// <summary>
        /// Highlights and created series for calculated point by % of enthalpy or temperature
        /// on the chart
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="mollierModel">Mollier points on chart</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns></returns>
        public static List<Series> AddColorPoint(this Chart chart, MollierModel mollierModel, MollierControlSettings mollierControlSettings)
        {
            if (mollierModel == null)
            {
                return null;
            }

            List<UIMollierPoint> uIMollierPoints = mollierModel.GetMollierObjects<UIMollierPoint>();

            return chart.AddColorPoint(uIMollierPoints, mollierControlSettings);
        }
    
        private static Series addColorPoint(this Chart chart, Point2D colorPoint, MollierControlSettings mollierControlSettings)
        {
            Series result = chart.Series.Add(Guid.NewGuid().ToString());
            result.IsVisibleInLegend = false;
            result.ChartType = SeriesChartType.Point;
            result.BorderWidth = 4;
            result.MarkerColor = Color.Red;
            result.MarkerSize = 10;
            result.MarkerStyle = MarkerStyle.Circle;
            result.Tag = "ColorPoint";
            result.Points.AddXY(colorPoint.X, colorPoint.Y);

            return result;
        }
        private static Series addColorPointLabel(this Chart chart, Point2D colorPoint, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;

            Series result = chart.Series.Add(Guid.NewGuid().ToString());
            result.IsVisibleInLegend = false;
            result.ChartType = SeriesChartType.Point;
            result.BorderWidth = 4;
            result.MarkerColor = Color.Red;
            result.MarkerSize = 15;
            result.MarkerStyle = MarkerStyle.Circle;
            result.Tag = "ColorPointLabel";

            if (chartType == ChartType.Mollier)
            {
                result.Points.AddXY(20, 0);
            }
            else
            {
                result.Points.AddXY(0, 15); // TODO: Changed
            }
            result.Label = Query.ToolTipText(colorPoint.ToMollier(chartType, mollierControlSettings.Pressure), chartType);

            return result;
        }
    }
}
