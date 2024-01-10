using SAM.Core;
using SAM.Core.UI;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void AssignAdjacencyCluster(this IFilter filter, AdjacencyCluster adjacencyCluster)
        {
            if (filter == null)
            {
                return;
            }

            if (filter is IUIFilter)
            {
                AssignAdjacencyCluster((filter as dynamic).Filter, adjacencyCluster);
            }
            else if (filter is LogicalFilter)
            {
                ((LogicalFilter)filter).Filters?.ForEach(x => AssignAdjacencyCluster(x, adjacencyCluster));
            }
            else if (filter is IAdjacencyClusterFilter)
            {
                ((IAdjacencyClusterFilter)filter).AdjacencyCluster = adjacencyCluster;
                if(filter is IUIFilter)
                {
                    AssignAdjacencyCluster((filter as dynamic).Filter, adjacencyCluster);
                }
            }
            else if (filter is ISAMObjectRelationClusterFilter)
            {
                ((ISAMObjectRelationClusterFilter)filter).SAMObjectRelationCluster = adjacencyCluster;
                if (filter is IUIFilter)
                {
                    AssignAdjacencyCluster((filter as dynamic).Filter, adjacencyCluster);
                }
            }
        }
    }
}