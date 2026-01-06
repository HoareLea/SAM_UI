// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditProperties(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            using (Windows.Forms.AnalyticalModelForm analyticalModelForm = new Windows.Forms.AnalyticalModelForm(analyticalModel, Core.Query.Enums(typeof(AnalyticalModel))))
            {
                if(analyticalModelForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                analyticalModel = analyticalModelForm.AnalyticalModel;
            }

            if(analyticalModel == null)
            {
                return;
            }

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}
