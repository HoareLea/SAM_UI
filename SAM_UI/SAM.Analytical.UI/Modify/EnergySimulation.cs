// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        [Obsolete("Use  SAM.Analytical.UI.WPF.Simulate Instead")]
        public static void EnergySimulation(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel_Temp = Query.Simulate(analyticalModel, uIAnalyticalModel.Path, owner);
            if (analyticalModel_Temp == null)
            {
                return;
            }

            uIAnalyticalModel.JSAMObject = analyticalModel_Temp;
        }
    }
}
