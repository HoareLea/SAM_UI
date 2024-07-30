﻿using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public class UINumberFilter : UIFilter<INumberFilter>
    {
        public UINumberFilter(string name, Type type, INumberFilter numberFilter)
            :base(name, type, numberFilter)
        {

        }

        public UINumberFilter(UINumberFilter uINumberFilter)
            : base(uINumberFilter)
        {

        }

        public UINumberFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
