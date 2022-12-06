using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class SpaceAppearanceSettings : IJSAMObject
    {
        private ParameterAppearanceSettings parameterAppearanceSettings;

        public SpaceAppearanceSettings(string parameterName)
        {
            parameterAppearanceSettings = new ParameterAppearanceSettings(parameterName);
        }

        public SpaceAppearanceSettings(ZoneAppearanceSettings zoneAppearanceSettings)
        {
            if(zoneAppearanceSettings != null)
            {
                parameterAppearanceSettings = new ZoneAppearanceSettings(zoneAppearanceSettings);
            }
        }

        public SpaceAppearanceSettings(SpaceAppearanceSettings spaceAppearanceSettings)
        {
            if (spaceAppearanceSettings?.parameterAppearanceSettings != null)
            {
                parameterAppearanceSettings = Core.Query.Clone(spaceAppearanceSettings.parameterAppearanceSettings);
            }
        }

        public SpaceAppearanceSettings(InternalConditionAppearanceSettings internalConditionAppearanceSettings)
        {
            if (internalConditionAppearanceSettings != null)
            {
                parameterAppearanceSettings = new InternalConditionAppearanceSettings(internalConditionAppearanceSettings);
            }
        }

        public SpaceAppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
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
