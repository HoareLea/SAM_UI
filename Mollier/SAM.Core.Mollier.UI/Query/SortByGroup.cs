// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static List<IMollierProcess> SortByGroup(this IEnumerable<IMollierProcess> mollierProcesses)
        {
            List<IMollierGroup> mollierGroups = Mollier.Query.Group(mollierProcesses);

            List<IMollierProcess> result = new List<IMollierProcess>();
            mollierGroups.ForEach(x =>
            {
                result.AddRange(((MollierGroup)x).GetObjects<IMollierProcess>());
            });

            return result;
        }
    }
}
