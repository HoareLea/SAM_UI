using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static List<Series> AddMollierZones(this Chart chart, IEnumerable<UIMollierZone> mollierZones, MollierControlSettings mollierControlSettings)
        {
            if(mollierZones == null)
            {
                return null;
            }
            List<Series> result = new List<Series>();
            ChartType chartType = mollierControlSettings.ChartType;

            foreach (MollierZone mollierZone in mollierZones)
            {
                UIMollierZone uIMollierZone = mollierZone is UIMollierZone ? (UIMollierZone)mollierZone 
                : new UIMollierZone(mollierZone, mollierControlSettings.UIMollierZoneColor, mollierControlSettings.UIMollierZoneText);
                
                Series series = chart.Series.Add("Mollier Zone " + System.Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 2;
                series.Color = uIMollierZone.Color;
                series.Tag = uIMollierZone;

                foreach(MollierPoint mollierPoint in mollierZone.MollierPoints)
                {
                    Point2D point = Convert.ToSAM(mollierPoint, chartType);
                    series.Points.AddXY(point.X, point.Y);
                }

                result.Add(series);
            }

            return result;
        }
    
        public static List<Series> AddMollierZones(this Chart chart, MollierModel mollierModel, MollierControlSettings mollierControlSettings)
        {
            if (mollierModel == null)
            {
                return null;
            }

            List<UIMollierZone> uIMollierZones = mollierModel.GetMollierObjects<UIMollierZone>();

            return chart.AddMollierZones(uIMollierZones, mollierControlSettings);
        }
    }
}
