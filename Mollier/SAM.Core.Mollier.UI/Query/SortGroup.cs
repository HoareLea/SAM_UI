// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static MollierGroup SortGroup(this MollierGroup mollierGroup)
        {
            if(mollierGroup == null)
            {
                return null;
            }

            List<IMollierProcess> mollierProcesses = mollierGroup.GetObjects<IMollierProcess>(false);
            List<IMollierGroup> processesAsGroups = Mollier.Query.Group(mollierProcesses);

            mollierProcesses?.ForEach(x => mollierGroup.Remove(x));
            
            foreach(MollierGroup mollierGroupTemp in processesAsGroups)
            {
                List<IMollierProcess> mollierProcessesTemp = mollierGroupTemp.GetObjects<IMollierProcess>(false);

                mollierProcessesTemp.ForEach(x => mollierGroup.Add(x));
            }

            return mollierGroup;
        }

    }
}
