// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Core.Mollier.UI
{
    public class SelectMollierPointEventArgs : EventArgs
    {
        public MollierPoint MollierPoint {get; set;}

        public SelectMollierPointEventArgs()
            :base()
        {

        }
    }
}
