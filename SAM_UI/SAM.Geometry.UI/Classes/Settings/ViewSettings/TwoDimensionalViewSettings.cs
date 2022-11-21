using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public abstract class TwoDimensionalViewSettings : ViewSettings
    {
        private Plane plane;

        public TwoDimensionalViewSettings(int id, Plane plane, AppearanceSettings appearanceSettings)
            :base(id, appearanceSettings)
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

        public virtual bool FromJObject(JObject jObject)
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

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(plane != null)
            {
                jObject.Add("Plane", plane.ToJObject());
            }

            return jObject;
        }
    }
}
