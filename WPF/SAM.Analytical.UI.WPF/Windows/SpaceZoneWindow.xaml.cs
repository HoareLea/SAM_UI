using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SpaceZoneWindow.xaml
    /// </summary>
    public partial class SpaceZoneWindow : System.Windows.Window
    {
        public SpaceZoneWindow()
        {
            InitializeComponent();
        }

        public SpaceZoneWindow(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null, Zone selectedZone = null)
        {
            InitializeComponent();

            spaceZoneControl.AdjacencyCluster = adjacencyCluster;

            if (spaces != null)
            {
                spaceZoneControl.Spaces = new List<Space>(spaces);
            }

            if(selectedSpaces != null)
            {
                spaceZoneControl.SelectedSpaces = new List<Space>(selectedSpaces);
            }

            spaceZoneControl.SelectedZone = selectedZone;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            if(spaceZoneControl.Spaces == null || spaceZoneControl.Spaces.Count == 0)
            {
                MessageBox.Show("Please select spaces");
                return;
            }

            if(spaceZoneControl.SelectedZone == null)
            {
                MessageBox.Show("Please select zones");
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return spaceZoneControl.AdjacencyCluster;
            }

            set
            {
                spaceZoneControl.AdjacencyCluster = value;
            }
        }

        public List<Space> SelectedSpaces
        {
            get
            {
                return spaceZoneControl.SelectedSpaces;
            }

            set
            {
                spaceZoneControl.Spaces = value;
            }
        }

        public Zone SelectedZone
        {
            get
            {
                return spaceZoneControl.SelectedZone;
            }
        }
    }
}
