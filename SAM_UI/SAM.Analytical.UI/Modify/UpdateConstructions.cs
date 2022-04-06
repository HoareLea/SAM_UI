using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void UpdateConstructions(this AdjacencyCluster adjacencyCluster, ConstructionLibrary constructionLibrary)
        {
            List<Construction> constructions = constructionLibrary?.GetConstructions();
            if (constructions == null)
            {
                return;
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels != null)
            {
                for (int i = constructions.Count - 1; i >= 0; i--)
                {
                    bool exists = false;
                    foreach (Panel panel in panels)
                    {
                        Construction construction = panel?.Construction;
                        if (construction != null)
                        {
                            if (construction.Guid == constructions[i].Guid)
                            {
                                adjacencyCluster.AddObject(Create.Panel(panel, constructions[i]));
                                exists = true;
                            }
                        }
                    }

                    if (exists)
                    {
                        constructions.RemoveAt(i);
                    }
                }
            }

            foreach (Construction construction_Temp in constructions)
            {
                adjacencyCluster.AddObject(construction_Temp);
            }
        }
    }
}