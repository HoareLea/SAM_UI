using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void UpdateApertureConstructions(this AdjacencyCluster adjacencyCluster, ApertureConstructionLibrary apertureConstructionLibrary)
        {
            List<ApertureConstruction> apertureConstructions = apertureConstructionLibrary?.GetApertureConstructions();
            if (apertureConstructions == null)
            {
                return;
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels != null)
            {
                for (int i = apertureConstructions.Count - 1; i >= 0; i--)
                {
                    bool exists = false;
                    foreach (Panel panel in panels)
                    {
                        List<Aperture> apertures = panel.Apertures;
                        if (apertures == null || apertures.Count == 0)
                        {
                            continue;
                        }

                        foreach (Aperture aperture in apertures)
                        {
                            ApertureConstruction apertureConstruction = aperture?.ApertureConstruction;
                            if (apertureConstruction == null)
                            {
                                continue;
                            }

                            if (apertureConstruction.Guid == apertureConstructions[i].Guid)
                            {
                                panel.RemoveAperture(aperture.Guid);
                                panel.AddAperture(new Aperture(aperture, apertureConstructions[i]));
                                adjacencyCluster.AddObject(panel);
                                exists = true;
                            }
                        }
                    }

                    if (exists)
                    {
                        apertureConstructions.RemoveAt(i);
                    }
                }
            }

            foreach (ApertureConstruction apertureConstruction_Temp in apertureConstructions)
            {
                adjacencyCluster.AddObject(apertureConstruction_Temp);
            }
        }
    }
}