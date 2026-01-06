// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RemoveAirMovementObjects(this UIAnalyticalModel uIAnalyticalModel)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            adjacencyCluster = new AdjacencyCluster(adjacencyCluster);
            List<System.Guid> guids = adjacencyCluster.RemoveAirMovementObjects<IAirMovementObject>();
            if(guids == null || guids.Count == 0)
            {
                MessageBox.Show("Nothing to be removed.");
                return;
            }

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
            
            MessageBox.Show("Objects has been removed.");

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
        }
    }
}
