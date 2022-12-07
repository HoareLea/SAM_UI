using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class AnalyticalThreeDimensionalViewSettings : ThreeDimensionalViewSettings
    {
        public AnalyticalThreeDimensionalViewSettings(Guid guid, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(guid, appearanceSettings, types)
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

        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
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

            return jObject;
        }
    }
}
