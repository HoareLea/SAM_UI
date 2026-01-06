// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static ScaleTransform ScaleTransform(this UIElement uIElement)
        {
            if(uIElement == null)
            {
                return null;
            }

            return (ScaleTransform)((TransformGroup)uIElement.RenderTransform).Children.First(scaleTransform => scaleTransform is ScaleTransform);
        }
    }
}
