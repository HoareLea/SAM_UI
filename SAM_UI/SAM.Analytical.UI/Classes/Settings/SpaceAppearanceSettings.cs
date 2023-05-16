using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class SpaceAppearanceSettings : TypeAppearanceSettings
    {
        public SpaceAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public SpaceAppearanceSettings(ZoneAppearanceSettings zoneAppearanceSettings)
            :base(zoneAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(SpaceAppearanceSettings spaceAppearanceSettings)
            :base(spaceAppearanceSettings?.GetAppearanceSettings<Core.UI.IAppearanceSettings>())
        {

        }

        public SpaceAppearanceSettings(InternalConditionAppearanceSettings internalConditionAppearanceSettings)
            :base(internalConditionAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
