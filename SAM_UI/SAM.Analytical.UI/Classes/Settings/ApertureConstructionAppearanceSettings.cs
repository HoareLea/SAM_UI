// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ApertureConstructionAppearanceSettings : TypeAppearanceSettings<Aperture>
    {
        public ApertureConstructionAppearanceSettings(string parameterName)
            :base(parameterName)
        {
        }

        public ApertureConstructionAppearanceSettings(ApertureConstructionAppearanceSettings apertureConstructionAppearanceSettings)
            :base(apertureConstructionAppearanceSettings)
        {

        }

        public ApertureConstructionAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
