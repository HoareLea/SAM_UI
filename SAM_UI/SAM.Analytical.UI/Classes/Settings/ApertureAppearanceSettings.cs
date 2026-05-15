// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ApertureAppearanceSettings : TypeAppearanceSettings<Aperture>
    {
        public ApertureAppearanceSettings(JsonObject jObject) 
            : base(jObject)
        {
        }

        public ApertureAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public ApertureAppearanceSettings(ApertureAppearanceSettings apertureAppearanceSettings)
            :base(apertureAppearanceSettings)
        {

        }

        public ApertureAppearanceSettings(ApertureConstructionAppearanceSettings apertureConstructionAppearanceSettings)
            : base(apertureConstructionAppearanceSettings as ValueAppearanceSettings)
        {

        }

        public ApertureAppearanceSettings(PanelAppearanceSettings panelConstructionAppearanceSettings)
            : base(panelConstructionAppearanceSettings)
        {

        }

        public ApertureAppearanceSettings(OpeningPropertiesAppearanceSettings openingPropertiesAppearanceSettings)
            : base(openingPropertiesAppearanceSettings)
        {

        }
    }
}
