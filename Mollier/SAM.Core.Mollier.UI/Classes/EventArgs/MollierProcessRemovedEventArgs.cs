using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierProcessRemovedEventArgs : EventArgs
    {
        public UIMollierProcess MollierProcess { get; private set;}

        public MollierProcessRemovedEventArgs(UIMollierProcess mollierProcess)
            :base()
        {
            mollierProcess = mollierProcess;
        }
    }
}
