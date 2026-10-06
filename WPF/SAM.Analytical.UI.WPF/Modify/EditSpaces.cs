// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditSpaces(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            SpacesWindow spacesWindow = new SpacesWindow(adjacencyCluster.GetSpaces(), analyticalModel);
            bool? dialogResult = owner == null ? spacesWindow.ShowDialog() : spacesWindow.ShowDialog(owner);
            if (dialogResult != true)
            {
                return;
            }

            // Read back from the window rather than the pre-dialog capture this used to take: editing
            // a space's internal condition can update the profile library or adjacency cluster (e.g.
            // via the "select from library" picker), and SpacesWindow tracks that internally. Using the
            // pre-dialog values here silently discarded those edits.
            adjacencyCluster = spacesWindow.AdjacencyCluster ?? adjacencyCluster;

            IEnumerable<Space> spaces = spacesWindow.Spaces;
            if (spaces != null && spaces.Count() != 0)
            {
                foreach (Space space in spaces)
                {
                    adjacencyCluster.AddObject(space);
                }
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, spacesWindow.ProfileLibrary ?? analyticalModel.ProfileLibrary);
        }
    }
}
