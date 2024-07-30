using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void UpdateApertureConstructions(this UIAnalyticalModel uIAnalyticalModel, ApertureConstructionLibrary apertureConstructionLibrary)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<ApertureConstruction> apertureConstructions = apertureConstructionLibrary?.GetApertureConstructions();
            if (apertureConstructions == null)
            {
                return;
            }

            Windows.Modify.UpdateApertureConstructions(adjacencyCluster, apertureConstructions);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster);
        }

        public static void UpdateApertureConstructions(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if(apertureConstructions == null || apertureConstructions.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            Windows.Modify.UpdateApertureConstructions(adjacencyCluster, apertureConstructions);


            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster);
        }
    }
}