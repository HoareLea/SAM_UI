using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SpaceZoneWindow.xaml
    /// </summary>
    public partial class SpaceMechanicalSystemWindow : System.Windows.Window
    {
        public SpaceMechanicalSystemWindow()
        {
            InitializeComponent();
        }

        public SpaceMechanicalSystemWindow(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IEnumerable<Space> selectedSpaces = null, MechanicalSystem selectedMechanicalSystem = null)
        {
            InitializeComponent();

            spaceMechanicalSystemControl.AdjacencyCluster = adjacencyCluster;

            if (spaces != null)
            {
                spaceMechanicalSystemControl.Spaces = new List<Space>(spaces);
            }

            if(selectedSpaces != null)
            {
                spaceMechanicalSystemControl.SelectedSpaces = new List<Space>(selectedSpaces);
            }

            spaceMechanicalSystemControl.SelectedMechanicalSystem = selectedMechanicalSystem;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            if(spaceMechanicalSystemControl.Spaces == null || spaceMechanicalSystemControl.Spaces.Count == 0)
            {
                MessageBox.Show("Please select spaces");
                return;
            }

            if(spaceMechanicalSystemControl.SelectedMechanicalSystem == null)
            {
                MessageBox.Show("Please select Me");
                return;
            }
            
            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return spaceMechanicalSystemControl.AdjacencyCluster;
            }

            set
            {
                spaceMechanicalSystemControl.AdjacencyCluster = value;
            }
        }

        public List<Space> SelectedSpaces
        {
            get
            {
                return spaceMechanicalSystemControl.SelectedSpaces;
            }

            set
            {
                spaceMechanicalSystemControl.Spaces = value;
            }
        }

        public MechanicalSystem SelectedMechanicalSystem
        {
            get
            {
                return spaceMechanicalSystemControl.SelectedMechanicalSystem;
            }
        }

        public string MechanicalSystemCategory
        {
            get
            {
                return spaceMechanicalSystemControl.MechanicalSystemCategory;
            }

            set
            {
                spaceMechanicalSystemControl.MechanicalSystemCategory = value;
            }
        }
    }
}
