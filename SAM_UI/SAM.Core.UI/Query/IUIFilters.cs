// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static List<IUIFilter> IUIFilters(this System.Type type)
        {
            if(type == null)
            {
                return null;
            }

            List<IUIFilter> result = new List<IUIFilter>();
            if (typeof(ISAMObject).IsAssignableFrom(type))
            {
                result.Add(new UITextFilter(string.Format("{0} Name", type.Name), type, new NameFilter(TextComparisonType.Contains, string.Empty)));
                result.Add(new UITextFilter(string.Format("{0} Guid", type.Name), type, new GuidFilter(TextComparisonType.Contains, string.Empty)));
            }

            if(typeof(ParameterizedSAMObject).IsAssignableFrom(type))
            {
                //result.Add(new UIParameterFilter(string.Format("{0} Parameter", type.Name), type, new ParameterFilter(string.Empty, string.Empty, TextComparisonType.Equals)));
            }

            if(result.Count > 0)
            {
                //result.Add(new UILogicalFilter(string.Format("{0} Logical And/Or", type.Name), type, new LogicalFilter(FilterLogicalOperator.Or)));
            }

            result?.Sort((x, y) => x.Name.CompareTo(y.Name));

            return result;
        }
    }
}
