using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class InternalConditionAppearanceSettings : TypeAppearanceSettings<InternalCondition>
    {

        public InternalConditionAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public InternalConditionAppearanceSettings(InternalConditionAppearanceSettings internalConditionAppearanceSettings)
            :base(internalConditionAppearanceSettings)
        {

        }

        public InternalConditionAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
