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
            comboBox_ZoneType.Items.Clear();
            comboBox_ZoneType.Items.Add(string.Empty);

            //HashSet<string> zoneCategories = new HashSet<string>();
            //foreach (ZoneType zoneType in Enum.GetValues(typeof(ZoneType)))
            //{
            //    if (zoneType == ZoneType.Undefined)
            //    {
            //        continue;
            //    }

            //    zoneCategories.Add(Core.Query.Description(zoneType));
            //}

            //List<Zone> zones = adjacencyCluster?.GetObjects<Zone>();
            //if (zones != null && zones.Count != 0)
            //{
            //    foreach (Zone zone in zones)
            //    {
            //        if (!zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrWhiteSpace(zoneCategory))
            //        {
            //            continue;
            //        }

            //        ZoneType zoneType = Core.Query.Enum<ZoneType>(zoneCategory);
            //        if(zoneType == ZoneType.Undefined || zoneType == ZoneType.Other)
            //        {
            //            zoneCategories.Add(zoneCategory);
            //        }
            //    }
            //}

            List<string> zoneCategories = Query.ZoneCategories(adjacencyCluster);

            foreach (string zoneCategory in zoneCategories)
            {
                comboBox_ZoneType.Items.Add(zoneCategory);
            }
        }

        private void button_Add_Click(object sender, RoutedEventArgs e)
        {
            Zone zone = new Zone("New Zone");
            zone.SetValue(ZoneParameter.ZoneCategory, ZoneCategory);

            ZoneWindow zoneWindow = new ZoneWindow(zone, adjacencyCluster);
            bool? result = zoneWindow.ShowDialog();
            if(result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            zone = zoneWindow.Zone;
            if(zone == null)
            {
                return;
            }

            adjacencyCluster.AddObject(zone);
            LoadZoneCategories();

            if(zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory))
            {
                comboBox_ZoneType.SelectedItem = zoneCategory;
            }

            LoadZones();
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
        }

        private void comboBox_ZoneType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadZones();
        }
    }
}
