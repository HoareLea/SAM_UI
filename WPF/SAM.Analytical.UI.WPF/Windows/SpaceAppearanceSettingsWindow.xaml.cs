using System.Windows;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Interaction logic for SpaceAppearanceSettingsWindow.xaml
    /// </summary>
    public partial class SpaceAppearanceSettingsWindow : System.Windows.Window
    {
        public SpaceAppearanceSettingsWindow()
        {
            InitializeComponent();
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return spaceAppearanceSettingsControl.AdjacencyCluster;
            }

            set
            {
                spaceAppearanceSettingsControl.AdjacencyCluster = value;
            }
        }

        public SpaceAppearanceSettings SpaceAppearanceSettings
        {
            get
            {
                return spaceAppearanceSettingsControl.SpaceAppearanceSettings;
            }

            set
            {
                spaceAppearanceSettingsControl.SpaceAppearanceSettings = value;
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
