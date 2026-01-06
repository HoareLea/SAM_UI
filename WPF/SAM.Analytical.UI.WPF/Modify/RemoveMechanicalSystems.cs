// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


using SAM.Core;
using SAM.Core.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RemoveMechanicalSystems<T>(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<T> mechanicalSystems) where T: MechanicalSystem
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || mechanicalSystems == null || mechanicalSystems.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            List<SAMObject> sAMObjects = new List<SAMObject>();
            foreach (T mechanicalSystem in mechanicalSystems)
            {
                if (mechanicalSystem == null)
                {
                    continue;
                }

                bool removed = adjacencyCluster.RemoveObject(mechanicalSystem.GetType(), mechanicalSystem.Guid);
                if (removed)
                {
                    sAMObjects.Add(mechanicalSystem);
                }
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new FullModification());//, new AnalyticalModelModification(sAMObjects));
        }
    }
}
