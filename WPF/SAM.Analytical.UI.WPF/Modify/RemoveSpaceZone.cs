// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


using SAM.Core;
using System.Collections.Generic;

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

            List<SAMObject> sAMObjects = new List<SAMObject>();

            adjacencyCluster.RemoveRelation(zone_Temp, space_Temp);
            sAMObjects.Add(zone_Temp);
            sAMObjects.Add(space_Temp);

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}
