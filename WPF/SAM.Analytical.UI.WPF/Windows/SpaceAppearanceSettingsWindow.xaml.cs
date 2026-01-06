// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Interaction logic for SpaceAppearanceSettingsWindow.xaml
    /// </summary>
    public partial class SpaceAppearanceSettingsWindow : System.Windows.Window
    {
        public SpaceAppearanceSettingsWindow()
        {
            InitializeComponent();
        }

        public SpaceAppearanceSettingsWindow(AdjacencyCluster adjacencyCluster, SpaceAppearanceSettings spaceAppearanceSettings)
        {
            InitializeComponent();

            AdjacencyCluster = adjacencyCluster;
            SpaceAppearanceSettings = spaceAppearanceSettings;
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return spaceAppearanceSettingsControl.AdjacencyCluster;
            }

            set
            {
                spaceAppearanceSettingsControl.AdjacencyCluster = value;
            }
        }

        public SpaceAppearanceSettings SpaceAppearanceSettings
        {
            get
            {
                return spaceAppearanceSettingsControl.SpaceAppearanceSettings;
            }

            set
            {
                spaceAppearanceSettingsControl.SpaceAppearanceSettings = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
