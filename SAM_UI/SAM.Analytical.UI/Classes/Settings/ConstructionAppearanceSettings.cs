using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ConstructionAppearanceSettings : TypeAppearanceSettings<Construction>
    {

        public ConstructionAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public ConstructionAppearanceSettings(ConstructionAppearanceSettings constructionAppearanceSettings)
            :base(constructionAppearanceSettings)
        {

        }

        public ConstructionAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
