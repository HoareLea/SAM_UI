// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
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
