using Newtonsoft.Json.Linq;
using SAM.Core.UI;

namespace SAM.Geometry.UI
{
    public abstract class TypeAppearanceSettings : ITypeAppearanceSettings
    {
        private IAppearanceSettings appearanceSettings;
        
        public TypeAppearanceSettings(string parameterName)
        {
            appearanceSettings = new ParameterAppearanceSettings(parameterName);
        }

        public TypeAppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public TypeAppearanceSettings(IAppearanceSettings appearanceSettings)
        {
            this.appearanceSettings = appearanceSettings == null ? null : Core.Query.Clone(appearanceSettings);
        }

        public T GetAppearanceSettings<T>() where T: IAppearanceSettings
        {
            if(appearanceSettings == null)
            {
                return default;
            }

            IAppearanceSettings result = Core.Query.Clone(appearanceSettings);
            if(!(result is T))
            {
                return default;
            }

            return (T)result;
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("AppearanceSettings"))
            {
                appearanceSettings = Core.Query.IJSAMObject(jObject.Value<JObject>("AppearanceSettings")) as ParameterAppearanceSettings;
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(appearanceSettings != null)
            {
                jObject.Add("AppearanceSettings", appearanceSettings.ToJObject());
            }

            return jObject;
        }
    }
}
