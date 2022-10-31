using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public class Segment3DObject : Segment3D, ISegment3DObject
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Segment3D Segment3D
        {
            get
            {
                return new Segment3D(this);
            }
        }

        public Segment3DObject(Segment3D segment3D)
            : base(segment3D)
        {

        }

        public Segment3DObject(Segment3D segment3D, CurveAppearance curveAppearance)
            : base(segment3D)
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
