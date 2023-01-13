using SAM.Core;
using System;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Func<Space, InternalCondition> DefaultMapFunc(InternalConditionLibrary internalConditionLibrary, TextMap textMap)
        {
            return new Func<Space, InternalCondition>( x => 
            {
                if (Analytical.Query.TryGetInternalCondition(x, internalConditionLibrary, textMap, out InternalCondition internalCondition))
                {
                    return internalCondition;
                }

                return null;
            });
        }
    }
}
