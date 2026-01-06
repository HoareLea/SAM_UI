using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public class UILogicalFilter : UIFilter<LogicalFilter>
    {

        public UILogicalFilter(string name, Type type, LogicalFilter logicalFilter)
            :base(name, type, logicalFilter)
        {

        }

        public UILogicalFilter(UILogicalFilter uILogicalFilter)
            :base(uILogicalFilter)
        {

        }

        public UILogicalFilter(JObject jObject)
            : base(jObject)
        {

        }

    }
}
