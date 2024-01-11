using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Geometry.Object.Spatial;

namespace SAM.Geometry.UI
{
    public class GeometryObjectCollection : SAMGeometry3DObjectCollection, ITaggable
    {
        public Tag Tag { get; set; }

        public GeometryObjectCollection()
            :base()
        {

        }

        public GeometryObjectCollection(JObject jObject)
            :base(jObject)
        {
            FromJObject(jObject);
        }

        public GeometryObjectCollection(GeometryObjectCollection geometryObjectCollection)
            :base(geometryObjectCollection)
        {
            Tag = geometryObjectCollection?.Tag;
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            Tag = Core.Query.Tag(jObject);

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if(result == null)
            {
                return null;
            }

            Core.Modify.Add(result, Tag);

            return result;
        }
    }
}
