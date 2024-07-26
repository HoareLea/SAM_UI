using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public class AdjacencyClusterSelectionChangedEventArgs<T> : EventArgs where T : SAMObject
    {
        private AdjacencyCluster adjacencyCluster;
        private HashSet<Guid> guids;

        public AdjacencyClusterSelectionChangedEventArgs(AdjacencyCluster adjacencyCluster, IEnumerable<Guid> guids)
        {
            this.adjacencyCluster = adjacencyCluster;
            if(guids != null)
            {
                this.guids = new HashSet<Guid>(guids);
            }
        }

        public AdjacencyClusterSelectionChangedEventArgs(AdjacencyCluster adjacencyCluster, IEnumerable<T> sAMObjects)
        {
            this.adjacencyCluster = adjacencyCluster;

            if(sAMObjects != null)
            {
                guids = new HashSet<Guid>();
                foreach(T sAMObject in sAMObjects)
                {
                    guids.Add(sAMObject.Guid);
                }
            }
        }

        public HashSet<Guid> Guids
        {
            get
            {
                return guids == null ? null : new HashSet<Guid>(guids);
            }
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return adjacencyCluster;
            }
        }

        public List<T> GetSAMObjects()
        {
            if(adjacencyCluster == null || guids == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach(Guid guid in guids)
            {
                T sAMObject = adjacencyCluster.GetObject<T>(guid);
                if(sAMObject == null)
                {
                    continue;
                }

                result.Add(sAMObject);
            }

            return result;
        }
    }
}
