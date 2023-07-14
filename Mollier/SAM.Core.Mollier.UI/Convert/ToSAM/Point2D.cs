using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static Point2D ToSAM(this DataPoint dataPoint, int indexY = 0)
        {
            if(dataPoint == null || indexY == -1)
            {
                return null;
            }

            return new Point2D(dataPoint.XValue, dataPoint.YValues[indexY]);
        }

    }
}

