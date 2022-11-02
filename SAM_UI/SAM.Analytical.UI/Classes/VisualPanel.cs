using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;

namespace SAM.Analytical.UI
{
    public class VisualPanel : SAMGeometry3DObjectCollection
    {
        private Panel panel;

        public VisualPanel(Panel panel)
            :base()
        {
            this.panel = panel;
        }

        public VisualPanel(JObject jObject)
            :base(jObject)
        {
            FromJObject(jObject);
        }

        public VisualPanel(VisualPanel visualPanel)
            :base(visualPanel)
        {
            panel = visualPanel?.panel;
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("Panel"))
            {
                panel = new Core.JSAMObjectWrapper(jObject.Value<JObject>("Panel")).ToIJSAMObject() as Panel;
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

            if(panel != null)
            {
                result.Add("Panel", panel.ToJObject());
            }

            return result;
        }
    }
}
