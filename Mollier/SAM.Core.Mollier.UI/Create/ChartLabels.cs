using SAM.Core.Mollier.UI.Classes;
using SAM.Geometry.Mollier;
using SAM.Geometry.Planar;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public static partial class Create
    {
        public static List<ChartLabel> ChartLabels(List<Solver2DResult> solver2DResults, MollierControlSettings mollierControlSettings, Vector2D scaleVector, double axesRatio)
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
                Rectangle2D labelShape = solver2DResult.Closed2D<Rectangle2D>();
                if (labelShape == null)
                {
                    continue;
                }

                UIMollierPoint uIMollierPoint = solver2DData.Tag as UIMollierPoint;
                if (uIMollierPoint == null)
                {
                    continue;
                }

                Tuple<Point2D, double> positionAngleLabel = getPositionAngle(labelShape);

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
