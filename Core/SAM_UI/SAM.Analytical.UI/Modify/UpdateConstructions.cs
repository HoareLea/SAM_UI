﻿using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void UpdateConstructions(this UIAnalyticalModel uIAnalyticalModel, ConstructionLibrary constructionLibrary)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            Windows.Modify.UpdateConstructions(adjacencyCluster, constructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster);
        }

        public static void UpdateConstructions(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Construction> constructions)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            Windows.Modify.UpdateConstructions(adjacencyCluster, constructions);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster);
        }
    }
}