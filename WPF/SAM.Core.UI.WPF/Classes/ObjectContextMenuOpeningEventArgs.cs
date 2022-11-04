using System;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    public class ObjectContextMenuOpeningEventArgs : EventArgs
    {
        public IVisualJSAMObject VisualJSAMObject { get; }
        public ContextMenuEventArgs ContextMenuEventArgs { get; }
        public ContextMenu ContextMenu { get; }

        public ObjectContextMenuOpeningEventArgs(ContextMenu contextMenu, ContextMenuEventArgs contextMenuEventArgs, IVisualJSAMObject visualJSAMObject)
        {
            ContextMenu = contextMenu;
            ContextMenuEventArgs = contextMenuEventArgs;
            VisualJSAMObject = visualJSAMObject;
        }
    }
}
