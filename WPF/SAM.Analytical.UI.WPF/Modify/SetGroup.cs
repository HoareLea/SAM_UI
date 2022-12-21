using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void SetGroup(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, string group)
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

            ViewSettings viewSettings =  uIGeometrySettings.GetViewSettings(guid) as ViewSettings;
            if(viewSettings == null)
            {
                return;
            }

            viewSettings.SetValue(ViewSettingsParameter.Group, group);
            uIGeometrySettings.AddViewSettings(viewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}