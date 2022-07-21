using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Mollier.UI
{
    public class BuiltInVisibilitySetting : VisibilitySetting
    {
        public ChartParameterType ChartParameterType { get; set; }
        public ChartDataType ChartDataType { get; set; }


        public BuiltInVisibilitySetting(ChartParameterType chartParameterType, ChartDataType chartDataType, Color color)
            : base(color)
        {
            ChartParameterType = chartParameterType;
            ChartDataType = chartDataType;
        }

        public BuiltInVisibilitySetting(ChartParameterType chartParameterType,  Color color)
            : base(color)
        {
            ChartParameterType = chartParameterType;
            ChartDataType = ChartDataType.Undefined;
        }

        public BuiltInVisibilitySetting(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }

        public BuiltInVisibilitySetting(BuiltInVisibilitySetting builtInVisibilitySetting)
            : base(builtInVisibilitySetting)
        {
            ChartParameterType = builtInVisibilitySetting.ChartParameterType;
            ChartDataType = builtInVisibilitySetting.ChartDataType;
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("ChartDataType"))
            {
                if (Enum.TryParse(jObject.Value<string>("ChartDataType"), out ChartDataType chartDataType))
                {
                    ChartDataType = chartDataType;
                }
            }

            if (jObject.ContainsKey("ChartParameterType"))
            {
                if (Enum.TryParse(jObject.Value<string>("ChartParameterType"), out ChartParameterType chartParameterType))
                {
                    ChartParameterType = chartParameterType;
                }
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if(jObject == null)
            {
                return null;
            }

            jObject.Add("ChartParameterType", ChartParameterType.ToString());
            jObject.Add("ChartDataType", ChartDataType.ToString());

            return jObject;
        }
    }
}
