// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditZones(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null, Zone selectedZone = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            SpaceZoneWindow spaceZoneWindow = new SpaceZoneWindow(adjacencyCluster, spaces, selectedSpaces, selectedZone);
            bool? result = spaceZoneWindow.ShowDialog();
            if(result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            adjacencyCluster = spaceZoneWindow.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            Zone zone_Temp = spaceZoneWindow.SelectedZone;
            if (zone_Temp != null)
            {
                adjacencyCluster.AddObject(zone_Temp);
            }

            zone_Temp.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory);

            List<SAMObject> sAMObjects = new List<SAMObject>();

            List<Space> spaces_Temp = spaceZoneWindow.SelectedSpaces;
            if(spaces_Temp != null && spaces_Temp.Count != 0 )
            {
                foreach (Space space in spaces_Temp)
                {
                    adjacencyCluster.AddObject(space);
                    if(zone_Temp != null)
                    {
                        List<Zone> zones_Old = adjacencyCluster.GetZones(space, zoneCategory);
                        if(zones_Old != null && zones_Old.Count != 0)
                        {
                            foreach(Zone zone_Old in zones_Old)
                            {
                                adjacencyCluster.RemoveRelation(zone_Old, space);
                                if (sAMObjects.Find(x => x is Zone && x.Guid == zone_Old.Guid) == null)
                                {
                                    sAMObjects.Add(zone_Old);
                                }
                            }
                        }
                        
                        adjacencyCluster.AddRelation(zone_Temp, space);
                        sAMObjects.Add(zone_Temp);
                        sAMObjects.Add(space);
                    }
                }
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }

        public static void EditZones(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, string zoneCategory, IEnumerable<Space> selectedSpaces = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            SpaceZoneWindow spaceZoneWindow = new SpaceZoneWindow(adjacencyCluster, spaces, selectedSpaces);
            spaceZoneWindow.ZoneCategory = zoneCategory;
            bool? result = spaceZoneWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            adjacencyCluster = spaceZoneWindow.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            Zone zone_Temp = spaceZoneWindow.SelectedZone;
            if (zone_Temp != null)
            {
                adjacencyCluster.AddObject(zone_Temp);
            }

            zone_Temp.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory_Temp);

            List<SAMObject> sAMObjects = new List<SAMObject>();

            List<Space> spaces_Temp = spaceZoneWindow.SelectedSpaces;
            if (spaces_Temp != null && spaces_Temp.Count != 0)
            {
                foreach (Space space in spaces_Temp)
                {
                    adjacencyCluster.AddObject(space);
                    if (zone_Temp != null)
                    {
                        List<Zone> zones_Old = adjacencyCluster.GetZones(space, zoneCategory_Temp);
                        if (zones_Old != null && zones_Old.Count != 0)
                        {
                            foreach (Zone zone_Old in zones_Old)
                            {
                                adjacencyCluster.RemoveRelation(zone_Old, space);
                                if (sAMObjects.Find(x => x is Zone && x.Guid == zone_Old.Guid) == null)
                                {
                                    sAMObjects.Add(zone_Old);
                                }
                            }
                        }

                        adjacencyCluster.AddRelation(zone_Temp, space);
                        sAMObjects.Add(zone_Temp);
                        sAMObjects.Add(space);
                    }
                }
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}
