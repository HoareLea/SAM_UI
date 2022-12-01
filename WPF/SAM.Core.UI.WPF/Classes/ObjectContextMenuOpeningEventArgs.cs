using System;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public class ObjectContextMenuOpeningEventArgs : EventArgs
    {
        public ModelVisual3D ModelVisual3D { get; }
        public ContextMenuEventArgs ContextMenuEventArgs { get; }
        public ContextMenu ContextMenu { get; }

        public ObjectContextMenuOpeningEventArgs(ContextMenu contextMenu, ContextMenuEventArgs contextMenuEventArgs, ModelVisual3D modelVisual3D)
        {
            ContextMenu = contextMenu;
            ContextMenuEventArgs = contextMenuEventArgs;
            ModelVisual3D = modelVisual3D;
        }
    }
}
