// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static T DependencyObject<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            T t = dependencyObject as T;
            while (t == null && dependencyObject != null)
            {
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
                t = dependencyObject as T;
            }
            return t;
        }

        public static T DependencyObject<T>(UIElement uIElement, Point point) where T : DependencyObject
        {
            if (uIElement == null)
            {
                return null;
            }

            IInputElement inputElement = uIElement.InputHitTest(point);
            if (inputElement == null)
            {
                return null;
            }

            return DependencyObject<T>(inputElement as DependencyObject);
        }
    }
}
