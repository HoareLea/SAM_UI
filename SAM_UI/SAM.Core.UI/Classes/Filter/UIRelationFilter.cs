using System.Text.Json.Nodes;
using System;

namespace SAM.Core.UI
{
    public class UIRelationFilter : UIFilter<IRelationFilter>
    {
        public UIRelationFilter(string name, Type type, IRelationFilter relationFilter)
            :base(name, type, relationFilter)
        {

        }

        public UIRelationFilter(UIRelationFilter uIRelationFilter)
            : base(uIRelationFilter)
        {

        }

        public UIRelationFilter(JsonObject jObject)
            : base(jObject)
        {

        }
    }
}
