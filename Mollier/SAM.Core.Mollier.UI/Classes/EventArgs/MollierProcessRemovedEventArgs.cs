using System;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public class MollierProcessRemovedEventArgs : EventArgs
    {
        public UIMollierProcess UIMollierProcess { get; private set;}

        public List<UIMollierProcess> UIMollierProcesses { get; set; }
        public MollierProcessRemovedEventArgs(UIMollierProcess uIMollierProcess)
            :base()
        {
            UIMollierProcess = uIMollierProcess;
        }


    }
}
