// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
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
    }
}
