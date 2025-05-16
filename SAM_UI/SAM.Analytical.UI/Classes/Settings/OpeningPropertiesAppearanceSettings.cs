using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class OpeningPropertiesAppearanceSettings : TypeAppearanceSettings<IOpeningProperties>
    {
        public OpeningPropertiesAppearanceSettings(JObject jObject) 
            : base(jObject)
        {
        }

        public OpeningPropertiesAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public OpeningPropertiesAppearanceSettings(OpeningPropertiesAppearanceSettings openingPropertiesAppearanceSettings)
            :base(openingPropertiesAppearanceSettings)
        {

        }
    }
}
