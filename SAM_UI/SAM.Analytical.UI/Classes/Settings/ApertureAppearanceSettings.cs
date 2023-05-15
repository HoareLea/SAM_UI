using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ApertureAppearanceSettings : IAppearanceSettings
    {
        private ParameterAppearanceSettings parameterAppearanceSettings;

        public ApertureAppearanceSettings(string parameterName)
        {
            parameterAppearanceSettings = new ParameterAppearanceSettings(parameterName);
        }

        public ApertureAppearanceSettings(ApertureConstructionAppearanceSettings apertureConstructionAppearanceSettings)
        {
            if(apertureConstructionAppearanceSettings != null)
            {
                parameterAppearanceSettings = new ApertureConstructionAppearanceSettings(apertureConstructionAppearanceSettings);
            }
        }

        public ApertureAppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ApertureAppearanceSettings(ApertureAppearanceSettings apertureAppearanceSettings)
        {
            if (apertureAppearanceSettings?.parameterAppearanceSettings != null)
            {
                parameterAppearanceSettings = Core.Query.Clone(apertureAppearanceSettings.parameterAppearanceSettings);
            }
        }

        public T ParameterAppearanceSettings<T>() where T: ParameterAppearanceSettings
        {
            if(parameterAppearanceSettings == null)
            {
                return null;
            }

            return parameterAppearanceSettings as T;
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ParameterAppearanceSettings"))
            {
                parameterAppearanceSettings = Core.Query.IJSAMObject(jObject.Value<JObject>("ParameterAppearanceSettings")) as ParameterAppearanceSettings;
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(parameterAppearanceSettings != null)
            {
                jObject.Add("ParameterAppearanceSettings", parameterAppearanceSettings.ToJObject());
            }

            return jObject;
        }
    }
}
