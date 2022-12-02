using Newtonsoft.Json.Linq;
using SAM.Core;
using System;

namespace SAM.Analytical.UI
{
    public class ZoneAppearanceSettings : IJSAMObject
    {
        string ZoneCategory { get; set; }

        public ZoneAppearanceSettings(string zoneCategory)
        {
            ZoneCategory = zoneCategory;
        }

        public ZoneAppearanceSettings(ZoneAppearanceSettings zoneAppearanceSettings)
        {
            if(zoneAppearanceSettings != null)
            {
                ZoneCategory = zoneAppearanceSettings.ZoneCategory;
            }
        }

        public ZoneAppearanceSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ZoneCategory"))
            {
                ZoneCategory = jObject.Value<string>("ZoneCategory");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(ZoneCategory != null)
            {
                jObject.Add("ZoneCategory", ZoneCategory);
            }

            return jObject;
        }
    }
}
