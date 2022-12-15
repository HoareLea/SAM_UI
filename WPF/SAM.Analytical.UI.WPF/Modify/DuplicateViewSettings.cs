using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void DuplicateViewSettings(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
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
            if(viewSettings == null)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(viewSettings.Name) ? viewSettings.DefaultName() : viewSettings.Name;
            name = string.Format("New + {0}", name);


            if(viewSettings is AnalyticalTwoDimensionalViewSettings)
            {
                viewSettings = new AnalyticalTwoDimensionalViewSettings(System.Guid.NewGuid(), name, (AnalyticalTwoDimensionalViewSettings)viewSettings);
            }
            else if(viewSettings is AnalyticalThreeDimensionalViewSettings)
            {
                viewSettings = new AnalyticalThreeDimensionalViewSettings(System.Guid.NewGuid(), name, (AnalyticalThreeDimensionalViewSettings)viewSettings);
            }
            else
            {
                return;
            }


            if(!uIGeometrySettings.AddViewSettings(viewSettings))
            {
                return;
            };

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}