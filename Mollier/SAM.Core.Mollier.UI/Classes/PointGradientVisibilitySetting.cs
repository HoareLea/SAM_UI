using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public bool FromJObject(JObject jObject)
        {
            if (!base.FromJObject(jObject))
            {
                return false;
            }
            if (jObject.ContainsKey("GradientColor"))
            {
                if (Enum.TryParse(jObject.Value<string>("GradientColor"), out Color gradientColor))
                {
                    GradientColor = gradientColor;
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
            jObject.Add("GradientColor", GradientColor.ToString());//there is saves as "yellow" now alpha red etc, bad format
            jObject.Add("ChartParameterType", ChartParameterType.ToString());
            jObject.Add("ChartDataType", ChartDataType.ToString());
            return jObject;
        }
    }
}
