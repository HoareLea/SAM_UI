// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {

        public static T Parent<T>(this DependencyObject dependencyObject) where T : DependencyObject
        {
            if(dependencyObject == null)
            {
                return null;
            }

            return VisualTreeHelper.GetParent(dependencyObject) as T;
        }
    }
}
