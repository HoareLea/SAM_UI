// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class CoolingSystemAppearanceSettings : TypeAppearanceSettings<CoolingSystem>
    {

        public CoolingSystemAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public CoolingSystemAppearanceSettings(CoolingSystemAppearanceSettings coolingSystemAppearanceSettings)
            :base(coolingSystemAppearanceSettings)
        {

        }

        public CoolingSystemAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }
    }
}
