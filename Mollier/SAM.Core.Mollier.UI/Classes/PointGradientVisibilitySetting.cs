using Newtonsoft.Json.Linq;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public class PointGradientVisibilitySetting : VisibilitySetting, IUserVisibilitySetting
    {
        public Color GradientColor { get; set; }
        public ChartDataType ChartDataType { get; } = ChartDataType.Undefined;
        public ChartParameterType ChartParameterType { get; } = ChartParameterType.Point;


        public PointGradientVisibilitySetting(PointGradientVisibilitySetting pointGradientVisibilitySetting)
            :base(pointGradientVisibilitySetting)
        {
            GradientColor = pointGradientVisibilitySetting.GradientColor;
        }

        public PointGradientVisibilitySetting(JObject jObject)
            : base(jObject)
        {

        }


        public PointGradientVisibilitySetting(Color color, Color gradientColor)
            :base(color)
        {
            GradientColor = gradientColor;
        }

        public override bool FromJObject(JObject jObject)
        {
            if (!base.FromJObject(jObject))
            {
                return false;
            }
            //if (jObject.ContainsKey("GradientColor"))
            //{
            //    if (Enum.TryParse(jObject.Value<string>("GradientColor"), out Color gradientColor))
            //    {
            //        GradientColor = gradientColor;
            //    }
            //}

            if (jObject.ContainsKey("GradientColor"))
            {
                JObject jObject_Color = jObject.Value<JObject>("GradientColor");
                if (jObject_Color != null)
                {
                    SAMColor sAMColor = new SAMColor(jObject_Color);
                    if (sAMColor != null)
                    {
                        GradientColor = sAMColor.ToColor();
                    }
                }
            }


            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if (jObject == null)
            {
                return null;
            }

            if (GradientColor != Color.Empty)
            {
                jObject.Add("GradientColor", (new SAMColor(GradientColor)).ToJObject());
            }

            //jObject.Add("GradientColor", GradientColor.ToString());//there is saves as "yellow" now alpha red etc, bad format
            jObject.Add("ChartParameterType", ChartParameterType.ToString());
            jObject.Add("ChartDataType", ChartDataType.ToString());
            return jObject;
        }
    }
}
