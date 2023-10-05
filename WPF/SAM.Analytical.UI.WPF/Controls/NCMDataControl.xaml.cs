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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for NCMDataControl.xaml
    /// </summary>
    public partial class NCMDataControl : UserControl
    {
        public NCMDataControl()
        {
            InitializeComponent();
        }

        private void Button_Name_Click(object sender, RoutedEventArgs e)
        {
            NCMNameCollectionWindow nCMNameCollectionWindow = new NCMNameCollectionWindow(SAM.Analytical.Query.DefaultNCMNameCollection(), new NCMNameCollectionOptions() { Editable = false });
            if(nCMNameCollectionWindow.ShowDialog() != true)
            {
                return;
            }
        }
    }
}
