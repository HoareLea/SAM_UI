// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static T ParentDependencyObject<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject == null)
            {
                return null;
            }

            DependencyObject dependencyObject_Parent = VisualTreeHelper.GetParent(dependencyObject);
            if (dependencyObject_Parent == null)
            {
                return null;
            }

            return DependencyObject<T>(dependencyObject_Parent);
        }
    }
}
