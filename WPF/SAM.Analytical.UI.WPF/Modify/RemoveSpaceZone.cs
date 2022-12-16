
namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RemoveSpaceZone(this UIAnalyticalModel uIAnalyticalModel, Space space, Zone zone)
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
            if (zone_Temp == null)
            {
                return;
            }

            adjacencyCluster.RemoveRelation(zone_Temp, space_Temp);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }
    }
}