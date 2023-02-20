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

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for RenameSpacesWindow.xaml
    /// </summary>
    public partial class RenameSpacesWindow : System.Windows.Window
    {
        public RenameSpacesWindow(UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces)
        {
            InitializeComponent();

            renameSpacesControl.UIAnalyticalModel = uIAnalyticalModel;
            renameSpacesControl.Spaces = spaces == null ? null : new List<Space>(spaces);
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            renameSpacesControl.Rename();

            DialogResult = true;
            Close();
        }

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
            renameSpacesControl.Rename();

            DialogResult = true;
        }
    }
}
