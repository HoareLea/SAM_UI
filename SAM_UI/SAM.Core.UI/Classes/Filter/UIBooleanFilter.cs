// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public class UIBooleanFilter : UIFilter<IBooleanFilter>
    {
        public UIBooleanFilter(string name, Type type, IBooleanFilter booleanFilter)
            :base(name, type, booleanFilter)
        {

        }

        public UIBooleanFilter(UIBooleanFilter uIBooleanFilter)
            : base(uIBooleanFilter)
        {

        }

        public UIBooleanFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
