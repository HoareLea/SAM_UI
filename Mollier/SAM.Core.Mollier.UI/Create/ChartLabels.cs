using SAM.Core.Mollier.UI.Classes;
using SAM.Geometry.Mollier;
using SAM.Geometry.Planar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Create
    {
        public static List<ChartLabel> ChartLabels(List<Solver2DResult> solver2DResults, MollierControlSettings mollierControlSettings, Vector2D scaleVector, double axesRatio, Chart chart)
        {
            if (solver2DResults == null)
            {
                return null;
            }

            List<ChartLabel> result = new List<ChartLabel>();
            ChartType chartType = mollierControlSettings.ChartType;

            Func<Vector2D, int> vectorToAngle = new Func<Vector2D, int>((Vector2D vector) =>
            {
                if (vector.Y == 0) return 0;
                if (vector.X == 0) return 90;

                double tan = chartType == ChartType.Mollier ? vector.Y / vector.X : vector.Y / vector.X;
                int angle = -System.Convert.ToInt32(System.Math.Atan(tan) * 180 / System.Math.PI);

                return angle;
            });


            //TODO: [JAKUB] Find better method to wait
            //bool valid = false;
            //try
            //{
            //    chart.ChartAreas[0].AxisX.ValueToPixelPosition(0);
            //    valid = true;
            //}
            //catch
            //{
            //    valid = false;
            //}

            //while(!valid)
            //{
            //    chart.Invalidate();
            //    System.Threading.Thread.Sleep(1000);
            //    try
            //    {
            //        chart.ChartAreas[0].AxisX.ValueToPixelPosition(0);
            //        valid = true;
            //    }
            //    catch
            //    {
            //        valid = false;
            //    }
            //}

            Func<Rectangle2D, Tuple<Point2D, double>> getPositionAngle = new Func<Rectangle2D, Tuple<Point2D, double>>((Rectangle2D rectangle) => 
            {
                if (rectangle == null)
                {
                    return null;
                }

                double distanceFromCenter = (chartType == ChartType.Mollier ? 0.8 : 0.4) * scaleVector.Y;
                Point2D center = rectangle.GetCentroid().GetScaledY(1 / axesRatio); // re-scaled point
                Point2D point = new Point2D(center.X, center.Y - distanceFromCenter);

                double angle = vectorToAngle(rectangle.WidthDirection);

                return new Tuple<Point2D, double>(point, angle);
            });

            foreach (Solver2DResult solver2DResult in solver2DResults)
            {
                Solver2DData solver2DData = solver2DResult.Solver2DData;
                Rectangle2D rectangle2D = solver2DResult.Closed2D<Rectangle2D>();
                if (rectangle2D == null)
                {
                    continue;
                }

                UIMollierPoint uIMollierPoint = solver2DData.Tag as UIMollierPoint;
                if (uIMollierPoint == null)
                {
                    continue;
                }

                Tuple<Point2D, double> positionAngleLabel = getPositionAngle(rectangle2D);

                //START
                //Point2D point = rectangle2D.GetCentroid().GetScaledY(1 / axesRatio);

                //Vector2D vector2D = rectangle2D.HeightDirection * chart.Series[0].Font.Height;

                //point = new Point2D(chart.ChartAreas[0].AxisX.ValueToPixelPosition(point.X), chart.ChartAreas[0].AxisY.ValueToPixelPosition(point.Y));
                //point = point.GetMoved(vector2D.GetNegated());
                //point = new Point2D(chart.ChartAreas[0].AxisX.PixelPositionToValue(point.X), chart.ChartAreas[0].AxisY.PixelPositionToValue(point.Y));

                //positionAngleLabel = new Tuple<Point2D, double>(point, positionAngleLabel.Item2);
                //END

                string text = null;
                Color color = Color.Empty;

                UIMollierAppearance uIMollierAppearance = uIMollierPoint.UIMollierAppearance as UIMollierAppearance;
                if (uIMollierAppearance != null)
                {
                    text = (uIMollierPoint.UIMollierAppearance as UIMollierAppearance).Label;
                    color = uIMollierPoint.UIMollierAppearance.Color;

                    if (uIMollierAppearance.UIMollierLabelAppearance != null)
                    {
                        color = uIMollierAppearance.UIMollierLabelAppearance.Color;
                    }

                    if (color == Color.Empty)
                    {
                        color = Color.Black;
                    }
                }

                result.Add(new ChartLabel(positionAngleLabel.Item1, text, positionAngleLabel.Item2, color) { Tag = uIMollierPoint });
            }

            return result;
        }

    }
}
