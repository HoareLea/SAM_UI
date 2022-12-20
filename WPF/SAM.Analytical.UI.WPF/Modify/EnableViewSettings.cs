using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EnableViewSettings(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, bool enabled)
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

            if(viewSettings.Enabled == enabled)
            {
                return;
            }

            viewSettings.Enabled = enabled;

            if(!uIGeometrySettings.AddViewSettings(viewSettings))
            {
                return;
            }

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}