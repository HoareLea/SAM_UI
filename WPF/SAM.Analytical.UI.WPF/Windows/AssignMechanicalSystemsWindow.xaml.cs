using SAM.Analytical.Windows.Controls;
using SAM.Core.UI.WPF;
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
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AssignMechanicalSystemsWindow.xaml
    /// </summary>
    public partial class AssignMechanicalSystemsWindow : System.Windows.Window
    {
        public event AdjacencyClusterChangedEventHandler AdjacencyClusterChanged;

        public AssignMechanicalSystemsWindow()
        {
            InitializeComponent();

            SpacesControl_Main.AdjacencyClusterSelectionChanged += SpacesControl_Main_AdjacencyClusterSelectionChanged;
            MechanicalSystemsControl_Main.AdjacencyClusterChanged += MechanicalSystemsControl_Main_AdjacencyClusterChanged;

            MechanicalSystemsControl_Main.SelectionMode = SelectionMode.Single;
        }

        private void MechanicalSystemsControl_Main_AdjacencyClusterChanged(object sender, AdjacencyClusterChangedEventArgs e)
        {
            SpacesControl_Main.AdjacencyCluster = SpacesControl_Main.AdjacencyCluster;
        }

        private void SpacesControl_Main_AdjacencyClusterSelectionChanged(object sender, AdjacencyClusterSelectionChangedEventArgs<Space> e)
        {
            List<Space> spaces = e.GetSAMObjects();

            Modify.UpdateMechanicalSystemsSelection(MechanicalSystemsControl_Main, AdjacencyCluster, spaces);
        }
        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return MechanicalSystemsControl_Main.AdjacencyCluster;
            }

            set
            {
                SpacesControl_Main.AdjacencyCluster = value;
                MechanicalSystemsControl_Main.AdjacencyCluster = value;
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return SpacesControl_Main.Spaces;
            }

            set
            {
                SpacesControl_Main.Spaces = value;
            }
        }

        public string MechanicalSystemCategory
        {
            get
            {
                return MechanicalSystemsControl_Main.MechanicalSystemCategory;
            }

            set
            {
                MechanicalSystemsControl_Main.MechanicalSystemCategory = value;
                //Modify.UpdateMechanicalSystemsSelection(MechanicalSystemsControl_Main, AdjacencyCluster, GetSelectedSpaces());
            }
        }

        public List<Space> GetSelectedSpaces()
        {
            return SpacesControl_Main.GetSelectedSpaces();
        }

        private void Apply()
        {
            List<MechanicalSystem> mechanicalSystems = MechanicalSystemsControl_Main.SelectedMechanicalSystems;
            if (mechanicalSystems == null || mechanicalSystems.Count != 1)
            {
                return;
            }

            List<Space> spaces = SpacesControl_Main.GetSelectedSpaces();
            if(spaces == null || spaces.Count == 0)
            {
                return;
            }

            MechanicalSystem mechanicalSystem = mechanicalSystems[0];

            AdjacencyCluster adjacencyCluster = AdjacencyCluster;
            if(adjacencyCluster != null)
            {
                adjacencyCluster = new AdjacencyCluster(adjacencyCluster);
            }

            Analytical.Modify.AssignMechanicalSystem(adjacencyCluster, mechanicalSystem, spaces, false);
                        
            AdjacencyClusterChanged.Invoke(this, new AdjacencyClusterChangedEventArgs(adjacencyCluster));

            AdjacencyCluster = adjacencyCluster;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.Modal())
            {
                DialogResult = false;
            }

            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            Apply();

            if(this.Modal())
            {
                DialogResult = true;
            }

            Close();
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Modify.UpdateMechanicalSystemsSelection(MechanicalSystemsControl_Main, AdjacencyCluster, GetSelectedSpaces());
        }
    }
}
