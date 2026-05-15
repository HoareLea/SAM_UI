using System.Text.Json.Nodes;
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

        public UITextFilter(JsonObject jObject)
            : base(jObject)
        {

        }
    }
}
