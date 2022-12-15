using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ThreeDimensionalViewSettings : ViewSettings
    {
        public ThreeDimensionalViewSettings(Guid guid, string name, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(guid, name, appearanceSettings, types)
        {

        }

        public ThreeDimensionalViewSettings(string name, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            : base(Guid.NewGuid(), name, appearanceSettings, types)
        {

        }

        public ThreeDimensionalViewSettings(JObject jObject)
            : base(jObject)
        {

        }

        public ThreeDimensionalViewSettings(ThreeDimensionalViewSettings threeDimensionalViewSettings)
            :base(threeDimensionalViewSettings)
        {

        }

        public ThreeDimensionalViewSettings(string name, ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(name, threeDimensionalViewSettings)
        {

        }

        public ThreeDimensionalViewSettings(Guid guid, string name, ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(guid, name, threeDimensionalViewSettings)
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
