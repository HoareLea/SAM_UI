using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class PanelAppearanceSettings : IAppearanceSettings
    {
        private ParameterAppearanceSettings parameterAppearanceSettings;

        public PanelAppearanceSettings(string parameterName)
        {
            parameterAppearanceSettings = new ParameterAppearanceSettings(parameterName);
        }

        public PanelAppearanceSettings(ConstructionAppearanceSettings constructionAppearanceSettings)
        {
            if(constructionAppearanceSettings != null)
            {
                parameterAppearanceSettings = new ConstructionAppearanceSettings(constructionAppearanceSettings);
            }
        }

        public PanelAppearanceSettings(PanelAppearanceSettings panelAppearanceSettings)
        {
            if (panelAppearanceSettings?.parameterAppearanceSettings != null)
            {
                parameterAppearanceSettings = Core.Query.Clone(panelAppearanceSettings.parameterAppearanceSettings);
            }
        }

        public PanelAppearanceSettings(JObject jObject)
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
