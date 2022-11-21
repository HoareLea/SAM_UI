using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Core.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.UI
{
    public class UIGeometrySettings : IUISettings
    {
        private Dictionary<int, IViewSettings> viewSettingsDictionary;
        
        public UIGeometrySettings()
        {
        }

        public UIGeometrySettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public UIGeometrySettings(UIGeometrySettings uIGeometrySettings)
        {
            if(uIGeometrySettings != null)
            {
                if(uIGeometrySettings.viewSettingsDictionary != null)
                {
                    viewSettingsDictionary = new Dictionary<int, IViewSettings>();
                    foreach(KeyValuePair<int, IViewSettings> keyValuePair in uIGeometrySettings.viewSettingsDictionary)
                    {
                        viewSettingsDictionary[keyValuePair.Key] = keyValuePair.Value?.Clone();
                    }
                }
            }
        }

        public bool Add(IViewSettings viewSettings)
        {
            if(viewSettings == null)
            {
                return false;
            }

            IViewSettings viewSettings_Temp = viewSettings.Clone();
            if (viewSettings_Temp == null)
            {
                return false;
            }

            if (viewSettingsDictionary == null)
            {
                viewSettingsDictionary = new Dictionary<int, IViewSettings>();
            }

            viewSettingsDictionary[viewSettings.Id] = viewSettings_Temp;
            return true;
        }

        public IViewSettings GetViewSettings(int id)
        {
            if(id == -1)
            {
                return null;
            }

            if(!viewSettingsDictionary.TryGetValue(id, out IViewSettings result))
            {
                return null;
            }

            return result.Clone();
        }

        public HashSet<int> GetIds()
        {
            if(viewSettingsDictionary == null)
            {
                return null;
            }

            return new HashSet<int>(viewSettingsDictionary.Keys);
        }

        public int NewId()
        {
            IEnumerable<int> ids = viewSettingsDictionary?.Keys;
            if(ids == null || ids.Count() == 0)
            {
                return 0;
            }

            int result = 0;
            while(ids.Contains(result))
            {
                result++;
            }

            return result;
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("ViewSettings"))
            {
                viewSettingsDictionary = new Dictionary<int, IViewSettings>();

                JArray jArray = jObject.Value<JArray>();
                if(jArray != null)
                {
                    foreach(JObject jObject_ViewSettings in jArray)
                    {
                        IViewSettings viewSettings = Core.Create.IJSAMObject<IViewSettings>(jObject_ViewSettings);
                        if(viewSettings == null)
                        {
                            continue;
                        }

                        viewSettingsDictionary[viewSettings.Id] = viewSettings;
                    }
                }
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(viewSettingsDictionary != null)
            {
                JArray jArray = new JArray();
                foreach(IViewSettings viewSettings in viewSettingsDictionary.Values)
                {
                    JObject jObject_ViewSettings = viewSettings?.ToJObject();
                    if(jObject_ViewSettings != null)
                    {
                        continue;
                    }

                    jArray.Add(jObject_ViewSettings);
                }

                jObject.Add("ViewSettings", jArray);
            }

            return jObject;
        }
    }
}
