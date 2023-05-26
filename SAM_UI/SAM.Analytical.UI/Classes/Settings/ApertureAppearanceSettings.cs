using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ApertureAppearanceSettings : TypeAppearanceSettings<Aperture>
    {
        public ApertureAppearanceSettings(JObject jObject) 
            : base(jObject)
        {
        }

        public ApertureAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public ApertureAppearanceSettings(ApertureAppearanceSettings apertureAppearanceSettings)
            :base(apertureAppearanceSettings)
        {

        }

        public ApertureAppearanceSettings(ApertureConstructionAppearanceSettings apertureConstructionAppearanceSettings)
            : base(apertureConstructionAppearanceSettings as ValueAppearanceSettings)
        {

        }

        public ApertureAppearanceSettings(PanelAppearanceSettings panelConstructionAppearanceSettings)
            : base(panelConstructionAppearanceSettings as ValueAppearanceSettings)
        {

        }

        public ApertureAppearanceSettings(OpeningPropertiesAppearanceSettings openingPropertiesAppearanceSettings)
            : base(openingPropertiesAppearanceSettings as ValueAppearanceSettings)
        {

        }
    }
}
