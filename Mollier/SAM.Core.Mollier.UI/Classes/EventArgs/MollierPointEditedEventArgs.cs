using System;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointEditedEventArgs : EventArgs
    {
        public List<UIMollierPoint> UIMollierPoints { get; set; }
        public UIMollierPoint UIMollierPoint { get; private set;}
        public UIMollierPoint EditedUIMollierPoint { get; private set;}

        public MollierPointEditedEventArgs(UIMollierPoint uIMollierPoint, UIMollierPoint editedUIMollierPoint)
            :base()
        {
            UIMollierPoint = uIMollierPoint;
            EditedUIMollierPoint = editedUIMollierPoint;
        }

    }
}
