using System;
using System.Windows.Input;

namespace SAM.Core.UI.WPF
{
    public class ObjectHooveredEventArgs : EventArgs
    {
        public IVisualJSAMObject VisualJSAMObject { get; }
        public MouseEventArgs MouseEventArgs { get; }

        public ObjectHooveredEventArgs(MouseEventArgs mouseEventArgs, IVisualJSAMObject visualJSAMObject)
        {
            MouseEventArgs = mouseEventArgs;
            VisualJSAMObject = visualJSAMObject;
        }
    }
}
