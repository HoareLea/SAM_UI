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
        /// The "Export Space reports..." context-menu item for a Space selection (one or many), shared by the 3D/2D
        /// view and the model tree. The selection is the item's Tag; the batch window defaults its scope to it.
        /// </summary>
        public static MenuItem MenuItem_SpaceReportPdfs(IEnumerable<Space>? spaces, RoutedEventHandler? click)
        {
            List<Space> spaces_Selected = spaces?.Where(x => x != null).GroupBy(x => x.Guid).Select(x => x.First()).ToList() ?? new List<Space>();

            MenuItem menuItem = new MenuItem()
            {
                Name = "MenuItem_SpaceReportPdfs",
                Header = "Export Space reports...",
                Tag = spaces_Selected,
                IsEnabled = spaces_Selected.Count > 0,
                ToolTip = spaces_Selected.Count == 1
                    ? "Export Space report PDFs for this Space into a folder"
                    : string.Format("Export Space report PDFs for the {0} selected Spaces into a folder", spaces_Selected.Count),
            };

            if (click != null)
            {
                menuItem.Click += click;
            }

            return menuItem;
        }
    }
}
