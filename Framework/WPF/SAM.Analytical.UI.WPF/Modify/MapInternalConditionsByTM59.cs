using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void MapInternalConditionsByTM59(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces = null)
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

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            TextMap textMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();

            InternalConditionLibrary internalConditionLibrary = Analytical.Query.DefaultInternalConditionLibrary_TM59();

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

            MapTM59InternalConditionsWindow mapTM59InternalConditionsWindow = new MapTM59InternalConditionsWindow(spaces_Temp, adjacencyCluster, textMap, internalConditionLibrary);
            bool? result = mapTM59InternalConditionsWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            spaces_Temp = mapTM59InternalConditionsWindow.GetSpaces(true);
            if (spaces_Temp == null || spaces_Temp.Count == 0)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();

            TM59Manager tM59Manager = new TM59Manager(textMap);

            foreach (Space space in spaces_Temp)
            {
                int occupancy = tM59Manager.Occupancy(space.InternalCondition);
                if(occupancy > 0)
                {
                    space.SetValue(SpaceParameter.Occupancy, occupancy);
                }

                adjacencyCluster.AddObject(space);
                sAMObjects.Add(space);
            }

            List<InternalCondition> internalConditions = internalConditionLibrary.GetInternalConditions();
            if(internalConditions != null)
            {
                foreach (InternalCondition internalCondition in internalConditions)
                {
                    if(!adjacencyCluster.Contains<InternalCondition>(internalCondition.Guid))
                    {
                        if(adjacencyCluster.AddObject(internalCondition))
                        {
                            sAMObjects.Add(internalCondition);
                        }

                    }
                }
            }

            List<Profile> profiles = Analytical.Query.DefaultProfileLibrary_TM59()?.GetProfiles();
            if(profiles != null)
            {
                foreach(Profile profile in profiles)
                {
                    if(analyticalModel.AddProfile(profile, false))
                    {
                        sAMObjects.Add(profile);
                    }
                }
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new AnalyticalModelModification(sAMObjects));
        }
    }
}