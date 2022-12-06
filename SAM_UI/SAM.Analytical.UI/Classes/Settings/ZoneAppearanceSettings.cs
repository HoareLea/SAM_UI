using Newtonsoft.Json.Linq;

namespace SAM.Analytical.UI
{
    public class ZoneAppearanceSettings : Geometry.UI.ParameterAppearanceSettings
    {
        public string ZoneCategory { get; set; }

        public ZoneAppearanceSettings(string zoneCategory)
            :base("Color")
        {
            ZoneCategory = zoneCategory;
        }

        public ZoneAppearanceSettings(string parameterName, string zoneCategory)
            : base(parameterName)
        {
            ZoneCategory = zoneCategory;
        }

        public ZoneAppearanceSettings(ZoneAppearanceSettings zoneAppearanceSettings)
            : base(zoneAppearanceSettings)
        {
            if(zoneAppearanceSettings != null)
            {
                ZoneCategory = zoneAppearanceSettings.ZoneCategory;
            }
        }

        public ZoneAppearanceSettings(JObject jObject)
            : base(jObject)
        {

        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("ZoneCategory"))
            {
                ZoneCategory = jObject.Value<string>("ZoneCategory");
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if(jObject == null)
            {
                return null;
            }

            if(ZoneCategory != null)
            {
                jObject.Add("ZoneCategory", ZoneCategory);
            }

            return jObject;
        }
    }
}
