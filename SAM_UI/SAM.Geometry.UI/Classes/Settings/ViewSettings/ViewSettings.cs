using Newtonsoft.Json.Linq;

namespace SAM.Geometry.UI
{
    public abstract class ViewSettings : IViewSettings
    {
        private int id = -1;
        private AppearanceSettings appearanceSettings;

        public ViewSettings(int id)
        {
            this.id = id;
        }

        public ViewSettings(int id, AppearanceSettings appearanceSettings)
        {
            this.id = id;
            this.appearanceSettings = appearanceSettings != null ? new AppearanceSettings(appearanceSettings) : null;
        }

        public ViewSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ViewSettings(ViewSettings viewSettings)
        {
            if(viewSettings != null)
            {
                id = viewSettings.id;
                
                if(viewSettings.appearanceSettings != null)
                {
                    appearanceSettings = new AppearanceSettings(viewSettings.appearanceSettings);
                }
            }
        }

        public int Id
        {
            get
            {
                return id;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Id"))
            {
                id = jObject.Value<int>("Id");
            }

            if (jObject.ContainsKey("AppearanceSettings"))
            {
                appearanceSettings = new AppearanceSettings(jObject.Value<JObject>("AppearanceSettings"));
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            jObject.Add("Id", id);

            if(appearanceSettings != null)
            {
                jObject.Add("AppearanceSettings", appearanceSettings.ToJObject());
            }

            return jObject;
        }
    }
}
