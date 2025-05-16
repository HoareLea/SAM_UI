using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.UI
{
    public class UIGeometrySettings : IUISettings
    {
        private Dictionary<Guid, IViewSettings> viewSettingsDictionary;
        private Guid activeGuid;
        
        public UIGeometrySettings()
        {
            activeGuid = Guid.Empty;
        }

        public UIGeometrySettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public UIGeometrySettings(UIGeometrySettings uIGeometrySettings)
        {
            activeGuid = Guid.Empty;

            if (uIGeometrySettings != null)
            {
                if(uIGeometrySettings.viewSettingsDictionary != null)
                {
                    viewSettingsDictionary = new Dictionary<Guid, IViewSettings>();
                    foreach(KeyValuePair<Guid, IViewSettings> keyValuePair in uIGeometrySettings.viewSettingsDictionary)
                    {
                        viewSettingsDictionary[keyValuePair.Key] = keyValuePair.Value?.Clone();
                    }
                }

                activeGuid = uIGeometrySettings.activeGuid;
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
                viewSettingsDictionary = new Dictionary<Guid, IViewSettings>();
            }

            viewSettingsDictionary[viewSettings.Guid] = viewSettings_Temp;
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

            if(viewSettingsDictionary?.Keys == null || !viewSettingsDictionary.Keys.Contains(activeGuid))
            {
                activeGuid = Guid.Empty;
            }

            return true;
        }

        public IViewSettings GetViewSettings(Guid guid)
        {
            if(guid == Guid.Empty || viewSettingsDictionary == null)
            {
                return null;
            }

            if(!viewSettingsDictionary.TryGetValue(guid, out IViewSettings result))
            {
                return null;
            }

            return result.Clone();
        }

        public bool RemoveViewSettings(Guid guid)
        {
            if(viewSettingsDictionary == null)
            {
                return false;
            }

            if(!viewSettingsDictionary.ContainsKey(guid))
            {
                return false;
            }

            viewSettingsDictionary.Remove(guid);
            return true;
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
            activeGuid = Guid.Empty;
            return true;
        }

        public HashSet<Guid> GetGuids()
        {
            if(viewSettingsDictionary == null)
            {
                return null;
            }

            return new HashSet<Guid>(viewSettingsDictionary.Keys);
        }

        public Guid ActiveGuid
        {
            get
            {
                return activeGuid;
            }
            set
            {
                if(viewSettingsDictionary == null)
                {
                    return;
                }

                activeGuid = value;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("ViewSettings"))
            {
                viewSettingsDictionary = new Dictionary<Guid, IViewSettings>();

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

                        viewSettingsDictionary[viewSettings.Guid] = viewSettings;
                    }
                }
            }

            if(jObject.ContainsKey("ActiveGuid"))
            {
                activeGuid = Core.Query.Guid(jObject, "ActiveGuid");
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

            if(activeGuid != Guid.Empty)
            {
                jObject.Add("ActiveGuid", activeGuid);
            }

            return jObject;
        }
    }
}
