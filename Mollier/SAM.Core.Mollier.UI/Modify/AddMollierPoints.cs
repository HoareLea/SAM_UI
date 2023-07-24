using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static Series AddMollierPoints(this Chart chart, IEnumerable<UIMollierPoint> uIMollierPoints, MollierControlSettings mollierControlSettings)
        {
            if(uIMollierPoints == null || mollierControlSettings.DivisionArea) 
            {
                return null;
            }

            if (mollierControlSettings == null || chart == null)
            {
                return null;
            }

            Series result = chart.Series.ToList().Find(x => x.Name == "MollierPoints"); 
            if (result == null)
            {
                result = chart.Series.Add("MollierPoints");
                result.IsVisibleInLegend = false;
                result.ChartType = SeriesChartType.Point;
                int index_Temp = result.Points.AddXY(1, 0); //Has to be added to properly show first point on HumidityRatio = 0
                result.Points[index_Temp].MarkerSize = 0;
            }
            else
            {
                result.Points.Clear();
            }
            result.Tag = uIMollierPoints;

            
            Dictionary<MollierPoint, int> dictionary = new Dictionary<MollierPoint, int>();
            double maxCount = 0;
            List<MollierPoint>[,] rectangles_points;
            PointGradientVisibilitySetting pointGradientVisibilitySetting = mollierControlSettings.VisibilitySettings.GetVisibilitySetting("User", ChartParameterType.Point) as PointGradientVisibilitySetting;
            if (pointGradientVisibilitySetting != null)
            {
                dictionary = Query.NeighborhoodCount((uIMollierPoints as List<UIMollierPoint>).ConvertAll(x => x.MollierPoint), out maxCount, out rectangles_points);
            }
            else
            {
                result.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Point, ChartDataType.Undefined);
            }


            ChartType chartType = mollierControlSettings.ChartType;
            foreach(UIMollierPoint uIMollierPoint in uIMollierPoints)
            {
                MollierPoint mollierPoint = uIMollierPoint?.MollierPoint;
                if(mollierPoint == null)
                {
                    continue;
                }

                Point2D point = Convert.ToSAM(mollierPoint, chartType);
                if(result.Contains(point.X, point.Y, Tolerance.MacroDistance))
                {
                    continue;
                }
                int index = result.Points.AddXY(point.X, point.Y);

                List<MollierPoint> testList = dictionary.Keys.ToList();

                foreach(MollierPoint test in testList)
                {
                        bool boolean = test == mollierPoint; // TU POWINNO BYĆ W JAKIMS PRZYBLIZENIU SPRAWDZANE BO NIE ZNAJDUJE MIMO TYCH SAMYCH WSPOLRZEDNYCH
                                                          // TODO: GRADIENT POINT BUG
                }

                // Checking whether gradient point is activated 
                if (pointGradientVisibilitySetting != null)
                {
                    double value = maxCount == 0 ? 0 : System.Convert.ToDouble(dictionary[mollierPoint]) / maxCount;
                    result.Points[index].Color = Core.Query.Lerp(pointGradientVisibilitySetting.Color, pointGradientVisibilitySetting.GradientColor, value);
                }
                result.Points[index].ToolTip = Query.ToolTipText(mollierPoint, chartType, null);
                result.Points[index].Tag = mollierPoint;
                result.Points[index].MarkerSize = 7; //TODO: Change size of marker and make it const
                result.Points[index].MarkerStyle = MarkerStyle.Circle;

                if (uIMollierPoint.UIMollierAppearance != null)
                {
                    if (uIMollierPoint.UIMollierAppearance.Color != System.Drawing.Color.Empty)
                    {
                        result.Points[index].Color = uIMollierPoint.UIMollierAppearance.Color;
                    }

                    if (!string.IsNullOrWhiteSpace(uIMollierPoint.UIMollierAppearance.Label))
                    {
                        result.Points[index].Label = uIMollierPoint.UIMollierAppearance.Label;
                    }
                }

            }
                return result;

        }
    }
}
