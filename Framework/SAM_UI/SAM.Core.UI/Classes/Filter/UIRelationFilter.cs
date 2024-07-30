using Newtonsoft.Json.Linq;
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

        public UIRelationFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
