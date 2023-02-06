using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, SpaceParameter spaceParameter)
        {
            return Texts(internalConditionDatas, (Enum)spaceParameter);
        }

        public static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, InternalConditionParameter internalConditionParameter)
        {
            return Texts(internalConditionDatas, (Enum)internalConditionParameter);
        }

        private static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, Enum @enum)
        {
            if(internalConditionDatas == null)
            {
                return null;
            }

            HashSet<string> result = new HashSet<string>();
            foreach(InternalConditionData internalConditionData in internalConditionDatas)
            {
                if(!internalConditionData.TryGetValue(@enum as dynamic, out string value, true))
                {
                    continue;
                }

                result.Add(value);
            }

            return result.ToList();
        }
    }
}