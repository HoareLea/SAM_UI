using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AllowTreeViewItemByType(this TreeViewItemHighlightedEventArgs treeViewItemHighlightedEventArgs, Type type = null)
        {
            TreeViewItem treeViewItem = treeViewItemHighlightedEventArgs.TreeViewItem;
            if (treeViewItem == null)
            {
                treeViewItemHighlightedEventArgs.EventResult = EventResult.Failed;
                return;
            }

            Type type_1 = treeViewItem.Tag?.GetType();
            if (type_1 == null)
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
                Type type_2 = treeViewItem_Temp.Tag?.GetType();
                if (type_2 == null)
                {
                    continue;
                }

                if(type == null)
                {
                    if (type_2.IsAssignableFrom(type_1) || type_1.IsAssignableFrom(type_2))
                    {
                        treeViewItemHighlightedEventArgs.EventResult = EventResult.Succeeded;
                        break;
                    }
                }
                else
                {
                    if (type.IsAssignableFrom(type_1) || type.IsAssignableFrom(type_2))
                    {
                        treeViewItemHighlightedEventArgs.EventResult = EventResult.Succeeded;
                        break;
                    }
                }
            }
        }
    }
}