using SAM.Geometry.Planar;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        [Obsolete("To be removed Maciek")] // TODO: Remove (MollierProcess)
        public static bool Intersect(Control control, MollierControlSettings mollierControlSettings, MollierPoint mollierPointNew, string label, List<UIMollierProcess> mollierProcesses)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            double NewPoint_X = chartType == ChartType.Mollier ? mollierPointNew.HumidityRatio * 1000 : mollierPointNew.DryBulbTemperature;
            double NewPoint_Y = chartType == ChartType.Mollier ? mollierPointNew.DryBulbTemperature : mollierPointNew.HumidityRatio;
            Vector2D vector2D = Query.ScaleVector2D(control, mollierControlSettings);
            double y = 0.95 * vector2D.Y;
            double x = 0.2 * vector2D.X;// 0.2 is one letter width in mollier
            x *= label.Length;
            if (chartType == ChartType.Psychrometric)
            {
                y = 0.00048 * vector2D.Y;
                x = 0.48 * vector2D.X;// 0.25 is one letter width in psychro
                x *= label.Length;
            }
            Point2D origin = new Point2D(NewPoint_X - x / 2.0, NewPoint_Y + y);

            Rectangle2D rectangle2Dnew = new Rectangle2D(origin, x, y);

            for (int i = 0; i < mollierProcesses.Count; i++)
            {
                IMollierProcess mollierProcess = mollierProcesses[i].MollierProcess;
                Point2D start = chartType == ChartType.Mollier ? new Point2D(mollierProcess.Start.HumidityRatio * 1000, mollierProcess.Start.DryBulbTemperature) : new Point2D(mollierProcess.Start.DryBulbTemperature, mollierProcess.Start.HumidityRatio);
                Point2D end = chartType == ChartType.Mollier ? new Point2D(mollierProcess.End.HumidityRatio * 1000, mollierProcess.End.DryBulbTemperature) : new Point2D(mollierProcess.End.DryBulbTemperature, mollierProcess.End.HumidityRatio);

                Segment2D segment2D = new Segment2D(start, end);

                if (rectangle2Dnew.Intersect(segment2D, Tolerance.MicroDistance))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
