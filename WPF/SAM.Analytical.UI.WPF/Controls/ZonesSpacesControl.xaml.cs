// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ZonesControl.xaml
    /// </summary>
    public partial class ZonesSpacesControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;

        public event SelectionChangedEventHandler ZoneCategorySelectionChanged;

        public ZonesSpacesControl()
        {
            InitializeComponent();
        }

        public ZonesSpacesControl(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadZoneCategories();
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
            treeView_Zones.Items.Clear();

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

                    TreeViewItem listViewItem = new TreeViewItem() { Header = zone.Name, Tag = zone };

                    treeView_Zones.Items.Add(listViewItem);
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
            if(selected)
            {
                TreeViewItem treeViewItem = treeView_Zones.SelectedItem as TreeViewItem;
                if(treeViewItem == null)
                {
                    return null;
                }

                if (treeViewItem.Tag is Space)
                {
                    treeViewItem = treeViewItem.Parent as TreeViewItem;
                }

                if (treeViewItem.Tag is Zone)
                {
                    return new List<Zone>() { (Zone)treeViewItem.Tag };
                }

                return null;
            }

            List<Zone> result = new List<Zone>();
            foreach (TreeViewItem treeViewItem_Temp in treeView_Zones.Items)
            {
                if (treeViewItem_Temp.Tag is Zone)
                {
                    return new List<Zone>() { (Zone)treeViewItem_Temp.Tag };
                }
            }

            return result;
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
        }
    }
}
