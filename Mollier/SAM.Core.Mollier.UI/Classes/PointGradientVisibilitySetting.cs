using System.Text.Json.Nodes;
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

        public PointGradientVisibilitySetting(JsonObject jObject)
            : base(jObject)
        {

        }


        public PointGradientVisibilitySetting(Color color, Color gradientColor)
            :base(color)
        {
            GradientColor = gradientColor;
        }

        public override bool FromJsonObject(JsonObject jObject)
        {
            if (!base.FromJsonObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("GradientColor"))
            {
                JsonObject jObject_Color = jObject["GradientColor"] as JsonObject;
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
                ChartParameterType = Core.Query.Enum<ChartParameterType>(jObject["ChartParameterType"]?.GetValue<string>() ?? null);
            }

            if (jObject.ContainsKey("ChartDataType"))
            {
                ChartDataType = Core.Query.Enum<ChartDataType>(jObject["ChartDataType"]?.GetValue<string>() ?? null);
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jObject = base.ToJsonObject();
            if (jObject == null)
            {
                return null;
            }

            if (GradientColor != Color.Empty)
            {
                jObject.Add("GradientColor", (new SAMColor(GradientColor)).ToJsonObject());
            }

            jObject.Add("ChartParameterType", ChartParameterType.ToString());
            jObject.Add("ChartDataType", ChartDataType.ToString());
            return jObject;
        }
    }
}
