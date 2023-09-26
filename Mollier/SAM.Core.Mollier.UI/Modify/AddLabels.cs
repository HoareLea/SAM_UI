using System.Drawing;
using System;
using System.Windows.Forms.DataVisualization.Charting;
using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        /// <summary>
        /// Creates series for labels of all mollier objects that exist on the chart.
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of created series</returns>
        public static List<Series> AddLabels(this Chart chart, MollierControlSettings mollierControlSettings)
        {
            Vector2D scaleVector = Query.ScaleVector2D(chart.Parent, mollierControlSettings);
            double axesRatio = Query.AxesRatio(chart, mollierControlSettings);

            List<IClosed2D> obstacles = Query.Obstacles(chart, mollierControlSettings);

            List<Solver2DData> solverData = Create.Solver2DDatas(chart, mollierControlSettings);

            Point2D chartMinPoint = new Point2D(chart.ChartAreas[0].AxisX.Minimum, chart.ChartAreas[0].AxisY.Minimum * axesRatio);
            Point2D chartMaxPoint = new Point2D(chart.ChartAreas[0].AxisX.Maximum, chart.ChartAreas[0].AxisY.Maximum * axesRatio);
            Rectangle2D chartArea = new Rectangle2D(new BoundingBox2D(chartMinPoint, chartMaxPoint));

            Solver2DSettings solver2DSettings = new Solver2DSettings();
            solver2DSettings.MaxStepPoint = 5;
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
                if (rectangle != null && mollierControlSettings.VisualizeSolver)
                {
                    visualizeRectangle(chart, rectangle, Color.Red, axesRatio);
                }
            }

            if (solverData == null || solverData.Count == 0)
            {
                return null;
            }

            List<Solver2DResult> solver2DResults = solver.Solve(solver2DSettings);

            if (solver2DResults == null) return null;

            List<ChartLabel> labelsPositions = getChartLabels(solver2DResults, mollierControlSettings, scaleVector, axesRatio);
            
            labelsPositions.fixPositions(chart);
            List<Series> result = addChartLabels(chart, labelsPositions);

            return result;
        }
        private static List<Series> addChartLabels(Chart chart, List<ChartLabel> chartLabels)
        {
            List<Series> result = new List<Series>();

            foreach (ChartLabel chartLabel in chartLabels)
            {
                result.Add(addChartLabel(chart, chartLabel));
            }

            return result;
        }
        private static Series addChartLabel(Chart chart, ChartLabel chartLabel)
        {
            Series result = chart.Series.Add(Guid.NewGuid().ToString());
            result.SmartLabelStyle.Enabled = false;
            result.IsVisibleInLegend = false;
            result.Color = Color.Transparent;
            result.ChartType = SeriesChartType.Point;
            result.Points.AddXY(chartLabel.Position.X, chartLabel.Position.Y);
            result.Label = chartLabel.Text;
            result.LabelAngle = System.Convert.ToInt32(chartLabel.Angle) % 90;
            result.LabelForeColor = chartLabel.Color;
            result.Tag = "Label " + Guid.NewGuid().ToString();

            return result;
        }



        private static void visualizeRectangle(Chart chart, Rectangle2D rectangle, Color color, double yTOX)
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
        
        // TODO: [LABELS] Methods used above and to move from there
        private static List<ChartLabel> getChartLabels(List<Solver2DResult> solver2DResults, MollierControlSettings mollierControlSettings, Vector2D scaleVector, double axesRatio)
        {
            if (solver2DResults == null) return null;

            List<ChartLabel> result = new List<ChartLabel>();
            ChartType chartType = mollierControlSettings.ChartType;

            foreach (Solver2DResult solver2DResult in solver2DResults)
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

                result.Add(new ChartLabel(positionAngleLabel.Item1, text, positionAngleLabel.Item2, color));
            }

            return result;
        }
        private static Tuple<Point2D, double> getPositionAngle(Rectangle2D rectangle, ChartType chartType, Vector2D scaleVector, double axesRatio)
        {
            if (rectangle == null) return null;

            double distanceFromCenter = (chartType == ChartType.Mollier ? 0.8  : 0.4) * scaleVector.Y;
            Point2D center = rectangle.GetCentroid().GetScaledY(1 / axesRatio); // re-scaled point
            Point2D point = new Point2D(center.X, center.Y - distanceFromCenter);

            double angle = vectorToAngle(rectangle.WidthDirection, chartType);

            return new Tuple<Point2D, double>(point, angle);
        }
        private static List<ChartLabel> fixPositions(this List<ChartLabel> chartLabels, Chart chart, double tolerance = Tolerance.Distance)
        {
            foreach (ChartLabel chartLabel in chartLabels)
            {
                chartLabel.Position.Y = System.Math.Max(chartLabel.Position.Y, chart.ChartAreas[0].AxisY.Minimum);
            }
            return chartLabels;
        }
        private static int vectorToAngle(Vector2D vector, ChartType chartType)
        {
            if (vector.Y == 0) return 0;
            if (vector.X == 0) return 90;

            double tan = chartType == ChartType.Mollier ? vector.Y / vector.X : vector.Y / vector.X;
            int angle = -System.Convert.ToInt32(System.Math.Atan(tan) * 180 / System.Math.PI);

            return angle;
        }

    }
}
