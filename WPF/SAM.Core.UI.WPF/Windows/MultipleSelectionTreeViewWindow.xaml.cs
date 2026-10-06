using System.Collections.Generic;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleSelectionTreeViewWindow.xaml
    /// </summary>
    public partial class MultipleSelectionTreeViewWindow : Window
    {
        public event GettingTextEventHandler GettingText;
        public event GettingCategoryEventHandler GettingCategory;

        /// <summary>
        /// Raised once per object while <see cref="SetObjects{T}"/> builds the tree, to ask whether that
        /// object should start out ticked. Subscribe before calling <see cref="SetObjects{T}"/> - the
        /// tree is built there, so a handler attached afterwards is never asked.
        /// </summary>
        public event GettingCheckedEventHandler GettingChecked;

        public MultipleSelectionTreeViewWindow()
        {
            InitializeComponent();

            MultipleSelectionTreeViewControl_Main.GettingCategory += TreeViewControl_Main_GettingCategory;
            MultipleSelectionTreeViewControl_Main.GettingText += TreeViewControl_Main_GettingText;
            MultipleSelectionTreeViewControl_Main.GettingChecked += TreeViewControl_Main_GettingChecked;
        }

        private void TreeViewControl_Main_GettingChecked(object sender, GettingCheckedEventArgs e)
        {
            GettingChecked?.Invoke(this, e);
        }

        private void TreeViewControl_Main_GettingText(object sender, GettingTextEventArgs e)
        {
            GettingText?.Invoke(this, e);
        }

        private void TreeViewControl_Main_GettingCategory(object sender, GettingCategoryEventArgs e)
        {
            GettingCategory?.Invoke(this, e);
        }

        public string UndefinedText
        {
            get
            {
                return MultipleSelectionTreeViewControl_Main.UndefinedText;
            }

            set
            {
                MultipleSelectionTreeViewControl_Main.UndefinedText = value;
            }
        }

        public List<T> GetObjects<T>()
        {
            return MultipleSelectionTreeViewControl_Main.GetObjects<T>();
        }

        public void SetObjects<T>(IEnumerable<T> objects)
        {
            MultipleSelectionTreeViewControl_Main.SetObjects(objects);
        }

        public void SelectAll()
        {
            MultipleSelectionTreeViewControl_Main.SelectAll();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }
    }
}
