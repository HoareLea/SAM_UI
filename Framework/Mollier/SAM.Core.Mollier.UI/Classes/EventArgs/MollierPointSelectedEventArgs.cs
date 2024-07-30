using SAM.Geometry.Planar;
using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointSelectedEventArgs : EventArgs
    {
        public MollierPoint MollierPoint {get;}

        public Point2D Location { get;}

        public MollierPointSelectedEventArgs(MollierPoint mollierPoint, Point2D location)
            :base()
        {
            MollierPoint = mollierPoint;
            Location = location;
        }
    }
}
