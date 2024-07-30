using System.Windows;

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
