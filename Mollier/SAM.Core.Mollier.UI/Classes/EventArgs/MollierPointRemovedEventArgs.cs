using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointRemovedEventArgs : EventArgs
    {
        public UIMollierPoint UIMollierPoint {get; private set;}

        public MollierPointRemovedEventArgs(UIMollierPoint uIMollierPoint)
            :base()
        {
            UIMollierPoint = uIMollierPoint;
        }
    }
}
