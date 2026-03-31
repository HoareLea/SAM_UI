// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void Hide(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, IEnumerable<IJSAMObject> jSAMObjects, IEnumerable<bool> hide = null)
        {
            if(jSAMObjects == null || jSAMObjects.Count() == 0)
            {
                return;
            }

            if(hide != null)
            {
                if(jSAMObjects.Count() != hide.Count())
                {
                    return;
                }
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

            Hide(viewSettings, jSAMObjects, hide);

            uIGeometrySettings.AddViewSettings(viewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings, true));
        }

        public static void Hide(this ViewSettings viewSettings, IEnumerable<IJSAMObject> jSAMObjects, IEnumerable<bool> hide = null)
        {
            if (jSAMObjects == null || jSAMObjects.Count() == 0)
            {
                return;
            }

            if (hide != null)
            {
                if (jSAMObjects.Count() != hide.Count())
                {
                    return;
                }
            }

            int count = jSAMObjects.Count();

            for(int i=0; i < count; i++)
            {
                SAMObject sAMObject = jSAMObjects.ElementAt(i) as SAMObject;
                if (sAMObject == null)
                {
                    continue;
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

                bool visible = false;
                if(hide != null)
                {
                    visible = hide.ElementAt(i);
                }

                viewSettings.RemoveAppearances(sAMObject.Guid);

                if (!visible)
                {
                    surfaceAppearances.ForEach(x => x.Visible = visible);
                    viewSettings.SetAppearances(sAMObject.Guid, surfaceAppearances);
                }
            }
        }
    }
}