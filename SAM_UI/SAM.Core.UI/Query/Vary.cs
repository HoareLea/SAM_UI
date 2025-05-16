using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.UI
{
    public static partial class Query
    {

        public static bool Vary<T>(this IEnumerable<T> values)
        {
            if(values == null || values.Count() == 0)
            {
                return false;
            }

            T value = values.ElementAt(0);
            foreach(T value_Temp in values)
            {
                if(string.IsNullOrEmpty(value?.ToString()) && string.IsNullOrEmpty(value_Temp?.ToString()))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(value?.ToString()) || string.IsNullOrEmpty(value_Temp?.ToString()))
                {
                    return true;
                }

                if(!value_Temp.ToString().Equals(value.ToString()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}