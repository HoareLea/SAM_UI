using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using SAM.Core.Grasshopper.Mollier;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public static partial class Query
    {
        public static List<IMollierPoint> MollierPoints(this IEnumerable<IGH_Param> gH_Params)
        {
            if (gH_Params == null)
            {
                return null;
            }

            List<IMollierPoint> result = new List<IMollierPoint>();
            foreach (IGH_Param gH_Param in gH_Params)
            {
                GooMollierPointParam gooMollierPointParam = gH_Param as GooMollierPointParam;
                if (gooMollierPointParam == null)
                {
                    continue;
                }

                IGH_Structure gH_Structure = gooMollierPointParam.VolatileData;
                foreach (object @object in gH_Structure.AllData(true))
                {
                    IMollierPoint mollierPoint = (@object as GooMollierPoint)?.Value;
                    if (mollierPoint != null)
                    {
                        result.Add(mollierPoint);
                    }
                }
            }

            return result;
        }
    }
}
