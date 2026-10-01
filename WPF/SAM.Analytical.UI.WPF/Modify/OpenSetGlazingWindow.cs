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
        /// The 3D right-click entry: opens "Set glazing" on the aperture construction most of the selected apertures use
        /// (ties: the first selected aperture's), with the selection kept for "Selected apertures only".
        /// </summary>
        public static SetGlazingResult OpenSetGlazingWindow(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Aperture> apertures, System.Windows.Window owner = null)
        {
            List<Aperture> apertures_Selected = apertures?.Where(x => x != null).ToList() ?? new List<Aperture>();

            Guid? apertureConstructionGuid = apertures_Selected
                .Where(x => x.TypeGuid != Guid.Empty)
                .Select((x, i) => new { x.TypeGuid, Index = i })
                .GroupBy(x => x.TypeGuid)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Min(y => y.Index))
                .Select(x => (Guid?)x.Key)
                .FirstOrDefault();

            return OpenSetGlazingWindow(uIAnalyticalModel, apertureConstructionGuid, apertures_Selected.ConvertAll(x => x.Guid), owner);
        }

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
