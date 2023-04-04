using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class TwoDimensionalViewSettings : ViewSettings
    {
        private Plane plane;
        private TextAppearance textAppearance;

        public TwoDimensionalViewSettings(Guid guid, string name, Plane plane, AppearanceSettings appearanceSettings, IEnumerable<Type> types, TextAppearance textAppearance)
            :base(guid, name, appearanceSettings, types)
        {
            this.plane = plane;
            this.textAppearance = textAppearance;
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
                    textAppearance = twoDimensionalViewSettings.textAppearance == null ? null : new TextAppearance(twoDimensionalViewSettings.textAppearance);
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
                    textAppearance = twoDimensionalViewSettings.textAppearance == null ? null : new TextAppearance(twoDimensionalViewSettings.textAppearance);
                }
            }
        }

        public TwoDimensionalViewSettings(Guid guid, string name, TwoDimensionalViewSettings twoDimensionalViewSettings)
            : base(guid, name, twoDimensionalViewSettings)
        {
            if (twoDimensionalViewSettings != null)
            {
                if (twoDimensionalViewSettings.plane != null)
                {
                    plane = new Plane(twoDimensionalViewSettings.plane);
                    textAppearance = twoDimensionalViewSettings.textAppearance == null ? null : new TextAppearance(twoDimensionalViewSettings.textAppearance);
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

        public TextAppearance TextAppearance
        {
            get
            {
                return textAppearance;
            }
            set
            {
                textAppearance = value;
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

            if (jObject.ContainsKey("TextAppearance"))
            {
                textAppearance = new TextAppearance(jObject.Value<JObject>("TextAppearance"));
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

            if (textAppearance != null)
            {
                jObject.Add("TextAppearance", textAppearance.ToJObject());
            }

            return jObject;
        }
    }
}
