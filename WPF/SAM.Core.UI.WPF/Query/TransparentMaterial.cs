// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static System.Windows.Media.Media3D.Material TransaprentMaterial()
        {
            return new EmissiveMaterial(Brushes.Transparent);
        }
    }
}
