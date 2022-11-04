using System;
using System.Windows.Input;

namespace SAM.Core.UI.WPF
{
    public class ObjectDoubleClickedEventArgs : EventArgs
    {
        public IVisualJSAMObject VisualJSAMObject { get; }
        public MouseButtonEventArgs MouseButtonEventArgs { get; }

        public ObjectDoubleClickedEventArgs(MouseButtonEventArgs mouseButtonEventArgs, IVisualJSAMObject visualJSAMObject)
        {
            MouseButtonEventArgs = mouseButtonEventArgs;
            VisualJSAMObject = visualJSAMObject;
        }
    }
}
