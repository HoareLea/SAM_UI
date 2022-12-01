using Newtonsoft.Json.Linq;
using System.Windows.Media;

namespace SAM.Geometry.UI
{
    public abstract class Appearance :IAppearance
    {
        public Color Color { get; set; }

        public double Opacity { get; set; } = 1;

        public Appearance(Color color)
        {
            Color = color;
        }

        public Appearance(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Appearance(Appearance appearance)
        {
            if(appearance != null)
            {
                Color = appearance.Color;
                Opacity = appearance.Opacity;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Color"))
            {
                Core.SAMColor sAMColor = new Core.SAMColor(jObject.Value<JObject>("Color"));
                if (sAMColor != null)
                {
                    Color = Color.FromArgb(sAMColor.Alpha, sAMColor.Red, sAMColor.Green, sAMColor.Blue);
                }
            }

            if (jObject.ContainsKey("Opacity"))
            {
                Opacity = jObject.Value<double>("Opacity");
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            jObject.Add("Color", new Core.SAMColor(Color.A, Color.R, Color.G, Color.B).ToJObject());
            jObject.Add("Opacity", Opacity);

            return jObject;
        }
    }
}
