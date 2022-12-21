namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignSpaceInternalCondition(this UIAnalyticalModel uIAnalyticalModel, Space space, InternalCondition internalCondition)
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

            space_Temp.InternalCondition = internalCondition;
            adjacencyCluster.AddObject(space_Temp);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }
    }
}