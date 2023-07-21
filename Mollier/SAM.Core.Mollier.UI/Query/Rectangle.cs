using SAM.Geometry.Planar;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Rectangle Rectangle(Point p1, Point p2)
        {
            return new Rectangle(System.Math.Min(p1.X, p2.X), System.Math.Min(p1.Y, p2.Y), System.Math.Abs(p1.X - p2.X), System.Math.Abs(p1.Y - p2.Y));
        }

    }
}
