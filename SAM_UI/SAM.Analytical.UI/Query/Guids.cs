// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static HashSet<Guid> Guids<T>(this IEnumerable<T> analyticalModelModifications) where T: AnalyticalModelModification
        {
            if(analyticalModelModifications == null || analyticalModelModifications.Count() == 0)
            {
                return null;
            }

            HashSet<Guid> result = new HashSet<Guid>();
            foreach(T t in analyticalModelModifications)
            {
                List<Guid> guids = t?.Guids;
                if(guids == null || guids.Count == 0)
                {
                    continue;
                }

                guids.ForEach(x => result.Add(x));
            }

            return result;
        }
    }
}
