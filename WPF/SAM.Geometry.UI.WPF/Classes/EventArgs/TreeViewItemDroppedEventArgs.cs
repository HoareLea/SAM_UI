// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows.Controls;

namespace SAM.Geometry.UI.WPF
{
    public class TreeViewItemDroppedEventArgs : EventArgs
    {
        public TreeViewItem SelectedTreeViewItem { get; }
        public TreeViewItem TargetTreeViewItem { get; }
        public EventResult EventResult { get; set; } = EventResult.Succeeded;

        public TreeViewItemDroppedEventArgs(TreeViewItem selectedTreeViewItem, TreeViewItem targetTreeViewItem)
        {
            SelectedTreeViewItem = selectedTreeViewItem;
            TargetTreeViewItem = targetTreeViewItem;
        }
    }
}
