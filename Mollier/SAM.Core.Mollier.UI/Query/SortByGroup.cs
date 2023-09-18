using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static List<IMollierProcess> SortByGroup(this IEnumerable<IMollierProcess> mollierProcesses)
        {
            List<IMollierGroup> mollierGroups = Mollier.Query.Group(mollierProcesses);

            List<IMollierProcess> result = new List<IMollierProcess>();
            mollierGroups.ForEach(x =>
            {
                result.AddRange(((MollierGroup)x).GetObjects<IMollierProcess>());
            });

            return result;
        }
    }
}
