using System.Text.Json.Nodes;
using System;

namespace SAM.Core.UI
{
    public class UITypeFilter : UIFilter<TypeFilter>
    {
        public UITypeFilter(string name, Type type)
            :base(name, type, new TypeFilter() { Type = type })
        {

        }

        public UITypeFilter(UITypeFilter uITypeFilter)
            : base(uITypeFilter)
        {

        }

        public UITypeFilter(JsonObject jObject)
            : base(jObject)
        {

        }
    }
}
