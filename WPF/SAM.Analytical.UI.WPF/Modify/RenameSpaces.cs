// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RenameSpaces(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces)
        {
            if(uIAnalyticalModel == null || spaces == null || spaces.Count() == 0)
            {
                return;
            }

            RenameSpacesWindow renameSpacesWindow = new RenameSpacesWindow(uIAnalyticalModel, spaces);
            bool? result = renameSpacesWindow.ShowDialog();
            if (result != null && result.HasValue && result.Value)
            {

            }
        }
    }
}
