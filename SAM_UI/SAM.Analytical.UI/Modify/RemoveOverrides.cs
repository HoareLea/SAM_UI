using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void RemoveOverrides(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
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

            viewSettings.RemoveAppearances();

            uIGeometrySettings.AddViewSettings(viewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings, true));
        }
    }
}