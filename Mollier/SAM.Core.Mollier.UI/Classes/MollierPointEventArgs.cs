using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointEventArgs : EventArgs
    {
        public MollierPoint MollierPoint { get; private set;  }

        public MollierPointEventArgs(MollierPoint mollierPoint)
            :base()
        {
            MollierPoint = mollierPoint;
        }
    }
}
