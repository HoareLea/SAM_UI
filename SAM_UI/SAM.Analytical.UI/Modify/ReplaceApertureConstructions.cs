using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void ReplaceApertureConstructions(this AdjacencyCluster adjacencyCluster, ApertureConstructionLibrary apertureConstructionLibrary)
        {
            if(adjacencyCluster == null)
            {
                return;
            }

            List<ApertureConstruction> apertureConstructions_Temp = adjacencyCluster.GetObjects<ApertureConstruction>();
            adjacencyCluster.Remove(apertureConstructions_Temp);

            UpdateApertureConstructions(adjacencyCluster, apertureConstructionLibrary);
        }
    }
}