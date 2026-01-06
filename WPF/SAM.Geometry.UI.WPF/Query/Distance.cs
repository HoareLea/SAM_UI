// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI.WPF;
using System.Windows;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static double Distance(this Point? point_1, Point? point_2)
        {
            if(point_1 == null || !point_1.HasValue || point_2 == null || !point_2.HasValue || point_1.Value.IsNaN() || point_2.Value.IsNaN())
            {
                return double.NaN;
            }

            return point_1.Value.ToSAM().Distance(point_2.Value.ToSAM());
        }
    }
}
