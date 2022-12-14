using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public class Camera : ICamera
    {
        private Point3D location;

        public Camera(Camera camera)
        {
            if(camera != null)
            {
                location = camera.location != null ? new Point3D(camera.location) : null;
            }
        }

        public Camera(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Point3D Location
        {
            get
            {
                return location;
            }

            set
            {
                location = value;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("Location"))
            {
                location = new Point3D(jObject.Value<JObject>("Location"));
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(location != null)
            {
                jObject.Add("Location", location.ToJObject());
            }

            return jObject;
        }
    }
}
