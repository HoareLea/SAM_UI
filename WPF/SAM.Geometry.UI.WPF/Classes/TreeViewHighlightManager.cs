using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SAM.Geometry.UI.WPF
{
    public class TreeViewHighlightManager
    {
        private List<TreeViewItem> treeViewItems = new List<TreeViewItem>();

        private SolidColorBrush solidColorBrush_Highlight_Background = new SolidColorBrush(Color.FromRgb(System.Drawing.SystemColors.Highlight.R, System.Drawing.SystemColors.Highlight.G, System.Drawing.SystemColors.Highlight.B));
        private SolidColorBrush solidColorBrush_Highlight_Foreground = new SolidColorBrush(Color.FromRgb(System.Drawing.SystemColors.HighlightText.R, System.Drawing.SystemColors.HighlightText.G, System.Drawing.SystemColors.HighlightText.B));
        private SolidColorBrush solidColorBrush_LostFocus_Background = new SolidColorBrush(Color.FromRgb(System.Drawing.SystemColors.Control.R, System.Drawing.SystemColors.Control.G, System.Drawing.SystemColors.Control.B));
        private SolidColorBrush solidColorBrush_LostFocus_Foreground = new SolidColorBrush(Color.FromRgb(System.Drawing.SystemColors.HighlightText.R, System.Drawing.SystemColors.HighlightText.G, System.Drawing.SystemColors.HighlightText.B));
        private SolidColorBrush solidColorBrush_Foreground;
        private SolidColorBrush solidColorBrush_Background;

        private TreeView treeView;

        private bool enabled = true;

        public event TreeViewItemHighlightedEventHandler TreeViewItemHighlighted;

        public TreeViewHighlightManager(TreeView treeView)
        {
            SetTreeView(treeView);

            solidColorBrush_Background = treeView.Background as SolidColorBrush;
            solidColorBrush_Foreground = treeView.Foreground as SolidColorBrush;
        }

        public TreeView TreeView
        {
            get
            {
                return GetTreeView();
            }
        }

        private void SetTreeView(TreeView treeView)
        {
            if(this.treeView == treeView)
            {
                return;
            }

           if(this.treeView != null)
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged; 
                treeView.GotFocus += TreeView_GotFocus; 
                treeView.LostFocus -= TreeView_LostFocus;
            }
            
            this.treeView = treeView;
            treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
            treeView.GotFocus += TreeView_GotFocus;
            treeView.LostFocus += TreeView_LostFocus;
        }

        private void TreeView_LostFocus(object sender, RoutedEventArgs e)
        {
            LostFocus();
        }

        private void TreeView_GotFocus(object sender, RoutedEventArgs e)
        {
            Highlight(true);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(!enabled)
            {
                return;
            }
            
            TreeViewItem treeViewItem_Old = e.OldValue as TreeViewItem;
            if(treeViewItem_Old != null)
            {
                treeViewItem_Old.Background = solidColorBrush_Background;
                treeViewItem_Old.Foreground = solidColorBrush_Foreground;
            }
            
            if (treeView.SelectedItem != null)
            {
                TreeViewItem treeViewItem = treeView.SelectedItem as TreeViewItem;
                if (treeViewItem != null)
                {
                    treeViewItem.Background = solidColorBrush_Background;
                    treeViewItem.Foreground = solidColorBrush_Highlight_Foreground;
                }
            }

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Highlight(false);
                treeViewItems.Clear();
            }
            else
            {
                TreeViewItem treeViewItem = treeView.SelectedItem as TreeViewItem;
                if(treeViewItem != null)
                {
                    TreeViewItemHighlightedEventArgs treeViewItemHighlightedEventArgs = new TreeViewItemHighlightedEventArgs(treeViewItem, GetHighlightedTreeViewItems());
                    TreeViewItemHighlighted?.Invoke(treeView, treeViewItemHighlightedEventArgs);

                    if (treeViewItemHighlightedEventArgs.EventResult == EventResult.Succeeded)
                    {
                        treeViewItems.Add(treeView.SelectedItem as TreeViewItem);
                    }

                    Highlight(true);
                }
            }
        }

        public List<TreeViewItem> HighlightedTreeViewItems
        {
            get
            {
                return GetHighlightedTreeViewItems();
            }

            set
            {
                SetHighlightedTreeViewItems(value);
            }
        }

        private List<TreeViewItem> GetHighlightedTreeViewItems()
        {
            return new List<TreeViewItem>(treeViewItems);
        }

        private void SetHighlightedTreeViewItems(IEnumerable<TreeViewItem> treeViewItems)
        {
            if(treeViewItems == null || treeViewItems.Count() == 0)
            {
                return;
            }

            foreach(TreeViewItem treeViewItem in treeViewItems)
            {
                if(this.treeViewItems.Contains(treeViewItem))
                {
                    continue;
                }

                Highlight(true);
            }
        }

        private void Highlight(bool highlight)
        {
            foreach(TreeViewItem treeViewItem in treeViewItems)
            {
                if (treeView.SelectedItem == treeViewItem)
                {
                    treeViewItem.Background = solidColorBrush_Background;
                    treeViewItem.Foreground = solidColorBrush_Highlight_Foreground;
                    continue;
                }

                if (highlight)
                {
                    treeViewItem.Background = solidColorBrush_Highlight_Background;
                    treeViewItem.Foreground = solidColorBrush_Highlight_Foreground;
                }
                else
                {
                    treeViewItem.Background = solidColorBrush_Background;
                    treeViewItem.Foreground = solidColorBrush_Foreground;
                }
            }
        }

        private void LostFocus()
        {
            foreach (TreeViewItem treeViewItem in treeViewItems)
            {
                if (treeView.SelectedItem == treeViewItem)
                {
                    treeViewItem.Background = solidColorBrush_Background;
                    treeViewItem.Foreground = solidColorBrush_Highlight_Foreground;
                    continue;
                }

                treeViewItem.Background = solidColorBrush_LostFocus_Background;
                treeViewItem.Foreground = solidColorBrush_LostFocus_Foreground;
            }
        }

        private TreeView GetTreeView()
        {
            return treeView;
        }

        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                enabled = value;
                if(!enabled)
                {
                    Highlight(false);
                    treeViewItems.Clear();
                }
            }
        }
    }
}
