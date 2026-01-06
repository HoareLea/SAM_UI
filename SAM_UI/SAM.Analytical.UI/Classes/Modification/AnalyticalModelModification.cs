// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.UI;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public class AnalyticalModelModification : Modification
    {
        private List<SAMObject> sAMObjects;

        public AnalyticalModelModification(IEnumerable<SAMObject> sAMObjects)
        {
            this.sAMObjects = sAMObjects == null ? null : new List<SAMObject>(sAMObjects);
        }

        public List<Guid> Guids
        {
            get
            {
                return sAMObjects?.ConvertAll(x => x.Guid);
            }
        }
    }
}
