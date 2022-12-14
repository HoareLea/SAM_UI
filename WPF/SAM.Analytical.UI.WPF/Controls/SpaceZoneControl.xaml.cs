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
        public SpaceZoneControl()
        {
            InitializeComponent();

            zonesControl.SelectionMode = SelectionMode.Single;
        }

        public SpaceZoneControl(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null, Zone selectedZone = null)
        {
            InitializeComponent();

            SetSpaces(spaces);

            zonesControl.SelectionMode = SelectionMode.Single;
            zonesControl.AdjacencyCluster = adjacencyCluster;

            SetSelectedSpaces(selectedSpaces);
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
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return GetSpaces(false);
            }

            set
            { 
                SetSpaces(value);
            }
        }

        public List<Space> SelectedSpaces
        {
            get
            {
                return GetSpaces(true);
            }

            set
            {
                SetSelectedSpaces(value);
            }
        }

        public Zone SelectedZone
        {
            get
            {
                return zonesControl.SelectedZones?.FirstOrDefault();
            }

            set
            {
                zonesControl.SelectedZones = value == null ? null : new List<Zone>() { value };
            }
        }

        private List<Space> GetSpaces(bool selected = true)
        {
            if(listView_Spaces.SelectedItems == null)
            {
                return null;
            }

            System.Collections.IList list = selected ? listView_Spaces.SelectedItems : listView_Spaces.Items;

            List<Space> result = new List<Space>();
            foreach(ListViewItem listViewItem in list)
            {
                Space space = listViewItem.Tag as Space;
                if(space == null)
                {
                    continue;
                }

                result.Add(space);
            }

            return result;
        }

        private void SetSpaces(IEnumerable<Space> spaces)
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
                listView_Spaces.Items.Add(listViewItem);
            }
        }

        private void SetSelectedSpaces(IEnumerable<Space> spaces)
        {
            if(spaces == null || listView_Spaces.Items == null)
            {
                return;
            }

            listView_Spaces.SelectedItems.Clear();


            foreach(ListViewItem listViewItem in listView_Spaces.Items)
            {
                Space space = listViewItem?.Tag as Space;
                if(space == null)
                {
                    continue;
                }

                foreach(Space space_Temp in spaces)
                {
                    if(space_Temp == null)
                    {
                        continue;
                    }

                    if(space_Temp.Guid == space.Guid)
                    {
                        listView_Spaces.SelectedItems.Add(listViewItem);
                        break;
                    }
                }
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
            //SelectZoneCategory();
        }
    }
}
