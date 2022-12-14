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
    /// Interaction logic for ZonesControl.xaml
    /// </summary>
    public partial class ZonesControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;

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
            LoadZoneCategories();
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
                return comboBox_ZoneType?.SelectedItem?.ToString();
            }

            set
            {
                LoadZoneCategories();
                comboBox_ZoneType.SelectedItem = value;
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
            object selectedItem = comboBox_ZoneType.SelectedItem;

            comboBox_ZoneType.Items.Clear();
            comboBox_ZoneType.Items.Add(string.Empty);

            List<string> zoneCategories = Query.ZoneCategories(adjacencyCluster);

            foreach (string zoneCategory in zoneCategories)
            {
                comboBox_ZoneType.Items.Add(zoneCategory);
            }

            comboBox_ZoneType.SelectedItem = selectedItem;
        }

        public List<Zone> Zones
        {
            get
            {
                List<Zone> result = new List<Zone>();
                foreach(ListViewItem listViewItem in listView_Zones.SelectedItems)
                {
                    Zone zone = listViewItem?.Tag as Zone;
                    if(zone == null)
                    {
                        continue;
                    }

                    result.Add(zone);
                }

                return result;
            }

            set
            {
                if(value == null)
                {
                    return;
                }

                foreach (ListViewItem listViewItem in listView_Zones.Items)
                {
                    Zone zone = listViewItem?.Tag as Zone;
                    if (zone == null)
                    {
                        continue;
                    }

                    if(value.Find(x => x.Guid == zone.Guid) != null)
                    {
                        listView_Zones.SelectedItems.Add(listViewItem);
                    }
                }
            }
        }

        private void comboBox_ZoneType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadZones();
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
                comboBox_ZoneType.SelectedItem = zoneCategory;
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
                comboBox_ZoneType.SelectedItem = zoneCategory;
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
    }
}
