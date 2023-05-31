using SAM.Core;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static ViewSettings DefaultViewSettings(AnalyticalModel analyticalModel = null)
        {
            List<System.Type> types = new List<System.Type> { typeof(Panel), typeof(Aperture) };

            if (analyticalModel != null)
            {
                List<Aperture> apertures = analyticalModel.AdjacencyCluster?.GetApertures();
                if(apertures != null && apertures.Count > 100)
                {
                    types.Remove(typeof(Aperture));
                }
            }

            ViewSettings result = new ThreeDimensionalViewSettings(System.Guid.NewGuid(), "3D View", null,  types, null);
            if(analyticalModel != null)
            {
                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                if(adjacencyCluster != null)
                {
                    List<Panel> panels = adjacencyCluster.GetPanels();
                    if(panels != null && panels.Count > 0)
                    {
                        List<IJSAMObject> jSAMObjects = new List<IJSAMObject>();
                        foreach (Panel panel in panels)
                        {
                            if(adjacencyCluster.BoundaryType(panel) == BoundaryType.Linked)
                            {
                                jSAMObjects.Add(panel);
                                if(types.Contains(typeof(Aperture)))
                                {
                                    List<Aperture> apertures = panel.Apertures;
                                    if (apertures != null)
                                    {
                                        foreach (Aperture aperture in apertures)
                                        {
                                            jSAMObjects.Add(aperture);
                                        }
                                    }
                                }
                            }
                        }

                        //if(panels.Count > 1000)
                        //{
                        //    List<Tuple<double, Panel>> tuples = panels.ConvertAll(x => new Tuple<double, Panel>(SAM.Core.Query.Round(x.GetElevationRange().Min, 1), x));
                        //    tuples.Sort((x, y) => x.Item1.CompareTo(y.Item1));

                        //    double value = tuples[tuples.Count / 2].Item1;

                            
                        //}

                        Modify.Hide(result, jSAMObjects);
                    }
                }

            }

            return result;
        }
    }
}