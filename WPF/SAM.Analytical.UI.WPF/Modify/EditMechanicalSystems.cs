// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary), new AnalyticalModelModification(sAMObjects));
        }

        public static void EditMechanicalSystems(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, MechanicalSystemCategory mechanicalSystemCategory, IEnumerable<Space> selectedSpaces = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<SAMObject> sAMObjects = EditMechanicalSystems(adjacencyCluster, spaces, mechanicalSystemCategory, selectedSpaces);
            if(sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary), new AnalyticalModelModification(sAMObjects));
        }

        public static List<SAMObject> EditMechanicalSystems(this AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, MechanicalSystemCategory mechanicalSystemCategory, IEnumerable<Space> selectedSpaces = null)
        {
            if(adjacencyCluster == null)
            {
                return null;
            }

            SpaceMechanicalSystemWindow spaceMechanicalSystemWindow = new SpaceMechanicalSystemWindow(adjacencyCluster, spaces, selectedSpaces);
            spaceMechanicalSystemWindow.MechanicalSystemCategory = Core.Query.Description(mechanicalSystemCategory);
            bool? dialogResult = spaceMechanicalSystemWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return null;
            }

            adjacencyCluster = spaceMechanicalSystemWindow.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return null;
            }

            MechanicalSystem mechanicalSystem_Temp = spaceMechanicalSystemWindow.SelectedMechanicalSystem;
            if (mechanicalSystem_Temp != null)
            {
                adjacencyCluster.AddObject(mechanicalSystem_Temp);
            }

            List<SAMObject> result = new List<SAMObject>();

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
                                if (result.Find(x => x is Zone && x.Guid == mechanicalSystem_Old.Guid) == null)
                                {
                                    result.Add(mechanicalSystem_Old);
                                }
                            }
                        }

                        adjacencyCluster.AddRelation(mechanicalSystem_Temp, space);
                        result.Add(mechanicalSystem_Temp);
                        result.Add(space);
                    }
                }
            }

            return result;
        }
    }
}
