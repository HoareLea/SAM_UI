using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public class Face3DObject : Face3D, IFace3DObject
    {
        public SurfaceAppearance SurfaceAppearance { get; set; }

        public Face3D Face3D
        {
            get
            {
                return new Face3D(this);
            }
        }

        public Face3DObject(Face3D face3D)
            : base(face3D)
        {

        }

        public Face3DObject(Face3D face3D, SurfaceAppearance surfaceAppearance)
            : base(face3D)
        {
            if(surfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(surfaceAppearance);
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("SurfaceAppearance"))
            {
                SurfaceAppearance = new SurfaceAppearance(jObject.Value<JObject>("SurfaceAppearance"));
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

            if(SurfaceAppearance != null)
            {
                jObject.Add("SurfaceAppearance", SurfaceAppearance.ToJObject());
            }

            return jObject;
        }
    }
}
