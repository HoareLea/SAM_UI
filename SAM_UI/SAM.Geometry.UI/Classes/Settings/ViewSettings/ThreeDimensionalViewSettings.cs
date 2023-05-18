﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ThreeDimensionalViewSettings : ViewSettings
    {
        public ThreeDimensionalViewSettings(Guid guid, string name, GuidAppearanceSettings guidAppearanceSettings, IEnumerable<Type> types, IEnumerable<ValueAppearanceSettings> valueAppearanceSettings)
            :base(guid, name, guidAppearanceSettings, types, valueAppearanceSettings)
        {

        }

        public ThreeDimensionalViewSettings(string name, GuidAppearanceSettings appearanceSettings, IEnumerable<Type> types, IEnumerable<ValueAppearanceSettings> valueAppearanceSettings)
            : base(Guid.NewGuid(), name, appearanceSettings, types, valueAppearanceSettings)
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
