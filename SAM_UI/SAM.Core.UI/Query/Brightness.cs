// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.UI
{
    public static partial class Query
    {

        /// <summary>
        /// Creates color with corrected brightness
        /// </summary>
        /// <param name="color">Color</param>
        /// <param name="factor">Brightness correction factor between -1 and 1. Negative factor produce darker color</param>
        /// <returns>Color</returns>
        public static System.Windows.Media.Color Brightness(this System.Windows.Media.Color color, double factor)
        {
            System.Drawing.Color color_Temp = Core.Query.Brightness(color.ToDrawing(), factor);

            return color_Temp.ToMedia();
        }
    }
}
