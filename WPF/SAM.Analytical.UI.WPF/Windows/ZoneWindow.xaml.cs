using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ZoneWindow.xaml
    /// </summary>
    public partial class ZoneWindow : System.Windows.Window
    {
        public ZoneWindow()
        {
            InitializeComponent();
        }

        public ZoneWindow(Zone zone, AdjacencyCluster adjacencyCluster = null)
        {
            InitializeComponent();

            zoneControl.AdjacencyCluster = adjacencyCluster;
            zoneControl.Zone = zone;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            Zone zone = Zone;
            if(zone == null)
            {
                MessageBox.Show("Zone is invalid");
                return;
            }

            if(!zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrWhiteSpace(zoneCategory))
            {
                MessageBox.Show("Provide valid zone type");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public Zone Zone
        {
            get
            {
                return zoneControl.Zone;
            }

            set
            {
                zoneControl.Zone = value;
            }
        }
    }
}
