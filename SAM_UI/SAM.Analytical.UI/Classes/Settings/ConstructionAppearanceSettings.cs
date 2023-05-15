using Newtonsoft.Json.Linq;

namespace SAM.Analytical.UI
{
    public class ConstructionAppearanceSettings : Geometry.UI.ParameterAppearanceSettings
    {

        public ConstructionAppearanceSettings(string parameterName)
            :base(parameterName)
        {
            ParameterName = parameterName;
        }

        public ConstructionAppearanceSettings(ConstructionAppearanceSettings constructionAppearanceSettings)
            :base(constructionAppearanceSettings)
        {

        }

        public ConstructionAppearanceSettings(JObject jObject)
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
