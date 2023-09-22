using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

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