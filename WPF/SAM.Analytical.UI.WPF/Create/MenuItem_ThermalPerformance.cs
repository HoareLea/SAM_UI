// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        /// <summary>
        /// The 3D view's right-click "Set U-value..." (panels) and "Set glazing..." (apertures). Since Stage F they open the Thermal Performance panel for
        /// the selected elements (<c>documentation/Thermal-StageF-Final-Convergence.md</c>): the panel follows the selection and the row of their construction
        /// starts editing - a target U for panels, <c>Change…</c> for apertures. The construction-level Set U-value / Set glazing windows stay on the Tools ribbon.
        /// The elements are the item's Tag.
        /// </summary>
        public static MenuItem MenuItem_ThermalPerformance(IEnumerable<SAMObject> elements, bool apertures, RoutedEventHandler click)
        {
            List<SAMObject> elements_Selected = elements?.Where(x => x != null && (apertures ? x is Aperture : x is Panel)).GroupBy(x => x.Guid).Select(x => x.First()).ToList() ?? new List<SAMObject>();

            MenuItem menuItem = new MenuItem()
            {
                Name = apertures ? "MenuItem_SetGlazing" : "MenuItem_SetUValue",
                Header = apertures ? "Set glazing..." : "Set U-value...",
                Tag = elements_Selected,
                IsEnabled = elements_Selected.Count > 0,
                ToolTip = apertures
                    ? "Choose a glazing system for the selected apertures in the Thermal Performance panel (filters, compare, scope, check before Apply, one Undo)."
                    : "Set a target U-value for the selected panels in the Thermal Performance panel (preview, alternatives, scope, check before Apply, one Undo).",
            };

            if (click != null)
            {
                menuItem.Click += click;
            }

            return menuItem;
        }
    }
}
