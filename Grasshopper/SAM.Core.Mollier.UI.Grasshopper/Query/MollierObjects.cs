using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Mollier;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public static partial class Query
    {
        public static List<IMollierObject> MollierObjects(this IEnumerable<IGH_Param> gH_Params)
        {
            if (gH_Params == null)
            {
                return null;
            }

            List<IMollierObject> result = new List<IMollierObject>();
            foreach (IGH_Param gH_Param in gH_Params)
            {
                //GooMollierObjectParam gooMollierObjectParam = gH_Param as GooMollierObjectParam;
                //if (gooMollierObjectParam == null)
                //{
                //    continue;
                //}

                IGH_Structure gH_Structure = gH_Param.VolatileData;
                foreach (object @object in gH_Structure.AllData(true))
                {
                    IMollierObject mollierObject = @object is IGH_Goo ? (@object as dynamic).Value : @object as IMollierObject;
                    if (mollierObject != null)
                    {
                        result.Add(mollierObject);
                    }
                }
            }

            return result;
        }
    }
}
