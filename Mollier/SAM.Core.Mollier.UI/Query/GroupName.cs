using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static string GroupName(IMollierObject mollierObject, IEnumerable<MollierGroup> mollierGroups)
        {
            string result = string.Empty;
            if (mollierObject == null || mollierGroups == null)
            {
                return result;
            }

            if (mollierObject is UIMollierPoint)
            {
                foreach (MollierGroup mollierGroup in mollierGroups)
                {
                    if (mollierGroup.GetObjects<UIMollierPoint>().Find(x => x == (UIMollierPoint)mollierObject) != null)
                    {
                        result = mollierGroup.Name;
                        break;
                    }
                }
            }
            else if (mollierObject is UIMollierProcess)
            {
                foreach (MollierGroup mollierGroup in mollierGroups)
                {
                    if (mollierGroup.GetObjects<UIMollierProcess>().Find(x => x == (UIMollierProcess)mollierObject) != null)
                    {
                        result = mollierGroup.Name;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
