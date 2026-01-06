// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static List<T> ViewSettings<T>(this AnalyticalModel analyticalModel) where T: IViewSettings
        {
            if (analyticalModel == null)
            {
                return null;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return null;
            }

            return uIGeometrySettings.GetViewSettings<T>();
        }
    }
}
