// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public class ObjectContextMenuOpeningEventArgs : EventArgs
    {
        public List<ModelVisual3D> ModelVisual3Ds { get; }
        public ContextMenuEventArgs ContextMenuEventArgs { get; }
        public ContextMenu ContextMenu { get; }

        public ObjectContextMenuOpeningEventArgs(ContextMenu contextMenu, ContextMenuEventArgs contextMenuEventArgs, IEnumerable<ModelVisual3D> modelVisual3Ds)
        {
            ContextMenu = contextMenu;
            ContextMenuEventArgs = contextMenuEventArgs;

            if (modelVisual3Ds != null)
                ModelVisual3Ds = new List<ModelVisual3D>(modelVisual3Ds);
        }
    }
}
