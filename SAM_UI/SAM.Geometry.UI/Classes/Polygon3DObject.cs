using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public class Polygon3DObject : Polygon3D, IPolygon3DObject
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Polygon3D Polygon3D
        {
            get
            {
                return new Polygon3D(this);
            }
        }

        public Polygon3DObject(Polygon3D polygon3D)
            : base(polygon3D)
        {

        }

        public Polygon3DObject(JObject jObject)
            : base(jObject)
        {

        }

        public Polygon3DObject(Polygon3DObject polygon3DObject)
                : base(polygon3DObject)
        {
            if (polygon3DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(polygon3DObject?.CurveAppearance);
            }
        }

        public Polygon3DObject(Polygon3D polygon3D, CurveAppearance curveAppearance)
            : base(polygon3D)
        {
            if (curveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(curveAppearance);
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if (!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("CurveAppearance"))
            {
                CurveAppearance = new CurveAppearance(jObject.Value<JObject>("CurveAppearance"));
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if (jObject == null)
            {
                return null;
            }

            if (CurveAppearance != null)
            {
                jObject.Add("CurveAppearance", CurveAppearance.ToJObject());
            }

            return jObject;
        }
    }
}
