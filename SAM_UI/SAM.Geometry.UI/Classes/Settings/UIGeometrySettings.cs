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
        private int id;
        
        public UIGeometrySettings()
        {
            id = -1;
        }

        public UIGeometrySettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public UIGeometrySettings(UIGeometrySettings uIGeometrySettings)
        {
            id = -1;

            if (uIGeometrySettings != null)
            {
                if(uIGeometrySettings.viewSettingsDictionary != null)
                {
                    viewSettingsDictionary = new Dictionary<int, IViewSettings>();
                    foreach(KeyValuePair<int, IViewSettings> keyValuePair in uIGeometrySettings.viewSettingsDictionary)
                    {
                        viewSettingsDictionary[keyValuePair.Key] = keyValuePair.Value?.Clone();
                    }
                }

                id = uIGeometrySettings.id;
            }
        }

        public bool AddViewSettings(IViewSettings viewSettings)
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

        public bool SetViewSettings(IEnumerable<IViewSettings> viewSettings)
        {
            ClearViewSettings();

            if(viewSettings != null)
            {
                foreach(ViewSettings viewSettings_Temp in viewSettings)
                {
                    AddViewSettings(viewSettings_Temp);
                }
            }

            if(viewSettingsDictionary?.Keys == null || !viewSettingsDictionary.Keys.Contains(id))
            {
                id = -1;
            }

            return true;
        }

        public IViewSettings GetViewSettings(int id)
        {
            if(id == -1 || viewSettingsDictionary == null)
            {
                return null;
            }

            if(!viewSettingsDictionary.TryGetValue(id, out IViewSettings result))
            {
                return null;
            }

            return result.Clone();
        }

        public bool RemoveViewSettins(int id)
        {
            if(viewSettingsDictionary == null)
            {
                return false;
            }

            if(!viewSettingsDictionary.ContainsKey(id))
            {
                return false;
            }

            viewSettingsDictionary.Remove(id);
            SetIds();
            return true;
        }

        private void SetIds()
        {
            if(viewSettingsDictionary == null || viewSettingsDictionary.Count == 0)
            {
                return;
            }

            List<IViewSettings> viewSettings = new List<IViewSettings>(viewSettingsDictionary.Values);
            viewSettingsDictionary.Clear();

            for (int i = 0; i < viewSettings.Count(); i++)
            {
                viewSettingsDictionary[i] = viewSettings.ElementAt(i);
            }
        }

        public List<T> GetViewSettings<T>() where T: IViewSettings
        {
            if(viewSettingsDictionary == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach(IViewSettings viewSettings in viewSettingsDictionary.Values)
            {
                if(viewSettings is T)
                {
                    T t = (T)viewSettings.Clone();

                    if(t != null)
                    {
                        result.Add(t);
                    }
                }
            }

            return result;
        }

        public bool ClearViewSettings()
        {
            if(viewSettingsDictionary == null || viewSettingsDictionary.Count == 0)
            {
                return false;
            }

            viewSettingsDictionary.Clear();
            id = -1;
            return true;
        }

        public HashSet<int> GetIds()
        {
            if(viewSettingsDictionary == null)
            {
                return null;
            }

            return new HashSet<int>(viewSettingsDictionary.Keys);
        }

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                if(viewSettingsDictionary == null)
                {
                    return;
                }

                if(value < -1 || value >= viewSettingsDictionary.Count)
                {
                    return;
                }

                id = value;
            }
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

                JArray jArray = jObject.Value<JArray>("ViewSettings");
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

            if(jObject.ContainsKey("Id"))
            {
                id = jObject.Value<int>("Id");
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
                    if(jObject_ViewSettings == null)
                    {
                        continue;
                    }

                    jArray.Add(jObject_ViewSettings);
                }

                jObject.Add("ViewSettings", jArray);
            }

            if(id != -1)
            {
                jObject.Add("Id", id);
            }

            return jObject;
        }
    }
}
