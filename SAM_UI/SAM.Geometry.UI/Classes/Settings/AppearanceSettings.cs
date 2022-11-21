using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class AppearanceSettings : IAppearanceSettings
    {
        private Dictionary<Guid, List<IAppearance>> appearanceDictionary;

        public AppearanceSettings()
        {

        }

        public AppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public AppearanceSettings(AppearanceSettings appearanceSettings)
        {
            if(appearanceSettings != null)
            {
                if(appearanceSettings.appearanceDictionary != null)
                {
                    appearanceDictionary = new Dictionary<Guid, List<IAppearance>>();
                    foreach(KeyValuePair<Guid, List<IAppearance>> keyValuePair in appearanceSettings.appearanceDictionary)
                    {
                        appearanceDictionary[keyValuePair.Key] = keyValuePair.Value?.ConvertAll(x => Core.Query.Clone(x));
                    }
                }
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("AppearanceDictionary"))
            {
                appearanceDictionary = new Dictionary<Guid, List<IAppearance>>();

                JArray jArray = jObject.Value<JArray>("AppearanceDictionary");
                if(jArray != null)
                {
                    foreach(JObject jObject_Temp in jArray)
                    {
                        if(!jObject_Temp.ContainsKey("Guid"))
                        {
                            continue;
                        }

                        Guid guid = Guid.Parse(jObject_Temp.Value<string>("Guid"));

                        List<IAppearance> appearances = null;
                        if (!jObject_Temp.ContainsKey("Appearances"))
                        {
                            appearances = new List<IAppearance>();
                            JArray jArray_Appearance = jObject.Value<JArray>("Appearances");
                            if(jArray_Appearance != null)
                            {
                                foreach(JObject jObject_Appearance in jArray_Appearance)
                                {
                                    IAppearance appearance = Core.Create.IJSAMObject<IAppearance>(jObject_Appearance);
                                    if(appearance != null)
                                    {
                                        appearances.Add(appearance);
                                    }
                                }
                            }
                        }

                        appearanceDictionary[guid] = appearances;
                    }
                }
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(appearanceDictionary != null)
            {
                JArray jArray = new JArray();
                foreach(KeyValuePair<Guid, List<IAppearance>> keyValuePair in appearanceDictionary)
                {
                    JObject jObject_Temp = new JObject();
                    jObject_Temp.Add("Guid", keyValuePair.Key);

                    JArray jArray_Appearance = new JArray();
                    foreach(IAppearance appearance in keyValuePair.Value)
                    {
                        JObject jObject_Appearance = appearance?.ToJObject();
                        if(jObject_Appearance == null)
                        {
                            continue;
                        }

                        jArray_Appearance.Add(jObject_Appearance);
                    }

                    jObject_Temp.Add("Appearances", jArray_Appearance);

                    jArray.Add(jObject_Temp);
                }

                jObject.Add("AppearanceDictionary", jArray);
            }

            return jObject;
        }
    }
}
