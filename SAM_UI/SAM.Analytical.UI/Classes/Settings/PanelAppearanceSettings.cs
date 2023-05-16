using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class PanelAppearanceSettings : TypeAppearanceSettings
    {
        public PanelAppearanceSettings(string parameterName)
            :base(parameterName)
        {
        }

        public PanelAppearanceSettings(ConstructionAppearanceSettings constructionAppearanceSettings)
            :base(constructionAppearanceSettings)
        {

        }

        public PanelAppearanceSettings(PanelAppearanceSettings panelAppearanceSettings)
            : base(panelAppearanceSettings?.GetAppearanceSettings<Core.UI.IAppearanceSettings>())
        {

        }

        public PanelAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
