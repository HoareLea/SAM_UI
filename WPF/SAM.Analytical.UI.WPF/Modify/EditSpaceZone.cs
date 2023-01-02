using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditSpaceZone(this UIAnalyticalModel uIAnalyticalModel, Space space, string zoneCategory = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || space == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            Space space_Temp = adjacencyCluster.GetObject<Space>(space.Guid);
            if(space_Temp == null)
            {
                return;
            }

            List<Space> spaces = new List<Space>() { space_Temp };
            
            SpaceZoneWindow spaceZoneWindow = new SpaceZoneWindow(adjacencyCluster, spaces, spaces);
            spaceZoneWindow.ZoneCategory = zoneCategory;
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

            zone_Temp.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory_Temp);

            List<Space> spaces_Temp = spaceZoneWindow.SelectedSpaces;
            if(spaces_Temp != null && spaces_Temp.Count != 0 )
            {
                foreach (Space space_Selected in spaces_Temp)
                {
                    adjacencyCluster.AddObject(space_Selected);
                    if(zone_Temp != null)
                    {
                        List<Zone> zones_Old = adjacencyCluster.GetZones(space_Selected, zoneCategory_Temp);
                        if(zones_Old != null && zones_Old.Count != 0)
                        {
                            foreach(Zone zone_Old in zones_Old)
                            {
                                adjacencyCluster.RemoveRelation(zone_Old, space_Selected);
                            }
                        }
                        
                        adjacencyCluster.AddRelation(zone_Temp, space_Selected);
                    }
                }
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }

        public static void EditSpaceZone(this UIAnalyticalModel uIAnalyticalModel, Zone zone)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            List<Zone> zones = adjacencyCluster.GetZones();
            Zone zone_Temp = zones.Find(x => x.Guid == zone.Guid);

            SpaceZoneWindow spaceZoneWindow = new SpaceZoneWindow(adjacencyCluster, adjacencyCluster.GetSpaces(), adjacencyCluster.GetRelatedObjects<Space>(zone_Temp), zone_Temp);
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

            zone_Temp = spaceZoneWindow.SelectedZone;
            if (zone_Temp != null)
            {
                adjacencyCluster.AddObject(zone_Temp);
            }

            zone_Temp.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory);

            List<Space> spaces_Temp = spaceZoneWindow.SelectedSpaces;
            if (spaces_Temp != null && spaces_Temp.Count != 0)
            {
                foreach (Space space in spaces_Temp)
                {
                    adjacencyCluster.AddObject(space);
                    if (zone_Temp != null)
                    {
                        List<Zone> zones_Old = adjacencyCluster.GetZones(space, zoneCategory);
                        if (zones_Old != null && zones_Old.Count != 0)
                        {
                            foreach (Zone zone_Old in zones_Old)
                            {
                                adjacencyCluster.RemoveRelation(zone_Old, space);
                            }
                        }

                        adjacencyCluster.AddRelation(zone_Temp, space);
                    }
                }
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }

        public static void EditSpaceZone(this UIAnalyticalModel uIAnalyticalModel, string zoneCategory)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<Zone> zones = adjacencyCluster.GetZones();
            if (zones == null || zones.Count == 0)
            {
                return;
            }

            foreach (Zone zone in zones)
            {
                if (zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory_Temp) && zoneCategory == zoneCategory_Temp)
                {
                    EditSpaceZone(uIAnalyticalModel, zone);
                    return;
                }
            }
        }
    }
}