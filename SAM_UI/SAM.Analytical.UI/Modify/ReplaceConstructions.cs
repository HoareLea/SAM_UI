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

            Windows.Modify.UpdateConstructions(adjacencyCluster, constructionLibrary);
        }

        public static void ReplaceConstructions(this UIAnalyticalModel uIAnalyticalModel, ConstructionLibrary constructionLibrary)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<Construction> constructions_Temp = adjacencyCluster.GetObjects<Construction>();
            adjacencyCluster.Remove(constructions_Temp);

            Windows.Modify.UpdateConstructions(adjacencyCluster, constructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster);
        }
    }
}