using System.Collections.Generic;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Simplify all IUIFilters to base IFilters (example: UILogicalFilter -> LogicalFilter, UIRelationFilter -> IRelationFilter etc.)
        /// </summary>
        /// <param name="filter">Given filter</param>
        /// <returns>Simplified IFilter</returns>
        public static IFilter Reduce(this IFilter filter)
        {
            if (filter == null)
            {
                return null;
            }

            IFilter result = filter;
            while (result is IUIFilter)
            {
                result = ((IUIFilter)filter as dynamic).Filter;
            }

            if(result is IRelationFilter)
            {
                IRelationFilter relationFilter = (IRelationFilter)result;
                relationFilter.Filter = Reduce(relationFilter.Filter);
                return relationFilter;
            }

            if(result is LogicalFilter)
            {
                LogicalFilter logicalFilter = (LogicalFilter)result;
                if (logicalFilter == null)
                {
                    return result;
                }

                List<IFilter> filters = logicalFilter.Filters;
                if (filters == null || filters.Count == 0)
                {
                    return result;
                }

                for (int i = 0; i < filters.Count; i++)
                {
                    filters[i] = Reduce(filters[i]);
                }

                result = new LogicalFilter(logicalFilter) { Filters = filters };
                return result;
            }


            return result;
        }
    }
}