using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Mollier;
using SAM.Geometry.Grasshopper.Mollier;
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
                if (!(gH_Param is GooMollierProcessParam) && !(gH_Param is GooMollierObjectParam))
                {
                    continue;
                }

                IGH_Structure gH_Structure = gH_Param.VolatileData;
                foreach (object @object in gH_Structure.AllData(true))
                {
                    object object_Temp = @object is IGH_Goo ? (@object as dynamic)?.Value : @object;
                    if(object_Temp == null)
                    {
                        continue;
                    }

                    if(object_Temp is T)
                    {
                        result.Add((T)object_Temp);
                    }
                    else if(object_Temp is MollierGroup)
                    {
                        MollierGroup mollierGroup = object_Temp as MollierGroup;
                        List<T> values = mollierGroup.GetObjects<T>(true);
                        if(values != null)
                        {
                            result.AddRange(values);
                        }
                    }
                }
            }

            return result;
        }
    }
}
