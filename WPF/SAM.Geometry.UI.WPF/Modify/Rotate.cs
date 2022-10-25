using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {
        public static bool Rotate(this ProjectionCamera projectionCamera, Vector3D axis, double angle)
        {
            if(projectionCamera == null || axis == null)
            {
                return false;
            }

            Matrix3D matrix3D = new Matrix3D();
            matrix3D.RotateAt(new Quaternion(axis, angle), projectionCamera.Position);
            projectionCamera.LookDirection *= matrix3D;
            return true;
        }

        public static bool Rotate(this ProjectionCamera projectionCamera, Vector3D axis, double angle, Point3D center)
        {
            if (projectionCamera == null || axis == null)
            {
                return false;
            }

            Matrix3D matrix3D = new Matrix3D();
            matrix3D.RotateAt(new Quaternion(axis, angle), center);
            projectionCamera.LookDirection *= matrix3D;
            return true;
        }

        public static bool Rotate(this ProjectionCamera projectionCamera, Key key, double angle)
        {
            if (projectionCamera == null)
            {
                return false;
            }

            switch (key)
            {
                case Key.Left:
                    return projectionCamera.Rotate(projectionCamera.YawAxis(), +angle);

                case Key.Right:
                    return projectionCamera.Rotate(projectionCamera.YawAxis(), -angle);

                case Key.Down:
                    return projectionCamera.Rotate(projectionCamera.PitchAxis(), +angle);

                case Key.Up:
                    return projectionCamera.Rotate(projectionCamera.PitchAxis(), -angle);
            }

            return false;
        }

        public static bool Rotate(this PerspectiveCamera perspectiveCamera, Key key)
        {
            if(perspectiveCamera == null)
            {
                return false;
            }

            return perspectiveCamera.Rotate(key, perspectiveCamera.FieldOfView / 45d);
        }

        public static bool Rotate(this PerspectiveCamera perspectiveCamera, Planar.Vector2D vector2D, Spatial.Point3D center)
        {
            if(perspectiveCamera == null || center == null || center.IsNaN())
            {
                return false;
            }

            Spatial.Point3D position = perspectiveCamera.Position.ToSAM();

            Spatial.Plane plane = perspectiveCamera.Plane();

            Spatial.Point3D position_New = Geometry.Spatial.Query.Convert(plane, Geometry.Spatial.Query.Convert(plane, position).GetMoved(vector2D));
            if(position_New == null || !position_New.IsValid())
            {
                return false;
            }

            Spatial.Sphere sphere = new Spatial.Sphere(center, position.Distance(center));

            position_New = Geometry.Spatial.Query.Project(sphere, position_New);

            perspectiveCamera.Position = position_New.ToMedia3D();
            perspectiveCamera.LookDirection = SAM.Geometry.UI.WPF.Convert.ToMedia3D(new Spatial.Vector3D(position_New, center).GetNormalized());

            return true;
        }

        public static bool Rotate(this ModelVisual3D modelVisual3D, Spatial.Vector3D axis, Spatial.Point3D center, double angle)
        {
            if(modelVisual3D == null || axis == null || center == null || !center.IsValid())
            {
                return false;
            }

            RotateTransform3D rotateTransform3D = new RotateTransform3D();
            rotateTransform3D.CenterX = center.X;
            rotateTransform3D.CenterY = center.Y;
            rotateTransform3D.CenterZ = center.Z;
            rotateTransform3D.Rotation = new AxisAngleRotation3D(axis.ToMedia3D(), angle);

            modelVisual3D.Transform = HelixToolkit.Wpf.Transform3DHelper.CombineTransform(rotateTransform3D, modelVisual3D.Transform);

            //Transform3DGroup transform3DGroup = modelVisual3D.Transform as Transform3DGroup;
            //if (transform3DGroup == null)
            //{
            //    transform3DGroup = new Transform3DGroup();
            //}

            //transform3DGroup.Children.Add(rotateTransform3D);

            //modelVisual3D.Transform = transform3DGroup;
            return true;
        }
    }
}