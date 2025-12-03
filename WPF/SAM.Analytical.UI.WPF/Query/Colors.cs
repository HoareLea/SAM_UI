using System.Collections.Generic;
using System.Drawing;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static List<Color>? Colors(this IEnumerable<InternalConditionData> internalConditionDatas)
        {
            if(internalConditionDatas == null)
            {
                return null;
            }

            HashSet<Color> hashSet = [];
            foreach(InternalConditionData internalConditionData in internalConditionDatas)
            {
                if(internalConditionData == null)
                {
                    continue;
                }

                if(!internalConditionData.TryGetValue(InternalConditionParameter.Color, out Color color))
                {
                    color = Color.Empty;
                }

                hashSet.Add(color);

            }

            return [.. hashSet];
        }
    }
}