// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Windows;

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

            if (textMap == null || internalConditionLibrary == null)
            {
                MessageBox.Show(
                    "The TM59 InternalCondition TextMap or InternalConditionLibrary resource could not be " +
                    "loaded (SAM_InternalConditionTextMap_TM59.JSON / SAM_InternalConditionLibrary_TM59.JSON). " +
                    "TM59 - Map Internal Conditions cannot run without them.",
                    "TM59 - Map Internal Conditions");
                return;
            }

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

            foreach (Space space in spaces_Temp)
            {
                // TM59Occupancy(InternalCondition) is the documented people-per-condition table
                // (Studio/1-bed=2, 2-bed=3, 3-bed=4, Double=2, Single=1, non-habitable=0) - not the
                // fuzzy name-based TM59Manager.Occupancy, which reads bedroom-count digits out of the
                // condition name and so returns e.g. 1 for every "1 Bed Apt. *" condition regardless
                // of how many people actually occupy the flat.
                //
                // Always set it explicitly (including 0 for non-habitable) - a space remapped from a
                // bedroom to a corridor/bathroom/stairs/cupboard must not retain its old positive
                // Occupancy. UpdateAreaPerPerson then re-derives AreaPerPerson from that Occupancy;
                // for Occupancy == 0 it writes AreaPerPerson == 0 rather than dividing by zero, and it
                // never creates or modifies SpaceParameter.Area itself.
                int occupancy = TM59Manager.TM59Occupancy(space.InternalCondition);
                space.SetValue(SpaceParameter.Occupancy, occupancy);
                space.UpdateAreaPerPerson();

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