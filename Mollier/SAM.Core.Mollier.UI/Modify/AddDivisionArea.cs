using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        /// <summary>
        /// Creates series for division area lines
        /// More about division area in Query.DivisionArea
        /// </summary>
        /// <param name="chart">Chart</param>
        /// <param name="uIMollierPoints">UIMollierPoints list by which it divides chart</param>
        /// <param name="mollierControlSettings">MollierControlSettings</param>
        /// <returns>List of divided areas lines series</returns>
        public static List<Series> AddDivisionArea(this Chart chart, IEnumerable<UIMollierPoint> uIMollierPoints, MollierControlSettings mollierControlSettings)
        {
            if (mollierControlSettings.DivisionArea == false)
            {
                return null;
            }
            List<Series> result = new List<Series>();
            List<UIMollierZone> divisionAreas = uIMollierPoints.DivisionMollierZones(mollierControlSettings);

            foreach (UIMollierZone divisionArea in divisionAreas)
            {
                result.Add(chart.addDivisionArea(divisionArea, mollierControlSettings));
            }

            return result;
        }
        private static Series addDivisionArea(this Chart chart, UIMollierZone divisionArea, MollierControlSettings mollierControlSettings)
        {
            if (divisionArea == null || divisionArea.MollierPoints == null || divisionArea.MollierPoints.Count < 4)
            {
                return null;
            }

            Series series = chart.Series.Add("DivisionArea " + Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 3;
            series.Color = divisionArea.Color;

            if(!mollierControlSettings.DivisionAreaLabels)
            {
                divisionArea.Text = "";
            }
            series.Tag = divisionArea;

            foreach (MollierPoint mollierPoint in divisionArea.MollierPoints)
            {
                Point2D point = mollierPoint.ToSAM(mollierControlSettings.ChartType);
                series.Points.AddXY(point.X, point.Y);
            }
            Point2D closingPoint = divisionArea.MollierPoints[0].ToSAM(mollierControlSettings.ChartType);
            series.Points.AddXY(closingPoint.X, closingPoint.Y);

            return series;
        }
    }
}