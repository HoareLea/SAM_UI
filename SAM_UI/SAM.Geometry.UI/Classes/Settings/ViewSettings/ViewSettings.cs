using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public abstract class ViewSettings : SAMObject, IViewSettings
    {
        private AppearanceSettings appearanceSettings;
        private Types types;
        private Legend legend;
        private Camera camera;
        private bool enabled = true;

        public ViewSettings(Guid guid, string name)
            :base(guid, name)
        {

        }

        public ViewSettings(Guid guid, string name, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(guid, name)
        {
            this.appearanceSettings = appearanceSettings != null ? new AppearanceSettings(appearanceSettings) : null;
            this.types = types != null ? new Types(types) : null;
        }

        public ViewSettings(JObject jObject)
            :base(jObject)
        {

        }

        public ViewSettings(ViewSettings viewSettings)
            :base(viewSettings)
        {
            if(viewSettings != null)
            {
                if(viewSettings.appearanceSettings != null)
                {
                    appearanceSettings = new AppearanceSettings(viewSettings.appearanceSettings);
                }

                if(viewSettings.types != null)
                {
                    types = new Types(viewSettings.types);
                }

                if(viewSettings.legend != null)
                {
                    legend = new Legend(viewSettings.legend);
                }

                if(viewSettings.camera != null)
                {
                    camera = viewSettings.camera.Clone();
                }

                enabled = viewSettings.enabled;
            }
        }

        public ViewSettings(string name, ViewSettings viewSettings)
            :base(viewSettings)
        {
            this.name = name;

            if (viewSettings != null)
            {
                if (viewSettings.appearanceSettings != null)
                {
                    appearanceSettings = new AppearanceSettings(viewSettings.appearanceSettings);
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
            }
        }

        public ViewSettings(Guid guid, string name, ViewSettings viewSettings)
            : base(guid, viewSettings)
        {
            this.name = name;

            if (viewSettings != null)
            {
                if (viewSettings.appearanceSettings != null)
                {
                    appearanceSettings = new AppearanceSettings(viewSettings.appearanceSettings);
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
            }
        }

        public bool IsValid(Type type)
        {
            if(types == null)
            {
                return false;
            }

            return types.Contains(type);
        }

        public bool IsValid(object @object)
        {
            if(@object == null)
            {
                return false;
            }

            if(@object is Type)
            {
                return IsValid((Type)@object);
            }

            return IsValid(@object.GetType());
        }

        public List<T> GetAppearances<T>(Guid guid) where T: IAppearance
        {
            return appearanceSettings?.GetAppearances<T>(guid);
        }

        public List<T> GetAppearances<T>(SAMObject sAMObject) where T: IAppearance
        {
            if(sAMObject == null)
            {
                return null;
            }

            return GetAppearances<T>(sAMObject.Guid);
        }

        public List<IAppearance> GetAppearances(Guid guid)
        {
            return appearanceSettings?.GetAppearances(guid);
        }

        public bool SetAppearances(Guid guid, IEnumerable<IAppearance> appearances)
        {
            if(appearanceSettings == null)
            {
                appearanceSettings = new AppearanceSettings();
            }

            return appearanceSettings.SetAppearances(guid, appearances);
        }

        public bool AddAppearances(Guid guid, IEnumerable<IAppearance> appearances)
        {
            if (appearanceSettings == null)
            {
                appearanceSettings = new AppearanceSettings();
            }

            return appearanceSettings.AddAppearances(guid, appearances);
        }

        public bool SetTypes(IEnumerable<Type> types)
        {
            this.types = new Types(types);
            return true;
        }

        public bool ContainsType(Type type)
        {
            if(types == null)
            {
                return false;
            }

            return types.Contains(type);
        }

        public bool ContainsAppearances(Guid guid)
        {
            if(appearanceSettings == null)
            {
                return false;
            }

            return appearanceSettings.ContainsAppearances(guid);
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if(!result)
            {
                return result;
            }

            if (jObject.ContainsKey("AppearanceSettings"))
            {
                appearanceSettings = new AppearanceSettings(jObject.Value<JObject>("AppearanceSettings"));
            }

            if (jObject.ContainsKey("Types"))
            {
                types = new Types(jObject.Value<JObject>("Types"));
            }

            if(jObject.ContainsKey("Legend"))
            {
                legend = new Legend(jObject.Value<JObject>("Legend"));
            }

            if (jObject.ContainsKey("Camera"))
            {
                camera = Core.Query.IJSAMObject(jObject.Value<JObject>("Camera")) as Camera;
            }

            if(jObject.ContainsKey("Enabled"))
            {
                enabled = jObject.Value<bool>("Enabled");
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();

            if (appearanceSettings != null)
            {
                jObject.Add("AppearanceSettings", appearanceSettings.ToJObject());
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

            jObject.Add("Enabled", enabled);

            return jObject;
        }

        public Legend Legend
        {
            get
            {
                return legend == null ? null : new Legend(legend);
            }

            set
            {
                if(value != null)
                {
                    legend = new Legend(value);
                }
                else
                {
                    legend = value;
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
    }
}
