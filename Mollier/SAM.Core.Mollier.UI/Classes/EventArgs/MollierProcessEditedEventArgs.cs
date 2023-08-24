using System;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public class MollierProcessEditedEventArgs : EventArgs
    {
        public List<UIMollierProcess> UIMollierProcesses { get; set; }
        public UIMollierProcess UIMollierProcess { get; private set;}
        public UIMollierProcess EditedUIMollierProcess { get; private set;}

        public MollierProcessEditedEventArgs(UIMollierProcess uIMollierProcess, UIMollierProcess editedUIMollierProcess)
            :base()
        {
            UIMollierProcess = uIMollierProcess;
            EditedUIMollierProcess = editedUIMollierProcess;
        }


    }
}
