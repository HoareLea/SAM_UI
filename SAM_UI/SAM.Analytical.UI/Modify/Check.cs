// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void Check(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            Core.Log log = null;

            System.Action action = new System.Action(() => 
            {
                log = analyticalModel.Log();
            });

            Core.Windows.Forms.MarqueeProgressForm.Show("Loading Data", action, owner);
            if(log == null)
            {
                return;
            }

            using (Core.Windows.Forms.LogForm logForm = new Core.Windows.Forms.LogForm(log))
            {
                if(logForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
            }
        }
    }
}
