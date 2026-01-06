// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static Viewport3DVisual Viewport3DVisual(this Visual3D visual3D)
        {
            DependencyObject dependencyObject = visual3D;
            while (dependencyObject != null)
            {
                if (!(dependencyObject is ModelVisual3D))
                {
                    break;
                }
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            return dependencyObject as Viewport3DVisual;
        }

        public static Viewport3DVisual Viewport3DVisual(this Viewport3D viewport3D)
        {
            Visual3DCollection visual3DCollection = viewport3D?.Children;
            if(visual3DCollection == null || visual3DCollection.Count == 0)
            {
                return null;
            }

            return Viewport3DVisual(visual3DCollection[0]);
        }
    }
}
