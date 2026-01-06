// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {

        public static List<DependencyObject> Branch(this DependencyObject dependencyObject)
        {
            if(dependencyObject == null)
            {
                return null;
            }

            DependencyObject parent = dependencyObject.Parent<DependencyObject>();
            List<DependencyObject> result = new List<DependencyObject>();
            if (parent is System.Windows.Media.Media3D.Viewport3DVisual)
            {
                return result;
            }

            if (parent != null)
            {
                result.Add(parent);
                List<DependencyObject> branch = Branch(parent);
                if(branch != null)
                {
                    result.AddRange(branch);
                }
            }

            return result;
        }
    }
}
