using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System;
using SAM.Geometry.Planar;
using SAM.Geometry.Mollier;

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
        /// <summary>
        /// Creates series for division area lines
        /// More about division area in Query.DivisionArea
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="mollierModel"></param>
        /// <param name="mollierControlSettings"></param>
        /// <returns>List of created series</returns>
        public static List<Series> AddDivisionArea(this Chart chart, MollierModel mollierModel, MollierControlSettings mollierControlSettings)
        {
            if (mollierModel == null)
            {
                return null;
            }

            List<UIMollierPoint> uIMollierPoints = mollierModel.GetMollierObjects<UIMollierPoint>();

            return chart.AddDivisionArea(uIMollierPoints, mollierControlSettings);
        }
        private static Series addDivisionArea(this Chart chart, UIMollierZone divisionArea, MollierControlSettings mollierControlSettings)
        {
            if (divisionArea == null || divisionArea == null)
            {
                return null;
            }
            if(divisionArea.MollierPoints == null || divisionArea.MollierPoints.Count < 4)
            {
                return null;
            }

            Series series = chart.Series.Add("DivisionArea " + Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 3;
            series.Color = divisionArea.UIMollierAppearance.Color;

            if(!mollierControlSettings.DivisionAreaLabels)
            {
                string text = "";
                divisionArea.UIMollierAppearance = new UIMollierAppearance(divisionArea.UIMollierAppearance.Color, text);
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