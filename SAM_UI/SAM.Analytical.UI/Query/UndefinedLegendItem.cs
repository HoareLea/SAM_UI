// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System.Drawing;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static LegendItem UndefinedLegendItem(string name = "Undefined", Color? color = null)
        {

            string name_Temp = name;
            if(string.IsNullOrWhiteSpace(name_Temp))
            {
                name_Temp = "Undefined";
            }


            Color color_Temp = System.Drawing.Color.LightGray;
            if(color != null && color.HasValue)
            {
                color_Temp = color.Value;
            }

            return new LegendItem(color_Temp, name_Temp);
        }
    }
}
