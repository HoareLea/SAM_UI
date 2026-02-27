using SAM.Core;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static List<bool> IsHidden(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, IEnumerable<IJSAMObject> jSAMObjects)
        {
            if(jSAMObjects == null || jSAMObjects.Count() == 0)
            {
                return null;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return null;
            }

            ViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid) as ViewSettings;
            if(viewSettings == null)
            {
                return null;
            }

            int count = jSAMObjects.Count();

            List<bool> result = [.. Enumerable.Repeat(false, count)];

            for (int i = 0; i < count; i++)
            {
                result[i] = IsHidden(viewSettings, jSAMObjects.ElementAt(i));
            }

            return result;
        }

        public static bool IsHidden(this ViewSettings viewSettings, IJSAMObject jSAMObject)
        {
            if (jSAMObject is null || viewSettings is null)
            {
                return false;
            }

            SAMObject sAMObject = jSAMObject as SAMObject;
            if (sAMObject == null)
            {
                return false;
            }

            List<SurfaceAppearance> surfaceAppearances = viewSettings.GetAppearances<SurfaceAppearance>(sAMObject.Guid);
            if (surfaceAppearances == null)
            {
                SurfaceAppearance surfaceAppearance = new SurfaceAppearance(System.Drawing.Color.FromArgb(0, 0, 0), System.Drawing.Color.FromArgb(0, 0, 0), 0);

                surfaceAppearances = new List<SurfaceAppearance>() { surfaceAppearance };
                if (sAMObject is Aperture)
                {
                    surfaceAppearances.Add(surfaceAppearance.Clone());
                }
            }

            return surfaceAppearances.Find(x => x.Visible) != null;
        }
    }
}