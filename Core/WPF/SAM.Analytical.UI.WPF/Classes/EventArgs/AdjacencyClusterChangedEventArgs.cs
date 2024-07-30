using System;

namespace SAM.Analytical.UI.WPF
{
    public class AdjacencyClusterChangedEventArgs : EventArgs
    {
        private AdjacencyCluster adjacencyCluster;

        public AdjacencyClusterChangedEventArgs(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return adjacencyCluster;
            }
        }
    }
}
