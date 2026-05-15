using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Core.UI
{
    public abstract class RelationClusterAppearanceSettings : ValueAppearanceSettings
    {
        public RelationCluster RelationCluster { get; set; }

        public RelationClusterAppearanceSettings()
            :base()
        {

        }
        
        public RelationClusterAppearanceSettings(RelationClusterAppearanceSettings relationClusterAppearanceSettings)
            :base(relationClusterAppearanceSettings)
        {
            if(relationClusterAppearanceSettings != null)
            {
                RelationCluster = relationClusterAppearanceSettings.RelationCluster;
            }
        }

        public RelationClusterAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {

        }
    }
}
