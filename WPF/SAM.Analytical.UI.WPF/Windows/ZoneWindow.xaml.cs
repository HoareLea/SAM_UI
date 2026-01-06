// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ZoneWindow.xaml
    /// </summary>
    public partial class ZoneWindow : System.Windows.Window
    {
        public ZoneWindow()
        {
            InitializeComponent();
        }

        public ZoneWindow(Zone zone, AdjacencyCluster adjacencyCluster = null)
        {
            InitializeComponent();

            zoneControl.AdjacencyCluster = adjacencyCluster;
            zoneControl.Zone = zone;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            Zone zone = Zone;
            if(zone == null)
            {
                MessageBox.Show("Zone is invalid");
                return;
            }

            if(!zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrWhiteSpace(zoneCategory))
            {
                MessageBox.Show("Provide valid zone type");
                return;
            }

            AdjacencyCluster adjacencyCluster = zoneControl.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                List<Zone> zones = adjacencyCluster.GetZones();
                if(zones != null && zones.Count != 0)
                {
                    foreach(Zone zone_Temp in zones)
                    {
                        if(zone_Temp == null)
                        {
                            continue;
                        }

                        if(!zone_Temp.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory_Temp) || string.IsNullOrWhiteSpace(zoneCategory_Temp))
                        {
                            continue;
                        }

                        if(!zoneCategory.Equals(zoneCategory_Temp))
                        {
                            continue;
                        }

                        if(zone.Name == zone_Temp.Name && zone.Guid != zone_Temp.Guid)
                        {
                            MessageBox.Show("Zone with the same name already exists. Please provide different name.");
                            return;
                        }
                    }
                }
            }

            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public Zone Zone
        {
            get
            {
                return zoneControl.Zone;
            }

            set
            {
                zoneControl.Zone = value;
            }
        }
    }
}
