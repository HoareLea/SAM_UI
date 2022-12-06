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

            zonesControl.SelectionMode = SelectionMode.Single;
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

        private void SelectZoneCategory()
        {
            List<Space> spaces = Spaces;
            if (spaces != null && spaces.Count != 0)
            {
                AdjacencyCluster adjacencyCluster = AdjacencyCluster;
                if (adjacencyCluster != null)
                {
                    List<Tuple<string, int>> tuples = new List<Tuple<string, int>>();
                    foreach (Space space in spaces)
                    {
                        List<Zone> zones = adjacencyCluster.GetZones(space);
                        if (zones != null && zones.Count != 0)
                        {
                            foreach (Zone zone in zones)
                            {
                                if (zone == null || !zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrEmpty(zoneCategory))
                                {
                                    continue;
                                }

                                int index = tuples.FindIndex(x => x.Item1 == zoneCategory);
                                if (index == -1)
                                {
                                    index = tuples.Count;
                                    tuples.Add(new Tuple<string, int>(zoneCategory, 0));
                                }

                                tuples[index] = new Tuple<string, int>(zoneCategory, tuples[index].Item2 + 1);
                            }
                        }
                    }

                    if (tuples != null && tuples.Count != 0)
                    {
                        tuples.Sort((x, y) => y.Item2.CompareTo(x.Item2));
                        zonesControl.ZoneCategory = tuples[0].Item1;
                    }
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SelectZoneCategory();
        }
    }
}
