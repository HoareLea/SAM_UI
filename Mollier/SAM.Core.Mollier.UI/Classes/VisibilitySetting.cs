using Newtonsoft.Json.Linq;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public abstract class VisibilitySetting : IVisibilitySetting
    {
        public Color Color { get; set; } = Color.Empty;

        public bool Visible { get; set; } = true;

        public VisibilitySetting(Color color)
        {
            Color = color;
        }

        public VisibilitySetting(VisibilitySetting visibilitySetting)
        {
            Color = visibilitySetting.Color;
            Visible = visibilitySetting.Visible;
        }
        public VisibilitySetting(JObject jObject)
        {
            FromJObject(jObject);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Color"))
            {
                JObject jObject_Color = jObject.Value<JObject>("Color");
                if (jObject_Color != null)
                {
                    SAMColor sAMColor = new SAMColor(jObject_Color);
                    if (sAMColor != null)
                    {
                        Color = sAMColor.ToColor();
                    }
                }
            }

            if (jObject.ContainsKey("Visible"))
            {
                Visible = jObject.Value<bool>("Visible");
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (Color != Color.Empty)
            {
                result.Add("Color", (new SAMColor(Color)).ToJObject());
            }

            result.Add("Visible", Visible);

            return result;
        }
    }
}
