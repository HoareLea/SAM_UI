using Newtonsoft.Json.Linq;

namespace SAM.Geometry.UI
{
    public class ParameterAppearanceSettings : Core.UI.IAppearanceSettings
    {
        public string ParameterName { get; set; }

        public ParameterAppearanceSettings(string parameterName)
        {
            ParameterName = parameterName;
        }

        public ParameterAppearanceSettings(ParameterAppearanceSettings parameterAppearanceSettings)
        {
            if(parameterAppearanceSettings != null)
            {
                ParameterName = parameterAppearanceSettings.ParameterName;
            }
        }

        public ParameterAppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ParameterName"))
            {
                ParameterName = jObject.Value<string>("ParameterName");
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(ParameterName != null)
            {
                jObject.Add("ParameterName", ParameterName);
            }

            return jObject;
        }
    }
}
