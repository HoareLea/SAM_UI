using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditMechanicalSystems(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null, MechanicalSystem selectedMechanicalSystem = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            SpaceMechanicalSystemWindow spaceZoneWindow = new SpaceMechanicalSystemWindow(adjacencyCluster, spaces, selectedSpaces, selectedMechanicalSystem);
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

            MechanicalSystem mechanicalSystem_Temp = spaceZoneWindow.SelectedMechanicalSystem;
            if (mechanicalSystem_Temp != null)
            {
                adjacencyCluster.AddObject(mechanicalSystem_Temp);
            }
            

            List<SAMObject> sAMObjects = new List<SAMObject>();

            List<Space> spaces_Temp = spaceZoneWindow.SelectedSpaces;
            if(spaces_Temp != null && spaces_Temp.Count != 0 )
            {
                foreach (Space space in spaces_Temp)
                {
                    adjacencyCluster.AddObject(space);
                    if(mechanicalSystem_Temp != null)
                    {
                        List<MechanicalSystem> mechanicalSystems_Old = adjacencyCluster.MechanicalSystems(space, mechanicalSystem_Temp.MechanicalSystemCategory());
                        if(mechanicalSystems_Old != null && mechanicalSystems_Old.Count != 0)
                        {
                            foreach(MechanicalSystem mechanicalSystem_Old in mechanicalSystems_Old)
                            {
                                adjacencyCluster.RemoveRelation(mechanicalSystem_Old, space);
                                if (sAMObjects.Find(x => x is Zone && x.Guid == mechanicalSystem_Old.Guid) == null)
                                {
                                    sAMObjects.Add(mechanicalSystem_Old);
                                }
                            }
                        }
                        
                        adjacencyCluster.AddRelation(mechanicalSystem_Temp, space);
                        sAMObjects.Add(mechanicalSystem_Temp);
                        sAMObjects.Add(space);
                    }
                }
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }

        public static void EditMechanicalSystems(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, MechanicalSystemCategory mechanicalSystemCategory, IEnumerable<Space> selectedSpaces = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            SpaceMechanicalSystemWindow spaceMechanicalSystemWindow = new SpaceMechanicalSystemWindow(adjacencyCluster, spaces, selectedSpaces);
            spaceMechanicalSystemWindow.MechanicalSystemCategory = Core.Query.Description(mechanicalSystemCategory);
            bool? result = spaceMechanicalSystemWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            adjacencyCluster = spaceMechanicalSystemWindow.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            MechanicalSystem mechanicalSystem_Temp = spaceMechanicalSystemWindow.SelectedMechanicalSystem;
            if (mechanicalSystem_Temp != null)
            {
                adjacencyCluster.AddObject(mechanicalSystem_Temp);
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();

            List<Space> spaces_Temp = spaceMechanicalSystemWindow.SelectedSpaces;
            if (spaces_Temp != null && spaces_Temp.Count != 0)
            {
                foreach (Space space in spaces_Temp)
                {
                    adjacencyCluster.AddObject(space);
                    if (mechanicalSystem_Temp != null)
                    {
                        List<MechanicalSystem> mechanicalSystems_Old = adjacencyCluster.MechanicalSystems(space, mechanicalSystem_Temp.MechanicalSystemCategory());
                        if (mechanicalSystems_Old != null && mechanicalSystems_Old.Count != 0)
                        {
                            foreach (MechanicalSystem mechanicalSystem_Old in mechanicalSystems_Old)
                            {
                                adjacencyCluster.RemoveRelation(mechanicalSystem_Old, space);
                                if (sAMObjects.Find(x => x is Zone && x.Guid == mechanicalSystem_Old.Guid) == null)
                                {
                                    sAMObjects.Add(mechanicalSystem_Old);
                                }
                            }
                        }

                        adjacencyCluster.AddRelation(mechanicalSystem_Temp, space);
                        sAMObjects.Add(mechanicalSystem_Temp);
                        sAMObjects.Add(space);
                    }
                }
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}