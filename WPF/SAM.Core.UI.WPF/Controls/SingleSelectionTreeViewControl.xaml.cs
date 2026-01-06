// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for SingleSelectionTreeViewControl.xaml
    /// </summary>
    public partial class SingleSelectionTreeViewControl : UserControl
    {
        public string UndefinedText { get; set; } = "???";

        public event GettingTextEventHandler GettingText;
        public event GettingCategoryEventHandler GettingCategory;
        public event CompareObjectsEventHandler CompareObjects;

        public SingleSelectionTreeViewControl()
        {
            InitializeComponent();
        }

        public void SetObjects<T>(IEnumerable<T> objects)
        {
            TreeView_Main.Items.Clear();

            if (objects == null || objects.Count() == 0)
            {
                return;
            }

            foreach (T @object in objects)
            {
                if (@object == null)
                {
                    continue;
                }

                TreeViewItem treeViewItem = null;
                if (GettingCategory != null)
                {
                    GettingCategoryEventArgs gettingCategoryEventArgs = new GettingCategoryEventArgs(@object);
                    GettingCategory.Invoke(this, gettingCategoryEventArgs);
                    treeViewItem = UpdateTreeViewItem(TreeView_Main.Items, gettingCategoryEventArgs.Category);
                }

                ItemCollection itemCollection = treeViewItem == null ? TreeView_Main.Items : treeViewItem.Items;
                if (itemCollection == null)
                {
                    continue;
                }

                TreeViewItem treeViewItem_New = UpdateTreeViewItem(@object);
                if (treeViewItem_New == null)
                {
                    continue;
                }

                itemCollection.Add(treeViewItem_New);
            }
        }

        private TreeViewItem UpdateTreeViewItem(ItemCollection itemCollection, Category category)
        {
            if (category == null || itemCollection == null)
            {
                return null;
            }

            string text = category.Name;
            if (string.IsNullOrEmpty(text))
            {
                text = UndefinedText;
            }

            TreeViewItem result = null;

            foreach (TreeViewItem treeViewItem in itemCollection)
            {
                if (treeViewItem == null || treeViewItem.Tag != null)
                {
                    continue;
                }

                string text_Temp = treeViewItem.Header as string;
                if (text_Temp == text)
                {
                    result = treeViewItem;
                    break;
                }
            }

            if (result == null)
            {
                result = new TreeViewItem() { Header = text, Tag = null };
                itemCollection.Add(result);
            }

            if (category.SubCategory == null)
            {
                return result;
            }
            else
            {
                return UpdateTreeViewItem(result.Items, category.SubCategory);
            }
        }

        private TreeViewItem UpdateTreeViewItem(object @object)
        {
            if (@object == null)
            {
                return null;
            }

            string text = null;
            if (GettingText != null)
            {
                GettingTextEventArgs gettingTextEventArgs = new GettingTextEventArgs(@object);
                GettingText.Invoke(this, gettingTextEventArgs);
                text = gettingTextEventArgs.Text;
            }

            if (string.IsNullOrEmpty(text))
            {
                text = @object.ToString();
            }

            if (string.IsNullOrEmpty(text))
            {
                text = UndefinedText;
            }

            return new TreeViewItem() { Header = text, Tag = @object };
        }

        public T GetSlecledObject<T>()
        {
            object @object = (TreeView_Main.SelectedItem as TreeViewItem)?.Tag;

            return @object is T ? (T)@object : default;
        }

        public void SetSelectedObject<T>(T @object)
        {
            TreeViewItem treeViewItem = TreeView_Main.SelectedItem as TreeViewItem;
            if(treeViewItem != null)
            {
                treeViewItem.IsSelected = false;
            }

            if(@object == null)
            {
                return;
            }

            treeViewItem = GetTreeViewItem(@object);
            if(treeViewItem == null)
            {
                return;
            }

            treeViewItem.IsSelected = true;
        }

        public TreeViewItem GetTreeViewItem(object @object, ItemCollection itemCollection = null)
        {
            if(@object == null)
            {
                return null;
            }

            if(itemCollection == null)
            {
                itemCollection = TreeView_Main.Items;
            }

            if(itemCollection == null)
            {
                return null;
            }

            foreach(TreeViewItem treeViewItem in itemCollection)
            {
                if(treeViewItem.Tag != null)
                {
                    CompareObjectsEventArgs compareObjectsEventArgs = new CompareObjectsEventArgs(@object, treeViewItem.Tag);
                    CompareObjects?.Invoke(this, compareObjectsEventArgs);
                    if (compareObjectsEventArgs != null && compareObjectsEventArgs.Equals != null && compareObjectsEventArgs.Equals.HasValue)
                    {
                        if (compareObjectsEventArgs.Equals.Value)
                        {
                            return treeViewItem;
                        }
                    }
                    else
                    {
                        if (treeViewItem.Tag == @object)
                        {
                            return treeViewItem;
                        }
                    }
                }

                if(treeViewItem.Items != null && treeViewItem.Items.Count != 0)
                {
                    TreeViewItem result = GetTreeViewItem(@object, treeViewItem.Items);
                    if(result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private bool ExpandAll(ItemCollection itemCollection, bool expand)
        {
            if(itemCollection == null)
            {
                return false;
            }

            foreach(TreeViewItem treeViewItem in itemCollection)
            {
                treeViewItem.IsExpanded = expand;

                if(treeViewItem.Items != null && treeViewItem.Items.Count != 0)
                {
                    ExpandAll(treeViewItem.Items, expand);
                }
            }

            return true;
        }

        public bool ExpandAll(bool expand = true)
        {
            return ExpandAll(TreeView_Main.Items, expand);
        }


    }
}
