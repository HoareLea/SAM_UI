using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static DataPoint ToChart(this Point2D point2D)
        {
            if(point2D == null)
            {
                return null;
            }

            return new DataPoint(point2D.X, point2D.Y);
        }
    }
}

