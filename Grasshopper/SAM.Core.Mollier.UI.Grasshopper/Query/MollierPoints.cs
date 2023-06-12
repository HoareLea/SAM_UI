using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using SAM.Core.Grasshopper.Mollier;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public static partial class Query
    {
        public static List<MollierPoint> MollierPoints(this IEnumerable<IGH_Param> gH_Params)
        {
            if (gH_Params == null)
            {
                return null;
            }

            List<MollierPoint> result = new List<MollierPoint>();
            foreach (IGH_Param gH_Param in gH_Params)
            {
                GooMollierProcessParam gooMollierProcessParam = gH_Param as GooMollierProcessParam;
                if (gooMollierProcessParam == null)
                {
                    continue;
                }

                IGH_Structure gH_Structure = gooMollierProcessParam.VolatileData;
                foreach (object @object in gH_Structure.AllData(true))
                {
                    MollierPoint mollierPoint = (@object as GooMollierPoint)?.Value;
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
