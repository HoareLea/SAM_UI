// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class OpeningPropertiesAppearanceSettings : TypeAppearanceSettings<IOpeningProperties>
    {
        public OpeningPropertiesAppearanceSettings(JsonObject jObject) 
            : base(jObject)
        {
        }

        public OpeningPropertiesAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public OpeningPropertiesAppearanceSettings(OpeningPropertiesAppearanceSettings openingPropertiesAppearanceSettings)
            :base(openingPropertiesAppearanceSettings)
        {

        }
    }
}
