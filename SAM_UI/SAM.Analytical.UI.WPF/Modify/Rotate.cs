using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static T Rotate<T>(this T projectionCamera, Vector3D axis, double angle) where T : ProjectionCamera
        {
            if(projectionCamera == null || axis == null)
            {
                return null;
            }

            Matrix3D matrix3D = new Matrix3D();
            matrix3D.RotateAt(new Quaternion(axis, angle), projectionCamera.Position);
            projectionCamera.LookDirection *= matrix3D;
            return projectionCamera;
        }

        public static T Rotate<T>(this T projectionCamera, Vector3D axis, double angle, Point3D center) where T : ProjectionCamera
        {
            if (projectionCamera == null || axis == null)
            {
                return null;
            }

            Matrix3D matrix3D = new Matrix3D();
            matrix3D.RotateAt(new Quaternion(axis, angle), center);
            projectionCamera.LookDirection *= matrix3D;
            return projectionCamera;
        }

        public static T Rotate<T>(this T projectionCamera, Key key, double angle) where T : ProjectionCamera
        {
            if (projectionCamera == null)
            {
                return null;
            }

            switch (key)
            {
                case Key.Left:
                    projectionCamera.Rotate(projectionCamera.YawAxis(), +angle);
                    break;

                case Key.Right:
                    projectionCamera.Rotate(projectionCamera.YawAxis(), -angle);
                    break;

                case Key.Down:
                    projectionCamera.Rotate(projectionCamera.PitchAxis(), +angle);
                    break;

                case Key.Up:
                    projectionCamera.Rotate(projectionCamera.PitchAxis(), -angle);
                    break;
            }

            return projectionCamera;
        }

        public static PerspectiveCamera Rotate(this PerspectiveCamera perspectiveCamera, Key key)
        {
            if(perspectiveCamera == null)
            {
                return null;
            }

            perspectiveCamera.Rotate(key, perspectiveCamera.FieldOfView / 45d);
            return perspectiveCamera;
        }
    }
}