using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using SAM.Geometry.Planar;
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public static partial class Temporary2
    {
        public static void Tests(Control control, Chart chart, MollierControlSettings mollierControlSettings)
        {
            Rectangle2D rectangle = new Rectangle2D(2, 3);
            Circle2D circle = new Circle2D(new Point2D(1, 4), 2);

            Circle2D circle2 = new Circle2D(new Point2D(10, 14), 1);

            Circle2D circle3 = new Circle2D(new Point2D(3, 4), System.Math.Sqrt(2) - 0.001);
            Circle2D circle4 = new Circle2D(new Point2D(1, 1), 0.5);
            bool check = circle.InRange(rectangle);
            bool check2 = circle2.InRange(rectangle); // brakuje tej metody
            bool check3 = circle3.InRange(rectangle);
            bool check4 = circle4.InRange(rectangle);
            //bool check5 = rectangle.Inside(circle4);
            /* foreach(Solver2DResult resultDa ta in solver2DResults)
            {
                Solver2DData solver2DData = resultData.Solver2DData;
                Rectangle2D labelShape = resultData.Closed2D<Rectangle2D>();
                if (labelShape == null) continue;
                if (!(solver2DData.Tag is UIMollierPoint)) continue;

                addToChartR(chart, labelShape, Color.Red, axesRatio.Y);
            }
            return new List<Series>();*/


        }
     /*   public static void addToChartR(Chart chart, Rectangle2D rectangle, Color color, double yTOX)
        {
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 1;
            series.Color = color;

            List<Point2D> resultPoints = rectangle.GetPoints();
            foreach (Point2D point in resultPoints)
            {
                series.Points.AddXY(point.X, point.Y / yTOX);
            }
            
            series.Points.AddXY(resultPoints[0].X, resultPoints[0].Y / yTOX);

        }





        // Modify AddLabels
        public static List<Series> AddLabels(Control control, Chart chart, MollierControlSettings mollierControlSettings)
        {
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);
            Vector2D axesRatio = getAxesRatio(chart, mollierControlSettings);

            List<IClosed2D> obstacles = getObstacles(control, chart, mollierControlSettings, scaleVector, axesRatio);
            List<Solver2DData> solverData = getLabelsData(control, chart, mollierControlSettings, scaleVector, axesRatio);

            Point2D chartMinPoint = new Point2D(chart.ChartAreas[0].AxisX.Minimum, chart.ChartAreas[0].AxisY.Minimum * axesRatio.Y);
            Point2D chartMaxPoint = new Point2D(chart.ChartAreas[0].AxisX.Maximum, chart.ChartAreas[0].AxisY.Maximum * axesRatio.Y);
            Rectangle2D chartArea = new Rectangle2D(new BoundingBox2D(chartMinPoint, chartMaxPoint));

            Solver2DSettings solver2DSettings = new Solver2DSettings();
            Solver2D solver = new Solver2D(chartArea, obstacles);

            foreach (Solver2DData solver2DData in solverData)
            {
                Rectangle2D rectangle = solver2DData.Closed2D<Rectangle2D>();

                Polyline2D polyline = solver2DData.Geometry2D<Polyline2D>();
                Point2D point = solver2DData.Geometry2D<Point2D>();

                if (rectangle != null && polyline != null)
                {
                    solver.Add(rectangle, polyline, tag: solver2DData.Tag);
                }
                else if (rectangle != null && point != null)
                {
                    solver.Add(rectangle, point, tag: solver2DData.Tag);
                }
            }

            if (solverData == null || solverData.Count == 0)
            {
                return null;
            }

            List<Solver2DResult> solver2DResults = solver.Solve(solver2DSettings);
            if (solver2DResults == null) return null;

            List<Tuple<Point2D, string, double, Color>> labelsPositions = getLabelsPositions(solver2DResults, mollierControlSettings.ChartType, scaleVector, axesRatio);
            labelsPositions = fixPositions(chart, labelsPositions);
            List<Series> result = addLabels(chart, labelsPositions);

            //  Func <List<Series>> func = new Func<List<Series>>( { return null; });
            // Func<List<Series>> fun = new Func<>() { return null; }

            Func<Chart, List<Tuple<Point2D, string, double, Color>>, List<Series>> function = new Func<Chart, List<Tuple<Point2D, string, double, Color>>,List<Series>>((x, y) =>
            {


                return null;
            });

          //  function.Invoke();

            return result;
        }
        // klasa ChartLabel i zrobić AddChartLabels i AddChartLabel zmienć na public, z this
        private static List<Series> addLabels(Chart chart, List<Tuple<Point2D, string, double, Color>> labelsPositions) 
        {
            List<Series> result = new List<Series>();

            foreach (Tuple<Point2D, string, double, Color> labelPosition in labelsPositions)
            {
                result.Add(addLabel(chart, labelPosition));
            }

            return result;
        }
        private static Series addLabel(Chart chart, Tuple<Point2D, string, double, Color> labelPosition)
        {
            Series result = chart.Series.Add(Guid.NewGuid().ToString());
            result.SmartLabelStyle.Enabled = false;
            result.IsVisibleInLegend = false;
            result.Color = Color.Transparent;
            result.ChartType = SeriesChartType.Point;
            result.Points.AddXY(labelPosition.Item1.X, labelPosition.Item1.Y);
            result.Label = labelPosition.Item2;
            result.LabelAngle = System.Convert.ToInt32(labelPosition.Item3) % 90;
            result.LabelForeColor = labelPosition.Item4;
            result.Tag = "Label " + Guid.NewGuid().ToString();

            return result;
        }





        // Do Query
        public static List<IClosed2D> getObstacles(Control control, Chart chart, MollierControlSettings mollierControlSettings)
        {
            List<IClosed2D> result = new List<IClosed2D>();
            ChartType chartType = mollierControlSettings.ChartType;
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);
            double axesRatio = getAxesRatio(chart, mollierControlSettings);
           // to może być control chart.Parent control do usuniecia z parametrów i do Query nazwać obstacles

            foreach (Series series in chart.Series)
            {
                if (series.Tag is UIMollierProcess)
                {
                    UIMollierProcess process = (UIMollierProcess)series.Tag;
                    result.AddRange(processToIClosed2D(process, chartType, scaleVector, axesRatio));
                }
                if (series.Tag is UIMollierPoint)
                {
                    UIMollierPoint point = (UIMollierPoint)series.Tag;
                    result.Add(pointToIClosed2D(point, chartType, scaleVector, axesRatio));
                }
                if (series.Tag is MollierZone)
                {
                    MollierZone zone = (MollierZone)series.Tag;
                    result.AddRange(zoneToIClosed2D(zone, chartType, scaleVector, axesRatio));
                }
            }

            return result;
        }

        // zostawić private obstacles_Process te poniżej analogicznie
        private static List<IClosed2D> processToIClosed2D(UIMollierProcess process, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            List<IClosed2D> result = new List<IClosed2D>();
            double processWidth = 0.2; //  To change ? bad look on psychrometrics 
            Point2D start = Convert.ToSAM(process.Start, chartType); 
            Point2D end = Convert.ToSAM(process.End, chartType);

            Segment2D processSegment = new Segment2D(start, end);
            Vector2D widthVector = processSegment.Direction.Unit.GetPerpendicular() * processWidth / 2;

            Rectangle2D processRectangle = segment2DToRectangle2D(new Segment2D(start, end), widthVector, axesRatio);

            result.Add(processRectangle);
            // TODO: Start and end point circles
            //result.Add(pointToIClosed2D(new UIMollierPoint(process.Start), chartType, scaleVector, axesRatio));
            //result.Add(pointToIClosed2D(new UIMollierPoint(process.End), chartType, scaleVector, axesRatio));

            return result;
        }
        private static IClosed2D pointToIClosed2D(UIMollierPoint point, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            double pointRadius = 0.2 * scaleVector.Y;
            Point2D center = scale(Convert.ToSAM(point, chartType), axesRatio);

            return new Circle2D(center, pointRadius);
        }
        private static List<IClosed2D> zoneToIClosed2D(MollierZone zone, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            List<IClosed2D> result = new List<IClosed2D>();
            double zoneWidth = 0.2;

            for (int i = 0; i < zone.MollierPoints.Count; i++)
            {
                int previousPointID = i == 0 ? zone.MollierPoints.Count - 1 : i - 1;
                Point2D start = Convert.ToSAM(zone.MollierPoints[i], chartType);
                Point2D end = Convert.ToSAM(zone.MollierPoints[previousPointID], chartType);

                Segment2D zoneSegment = new Segment2D(start, end);
                Vector2D widthVector = zoneSegment.Direction.Unit.GetPerpendicular() * zoneWidth / 2;

                result.Add(segment2DToRectangle2D(zoneSegment, widthVector, axesRatio));
            }

            return result;
        }

        // Create -> Rectangle2D -> : nazwa metody: Rectangle2D planar create
        private static Rectangle2D segment2DToRectangle2D(Segment2D segment2D, double width, double axesRatio = 1)
        {
            // pbliczać perpendicular wektor z długością width / 2
            List<Point2D> rectanglePoints = new List<Point2D>();
            rectanglePoints.Add(scale(segment2D.Start + widthVector, axesRatio));
            rectanglePoints.Add(scale(segment2D.End + widthVector, axesRatio));
            rectanglePoints.Add(scale(segment2D.Start + widthVector.GetNegated(), axesRatio));
            rectanglePoints.Add(scale(segment2D.End + widthVector.GetNegated(), axesRatio));

            return Geometry.Planar.Create.Rectangle2D(rectanglePoints);
        }



        //Create -> Solver2DDatas: public Solver2DDatas(this chart); 
        public static List<Solver2DData> getLabelsData(Chart chart, MollierControlSettings mollierControlSettings, double axesRatio)
        {
            List<Solver2DData> result = new List<Solver2DData>();
            ChartType chartType = mollierControlSettings.ChartType;

            foreach (Series series in chart.Series)
            {
                if (series.Tag is UIMollierPoint)
                {
                    UIMollierPoint point = (UIMollierPoint)series.Tag;
                    result.AddRange(getPointData(point, chartType, scaleVector, axesRatio));
                }
                if (series.Tag is UIMollierProcess)
                {
                    UIMollierProcess process = (UIMollierProcess)series.Tag;
                    result.AddRange(getProcessData(process, chartType, scaleVector, axesRatio));
                }
                if (series.Tag is ConstantValueCurve && !(series.Tag is ConstantTemperatureCurve)) 
                {
                    ConstantValueCurve curve = (ConstantValueCurve)series.Tag;
                    result.AddRange(getCurveUnitData(chart, curve, chartType, scaleVector, axesRatio, mollierControlSettings));
                }
                if(series.Tag is MollierControlZone)
                {
                    MollierControlZone zone = (MollierControlZone)series.Tag;
                    result.AddRange(getZoneData(zone, chartType, scaleVector, axesRatio));
                }
            }
            result.AddRange(getCurvesNamesData(chart, chartType, scaleVector, axesRatio, mollierControlSettings));
            return result;
        }

        //Solver2DDatas_UIMolierPOint zostawić scaleVector
        private static List<Solver2DData> getPointData(UIMollierPoint mollierPoint, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            List<Solver2DData> result = new List<Solver2DData>();
            if (mollierPoint == null) return result;

            Point2D point = Convert.ToSAM(mollierPoint, chartType);
            string text = mollierPoint.UIMollierAppearance.Label;
            Point2D labelCenter = getLabelCenter(point, chartType, scaleVector);

            Rectangle2D labelRectangle = textToRectangle(labelCenter, text, chartType, scaleVector, axesRatio);

            Solver2DData solver2DData = new Solver2DData(labelRectangle, scale(point, axesRatio));
            solver2DData.Tag = mollierPoint;
            result.Add(solver2DData);

            return result;
        }
        private static List<Solver2DData> getProcessData(UIMollierProcess process, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            List<Solver2DData> result = new List<Solver2DData>();

            UIMollierPoint start = new UIMollierPoint(process.Start, process.UIMollierAppearance_Start);
            result.AddRange(getPointData(start, chartType, scaleVector, axesRatio));

            UIMollierPoint mid = new UIMollierPoint(getMidPoint(process.Start, process.End), process.UIMollierAppearance);
            result.AddRange(getPointData(mid, chartType, scaleVector, axesRatio));

            UIMollierPoint end = new UIMollierPoint(process.End, process.UIMollierAppearance_End);
            result.AddRange(getPointData(end, chartType, scaleVector, axesRatio));

            return result;
        }
        private static List<Solver2DData> getCurveUnitData(Chart chart, ConstantValueCurve curve, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio, MollierControlSettings mollierControlSettings)
        {
            List<Solver2DData> result = new List<Solver2DData>();
            if (mollierControlSettings.DisableUnits || (curve is ConstantEnthalpyCurve && curve.Value % 10000 != 0))
            {
                return result;
            }

            string text = getCurveUnitText(curve);
            Color color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Unit, curve.ChartDataType);

            Point2D defaultPoint2D = Query.DefaultPoint2D(chart, mollierControlSettings, curve, chartType);
            if (defaultPoint2D == null) return result;

            Polyline2D polyline = curveToPolyline(curve, chartType, axesRatio);
            if (curve.ChartDataType == ChartDataType.Enthalpy)
            {
                polyline = extendEnthalpyCurve(curve, defaultPoint2D, chartType, axesRatio);
            }
               
            Rectangle2D rectangle = getLabelRectangle(defaultPoint2D, polyline, text, mollierControlSettings, scaleVector, axesRatio);
            if (rectangle == null) return result;

            Solver2DData solver2DData = new Solver2DData(rectangle, polyline);
            solver2DData.Tag = new UIMollierPoint(Convert.ToMollier(rectangle.GetCentroid(), chartType, mollierControlSettings.Pressure), new UIMollierAppearance(color, text));
            solver2DData.Priority = getChartDataTypePriority(curve.ChartDataType);
            result.Add(solver2DData);
    
            return result;
        }
        
        // dopisywać summary 
        private static List<Solver2DData> getCurvesNamesData(Chart chart, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio, MollierControlSettings mollierControlSettings)
        {
            if (mollierControlSettings.DisableLabels) return new List<Solver2DData>();

            List<Solver2DData> result = new List<Solver2DData>();
            Dictionary<ChartDataType, List<ConstantValueCurve>> orderedCurves = orderCurves(chart, chartType);

            // TODO: [maciek] : maybe wrap also this foreach to one method
            foreach(KeyValuePair<ChartDataType, List<ConstantValueCurve>> curves in orderedCurves)
            {
                ChartDataType chartDataType = curves.Key;
                ConstantValueCurve curve = curves.Value[curves.Value.Count / 2];
                string text = getCurveNameText(chartDataType);

                Point2D defaultPoint2D = Query.DefaultPoint2D(chart, mollierControlSettings, curve, chartType, ChartParameterType.Label);
                if(defaultPoint2D == null)
                {
                    return result;
                }
                Polyline2D polyline = curveToPolyline(curve, chartType, axesRatio);
                Rectangle2D rectangle = getLabelRectangle(defaultPoint2D, polyline, text, mollierControlSettings, scaleVector, axesRatio);

                Solver2DData solver2DData = new Solver2DData(rectangle, polyline);

                Color color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Label, curve.ChartDataType);
                solver2DData.Tag = new UIMollierPoint(Convert.ToMollier(defaultPoint2D, chartType, mollierControlSettings.Pressure) , new UIMollierAppearance(color, text));
                solver2DData.Priority = getChartDataTypePriority(chartDataType);

                result.Add(solver2DData);
            }

            return result;
        }
        private static List<Solver2DData> getZoneData(MollierControlZone zone, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            List<Solver2DData> result = new List<Solver2DData>();
            if (zone == null) return result;

            Point2D point = Convert.ToSAM(zone.GetCenter(), chartType);


            return result;
        }
       
        
        
        private static Rectangle2D getLabelRectangle(Point2D point, Polyline2D polyline, string text, MollierControlSettings mollierControlSettings, Vector2D scaleVector, Vector2D axesRatio)
        {

            ChartType chartType = mollierControlSettings.ChartType;
            Point2D labelCenter = getLabelCenter(point, chartType, scaleVector);

            Rectangle2D rectangleShape = textToRectangle(labelCenter, text, chartType, scaleVector, axesRatio);
            if (rectangleShape == null) return null;

            List<Segment2D> segments = polyline.ClosestSegment2Ds(scale(point, axesRatio));
            if (segments == null) return null;
            
            Segment2D curveSegment = segments[0];
            double distance = rectangleShape.GetCentroid().Y - point.Y * axesRatio.Y;
            bool clockwise = curveSegment.Direction.GetPerpendicular().Y < 0;
             
            Rectangle2D result = Geometry.Planar.Query.MoveToSegment2D(rectangleShape, curveSegment, scale(point, axesRatio), distance, clockwise);
            return fixRectangleShape(result, rectangleShape);
        }

        private static Dictionary<ChartDataType, List<ConstantValueCurve>> orderCurves(Chart chart, ChartType chartType)
        {
            Dictionary<ChartDataType, List<ConstantValueCurve>> result = new Dictionary<ChartDataType, List<ConstantValueCurve>>();
            
            foreach (Series series in chart.Series)
            {
                if (series.Tag is ConstantValueCurve && !(series.Tag is ConstantTemperatureCurve))
                {
                    ConstantValueCurve curve = (ConstantValueCurve)series.Tag;
                    if (!result.ContainsKey(curve.ChartDataType))
                    {
                        result.Add(curve.ChartDataType, new List<ConstantValueCurve>());
                    }
                    if (onChart(curve, chart, chartType))
                    {
                        if (curve.ChartDataType == ChartDataType.Enthalpy && curve.Value % 10000 != 0) continue;
                        result[curve.ChartDataType].Add(curve);
                    }
                }
            }

            return result;
        }
        private static Point2D getLabelCenter(Point2D point, ChartType chartType, Vector2D scaleVector)
        {
            // Method returns point's label 
            if(chartType == ChartType.Mollier)
            {
                return new Point2D(point.X, point.Y + 0.95 * scaleVector.Y);
            }
            else
            {
                return new Point2D(point.X, point.Y + 0.0005 * scaleVector.Y);
            }
        }


        private static Rectangle2D textToRectangle(Point2D center, string text, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            double capitalLetterHeight = (chartType == ChartType.Mollier ? 1 : 0.00055) * scaleVector.Y;
            double lowercaseHeight = (chartType == ChartType.Mollier ? 0.95 : 0.0005) * scaleVector.Y;
            double letterWidth = (chartType == ChartType.Mollier ? 0.165 : 0.375) * scaleVector.X;

            double width = letterWidth * text.Length;
            double height = containsCapitalLetter(text) ? capitalLetterHeight : lowercaseHeight;

            List<Point2D> rectanglePoints = new List<Point2D>();
            rectanglePoints.Add(scale(new Point2D(center.X - width / 2, center.Y - height / 2), axesRatio));
            rectanglePoints.Add(scale(new Point2D(center.X - width / 2, center.Y + height / 2), axesRatio));
            rectanglePoints.Add(scale(new Point2D(center.X + width / 2, center.Y - height / 2), axesRatio));
            rectanglePoints.Add(scale(new Point2D(center.X + width / 2, center.Y + height / 2), axesRatio));

            return Geometry.Planar.Create.Rectangle2D(rectanglePoints);
        }
        private static bool containsCapitalLetter(string text)
        {
            return !text.ToLower().Equals(text);
        }
        
        // create tu COnstantValueCurve do SAM Mollier
        private static Polyline2D curveToPolyline(ConstantValueCurve curve, ChartType chartType, double axesRatio = 1)
        {
            List<Segment2D> segments = new List<Segment2D>();
            for(int i=0; i<curve.MollierPoints.Count - 1; i++)
            {
                Point2D start = scale(Convert.ToSAM(curve.MollierPoints[i], chartType), axesRatio);
                Point2D end = scale(Convert.ToSAM(curve.MollierPoints[i+1], chartType), axesRatio);

                segments.Add(new Segment2D(start, end));
            }

            return new Polyline2D(segments);
        }
        private static MollierPoint getMidPoint(MollierPoint mollierPoint1, MollierPoint mollierPoint2)
        {
            if (mollierPoint1 == null || mollierPoint2 == null) return null;
            double dryBulbTemperature = mollierPoint1.DryBulbTemperature + (mollierPoint2.DryBulbTemperature - mollierPoint1.DryBulbTemperature) / 2;
            double humidityRatio = mollierPoint1.HumidityRatio + (mollierPoint2.HumidityRatio - mollierPoint1.HumidityRatio) / 2;

            return new MollierPoint(dryBulbTemperature, humidityRatio, mollierPoint1.Pressure);
        }
        private static string getCurveUnitText(ConstantValueCurve curve)
        {
            if(curve == null)
            {
                return "";
            }
            switch (curve.ChartDataType)
            {
                case ChartDataType.RelativeHumidity:
                    return curve.Value.ToString() + "%";
                case ChartDataType.Enthalpy:
                    return (curve.Value/ 1000).ToString();
                default:
                    return curve.Value.ToString();
            }
        }
        private static string getCurveNameText(ChartDataType chartDataType)
        {
            switch (chartDataType)
            {
                case ChartDataType.RelativeHumidity:
                    return "Relative Humidity φ [%]";
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
        private static Polyline2D extendEnthalpyCurve(ConstantValueCurve curve, Point2D newEnd, ChartType chartType, Vector2D axesRatio)
        {
            Point2D start = scale(Convert.ToSAM(curve.Start, chartType), axesRatio);
            Segment2D segment = new Segment2D(start, scale(newEnd, axesRatio));
         
            return new Polyline2D(new List<Segment2D>() { segment });
        }
        private static int getChartDataTypePriority(ChartDataType chartDataType)
        {
            switch(chartDataType)
            {
                case ChartDataType.HeatingProcess:
                    return 1;
                case ChartDataType.CoolingProcess:
                    return 1;
                case ChartDataType.HeatRecoveryProcess:
                    return 1;
                case ChartDataType.MixingProcess:
                    return 1;
                case ChartDataType.AdiabaticHumidificationProcess:
                    return 1;
                case ChartDataType.SteamHumidificationProcess:
                    return 1;
                case ChartDataType.UndefinedProcess:
                    return 1;
                case ChartDataType.RelativeHumidity:
                    return 2;
                case ChartDataType.Enthalpy:
                    return 3;
                case ChartDataType.Density:
                    return 4;
                case ChartDataType.SpecificVolume:
                    return 5;
                case ChartDataType.WetBulbTemperature:
                    return 6;
                default: return 10;
            }
        }


        private static List<Tuple<Point2D, string, double, Color>> getLabelsPositions(List<Solver2DResult> solver2DResults, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            if (solver2DResults == null) return null;

            List<Tuple<Point2D, string, double, Color>> result = new List<Tuple<Point2D, string, double, Color>>();

            foreach(Solver2DResult solver2DResult in solver2DResults)
            {
                Solver2DData solver2DData = solver2DResult.Solver2DData;
                Rectangle2D labelShape = solver2DResult.Closed2D<Rectangle2D>();
                if (labelShape == null) continue;
                if (!(solver2DData.Tag is UIMollierPoint)) continue; 

                Tuple<Point2D, double> positionAngleLabel = getPositionAngle(labelShape, chartType, scaleVector, axesRatio);
                string text = ((UIMollierPoint)solver2DData.Tag).UIMollierAppearance.Label;
                Color color = ((UIMollierPoint)solver2DData.Tag).UIMollierAppearance.Color;
                if (color == Color.Empty)
                {
                    color = Color.Black;
                }

                result.Add(new Tuple<Point2D, string, double, Color>(positionAngleLabel.Item1, text, positionAngleLabel.Item2, color));
            }

            return result;
        }
        private static Tuple<Point2D, double> getPositionAngle(Rectangle2D rectangle, ChartType chartType, Vector2D scaleVector, Vector2D axesRatio)
        {
            if (rectangle == null) return null;

            double distanceFromCenter = (chartType == ChartType.Mollier ? 1.4 : 0.0007) * scaleVector.Y;
            Point2D center = reScale(rectangle.GetCentroid(), axesRatio);
            Point2D point = new Point2D(center.X, center.Y - distanceFromCenter);

            double angle = vectorToAngle(rectangle.WidthDirection, chartType);

            return new Tuple<Point2D, double>(point, angle);
        }
        private static int vectorToAngle(Vector2D vector, ChartType chartType)
        {
            if (vector.Y == 0) return 0;
            if (vector.X == 0) return 90;

            double tan = chartType == ChartType.Mollier ? vector.Y / vector.X : vector.Y / vector.X;
            int angle = -System.Convert.ToInt32(System.Math.Atan(tan) * 180 / System.Math.PI);

            return angle;
        }
   

        private static Point2D scale(Point2D point, Vector2D axesRatio = null)
        {
            double newX = point.X;
            double newY = point.Y;

            if(axesRatio != null)
            {
                newX *= axesRatio.X;
                newY *= axesRatio.Y;
            }

            return new Vector2D(newX, newY);
        }
        private static Point2D reScale(Point2D point, Vector2D axesRatio = null)
        {
            double newX = point.X;
            double newY = point.Y;

            if (axesRatio != null)
            {
                newX /= axesRatio.X;
                newY /= axesRatio.Y;
            }

            return new Vector2D(newX, newY);
        }
        private static double getAxesRatio(Chart chart, MollierControlSettings mollierControlSettings)
        {
            //Query bez get this, public, summary napisaćw opisie że to x do y
            ChartType chartType = mollierControlSettings.ChartType;
            double realOXSize = chartType == ChartType.Mollier ? 30.45 : 28;
            double realOYSize = chartType == ChartType.Mollier ? 13.3 : 13.3;

            double OXRatio = chart.ChartAreas[0].AxisX.Maximum - chart.ChartAreas[0].AxisX.Minimum;
            double OYRatio = chart.ChartAreas[0].AxisY.Maximum - chart.ChartAreas[0].AxisY.Minimum;

            double xScale = OXRatio / realOXSize;
            double yScale = OYRatio / realOYSize;

            return  xScale / yScale;
        }

        private static bool onChart(ConstantValueCurve curve, Chart chart, ChartType chartType)
        {
            Point2D chartMinPoint = new Point2D(chart.ChartAreas[0].AxisX.Minimum, chart.ChartAreas[0].AxisY.Minimum);
            Point2D chartMaxPoint = new Point2D(chart.ChartAreas[0].AxisX.Maximum, chart.ChartAreas[0].AxisY.Maximum);
            Rectangle2D chartArea = new Rectangle2D(new BoundingBox2D(chartMinPoint, chartMaxPoint));

            Segment2D segment = new Segment2D(Convert.ToSAM(curve.Start, chartType), Convert.ToSAM(curve.End, chartType));
            return chartArea.InRange(segment);
        }
        private static Rectangle2D fixRectangleShape(Rectangle2D calculatedRectangle, Rectangle2D shapeRectangle)
        {
            // Calculated rectangle should have same shape
            // Especially they should have same width and heigth
            if (calculatedRectangle == null || shapeRectangle == null)
            {
                return calculatedRectangle;
            }
            if (System.Math.Abs(shapeRectangle.Width - calculatedRectangle.Width) < Tolerance.MacroDistance)
            {
                return calculatedRectangle;
            }

            Rectangle2D result = new Rectangle2D(calculatedRectangle.Origin, -calculatedRectangle.Height, calculatedRectangle.Width, calculatedRectangle.WidthDirection);
            return result;
        }
        private static List<Tuple<Point2D, string, double, Color>> fixPositions(Chart chart, List<Tuple<Point2D, string, double, Color>> positions, double tolerance = Tolerance.Distance)
        {
            foreach(Tuple<Point2D, string, double, Color> info in positions)
            {
                info.Item1.Y = System.Math.Max(info.Item1.Y, chart.ChartAreas[0].AxisY.Minimum);
            }
            return positions;
        }*/
    } 
}

