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
                    treeViewItem = GetTreeViewItem(TreeView_Main.Items, gettingCategoryEventArgs.Category);
                }

                ItemCollection itemCollection = treeViewItem == null ? TreeView_Main.Items : treeViewItem.Items;
                if (itemCollection == null)
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
        }

        private TreeViewItem GetTreeViewItem(ItemCollection itemCollection, Category category)
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
                return GetTreeViewItem(result.Items, category.SubCategory);
            }
        }

        private TreeViewItem GetTreeViewItem(object @object)
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

        public T GetObject<T>()
        {
            object @object = (TreeView_Main.SelectedItem as TreeViewItem)?.Tag;

            return @object is T ? (T)@object : default;
        }
    }
}
