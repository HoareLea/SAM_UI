using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static void AddMollierZones(this Chart chart, IEnumerable<MollierZone> mollierZones, MollierControlSettings mollierControlSettings)
        {
            if(mollierZones == null)
            {
                return;
            }

            foreach (MollierZone mollierZone in mollierZones)
            {
                Series series = chart.Series.Add("Mollier Zone " + System.Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 2;
                series.Color = System.Drawing.Color.Blue;
                string zoneText = "";

                if (mollierZone is MollierControlZone)
                {
                    MollierControlZone mollierControlZone = (MollierControlZone)mollierZone;
                    series.Color = mollierControlZone.Color;
                    zoneText = mollierControlZone.Text;
                }

                List<MollierPoint> mollierPoints = mollierZone.MollierPoints;
                ChartType chartType = mollierControlSettings.ChartType;
                int size = mollierPoints.Count;
                for (int i = 0; i < size; i++)
                {
                    MollierPoint mollierPoint = mollierPoints[i];
                    Point2D point = Convert.ToSAM(mollierPoint, chartType);
                    series.Points.AddXY(point.X, point.Y);
                }

                MollierPoint zoneCenterMollierPoint = mollierZone.GetCenter();
                Point2D coneCenterPoint = Convert.ToSAM(zoneCenterMollierPoint, chartType);

                AddLabel(chart, mollierControlSettings, coneCenterPoint.X, coneCenterPoint.Y, 0, 0, 0, zoneText, ChartDataType.Undefined,
                         ChartParameterType.Undefined, color: System.Drawing.Color.Black);
            }
        }
    }
}
