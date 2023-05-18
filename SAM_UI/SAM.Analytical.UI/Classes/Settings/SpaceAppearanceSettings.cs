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

        public SpaceAppearanceSettings(SpaceAppearanceSettings spaceAppearanceSettings)
            : base(spaceAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(ZoneAppearanceSettings zoneAppearanceSettings)
            :base(zoneAppearanceSettings as ValueAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(InternalConditionAppearanceSettings internalConditionAppearanceSettings)
            :base(internalConditionAppearanceSettings as ValueAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
