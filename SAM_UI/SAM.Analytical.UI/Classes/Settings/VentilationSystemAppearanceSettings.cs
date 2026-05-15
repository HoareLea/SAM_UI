using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class VentilationSystemAppearanceSettings : TypeAppearanceSettings<VentilationSystem>
    {

        public VentilationSystemAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public VentilationSystemAppearanceSettings(VentilationSystemAppearanceSettings ventilationSystemAppearanceSettings)
            :base(ventilationSystemAppearanceSettings)
        {

        }

        public VentilationSystemAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
