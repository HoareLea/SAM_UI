// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RemoveViewSettings(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
        {
            RemoveViewSettings(uIAnalyticalModel, new System.Guid[] { guid });
        }

        public static void RemoveViewSettings(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<System.Guid> guids)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return;
            }

            List<IViewSettings> viewSettingsList = new List<IViewSettings>();
            foreach(System.Guid guid in guids)
            {
                IViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid);
                if (viewSettings == null)
                {
                    continue;
                }

                if (!uIGeometrySettings.RemoveViewSettings(guid))
                {
                    continue;
                };

                viewSettingsList.Add(viewSettings);
            }

            if(viewSettingsList == null || viewSettingsList.Count == 0)
            {
                return;
            }

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettingsList));
        }
    }
}
