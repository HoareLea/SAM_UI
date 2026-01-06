// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static List<string> ZoneCategories(this AdjacencyCluster adjacencyCluster)
        {
            HashSet<string> zoneCategories = new HashSet<string>();

            foreach(ZoneType zoneType in Enum.GetValues(typeof(ZoneType)))
            {
                if(zoneType == ZoneType.Undefined)
                {
                    continue;
                }

                zoneCategories.Add(Core.Query.Description(zoneType));
            }

            List<string> result = null;

            if (adjacencyCluster == null)
            {
                result = new List<string>(zoneCategories);
                result.Sort();
                return result;
            }

            List<Zone> zones = adjacencyCluster.GetZones();
            if(zones != null && zones.Count != 0)
            {
                foreach(Zone zone in zones)
                {
                    if(zone == null)
                    {
                        continue;
                    }

                    if(!zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrWhiteSpace(zoneCategory))
                    {
                        continue;
                    }

                    zoneCategories.Add(zoneCategory);
                }
            }

            result = new List<string>(zoneCategories);
            result.Sort();

            return result;
        }
    }
}
