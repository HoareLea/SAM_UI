using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ThreeDimensionalViewSettings : ViewSettings
    {
        private List<Plane> planes;

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
            planes = threeDimensionalViewSettings?.Planes?.ConvertAll(x => new Plane(x));
        }

        public ThreeDimensionalViewSettings(string name, ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(name, threeDimensionalViewSettings)
        {
            planes = threeDimensionalViewSettings?.Planes?.ConvertAll(x => new Plane(x));
        }

        public ThreeDimensionalViewSettings(Guid guid, string name, ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(guid, name, threeDimensionalViewSettings)
        {
            planes = threeDimensionalViewSettings?.Planes?.ConvertAll(x => new Plane(x));
        }

        public List<Plane> Planes
        {
            get
            {
                return planes;
            }

            set
            {
                planes = value;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("Planes"))
            {
                JArray jArray = jObject.Value<JArray>("Planes");
                if(jArray != null)
                {
                    planes = new List<Plane>();
                    foreach(JObject jObject_Plane in jArray)
                    {
                        planes.Add(new Plane(jObject_Plane));
                    }
                }
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

            if(planes != null)
            {
                JArray jArray = new JArray();
                foreach(Plane plane in planes)
                {
                    jArray.Add(plane.ToJObject());
                }
                jObject.Add("Planes", jArray);
            }

            return jObject;
        }
    }
}
