// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static System.Windows.Media.Color? Color(this Space space, AdjacencyCluster adjacencyCluster = null, ViewSettings viewSettings = null)
        {
            if(space == null)
            {
                return null;
            }

            System.Drawing.Color color_Temp;

            if(adjacencyCluster == null && viewSettings == null)
            {
                if (space.TryGetValue(SpaceParameter.Color, out color_Temp))
                {
                    return color_Temp.ToMedia();
                }

                return System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGray).ToMedia();
            }

            SpaceAppearanceSettings spaceAppearanceSettings = viewSettings?.GetValueAppearanceSettings<SpaceAppearanceSettings>()?.FirstOrDefault();
            if (spaceAppearanceSettings != null)
            {
                IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
                if (appearanceSettings is ZoneAppearanceSettings)
                {
                    string zoneCategory = ((ZoneAppearanceSettings)appearanceSettings).ZoneCategory;
                    if (!string.IsNullOrWhiteSpace(zoneCategory))
                    {
                        if (adjacencyCluster != null)
                        {
                            List<Zone> zones = adjacencyCluster.GetZones(space, zoneCategory);
                            if (zones != null && zones.Count != 0)
                            {
                                if (zones.FirstOrDefault().TryGetValue(ZoneParameter.Color, out SAMColor sAMColor) && sAMColor != null)
                                {
                                    return sAMColor.ToColor().ToMedia();
                                }
                            }
                        }
                    }
                }
            }

            if (space.TryGetValue(SpaceParameter.Color, out color_Temp))
            {
                return color_Temp.ToMedia();
            }

            return System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGray).ToMedia();
        }
    }
}
