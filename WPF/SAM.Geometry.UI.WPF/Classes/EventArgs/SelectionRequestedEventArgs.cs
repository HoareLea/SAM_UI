// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI.WPF
{
    public class SelectionRequestedEventArgs : EventArgs
    {
        public List<SAMObject> SAMObjects { get; }

        public SelectionRequestedEventArgs(SAMObject sAMObject)
        {
            if (sAMObject != null)
            {
                SAMObjects = new List<SAMObject>() { sAMObject };
            }
        }

        public SelectionRequestedEventArgs(IEnumerable<SAMObject> sAMObjects)
        {
            if(sAMObjects != null)
            {
                SAMObjects = new List<SAMObject>(sAMObjects);
            }
        }
    }
}
