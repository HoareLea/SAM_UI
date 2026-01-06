// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditInternalConditions(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            InternalConditionWithSpacesWindow internalConditionWindow = new InternalConditionWithSpacesWindow(uIAnalyticalModel, spaces);
            bool? dialogResult = internalConditionWindow.ShowDialog();
            if(dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }
        }
    }
}
