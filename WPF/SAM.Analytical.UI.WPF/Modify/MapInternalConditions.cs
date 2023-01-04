using SAM.Core;
using SAM.Core.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void MapInternalConditions(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces = null)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            TextMap textMap = SAM.Analytical.Query.DefaultInternalConditionTextMap();

            InternalConditionLibrary internalConditionLibrary = new InternalConditionLibrary("Default");
            analyticalModel?.AdjacencyCluster.GetInternalConditions(false, true)?.ToList()?.ForEach(x => internalConditionLibrary.Add(x));

            List<Space> spaces_Temp = analyticalModel.GetSpaces();
            spaces_Temp?.Sort((x, y) => x.Name.CompareTo(y.Name));

            if(spaces != null)
            {
                List<Space> spaces_Temp_1 = new List<Space>();
                foreach(Space space in spaces)
                {
                    if(space == null)
                    {
                        continue;
                    }

                    Space space_Temp = spaces_Temp.Find(x => x.Guid == space.Guid);
                    if(space_Temp == null)
                    {
                        continue;
                    }

                    spaces_Temp_1.Add(space_Temp);
                }

                spaces_Temp = spaces_Temp_1;
            }

            MapInternalConditionsWindow mapInternalConditionsWindow = new MapInternalConditionsWindow(spaces_Temp, textMap, internalConditionLibrary);
            bool? result = mapInternalConditionsWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            spaces_Temp = mapInternalConditionsWindow.GetSpaces(true);
            if (spaces_Temp == null || spaces_Temp.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            foreach (Space space in spaces_Temp)
            {
                adjacencyCluster.AddObject(space);
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new FullModification());
        }
    }
}