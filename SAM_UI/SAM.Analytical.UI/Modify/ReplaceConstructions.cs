using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void ReplaceConstructions(this AdjacencyCluster adjacencyCluster, ConstructionLibrary constructionLibrary)
        {
            if(adjacencyCluster == null)
            {
                return;
            }

            List<Construction> constructions_Temp = adjacencyCluster.GetObjects<Construction>();
            adjacencyCluster.Remove(constructions_Temp);

            UpdateConstructions(adjacencyCluster, constructionLibrary);
        }
    }
}