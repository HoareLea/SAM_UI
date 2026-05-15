using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class HeatingSystemAppearanceSettings : TypeAppearanceSettings<HeatingSystem>
    {

        public HeatingSystemAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public HeatingSystemAppearanceSettings(HeatingSystemAppearanceSettings heatingSystemAppearanceSettings)
            :base(heatingSystemAppearanceSettings)
        {

        }

        public HeatingSystemAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
