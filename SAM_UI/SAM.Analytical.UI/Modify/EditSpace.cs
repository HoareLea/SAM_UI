// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Windows.Forms;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditSpace(this UIAnalyticalModel uIAnalyticalModel, Space space, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;


            Space space_Temp = null;
            using (SpaceForm spaceForm = new SpaceForm(space, analyticalModel.ProfileLibrary, analyticalModel.AdjacencyCluster, Core.Query.Enums(typeof(Space))))
            {
                if(spaceForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                adjacencyCluster = spaceForm.AdjacencyCluster;
                profileLibrary = spaceForm.ProfileLibrary;
                space_Temp = spaceForm.Space;
            }

            if(space_Temp != null)
            {
                adjacencyCluster?.AddObject(space_Temp);
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }
    }
}
