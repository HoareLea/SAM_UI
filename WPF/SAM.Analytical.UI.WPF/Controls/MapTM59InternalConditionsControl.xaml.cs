// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MapTM59InternalConditionsControl.xaml
    /// </summary>
    public partial class MapTM59InternalConditionsControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;

        public MapTM59InternalConditionsControl()
        {
            InitializeComponent();

            Load();
        }

        public MapTM59InternalConditionsControl(IEnumerable<Space> spaces, AdjacencyCluster adjacencyCluster, TextMap textMap = null, InternalConditionLibrary internalConditionLibrary = null)
        {
            InitializeComponent();

            this.adjacencyCluster = adjacencyCluster;

            mapInternalConditionsControl.TextMap = textMap;
            mapInternalConditionsControl.InternalConditionLibrary = internalConditionLibrary;

            mapInternalConditionsControl.Spaces = spaces?.ToList();

            Load();
        }

        private void Load()
        {
            LoadZones();
            SetMapFunc();
        }

        private void LoadZones()
        {
            string value = comboBox_ZoneType.Text;

            comboBox_ZoneType.Items.Clear();

            List<Zone> zones = adjacencyCluster?.GetZones();
            if(zones == null || zones.Count == 0)
            {
                return;
            }

            HashSet<string> categories = new HashSet<string>();
            foreach(Zone zone in zones)
            {
                if(zone.TryGetValue(ZoneParameter.ZoneCategory, out string category) && !string.IsNullOrWhiteSpace(category))
                {
                    categories.Add(category);
                }
            }

            foreach(string category in categories)
            {
                comboBox_ZoneType.Items.Add(category);
            }

            if(!string.IsNullOrWhiteSpace(value))
            {
                comboBox_ZoneType.Text = value;
            }

        }

        public TextMap TextMap
        {
            get
            {
                return mapInternalConditionsControl.TextMap;
            }

            set
            {
                mapInternalConditionsControl.TextMap = value;
                SetMapFunc();
            }
        }

        public InternalConditionLibrary InternalConditionLibrary
        {
            get
            {
                return mapInternalConditionsControl.InternalConditionLibrary;
            }

            set
            {
                mapInternalConditionsControl.InternalConditionLibrary = value;
                SetMapFunc();
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return mapInternalConditionsControl.Spaces;
            }

            set
            {
                mapInternalConditionsControl.Spaces = value;
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
                SetAdjacencyCluster(value);
            }
        }

        private void SetAdjacencyCluster(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;
            Load();
        }

        private void comboBox_ZoneType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Space> spaces = Spaces;

            mapInternalConditionsControl.GroupFunc = new Func<Space, string>(x => 
            {
                Zone zone = adjacencyCluster?.GetZones(x, comboBox_ZoneType.SelectedItem as string)?.FirstOrDefault();
                if(zone == null)
                {
                    return null;
                }

                return zone.Name;
            });

            SetMapFunc();
        }

        public void SetMapFunc()
        {
            TextMap textMap = TextMap;
            InternalConditionLibrary internalConditionLibrary = InternalConditionLibrary;
            string zoneType = comboBox_ZoneType.SelectedItem as string;

            TM59Manager tM59Manager = new TM59Manager(textMap);

            mapInternalConditionsControl.MapFunc = new Func<Space, InternalCondition>(x => 
            {
                return tM59Manager.GetInternalCondition(adjacencyCluster, internalConditionLibrary, x, zoneType);
            });
        }

        public List<Space> GetSpaces(bool selected = false)
        {
            return mapInternalConditionsControl.GetSpaces(selected);
        }
    }
}
