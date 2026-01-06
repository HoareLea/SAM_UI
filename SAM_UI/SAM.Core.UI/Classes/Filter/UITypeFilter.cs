// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
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

        public UITypeFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
