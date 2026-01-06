// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class ZoneAppearanceSettings : TypeAppearanceSettings<Zone>
    {
        public string ZoneCategory { get; set; }

        public ZoneAppearanceSettings(string zoneCategory)
            :base("Color")
        {
            ZoneCategory = zoneCategory;
        }

        public ZoneAppearanceSettings(string parameterName, string zoneCategory)
            : base(parameterName)
        {
            ZoneCategory = zoneCategory;
        }

        public ZoneAppearanceSettings(ZoneAppearanceSettings zoneAppearanceSettings)
            : base(zoneAppearanceSettings)
        {
            if(zoneAppearanceSettings != null)
            {
                ZoneCategory = zoneAppearanceSettings.ZoneCategory;
            }
        }

        public ZoneAppearanceSettings(JObject jObject)
            : base(jObject)
        {

        }
    }
}
