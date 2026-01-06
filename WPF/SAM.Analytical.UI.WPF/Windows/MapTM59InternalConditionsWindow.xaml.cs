// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MapTM59InternalConditionsWindow.xaml
    /// </summary>
    public partial class MapTM59InternalConditionsWindow : System.Windows.Window
    {
        public MapTM59InternalConditionsWindow()
        {
            InitializeComponent();
        }

        public MapTM59InternalConditionsWindow(IEnumerable<Space> spaces, AdjacencyCluster adjacencyCluster, TextMap textMap = null, InternalConditionLibrary internalConditionLibrary = null)
        {
            InitializeComponent();

            mapTM59InternalConditionsControl.AdjacencyCluster = adjacencyCluster;

            mapTM59InternalConditionsControl.TextMap = textMap;
            mapTM59InternalConditionsControl.InternalConditionLibrary = internalConditionLibrary;
            mapTM59InternalConditionsControl.Spaces = spaces == null ? null : new List<Space>(spaces);
        }

        public List<Space> Spaces
        {
            get
            {
                return mapTM59InternalConditionsControl.Spaces;
            }
        }

        public List<Space> GetSpaces(bool selected = false)
        {
            return mapTM59InternalConditionsControl.GetSpaces(selected);
        }

        private void button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
