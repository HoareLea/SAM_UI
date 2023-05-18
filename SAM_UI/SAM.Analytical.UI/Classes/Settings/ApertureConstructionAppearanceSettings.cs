using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ApertureConstructionAppearanceSettings : TypeAppearanceSettings
    {
        public ApertureConstructionAppearanceSettings(string parameterName)
            :base(parameterName)
        {
        }

        public ApertureConstructionAppearanceSettings(ApertureConstructionAppearanceSettings apertureConstructionAppearanceSettings)
            :base(apertureConstructionAppearanceSettings)
        {

        }

        public ApertureConstructionAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }
    }
}
