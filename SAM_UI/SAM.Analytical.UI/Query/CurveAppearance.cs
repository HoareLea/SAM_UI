// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {       
        public static CurveAppearance CurveAppearance(this Panel panel)
        {
            if (panel == null)
            {
                return null;
            }

            return new CurveAppearance(System.Drawing.Color.FromArgb(105, 105, 105), 0.04);
        }

        public static CurveAppearance CurveAppearance(this Panel panel, ViewSettings viewSettings)
        {
            CurveAppearance result = viewSettings.GetAppearances<CurveAppearance>(panel)?.FirstOrDefault();
            if (result == null)
            {
                result = CurveAppearance(panel);
            }

            return result;
        }
    }
}
