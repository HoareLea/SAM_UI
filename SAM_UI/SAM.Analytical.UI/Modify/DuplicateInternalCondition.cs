// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void DuplicateInternalCondition(this UIAnalyticalModel uIAnalyticalModel, InternalCondition internalCondition, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            internalCondition = Windows.Modify.Duplicate(analyticalModel, internalCondition, owner);
            if (internalCondition == null)
            {
                return;
            }

            if(!analyticalModel.AddInternalCondition(internalCondition))
            {
                return;
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel);
        }
    }
}
