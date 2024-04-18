using System.Drawing;
using Newtonsoft.Json.Linq;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI.Classes
{
    public class ChartLabel : IJSAMObject
    {
        public object Tag { get; set; }

        public Point2D Position { get; set; }
        public string Text { get; set; }
        public double Angle { get; set; }
        public Color Color { get; set; }
        
        public ChartLabel()
        {

        }

        public ChartLabel(ChartLabel chartLabel)
        {
            Position = chartLabel.Position;
            Text = chartLabel.Text;
            Angle = chartLabel.Angle;
            Color = chartLabel.Color;
        }
        public ChartLabel(Point2D position, string text, double angle, Color color)
        {
            Position = position;
            Text = text;
            Angle = angle;
            Color = color;
        }
        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Position"))
            {
                JObject jObject_Position = jObject.Value<JObject>("Position");
                if (jObject_Position != null)
                {
                    Position = new Point2D(jObject_Position);
                }
            }

            if (jObject.ContainsKey("Text"))
            {
                Text = jObject.Value<string>("Text");
            }

            if (jObject.ContainsKey("Angle"))
            {
                Angle = jObject.Value<double>("Angle");
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

            return true;
        }
        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (Position != null)
            {
                result.Add("Position", new Point2D(Position).ToJObject());
            }

            if (Text != null)
            {
                result.Add("Text", Text);
            }

            if (double.IsNaN(Angle))
            {
                result.Add("Angle", Angle);
            }

            if (Color != Color.Empty)
            {
                result.Add("Color", new SAMColor(Color).ToJObject());
            }


            return result;
        }
    }
}