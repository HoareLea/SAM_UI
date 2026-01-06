// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static string GroupName(IMollierObject mollierObject, IEnumerable<MollierGroup> mollierGroups)
        {
            string result = string.Empty;
            if (mollierObject == null || mollierGroups == null)
            {
                return result;
            }

            if (mollierObject is UIMollierPoint)
            {
                foreach (MollierGroup mollierGroup in mollierGroups)
                {
                    if (mollierGroup.GetObjects<UIMollierPoint>().Find(x => x == (UIMollierPoint)mollierObject) != null)
                    {
                        result = mollierGroup.Name;
                        break;
                    }
                }
            }
            else if (mollierObject is UIMollierProcess)
            {
                foreach (MollierGroup mollierGroup in mollierGroups)
                {
                    if (mollierGroup.GetObjects<UIMollierProcess>().Find(x => x == (UIMollierProcess)mollierObject) != null)
                    {
                        result = mollierGroup.Name;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
