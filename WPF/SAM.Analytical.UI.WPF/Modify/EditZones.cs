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
                            }
                        }
                        
                        adjacencyCluster.AddRelation(zone_Temp, space);
                    }
                }
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
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
                            }
                        }

                        adjacencyCluster.AddRelation(zone_Temp, space);
                    }
                }
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }
    }
}