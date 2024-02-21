using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ZonesControl.xaml
    /// </summary>
    public partial class MechanicalSystemsControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;

        public event SelectionChangedEventHandler MechanicalSystemCategorySelectionChanged;
        public event AdjacencyClusterChangedEventHandler AdjacencyClusterChanged;

        public MechanicalSystemsControl()
        {
            InitializeComponent();
        }

        public MechanicalSystemsControl(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMechanicalSystemCategories();
        }

        public SelectionMode SelectionMode
        {
            get
            {
                return listView_MechanicalSystems.SelectionMode;
            }

            set
            {
                listView_MechanicalSystems.SelectionMode = value;
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
                LoadMechanicalSystems();
            }
        }

        public string MechanicalSystemCategory
        {
            get
            {
                return comboBox_MechanicalSystemCategory?.SelectedItem?.ToString();
            }

            set
            {
                LoadMechanicalSystemCategories();
                comboBox_MechanicalSystemCategory.SelectedItem = value;
            }
        }

        private void LoadMechanicalSystems()
        {
            listView_MechanicalSystems.Items.Clear();

            if (adjacencyCluster == null)
            {
                return;
            }

            List<MechanicalSystem> mechanicalSystems = adjacencyCluster?.GetObjects<MechanicalSystem>();
            if(mechanicalSystems != null && mechanicalSystems.Count != 0)
            {
                foreach(MechanicalSystem mechanicalSystem in mechanicalSystems)
                {
                    if(mechanicalSystem == null)
                    {
                        continue;
                    }

                    if(MechanicalSystemCategory != Core.Query.Description(mechanicalSystem.MechanicalSystemCategory()))
                    {
                        continue;
                    }

                    ListViewItem listViewItem = new ListViewItem() { Content = mechanicalSystem.FullName, Tag = mechanicalSystem };

                    listView_MechanicalSystems.Items.Add(listViewItem);
                }
            }

        }

        private void LoadMechanicalSystemCategories()
        {
            object selectedItem = comboBox_MechanicalSystemCategory.SelectedItem;
            List<MechanicalSystem> selectedMechanicalSystems = SelectedMechanicalSystems;

            comboBox_MechanicalSystemCategory.Items.Clear();
            comboBox_MechanicalSystemCategory.Items.Add(string.Empty);

            IEnumerable<MechanicalSystemCategory> mechanicalSystemCategories = System.Enum.GetValues(typeof(MechanicalSystemCategory)).Cast<MechanicalSystemCategory>();

            foreach (MechanicalSystemCategory mechanicalSystemCategory in mechanicalSystemCategories)
            {
                if(mechanicalSystemCategory == Analytical.MechanicalSystemCategory.Undefined)
                {
                    continue;
                }

                string name = Core.Query.Description(mechanicalSystemCategory);

                comboBox_MechanicalSystemCategory.Items.Add(name);
            }

            comboBox_MechanicalSystemCategory.SelectedItem = selectedItem;
            SelectedMechanicalSystems = selectedMechanicalSystems;
        }

        public List<MechanicalSystem> MechanicalSystems
        {
            get
            {
                return GetMechanicalSystems(false);
            }
        }

        private List<MechanicalSystem> GetMechanicalSystems(bool selected = true)
        {
            System.Collections.IList list = selected ? listView_MechanicalSystems.SelectedItems : listView_MechanicalSystems.Items;
            if(list == null)
            {
                return null;
            }

            List <MechanicalSystem> result = new List<MechanicalSystem>();
            foreach (ListViewItem listViewItem in list)
            {
                MechanicalSystem mechanicalSystem = listViewItem?.Tag as MechanicalSystem;
                if (mechanicalSystem == null)
                {
                    continue;
                }

                result.Add(mechanicalSystem);
            }

            return result;
        }

        public List<MechanicalSystem> SelectedMechanicalSystems
        {
            get
            {
                return GetMechanicalSystems(true);
            }

            set
            {
                SetSelectedMechanicalSystems(value);
            }
        }

        private void SetSelectedMechanicalSystems(IEnumerable<MechanicalSystem> mechanicalSystems)
        {
            if(listView_MechanicalSystems.SelectionMode == SelectionMode.Single)
            {
                listView_MechanicalSystems.SelectedItem = null;
            }
            else
            {
                listView_MechanicalSystems.SelectedItems.Clear();
            }

            if(comboBox_MechanicalSystemCategory.Items.Count == 0)
            {
                LoadMechanicalSystemCategories();
            }

            if (mechanicalSystems == null)
            {
                return;
            }

            foreach (MechanicalSystem mechanicalSystem in mechanicalSystems)
            {
                if (mechanicalSystem != null)
                {
                    string mechanicalSystemCategory = Core.Query.Description(Analytical.Query.MechanicalSystemCategory(mechanicalSystem)); 

                    if (comboBox_MechanicalSystemCategory.Items.Contains(mechanicalSystemCategory) && MechanicalSystemCategory != mechanicalSystemCategory)
                    {
                        MechanicalSystemCategory = mechanicalSystemCategory;
                        break;
                    }
                }
            }

            foreach (ListViewItem listViewItem in listView_MechanicalSystems.Items)
            {
                MechanicalSystem mechanicalSystem = listViewItem?.Tag as MechanicalSystem;
                if (mechanicalSystem == null)
                {
                    continue;
                }

                foreach(MechanicalSystem mechanicalSystem_Temp in mechanicalSystems)
                {
                    if(mechanicalSystem_Temp != null && mechanicalSystem_Temp.Guid == mechanicalSystem.Guid)
                    {

                        if (listView_MechanicalSystems.SelectionMode == SelectionMode.Single)
                        {
                            listView_MechanicalSystems.SelectedItem = listViewItem;
                            return;
                        }
                        else
                        {
                            listView_MechanicalSystems.SelectedItems.Add(listViewItem);
                            break;
                        }

                    }
                }
            }
        }

        private void comboBox_MechanicalSystemCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadMechanicalSystems();
            MechanicalSystemCategorySelectionChanged?.Invoke(this, e);
        }

        private void button_Edit_Click(object sender, RoutedEventArgs e)
        {
            //List<Zone> zones = SelectedZones;
            //if(zones == null || zones.Count != 1)
            //{
            //    MessageBox.Show("Select single Zone");
            //    return;
            //}

            //ZoneWindow zoneWindow = new ZoneWindow(zones[0], adjacencyCluster);
            //bool? result = zoneWindow.ShowDialog();
            //if (result == null || !result.HasValue || !result.Value)
            //{
            //    return;
            //}

            //Zone zone = zoneWindow.Zone;
            //if (zone == null)
            //{
            //    return;
            //}

            //adjacencyCluster.AddObject(zone);
            //LoadMechanicalSystemCategories();

            //if (zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory))
            //{
            //    comboBox_ZoneCategory.SelectedItem = zoneCategory;
            //}

            //LoadMechanicalSystems();
        }

        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            List<MechanicalSystem> mechanicalSystems = SelectedMechanicalSystems;
            if (mechanicalSystems == null || mechanicalSystems.Count == 0)
            {
                MessageBox.Show("Select Mechanical Systems");
                return;
            }

            if(MessageBox.Show("Are you sure to remove selected MechanicalSystems?", "Remove MechanicalSystems", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            foreach(MechanicalSystem mechanicalSystem in mechanicalSystems)
            {
                adjacencyCluster.RemoveObject<MechanicalSystem>(mechanicalSystem.Guid);
            }

            LoadMechanicalSystemCategories();
            LoadMechanicalSystems();

            AdjacencyClusterChanged?.Invoke(this, new AdjacencyClusterChangedEventArgs(adjacencyCluster));
        }

        private void button_Add_Click(object sender, RoutedEventArgs e)
        {
            SystemTypeLibrary systemTypeLibrary = Analytical.Query.DefaultSystemTypeLibrary();

            List<MechanicalSystemType> mechanicalSystemTypes = systemTypeLibrary.MechanicalSystemTypes(Core.Query.Enum<MechanicalSystemCategory>(MechanicalSystemCategory));
            if(mechanicalSystemTypes == null || mechanicalSystemTypes.Count == 0)
            {
                return;
            }


            MechanicalSystemType mechanicalSystemType = null;
            using (Core.Windows.Forms.ComboBoxForm<MechanicalSystemType> comboBoxForm = new Core.Windows.Forms.ComboBoxForm<MechanicalSystemType>("Select System Type", mechanicalSystemTypes, x => x.Name))
            {
                comboBoxForm.TopMost = true;
                if (comboBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                mechanicalSystemType = comboBoxForm.SelectedItem;
            }

            if(mechanicalSystemType == null)
            {
                return;
            }

            MechanicalSystem mechanicalSystem = adjacencyCluster?.AddMechanicalSystem(mechanicalSystemType, new List<Space>());
            if (mechanicalSystem == null)
            {
                return;
            }

            if(mechanicalSystem is VentilationSystem)
            {

            }

            LoadMechanicalSystemCategories();
            LoadMechanicalSystems();

            if(SelectionMode == SelectionMode.Single)
            {
                listView_MechanicalSystems.SelectedItem = null;
            }
            else
            {
                listView_MechanicalSystems.SelectedItems.Clear();
            }

            foreach(ListViewItem listViewItem in listView_MechanicalSystems.Items)
            {
                if(listViewItem.Tag == mechanicalSystem)
                {
                    listView_MechanicalSystems.SelectedItem = listViewItem;
                    break;
                }
            }

            AdjacencyClusterChanged?.Invoke(this, new AdjacencyClusterChangedEventArgs(adjacencyCluster));
        }

        private void listView_MechanicalSystems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
