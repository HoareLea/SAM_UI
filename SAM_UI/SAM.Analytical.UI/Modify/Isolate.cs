using SAM.Core;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void Isolate(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid, IEnumerable<IJSAMObject> jSAMObjects)
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

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
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

            Isolate(viewSettings, adjacencyCluster, jSAMObjects);

            uIGeometrySettings.AddViewSettings(viewSettings);

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings, true));
        }

        public static void Isolate(this ViewSettings viewSettings, AdjacencyCluster adjacencyCluster, IEnumerable<IJSAMObject> jSAMObjects)
        {
            if (adjacencyCluster == null || jSAMObjects == null || jSAMObjects.Count() == 0)
            {
                return;
            }

            List<SAMObject> sAMObjects = [];

            if(adjacencyCluster.GetObjects<Panel>() is List<Panel> panels)
            {
                foreach(Panel panel in panels)
                {
                    sAMObjects.Add(panel);

                    List<Aperture> apertures = panel.Apertures;
                    if(apertures != null)
                    {
                        foreach(Aperture aperture in apertures)
                        {
                            sAMObjects.Add(aperture);
                        }
                    }
                }
            }

            adjacencyCluster.GetObjects<Space>()?.ForEach(sAMObjects.Add);

            sAMObjects.RemoveAll(x => x == null);


            HashSet<System.Guid> guids = [];
            foreach(IJSAMObject jSAMObject in jSAMObjects)
            {
                if(jSAMObject is not SAMObject sAMObject)
                {
                    continue;
                }

                System.Guid guid = sAMObject.Guid;

                guids.Add(guid);

                int index = sAMObjects.FindIndex(x => x.Guid == guid);
                if(index == -1)
                {
                    sAMObjects.Add(sAMObject);
                }
            }

            foreach (SAMObject sAMObject in sAMObjects)
            {
                List<SurfaceAppearance> surfaceAppearances = viewSettings.GetAppearances<SurfaceAppearance>(sAMObject.Guid);

                bool visible = guids.Contains(sAMObject.Guid);
                if(visible && (surfaceAppearances == null || surfaceAppearances.Count == 0))
                {
                    continue;
                }

                if (surfaceAppearances == null)
                {
                    SurfaceAppearance surfaceAppearance = new SurfaceAppearance(System.Drawing.Color.FromArgb(0, 0, 0), System.Drawing.Color.FromArgb(0, 0, 0), 0);

                    surfaceAppearances = new List<SurfaceAppearance>() { surfaceAppearance };
                    if (sAMObject is Aperture)
                    {
                        surfaceAppearances.Add(surfaceAppearance.Clone());
                    }
                }

                surfaceAppearances?.ForEach(x => x.Visible = visible);

                viewSettings.RemoveAppearances(sAMObject.Guid);

                viewSettings.SetAppearances(sAMObject.Guid, surfaceAppearances);
            }


        }
    }
}