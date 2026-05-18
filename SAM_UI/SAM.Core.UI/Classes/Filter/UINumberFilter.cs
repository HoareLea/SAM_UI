// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
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

        public UINumberFilter(JsonObject jObject)
            : base(jObject)
        {

        }
    }
}
