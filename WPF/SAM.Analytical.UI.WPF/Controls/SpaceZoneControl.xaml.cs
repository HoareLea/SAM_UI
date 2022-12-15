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
            zonesControl.ZoneCategorySelectionChanged += ZonesControl_ZoneCategorySelectionChanged;
        }

        public SpaceZoneControl(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null, Zone selectedZone = null)
        {
            InitializeComponent();

            SetSpaces(spaces);

            zonesControl.SelectionMode = SelectionMode.Single;
            zonesControl.AdjacencyCluster = adjacencyCluster;
            zonesControl.ZoneCategorySelectionChanged += ZonesControl_ZoneCategorySelectionChanged;

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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateZonesSelection()
        {
            List<Space> spaces = SelectedSpaces;
            if (spaces == null || spaces.Count == 0)
            {
                return;
            }

            List<Zone> zones = zonesControl.Zones;
            if (zones == null || zones.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }


            foreach (Space space in spaces)
            {
                List<Zone> zones_temp = adjacencyCluster.GetZones(space);
                if (zones_temp == null || zones_temp.Count == 0)
                {
                    zones = null;
                    break;
                }

                for (int i = zones.Count - 1; i >= 0; i--)
                {
                    if (zones_temp.Find(x => x.Guid == zones[i].Guid) == null)
                    {
                        zones.RemoveAt(i);
                    }
                }

                if (zones.Count == 0)
                {
                    break;
                }
            }

            zonesControl.SelectedZones = zones;
        }

        private void ZonesControl_ZoneCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateZonesSelection();
        }

        private void listView_Spaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateZonesSelection();
        }
    }
}
