using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static List<MollierZone> MollierZones()
        {
            double pressure = 101235;
            List<MollierZone> result = new List<MollierZone>();
            int zonesCount = 2;
            List<MollierPoint> mollierPoints_1 = new List<MollierPoint>();
            mollierPoints_1.Add(new MollierPoint(20, 0.004336446, pressure));
            mollierPoints_1.Add(new MollierPoint(26, 0.006254492, pressure));
            mollierPoints_1.Add(new MollierPoint(26, 0.0115, pressure));
            mollierPoints_1.Add(new MollierPoint(23.11673, 0.0115, pressure));
            mollierPoints_1.Add(new MollierPoint(20, 0.009482655, pressure));
            mollierPoints_1.Add(new MollierPoint(20, 0.004336446, pressure));

            List<MollierPoint> mollierPoints_2 = new List<MollierPoint>();
            mollierPoints_2.Add(new MollierPoint(24, 0.005545342, pressure));
            mollierPoints_2.Add(new MollierPoint(28, 0.007042524, pressure));
            mollierPoints_2.Add(new MollierPoint(28, 0.0115, pressure));
            mollierPoints_2.Add(new MollierPoint(24.11673, 0.0115, pressure));
            mollierPoints_2.Add(new MollierPoint(24, 0.005545342, pressure));

            result.Add(new MollierZone(mollierPoints_1));
            result.Add(new MollierZone(mollierPoints_2));

            return result;
        }
    }
}

