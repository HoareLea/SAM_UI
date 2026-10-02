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
        /// Opens "Set glazing" on <paramref name="apertureConstructionGuid"/>, or with the aperture construction picker
        /// open when it is null (Tools > Glazing Calculator). Returns the applied change, or null when nothing was applied.
        /// </summary>
        public static SetGlazingResult OpenSetGlazingWindow(this UIAnalyticalModel uIAnalyticalModel, Guid? apertureConstructionGuid, IEnumerable<Guid> selectedApertureGuids = null, System.Windows.Window owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject == null)
            {
                return null;
            }

            SetGlazingWindow setGlazingWindow = new SetGlazingWindow(uIAnalyticalModel, apertureConstructionGuid, selectedApertureGuids);
            if (owner != null)
            {
                setGlazingWindow.Owner = owner;
            }
            else
            {
                setGlazingWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            setGlazingWindow.ShowDialog();
            return setGlazingWindow.Result;
        }
    }
}
