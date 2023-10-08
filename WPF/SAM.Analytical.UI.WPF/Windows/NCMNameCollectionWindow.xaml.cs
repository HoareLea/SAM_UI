using SAM.Analytical.Tas;
using SAM.Core.Windows.Forms;
using SAM.Core.Windows;
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
using Microsoft.Win32;
using SAM.Core;

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

        public NCMNameCollectionWindow(IEnumerable<NCMName> nCMNames, NCMNameCollectionOptions nCMNameCollectionOptions = null)
        {
            InitializeComponent();

            NCMNameCollectionControl_Main.NCMNameCollection = nCMNames == null ? null : new NCMNameCollection(nCMNames);
            NCMNameCollectionControl_Main.NCMNameCollectionOptions = nCMNameCollectionOptions;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void button_Open_Click(object sender, RoutedEventArgs e)
        {
            string path = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "TAS TIC files (*.tic)|*.tic|SAM files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog(this) == false)
            {
                return;
            }
            path = openFileDialog.FileName;

            NCMNameCollection nCMNameCollection = null;

            if (System.IO.Path.GetExtension(path).ToLower().EndsWith("tic"))
            {
                Action action = () =>
                {

                    using (Core.Tas.SAMTICDocument sAMTICDocument = new Core.Tas.SAMTICDocument(path))
                    {
                        nCMNameCollection = sAMTICDocument?.ToSAM();
                    }
                };

                MarqueeProgressForm.Show("Collecting data", action);
            }
            else
            {
                nCMNameCollection = Core.Convert.ToSAM<NCMNameCollection>(path)?.FirstOrDefault();
            }

            NCMNameCollectionControl_Main.NCMNameCollection = nCMNameCollection;
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            NCMNameCollection nCMNameCollection = NCMNameCollectionControl_Main.NCMNameCollection;

            string path = null;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "SAM files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog(this) == false)
            {
                return;
            }
            path = saveFileDialog.FileName;

            Core.Convert.ToFile((IJSAMObject)nCMNameCollection, path);
        }

        public NCMName SelectedNCMName
        {
            get
            {
                return NCMNameCollectionControl_Main.SelectedNCMName;
            }
        }
    }
}
