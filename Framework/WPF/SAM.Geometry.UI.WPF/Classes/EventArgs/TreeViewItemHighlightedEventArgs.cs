using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Geometry.UI.WPF
{
    public class TreeViewItemHighlightedEventArgs : EventArgs
    {
        public TreeViewItem TreeViewItem { get; }
        public List<TreeViewItem> HighlightedTreeViewItems { get; }
        public EventResult EventResult { get; set; } = EventResult.Succeeded;

        public TreeViewItemHighlightedEventArgs(TreeViewItem treeViewItem, IEnumerable<TreeViewItem> highlightedTreeViewItems)
        {
            TreeViewItem = treeViewItem;
            if(highlightedTreeViewItems != null)
            {
                HighlightedTreeViewItems = new List<TreeViewItem>(highlightedTreeViewItems);
                if(treeViewItem != null)
                {
                    HighlightedTreeViewItems.Remove(treeViewItem);
                }
            }
            
        }
    }
}
