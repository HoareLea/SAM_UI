// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;
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

        public UIBooleanFilter(JsonObject jObject)
            : base(jObject)
        {

        }
    }
}
