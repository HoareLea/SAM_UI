using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.UI
{
    public static partial class Create
    {
        public static LogicalFilter LogicalFilter(this IEnumerable<IUIFilter> uIFilters, FilterLogicalOperator filterLogicalOperator)
        {
            return new LogicalFilter(filterLogicalOperator, uIFilters?.ToList().ConvertAll(x => (x as dynamic).Filter as IFilter));
        }
    }
}
