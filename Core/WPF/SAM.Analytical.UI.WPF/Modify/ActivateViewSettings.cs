﻿using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void ActivateViewSettings(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
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

            IViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid);

            if (viewSettings == null)
            {
                return;
            }

            uIGeometrySettings.ActiveGuid = guid;

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings));
        }
    }
}