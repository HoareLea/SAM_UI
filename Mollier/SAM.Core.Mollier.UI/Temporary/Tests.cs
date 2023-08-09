using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using SAM.Geometry.Planar;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public static partial class Temporary
    {
        private static BoundingBox2D chartArea;

        private static double yOYToOXDefaultMollierRelation(ChartType chartType)
        {
            double realOYdistance = 13.5;
            double defaultOYInterval = Default.DryBulbTemperature_Max - Default.DryBulbTemperature_Min;
            double realOXdistance = 30.45;
            double defaultOXInterval = Default.HumidityRatio_Max - Default.HumidityRatio_Min;

            double scaleX = defaultOXInterval / realOXdistance;
            double scaleY = defaultOYInterval / realOYdistance;


            if(chartType == ChartType.Psychrometric)
            {
                return 1000;
            }
            return scaleX / scaleY;
        }
        public static Point2D rotatePointByAngle(Point2D origin, Point2D point, double angle, double yToXRelation = 1)
        {
            Point2D scaledPoint = new Point2D(point.X, point.Y * yToXRelation);
            Point2D scaledOrigin = new Point2D(origin.X, origin.Y * yToXRelation);

            double x = scaledPoint.X - scaledOrigin.X;
            double y = scaledPoint.Y - scaledOrigin.Y;
            double cos = System.Math.Cos(angle / 180 * System.Math.PI);
            double sin = System.Math.Sin(angle / 180 * System.Math.PI);

            return new Point2D(x * cos - y * sin + scaledOrigin.X, (x * sin + y * cos + scaledOrigin.Y) / yToXRelation );
        }
        public static void Tests(Control control, Chart chart, MollierControlSettings mollierControlSettings)
        {


            List<Point2D> points = new List<Point2D>();
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);
            Point2D center = new Point2D(5, 5);
            double width = 0.8 * scaleVector.X;
            double height = 0.95 * scaleVector.Y;
            double yToXRelation = yOYToOXDefaultMollierRelation(mollierControlSettings.ChartType);

            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 1;
            series.Color = Color.Red;

            Series series2 = chart.Series.Add(Guid.NewGuid().ToString());
            series2.IsVisibleInLegend = false;
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 1;
            series2.Color = Color.Blue;


            points.Add(new Point2D(center.X - width / 2, center.Y + height / 2));
            points.Add(new Point2D(center.X - width / 2, center.Y - height / 2));
            points.Add(new Point2D(center.X + width / 2, center.Y - height / 2));
            points.Add(new Point2D(center.X + width / 2, center.Y + height / 2));


            List<Point2D> rotatedPoints = new List<Point2D>();

            foreach (Point2D point in points)
            {
                Point2D point2 = rotatePointByAngle(center, point, 45, yToXRelation);
                rotatedPoints.Add(point2);
                series.Points.AddXY(point.X, point.Y);
                series2.Points.AddXY(point2.X + 1, point2.Y);
            }
            series.Points.AddXY(points[0].X, points[0].Y);
            series2.Points.AddXY(rotatedPoints[0].X + 1, rotatedPoints[0].Y); 



            Series series3 = chart.Series.Add(Guid.NewGuid().ToString());
            series3.IsVisibleInLegend = false;
            series3.ChartType = SeriesChartType.Point;
            series3.Color = Color.Transparent;
            series3.SmartLabelStyle.Enabled = false;
            series3.Points.AddXY(5, 7);
            series3.Label = "ABCD";
            series3.LabelAngle = 0;

            Series series4 = chart.Series.Add(Guid.NewGuid().ToString());
            series4.IsVisibleInLegend = false;
            series4.ChartType = SeriesChartType.Point;
            series4.Color = Color.Transparent;
            series4.SmartLabelStyle.Enabled = false;
            series4.Points.AddXY(6, 7);
            series4.Label = "ABCD";
            series4.LabelAngle = -45;

            double distanceFromCenter = 1.4;



        }


        private static string getUnitLabelValue(ConstantValueCurve curve)
        {
            string result = curve.Value.ToString();

            if(curve.ChartDataType == ChartDataType.Enthalpy)
            {
                result = (curve.Value / 1000).ToString();
            }
            if(curve.ChartDataType == ChartDataType.RelativeHumidity)
            {
                result += "%";
            }

            return result;
        }

        private static string getLineName(ChartDataType chartDataType)
        {
            switch(chartDataType)
            {
                case ChartDataType.RelativeHumidity:
                    return "Relative Humidity φ";
                case ChartDataType.Density:
                    return "Density ρ [kg/m³]";
                case ChartDataType.Enthalpy:
                    return "Enthalpy h [kJ/kg]";
                case ChartDataType.SpecificVolume:
                    return "Specific volume v [m³/kg]";
                case ChartDataType.WetBulbTemperature:
                    return "Wet Bulb Temperature t_wb [°C]";
                default:
                    return "";
            }
        }

        private static Point2D getDefaultLabelPosition(ConstantValueCurve curve, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;

            switch(curve.ChartDataType)
            {
                case ChartDataType.RelativeHumidity:
                    return Convert.ToSAM(curve.End, chartType);
                case ChartDataType.Density:
                    return Convert.ToSAM(curve.Start, chartType);
                case ChartDataType.Enthalpy:
                    return Convert.ToSAM(curve.End, chartType);
                case ChartDataType.SpecificVolume:
                    return Convert.ToSAM(curve.End, chartType);
                case ChartDataType.WetBulbTemperature:
                    return Convert.ToSAM(curve.End, chartType);
                default: return null;
            }
        }



        private static Dictionary<ConstantValueCurve, string> getLinesNamesLocations(List<ConstantValueCurve> curves)
        {
            Dictionary<ConstantValueCurve, string> result = new Dictionary<ConstantValueCurve, string>();

            ChartDataType actualChartDataType = curves[0].ChartDataType;
            int it = 0;
            for(int i=0; i<curves.Count; i++)
            {
                ConstantValueCurve curve = curves[i];
                if (curve.ChartDataType != actualChartDataType)
                {
                    int pos = (i - it) / 2 + it;
                    result.Add(curves[pos], getLineName(actualChartDataType));

                    it = i;
                    actualChartDataType = curve.ChartDataType;
                }
            }
            int posTemp = (curves.Count - it) / 2 + it;
            result.Add(curves[posTemp], getLineName(actualChartDataType));

            return result;
        }
        // change point string int to point string double
        private static List<Tuple<Point2D, string, int>> getCurvesUnitsLabels(Control control, Chart chart, List<Rectangle2D> createdLabels, List<UIMollierProcess> mollierProcesses, List<UIMollierPoint> mollierPoints, MollierControlSettings mollierControlSettings)
        {
            List<Tuple<Point2D, string, int>> result = new List<Tuple<Point2D, string, int>>();
            List<Segment2D> processesObstacles = getProcessesObstacles(mollierProcesses, mollierControlSettings);
            List<Point2D> pointsObstacles = getPointsObstacles(mollierPoints, mollierControlSettings);
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);

            if (createdLabels == null)
            {
                createdLabels = new List<Rectangle2D>();
            }

            List<ConstantValueCurve> curves = new List<ConstantValueCurve>();
            foreach(Series chartSeries in chart.Series)
            {
                if(chartSeries?.Tag is ConstantValueCurve)
                {
                    ConstantValueCurve curve = chartSeries.Tag as ConstantValueCurve;

                    if (curve.ChartDataType == ChartDataType.Enthalpy && curve.Value % 10000 != 0) continue;

                    curves.Add(curve);
                }
            }

            // there we save on what line we want to put the line name
            Dictionary<ConstantValueCurve, string> linesNamesLocations = getLinesNamesLocations(curves);

            double distanceFromCenter = mollierControlSettings.ChartType == ChartType.Mollier ? 1.4 : 0.0007;
            double distanceFromEdge = mollierControlSettings.ChartType == ChartType.Mollier ? 1 : 0.0005;

            foreach (ConstantValueCurve curve in curves)
            {
                if (curve.ChartDataType == ChartDataType.SpecificVolume && mollierControlSettings.ChartType == ChartType.Psychrometric)
                {
                    int kkk = 3;
                }
                Point2D defaultLabelPosition = getDefaultLabelPosition(curve, mollierControlSettings);

              //  if (mollierControlSettings.ChartType == ChartType.Psychrometric) defaultLabelPosition.Y /= 1000;

                string text = getUnitLabelValue(curve);

                if(linesNamesLocations.ContainsKey(curve))
                {
                    text = text + " " + linesNamesLocations[curve];
                }

                double defaultAccuracy = curve.ChartDataType != ChartDataType.Density ? -0.1 * scaleVector.X : 0.1 * scaleVector.X;

                if (mollierControlSettings.ChartType == ChartType.Psychrometric && curve.ChartDataType != ChartDataType.RelativeHumidity)
                    defaultAccuracy *= (-1);
             
                    double defaultOffsetNumber = 200;
                double deltaX = scaleVector.X * defaultAccuracy;

                Point2D previousPosition = defaultLabelPosition;
                Point2D actualPosition = defaultLabelPosition;
                Point2D nextPosition = defaultLabelPosition;

                for (int i=1; i< defaultOffsetNumber; i++)
                {
                    previousPosition = actualPosition;
                    actualPosition = nextPosition;
                    double nextY = getY(curve, (defaultLabelPosition.X + i * deltaX), mollierControlSettings);
                    if (double.IsNaN(nextY)) continue;  
                    nextPosition = new Point2D(defaultLabelPosition.X + i*deltaX, nextY);
                    
                    Vector2D angleVector = new Vector2D(nextPosition.X - previousPosition.X, nextPosition.Y - previousPosition.Y);
                    int angle = vectorToAngle(angleVector, mollierControlSettings.ChartType);

                 //   int angle = vectorToAngle(new Vector2D(previousPosition.X - nextPosition.X, previousPosition.Y - nextPosition.Y), mollierControlSettings.ChartType);
                    Rectangle2D labelShape = getLabelShape(control, new Point2D(actualPosition.X, actualPosition.Y - (distanceFromEdge / 2) + distanceFromCenter), text, -angle, mollierControlSettings); // there must be -angle
                    
                    if (onChart(labelShape) && !intersect(labelShape, processesObstacles) && !intersect(labelShape, pointsObstacles) && !intersect(labelShape, createdLabels))
                    {
                        result.Add(new Tuple<Point2D, string, int>(new Point2D(actualPosition.X, actualPosition.Y - (distanceFromEdge/2)), text, angle));
                        createdLabels.Add(labelShape);
                        break;
                    }
                }
            }

            return result;
        }

        private static List<Rectangle2D> getProcessesLabelsShape(Control control, Dictionary<Point2D, string> processesLabels, MollierControlSettings mollierControlSettings)
        {
            if (processesLabels == null)
            {
                return null;
            }
            List<Rectangle2D> result = new List<Rectangle2D>();
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);
            ChartType chartType = mollierControlSettings.ChartType;
            double letterHeight = chartType == ChartType.Mollier ? 1.2 * scaleVector.Y : 0.00049 * scaleVector.Y;
            double letterWidth = chartType == ChartType.Mollier ? 0.2 * scaleVector.X : 0.49 * scaleVector.X;
            double distanceFromCenter = mollierControlSettings.ChartType == ChartType.Mollier ? 1.4 : 0.0007;


            foreach (KeyValuePair<Point2D, string> processLabel in processesLabels)
            {
                Point2D location = processLabel.Key;
                string text = processLabel.Value;
                Point2D center = new Point2D(location.X, location.Y + distanceFromCenter);

                List<Point2D> points = getCornerPoints(center, letterWidth * text.Length, letterHeight);
                result.Add(Geometry.Planar.Create.Rectangle2D(points));
            }
            return result;
        }
        public static Series AddLinesLabel1(Control control, Chart chart, List<UIMollierProcess> mollierProcesses, List<UIMollierPoint> mollierPoints, Dictionary<Point2D, string> processesLabels, MollierControlSettings mollierControlSettings)
        {
            Series series = new Series();
            List<Rectangle2D> createdLabels = getProcessesLabelsShape(control, processesLabels, mollierControlSettings);

            chartArea = new BoundingBox2D(new Point2D(chart.ChartAreas[0].AxisX.Minimum, chart.ChartAreas[0].AxisY.Minimum),
                                          new Point2D(chart.ChartAreas[0].AxisX.Maximum, chart.ChartAreas[0].AxisY.Maximum));

            //Tuple<Point2D, string> processesLabels = getProcessesLabels();
            //Tuple<Point2D, string> lineTypeLabels = getLineTypeLabels();
            List <Tuple<Point2D, string, int>> lineUnitLabels = getCurvesUnitsLabels(control, chart, createdLabels, mollierProcesses, mollierPoints, mollierControlSettings);

            foreach(Tuple<Point2D, string, int> lineUnitLabel in lineUnitLabels)
            {
                Point2D position = lineUnitLabel.Item1;
                //if (mollierControlSettings.ChartType == ChartType.Psychrometric) position.Y /= 1000;
                string text = lineUnitLabel.Item2;
                int angle = lineUnitLabel.Item3; // powinno być list of label lineUnitLabels -> taki label to byłaby klasa zawierająca pos, text, 
                Modify.AddLabel(chart, mollierControlSettings, position.X, position.Y, angle, 0, 0, text, ChartDataType.Undefined, ChartParameterType.Point, color: Color.Gray);
            }
            return series;
        }





        private static bool intersect(Rectangle2D labelShape, List<Point2D> points)
        {
            if (points == null) return false;

            foreach (Point2D point in points)
            {
                if (labelShape.InRange(point))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool intersect(Rectangle2D labelShape, List<Segment2D> segments)
        {
            if(segments == null) return false;

            foreach(Segment2D segment in segments)
            {
                if (labelShape.Intersect(segment))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool intersect(Rectangle2D labelShape, List<Rectangle2D> rectangles)
        {
            if (rectangles == null) return false;

            foreach(Rectangle2D rectangle in rectangles)
            {
                if(labelShape.Intersect(rectangle))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool onChart(Rectangle2D labelShape)
        {
            return chartArea.Inside(labelShape);
        }
        


        private static List<Point2D> getCornerPoints(Point2D center, double width, double height)
        {
            List<Point2D> cornerPoints = new List<Point2D>();

            cornerPoints.Add(new Point2D(center.X - width / 2, center.Y + height / 2));
            cornerPoints.Add(new Point2D(center.X - width / 2, center.Y - height / 2));
            cornerPoints.Add(new Point2D(center.X + width / 2, center.Y - height / 2));
            cornerPoints.Add(new Point2D(center.X + width / 2, center.Y + height / 2));

            return cornerPoints;
        }
        private static Rectangle2D getLabelShape(Control control, Point2D centerPosition, string label, double angle, MollierControlSettings mollierControlSettings)
        {
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);
            ChartType chartType = mollierControlSettings.ChartType;
            double letterHeight = chartType == ChartType.Mollier ? 1.2 * scaleVector.Y : 0.00049 * scaleVector.Y;
            double letterWidth = chartType == ChartType.Mollier ? 0.2 * scaleVector.X : 0.49 * scaleVector.X;
            double yToXRelation = yOYToOXDefaultMollierRelation(mollierControlSettings.ChartType);

            double width = label.Length * letterWidth;
            double height = letterHeight;

            List<Point2D> rectanglePoints = getCornerPoints(centerPosition, width, height);
            List<Point2D> rotatedRectanglePoints = new List<Point2D>();
            foreach(Point2D rectanglePoint in rectanglePoints)
            {
                Point2D rotatedRectanglePoint = rotatePointByAngle(centerPosition, rectanglePoint, angle, yToXRelation);
                rotatedRectanglePoints.Add(rotatedRectanglePoint);
            }

            return SAM.Geometry.Planar.Create.Rectangle2D(rotatedRectanglePoints);
        }
        private static int vectorToAngle(Vector2D vector, ChartType chartType)
        {
            if (vector.Y == 0) return 0;
            if (vector.X == 0) return 90;

            double tan = chartType == ChartType.Mollier ? vector.Y / vector.X / 4 : vector.Y / vector.X * 1000;
            int angle =  - System.Convert.ToInt32(System.Math.Atan(tan) * 180 / System.Math.PI);

            return angle;
        }
        
        private static double getY(ConstantValueCurve curve, double x, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            Point2D start = null; Point2D end = null;

            for(int i=0; i<curve.MollierPoints.Count - 1; i++)
            {
                Point2D startTemp = Convert.ToSAM(curve.MollierPoints[i], chartType);
                Point2D endTemp = Convert.ToSAM(curve.MollierPoints[i + 1], chartType);
                if(System.Math.Min(startTemp.X, endTemp.X) <= x && x <= System.Math.Max(startTemp.X, endTemp.X))
                {
                    start = startTemp;
                    end = endTemp;
                }
            }

      //      if (chartType == ChartType.Psychrometric) return linearY(start, end, x) * 1000;

            // znajdz w SAMie
            return linearY(start, end, x);
        }
        private static double linearY(Point2D start, Point2D end, double x)
        {
            if (start == null || end == null) return double.NaN;
            double a = (end.Y - start.Y) / (end.X - start.X);
            double b = start.Y - a * start.X;

            return a * x + b  ;
        }
    
        private static bool growing(ConstantValueCurve curve, MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings.ChartType == ChartType.Mollier)
            {
                if(curve.End.HumidityRatio > curve.Start.HumidityRatio)
                {
                    if(curve.End.DryBulbTemperature > curve.Start.DryBulbTemperature)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false ; // to change
            }
        }
        private static List<Point2D> generateOffsetsPoints(ConstantValueCurve curve, Point2D defaultLabelPosition, Vector2D scaleVector, MollierControlSettings mollierControlSettings, int offsetsNumber = 100)
        {
            List<Point2D> offsetsPoints = new List<Point2D>();
            double defaultAccuracy = growing(curve, mollierControlSettings) ? -0.1 * scaleVector.X : 0.1 * scaleVector.X;
            double defaultOffsetNumber = 50;
            double deltaX = scaleVector.X * defaultAccuracy;

            double prevY = 0;
            for(int i=0; i< defaultOffsetNumber; i++)
            {
                double offsetX = defaultLabelPosition.X + (i * deltaX);
                double offsetY = getY(curve, defaultLabelPosition.X + offsetX, mollierControlSettings);

                if (double.IsNaN(offsetX) || double.IsNaN(offsetY)) continue;
                offsetsPoints.Add(new Point2D(offsetX, offsetY));
            }

            return offsetsPoints;
        }

        private static Segment2D getVisibleLinePart(Segment2D line, MollierControlSettings mollierControlSettings)
        {
            List<Point2D> intersectionsPoints = chartArea.Intersections(line);
            if (chartArea.Inside(line.Start)) intersectionsPoints.Add(line.Start);
            if (chartArea.Inside(line.End)) intersectionsPoints.Add(line.End);

            if(intersectionsPoints.Count != 2)
            {
                return null;
            }

            return new Segment2D(intersectionsPoints[0], intersectionsPoints[1]);
        }
        
       
        private static List<Segment2D> getProcessesObstacles(List<UIMollierProcess> mollierProcesses, MollierControlSettings mollierControlSettings)
        {
            if(mollierProcesses == null)
            {
                return null;
            }

            List<Segment2D> processesLines = new List<Segment2D>();

            foreach (UIMollierProcess mollierProcess in mollierProcesses)
            {
                Point2D start = Convert.ToSAM(mollierProcess.Start, mollierControlSettings.ChartType);
                Point2D end = Convert.ToSAM(mollierProcess.End, mollierControlSettings.ChartType);
                Segment2D line = new Segment2D(start, end);

                processesLines.Add(getVisibleLinePart(line, mollierControlSettings));
            }

            return processesLines;
        }
        private static List<Point2D> getPointsObstacles(List<UIMollierPoint> mollierPoints, MollierControlSettings mollierControlSettings)
        {
            if(mollierPoints == null)
            {
                return null;
            }
            // wrapped into method because there might be circle2D not point2D to get more precisely
            List<Point2D> pointsList = new List<Point2D>();
            foreach (MollierPoint mollierPoint in mollierPoints)
            {
                Point2D point = Convert.ToSAM(mollierPoint, mollierControlSettings.ChartType);
                pointsList.Add(point);
            }
            return pointsList;
        }
    
    
    }
}
