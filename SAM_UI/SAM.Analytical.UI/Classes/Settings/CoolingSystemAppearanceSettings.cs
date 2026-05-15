using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class CoolingSystemAppearanceSettings : TypeAppearanceSettings<CoolingSystem>
    {

        public CoolingSystemAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public CoolingSystemAppearanceSettings(CoolingSystemAppearanceSettings coolingSystemAppearanceSettings)
            :base(coolingSystemAppearanceSettings)
        {

        }

        public CoolingSystemAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
