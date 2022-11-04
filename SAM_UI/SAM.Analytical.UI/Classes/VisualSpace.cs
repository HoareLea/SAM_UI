using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Analytical.UI
{
    public class VisualSpace : SAMGeometry3DObjectCollection
    {
        private Space space;

        public VisualSpace(Space space)
            :base()
        {
            this.space = space;
        }

        public VisualSpace(JObject jObject)
            :base(jObject)
        {
            FromJObject(jObject);
        }

        public VisualSpace(VisualSpace visualSpace)
            :base(visualSpace)
        {
            space = visualSpace?.space;
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("Space"))
            {
                space = new Core.JSAMObjectWrapper(jObject.Value<JObject>("Space")).ToIJSAMObject() as Space;
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if(result == null)
            {
                return null;
            }

            if(space != null)
            {
                result.Add("Space", space.ToJObject());
            }

            return result;
        }
    }
}
