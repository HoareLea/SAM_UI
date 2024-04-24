using System.Drawing;
using System;
using System.Windows.Forms.DataVisualization.Charting;
using SAM.Geometry.Planar;
using System.Collections.Generic;
using SAM.Geometry.Mollier;
using SAM.Core.Mollier.UI.Classes;
using System.Windows.Forms;

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
            //scaleVector = new Vector2D(scaleVector.X, scaleVector.Y > 1 ? 1 : scaleVector.Y); //TODO: [JAKUB] TEMPORARY ADDED

            double axesRatio = Query.AxesRatio(chart, mollierControlSettings);

            List<IClosed2D> obstacles = Query.Obstacles(chart, mollierControlSettings);
            List<Solver2DData> solver2DDatas = Create.Solver2DDatas(chart, mollierControlSettings);

            Point2D chartMinPoint = new Point2D(chart.ChartAreas[0].AxisX.Minimum, chart.ChartAreas[0].AxisY.Minimum * axesRatio);
            Point2D chartMaxPoint = new Point2D(chart.ChartAreas[0].AxisX.Maximum, chart.ChartAreas[0].AxisY.Maximum * axesRatio);
            Rectangle2D chartArea = new Rectangle2D(new BoundingBox2D(chartMinPoint, chartMaxPoint));

            Solver2DSettings solver2DSettings = new Solver2DSettings();
            //solver2DSettings.MaxStepPoint = 5;
            Solver2D solver = new Solver2D(chartArea, obstacles);

            solver.AddRange(solver2DDatas);

            if(mollierControlSettings.VisualizeSolver)
            {
                Action<Rectangle2D, Color, double> visualizeRectangle = new Action<Rectangle2D, Color, double>((Rectangle2D rectangle2D, Color color, double yTOX) => 
                {
                    Series series = chart.Series.Add(Guid.NewGuid().ToString());
                    series.IsVisibleInLegend = false;
                    series.ChartType = SeriesChartType.Line;
                    series.BorderWidth = 1;
                    series.Color = color;

                    List<Point2D> cornerPoints = rectangle2D.GetPoints();
                    foreach (Point2D point in cornerPoints)
                    {
                        series.Points.AddXY(point.X, point.Y / yTOX);
                    }

                    series.Points.AddXY(cornerPoints[0].X, cornerPoints[0].Y / yTOX);
                });
                
                solver2DDatas.ForEach(x => visualizeRectangle(x.Closed2D<Rectangle2D>(), Color.Red, axesRatio));
            }

            if (solver2DDatas == null || solver2DDatas.Count == 0)
            {
                return null;
            }

            List<Solver2DResult> solver2DResults = solver.Solve();
            if (solver2DResults == null)
            {
                return null;
            }

            List<ChartLabel> chartLabels = Create.ChartLabels(solver2DResults, mollierControlSettings, scaleVector, axesRatio, chart);
            for(int i=0; i < chartLabels.Count; i++)
            {
                chartLabels[i].Position.Y = System.Math.Max(chartLabels[i].Position.Y, chart.ChartAreas[0].AxisY.Minimum);
            }

            List<Series> result = new List<Series>();

            foreach (ChartLabel chartLabel in chartLabels)
            {
                result.Add(AddLabel(chart, chartLabel));
            }

            List<Series> seriesList = AddLabels_Moved(chart, mollierControlSettings);
            if(seriesList != null)
            {
                result.AddRange(seriesList);
            }

            return result;
        }

        private static List<Series> AddLabels_Moved(Chart chart, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            Vector2D scaleVector = Query.ScaleVector2D(chart.Parent, mollierControlSettings);
            double axesRatio = Query.AxesRatio(chart, mollierControlSettings);

            List<Tuple<UIMollierPoint, Series>> tuples = new List<Tuple<UIMollierPoint, Series>>();
            foreach (Series series in chart.Series)
            {
                if(series.Tag is IEnumerable<UIMollierPoint> || series.Tag is UIMollierProcess || series.Tag is UIMollierPoint)
                {
                    if (series.Tag is UIMollierPoint)
                    {
                        tuples.Add(new Tuple<UIMollierPoint, Series>((UIMollierPoint)series.Tag, series));
                        continue;
                    }

                    foreach (DataPoint dataPoint in series.Points)
                    {
                        if (dataPoint.Tag is UIMollierPoint)
                        {
                            tuples.Add(new Tuple<UIMollierPoint, Series>((UIMollierPoint)dataPoint.Tag, series));
                        }
                    }
                }
            }

            List<Series> result = new List<Series>();

            foreach(Tuple<UIMollierPoint, Series> tuple in tuples)
            {
                UIMollierPoint uIMollierPoint = tuple.Item1;
                Series series = tuple.Item2;

                UIMollierPointAppearance uIMollierPointAppearance = uIMollierPoint.UIMollierAppearance as UIMollierPointAppearance;
                if (uIMollierPointAppearance == null)
                {
                    continue;
                }

                UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierPointAppearance.UIMollierLabelAppearance;
                if (uIMollierLabelAppearance == null)
                {
                    continue;
                }

                Vector2D vector2D = uIMollierLabelAppearance.Vector2D;
                if (vector2D == null)
                {
                    continue;
                }

                string text = uIMollierLabelAppearance.Text;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                Color color = uIMollierLabelAppearance.Color;
                if (color == Color.Empty)
                {
                    color = Color.Black;
                }

                Point2D point = Convert.ToSAM(uIMollierPoint, chartType);
                point.Move(vector2D);

                point = new Point2D(chart.ChartAreas[0].AxisX.ValueToPixelPosition(point.X), chart.ChartAreas[0].AxisY.ValueToPixelPosition(point.Y));

                Size size = TextRenderer.MeasureText(text, series.Font);

                point = new Point2D(chart.ChartAreas[0].AxisX.PixelPositionToValue(point.X), chart.ChartAreas[0].AxisY.PixelPositionToValue(point.Y + size.Height));

                ChartLabel chartLabel = new ChartLabel(point, text, 0, color) { Tag = uIMollierPoint };

                Series series_Temp = AddLabel(chart, chartLabel);
                if (series_Temp != null)
                {
                    result.Add(series_Temp);
                }
            }

            return result;
        }       
    }
}
