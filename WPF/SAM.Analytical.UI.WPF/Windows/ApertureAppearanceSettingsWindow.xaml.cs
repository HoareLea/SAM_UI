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
    /// Interaction logic for ApertureAppearanceSettingsWindow.xaml
    /// </summary>
    public partial class ApertureAppearanceSettingsWindow : System.Windows.Window
    {
        public ApertureAppearanceSettingsWindow()
        {
            InitializeComponent();
        }

        public ApertureAppearanceSettingsWindow(AdjacencyCluster adjacencyCluster, ApertureAppearanceSettings apertureAppearanceSettings)
        {
            InitializeComponent();

            AdjacencyCluster = adjacencyCluster;
            ApertureAppearanceSettings = apertureAppearanceSettings;
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return apertureAppearanceSettingsControl.AdjacencyCluster;
            }

            set
            {
                apertureAppearanceSettingsControl.AdjacencyCluster = value;
            }
        }

        public ApertureAppearanceSettings ApertureAppearanceSettings
        {
            get
            {
                return apertureAppearanceSettingsControl.ApertureAppearanceSettings;
            }

            set
            {
                apertureAppearanceSettingsControl.ApertureAppearanceSettings = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
