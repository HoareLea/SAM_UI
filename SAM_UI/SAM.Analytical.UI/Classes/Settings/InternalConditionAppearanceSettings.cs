// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class InternalConditionAppearanceSettings : TypeAppearanceSettings<InternalCondition>
    {

        public InternalConditionAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public InternalConditionAppearanceSettings(InternalConditionAppearanceSettings internalConditionAppearanceSettings)
            :base(internalConditionAppearanceSettings)
        {

        }

        public InternalConditionAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }
    }
}
