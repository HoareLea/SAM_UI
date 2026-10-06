using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleSelectionTreeViewControl.xaml
    /// </summary>
    public partial class MultipleSelectionTreeViewControl : UserControl
    {
        public string UndefinedText { get; set; } = "???";
        
        public event GettingTextEventHandler GettingText;
        public event GettingCategoryEventHandler GettingCategory;

        /// <summary>
        /// Raised once per object while <see cref="SetObjects{T}"/> builds the tree, to ask whether that
        /// object should start out ticked. With no handler attached nothing is ticked, which is how the
        /// control behaved before this event existed.
        /// </summary>
        public event GettingCheckedEventHandler GettingChecked;

        public MultipleSelectionTreeViewControl()
        {
            InitializeComponent();
        }

        public void SetObjects<T>(IEnumerable<T> objects)
        {
            TreeView_Main.Items.Clear();

            if(objects == null || objects.Count() == 0)
            {
                return;
            }

            foreach(T @object in objects)
            {
                if(@object == null)
                {
                    continue;
                }

                TreeViewItem treeViewItem = null;
                if(GettingCategory != null)
                {
                    GettingCategoryEventArgs gettingCategoryEventArgs = new GettingCategoryEventArgs(@object);
                    GettingCategory.Invoke(this, gettingCategoryEventArgs);
                    treeViewItem = GetTreeViewItem(TreeView_Main.Items, gettingCategoryEventArgs.Category);
                }

                ItemCollection itemCollection = treeViewItem == null ? TreeView_Main.Items : treeViewItem.Items;
                if(itemCollection == null)
                {
                    continue;
                }

                TreeViewItem treeViewItem_New = GetTreeViewItem(@object);
                if (treeViewItem_New == null)
                {
                    continue;
                }

                itemCollection.Add(treeViewItem_New);
            }

            RefreshSelection(TreeView_Main.Items);
        }

        /// <summary>
        /// Recomputes every category node's tick from its children, bottom-up.
        /// <para>
        /// Only matters when <see cref="GettingChecked"/> pre-ticked some leaves. The per-item handler
        /// that normally keeps a parent in step runs on a user click, when the item is already in the
        /// tree; during <see cref="SetObjects{T}"/> an item has no Parent yet, so nothing propagates and
        /// a category whose children are all ticked would otherwise still render unticked.
        /// </para>
        /// </summary>
        private void RefreshSelection(ItemCollection itemCollection)
        {
            if(itemCollection == null)
            {
                return;
            }

            foreach(TreeViewItem treeViewItem in itemCollection)
            {
                if(treeViewItem == null)
                {
                    continue;
                }

                RefreshSelection(treeViewItem.Items);

                if(treeViewItem.Items == null || treeViewItem.Items.Count == 0)
                {
                    continue;
                }

                CheckBox checkBox = treeViewItem.Header as CheckBox;
                if(checkBox == null)
                {
                    continue;
                }

                HashSet<bool?> bools = new HashSet<bool?>();
                foreach(TreeViewItem treeViewItem_Child in treeViewItem.Items)
                {
                    CheckBox checkBox_Child = treeViewItem_Child?.Header as CheckBox;
                    if(checkBox_Child == null)
                    {
                        continue;
                    }

                    bools.Add(checkBox_Child.IsChecked);
                }

                if(bools.Count == 0)
                {
                    continue;
                }

                bool selected = !(bools.Contains(null) || bools.Contains(false));
                if(checkBox.IsChecked == selected)
                {
                    continue;
                }

                checkBox.Checked -= CheckBox_Updated;
                checkBox.Unchecked -= CheckBox_Updated;

                checkBox.IsChecked = selected;

                checkBox.Checked += CheckBox_Updated;
                checkBox.Unchecked += CheckBox_Updated;
            }
        }

        public List<T> GetObjects<T>(bool selected = true)
        {
            return GetObjects<T>(TreeView_Main.Items, selected);
        }

        private List<T> GetObjects<T>(ItemCollection itemCollection, bool selected = true)
        {
            if(itemCollection == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach(TreeViewItem treeViewItem in itemCollection)
            {
                if(treeViewItem == null)
                {
                    continue;
                }

                List<T> childs = GetObjects<T>(treeViewItem.Items, selected);
                if(childs != null)
                {
                    result.AddRange(childs);
                }

                if (!(treeViewItem.Tag is T))
                {
                    continue;
                }

                if(selected)
                {
                    CheckBox checkBox = treeViewItem.Header as CheckBox;
                    if(checkBox != null)
                    {
                        if(checkBox.IsChecked == null || !checkBox.IsChecked.HasValue || !checkBox.IsChecked.Value)
                        {
                            continue;
                        }
                    }
                }

                result.Add((T)treeViewItem.Tag);
            }

            return result;
        }

        private TreeViewItem GetTreeViewItem(ItemCollection itemCollection, Category category)
        {
            if (category == null || itemCollection == null)
            {
                return null;
            }

            string text = category.Name;
            if(string.IsNullOrEmpty(text))
            {
                text = UndefinedText;
            }

            TreeViewItem result = null;

            foreach(TreeViewItem treeViewItem in itemCollection)
            {
                if(treeViewItem == null || treeViewItem.Tag != null)
                {
                    continue;
                }

                CheckBox checkBox = treeViewItem.Header as CheckBox;
                if(checkBox == null)
                {
                    continue;
                }

                string text_Temp = checkBox.Content as string; 
                if(text_Temp == text)
                {
                    result = treeViewItem;
                    break;
                }
            }

            if(result == null)
            {
                CheckBox checkBox = new CheckBox() { Content = text };
                checkBox.Checked += CheckBox_Updated;
                checkBox.Unchecked += CheckBox_Updated;

                result = new TreeViewItem() { Header = checkBox, Tag = null };
                itemCollection.Add(result);
            }

            if(category.SubCategory == null)
            {
                return result;
            }
            else
            {
                return GetTreeViewItem(result.Items, category.SubCategory);
            }

        }

        private void CheckBox_Updated(object sender, System.Windows.RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if(checkBox == null)
            {
                return;
            }

            TreeViewItem treeViewItem = checkBox.Parent as TreeViewItem;
            if(treeViewItem == null)
            {
                return;
            }

            SetSelection(treeViewItem.Items, checkBox.IsChecked);
            SetSelection(treeViewItem);
        }

        private TreeViewItem GetTreeViewItem(object @object)
        {
            if (@object == null)
            {
                return null;
            }

            string text = null;
            if(GettingText != null)
            {
                GettingTextEventArgs gettingTextEventArgs = new GettingTextEventArgs(@object);
                GettingText.Invoke(this, gettingTextEventArgs);
                text = gettingTextEventArgs.Text;
            }

            if(string.IsNullOrEmpty(text))
            {
                text = @object.ToString();
            }

            if(string.IsNullOrEmpty(text))
            {
                text = UndefinedText;
            }

            bool @checked = false;
            if(GettingChecked != null)
            {
                GettingCheckedEventArgs gettingCheckedEventArgs = new GettingCheckedEventArgs(@object);
                GettingChecked.Invoke(this, gettingCheckedEventArgs);
                @checked = gettingCheckedEventArgs.Checked;
            }

            CheckBox checkBox = new CheckBox() { Content = text };

            // Set before the handlers are attached, not after. CheckBox_Updated exists to keep a parent
            // node in step with its children, and it cannot do that here: this item has no Parent until
            // SetObjects adds it to the tree, so the handler would walk up to nothing. Parent state is
            // brought in line once, by RefreshSelection, after the whole tree is built.
            checkBox.IsChecked = @checked;

            checkBox.Checked += CheckBox_Updated;
            checkBox.Unchecked += CheckBox_Updated;

            return new TreeViewItem() { Header = checkBox, Tag = @object };
        }

        private void SetSelection(TreeViewItem treeViewItem)
        {
            TreeViewItem treeViewItem_Parent = treeViewItem?.Parent as TreeViewItem;
            if (treeViewItem_Parent == null)
            {
                return;
            }

            CheckBox checkBox_Parent = treeViewItem_Parent.Header as CheckBox;
            if (checkBox_Parent == null)
            {
                return;
            }

            HashSet<bool?> bools = new HashSet<bool?>();
            foreach (TreeViewItem treeViewItem_Child in treeViewItem_Parent.Items)
            {
                CheckBox checkBox_Child = treeViewItem_Child.Header as CheckBox;
                if (checkBox_Child == null)
                {
                    continue;
                }

                bools.Add(checkBox_Child.IsChecked);
            }

            if (bools == null || bools.Count == 0)
            {
                return;
            }

            bool selected = !(bools.Contains(null) || bools.Contains(false));

            if (checkBox_Parent.IsChecked == selected)
            {
                return;
            }

            checkBox_Parent.Checked -= CheckBox_Updated;
            checkBox_Parent.Unchecked -= CheckBox_Updated;

            checkBox_Parent.IsChecked = selected;

            checkBox_Parent.Checked += CheckBox_Updated;
            checkBox_Parent.Unchecked += CheckBox_Updated;

            SetSelection(treeViewItem_Parent);
        }

        private void SetSelection(bool? selected)
        {
            SetSelection(TreeView_Main.Items, selected);
        }

        private void SetSelection(ItemCollection itemCollection, bool? selected)
        {
            if(itemCollection == null)
            {
                return;
            }

            foreach(TreeViewItem treeViewItem in itemCollection)
            {
                CheckBox checkBox = treeViewItem.Header as CheckBox;
                if(checkBox == null)
                {
                    continue;
                }

                checkBox.Checked -= CheckBox_Updated;
                checkBox.Unchecked -= CheckBox_Updated;

                checkBox.IsChecked = selected;

                checkBox.Checked += CheckBox_Updated;
                checkBox.Unchecked += CheckBox_Updated;

                SetSelection(treeViewItem.Items, selected);
            }
        }

        /// <summary>Checks every node (mirrors the WinForms TreeViewForm `@checked: x =&gt; true` initial state).</summary>
        public void SelectAll()
        {
            SetSelection(true);
        }

        private void Button_SelectAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetSelection(true);
        }

        private void Button_SelectNone_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetSelection(false);
        }
    }
}
