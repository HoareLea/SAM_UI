using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static T ViewSettings<T>(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid) where T: IViewSettings
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return default;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return default;
            }

            IViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid);
            if (viewSettings == null)
            {
                return default;
            }

            return viewSettings is T ? (T)viewSettings : default;
        }
    }
}
