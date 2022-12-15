using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditSpaceZone(this UIAnalyticalModel uIAnalyticalModel, Space space)
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
                foreach (Space space_Selected in spaces_Temp)
                {
                    adjacencyCluster.AddObject(space_Selected);
                    if(zone_Temp != null)
                    {
                        List<Zone> zones_Old = adjacencyCluster.GetZones(space_Selected, zoneCategory);
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
    }
}