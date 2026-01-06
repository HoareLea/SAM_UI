// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static bool Zoom(this UIElement uIElement, Point point, double delta)
        {
            if(uIElement == null || point.IsNaN())
            {
                return false;
            }

            ScaleTransform scaleTransform = Query.ScaleTransform(uIElement);
            if(scaleTransform == null)
            {
                return false;
            }

            TranslateTransform translateTransform = Query.TranslateTransform(uIElement);
            if(translateTransform == null)
            {
                return false;
            }

            double zoom = delta > 0 ? .2 : -.2;
            if (!(delta > 0) && (scaleTransform.ScaleX < .4 || scaleTransform.ScaleY < .4))
            {
                return false;
            }

            double absoluteX = point.X * scaleTransform.ScaleX + translateTransform.X;
            double absoluteY = point.Y * scaleTransform.ScaleY + translateTransform.Y;

            scaleTransform.ScaleX += zoom;
            scaleTransform.ScaleY += zoom;

            translateTransform.X = absoluteX - point.X * scaleTransform.ScaleX;
            translateTransform.Y = absoluteY - point.Y * scaleTransform.ScaleY;

            return true;
        }
    }
}
