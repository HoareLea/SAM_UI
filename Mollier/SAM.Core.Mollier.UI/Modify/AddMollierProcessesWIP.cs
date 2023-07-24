using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        // ------------------GENERAL-ADD-PROCESSES-METHOD----------------------
        public static List<UIMollierProcess> AddMollierProcesses(this Chart chart, Control control, List<List<UIMollierProcess>> systems, List<UIMollierProcess> mollierProcesses, MollierControlSettings mollierControlSettings)
        {
            if(systems == null)
            {
                return null;
            }
            ChartType chartType = mollierControlSettings.ChartType;
            List<UIMollierProcess> labeledMollierProcesses = generateLabelsForProcessesSystems(systems);

            labeledMollierProcesses?.Sort((x, y) => System.Math.Max(x.Start.HumidityRatio, x.End.HumidityRatio).CompareTo(System.Math.Max(y.Start.HumidityRatio, y.End.HumidityRatio)));
            List<UIMollierPoint> processPointsToLabel = new List<UIMollierPoint>();

            foreach(UIMollierProcess uIMollierProcess in labeledMollierProcesses)
            {
                MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
                MollierPoint start = mollierProcess?.Start;
                MollierPoint end = mollierProcess?.End;

                if (start == null || end == null)
                {
                    continue;
                }

                if (mollierProcess is UndefinedProcess)
                {
                    createRoomProcessSeries(chart, uIMollierProcess, mollierControlSettings);
                    continue;
                }

                createProcessSeries(chart, uIMollierProcess, mollierControlSettings);
                createProcessPointsSeries(chart, start, uIMollierProcess, chartType, toolTipName: uIMollierProcess.UIMollierAppearance_Start.Label);
                createProcessPointsSeries(chart, end, uIMollierProcess, chartType, toolTipName: uIMollierProcess.UIMollierAppearance_End.Label);

                MollierPoint mid = mollierPointsMid(start, end);
                processPointsToLabel.Add(new UIMollierPoint(uIMollierProcess.Start,
                    new UIMollierAppearance(uIMollierProcess.UIMollierAppearance_Start.Color, uIMollierProcess.UIMollierAppearance_Start.Label)));
                processPointsToLabel.Add(new UIMollierPoint(uIMollierProcess.End, 
                    new UIMollierAppearance(uIMollierProcess.UIMollierAppearance_End.Color, uIMollierProcess.UIMollierAppearance_End.Label)));
                processPointsToLabel.Add(new UIMollierPoint(mid,
                    new UIMollierAppearance(Color.Empty, uIMollierProcess.UIMollierAppearance.Label)));

                //cooling process create one unique process with ADP point
                if (mollierProcess is CoolingProcess)
                {
                    UIMollierPoint ADPPoint = createCoolingAdditionalLines(chart, uIMollierProcess, mollierControlSettings);
                    processPointsToLabel.Add(ADPPoint);
                }
            }
            labelProcessPoints(chart, control, processPointsToLabel, mollierProcesses, mollierControlSettings);
            return labeledMollierProcesses;
        }

        
        // ------------------SERIES--------------------------------------------
        private static Series createDewPointDashLineSeries(this Chart chart, MollierPoint mollierPoint_1, MollierPoint mollierPoint_2, IMollierProcess mollierProcess, ChartType chartType, Color color, int borderWidth, ChartDashStyle borderDashStyle)
        {
            // Create additional dash line in Cooling process to ADP 
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            if (chartType == ChartType.Mollier)
            {
                series.Points.AddXY(mollierPoint_1.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(mollierPoint_1));
                series.Points.AddXY(mollierPoint_2.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(mollierPoint_2));
            }
            else
            {
                series.Points.AddXY(mollierPoint_1.DryBulbTemperature, mollierPoint_1.HumidityRatio);
                series.Points.AddXY(mollierPoint_2.DryBulbTemperature, mollierPoint_2.HumidityRatio);
            }
            series.Color = color;
            series.IsVisibleInLegend = false;
            series.BorderWidth = borderWidth;
            series.ChartType = SeriesChartType.Spline;
            series.BorderDashStyle = borderDashStyle;
            series.Tag = "dashLine";

            return series;
        }          
        private static List<Series> createRoomProcessSeries(this Chart chart, UIMollierProcess uIMollierProcess, MollierControlSettings mollierControlSettings)
        {
            List<Series> result = new List<Series>();

            //Defines the end label of the process
            MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
            //Specified the color of the Room air condition point
            Color color = uIMollierProcess.UIMollierAppearance.Color == Color.Empty ? Color.Gray : uIMollierProcess.UIMollierAppearance.Color;
            //Creating series for room process
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.Color = color;
            series.BorderDashStyle = ChartDashStyle.Dash;
            series.BorderWidth = 3;
            series.Tag = mollierProcess;
            //add start and end point to the process series
            MollierPoint start = mollierProcess.Start;
            MollierPoint end = mollierProcess.End;

            int index;
            index = mollierControlSettings.ChartType == ChartType.Mollier ? series.Points.AddXY(start.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(start)) : series.Points.AddXY(start.DryBulbTemperature, start.HumidityRatio);
            series.Points[index].Tag = start;
            index = mollierControlSettings.ChartType == ChartType.Mollier ? series.Points.AddXY(end.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(end)) : series.Points.AddXY(end.DryBulbTemperature, end.HumidityRatio);
            series.Points[index].Tag = end;
            series.ToolTip = Query.ToolTipText(start, end, mollierControlSettings.ChartType, Query.FullProcessName(uIMollierProcess));

            //creating series for Room air condition point 
            Series seriesRoomPoint = chart.Series.Add(Guid.NewGuid().ToString());
            seriesRoomPoint.IsVisibleInLegend = false;
            seriesRoomPoint.ChartType = SeriesChartType.Point;
            seriesRoomPoint.Color = Color.Gray;
            seriesRoomPoint.MarkerStyle = MarkerStyle.Triangle;
            seriesRoomPoint.MarkerBorderWidth = 2;
            seriesRoomPoint.MarkerBorderColor = color;
            seriesRoomPoint.MarkerSize = 8;
            seriesRoomPoint.Tag = mollierProcess;
            //add Room air condition point to the series and create label for it
            double X = mollierControlSettings.ChartType == ChartType.Mollier ? end.HumidityRatio * 1000 : end.DryBulbTemperature;
            double Y = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(end) : end.HumidityRatio;
            seriesRoomPoint.Points.AddXY(X, Y);
            seriesRoomPoint.Points[0].ToolTip = Query.ToolTipText(end, mollierControlSettings.ChartType, "ROOM");
            if (!string.IsNullOrWhiteSpace(uIMollierProcess?.UIMollierAppearance_Start?.Label))
            {
                createProcessPointsSeries(chart, start, uIMollierProcess, mollierControlSettings.ChartType);
            }

            result.Add(series);
            result.Add(seriesRoomPoint);
            return result;
        }
        private static Series createProcessSeries(Chart chart, UIMollierProcess uImollierProcess, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            MollierProcess mollierProcess = uImollierProcess.MollierProcess;
            MollierPoint start = mollierProcess.Start; 
            MollierPoint end = mollierProcess.End;
            Series series = chart.Series.Add(Guid.NewGuid().ToString());

            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 4;
            series.Color = (uImollierProcess.UIMollierAppearance.Color == Color.Empty) ?
                mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Line, mollierProcess)
                : uImollierProcess.UIMollierAppearance.Color;
            series.Tag = mollierProcess;

            int index;
            series.ToolTip = Query.ToolTipText(start, end, chartType, Query.FullProcessName(mollierProcess));
            index = chartType == ChartType.Mollier ? series.Points.AddXY(start.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(start)) : series.Points.AddXY(start.DryBulbTemperature, start.HumidityRatio);
            series.Points[index].Tag = start;
            index = chartType == ChartType.Mollier ? series.Points.AddXY(end.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(end)) : series.Points.AddXY(end.DryBulbTemperature, end.HumidityRatio);
            series.Points[index].Tag = end;

            return series;
        }
        private static UIMollierPoint createCoolingAdditionalLines(Chart chart, UIMollierProcess uIMollierProcess, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
            MollierPoint start = mollierProcess.Start;
            MollierPoint end = mollierProcess.End;

            if (mollierProcess == null || !(mollierProcess is CoolingProcess) ||start == null || end == null || start.HumidityRatio == end.HumidityRatio)
            {
                return null; 
            }
            MollierPoint apparatusDewMollierPoint = ((CoolingProcess)mollierProcess).ApparatusDewPoint();
            MollierPoint secondPoint = ((CoolingProcess)mollierProcess).DewPoint();

            createProcessPointsSeries(chart, apparatusDewMollierPoint, uIMollierProcess, chartType, toolTipName: "Dew Point", pointType: "DewPoint");
            createProcessPointsSeries(chart, secondPoint, uIMollierProcess, chartType, pointType: "SecondPoint");

            createDewPointDashLineSeries(chart, start, secondPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);
            createDewPointDashLineSeries(chart, end, apparatusDewMollierPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);
            createDewPointDashLineSeries(chart, end, secondPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);

            List<MollierPoint> mollierPoints = Mollier.Query.ProcessMollierPoints((CoolingProcess)mollierProcess);
            if (mollierPoints != null && mollierPoints.Count > 1)
            {
                for (int j = 0; j < mollierPoints.Count - 1; j++)
                {
                    createDewPointDashLineSeries(chart, mollierPoints[j], mollierPoints[j + 1], mollierProcess, chartType, Color.Gray, 3, ChartDashStyle.Solid);
                }
            }

            return new UIMollierPoint(apparatusDewMollierPoint, new UIMollierAppearance(Color.Empty, "ADP"));
        }
        private static Series createProcessPointsSeries(this Chart chart, MollierPoint mollierPoint, UIMollierProcess UI_MollierProcess, ChartType chartType, string toolTipName = null, string pointType = "Default")
        {
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            int index = chartType == ChartType.Mollier ? series.Points.AddXY(mollierPoint.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(mollierPoint)) : series.Points.AddXY(mollierPoint.DryBulbTemperature, mollierPoint.HumidityRatio);
            switch (pointType)
            {
                case "Default":
                    series.MarkerSize = 8;
                    series.MarkerBorderWidth = 2;
                    series.MarkerBorderColor = Color.Orange;
                    break;
                case "DewPoint":
                    series.MarkerSize = 8;
                    break;
                case "SecondPoint":
                    series.MarkerSize = 5;
                    break;
            }
            series.Color = Color.Gray;
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Point;
            series.Tag = UI_MollierProcess.MollierProcess;
            if (pointType == "SecondPoint")
            {
                series.Tag = "SecondPoint";
            }
            series.MarkerStyle = MarkerStyle.Circle;
            series.Points[index].ToolTip = Query.ToolTipText(mollierPoint, chartType, toolTipName);

            return series;
        }
        
        
        // ------------------POINTS-LABELS-------------------------------------
        private static void labelProcessPoints(Chart chart, Control control, List<UIMollierPoint> pointsToLabel, List<UIMollierProcess> mollierProcesses, MollierControlSettings mollierControlSettings)
        {
            Dictionary<Point2D, string> labelsLocations = getLabelsLocations(control, pointsToLabel, 
                                                                mollierProcesses, mollierControlSettings);
            foreach(KeyValuePair<Point2D, string> pointLabelPair in labelsLocations)
            {
                Point2D labelLocation = pointLabelPair.Key;
                string label = pointLabelPair.Value;

                AddLabel(chart, mollierControlSettings, labelLocation.X, labelLocation.Y, 0, 0, 0, 
                        label, ChartDataType.Undefined, ChartParameterType.Point, Color.Black);
            }
        }
      
        private static Dictionary<Point2D, string> getLabelsLocations(Control control, List<UIMollierPoint> pointToLabel, List<UIMollierProcess> mollierProcesses, MollierControlSettings mollierControlSettings)
        {
            List<Segment2D> chartLines = processesToChartLines(mollierProcesses, mollierControlSettings);
            List<Tuple<BoundingBox2D, string>> labelsStartingView = 
                getLabelsStartingView(control, pointToLabel, mollierControlSettings);  // bounding box represents shape of label on a chart
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);

            double distanceFromCenter = mollierControlSettings.ChartType == ChartType.Mollier ? 1.4 : 0.0007;
            double distanceFromEdge = mollierControlSettings.ChartType == ChartType.Mollier ? 1 : 0.0005;

            List<BoundingBox2D> labelsStartingShapesLocations = new List<BoundingBox2D>();
            foreach(Tuple<BoundingBox2D, string> labelStartingView in labelsStartingView)
            {
                labelsStartingShapesLocations.Add(labelStartingView.Item1);
            }

            List<Vector2D> offsets = generateCenterOffsets(scaleVector, mollierControlSettings.ChartType, distanceFromEdge); 

            Dictionary<BoundingBox2D, Vector2D> labelsFoundShapesLocations = FindSolution(labelsStartingShapesLocations, offsets, distanceFromCenter, segments: chartLines);
            Dictionary<BoundingBox2D, Point2D> labelsFoundView = labelsShapesToPoints(labelsFoundShapesLocations, distanceFromCenter, scaleVector);

            Dictionary<Point2D, string> labelsLocations = new Dictionary<Point2D, string>();

            foreach(KeyValuePair<BoundingBox2D, Point2D> labelFoundView in labelsFoundView) 
            {
                labelsLocations.Add(labelFoundView.Value, labelsStartingView.Find(el => el.Item1 == labelFoundView.Key).Item2);
            }       

            return labelsLocations;
        }     
        private static List<UIMollierProcess> generateLabelsForProcessesSystems(List<List<UIMollierProcess>> systems)
        {
            List<UIMollierProcess> labeledMollierProcesses = new List<UIMollierProcess>();

            char name = 'A';
            for (int i = 0; i < systems.Count; i++)
            {
                for (int j = 0; j < systems[i].Count; j++)
                {
                    UIMollierProcess UI_MollierProcess = systems[i][j];
                    MollierProcess mollierProcess = UI_MollierProcess.MollierProcess;
                    if (UI_MollierProcess.UIMollierAppearance_End?.Label == "SUP")
                    {
                        UI_MollierProcess.UIMollierAppearance_End.Label = null;
                    }

                    if (UI_MollierProcess.UIMollierAppearance_Start.Label == null)
                    {
                        UI_MollierProcess.UIMollierAppearance_Start.Label = name + "1";
                    }
                    else if (UI_MollierProcess.UIMollierAppearance_Start.Label == null && systems[i].Count > 1 &&  j == 0)
                    {
                        UI_MollierProcess.UIMollierAppearance_Start.Label = "OSA";
                    }

                    if (UI_MollierProcess.UIMollierAppearance_End.Label == null && systems[i].Count > 1 && j == systems[i].Count - 2 && systems[i][j + 1].MollierProcess is UndefinedProcess)
                    {
                        UI_MollierProcess.UIMollierAppearance_End.Label = "SUP";
                    }
                    else if (UI_MollierProcess.UIMollierAppearance_End.Label == null && systems[i].Count > 1 && j == systems[i].Count - 1)
                    {
                        UI_MollierProcess.UIMollierAppearance_End.Label = "SUP";
                    }

                    UI_MollierProcess.UIMollierAppearance.Label = UI_MollierProcess.UIMollierAppearance.Label == null ? Query.ProcessName(mollierProcess) : UI_MollierProcess.UIMollierAppearance.Label;
                    UI_MollierProcess.UIMollierAppearance_End.Label = UI_MollierProcess.UIMollierAppearance_End.Label == null ? name + "2" : UI_MollierProcess.UIMollierAppearance_End.Label;

                    name++;
                    labeledMollierProcesses.Add(UI_MollierProcess);
                }
            }

            return labeledMollierProcesses;
        //  this.mollierProcesses = mollierProcesses;//used only to remember point name to show it in tooltip
        }
        private static List<Vector2D> generateCenterOffsets(Vector2D scaleVector, ChartType chartType, double distanceFromCenter, int offsetsNumber = 8)
        {
            double xDifference = chartType == ChartType.Mollier ? 0.25 : 1000; // 1 jednostka po y jest wizualnie równa 4 jednostki po x w mollierze
            List<Vector2D> offsets = new List<Vector2D>();
            //   double offsetAngle = 360 / offsetsNumber;

            double offsetAngle = 180;

            for(double angle=0; angle < 360; angle += offsetAngle)
            {
                double radians = ConvertDegreesToRadians(angle);
                double offsetX = distanceFromCenter * System.Math.Sin(radians) * scaleVector.X * xDifference;
                double offsetY = distanceFromCenter * System.Math.Cos(radians) * scaleVector.Y;

                offsets.Add(new Vector2D(offsetX, offsetY));
            }

            offsetAngle = 90;

            for (double angle = 0; angle < 360; angle += offsetAngle)
            {
                double radians = ConvertDegreesToRadians(angle);
                double offsetX = distanceFromCenter * System.Math.Sin(radians) * scaleVector.X * xDifference;
                double offsetY = distanceFromCenter * System.Math.Cos(radians) * scaleVector.Y;

                offsets.Add(new Vector2D(offsetX, offsetY));
            }

            for (double angle = 45; angle < 360; angle += offsetAngle)
            {
                double radians = ConvertDegreesToRadians(angle);
                double offsetX = distanceFromCenter * System.Math.Sin(radians) * scaleVector.X * xDifference;
                double offsetY = distanceFromCenter * System.Math.Cos(radians) * scaleVector.Y;

                offsets.Add(new Vector2D(offsetX, offsetY));
            }
            return offsets;
        }
        private static List<Tuple<BoundingBox2D, string>> getLabelsStartingView(Control control, List<UIMollierPoint> pointsToLabel, MollierControlSettings mollierControlSettings)
        {
            List<Tuple<BoundingBox2D, string>> labelsStartingView = new List<Tuple<BoundingBox2D, string>>();
            Vector2D scaleVector = Query.ScaleVector2D(control, mollierControlSettings);
            ChartType chartType = mollierControlSettings.ChartType;

            double letterHeight = chartType == ChartType.Mollier ? 0.95 * scaleVector.Y : 0.00049 * scaleVector.Y;
            double letterWidth = chartType == ChartType.Mollier ? 0.2 * scaleVector.X : 0.49 * scaleVector.X;


            foreach (UIMollierPoint pointToLabel in pointsToLabel)
            {
                string label = pointToLabel.UIMollierAppearance.Label;
                Point2D point2DToLabel = Convert.ToSAM(pointToLabel.MollierPoint, chartType);

                BoundingBox2D labelShape = createBoxByCenter(point2DToLabel, label.Length * letterWidth, letterHeight);
                labelsStartingView.Add(new Tuple<BoundingBox2D, string>(labelShape, label));
            }

            return labelsStartingView;
        }   
        private static List<Segment2D> processesToChartLines(List<UIMollierProcess> mollierProcesses, MollierControlSettings mollierControlSettings)
        {
            List<Segment2D> lines = new List<Segment2D>();

            foreach (UIMollierProcess process in mollierProcesses)
            {
                Point2D start = Convert.ToSAM(process.Start, mollierControlSettings.ChartType);
                Point2D end = Convert.ToSAM(process.End, mollierControlSettings.ChartType);
                lines.Add(new Segment2D(start, end));
            }

            return lines;
        }
        private static Dictionary<BoundingBox2D, Point2D> labelsShapesToPoints(Dictionary<BoundingBox2D, Vector2D> shapesLocations, double distanceFromCenter, Vector2D scaleVector)
        {
            Dictionary<BoundingBox2D, Point2D> labelsFoundView = new Dictionary<BoundingBox2D, Point2D>();

            foreach(KeyValuePair<BoundingBox2D, Vector2D> shapeLocation in shapesLocations)
            {
                Point2D center = shapeLocation.Key.GetCentroid();
                center.Move(shapeLocation.Value); 
                labelsFoundView[shapeLocation.Key] = new Point2D(center.X, center.Y - (distanceFromCenter * scaleVector.Y));
            }

            return labelsFoundView;
        }
        private static BoundingBox2D shiftedBox(BoundingBox2D box, Vector2D offset)
        {
            double centerHeight = box.Height / 2;
            double centerWidth = box.Width / 2;

            // TODO: uprościć sin, cos z vectora

            double angle = Vector2D.WorldY.Angle(offset);

            double centerX = box.GetCentroid().X + offset.X + centerWidth * System.Math.Sin(angle);
            double centerY = box.GetCentroid().Y + offset.Y + centerHeight * System.Math.Cos(angle);

            return createBoxByCenter(new Point2D(centerX, centerY), box.Width, box.Height);

        }
        public static Dictionary<BoundingBox2D, Vector2D> FindSolution(IEnumerable<BoundingBox2D> boxes, List<Vector2D> offsets, double tolerance = Tolerance.Distance, List<Segment2D> segments = null)
        {
            Dictionary<BoundingBox2D, BoundingBox2D> shiftedBoxes = new Dictionary<BoundingBox2D, BoundingBox2D>();

            foreach(BoundingBox2D box in boxes)
            {
                foreach(Vector2D offset in offsets)
                {
                    BoundingBox2D boxToShift = shiftedBox(box, offset);

                    if(isSeparated(boxToShift, shiftedBoxes.Values) && isSeparated(boxToShift, segments))
                    {
                        shiftedBoxes.Add(box, boxToShift);
                        break;
                    }
                }

            }

            Dictionary<BoundingBox2D, Vector2D> result = new Dictionary<BoundingBox2D, Vector2D>();
            
             foreach(KeyValuePair<BoundingBox2D,BoundingBox2D> shiftedBox in shiftedBoxes)
            {
                result.Add(shiftedBox.Key, new Vector2D((shiftedBox.Value.Min.X - shiftedBox.Key.Min.X), 
                                                        (shiftedBox.Value.Min.Y - shiftedBox.Key.Min.Y)));
            }
            return result;
        }

        
        // General methods used above 
        private static double ConvertDegreesToRadians(double angle)
        {
            return System.Math.PI * angle / 180;
        }
        private static bool isSeparated(BoundingBox2D box, IEnumerable<BoundingBox2D> boxes)
        {
            foreach (BoundingBox2D boxTemp in boxes)
            {
                if (box.InRange(boxTemp))
                {
                    return false;
                }
            }
            return true;
        }
        private static bool isSeparated(BoundingBox2D box, List<Segment2D> segments, double tolerance = Tolerance.Distance)
        {
            foreach (Segment2D segment in segments)
            {
                if (Geometry.Planar.Query.Intersect(box, segment, tolerance))
                {
                    return false;
                }
            }
            return true;
        }
        private static BoundingBox2D createBoxByCenter(Point2D center, double width, double height)
        {
            // TODO: Move to SAM.Geometry.Planar.Create [JAKUB]
            Point2D min = new Point2D(center.X - width / 2, center.Y - height / 2);
            Point2D max = new Point2D(center.X + width / 2, center.Y + height / 2);

            return new BoundingBox2D(min, max);
        }
        private static MollierPoint mollierPointsMid(MollierPoint mollierPoint1, MollierPoint mollierPoint2)
        {
            double dryBulbTemperature = (mollierPoint1.DryBulbTemperature + mollierPoint2.DryBulbTemperature) / 2;
            double humidityRatio = (mollierPoint1.HumidityRatio + mollierPoint2.HumidityRatio) / 2;
            return new MollierPoint(dryBulbTemperature, humidityRatio, Standard.Pressure);
        }

    }
}
