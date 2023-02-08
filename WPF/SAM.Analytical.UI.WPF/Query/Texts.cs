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

        public static List<string> Texts(this IEnumerable<InternalCondition> internalConditions, InternalConditionParameter internalConditionParameter)
        {
            if(internalConditions == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(InternalCondition internalCondition in internalConditions)
            {
                if(internalCondition == null)
                {
                    continue;
                }

                if (!internalCondition.TryGetValue(internalConditionParameter, out string value, true))
                {
                    continue;
                }

                result.Add(value);
            }


            return result;
        }

        public static List<string> Texts(this IEnumerable<double> values)
        {
            if(values == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(double value in values)
            {
                if (double.IsNaN(value))
                {
                    result.Add(null);
                }
                else
                {
                    result.Add(value.ToString());
                }

            }

            return result;
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
                    result.Add(null);
                }
                else
                {
                    result.Add(value);
                }


            }

            return result.ToList();
        }
    }
}