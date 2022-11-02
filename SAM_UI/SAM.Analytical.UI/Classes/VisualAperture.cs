using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Analytical.UI
{
    public class VisualAperture : SAMGeometry3DObjectCollection
    {
        private Aperture aperture;

        public VisualAperture(Aperture aperture)
        {
            this.aperture = aperture;
        }
        public VisualAperture(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }

        public VisualAperture(VisualAperture visualAperture)
            : base(visualAperture)
        {
            aperture = visualAperture?.aperture;
        }

        public override bool FromJObject(JObject jObject)
        {
            if (!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("Aperture"))
            {
                aperture = new Core.JSAMObjectWrapper(jObject.Value<JObject>("Aperture")).ToIJSAMObject() as Aperture;
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if (result == null)
            {
                return null;
            }

            if (aperture != null)
            {
                result.Add("Aperture", aperture.ToJObject());
            }

            return result;
        }
    }
}
