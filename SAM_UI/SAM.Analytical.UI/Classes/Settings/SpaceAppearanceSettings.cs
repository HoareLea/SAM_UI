// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Core.UI;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class SpaceAppearanceSettings : TypeAppearanceSettings<Space>
    {
        public SpaceAppearanceSettings(string parameterName)
            :base(parameterName)
        {

        }

        public SpaceAppearanceSettings(SpaceAppearanceSettings spaceAppearanceSettings)
            : base(spaceAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(ZoneAppearanceSettings zoneAppearanceSettings)
            :base(zoneAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(InternalConditionAppearanceSettings internalConditionAppearanceSettings)
            :base(internalConditionAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(VentilationSystemAppearanceSettings ventilationSystemAppearanceSettings)
            : base(ventilationSystemAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(HeatingSystemAppearanceSettings heatingSystemAppearanceSettings)
            : base(heatingSystemAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(CoolingSystemAppearanceSettings coolingSystemAppearanceSettings)
            : base(coolingSystemAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(NCMDataAppearanceSettings nCMDataAppearanceSettings)
            : base(nCMDataAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(ComplexReferenceAppearanceSettings complexReferenceAppearanceSettings)
            : base(complexReferenceAppearanceSettings)
        {

        }

        public SpaceAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
