using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class TwoDimensionalViewSettings : ViewSettings
    {
        private Plane plane;

        public TwoDimensionalViewSettings(Guid guid, string name, Plane plane, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(guid, name, appearanceSettings, types)
        {
            this.plane = plane;
        }

        public TwoDimensionalViewSettings(JObject jObject)
            : base(jObject)
        {

        }

        public TwoDimensionalViewSettings(TwoDimensionalViewSettings twoDimensionalViewSettings)
            :base(twoDimensionalViewSettings)
        {
            if(twoDimensionalViewSettings != null)
            {
                if(twoDimensionalViewSettings.plane != null)
                {
                    plane = new Plane(twoDimensionalViewSettings.plane);
                }
            }
        }

        public TwoDimensionalViewSettings(string name, TwoDimensionalViewSettings twoDimensionalViewSettings)
        : base(name, twoDimensionalViewSettings)
        {
            if (twoDimensionalViewSettings != null)
            {
                if (twoDimensionalViewSettings.plane != null)
                {
                    plane = new Plane(twoDimensionalViewSettings.plane);
                }
            }
        }

        public Plane Plane
        {
            get
            {
                return plane == null ? null : new Plane(plane);
            }
            set
            {
                plane = value == null ? null : new Plane(value);
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("Plane"))
            {
                plane = new Plane(jObject.Value<JObject>("Plane"));
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

            if(plane != null)
            {
                jObject.Add("Plane", plane.ToJObject());
            }

            return jObject;
        }
    }
}
