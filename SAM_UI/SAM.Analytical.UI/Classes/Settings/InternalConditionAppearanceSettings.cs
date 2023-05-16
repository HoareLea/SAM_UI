using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class InternalConditionAppearanceSettings : TypeAppearanceSettings
    {

        public InternalConditionAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public InternalConditionAppearanceSettings(InternalConditionAppearanceSettings internalConditionAppearanceSettings)
            :base(internalConditionAppearanceSettings?.GetAppearanceSettings<Core.UI.IAppearanceSettings>())
        {

        }

        public InternalConditionAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }
    }
}
