using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Core.UI;

namespace SAM.Geometry.UI
{
    public abstract class TypeAppearanceSettings : ValueAppearanceSettings, ITypeAppearanceSettings
    {
        private ValueAppearanceSettings valueAppearanceSettings;
        
        public TypeAppearanceSettings(string parameterName)
            :base()
        {
            valueAppearanceSettings = new ParameterAppearanceSettings(parameterName);
        }

        public TypeAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }

        public TypeAppearanceSettings(TypeAppearanceSettings typeAppearanceSettings)
            : base(typeAppearanceSettings)
        {
            if(typeAppearanceSettings != null)
            {
                valueAppearanceSettings = typeAppearanceSettings.valueAppearanceSettings == null ? null : Core.Query.Clone(typeAppearanceSettings.valueAppearanceSettings);
            }
        }

        public TypeAppearanceSettings(ValueAppearanceSettings valueAppearanceSettings)
            :base()
        {
            this.valueAppearanceSettings = valueAppearanceSettings == null ? null : Core.Query.Clone(valueAppearanceSettings);
        }

        public T GetValueAppearanceSettings<T>() where T: ValueAppearanceSettings
        {
            if(valueAppearanceSettings == null)
            {
                return default;
            }

            IAppearanceSettings result = Core.Query.Clone(valueAppearanceSettings);
            if(!(result is T))
            {
                return default;
            }

            return (T)result;
        }

        public override bool TryGetValue<T>(IJSAMObject jSAMObject, out T value)
        {
            value = default;

            if(valueAppearanceSettings == null)
            {
                return false;
            }

            return valueAppearanceSettings.TryGetValue(jSAMObject, out value);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ValueAppearanceSettings"))
            {
                valueAppearanceSettings = Core.Query.IJSAMObject(jObject.Value<JObject>("ValueAppearanceSettings")) as ValueAppearanceSettings;
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(valueAppearanceSettings != null)
            {
                jObject.Add("ValueAppearanceSettings", valueAppearanceSettings.ToJObject());
            }

            return jObject;
        }
    }
}
