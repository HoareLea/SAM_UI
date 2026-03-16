using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public class Camera : ICamera
    {
        private Point3D location;
        private Vector3D lookDirection;
        private Vector3D upDirection;

        public Camera(Camera camera)
        {
            if(camera != null)
            {
                location = camera.location != null ? new Point3D(camera.location) : null;
                lookDirection = camera.lookDirection != null ? new Vector3D(camera.lookDirection) : null;
                upDirection = camera.upDirection != null ? new Vector3D(camera.upDirection) : null;
            }
        }

        public Camera(Point3D location, Vector3D lookDirection, Vector3D upDirection)
        {
            this.location = location;
            this.lookDirection = lookDirection;
            this.upDirection = upDirection;
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

        public Vector3D LookDirection
        {
            get
            {
                return lookDirection;
            }

            set
            {
                lookDirection = value;
            }
        }

        public Vector3D UpDirection
        {
            get
            {
                return upDirection;
            }

            set
            {
                upDirection = value;
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

            if (jObject.ContainsKey("LookDirection"))
            {
                lookDirection = new Vector3D(jObject.Value<JObject>("LookDirection"));
            }

            if (jObject.ContainsKey("UpDirection"))
            {
                upDirection = new Vector3D(jObject.Value<JObject>("UpDirection"));
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

            if(lookDirection != null)
            {
                jObject.Add("LookDirection", lookDirection.ToJObject());
            }

            if (upDirection != null)
            {
                jObject.Add("UpDirection", upDirection.ToJObject());
            }

            return jObject;
        }
    }
}
