using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Mollier.UI
{
    public abstract class VisibilitySetting : IVisibilitySetting
    {
        public Color Color { get; set; }

        public VisibilitySetting(Color color)
        {
            Color = color;
        }

        public VisibilitySetting(VisibilitySetting visibilitySetting)
        {
            Color = visibilitySetting.Color;
        }
        public VisibilitySetting(JObject jObject)
        {
            FromJObject(jObject);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Color"))
            {
                JObject jObject_Color = jObject.Value<JObject>("Color");
                if(jObject_Color != null)
                {
                    SAMColor sAMColor = new SAMColor(jObject_Color);
                    if (sAMColor != null)
                    {
                        Color = sAMColor.ToColor();
                    }
                }
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (Color == Color.Empty)
            {
                result.Add("Color", (new SAMColor(Color)).ToJObject());
            }

            return result;
        }
    }
}
