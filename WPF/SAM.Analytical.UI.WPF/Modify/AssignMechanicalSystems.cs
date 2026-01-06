// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignMechanicalSystems(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            AssignMechanicalSystemsWindow assignMechanicalSystemsWindow = new AssignMechanicalSystemsWindow();
            assignMechanicalSystemsWindow.AdjacencyCluster = adjacencyCluster;
            assignMechanicalSystemsWindow.Spaces = spaces == null ? null : new List<Space>(spaces);
            assignMechanicalSystemsWindow.MechanicalSystemCategory = Core.Query.Description(MechanicalSystemCategory.Ventilation);

            assignMechanicalSystemsWindow.AdjacencyClusterChanged += delegate(object sender, AdjacencyClusterChangedEventArgs e) 
            {
                AdjacencyCluster adjacencyCluster_New = e.AdjacencyCluster;
                
                uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster_New, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary), new FullModification());
            };

            assignMechanicalSystemsWindow.Show();
        }
    }
}
