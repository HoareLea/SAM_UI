using Newtonsoft.Json.Linq;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public class UIMollierZone : MollierZone
    {
        public Color Color { get; set; }
        public string Text { get; set; }

        public UIMollierZone(MollierZone mollierZone, Color color, string text)
            :base(mollierZone)
        {
            Color = color;
            Text = text;
        }

        public UIMollierZone(UIMollierZone uIMollierZone)
            :base(uIMollierZone)
        {
            Color = uIMollierZone.Color;
            Text = uIMollierZone.Text;
        }
        public UIMollierZone(JObject jObject)
            :base(jObject)
        {

        }

        public override bool FromJObject(JObject jObject)
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

            if(jObject.ContainsKey("Text"))
            {
                Text = jObject.Value<string>("Text");
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (Color != Color.Empty)
            {
                result.Add("Color", (new SAMColor(Color)).ToJObject());
            }

            if(Text != null)
            {
                result.Add("Text", Text);
            }

            return result;
        }
    }
}
