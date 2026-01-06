// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static Dictionary<System.Type, List<IFilter>> FilterDictionary(this IUIFilter uIFilter)
        {
            if(uIFilter == null)
            {
                return null;
            }

            Dictionary<System.Type, List<IFilter>> result = new Dictionary<System.Type, List<IFilter>>();

            System.Type type = uIFilter.Type;
            if (type != null)
            {
                if (!result.TryGetValue(type, out List<IFilter> filters) || filters == null)
                {
                    filters = new List<IFilter>();
                    result[type] = filters;
                }

                filters.Add(uIFilter);
            }

            if (uIFilter is UILogicalFilter)
            {
                List<IFilter> filters_Temp = ((UILogicalFilter)uIFilter).Filter?.Filters;
                if(filters_Temp != null)
                {
                    foreach(IFilter filter in filters_Temp)
                    {
                        if(filter is IUIFilter)
                        {
                            Dictionary<System.Type, List<IFilter>> dictionary = FilterDictionary((IUIFilter)filter);
                            if(dictionary != null)
                            {
                                foreach(KeyValuePair<System.Type, List<IFilter>> keyValuePair in dictionary)
                                {
                                    if(keyValuePair.Value == null || keyValuePair.Value.Count == 0)
                                    {
                                        continue;
                                    }

                                    if (!result.TryGetValue(keyValuePair.Key, out List<IFilter> filters) || filters == null)
                                    {
                                        filters = new List<IFilter>();
                                        result[keyValuePair.Key] = filters;
                                    }

                                    filters.AddRange(keyValuePair.Value);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
