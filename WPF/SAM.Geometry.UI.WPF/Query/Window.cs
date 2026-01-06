// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static Window Window(this FrameworkElement frameworkElement)
        {
            if(frameworkElement == null)
            {
                return null;
            }

            if(frameworkElement.Parent == null)
            {
                return null;
            }

            if(frameworkElement.Parent is Window)
            {
                return (Window)frameworkElement.Parent;
            }

            return Window(frameworkElement.Parent as FrameworkElement);
        }
    }
}
