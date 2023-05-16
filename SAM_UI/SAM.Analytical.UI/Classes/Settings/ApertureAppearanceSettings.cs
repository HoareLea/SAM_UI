using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ApertureAppearanceSettings : TypeAppearanceSettings
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
            :base(apertureAppearanceSettings?.GetAppearanceSettings<Core.UI.IAppearanceSettings>())
        {

        }

        public ApertureAppearanceSettings(ApertureConstructionAppearanceSettings apertureConstructionAppearanceSettings)
            : base(apertureConstructionAppearanceSettings)
        {

        }
    }
}
