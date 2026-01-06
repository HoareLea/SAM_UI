// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SpacesControl.xaml
    /// </summary>
    public partial class SpacesControl : UserControl
    {
        public event AdjacencyClusterSelectionChangedEventHandler<Space> AdjacencyClusterSelectionChanged;

        private AdjacencyCluster adjacencyCluster;
        private List<Space> spaces;

        public SpacesControl()
        {
            InitializeComponent();
        }

        public List<Space> Spaces
        {
            get
            {
                return spaces;
            }

            set
            {
                spaces = value;
                LoadSpaces(true);
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
                LoadZoneCategories();
                LoadSpaces();
            }
        }

        public List<Space> GetSelectedSpaces()
        {
            if(TreeView_Main.Items == null)
            {
                return null;
            }

            List<Space> result = new List<Space>();
            foreach(object @object in TreeView_Main.Items)
            {
                TreeViewItem treeViewItem = @object as TreeViewItem;
                if(treeViewItem == null)
                {
                    continue;
                }

                List<Space> spaces = GetSelectedSpaces(treeViewItem);
                if (spaces != null && spaces.Count != 0)
                {
                    result.AddRange(spaces);
                }

                CheckBox checkBox = treeViewItem.Header as CheckBox;
                if(checkBox == null)
                {
                    continue;
                }

                if(checkBox.IsChecked == null || !checkBox.IsChecked.HasValue || !checkBox.IsChecked.Value)
                {
                    continue;
                }

                Space space = treeViewItem.Tag as Space;
                if(space == null)
                {
                    continue;
                }

                result.Add(space);
            }

            return result;
        }

        public List<Space> GetSelectedSpaces(TreeViewItem treeViewItem)
        {
            if (treeViewItem?.Items == null)
            {
                return null;
            }

            List<Space> result = new List<Space>();
            foreach (object @object in treeViewItem.Items)
            {
                TreeViewItem treeViewItem_Temp = @object as TreeViewItem;
                if (treeViewItem_Temp == null)
                {
                    continue;
                }

                List<Space> spaces = GetSelectedSpaces(treeViewItem_Temp);
                if (spaces != null && spaces.Count != 0)
                {
                    result.AddRange(spaces);
                }

                CheckBox checkBox = treeViewItem_Temp.Header as CheckBox;
                if (checkBox == null)
                {
                    continue;
                }

                if (checkBox.IsChecked == null || !checkBox.IsChecked.HasValue || !checkBox.IsChecked.Value)
                {
                    continue;
                }

                Space space = treeViewItem_Temp.Tag as Space;
                if (space == null)
                {
                    continue;
                }

                result.Add(space);
            }

            return result;
        }

        private void LoadZoneCategories()
        {
            ComboBoxItem comboBoxItem_Selected = ComboBox_ZoneCategory.SelectedItem as ComboBoxItem;

            ComboBox_ZoneCategory.Items.Clear();

            ComboBoxItem comboBoxItem_All = new ComboBoxItem() { Content = "<All>", Tag = null };

            ComboBox_ZoneCategory.Items.Add(comboBoxItem_All);

            HashSet<string> zoneCategories = adjacencyCluster?.GetZoneCategories();
            if (zoneCategories != null)
            {
                List<string> zoneCategories_Sorted = new List<string>(zoneCategories);
                zoneCategories_Sorted.Sort();
                foreach (string zoneCategory in zoneCategories_Sorted)
                {
                    ComboBox_ZoneCategory.Items.Add(new ComboBoxItem() { Content = zoneCategory, Tag = zoneCategory });
                }
            }


            if (ComboBox_ZoneCategory.Items.Count < 2)
            {
                ComboBox_ZoneCategory.SelectedItem = comboBoxItem_All;
                ComboBox_ZoneCategory.IsEnabled = false;
                return;
            }

            ComboBox_ZoneCategory.IsEnabled = true;
            SelectedZoneCategory = comboBoxItem_Selected?.Tag as string;
        }

        public string SelectedZoneCategory
        {
            get
            {
                return (ComboBox_ZoneCategory?.SelectedItem as ComboBoxItem)?.Tag as string;
            }

            set
            {
                if (ComboBox_ZoneCategory.Items == null)
                {
                    return;
                }

                foreach (ComboBoxItem comboBoxItem in ComboBox_ZoneCategory.Items)
                {
                    if (comboBoxItem.Tag as string == value)
                    {
                        ComboBox_ZoneCategory.SelectedItem = comboBoxItem;
                        return;
                    }
                }
            }
        }

        private void LoadSpaces(bool selectAll = false)
        {
            List<Space> selectedSpaces = GetSelectedSpaces();

            TreeView_Main.Items.Clear();

            if ((spaces == null || spaces.Count == 0) && adjacencyCluster == null)
            {
                return;
            }

            List<Space> spaces_Temp = adjacencyCluster?.GetSpaces();
            if (spaces != null)
            {
                for (int i = spaces_Temp.Count - 1; i >= 0; i--)
                {
                    if (spaces.Find(x => x.Guid == spaces_Temp[i].Guid) != null)
                    {
                        continue;
                    }

                    spaces_Temp.RemoveAt(i);
                }
            }

            if (spaces_Temp == null || spaces_Temp.Count == 0)
            {
                return;
            }

            Dictionary<string, List<Space>> dictionary = new Dictionary<string, List<Space>>();

            string zoneCategory = SelectedZoneCategory;
            if (zoneCategory == null)
            {
                dictionary["All"] = spaces_Temp;
            }
            else
            {
                List<Zone> zones = adjacencyCluster.GetObjects<Zone>();
                if (zones != null && zones.Count != 0)
                {
                    foreach (Zone zone in zones)
                    {
                        if (zone == null)
                        {
                            continue;
                        }

                        string name = zone.Name;
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = "???";
                        }

                        if (!zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory_Temp) || zoneCategory_Temp != zoneCategory)
                        {
                            continue;
                        }

                        List<Space> spaces_Zone = adjacencyCluster.GetSpaces(zone);
                        spaces_Zone?.RemoveAll(x => spaces_Temp.Find(y => y.Guid == x.Guid) == null);

                        dictionary[name] = spaces_Zone;
                    }
                }
            }

            foreach (KeyValuePair<string, List<Space>> keyValuePair in dictionary)
            {
                CheckBox checkBox_ZoneCategory = new CheckBox() { Content = keyValuePair.Key };

                checkBox_ZoneCategory.Checked += CheckBox_ZoneCategory_Changed;
                checkBox_ZoneCategory.Unchecked += CheckBox_ZoneCategory_Changed;

                TreeViewItem treeViewItem = new TreeViewItem() { Header = checkBox_ZoneCategory, Tag = keyValuePair.Key };
                treeViewItem.IsExpanded = true;

                if (keyValuePair.Value != null)
                {
                    List<Tuple<CheckBox, bool>> tuples = new List<Tuple<CheckBox, bool>>();
                    foreach (Space space in keyValuePair.Value)
                    {
                        if (space == null)
                        {
                            continue;
                        }

                        string name = string.IsNullOrWhiteSpace(space.Name) ? "???" : space.Name;

                        CheckBox checkBox_Space = new CheckBox() { Content = name };

                        TreeViewItem treeViewItem_Space = new TreeViewItem() { Header = checkBox_Space, Tag = space };

                        treeViewItem.Items.Add(treeViewItem_Space);

                        checkBox_Space.Checked += CheckBox_Space_Changed;
                        checkBox_Space.Unchecked += CheckBox_Space_Changed;

                        tuples.Add(new Tuple<CheckBox, bool>(checkBox_Space, selectAll || selectedSpaces == null || selectedSpaces.Find(x => x.Guid == space.Guid) != null));
                    }

                    foreach(Tuple<CheckBox, bool> tuple in tuples)
                    {
                        tuple.Item1.IsChecked = tuple.Item2;
                    }
                }

                TreeView_Main.Items.Add(treeViewItem);
            }

            //AdjacencyClusterSelectionChanged.Invoke(this, new AdjacencyClusterSelectionChangedEventArgs<Space>(adjacencyCluster, GetSelectedSpaces()));
        }

        private void CheckBox_ZoneCategory_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            TreeViewItem treeViewItem = checkBox.Parent as TreeViewItem;
            if (treeViewItem == null)
            {
                return;
            }

            CheckBox checkBox_Main = treeViewItem?.Header as CheckBox;
            if (checkBox_Main == null)
            {
                return;
            }

            if (treeViewItem.Items == null)
            {
                return;
            }

            foreach (object @object in treeViewItem.Items)
            {
                CheckBox checkBox_Temp = (@object as TreeViewItem)?.Header as CheckBox;
                if (checkBox_Temp == null)
                {
                    continue;
                }

                if(checkBox_Temp.IsChecked != checkBox_Main.IsChecked)
                {
                    checkBox_Temp.Checked -= CheckBox_Space_Changed;
                    checkBox_Temp.Unchecked -= CheckBox_Space_Changed;
                    checkBox_Temp.IsChecked = checkBox_Main.IsChecked;
                    checkBox_Temp.Checked += CheckBox_Space_Changed;
                    checkBox_Temp.Unchecked += CheckBox_Space_Changed;
                }
            }

            AdjacencyClusterSelectionChanged.Invoke(this, new AdjacencyClusterSelectionChangedEventArgs<Space>(adjacencyCluster, GetSelectedSpaces()));
        }

        private void CheckBox_Space_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if(checkBox == null)
            {
                return;
            }

            TreeViewItem treeViewItem_ZoneCategory = (checkBox?.Parent as TreeViewItem)?.Parent as TreeViewItem;

            CheckBox checkBox_ZoneCategory = treeViewItem_ZoneCategory.Header as CheckBox;
            if (checkBox_ZoneCategory != null)
            {
                if (AllChildsSelected(treeViewItem_ZoneCategory))
                {
                    checkBox_ZoneCategory.IsChecked = checkBox.IsChecked;
                }
                else
                {
                    if (checkBox_ZoneCategory.IsChecked != null && checkBox_ZoneCategory.IsChecked.Value)
                    {
                        checkBox_ZoneCategory.Checked -= CheckBox_ZoneCategory_Changed;
                        checkBox_ZoneCategory.Unchecked -= CheckBox_ZoneCategory_Changed;
                        checkBox_ZoneCategory.IsChecked = false;
                        checkBox_ZoneCategory.Checked += CheckBox_ZoneCategory_Changed;
                        checkBox_ZoneCategory.Unchecked += CheckBox_ZoneCategory_Changed;

                    }
                }
            }

            AdjacencyClusterSelectionChanged.Invoke(this, new AdjacencyClusterSelectionChangedEventArgs<Space>(adjacencyCluster, GetSelectedSpaces()));
        }

        private void ComboBox_ZoneCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSpaces();
        }

        private void Button_None_Click(object sender, RoutedEventArgs e)
        {
            Select(false);
        }

        private void Button_All_Click(object sender, RoutedEventArgs e)
        {
            Select(true);
        }

        private void Select(bool selected)
        {
            if(TreeView_Main?.Items == null || TreeView_Main.Items.Count == 0)
            {
                return;
            }

            foreach (object @object in TreeView_Main.Items)
            {
                TreeViewItem treeViewItem_Temp = @object as TreeViewItem;
                if (treeViewItem_Temp == null)
                {
                    continue;
                }

                Select(treeViewItem_Temp, selected);

                CheckBox checkBox = treeViewItem_Temp.Header as CheckBox;
                if (checkBox == null)
                {
                    continue;
                }

                checkBox.IsChecked = selected;
            }
        }

        private void Select(TreeViewItem treeViewItem, bool selected)
        {
            if (treeViewItem?.Items == null || treeViewItem.Items.Count == 0)
            {
                return;
            }

            foreach (object @object in treeViewItem.Items)
            {
                TreeViewItem treeViewItem_Temp = @object as TreeViewItem;
                if (treeViewItem_Temp == null)
                {
                    continue;
                }

                Select(treeViewItem_Temp, selected);

                CheckBox checkBox = treeViewItem_Temp.Header as CheckBox;
                if (checkBox == null)
                {
                    continue;
                }

                checkBox.IsChecked = selected;
            }
        }
    
        private bool AllChildsSelected(TreeViewItem treeViewItem)
        {
            if(treeViewItem?.Items == null)
            {
                return false;
            }

            bool selected = false;

            int count = 0;
            foreach(object @object in treeViewItem.Items)
            {
                CheckBox checkBox = (@object as TreeViewItem).Header as CheckBox;
                if (checkBox == null)
                {
                    continue;
                }

                bool selected_Current = checkBox.IsChecked != null && checkBox.IsChecked.Value;

                if (count == 0)
                {
                    selected = selected_Current;
                }

                if (selected != selected_Current)
                {
                    return false;
                }

                count++;
            }

            return count > 0;
        }
    }
}
