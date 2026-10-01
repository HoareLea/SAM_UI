// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// The 3D right-click entry: opens "Set U-value" on the construction most of the selected panels use
        /// (ties: the first selected panel's), with the selection kept for "Selected panels only".
        /// </summary>
        public static SetUValueResult OpenSetUValueWindow(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Panel> panels, System.Windows.Window owner = null)
        {
            List<Panel> panels_Selected = panels?.Where(x => x != null).ToList() ?? new List<Panel>();

            Guid? constructionGuid = panels_Selected
                .Where(x => x.TypeGuid != Guid.Empty)
                .Select((x, i) => new { x.TypeGuid, Index = i })
                .GroupBy(x => x.TypeGuid)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Min(y => y.Index))
                .Select(x => (Guid?)x.Key)
                .FirstOrDefault();

            return OpenSetUValueWindow(uIAnalyticalModel, constructionGuid, panels_Selected.ConvertAll(x => x.Guid), owner);
        }

        /// <summary>
        /// Opens "Set U-value" on <paramref name="constructionGuid"/>, or with the construction picker open when it is
        /// null (Tools > U Value Calculator). Returns the applied change, or null when nothing was applied.
        /// </summary>
        public static SetUValueResult OpenSetUValueWindow(this UIAnalyticalModel uIAnalyticalModel, Guid? constructionGuid, IEnumerable<Guid> selectedPanelGuids = null, System.Windows.Window owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject == null)
            {
                return null;
            }

            SetUValueWindow setUValueWindow = new SetUValueWindow(uIAnalyticalModel, constructionGuid, selectedPanelGuids);
            if (owner != null)
            {
                setUValueWindow.Owner = owner;
            }
            else
            {
                setUValueWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            setUValueWindow.ShowDialog();
            return setUValueWindow.Result;
        }
    }
}
