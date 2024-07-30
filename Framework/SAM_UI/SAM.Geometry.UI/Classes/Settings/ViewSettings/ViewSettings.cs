using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.Object;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.UI
{
    public abstract class ViewSettings : SAMObject, IViewSettings
    {
        private Camera camera;
        private bool enabled = true;
        private GuidAppearanceSettings guidAppearanceSettings;
        private Legend legend;
        private Types types;
        private List<ValueAppearanceSettings> valueAppearanceSettings;

        public ViewSettings(Guid guid, string name)
            : base(guid, name)
        {
        }

        public ViewSettings(Guid guid, string name, GuidAppearanceSettings guidAppearanceSettings, IEnumerable<Type> types, IEnumerable<ValueAppearanceSettings> valueAppearanceSettings)
            : base(guid, name)
        {
            this.guidAppearanceSettings = guidAppearanceSettings != null ? new GuidAppearanceSettings(guidAppearanceSettings) : null;
            this.types = types != null ? new Types(types) : null;
            this.valueAppearanceSettings = valueAppearanceSettings == null ? null : valueAppearanceSettings.ToList().ConvertAll(x => x.Clone());
        }

        public ViewSettings(JObject jObject)
            : base(jObject)
        {
        }

        public ViewSettings(ViewSettings viewSettings)
            : base(viewSettings)
        {
            if (viewSettings != null)
            {
                if (viewSettings.guidAppearanceSettings != null)
                {
                    guidAppearanceSettings = new GuidAppearanceSettings(viewSettings.guidAppearanceSettings);
                }

                if (viewSettings.types != null)
                {
                    types = new Types(viewSettings.types);
                }

                if (viewSettings.legend != null)
                {
                    legend = new Legend(viewSettings.legend);
                }

                if (viewSettings.camera != null)
                {
                    camera = viewSettings.camera.Clone();
                }

                enabled = viewSettings.enabled;

                if (viewSettings.valueAppearanceSettings != null)
                {
                    valueAppearanceSettings = viewSettings.valueAppearanceSettings.ConvertAll(x => x?.Clone());
                }
            }
        }

        public ViewSettings(string name, ViewSettings viewSettings)
            : base(viewSettings)
        {
            this.name = name;

            if (viewSettings != null)
            {
                if (viewSettings.guidAppearanceSettings != null)
                {
                    guidAppearanceSettings = new GuidAppearanceSettings(viewSettings.guidAppearanceSettings);
                }

                if (viewSettings.types != null)
                {
                    types = new Types(viewSettings.types);
                }

                if (viewSettings.legend != null)
                {
                    legend = new Legend(viewSettings.legend);
                }

                if (viewSettings.camera != null)
                {
                    camera = viewSettings.camera.Clone();
                }

                enabled = viewSettings.enabled;

                if (viewSettings.valueAppearanceSettings != null)
                {
                    valueAppearanceSettings = viewSettings.valueAppearanceSettings.ConvertAll(x => x?.Clone());
                }
            }
        }

        public ViewSettings(Guid guid, string name, ViewSettings viewSettings)
            : base(guid, viewSettings)
        {
            this.name = name;

            if (viewSettings != null)
            {
                if (viewSettings.guidAppearanceSettings != null)
                {
                    guidAppearanceSettings = new GuidAppearanceSettings(viewSettings.guidAppearanceSettings);
                }

                if (viewSettings.types != null)
                {
                    types = new Types(viewSettings.types);
                }

                if (viewSettings.legend != null)
                {
                    legend = new Legend(viewSettings.legend);
                }

                if (viewSettings.camera != null)
                {
                    camera = viewSettings.camera.Clone();
                }

                enabled = viewSettings.enabled;

                if (viewSettings.valueAppearanceSettings != null)
                {
                    valueAppearanceSettings = viewSettings.valueAppearanceSettings.ConvertAll(x => x?.Clone());
                }
            }
        }

        public Camera Camera
        {
            get
            {
                return camera == null ? null : camera.Clone();
            }

            set
            {
                if (value != null)
                {
                    camera = value.Clone();
                }
                else
                {
                    camera = null;
                }
            }
        }

        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                enabled = value;
            }
        }

        public Legend Legend
        {
            get
            {
                return legend == null ? null : new Legend(legend);
            }

            set
            {
                if (value != null)
                {
                    legend = new Legend(value);
                }
                else
                {
                    legend = value;
                }
            }
        }

        public bool AddAppearances(Guid guid, IEnumerable<IAppearance> appearances)
        {
            if (guidAppearanceSettings == null)
            {
                guidAppearanceSettings = new GuidAppearanceSettings();
            }

            return guidAppearanceSettings.AddAppearances(guid, appearances);
        }

        public bool AddAppearanceSettings(ValueAppearanceSettings valueAppearanceSettings)
        {
            if (valueAppearanceSettings == null)
            {
                return false;
            }

            if (this.valueAppearanceSettings == null)
            {
                this.valueAppearanceSettings = new List<ValueAppearanceSettings>();
            }

            this.valueAppearanceSettings.RemoveAll(x => x?.GetType() == valueAppearanceSettings.GetType());

            this.valueAppearanceSettings.Add(valueAppearanceSettings);

            return true;
        }

        public bool ContainsAppearances(Guid guid)
        {
            if (guidAppearanceSettings == null)
            {
                return false;
            }

            return guidAppearanceSettings.ContainsAppearances(guid);
        }

        public bool ContainsType(Type type)
        {
            if (types == null)
            {
                return false;
            }

            return types.Contains(type);
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            if (jObject.ContainsKey("GuidAppearanceSettings"))
            {
                guidAppearanceSettings = new GuidAppearanceSettings(jObject.Value<JObject>("GuidAppearanceSettings"));
            }

            if (jObject.ContainsKey("Types"))
            {
                types = new Types(jObject.Value<JObject>("Types"));
            }

            if (jObject.ContainsKey("Legend"))
            {
                legend = new Legend(jObject.Value<JObject>("Legend"));
            }

            if (jObject.ContainsKey("Camera"))
            {
                camera = Core.Query.IJSAMObject(jObject.Value<JObject>("Camera")) as Camera;
            }

            if (jObject.ContainsKey("Enabled"))
            {
                enabled = jObject.Value<bool>("Enabled");
            }

            if(jObject.ContainsKey("ValueAppearanceSettings"))
            {
                JArray jArray = jObject.Value<JArray>("ValueAppearanceSettings");
                if(jArray != null)
                {
                    valueAppearanceSettings = new List<ValueAppearanceSettings>();
                    foreach(JObject jObject_Temp in jArray)
                    {
                        ValueAppearanceSettings valueAppearanceSettings_Temp = Core.Query.IJSAMObject(jObject_Temp) as ValueAppearanceSettings;
                        if(valueAppearanceSettings_Temp != null)
                        {
                            valueAppearanceSettings.Add(valueAppearanceSettings_Temp);
                        }
                    }
                }
            }

            return true;
        }

        public List<T> GetAppearances<T>(Guid guid) where T : IAppearance
        {
            return guidAppearanceSettings?.GetAppearances<T>(guid);
        }

        public List<T> GetAppearances<T>(SAMObject sAMObject) where T : IAppearance
        {
            if (sAMObject == null)
            {
                return null;
            }

            List<T> result = GetAppearances<T>(sAMObject.Guid);

            return result;
        }

        public List<IAppearance> GetAppearances(Guid guid)
        {
            return guidAppearanceSettings?.GetAppearances(guid);
        }

        public List<T> GetValueAppearanceSettings<T>(IJSAMObject jSAMObject) where T : ValueAppearanceSettings
        {
            if (valueAppearanceSettings == null || jSAMObject == null)
            {
                return null;
            }

            return valueAppearanceSettings.FindAll(x => x is T && x.IsValid(jSAMObject))?.Cast<T>()?.ToList();
        }

        public List<T> GetValueAppearanceSettings<T>() where T : ValueAppearanceSettings 
        {
            return valueAppearanceSettings?.OfType<T>()?.ToList();
        }

        public bool IsValid(Type type)
        {
            if (types == null)
            {
                return false;
            }

            return types.Contains(type);
        }

        public bool IsValid(object @object)
        {
            if (@object == null)
            {
                return false;
            }

            if (@object is Type)
            {
                return IsValid((Type)@object);
            }

            return IsValid(@object.GetType());
        }
        
        public bool RemoveAppearances(Guid guid)
        {
            if (guidAppearanceSettings == null)
            {
                return false;
            }

            return guidAppearanceSettings.RemoveAppearances(guid);
        }

        public bool RemoveAppearances()
        {
            if (guidAppearanceSettings == null)
            {
                return false;
            }

            return guidAppearanceSettings.RemoveAppearances();
        }

        public bool RemoveAppearanceSettings(ValueAppearanceSettings valueAppearanceSettings)
        {
            if (valueAppearanceSettings == null || this.valueAppearanceSettings == null || this.valueAppearanceSettings.Count == 0)
            {
                return false;
            }

            return this.valueAppearanceSettings.RemoveAll(x => x?.GetType() == valueAppearanceSettings.GetType()) > 0;
        }

        public bool RemoveAppearanceSettings<T>() where T: ValueAppearanceSettings
        {
            if (valueAppearanceSettings == null)
            {
                return false;
            }

            return valueAppearanceSettings.RemoveAll(x => x is T) > 0;
        }

        public bool SetAppearances(Guid guid, IEnumerable<IAppearance> appearances)
        {
            if (guidAppearanceSettings == null)
            {
                guidAppearanceSettings = new GuidAppearanceSettings();
            }

            return guidAppearanceSettings.SetAppearances(guid, appearances);
        }
        
        public bool SetTypes(IEnumerable<Type> types)
        {
            this.types = new Types(types);
            return true;
        }
        
        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();

            if (guidAppearanceSettings != null)
            {
                jObject.Add("GuidAppearanceSettings", guidAppearanceSettings.ToJObject());
            }

            if (types != null)
            {
                jObject.Add("Types", types.ToJObject());
            }

            if (legend != null)
            {
                jObject.Add("Legend", legend.ToJObject());
            }

            if (camera != null)
            {
                jObject.Add("Camera", camera.ToJObject());
            }

            if (valueAppearanceSettings != null)
            {
                JArray jArray = new JArray();
                foreach(ValueAppearanceSettings valueAppearanceSettings_Temp in valueAppearanceSettings)
                {
                    JObject jObject_Temp = valueAppearanceSettings_Temp?.ToJObject();
                    if(jObject_Temp != null)
                    {
                        jArray.Add(jObject_Temp);
                    }
                }

                jObject.Add("ValueAppearanceSettings", jArray);
            }

            jObject.Add("Enabled", enabled);

            return jObject;
        }
    }
}