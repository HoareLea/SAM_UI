using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for RelationClusterComplexReferenceWindow.xaml
    /// </summary>
    public partial class RelationClusterComplexReferenceWindow : Window
    {
        public RelationClusterComplexReferenceWindow()
        {
            InitializeComponent();
        }

        public RelationCluster RelationCluster
        {
            get
            {
                return RelationClusterComplexReferenceControl_Main.RelationCluster;
            }

            set
            {
                RelationClusterComplexReferenceControl_Main.RelationCluster = value;
            }
        }

        public IComplexReference ComplexReference
        {
            get
            {
                return RelationClusterComplexReferenceControl_Main.ComplexReference;
            }
        }

        public System.Type Type
        {
            get
            {
                return RelationClusterComplexReferenceControl_Main.Type;
            }

            set
            {
                RelationClusterComplexReferenceControl_Main.Type = value;
            }
        }

        public bool TypesEnabled
        {
            get
            {
                return RelationClusterComplexReferenceControl_Main.TypesEnabled;
            }

            set 
            {
                RelationClusterComplexReferenceControl_Main.TypesEnabled = value;
            }
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
