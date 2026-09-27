// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        /// <summary>
        /// The context-menu item of a one-Space report PDF (e.g. "Space Assumptions PDF") for a Space selection,
        /// shared by the 3D/2D view and the model tree. With several Spaces selected the item is shown disabled, with
        /// a tooltip saying why, rather than silently reporting the first. The selection is the item's Tag.
        /// </summary>
        public static MenuItem MenuItem_SpaceReportPdf(IEnumerable<Space>? spaces, SpaceReportPdf spaceReportPdf, RoutedEventHandler? click)
        {
            List<Space> spaces_Selected = spaces?.Where(x => x != null).GroupBy(x => x.Guid).Select(x => x.First()).ToList() ?? new List<Space>();

            MenuItem menuItem = new MenuItem()
            {
                Name = "MenuItem_" + spaceReportPdf.Id,
                Header = spaceReportPdf.Title,
                Tag = spaces_Selected,
                IsEnabled = spaces_Selected.Count == 1,
                ToolTip = spaces_Selected.Count == 1
                    ? string.Format("Create the {0} for this Space", spaceReportPdf.Title)
                    : string.Format("The {0} is created for one Space at a time: select a single Space.", spaceReportPdf.Title),
            };

            ToolTipService.SetShowOnDisabled(menuItem, true);

            if (click != null)
            {
                menuItem.Click += click;
            }

            return menuItem;
        }
    }
}
