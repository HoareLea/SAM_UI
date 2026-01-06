// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditOpeningProperties(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Aperture> apertures)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            ApertureWindow apertureWindow = new ApertureWindow(apertures);
            bool? result = apertureWindow.ShowDialog();
            if(result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();

            List<Aperture> apertures_Temp = apertureWindow.Apertures;
            if (apertures_Temp != null && apertures_Temp.Count != 0 )
            {
                foreach (Aperture aperture in apertures_Temp)
                {
                    Panel panel = adjacencyCluster.GetPanel(aperture);
                    if(panel == null)
                    {
                        continue;
                    }

                    sAMObjects.Add(aperture);

                    panel.RemoveAperture(aperture.Guid);
                    panel.AddAperture(aperture);

                    adjacencyCluster.AddObject(panel);

                    sAMObjects.Add(panel);
                }
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}
