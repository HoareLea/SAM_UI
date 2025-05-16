using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public class UIParameterFilter : UIFilter<ParameterFilter>
    {
        public UIParameterFilter(string name, Type type, ParameterFilter parameterFilter)
            :base(name, type, parameterFilter)
        {

        }

        public UIParameterFilter(UIParameterFilter uIParameterFilter)
            : base(uIParameterFilter)
        {

        }

        public UIParameterFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
