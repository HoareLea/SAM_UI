// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class PartFSpaceDataAppearanceSettings : TypeAppearanceSettings<PartFSpaceData>
    {

        public PartFSpaceDataAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public PartFSpaceDataAppearanceSettings(PartFSpaceDataAppearanceSettings partFSpaceDataAppearanceSettings)
            :base(partFSpaceDataAppearanceSettings)
        {

        }

        public PartFSpaceDataAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {
        }
    }
}
