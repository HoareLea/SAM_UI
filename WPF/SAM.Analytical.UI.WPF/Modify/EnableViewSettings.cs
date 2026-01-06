// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EnableViewSettings(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, bool enabled)
        {
            EnableViewSettings(uIAnalyticalModel, new System.Guid[] { guid }, enabled);
        }

        public static void EnableViewSettings(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<System.Guid> guids, bool enabled)
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

            List<ViewSettings> viewSettingsList = new List<ViewSettings>();
            foreach(System.Guid guid in guids)
            {
                ViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid) as ViewSettings;
                if (viewSettings == null)
                {
                    continue;
                }

                if (viewSettings.Enabled == enabled)
                {
                    continue;
                }

                viewSettingsList.Add(viewSettings);
            }

            if(viewSettingsList == null || viewSettingsList.Count == 0)
            {
                return;
            }

            bool success = false;
            foreach(ViewSettings viewSettings_Temp in viewSettingsList)
            {
                viewSettings_Temp.Enabled = enabled;

                if (uIGeometrySettings.AddViewSettings(viewSettings_Temp))
                {
                    success = true;
                }
            }

            if(!success)
            {
                return;
            }

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettingsList));
        }

        public static void EnableViewSettings(this UIAnalyticalModel uIAnalyticalModel, bool enabled)
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

            System.Func<ViewSettings, string> func = new System.Func<ViewSettings, string>(x => 
            {
                string result = x.Name;

                if (string.IsNullOrWhiteSpace(result))
                {
                    result = Query.DefaultName(x);
                }
                
                if(string.IsNullOrWhiteSpace(result))
                {
                    result = "???";
                }

                return result; 
            });

            List<ViewSettings> viewSettingsList = uIGeometrySettings.GetViewSettings<ViewSettings>();
            if(viewSettingsList == null || viewSettingsList.Count == 0)
            {
                return;
            }

            viewSettingsList.RemoveAll(x => !x.Enabled);
            if (viewSettingsList == null || viewSettingsList.Count == 0)
            {
                return;
            }

            using (Core.Windows.Forms.TreeViewForm<ViewSettings> treeViewForm = new Core.Windows.Forms.TreeViewForm<ViewSettings>("Select Views", viewSettingsList, func))
            {
                if(treeViewForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                viewSettingsList = treeViewForm.SelectedItems;
            }

            if(viewSettingsList == null || viewSettingsList.Count == 0)
            {
                return;
            }

            EnableViewSettings(uIAnalyticalModel, viewSettingsList.ConvertAll(x => x.Guid), enabled);
        }
    }
}
