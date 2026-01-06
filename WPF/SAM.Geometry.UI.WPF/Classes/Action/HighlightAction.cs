// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class HighlightAction :Visual3DsAction
    {

        public HighlightAction(Visual3D visual3D)
            :base(visual3D)
        {

        }

        public HighlightAction(IEnumerable<Visual3D> visual3Ds)
            : base(visual3Ds)
        {

        }

        public void Highlight(bool highlight)
        {
            List<Visual3D> visual3Ds = Visual3Ds;
            if(visual3Ds != null)
            {
                visual3Ds.ForEach(x => Modify.Highlight(x, highlight));
            }
        }
    }
}
