using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public abstract class AdjacencyClusterAppearanceSettings : ValueAppearanceSettings
    {
        public AdjacencyCluster AdjacencyCluster { get; set; }

        public AdjacencyClusterAppearanceSettings()
            :base()
        {

        }
        
        public AdjacencyClusterAppearanceSettings(AdjacencyClusterAppearanceSettings adjacencyClusterAppearanceSettings)
            :base(adjacencyClusterAppearanceSettings)
        {
            if(adjacencyClusterAppearanceSettings != null)
            {
                AdjacencyCluster = adjacencyClusterAppearanceSettings.AdjacencyCluster;
            }
        }

        public AdjacencyClusterAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
