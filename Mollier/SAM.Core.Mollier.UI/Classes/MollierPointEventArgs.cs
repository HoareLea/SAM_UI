// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointEventArgs : EventArgs
    {
        public MollierPoint MollierPoint { get; private set;  }

        public MollierPointEventArgs(MollierPoint mollierPoint)
            :base()
        {
            MollierPoint = mollierPoint;
        }
    }
}
