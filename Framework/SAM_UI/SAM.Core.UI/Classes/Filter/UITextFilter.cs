using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public class UITextFilter : UIFilter<ITextFilter>
    {
        public UITextFilter(string name, Type type, ITextFilter textFilter)
            :base(name, type, textFilter)
        {

        }

        public UITextFilter(UITextFilter uITextFilter)
            : base(uITextFilter)
        {

        }

        public UITextFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
