using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AllowTreeViewItemByType(this TreeViewItemHighlightedEventArgs treeViewItemHighlightedEventArgs)
        {
            TreeViewItem treeViewItem = treeViewItemHighlightedEventArgs.TreeViewItem;
            if (treeViewItem == null)
            {
                treeViewItemHighlightedEventArgs.EventResult = EventResult.Failed;
                return;
            }

            Type type = treeViewItem.Tag?.GetType();
            if (type == null)
            {
                treeViewItemHighlightedEventArgs.EventResult = EventResult.Failed;
                return;
            }


            List<TreeViewItem> treeViewItems = treeViewItemHighlightedEventArgs.HighlightedTreeViewItems;
            if (treeViewItems == null || treeViewItems.Count == 0)
            {
                return;
            }

            treeViewItemHighlightedEventArgs.EventResult = EventResult.Failed;
            foreach (TreeViewItem treeViewItem_Temp in treeViewItems)
            {
                Type type_Temp = treeViewItem_Temp.Tag?.GetType();
                if (type_Temp == null)
                {
                    continue;
                }

                if (type_Temp.IsAssignableFrom(type) || type.IsAssignableFrom(type_Temp))
                {
                    treeViewItemHighlightedEventArgs.EventResult = EventResult.Succeeded;
                    break;
                }
            }
        }
    }
}