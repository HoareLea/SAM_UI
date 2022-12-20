using SAM.Core;
using System;

namespace SAM.Geometry.UI.WPF
{
    public class ZoomRequestedEventArgs : EventArgs
    {
        public SAMObject SAMObject { get; }

        public ZoomRequestedEventArgs(SAMObject sAMObject)
        {
            SAMObject = sAMObject;
        }
    }
}
