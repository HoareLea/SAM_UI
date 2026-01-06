// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {
        public static bool Rotate(this PerspectiveCamera perspectiveCamera, Planar.Vector2D vector2D, Spatial.Point3D center)
        {
            if(perspectiveCamera == null || center == null || center.IsNaN())
            {
                return false;
            }

            Spatial.Point3D position = perspectiveCamera.Position.ToSAM();

            Spatial.Plane plane = perspectiveCamera.Plane();

            Spatial.Point3D position_New = Spatial.Query.Convert(plane, Spatial.Query.Convert(plane, position).GetMoved(vector2D));
            if(position_New == null || !position_New.IsValid())
            {
                return false;
            }

            Spatial.Sphere sphere = new Spatial.Sphere(center, position.Distance(center));

            position_New = Spatial.Query.Project(sphere, position_New);

            perspectiveCamera.Position = position_New.ToMedia3D();
            perspectiveCamera.LookDirection = Convert.ToMedia3D(new Spatial.Vector3D(position_New, center).GetNormalized());

            return true;
        }

        public static bool Rotate(this ModelVisual3D modelVisual3D, Spatial.Vector3D axis, Spatial.Point3D center, double angle)
        {
            if(modelVisual3D == null || axis == null || center == null || !center.IsValid())
            {
                return false;
            }

            Point3D point3D = center.ToMedia3D();

            RotateTransform3D rotateTransform3D = new RotateTransform3D();
            rotateTransform3D.CenterX = point3D.X;
            rotateTransform3D.CenterY = point3D.Z;
            rotateTransform3D.CenterZ = point3D.Y;
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
