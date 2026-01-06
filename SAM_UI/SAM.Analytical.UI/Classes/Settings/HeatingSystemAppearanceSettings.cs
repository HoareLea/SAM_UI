// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class HeatingSystemAppearanceSettings : TypeAppearanceSettings<HeatingSystem>
    {

        public HeatingSystemAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public HeatingSystemAppearanceSettings(HeatingSystemAppearanceSettings heatingSystemAppearanceSettings)
            :base(heatingSystemAppearanceSettings)
        {

        }

        public HeatingSystemAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }
    }
}
