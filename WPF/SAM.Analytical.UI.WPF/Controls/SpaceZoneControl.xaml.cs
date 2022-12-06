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
    /// Interaction logic for SpaceZoneControl.xaml
    /// </summary>
    public partial class SpaceZoneControl : UserControl
    {
        private List<Space> spaces;

        public SpaceZoneControl()
        {
            InitializeComponent();
        }

        public SpaceZoneControl(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces)
        {
            if(spaces != null)
            {
                this.spaces = new List<Space>(spaces);
            }
            
            InitializeComponent();

            zonesControl.SelectionMode = SelectionMode.Single;
            zonesControl.AdjacencyCluster = adjacencyCluster;
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return zonesControl.AdjacencyCluster;
            }
            set
            {
                zonesControl.AdjacencyCluster = value;
                LoadSpaces();
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return spaces;
            }

            set
            {
                spaces = value;
                LoadSpaces();
            }
        }

        public Zone Zone
        {
            get
            {
                return zonesControl.Zones?.FirstOrDefault();
            }
        }

        private void LoadSpaces()
        {
            listView_Spaces.Items.Clear();

            if(spaces == null)
            {
                return;
            }

            foreach(Space space in spaces)
            {
                if(string.IsNullOrWhiteSpace(space?.Name))
                {
                    continue;
                }

                ListViewItem listViewItem = new ListViewItem() { Content = space.Name, Tag = space };
                int index = listView_Spaces.Items.Add(listViewItem);
                listView_Spaces.SelectedItems.Add(listViewItem);
            }
        }
    }
}
