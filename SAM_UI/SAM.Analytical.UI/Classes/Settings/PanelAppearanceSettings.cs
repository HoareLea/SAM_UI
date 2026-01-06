// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class PanelAppearanceSettings : TypeAppearanceSettings<Panel>
    {
        public PanelAppearanceSettings(string parameterName)
            :base(parameterName)
        {
        }

        public PanelAppearanceSettings(ConstructionAppearanceSettings constructionAppearanceSettings)
            :base(constructionAppearanceSettings)
        {

        }

        public PanelAppearanceSettings(PanelAppearanceSettings panelAppearanceSettings)
            : base(panelAppearanceSettings)
        {

        }

        public PanelAppearanceSettings(BoundaryTypeAppearanceSettings boundaryTypeAppearanceSettings)
            : base(boundaryTypeAppearanceSettings)
        {

        }

        public PanelAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
