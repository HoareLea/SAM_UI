using SAM.Analytical.UI.WPF.Windows;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditViewSettings(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
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

            ViewSettingsWindow viewSettingsWindow = new ViewSettingsWindow(viewSettings, analyticalModel.AdjacencyCluster);
            bool? result = viewSettingsWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            uIGeometrySettings.AddViewSettings(viewSettingsWindow.ViewSettings);
            uIGeometrySettings.ActiveGuid = guid;

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}