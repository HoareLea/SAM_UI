// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ZoneControl.xaml
    /// </summary>
    public partial class ZoneControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;
        private Zone zone;

        public ZoneControl()
        {
            InitializeComponent();
        }

        public ZoneControl(Zone zone)
        {
            this.zone = zone;

            InitializeComponent();
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
                LoadZoneCategories();
                LoadZone();
            }
        }

        private void LoadZone()
        {
            if(zone == null)
            {
                return;
            }

            textBox_Name.Text = zone?.Name;

            if(zone.TryGetValue(ZoneParameter.Color, out Core.SAMColor sAMColor) && sAMColor != null)
            {
                button_Color.Background = new SolidColorBrush(Core.UI.Convert.ToMedia(sAMColor.ToColor()));
            }

            textBox_ZoneType_Name.IsEnabled = false;
            textBox_ZoneType_Name.Text = string.Empty;

            if (zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory))
            {
                ZoneType zoneType = Core.Query.Enum<ZoneType>(zoneCategory);
                if(zoneType != ZoneType.Undefined)
                {
                    comboBox_ZoneType.SelectedItem = Core.Query.Description(zoneType);
                    if(zoneType == ZoneType.Other)
                    {
                        textBox_ZoneType_Name.IsEnabled = true;
                        textBox_ZoneType_Name.Text = zoneCategory;
                    }
                }
                else if(!string.IsNullOrWhiteSpace(zoneCategory))
                {
                    comboBox_ZoneType.SelectedItem = Core.Query.Description(ZoneType.Other);
                    textBox_ZoneType_Name.IsEnabled = true;
                    textBox_ZoneType_Name.Text = zoneCategory;
                }
            }

        }

        private void LoadZoneCategories()
        {
            comboBox_ZoneType.Items.Clear();
            comboBox_ZoneType.Items.Add("");
            List<string> zoneCategories = Query.ZoneCategories(adjacencyCluster);

            foreach (string zoneCategory in zoneCategories)
            {
                comboBox_ZoneType.Items.Add(zoneCategory);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadZoneCategories();
            LoadZone();

            textBox_Name.Focus();
            textBox_Name.SelectAll();
        }

        public Zone Zone
        {
            get
            {
                return GetZone();
            }
            set
            {
                zone = value;
                LoadZone();
            }
        }

        private Zone GetZone()
        {
            Zone result = zone;
            if(result != null)
            {
                result = new Zone(result);
            }

            result = result == null ? new Zone(textBox_Name.Text) : new Zone(result, textBox_Name.Text);

            string zoneCategory = comboBox_ZoneType?.SelectedItem?.ToString();
            if(string.IsNullOrWhiteSpace(zoneCategory))
            {
                result.RemoveValue(ZoneParameter.ZoneCategory);
            }
            else
            {
                ZoneType zoneType = Core.Query.Enum<ZoneType>(zoneCategory);
                if(zoneType == ZoneType.Other)
                {
                    result.SetValue(ZoneParameter.ZoneCategory, textBox_ZoneType_Name.Text);
                }
                else
                {
                    result.SetValue(ZoneParameter.ZoneCategory, zoneCategory);
                }
            }

            SolidColorBrush solidColorBrush = button_Color.Background as SolidColorBrush;
            if(solidColorBrush != null)
            {
                Color color = solidColorBrush.Color;
                result.SetValue(ZoneParameter.Color, new Core.SAMColor(Core.UI.Convert.ToDrawing(color)));
            }

            return result;
        }

        private void comboBox_ZoneType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ZoneType zoneType = Core.Query.Enum<ZoneType>(comboBox_ZoneType?.SelectedItem?.ToString());

            if(zoneType == ZoneType.Other)
            {
                textBox_ZoneType_Name.IsEnabled = true;
                textBox_ZoneType_Name.Text = string.Empty;
            }
            else
            {
                textBox_ZoneType_Name.IsEnabled = false;
                textBox_ZoneType_Name.Text = string.Empty;
            }
        }

        private void button_Color_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                button_Color.Background = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }
    }
}
