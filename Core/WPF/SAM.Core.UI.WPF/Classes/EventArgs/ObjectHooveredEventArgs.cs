using System;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public class ObjectHooveredEventArgs : EventArgs
    {
        public ModelVisual3D ModelVisual3D { get; }
        public MouseEventArgs MouseEventArgs { get; }

        public ObjectHooveredEventArgs(MouseEventArgs mouseEventArgs, ModelVisual3D modelVisual3D)
        {
            MouseEventArgs = mouseEventArgs;
            ModelVisual3D = modelVisual3D;
        }
    }
}
