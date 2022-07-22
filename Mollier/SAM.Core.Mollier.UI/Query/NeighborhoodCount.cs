using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Dictionary<MollierPoint, int> NeighborhoodCount(this IEnumerable<MollierPoint> mollierPoints, out int maxCount)
        {
            maxCount = 2;
            Dictionary<MollierPoint, int> result = new Dictionary<MollierPoint, int>();
            return result;
        }
    }
}
