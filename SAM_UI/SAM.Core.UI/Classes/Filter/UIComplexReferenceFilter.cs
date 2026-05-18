// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using System;

namespace SAM.Core.UI
{
    public class UIComplexReferenceFilter : UIFilter<ComplexReferenceFilter>
    {
        public UIComplexReferenceFilter(string name, Type type, ComplexReferenceFilter complexReferenceFilter)
            :base(name, type, complexReferenceFilter)
        {

        }

        public UIComplexReferenceFilter(UIComplexReferenceFilter complexReferenceFilter)
            : base(complexReferenceFilter)
        {

        }

        public UIComplexReferenceFilter(JsonObject jObject)
            : base(jObject)
        {

        }
    }
}
