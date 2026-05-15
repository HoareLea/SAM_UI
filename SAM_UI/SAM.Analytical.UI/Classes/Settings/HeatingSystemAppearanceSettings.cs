// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
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

        public HeatingSystemAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
