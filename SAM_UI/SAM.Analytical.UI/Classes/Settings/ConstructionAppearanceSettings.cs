// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ConstructionAppearanceSettings : TypeAppearanceSettings<Construction>
    {

        public ConstructionAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public ConstructionAppearanceSettings(ConstructionAppearanceSettings constructionAppearanceSettings)
            :base(constructionAppearanceSettings)
        {

        }

        public ConstructionAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }
    }
}
