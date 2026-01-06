// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static TranslateTransform TranslateTransform(this UIElement uIElement)
        {
            return (TranslateTransform)((TransformGroup)uIElement.RenderTransform).Children.First(translateTransform => translateTransform is TranslateTransform);
        }
    }
}
