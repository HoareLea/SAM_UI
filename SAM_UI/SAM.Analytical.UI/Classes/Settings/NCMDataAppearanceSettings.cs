// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class NCMDataAppearanceSettings : TypeAppearanceSettings<NCMData>
    {

        public NCMDataAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public NCMDataAppearanceSettings(NCMDataAppearanceSettings nCMDataAppearanceSettings)
            :base(nCMDataAppearanceSettings)
        {

        }

        public NCMDataAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
