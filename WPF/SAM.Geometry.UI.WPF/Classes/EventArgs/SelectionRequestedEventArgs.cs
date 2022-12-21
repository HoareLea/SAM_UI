using SAM.Core;
using System;

namespace SAM.Geometry.UI.WPF
{
    public class SelectionRequestedEventArgs : EventArgs
    {
        public SAMObject SAMObject { get; }

        public SelectionRequestedEventArgs(SAMObject sAMObject)
        {
            SAMObject = sAMObject;
        }
    }
}
