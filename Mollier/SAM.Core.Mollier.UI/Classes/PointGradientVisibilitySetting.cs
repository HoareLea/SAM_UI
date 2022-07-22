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
            throw new NotImplementedException();
        }

        public JObject ToJObject()
        {
            throw new NotImplementedException();
        }
    }
}
