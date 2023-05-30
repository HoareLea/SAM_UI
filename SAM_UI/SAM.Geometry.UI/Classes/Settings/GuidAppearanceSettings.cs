using Newtonsoft.Json.Linq;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.UI
{
    public class GuidAppearanceSettings : Core.UI.IAppearanceSettings
    {
        private Dictionary<Guid, List<IAppearance>> appearanceDictionary;

        public GuidAppearanceSettings()
        {

        }

        public GuidAppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public GuidAppearanceSettings(GuidAppearanceSettings appearanceSettings)
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

        public bool ContainsAppearances(Guid guid)
        {
            if(appearanceDictionary == null)
            {
                return false;
            }

            return appearanceDictionary.ContainsKey(guid);
        }

        public bool AddAppearances(Guid guid, IEnumerable<IAppearance> appearances)
        {
            if(appearances == null || appearances.Count() == 0)
            {
                return false;
            }

            if(appearanceDictionary == null)
            {
                appearanceDictionary = new Dictionary<Guid, List<IAppearance>>();
            }

            if(!appearanceDictionary.TryGetValue(guid, out List<IAppearance> appearances_Temp) || appearances_Temp == null)
            {
                appearances_Temp = new List<IAppearance>();
                appearanceDictionary[guid] = appearances_Temp;
            }


            foreach(IAppearance appearance in appearances)
            {
                IAppearance appearance_Temp = Core.Query.Clone(appearance);
                if(appearance_Temp == null)
                {
                    continue;
                }

                appearances_Temp.Add(appearance_Temp);
            }

            return true;
        }

        public bool SetAppearances(Guid guid, IEnumerable<IAppearance> appearances)
        {
            RemoveAppearances(guid);

            return AddAppearances(guid, appearances);
        }

        public bool RemoveAppearances(Guid guid)
        {
            if(appearanceDictionary == null || appearanceDictionary.Count == 0)
            {
                return false;
            }

            return appearanceDictionary.Remove(guid);
        }

        public bool RemoveAppearances()
        {
            if (appearanceDictionary == null || appearanceDictionary.Count == 0)
            {
                return false;
            }

            appearanceDictionary.Clear();
            return true;
        }

        public List<T> GetAppearances<T>(Guid guid) where T: IAppearance
        {
            if(appearanceDictionary == null || !appearanceDictionary.ContainsKey(guid))
            {
                return null;
            }

            if (!appearanceDictionary.TryGetValue(guid, out List<IAppearance> appearances) || appearances == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            for(int i =0; i < appearances.Count; i++)
            {
                IAppearance appearance = appearances[i];
                if(!(appearance is T))
                {
                    continue;
                }

                appearance = Core.Query.Clone(appearance);
                if(!(appearance is T))
                {
                    continue;
                }

                result.Add((T)(object)appearance);
            }

            return result;
        }

        public List<T> GetAppearances<T>(SAMObject sAMObject) where T : IAppearance
        {
            return GetAppearances<T>(sAMObject);
        }

        public List<IAppearance> GetAppearances(Guid guid)
        {
            return GetAppearances<IAppearance>(guid);
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
