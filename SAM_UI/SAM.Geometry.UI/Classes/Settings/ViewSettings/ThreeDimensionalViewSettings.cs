using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ThreeDimensionalViewSettings : ViewSettings
    {
        public ThreeDimensionalViewSettings(int id, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(id, appearanceSettings, types)
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

        public virtual bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }


            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            return jObject;
        }
    }
}
