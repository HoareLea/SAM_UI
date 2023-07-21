using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        [Obsolete("To be changed Maciek")] // TODO: Change (MollierProcess)
        public static void AddDivisionArea(this Chart chart, IEnumerable<UIMollierPoint> uIMollierPoints, MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings.DivisionArea == false)
            {
                return;
            }
            int deltaRelativeHumidity = 10;//RH interval from neighborhoodcount
            int deltaEnthalpy = 3;//enthalpy interval from neighborhoodcount
            ChartType chartType = mollierControlSettings.ChartType;

            //base size
            int RH_size = 100 / deltaRelativeHumidity + 7;
            int Ent_size = 200 / deltaEnthalpy + 7;

            List<MollierPoint>[,] rectangles_points = new List<MollierPoint>[RH_size, Ent_size];//for every rh interval and every enthalpy interval it stores the list of points that belong to this area 
            double maxCount;
            Query.NeighborhoodCount((uIMollierPoints as List<UIMollierPoint>)?.ConvertAll(x => x.MollierPoint), out maxCount, out rectangles_points);

            for (int rh = 0; rh <= 100 - deltaRelativeHumidity; rh += 10)
            {
                for (int e = -39; e <= 140 - deltaEnthalpy; e += 3)
                {
                    int index_1 = rh / deltaRelativeHumidity;
                    int index_2 = e / deltaEnthalpy + 15;
                    if (rectangles_points[index_1, index_2] == null)
                    {
                        continue;
                    }

                    Series series = chart.Series.Add(System.Guid.NewGuid().ToString());
                    series.IsVisibleInLegend = false;
                    series.Tag = "GradientZone";
                    double pressure = mollierControlSettings.Pressure;
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh, e, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh, e, "Y", chartType, pressure));//first corner                
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh, e + deltaEnthalpy, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh, e + deltaEnthalpy, "Y", chartType, pressure));//second corner               
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e + deltaEnthalpy, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e + deltaEnthalpy, "Y", chartType, pressure));//third corner
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e, "Y", chartType, pressure));//fourth corner
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh, e, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh, e, "Y", chartType, pressure));//first corner again to close the zone

                    double value = maxCount == 0 ? 0 : System.Convert.ToDouble(System.Convert.ToInt32(System.Math.Log(rectangles_points[index_1, index_2].Count))) / maxCount;
                    series.Color = Core.Query.Lerp(System.Drawing.Color.Red, System.Drawing.Color.Blue, value);
                    series.ChartType = SeriesChartType.Line;
                    series.BorderWidth = 3;
                    if (mollierControlSettings.DivisionAreaLabels)
                    {
                        Series label = chart.Series.Add(System.Guid.NewGuid().ToString());
                        label.IsVisibleInLegend = false;
                        label.ChartType = SeriesChartType.Point;
                        if (chartType == ChartType.Mollier)
                        {
                            label.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "Y", chartType, pressure) - 0.5);
                        }
                        else
                        {
                            label.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "Y", chartType, pressure));
                        }
                        label.Color = System.Drawing.Color.Transparent;
                        label.Label = rectangles_points[index_1, index_2].Count.ToString();
                        label.Tag = "GradientZoneLabel";

                    }
                }
            }
        }
    }
}
