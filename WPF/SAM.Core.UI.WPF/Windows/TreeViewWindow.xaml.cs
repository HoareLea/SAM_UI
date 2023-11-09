using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for TreeViewWindow.xaml
    /// </summary>
    public partial class TreeViewWindow : Window
    {
        public event GettingTextEventHandler GettingText;
        public event GettingCategoryEventHandler GettingCategory;

        public TreeViewWindow()
        {
            InitializeComponent();

            TreeViewControl_Main.GettingCategory += TreeViewControl_Main_GettingCategory;
            TreeViewControl_Main.GettingText += TreeViewControl_Main_GettingText;
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
                return TreeViewControl_Main.UndefinedText;
            }

            set
            {
                TreeViewControl_Main.UndefinedText = value;
            }
        }

        public List<T> GetObjects<T>()
        {
            return TreeViewControl_Main.GetObjects<T>();
        }

        public void SetObjects<T>(IEnumerable<T> objects)
        {
            TreeViewControl_Main.SetObjects(objects);
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
