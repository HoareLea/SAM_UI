using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointEventArgs : EventArgs
    {
        public MollierPoint MolierPoint { get; private set;  }

        public MollierPointEventArgs(MollierPoint mollierPoint)
            :base()
        {
            MolierPoint = mollierPoint;
        }
    }
}
