using SAM.Geometry.Planar;
using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        [Obsolete("To be removed Maciek")] // TODO: Remove (MollierProcess)
        public static bool Overlaps(Control control, MollierControlSettings mollierControlSettings, MollierPoint mollierPointNew, Tuple<MollierPoint, string> mollierPointLabeled, string label)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            double NewPoint_X = chartType == ChartType.Mollier ? mollierPointNew.HumidityRatio * 1000 : mollierPointNew.DryBulbTemperature;
            double NewPoint_Y = chartType == ChartType.Mollier ? mollierPointNew.DryBulbTemperature : mollierPointNew.HumidityRatio;
            double OldPoint_X = chartType == ChartType.Mollier ? mollierPointLabeled.Item1.HumidityRatio * 1000 : mollierPointLabeled.Item1.DryBulbTemperature;
            double OldPoint_Y = chartType == ChartType.Mollier ? mollierPointLabeled.Item1.DryBulbTemperature : mollierPointLabeled.Item1.HumidityRatio;
            string OldLabel = mollierPointLabeled.Item2;
            Vector2D vector2D = Query.ScaleVector2D(control, mollierControlSettings);
            if (chartType == ChartType.Mollier)
            {
                double y = 0.95 * vector2D.Y;
                double x = 0.2 * vector2D.X;// 0.2 is one letter width in mollier
                x *= label.Length;

                Point2D origin = new Point2D(NewPoint_X - x / 2.0, NewPoint_Y + y);
                Rectangle2D rectangle2Dnew = new Rectangle2D(origin, x, y);


                x = 0.2 * vector2D.X;
                x *= OldLabel.Length;
                origin = new Point2D(OldPoint_X - x / 2.0, OldPoint_Y + y);
                Rectangle2D rectangle2Dold = new Rectangle2D(origin, x, y);

                return rectangle2Dnew.Intersect(rectangle2Dold, 0.001);
            }
            else
            {
                double y = 0.00049 * vector2D.Y;
                double x = 0.49 * vector2D.X;// 0.25 is one letter width in psychro
                x *= label.Length;

                Point2D origin = new Point2D(NewPoint_X - x / 2.0, NewPoint_Y + y);
                Rectangle2D rectangle2Dnew = new Rectangle2D(origin, x, y);


                x = 0.49 * vector2D.X;
                x *= OldLabel.Length;
                origin = new Point2D(OldPoint_X - x / 2.0, OldPoint_Y + y);
                Rectangle2D rectangle2Dold = new Rectangle2D(origin, x, y);

                return rectangle2Dnew.Intersect(rectangle2Dold, 0.0000001);
            }
            return false;

        }
    }
}
