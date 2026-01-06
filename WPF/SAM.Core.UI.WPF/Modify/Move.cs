using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static bool Move(this ProjectionCamera projectionCamera, Vector3D axis, double step)
        {
            if(projectionCamera == null || axis == null)
            {
                return false;
            }

            projectionCamera.Position += axis * step;
            return true;
        }

        public static bool Move(this ProjectionCamera projectionCamera, Key key, double step)
        {
            if (projectionCamera == null)
            {
                return false;
            }

            switch (key)
            {
                case Key.W:
                    return projectionCamera.Move(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? projectionCamera.YawAxis() : projectionCamera.RollAxis(), +step);
                    
                case Key.S:
                    return projectionCamera.Move(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? projectionCamera.YawAxis() : projectionCamera.RollAxis(), -step);

                case Key.A:
                    return projectionCamera.Move(projectionCamera.PitchAxis(), +step);

                case Key.D:
                    return projectionCamera.Move(projectionCamera.PitchAxis(), -step);
            }

            return false;
        }

        public static bool Move(this PerspectiveCamera perspectiveCamera, Key key) 
        {
            if(perspectiveCamera == null)
            {
                return false;
            }

            return perspectiveCamera.Move(key, perspectiveCamera.FieldOfView / 180d);
        } 
    }
}