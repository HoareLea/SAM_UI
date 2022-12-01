using Newtonsoft.Json.Linq;
using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public abstract class ViewSettings : IViewSettings
    {
        private int id = -1;
        private AppearanceSettings appearanceSettings;
        private Types types;

        public ViewSettings(int id)
        {
            this.id = id;
        }

        public ViewSettings(int id, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
        {
            this.id = id;
            this.appearanceSettings = appearanceSettings != null ? new AppearanceSettings(appearanceSettings) : null;
            this.types = types != null ? new Types(types) : null;
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

                if(viewSettings.types != null)
                {
                    types = new Types(viewSettings.types);
                }
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

            if (jObject.ContainsKey("Types"))
            {
                types = new Types(jObject.Value<JObject>("Types"));
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

            if (types != null)
            {
                jObject.Add("Types", types.ToJObject());
            }

            return jObject;
        }
    }
}
