// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static System.Windows.Media.Media3D.Vector3D ToMedia3D(this Spatial.Vector3D vector3D)
        {
            if(vector3D == null)
            {
                return new System.Windows.Media.Media3D.Vector3D(double.NaN, double.NaN, double.NaN);
            }

            return new System.Windows.Media.Media3D.Vector3D(vector3D.X, vector3D.Y, vector3D.Z);
        }
    }
}
