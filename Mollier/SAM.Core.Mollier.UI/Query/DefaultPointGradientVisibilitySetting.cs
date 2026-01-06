// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Counts default colors of Gradient Point
        /// </summary>
        /// <returns>Returns default colors of Gradient Point</returns>
        public static PointGradientVisibilitySetting DefaultPointGradientVisibilitySetting()
        {
            PointGradientVisibilitySetting result = new PointGradientVisibilitySetting(System.Drawing.Color.Red, System.Drawing.Color.Blue);

            return result;
        }
    }
}

