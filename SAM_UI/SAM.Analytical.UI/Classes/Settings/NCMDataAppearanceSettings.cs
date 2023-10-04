using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class NCMDataAppearanceSettings : TypeAppearanceSettings<NCMData>
    {

        public NCMDataAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public NCMDataAppearanceSettings(NCMDataAppearanceSettings nCMDataAppearanceSettings)
            :base(nCMDataAppearanceSettings)
        {

        }

        public NCMDataAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }
    }
}
