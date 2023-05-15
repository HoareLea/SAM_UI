using Newtonsoft.Json.Linq;

namespace SAM.Analytical.UI
{
    public class ApertureConstructionAppearanceSettings : Geometry.UI.ParameterAppearanceSettings
    {

        public ApertureConstructionAppearanceSettings(string parameterName)
            :base(parameterName)
        {
            ParameterName = parameterName;
        }

        public ApertureConstructionAppearanceSettings(ApertureConstructionAppearanceSettings apertureConstructionAppearanceSettings)
            :base(apertureConstructionAppearanceSettings)
        {

        }

        public ApertureConstructionAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject result = base.ToJObject();

            return result;
        }
    }
}
