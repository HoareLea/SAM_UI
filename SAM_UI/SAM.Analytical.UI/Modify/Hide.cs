using SAM.Core;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void Hide(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, IEnumerable<IJSAMObject> jSAMObjects)
        {
            if(jSAMObjects == null || jSAMObjects.Count() == 0)
            {
                return;
            }

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

            Hide(viewSettings, jSAMObjects);

            uIGeometrySettings.AddViewSettings(viewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings, true));
        }

        public static void Hide(this ViewSettings viewSettings, IEnumerable<IJSAMObject> jSAMObjects)
        {
            if (jSAMObjects == null || jSAMObjects.Count() == 0)
            {
                return;
            }

            foreach (IJSAMObject jSAMObject in jSAMObjects)
            {
                SAMObject sAMObject = jSAMObject as SAMObject;
                if (sAMObject == null)
                {
                    continue;
                }

                List<SurfaceAppearance> surfaceAppearances = viewSettings.GetAppearances<SurfaceAppearance>(sAMObject.Guid);
                if (surfaceAppearances == null)
                {
                    SurfaceAppearance surfaceAppearance = new SurfaceAppearance(System.Windows.Media.Color.FromRgb(0, 0, 0), System.Windows.Media.Color.FromRgb(0, 0, 0), 0);

                    surfaceAppearances = new List<SurfaceAppearance>() { surfaceAppearance };
                    if (sAMObject is Aperture)
                    {
                        surfaceAppearances.Add(surfaceAppearance.Clone());
                    }
                }

                surfaceAppearances.ForEach(x => x.Visible = false);

                viewSettings.RemoveAppearances(sAMObject.Guid);

                viewSettings.SetAppearances(sAMObject.Guid, surfaceAppearances);
            }
        }
    }
}