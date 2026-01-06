// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditLegend(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
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

            ViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid) as ViewSettings;
            if(viewSettings == null)
            {
                return;
            }

            Core.UI.WPF.LegendWindow legendWindow = new Core.UI.WPF.LegendWindow(viewSettings.Legend);
            legendWindow.UndefinedLegendItem = UI.Query.UndefinedLegendItem();
            bool? result = legendWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            viewSettings.Legend = legendWindow.Legend;

            uIGeometrySettings.AddViewSettings(viewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings));
        }
    }
}
