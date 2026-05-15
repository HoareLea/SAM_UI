// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class VentilationSystemAppearanceSettings : TypeAppearanceSettings<VentilationSystem>
    {

        public VentilationSystemAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public VentilationSystemAppearanceSettings(VentilationSystemAppearanceSettings ventilationSystemAppearanceSettings)
            :base(ventilationSystemAppearanceSettings)
        {

        }

        public VentilationSystemAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
