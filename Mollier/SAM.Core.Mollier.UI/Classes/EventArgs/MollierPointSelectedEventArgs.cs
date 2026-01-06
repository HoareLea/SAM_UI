// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System;

namespace SAM.Core.Mollier.UI
{
    public class MollierPointSelectedEventArgs : EventArgs
    {
        public MollierPoint MollierPoint {get;}

        public Point2D Location { get;}

        public MollierPointSelectedEventArgs(MollierPoint mollierPoint, Point2D location)
            :base()
        {
            MollierPoint = mollierPoint;
            Location = location;
        }
    }
}
