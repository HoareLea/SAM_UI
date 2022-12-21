using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignSpaceZone(this UIAnalyticalModel uIAnalyticalModel, Space space, Zone zone)
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

            Zone zone_Temp = adjacencyCluster.GetObject<Zone>(zone.Guid);
            if(zone_Temp == null)
            {
                return;
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
                    }
                }

                adjacencyCluster.AddRelation(zone_Temp, space_Temp);
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }
    }
}