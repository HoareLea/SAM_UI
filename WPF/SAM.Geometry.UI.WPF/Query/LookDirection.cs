using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static Vector3D LookDirection(this Point3D target, Point3D observer, out Vector3D upDirection)
        {
            Vector3D result = target - observer;
            result.Normalize();

            double a = result.X;
            double b = result.Y;
            double c = result.Z;

            //--- Find the one and only up vector (x, y, z) which has a positive z value (1),
            //--- which is perpendicular to the look vector (2) and and which ensures that
            //--- the resulting roll angle is 0, i.e. the resulting left vector (= up cross look)
            //--- lies within the xy-plane (or has a z value of 0) (3). In other words:
            //--- 1. z > 0 (e.g. 1)
            //--- 2. ax + by + cz = 0
            //--- 3. ay - bx = 0
            //--- If the observer position is right above or below the target point, i.e. a = b = 0 and c != 0,
            //--- we set the up vector to (1, 0, 0) for c > 0 and to (-1, 0, 0) for c < 0.

            double length = a * a + b * b;
            if (length > 1e-12)
            {
                upDirection = new Vector3D(-c * a / length, -c * b / length, 1);
                upDirection.Normalize();
            }
            else
            {
                if (c > 0)
                    upDirection = new Vector3D(1, 0, 0);
                else
                    upDirection = new Vector3D(-1, 0, 0);
            }

            return result;
        }
    }
}
