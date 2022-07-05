using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static VisualAnalyticalModel ToMedia3D(this AnalyticalModel analyticalModel)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            VisualAnalyticalModel result = new VisualAnalyticalModel(analyticalModel);

            List<Panel> panels = analyticalModel?.GetPanels();
            if (panels != null)
            {
                foreach (Panel panel in panels)
                {
                    VisualPanel visualPanel = panel?.ToMedia3D();
                    if (visualPanel == null)
                    {
                        continue;
                    }

                    result.Children.Add(visualPanel);

                    List<Aperture> apertures = panel.Apertures;
                    if (apertures != null)
                    {
                        foreach (Aperture aperture in apertures)
                        {
                            VisualAperture visualAperture = aperture?.ToMedia3D();
                            if (visualAperture == null)
                            {
                                continue;
                            }

                            result.Children.Add(visualAperture);
                        }
                    }
                }
            }

            //AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            //List<Space> spaces = analyticalModel.GetSpaces();
            //if (spaces != null)
            //{
            //    foreach(Space space in spaces)
            //    {
            //        VisualSpace visualSpace = space.ToMedia3D(adjacencyCluster);
            //        if (visualSpace == null)
            //        {
            //            continue;
            //        }

            //        Viewport.Children.Add(visualSpace);
            //    }
            //}

            return result;
        }
    }
}
