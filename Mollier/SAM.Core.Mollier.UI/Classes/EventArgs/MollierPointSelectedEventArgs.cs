using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointSelectedEventArgs : EventArgs
    {
        public MollierPoint MollierPoint {get; private set;}

        public MollierPointSelectedEventArgs(MollierPoint mollierPoint)
            :base()
        {
            MollierPoint = mollierPoint;
        }
    }
}
