using System.Collections.Generic;
using System.Linq;

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

            UpdateApertureConstructions(adjacencyCluster, apertureConstructions);
        }

        public static void UpdateApertureConstructions(this AdjacencyCluster adjacencyCluster, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if(adjacencyCluster == null || apertureConstructions == null || apertureConstructions.Count() == 0)
            {
                return;
            }

            List<ApertureConstruction> apertureConstructions_Temp = new List<ApertureConstruction>(apertureConstructions);

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels != null)
            {
                for (int i = apertureConstructions_Temp.Count - 1; i >= 0; i--)
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

                            if (apertureConstruction.Guid == apertureConstructions_Temp[i].Guid)
                            {
                                panel.RemoveAperture(aperture.Guid);
                                panel.AddAperture(new Aperture(aperture, apertureConstructions_Temp[i]));
                                adjacencyCluster.AddObject(panel);
                                exists = true;
                            }
                        }
                    }

                    if (exists)
                    {
                        apertureConstructions_Temp.RemoveAt(i);
                    }
                }
            }

            foreach (ApertureConstruction apertureConstruction_Temp in apertureConstructions_Temp)
            {
                adjacencyCluster.AddObject(apertureConstruction_Temp);
            }
        }
    }
}