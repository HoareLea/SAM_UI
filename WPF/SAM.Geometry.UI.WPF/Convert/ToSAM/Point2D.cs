// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static Planar.Point2D ToSAM(this System.Windows.Point point)
        {
            if(point == null)
            {
                return new Planar.Point2D(double.NaN, double.NaN);
            }

            return new Planar.Point2D(point.X, point.Y);
        }
    }
}
