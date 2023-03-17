using System;
using System.Collections.Generic;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static IFilter Transform(this IFilter filter)
        {
            IFilter result = filter?.Reduce();
            if(result == null)
            {
                return null;
            }

            if(filter is IRelationFilter)
            {
                IRelationFilter relationFilter = filter.Clone() as IRelationFilter;
                relationFilter.Filter = Transform(relationFilter.Filter);

                if(relationFilter is IMultiRelationFilter)
                {
                    IMultiRelationFilter multiRelationFilter = (IMultiRelationFilter)relationFilter;
                    IFilter baseFilter = multiRelationFilter.BaseFilter();
                    if(baseFilter != null && baseFilter.Inverted)
                    {
                        multiRelationFilter.FilterLogicalOperator = FilterLogicalOperator.And;
                    }
                }

                return relationFilter;
            }

            LogicalFilter logicalFilter = result as LogicalFilter;
            if(logicalFilter == null)
            {
                return result;
            }

            List<IFilter> filters = logicalFilter.Filters;
            if(filters == null || filters.Count == 0)
            {
                return result;
            }

            Dictionary<System.Type, List<IRelationFilter>> dictionary = new Dictionary<System.Type, List<IRelationFilter>>();
            for (int i = filters.Count - 1; i >= 0; i--)
            {
                IFilter filter_Temp = Transform(filters[i]);
                if (filter_Temp == null)
                {
                    filters.RemoveAt(i);
                    continue;
                }

                IRelationFilter relationFilter = filter_Temp as IRelationFilter;
                if (relationFilter == null)
                {
                    continue;
                }

                System.Type type = relationFilter.GetType();
                if (type == null)
                {
                    filters.RemoveAt(i);
                    continue;
                }

                if (!dictionary.TryGetValue(type, out List<IRelationFilter> relationFilters) || relationFilters == null)
                {
                    relationFilters = new List<IRelationFilter>();
                    dictionary[type] = relationFilters;
                }

                relationFilters.Add(relationFilter);
                filters.RemoveAt(i);
            }

            foreach (KeyValuePair<Type, List<IRelationFilter>> keyValuePair in dictionary)
            {
                if (keyValuePair.Key == null || keyValuePair.Value == null || keyValuePair.Value.Count == 0)
                {
                    continue;
                }

                List<IRelationFilter> relationFilters = keyValuePair.Value;
                if (relationFilters.Count == 1)
                {
                    filters.Add(relationFilters[0]);
                    continue;
                }

                List<Tuple<IRelationFilter, IFilter, List<Type>>> tuples = relationFilters.ConvertAll(x => new Tuple<IRelationFilter, IFilter, List<Type>>(x, x?.BaseFilter(), x?.Types()));

                tuples.RemoveAll(x => x.Item1 == null || x.Item2 == null || x.Item3 == null || x.Item3.Count == 0);
                tuples.Sort((x, y) => y.Item3.Count.CompareTo(x.Item3.Count));

                Func<List<Type>, List<Type>, bool> func = new Func<List<Type>, List<Type>, bool>((x, y)=>
                {
                    if(x == y)
                    {
                        return true;
                    }

                    if(x == null || y == null)
                    {
                        return false;
                    }

                    if(x.Count != y.Count)
                    {
                        return false;
                    }

                    for(int i =0; i < x.Count; i++)
                    {
                        if(x[i] != y[i])
                        {
                            return false;
                        }
                    }

                    return true;
                });

                while (tuples != null && tuples.Count > 1 && !tuples.TrueForAll(x => x.Item3.Count <= 0))
                {
                    int index = tuples.FindIndex(x => x.Item3.Count != 0);

                    Tuple<IRelationFilter, IFilter, List<Type>> tuple = tuples[index];
                    List<Tuple<IRelationFilter, IFilter, List<Type>>> tuples_Temp = tuples.FindAll(x => func.Invoke(tuple.Item3, x.Item3));
                    if(tuples_Temp.Count == 1)
                    {
                        tuple = new Tuple<IRelationFilter, IFilter, List<Type>>(tuple.Item1, tuple.Item1.FindByType(tuple.Item3[tuple.Item3.Count - 1]), tuple.Item3);

                        tuple.Item3.RemoveAt(tuple.Item3.Count - 1);
                    }
                    else
                    {
                        tuple.Item1.UpdateByType(tuple.Item3[tuple.Item3.Count - 1], new LogicalFilter(logicalFilter.FilterLogicalOperator, tuples_Temp.ConvertAll(x => x.Item2)));

                        tuple = new Tuple<IRelationFilter, IFilter, List<Type>>(tuple.Item1, tuple.Item1.BaseFilter(), tuple.Item1.Types());
                    }

                    tuples.RemoveAll(x => tuples_Temp.Contains(x) || x.Item1 == null || x.Item2 == null || x.Item3 == null || x.Item3.Count == 0);
                    tuples.Add(tuple);

                    tuples.Sort((x, y) => y.Item3.Count.CompareTo(x.Item3.Count));
                }

                tuples.ForEach(x => filters.Add(x.Item1));

                //filters.Add(new LogicalFilter(logicalFilter.FilterLogicalOperator, relationFilters));
            }

            if(filters.Count == 0)
            {
                return null;
            }

            if(filters.Count == 1)
            {
                return filters[0];
            }

            result = new LogicalFilter(logicalFilter) { Filters = filters };
            return result;

        }
    }
}