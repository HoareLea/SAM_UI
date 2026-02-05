// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RemovePanels(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Panel> panels)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || panels == null || panels.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<SAMObject> sAMObjects = [];
            foreach(Panel panel in panels)
            {
                if(panel == null)
                {
                    continue;
                }

                List<Space> spaces = adjacencyCluster.GetSpaces(panel);
                if(spaces == null || spaces.Count == 0)
                {
                    adjacencyCluster.RemoveObject(panel);
                    sAMObjects.Add(panel);
                    continue;
                }

                Panel panel_New = Analytical.Create.Panel(panel, PanelType.Air);
                adjacencyCluster.AddObject(panel_New);
                sAMObjects.Add(panel_New);

            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new FullModification());//, new AnalyticalModelModification(sAMObjects));
        }
    }
}