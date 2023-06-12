using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using SAM.Core.Grasshopper.Mollier;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public static partial class Query
    {
        public static List<T> MollierProcesses<T>(this IEnumerable<IGH_Param> gH_Params) where T: IMollierProcess
        {
            if (gH_Params == null)
            {
                return null;
            }

            List<T> result = new List<T>();
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
                    IMollierProcess mollierProcess_Temp = (@object as GooMollierProcess)?.Value;
                    if (mollierProcess_Temp is T)
                    {
                        result.Add((T)mollierProcess_Temp);
                    }
                }
            }

            return result;
        }
    }
}
