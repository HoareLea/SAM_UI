// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static List<IFilterControl> FilterControls(this Grid grid)
        {
            if(grid?.Children == null)
            {
                return null;
            }

            List<IFilterControl> result = new List<IFilterControl>();
            foreach (UIElement uIElement in grid.Children)
            {
                IFilterControl filterControl = uIElement as IFilterControl;
                if (filterControl == null)
                {
                    continue;
                }

                IUIFilter uIFilter = filterControl.UIFilter;
                if (uIFilter == null)
                {
                    continue;
                }

                result.Add(filterControl);
            }

            return result;
        }
    }
}
