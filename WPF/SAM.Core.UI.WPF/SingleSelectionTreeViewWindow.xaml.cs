using System.Collections.Generic;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for SingleSelectionTreeViewWindow.xaml
    /// </summary>
    public partial class SingleSelectionTreeViewWindow : Window
    {
        public event GettingTextEventHandler GettingText;
        public event GettingCategoryEventHandler GettingCategory;

        public SingleSelectionTreeViewWindow()
        {
            InitializeComponent();

            SingleSelectionTreeViewControl_Main.GettingText += SingleSelectionTreeViewControl_Main_GettingText;
            SingleSelectionTreeViewControl_Main.GettingCategory += SingleSelectionTreeViewControl_Main_GettingCategory;
        }

        private void SingleSelectionTreeViewControl_Main_GettingCategory(object sender, GettingCategoryEventArgs e)
        {
            GettingCategory?.Invoke(this, e);
        }

        private void SingleSelectionTreeViewControl_Main_GettingText(object sender, GettingTextEventArgs e)
        {
            GettingText?.Invoke(this, e);
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

        public T GetObject<T>()
        {
            return SingleSelectionTreeViewControl_Main.GetObject<T>();
        }

        public void SetObjects<T>(IEnumerable<T> objects)
        {
            SingleSelectionTreeViewControl_Main.SetObjects<T>(objects);
        }

        public string UndefinedText
        {
            get
            {
                return SingleSelectionTreeViewControl_Main.UndefinedText;
            }

            set
            {
                SingleSelectionTreeViewControl_Main.UndefinedText = value;
            }
        }
    }
}
