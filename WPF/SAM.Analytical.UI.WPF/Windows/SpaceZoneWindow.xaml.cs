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
    /// Interaction logic for SpaceZoneWindow.xaml
    /// </summary>
    public partial class SpaceZoneWindow : System.Windows.Window
    {
        public SpaceZoneWindow()
        {
            InitializeComponent();
        }

        public SpaceZoneWindow(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces)
        {
            InitializeComponent();

            spaceZoneControl.AdjacencyCluster = adjacencyCluster;

            if (spaces != null)
            {
                spaceZoneControl.Spaces = new List<Space>(spaces);
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            if(spaceZoneControl.Spaces == null || spaceZoneControl.Spaces.Count == 0)
            {
                MessageBox.Show("Please select spaces");
                return;
            }

            if(spaceZoneControl.Zone == null)
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

        public List<Space> Spaces
        {
            get
            {
                return spaceZoneControl.Spaces;
            }

            set
            {
                spaceZoneControl.Spaces = value;
            }
        }

        public Zone Zone
        {
            get
            {
                return spaceZoneControl.Zone;
            }
        }
    }
}
