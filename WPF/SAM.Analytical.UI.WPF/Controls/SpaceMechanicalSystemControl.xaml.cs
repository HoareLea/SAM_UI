using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SpaceZoneControl.xaml
    /// </summary>
    public partial class SpaceMechanicalSystemControl : UserControl
    {
        public SpaceMechanicalSystemControl()
        {
            InitializeComponent();

            mechanicalSystemControl.SelectionMode = SelectionMode.Single;
            mechanicalSystemControl.MechanicalSystemCategorySelectionChanged += ZonesControl_MechanicalSystemCategorySelectionChanged;
        }

        public SpaceMechanicalSystemControl(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null, Zone selectedZone = null)
        {
            InitializeComponent();

            SetSpaces(spaces);

            mechanicalSystemControl.SelectionMode = SelectionMode.Single;
            mechanicalSystemControl.AdjacencyCluster = adjacencyCluster;
            mechanicalSystemControl.MechanicalSystemCategorySelectionChanged += ZonesControl_MechanicalSystemCategorySelectionChanged;

            SetSelectedSpaces(selectedSpaces);
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return mechanicalSystemControl.AdjacencyCluster;
            }
            set
            {
                mechanicalSystemControl.AdjacencyCluster = value;
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return GetSpaces(false);
            }

            set
            { 
                SetSpaces(value);
            }
        }

        public List<Space> SelectedSpaces
        {
            get
            {
                return GetSpaces(true);
            }

            set
            {
                SetSelectedSpaces(value);
            }
        }

        public MechanicalSystem SelectedMechanicalSystem
        {
            get
            {
                return mechanicalSystemControl.SelectedMechanicalSystems?.FirstOrDefault();
            }

            set
            {
                mechanicalSystemControl.SelectedMechanicalSystems = value == null ? null : new List<MechanicalSystem>() { value };
            }
        }

        public string MechanicalSystemCategory
        {
            get 
            {
                return mechanicalSystemControl.MechanicalSystemCategory;
            }

            set
            {
                mechanicalSystemControl.MechanicalSystemCategory = value;
            }
        }

        private List<Space> GetSpaces(bool selected = true)
        {
            if(listView_Spaces.SelectedItems == null)
            {
                return null;
            }

            System.Collections.IList list = selected ? listView_Spaces.SelectedItems : listView_Spaces.Items;

            List<Space> result = new List<Space>();
            foreach(ListViewItem listViewItem in list)
            {
                Space space = listViewItem.Tag as Space;
                if(space == null)
                {
                    continue;
                }

                result.Add(space);
            }

            return result;
        }

        private void SetSpaces(IEnumerable<Space> spaces)
        {
            listView_Spaces.Items.Clear();

            if(spaces == null)
            {
                return;
            }

            foreach(Space space in spaces)
            {
                if(string.IsNullOrWhiteSpace(space?.Name))
                {
                    continue;
                }

                ListViewItem listViewItem = new ListViewItem() { Content = space.Name, Tag = space };
                listView_Spaces.Items.Add(listViewItem);
            }
        }

        private void SetSelectedSpaces(IEnumerable<Space> spaces)
        {
            if(spaces == null || listView_Spaces.Items == null)
            {
                return;
            }

            listView_Spaces.SelectedItems.Clear();


            foreach(ListViewItem listViewItem in listView_Spaces.Items)
            {
                Space space = listViewItem?.Tag as Space;
                if(space == null)
                {
                    continue;
                }

                foreach(Space space_Temp in spaces)
                {
                    if(space_Temp == null)
                    {
                        continue;
                    }

                    if(space_Temp.Guid == space.Guid)
                    {
                        listView_Spaces.SelectedItems.Add(listViewItem);
                        break;
                    }
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateMechanicalSystemsSelection()
        {
            List<Space> spaces = SelectedSpaces;
            if (spaces == null || spaces.Count == 0)
            {
                return;
            }

            List<MechanicalSystem> mechanicalSystems = mechanicalSystemControl.MechanicalSystems;
            if (mechanicalSystems == null || mechanicalSystems.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }


            foreach (Space space in spaces)
            {
                List<MechanicalSystem> mechanicalSystems_temp = adjacencyCluster.MechanicalSystems(space);
                if (mechanicalSystems_temp == null || mechanicalSystems_temp.Count == 0)
                {
                    mechanicalSystems = null;
                    break;
                }

                for (int i = mechanicalSystems.Count - 1; i >= 0; i--)
                {
                    if (mechanicalSystems_temp.Find(x => x.Guid == mechanicalSystems[i].Guid) == null)
                    {
                        mechanicalSystems.RemoveAt(i);
                    }
                }

                if (mechanicalSystems.Count == 0)
                {
                    break;
                }
            }

            mechanicalSystemControl.SelectedMechanicalSystems = mechanicalSystems;
        }

        private void ZonesControl_MechanicalSystemCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMechanicalSystemsSelection();
        }

        private void listView_Spaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMechanicalSystemsSelection();
        }
    }
}
