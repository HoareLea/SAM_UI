using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static T Move<T>(this T projectionCamera, Vector3D axis, double step) where T : ProjectionCamera
        {
            if(projectionCamera == null || axis == null)
            {
                return null;
            }

            projectionCamera.Position += axis * step;
            return projectionCamera;
        }

        public static T Move<T>(this T projectionCamera, Key key, double step) where T : ProjectionCamera
        {
            if (projectionCamera == null)
            {
                return null;
            }

            switch (key)
            {
                case Key.W:
                    projectionCamera.Move(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? projectionCamera.YawAxis() : projectionCamera.RollAxis(), +step);
                    break;
                case Key.S:
                    projectionCamera.Move(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? projectionCamera.YawAxis() : projectionCamera.RollAxis(), -step);
                    break;
                case Key.A:
                    projectionCamera.Move(projectionCamera.PitchAxis(), +step);
                    break;
                case Key.D:
                    projectionCamera.Move(projectionCamera.PitchAxis(), -step);
                    break;
            }

            return projectionCamera;
        }

        public static PerspectiveCamera Move(this PerspectiveCamera perspectiveCamera, Key key) 
        {
            if(perspectiveCamera == null)
            {
                return null;
            }

            perspectiveCamera.Move(key, perspectiveCamera.FieldOfView / 180d);
            return perspectiveCamera;
        } 
    }
}