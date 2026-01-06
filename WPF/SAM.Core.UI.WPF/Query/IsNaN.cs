// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static bool IsNaN(this Point3D point3D)
        {
            return double.IsNaN(point3D.X) || double.IsNaN(point3D.Y) || double.IsNaN(point3D.Z);
        }

        public static bool IsNaN(this System.Windows.Point point)
        {
            return double.IsNaN(point.X) || double.IsNaN(point.Y);
        }

        public static bool IsNaN(this Vector3D vector3D)
        {
            return double.IsNaN(vector3D.X) || double.IsNaN(vector3D.Y) || double.IsNaN(vector3D.Z);
        }
    }
}
