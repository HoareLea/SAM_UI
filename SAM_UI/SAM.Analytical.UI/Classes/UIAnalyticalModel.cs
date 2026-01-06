// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    public class UIAnalyticalModel : Core.UI.UIJSAMObject<AnalyticalModel>
    {
        public UIAnalyticalModel(string path)
            : base(path)
        {

        }

        public UIAnalyticalModel(AnalyticalModel analyticalModel)
            : base(analyticalModel)
        {

        }

        public UIAnalyticalModel()
            : base()
        {

        }
    }
}
