using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class AnalyticalThreeDimensionalViewSettings : ThreeDimensionalViewSettings, IAnalyticalViewSettings
    {
        public SpaceAppearanceSettings SpaceAppearanceSettings { get; set; }

        public AnalyticalThreeDimensionalViewSettings(Guid guid, string name, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(guid, name, appearanceSettings, types)
        {

        }

        public AnalyticalThreeDimensionalViewSettings(ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(threeDimensionalViewSettings)
        {

        }

        public AnalyticalThreeDimensionalViewSettings(JObject jObject)
            : base(jObject)
        {

        }

        public AnalyticalThreeDimensionalViewSettings(AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings)
            :base(analyticalThreeDimensionalViewSettings)
        {
            if (analyticalThreeDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalThreeDimensionalViewSettings.SpaceAppearanceSettings);
            }
        }

        public AnalyticalThreeDimensionalViewSettings(string name, AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings)
            : base(name, analyticalThreeDimensionalViewSettings)
        {
            if (analyticalThreeDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalThreeDimensionalViewSettings.SpaceAppearanceSettings);
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("SpaceAppearanceSettings"))
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(jObject.Value<JObject>("SpaceAppearanceSettings"));
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

            if (SpaceAppearanceSettings != null)
            {
                jObject.Add("SpaceAppearanceSettings", SpaceAppearanceSettings.ToJObject());
            }

            return jObject;
        }
    }
}
