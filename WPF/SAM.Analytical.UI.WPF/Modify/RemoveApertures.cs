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
        public static void RemoveApertures(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Aperture> apertures)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || apertures == null || apertures.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<SAMObject> sAMObjects = [];
            foreach(Aperture aperture in apertures)
            {
                if(aperture == null)
                {
                    continue;
                }

                adjacencyCluster.GetAperture(aperture.Guid, out Panel panel);
                if(panel is null)
                {
                    continue;
                }

                if(!panel.RemoveAperture(aperture.Guid))
                {
                    continue;
                }

                adjacencyCluster.AddObject(panel);
                sAMObjects.Add(panel);
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new FullModification());//, new AnalyticalModelModification(sAMObjects));
        }
    }
}