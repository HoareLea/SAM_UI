using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class PanelAppearanceSettings : TypeAppearanceSettings<Panel>
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
            : base(panelAppearanceSettings)
        {

        }

        public PanelAppearanceSettings(BoundaryTypeAppearanceSettings boundaryTypeAppearanceSettings)
            : base(boundaryTypeAppearanceSettings)
        {

        }

        public PanelAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
