using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        /// <summary>
        /// Creates series for all the zones 
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="mollierZones">Mollier zones</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of created series</returns>
        public static List<Series> AddMollierZones(this Chart chart, IEnumerable<UIMollierZone> mollierZones, MollierControlSettings mollierControlSettings)
        {
            if(mollierZones == null)
            {
                return null;
            }
            List<Series> result = new List<Series>();
            ChartType chartType = mollierControlSettings.ChartType;

            foreach (UIMollierZone uIMollierZone in mollierZones)
            {            
                Series series = chart.Series.Add("Mollier Zone " + System.Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.FastLine;
                series.BorderWidth = 2;
                series.Color = uIMollierZone.UIMollierAppearance.Color;
                series.Tag = uIMollierZone;

                foreach(MollierPoint mollierPoint in uIMollierZone.MollierPoints)
                {
                    Point2D point = Convert.ToSAM(mollierPoint, chartType);
                    series.Points.AddXY(point.X, point.Y);
                }

                result.Add(series);
            }

            return result;
        }

        /// <summary>
        /// Creates series for all the zones 
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="mollierModel">Mollier model</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of created series</returns>
        public static List<Series> AddMollierZones(this Chart chart, MollierModel mollierModel, MollierControlSettings mollierControlSettings)
        {
            if (mollierModel == null)
            {
                return null;
            }

            List<UIMollierZone> uIMollierZones = mollierModel.GetMollierObjects<UIMollierZone>();

            uIMollierZones = uIMollierZones?.FindAll(x => x.UIMollierAppearance.Visible == true);
            return chart.AddMollierZones(uIMollierZones, mollierControlSettings);
        }
    }
}
