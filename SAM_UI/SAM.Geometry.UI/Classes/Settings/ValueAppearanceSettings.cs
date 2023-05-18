using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Geometry.UI
{
    public abstract class ValueAppearanceSettings : Core.UI.IAppearanceSettings
    {
        public ValueAppearanceSettings(ValueAppearanceSettings valueAppearanceSettings)
        {
            if(valueAppearanceSettings != null)
            {

            }
        }

        public ValueAppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool IsValid(IJSAMObject jSAMObject)
        {
            if(jSAMObject == null)
            {
                return false;
            }

            return TryGetValue(jSAMObject, out object value);
        }

        public abstract bool TryGetValue<T>(IJSAMObject sAMObject, out T value);

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            //if (jObject.ContainsKey("ParameterName"))
            //{
            //    ParameterName = jObject.Value<string>("ParameterName");
            //}

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            //if(ParameterName != null)
            //{
            //    jObject.Add("ParameterName", ParameterName);
            //}

            return jObject;
        }
    }
}
