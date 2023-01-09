using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditZone(this UIAnalyticalModel uIAnalyticalModel, Zone zone)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            List<Zone> zones = adjacencyCluster.GetZones();
            Zone zone_Temp = zones.Find(x => x.Guid == zone.Guid);

            ZoneWindow zoneWindow = new ZoneWindow(zone_Temp, adjacencyCluster);
            bool? result = zoneWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();

            zone_Temp = zoneWindow.Zone;
            if (zone_Temp != null)
            {
                adjacencyCluster.AddObject(zone_Temp);
                List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(zone_Temp);
                spaces?.ForEach(x => sAMObjects.Add(x));
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}