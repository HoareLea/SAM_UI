
using SAM.Core;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RemoveZones(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Zone> zones)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || zones == null || zones.Count() == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            List<SAMObject> sAMObjects = new List<SAMObject>();
            foreach(Zone zone in zones)
            {
                if(zone == null)
                {
                    continue;
                }

                bool removed = adjacencyCluster.RemoveObject(zone.GetType(), zone.Guid);
                if(removed)
                {
                    sAMObjects.Add(zone);
                }
            }

            uIAnalyticalModel.SetJSAMObject( new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}