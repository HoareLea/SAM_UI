using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Drawing;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static Dictionary<Space, Color> Colors(this IEnumerable<Space> spaces, AdjacencyCluster adjacencyCluster = null, ViewSettings viewSettings = null)
        {
            foreach (Space space in spaces)
            {
                if (!Query.TryGetValue(space, adjacencyCluster, viewSettings, out object @object))
                {
                    @object = null;
                }

                //dictionary[space] = @object;
            }


            //TODO: Implement Method

            return null;
        }
    }
}