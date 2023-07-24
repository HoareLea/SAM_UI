using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        [Obsolete("To be changed Maciek")] // TODO: Change (MollierProcess)

        //TODO: [Maciek] Use/move to SAM.Core.Mollier.Default -> Remember to use SI units!
        // enthalpy in jules  * 1000
        // DO ZMIANY NA JAKIES CONSTY:
        private static int deltaRelativeHumidity = 10;//i interval from neighborhoodcount
        private static int deltaEnthalpy = 3000;//enthalpy interval from neighborhoodcount
        private static int relativeHumidity_size = 100 / deltaRelativeHumidity + 7; // base size (number of rectangles / areas)
        private static int enthalpy_size = 200 / deltaEnthalpy + 7;
        private static int relativeHumidityMin = 0;
        private static int relativeHumidityMax = 100;
        private static int enthalpyMin = -39000;
        private static int enthalpyMax = 140000;

        public static List<Series> AddDivisionArea(this Chart chart, IEnumerable<UIMollierPoint> uIMollierPoints, MollierControlSettings mollierControlSettings)
        {
            if (mollierControlSettings.DivisionArea == false)
            {
                return null;
            }
       
            List<MollierPoint>[,] pointsInAreas = new List<MollierPoint>[relativeHumidity_size, enthalpy_size];//for every i interval and every enthalpy interval it stores the list of points that belong to this area 
            double maxPointsNumberInOneArea;
            Query.NeighborhoodCount((uIMollierPoints as List<UIMollierPoint>)?.ConvertAll(x => x.MollierPoint), out maxPointsNumberInOneArea, out pointsInAreas);

            return createDivisionAreaSeries(chart, pointsInAreas, mollierControlSettings, maxPointsNumberInOneArea);
        }


        //TODO: [Maciek] Change to  func inside AddDivisionArea
        private static List<Series> createDivisionAreaSeries(Chart chart, List<MollierPoint>[,] pointsInAreas, MollierControlSettings mollierControlSettings, double maxPointsNumberInOneArea)
        {
            List<Series> result = new List<Series>();
            ChartType chartType = mollierControlSettings.ChartType;

            for (int i = relativeHumidityMin = 0; i <= relativeHumidityMax - deltaRelativeHumidity; i += deltaRelativeHumidity)
            {
                for (int j = enthalpyMin; j <= enthalpyMax - deltaEnthalpy; j += deltaEnthalpy)
                {
                    Tuple<int, int> areaIndex = getAreaIndex(i, j);
                    if (pointsInAreas[areaIndex.Item1, areaIndex.Item2] == null)
                    {
                        continue;
                    }

                    Series series = chart.Series.Add("DivisionAreas " + Guid.NewGuid().ToString());

                    series.IsVisibleInLegend = false;
                    series.Tag = "GradientZone";
                    double pressure = mollierControlSettings.Pressure;
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(i, j, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(i, j, "Y", chartType, pressure));//first corner                
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(i, j + deltaEnthalpy, "X", chartType, pressure), 
                                        Query.FindDivisionAreaCornerPoints(i, j + deltaEnthalpy, "Y", chartType, pressure));//second corner               
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(i + deltaRelativeHumidity, j + deltaEnthalpy, "X", chartType, pressure), 
                                        Query.FindDivisionAreaCornerPoints(i + deltaRelativeHumidity, j + deltaEnthalpy, "Y", chartType, pressure));//third corner
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(i + deltaRelativeHumidity, j, "X", chartType, pressure), 
                                        Query.FindDivisionAreaCornerPoints(i + deltaRelativeHumidity, j, "Y", chartType, pressure));//fourth corner
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(i, j, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(i, j, "Y", chartType, pressure));//first corner again to close the zone

                    double value = maxPointsNumberInOneArea == 0 ? 0 : System.Convert.ToDouble(System.Convert.ToInt32(System.Math.Log(pointsInAreas[areaIndex.Item1, areaIndex.Item2].Count))) / maxPointsNumberInOneArea;
                    series.Color = Core.Query.Lerp(System.Drawing.Color.Red, System.Drawing.Color.Blue, value);
                    series.ChartType = SeriesChartType.Line;
                    series.BorderWidth = 3;


                    result.Add(series);
                    if (mollierControlSettings.DivisionAreaLabels)
                    {
                        result.Add(labelDivisionAreas(chart, pointsInAreas, mollierControlSettings, i, j));
                    }
                }
            }
            return result;
        }

        //TODO: [Maciek] Change to  func inside AddDivisionArea
        private static Series labelDivisionAreas(Chart chart, List<MollierPoint>[,] pointsInAreas, MollierControlSettings mollierControlSettings, double relativeHumidity, double enthalpy )
        {
            ChartType chartType = mollierControlSettings.ChartType;
            double pressure = mollierControlSettings.Pressure;
            Tuple<int, int> areaIndex = getAreaIndex(relativeHumidity, enthalpy);

            Series labelsSeries = chart.Series.Add("DivisionAreaLabels " + Guid.NewGuid().ToString());
            labelsSeries.IsVisibleInLegend = false;
            labelsSeries.ChartType = SeriesChartType.Point;
            if (chartType == ChartType.Mollier)
            {
                labelsSeries.Points.AddXY(Query.FindDivisionAreaCornerPoints(relativeHumidity + deltaRelativeHumidity / 2, 
                                                                            enthalpy + deltaEnthalpy / 2, "X", chartType, pressure), 
                                          Query.FindDivisionAreaCornerPoints(relativeHumidity + deltaRelativeHumidity / 2, 
                                                                            enthalpy + deltaEnthalpy / 2, "Y", chartType, pressure) - 0.5);
            }
            else
            {
                labelsSeries.Points.AddXY(Query.FindDivisionAreaCornerPoints(relativeHumidity + deltaRelativeHumidity / 2, 
                                                                             enthalpy + deltaEnthalpy / 2, "X", chartType, pressure), 
                                          Query.FindDivisionAreaCornerPoints(relativeHumidity + deltaRelativeHumidity / 2, 
                                                                             enthalpy + deltaEnthalpy / 2, "Y", chartType, pressure));
            }
            labelsSeries.Color = System.Drawing.Color.Transparent;
            labelsSeries.Label = pointsInAreas[areaIndex.Item1, areaIndex.Item2].Count.ToString();
            labelsSeries.Tag = "GradientZoneLabel";
            return labelsSeries;
        }

        //TODO: [Maciek] Change to  func inside AddDivisionArea
        private static Tuple<int, int> getAreaIndex(double relativeHumidity, double enthalpy)
        {
            // At the beginning we offset to positive numbers and then gitting index
            int index_1 = System.Convert.ToInt32((relativeHumidity - relativeHumidityMin) / deltaRelativeHumidity); // zamiana i i e na indexy zrobic funkcje do tego
            int index_2 = System.Convert.ToInt32((enthalpy - enthalpyMin) / deltaEnthalpy);
            return new Tuple<int, int>(index_1, index_2);
        }
    }

}
