using SAM.Core;
using System.Collections.Generic;
using System.Linq;

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

        public static void AssignSpaceInternalCondition(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || spaces == null || spaces.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            List<Space> spaces_Temp = new List<Space>();
            List<InternalCondition> internalConditions_Temp = new List<InternalCondition>();

            IEnumerable<InternalCondition> internalConditions = adjacencyCluster.GetInternalConditions(false, true);

            foreach(Space space in spaces)
            {
                Space space_Temp = adjacencyCluster.GetObject<Space>(space.Guid);
                if (space_Temp == null)
                {
                    continue;
                }
                spaces_Temp.Add(space_Temp);

                InternalCondition internalCondition_Temp = space_Temp.InternalCondition;
                if(internalCondition_Temp != null)
                {
                    internalConditions_Temp.Add(internalCondition_Temp);
                }
            }

            if(spaces_Temp == null || spaces_Temp.Count == 0)
            {
                return;
            }

            InternalCondition internalCondition = null;
            using (Core.Windows.Forms.SearchForm<InternalCondition> searchForm = new Core.Windows.Forms.SearchForm<InternalCondition>("Select Internal Condition", internalConditions, (InternalCondition x) => x.Name, false))
            {
                searchForm.SelectionMode = System.Windows.Forms.SelectionMode.One;
                if (internalConditions_Temp != null && internalConditions_Temp.Count != 0 && internalConditions_Temp.TrueForAll(x => x.Name == internalConditions_Temp[0].Name))
                {
                    searchForm.SearchText = internalConditions_Temp[0].Name;
                }

                if(searchForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                internalCondition = searchForm.SelectedItems?.FirstOrDefault();
            }

            if(internalCondition == null)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();
            foreach(Space space_Temp in spaces_Temp)
            {
                space_Temp.InternalCondition = internalCondition;
                adjacencyCluster.AddObject(space_Temp);
                sAMObjects.Add(space_Temp);
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}