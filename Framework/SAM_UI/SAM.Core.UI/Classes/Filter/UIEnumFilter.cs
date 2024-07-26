using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public class UIEnumFilter : UIFilter<IEnumFilter>
    {
        public UIEnumFilter(string name, Type type, IEnumFilter enumFilter)
            :base(name, type, enumFilter)
        {

        }

        public UIEnumFilter(UIEnumFilter uIEnumFilter)
            :base(uIEnumFilter)
        {

        }

        public UIEnumFilter(JObject jObject)
            :base(jObject)
        {

        }
    }
}
