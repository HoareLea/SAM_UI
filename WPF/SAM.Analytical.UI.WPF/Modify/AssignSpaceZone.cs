// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignSpaceZone(this UIAnalyticalModel uIAnalyticalModel, Space space, Zone zone)
        {
            if (space == null)
            {
                return;
            }

            AssignSpaceZone(uIAnalyticalModel, new Space[] { space }, zone);
        }

        public static void AssignSpaceZone(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, Zone zone)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || spaces == null || spaces.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            Zone zone_Temp = adjacencyCluster.GetObject<Zone>(zone.Guid);
            if (zone_Temp == null)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();

            bool updated = false;
            foreach (Space space in spaces)
            {
                if (space == null)
                {
                    continue;
                }

                Space space_Temp = adjacencyCluster.GetObject<Space>(space.Guid);
                if (space_Temp == null)
                {
                    continue;
                }

                zone_Temp.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory);

                if (zone_Temp != null)
                {
                    List<Zone> zones_Old = adjacencyCluster.GetZones(space_Temp, zoneCategory);
                    if (zones_Old != null && zones_Old.Count != 0)
                    {
                        foreach (Zone zone_Old in zones_Old)
                        {
                            adjacencyCluster.RemoveRelation(zone_Old, space_Temp);
                            if (sAMObjects.Find(x => x is Zone && x.Guid == zone_Old.Guid) == null)
                            {
                                sAMObjects.Add(zone_Old);
                            }
                        }
                    }

                    adjacencyCluster.AddRelation(zone_Temp, space_Temp);
                    sAMObjects.Add(zone_Temp);
                    sAMObjects.Add(space_Temp);
                }

                updated = true;
            }

            if (!updated)
            {
                return;
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new FullModification());//, new AnalyticalModelModification(sAMObjects));
        }
    }
}
