using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class AnalyticalTwoDimensionalViewSettings : TwoDimensionalViewSettings
    {
        public ZoneAppearanceSettings ZoneAppearanceSettings { get; set; }

        public AnalyticalTwoDimensionalViewSettings(int id, Plane plane, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(id, plane, appearanceSettings, types)
        {

        }

        public AnalyticalTwoDimensionalViewSettings(TwoDimensionalViewSettings twoDimensionalViewSettings)
            : base(twoDimensionalViewSettings)
        {

        }

        public AnalyticalTwoDimensionalViewSettings(JObject jObject)
            : base(jObject)
        {

        }

        public AnalyticalTwoDimensionalViewSettings(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings)
            :base(analyticalTwoDimensionalViewSettings)
        {
            if (analyticalTwoDimensionalViewSettings?.ZoneAppearanceSettings != null)
            {
                ZoneAppearanceSettings = new ZoneAppearanceSettings(analyticalTwoDimensionalViewSettings.ZoneAppearanceSettings);
            }


        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("ZoneAppearanceSettings"))
            {
                ZoneAppearanceSettings = new ZoneAppearanceSettings(jObject.Value<JObject>("ZoneAppearanceSettings"));
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

            if(ZoneAppearanceSettings != null)
            {
                jObject.Add("ZoneAppearanceSettings", ZoneAppearanceSettings.ToJObject());
            }

            return jObject;
        }
    }
}
