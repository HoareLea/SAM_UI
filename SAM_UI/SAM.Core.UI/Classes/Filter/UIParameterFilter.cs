// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using System;

namespace SAM.Core.UI
{
    public class UIParameterFilter : UIFilter<ParameterFilter>
    {
        public UIParameterFilter(string name, Type type, ParameterFilter parameterFilter)
            :base(name, type, parameterFilter)
        {

        }

        public UIParameterFilter(UIParameterFilter uIParameterFilter)
            : base(uIParameterFilter)
        {

        }

        public UIParameterFilter(JsonObject jObject)
            : base(jObject)
        {

        }
    }
}
