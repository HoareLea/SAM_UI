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
    /// Interaction logic for NCMNameCollectionWindow.xaml
    /// </summary>
    public partial class NCMNameCollectionWindow : System.Windows.Window
    {
        public NCMNameCollectionWindow()
        {
            InitializeComponent();
        }

        public NCMNameCollectionWindow(IEnumerable<NCMName> nCMNames)
        {
            InitializeComponent();

            NCMNameCollectionControl_Main.NCMNameCollection = nCMNames == null ? null : new NCMNameCollection(nCMNames);
        }
    }
}
