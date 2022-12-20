using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ZonesControl.xaml
    /// </summary>
    public partial class ZonesControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;

        public event SelectionChangedEventHandler ZoneCategorySelectionChanged;

        public ZonesControl()
        {
            InitializeComponent();
        }

        public ZonesControl(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadZoneCategories();
        }

        public SelectionMode SelectionMode
        {
            get
            {
                return listView_Zones.SelectionMode;
            }

            set
            {
                listView_Zones.SelectionMode = value;
            }
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return adjacencyCluster;
            }

            set
            {
                adjacencyCluster = value;
                LoadZones();
            }
        }

        public string ZoneCategory
        {
            get
            {
                return comboBox_ZoneCategory?.SelectedItem?.ToString();
            }

            set
            {
                LoadZoneCategories();
                comboBox_ZoneCategory.SelectedItem = value;
            }
        }

        private void LoadZones()
        {
            listView_Zones.Items.Clear();

            if (adjacencyCluster == null)
            {
                return;
            }

            List<Zone> zones = adjacencyCluster?.GetObjects<Zone>();
            if(zones != null && zones.Count != 0)
            {
                foreach(Zone zone in zones)
                {
                    if(zone == null || !zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrEmpty(zoneCategory))
                    {
                        continue;
                    }

                    if(ZoneCategory != zoneCategory)
                    {
                        continue;
                    }

                    ListViewItem listViewItem = new ListViewItem() { Content = zone.Name, Tag = zone };

                    listView_Zones.Items.Add(listViewItem);
                }
            }

        }

        private void LoadZoneCategories()
        {
            object selectedItem = comboBox_ZoneCategory.SelectedItem;

            comboBox_ZoneCategory.Items.Clear();
            comboBox_ZoneCategory.Items.Add(string.Empty);

            List<string> zoneCategories = Query.ZoneCategories(adjacencyCluster);

            foreach (string zoneCategory in zoneCategories)
            {
                comboBox_ZoneCategory.Items.Add(zoneCategory);
            }

            comboBox_ZoneCategory.SelectedItem = selectedItem;
        }

        public List<Zone> Zones
        {
            get
            {
                return GetZones(false);
            }
        }

        private List<Zone> GetZones(bool selected = true)
        {
            System.Collections.IList list = selected ? listView_Zones.SelectedItems : listView_Zones.Items;
            if(list == null)
            {
                return null;
            }

            List <Zone> result = new List<Zone>();
            foreach (ListViewItem listViewItem in list)
            {
                Zone zone = listViewItem?.Tag as Zone;
                if (zone == null)
                {
                    continue;
                }

                result.Add(zone);
            }

            return result;
        }

        public List<Zone> SelectedZones
        {
            get
            {
                return GetZones(true);
            }

            set
            {
                SetSelectedZones(value);
            }
        }

        private void SetSelectedZones(IEnumerable<Zone> zones)
        {
            if(listView_Zones.SelectionMode == SelectionMode.Single)
            {
                listView_Zones.SelectedItem = null;
            }
            else
            {
                listView_Zones.SelectedItems.Clear();
            }

            if(comboBox_ZoneCategory.Items.Count == 0)
            {
                LoadZoneCategories();
            }

            if (zones == null)
            {
                return;
            }

            foreach (Zone zone in zones)
            {
                if (zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) && zoneCategory != null)
                {
                    if (comboBox_ZoneCategory.Items.Contains(zoneCategory) && ZoneCategory != zoneCategory)
                    {
                        ZoneCategory = zoneCategory;
                        break;
                    }
                }
            }

            foreach (ListViewItem listViewItem in listView_Zones.Items)
            {
                Zone zone = listViewItem?.Tag as Zone;
                if (zone == null)
                {
                    continue;
                }

                foreach(Zone zone_Temp in zones)
                {
                    if(zone_Temp != null && zone_Temp.Guid == zone.Guid)
                    {

                        if (listView_Zones.SelectionMode == SelectionMode.Single)
                        {
                            listView_Zones.SelectedItem = listViewItem;
                            return;
                        }
                        else
                        {
                            listView_Zones.SelectedItems.Add(listViewItem);
                            break;
                        }

                    }
                }
            }
        }

        private void comboBox_ZoneCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadZones();
            ZoneCategorySelectionChanged?.Invoke(this, e);
        }

        private void button_Edit_Click(object sender, RoutedEventArgs e)
        {
            List<Zone> zones = Zones;
            if(zones == null || zones.Count != 1)
            {
                MessageBox.Show("Select single Zone");
                return;
            }

            ZoneWindow zoneWindow = new ZoneWindow(zones[0], adjacencyCluster);
            bool? result = zoneWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            Zone zone = zoneWindow.Zone;
            if (zone == null)
            {
                return;
            }

            adjacencyCluster.AddObject(zone);
            LoadZoneCategories();

            if (zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory))
            {
                comboBox_ZoneCategory.SelectedItem = zoneCategory;
            }

            LoadZones();
        }

        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            List<Zone> zones = Zones;
            if (zones == null || zones.Count == 0)
            {
                MessageBox.Show("Select Zones");
                return;
            }

            if(MessageBox.Show("Are you sure to remove selected zones?", "Remove Zones", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            foreach(Zone zone in zones)
            {
                adjacencyCluster.RemoveObject<Zone>(zone.Guid);
            }

            LoadZoneCategories();
            LoadZones();
        }

        private void button_Add_Click(object sender, RoutedEventArgs e)
        {
            Zone zone = new Zone("New Zone");
            zone.SetValue(ZoneParameter.ZoneCategory, ZoneCategory);

            ZoneWindow zoneWindow = new ZoneWindow(zone, adjacencyCluster);
            bool? result = zoneWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            zone = zoneWindow.Zone;
            if (zone == null)
            {
                return;
            }

            adjacencyCluster.AddObject(zone);
            LoadZoneCategories();

            if (zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory))
            {
                comboBox_ZoneCategory.SelectedItem = zoneCategory;
            }

            LoadZones();

            if(SelectionMode == SelectionMode.Single)
            {
                listView_Zones.SelectedItem = null;
            }
            else
            {
                listView_Zones.SelectedItems.Clear();
            }

            foreach(ListViewItem listViewItem in listView_Zones.Items)
            {
                if(listViewItem.Tag == zone)
                {
                    listView_Zones.SelectedItem = listViewItem;
                    break;
                }
            }
        }

        private void listView_Zones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
