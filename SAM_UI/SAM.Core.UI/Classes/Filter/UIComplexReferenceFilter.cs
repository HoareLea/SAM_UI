// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
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

        public UIComplexReferenceFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
