using Newtonsoft.Json.Linq;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public class PointGradientVisibilitySetting : VisibilitySetting, IUserVisibilitySetting
    {
        public Color GradientColor { get; set; }
        public ChartDataType ChartDataType { get; set; } = ChartDataType.Undefined;
        public ChartParameterType ChartParameterType { get; set; } = ChartParameterType.Point;


        public PointGradientVisibilitySetting(PointGradientVisibilitySetting pointGradientVisibilitySetting)
            :base(pointGradientVisibilitySetting)
        {
            if(pointGradientVisibilitySetting != null)
            {
                GradientColor = pointGradientVisibilitySetting.GradientColor;
                ChartDataType = pointGradientVisibilitySetting.ChartDataType;
                ChartParameterType = pointGradientVisibilitySetting.ChartParameterType;
            }
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

            if (jObject.ContainsKey("ChartParameterType"))
            {
                ChartParameterType = Core.Query.Enum<ChartParameterType>(jObject.Value<string>("ChartParameterType"));
            }

            if (jObject.ContainsKey("ChartDataType"))
            {
                ChartDataType = Core.Query.Enum<ChartDataType>(jObject.Value<string>("ChartDataType"));
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

            jObject.Add("ChartParameterType", ChartParameterType.ToString());
            jObject.Add("ChartDataType", ChartDataType.ToString());
            return jObject;
        }
    }
}
