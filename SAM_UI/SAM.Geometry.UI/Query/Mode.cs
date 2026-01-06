// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Geometry.UI
{
    public static partial class Query
    {
        public static Mode Mode(this IViewSettings viewSettings)
        {
            if(viewSettings == null)
            {
                return UI.Mode.Undefined;
            }

            if(viewSettings is ThreeDimensionalViewSettings)
            {
                return UI.Mode.ThreeDimensional;
            }

            if(viewSettings is TwoDimensionalViewSettings)
            {
                return UI.Mode.TwoDimensional;
            }

            return UI.Mode.Undefined;
        }

    }
}
