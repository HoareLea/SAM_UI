using System;

namespace SAM.Core.Mollier.UI
{
    public class SelectMollierPointEventArgs : EventArgs
    {
        public MollierPoint MollierPoint {get; set;}

        public SelectMollierPointEventArgs()
            :base()
        {

        }
    }
}
