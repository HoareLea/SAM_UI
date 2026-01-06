using System.Windows;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Interaction logic for SpaceAppearanceSettingsWindow.xaml
    /// </summary>
    public partial class PanelAppearanceSettingsWindow : System.Windows.Window
    {
        public PanelAppearanceSettingsWindow()
        {
            InitializeComponent();
        }

        public PanelAppearanceSettingsWindow(AdjacencyCluster adjacencyCluster, PanelAppearanceSettings panelAppearanceSettings)
        {
            InitializeComponent();

            AdjacencyCluster = adjacencyCluster;
            PanelAppearanceSettings = panelAppearanceSettings;
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return panelAppearanceSettingsControl.AdjacencyCluster;
            }

            set
            {
                panelAppearanceSettingsControl.AdjacencyCluster = value;
            }
        }

        public PanelAppearanceSettings PanelAppearanceSettings
        {
            get
            {
                return panelAppearanceSettingsControl.PanelAppearanceSettings;
            }

            set
            {
                panelAppearanceSettingsControl.PanelAppearanceSettings = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
