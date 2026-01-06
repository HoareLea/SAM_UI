// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void SetGroup(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, string group)
        {
            SetGroup(uIAnalyticalModel, new System.Guid[] { guid}, group);
        }

        public static void SetGroup(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<System.Guid> guids, string group)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || guids == null || guids.Count() == 0)
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
                ViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid) as ViewSettings;
                if (viewSettings == null)
                {
                    return;
                }

                viewSettings.SetValue(ViewSettingsParameter.Group, group);
                uIGeometrySettings.AddViewSettings(viewSettings);
                viewSettingsList.Add(viewSettings);
            }

            if(viewSettingsList == null || viewSettingsList.Count == 0)
            {
                return;
            }

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettingsList, true));
        }
    }
}
