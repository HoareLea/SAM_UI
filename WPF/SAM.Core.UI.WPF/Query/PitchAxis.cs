// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static Vector3D PitchAxis(this ProjectionCamera projectionCamera)
        {
            if(projectionCamera == null)
            {
                return new Vector3D(double.NaN, double.NaN, double.NaN);
            }

            return Vector3D.CrossProduct(projectionCamera.UpDirection, projectionCamera.LookDirection);
        }
    }
}
